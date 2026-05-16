using SecretsClient.Core.DTOs;

namespace SecretsClient.Core.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<LoginResponse> RefreshTokenAsync(RefreshRequest request, CancellationToken ct);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task LogoutAsync(string userId, CancellationToken ct);
    Task<bool> ValidateTokenAsync(string token, CancellationToken ct);
}
