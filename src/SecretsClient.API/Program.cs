using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using SecretsClient.Infrastructure.DI;
using Microsoft.EntityFrameworkCore;
using SecretsClient.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// ===== Agregar Servicios =====

// 0. Base de Datos (SQL Server Express)
builder.Services.AddDbContext<SecretsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
    // Política permisiva: permite cualquier origen LAN (localhost y cualquier IP de la red local).
    // Los Allowed origins extra pueden agregarse vía la sección "Cors:Origins" del appsettings.json.
    options.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SecretsClient API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
