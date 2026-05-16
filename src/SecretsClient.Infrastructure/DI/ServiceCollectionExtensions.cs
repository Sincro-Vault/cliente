using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SecretsClient.Core.Services;
using SecretsClient.Infrastructure.Application;
using SecretsClient.Infrastructure.Auth;
using SecretsClient.Infrastructure.Crypto;
using SecretsClient.Infrastructure.Geo;
using SecretsClient.Infrastructure.Repositories;
using SecretsClient.Infrastructure.Shamir;
using SecretsClient.Infrastructure.Storage;
using SecretsClient.Infrastructure.Sync;
using Polly;
using Polly.Extensions.Http;

namespace SecretsClient.Infrastructure.DI;

/// <summary>
/// Extensiones para registrar los servicios de SecretsClient en el contenedor DI
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSecretsClientServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        // === Acceso al contexto HTTP (necesario para SecretManager) ===
        services.AddHttpContextAccessor();

        // === Servicios de Autenticación ===
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRsaSignatureService, RsaSignatureService>();

        // === Servicios de Sincronización ===
        services.AddScoped<ISyncService, ServerSyncService>();

        // === HttpClient para Servidor Central con Polly Retry ===
        services.AddHttpClient<ISyncService, ServerSyncService>(client =>
        {
            var baseUrl = config["Server:BaseUrl"] ?? "https://api.empresa-servidor.com";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "SecretsClient/1.0");
        })
        .AddPolicyHandler(GetRetryPolicy());

        // === Servicios de Persona 2: Criptografía, Shamir y Geofencing ===
        services.AddSingleton<IShamirService, ShamirService>();
        services.AddSingleton<ICryptoService, CryptoService>();
        services.AddSingleton<GpsLocationProvider>();
        services.AddHttpClient<IpGeolocationLocationProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "SecretsClient/1.0");
        });
        services.AddScoped<ICurrentLocationProvider>(sp => new CompositeLocationProvider(
            sp.GetRequiredService<GpsLocationProvider>(),
            sp.GetRequiredService<IpGeolocationLocationProvider>()));
        services.AddScoped<IGeoLocationService, GeoLocationService>();
        services.AddScoped<IGeoValidator, GeoValidator>();

        // === Servicios de Persona 1: Repositorios y Storage ===
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISecretRepository, SecretRepository>();
        services.AddScoped<IFragmentRepository, FragmentRepository>();
        services.AddScoped<ISecureStorage, SecureStorage>();

        // === Cliente HTTP para hablar con el servidor central (puerto 9000) ===
        services.AddHttpClient<IFragmentSyncClient, FragmentSyncClient>(client =>
        {
            var serverUrl = config["Server:CentralUrl"] ?? "http://localhost:9000";
            client.BaseAddress = new Uri(serverUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddPolicyHandler(GetRetryPolicy());

        // === Servicios de Gestión de Negocio ===
        services.AddScoped<ISecretManager, SecretManager>();
        services.AddScoped<IHealthCheckService, HealthCheckService>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} después de {timespan.TotalSeconds}s");
                });
    }
}
