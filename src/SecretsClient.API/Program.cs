using System.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Security.Authentication;
using SecretsClient.Infrastructure.DI;
using Microsoft.EntityFrameworkCore;
using SecretsClient.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Kestrel: aceptar TLS 1.2 y 1.3 (preferencia 1.3 si el SO lo soporta).
// Windows 10 SChannel solo soporta TLS 1.2; Windows 11+ y Linux soportan 1.3 nativo.
// HTTP plano (puerto 8080) sigue funcionando en paralelo sin cambios.
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
    });
});

// ===== Agregar Servicios =====

// 0. Base de Datos (SQLite — local por instancia, ver CLAUDE.md)
// Usamos AppData en entorno nativo para evitar problemas de permisos o pérdida de la base de datos
var dbPath = "secrets.db";
if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
{
    var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    var vaultDir = System.IO.Path.Combine(appDataPath, "SincroVault");
    if (!System.IO.Directory.Exists(vaultDir)) System.IO.Directory.CreateDirectory(vaultDir);
    dbPath = System.IO.Path.Combine(vaultDir, "secrets.db");
}
var connectionString = $"Data Source={dbPath}";

builder.Services.AddDbContext<SecretsDbContext>(options =>
    options.UseSqlite(connectionString));

// 1. Autenticación JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT:SecretKey no configurado"));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// 2. Controllers
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // Política permisiva: permite cualquier origen LAN (localhost y cualquier IP de la red local).
    // Los Allowed origins extra pueden agregarse vía la sección "Cors:Origins" del appsettings.json.
    var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();

    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();

        policy.SetIsOriginAllowed(origin =>
        {
            // Permitir explícitamente el origen solicitado
            if (string.Equals(origin, "https://vault.haroldsoftware.com", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Permitir orígenes configurados en appsettings.json
            if (allowedOrigins.Any(o => string.Equals(o, origin, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Permitir localhost, 127.0.0.1 y IPs locales LAN
            try
            {
                var uri = new Uri(origin);
                return uri.Host == "localhost" || 
                       uri.Host == "127.0.0.1" || 
                       uri.Host.StartsWith("192.168.") || 
                       uri.Host.StartsWith("10.") || 
                       uri.Host.StartsWith("172.");
            }
            catch
            {
                return false;
            }
        });
    });
});

// 3. Swagger/OpenAPI con soporte JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SecretsClient API",
        Version = "v1",
        Description = "API de Cliente Local para Gestión de Secretos con Fragmentación Criptográfica"
    });

    // Agregar definición de seguridad JWT
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Authorization header usando esquema Bearer",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // Requerir JWT en endpoints protegidos
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// 4. Registrar servicios de SecretsClient (Persona 3)
builder.Services.AddSecretsClientServices(builder.Configuration);

// TODO: Cuando Persona 1 y 2 completen sus servicios, descomentar:
// builder.Services.AddStorageServices(builder.Configuration);
// builder.Services.AddCryptographyServices();

var app = builder.Build();

// ===== Configurar Pipeline HTTP =====

// Middleware de diagnóstico profundo para peticiones y CORS
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var origin = context.Request.Headers["Origin"].ToString();
    var method = context.Request.Method;
    var path = context.Request.Path;
    
    logger.LogInformation($"[HTTP IN] {method} {path} | Origin header: '{origin}'");
    
    context.Response.OnStarting(() =>
    {
        var allowOrigin = context.Response.Headers["Access-Control-Allow-Origin"].ToString();
        logger.LogInformation($"[HTTP OUT] {method} {path} | Access-Control-Allow-Origin: '{allowOrigin}'");
        return Task.CompletedTask;
    });

    await next(context);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SecretsClient API v1");
        c.RoutePrefix = string.Empty;
    });
}

// Aplicar migraciones EF Core automaticamente.
// Vital para cuando se distribuye como ejecutable nativo a los usuarios.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SecretsDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Aplicando migraciones EF Core...");
        db.Database.Migrate();
        logger.LogInformation("Migraciones aplicadas correctamente.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error aplicando migraciones EF Core");
        throw;
    }
}

// NO usar UseHttpsRedirection — rompería el flujo HTTP del frontend con redirect 307.
// HTTP y HTTPS coexisten en paralelo (8080 y 8443). El frontend elige cual usar via .env.
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
