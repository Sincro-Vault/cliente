namespace SecretsClient.Core.DTOs;

/// <summary>
/// Solicitud de Registro de nuevo usuario
/// </summary>
public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    byte[]? CertificatePem = null);

/// <summary>
/// Respuesta de Registro exitoso
/// </summary>
public record RegisterResponse(
    string UserId,
    string Username,
    string Email,
    DateTime CreatedAt);

/// <summary>
/// Solicitud de Login
/// </summary>
public record LoginRequest(
    string Username,
    string Password);

/// <summary>
/// Respuesta de Login exitoso
/// </summary>
public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    string TokenType = "Bearer");

/// <summary>
/// Solicitud de Refresh de Token
/// </summary>
public record RefreshRequest(
    string RefreshToken);
