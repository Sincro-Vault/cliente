using System.Security.Cryptography;
using SecretsClient.Core.Services;

namespace SecretsClient.Infrastructure.Shamir;

public sealed class ShamirService : IShamirService
{
    private const int ThresholdOffset = 0;
    private const int ShareIndexOffset = 1;
    private const int ShareHeaderLength = 2;

    public Task<IReadOnlyList<FragmentShare>> GenerateSharesAsync(
        byte[] secretBytes,
        int totalShares,
        int threshold,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(secretBytes);
        ct.ThrowIfCancellationRequested();

        ValidateGenerationParameters(secretBytes, totalShares, threshold);

        var sharePayloads = Enumerable.Range(0, totalShares)
            .Select(_ => new byte[secretBytes.Length + ShareHeaderLength])
            .ToArray();

        for (var shareIndex = 1; shareIndex <= totalShares; shareIndex++)
        {
            var payload = sharePayloads[shareIndex - 1];
            payload[ThresholdOffset] = (byte)threshold;
            payload[ShareIndexOffset] = (byte)shareIndex;
        }

        for (var byteIndex = 0; byteIndex < secretBytes.Length; byteIndex++)
        {
            var polynomial = BuildPolynomial(secretBytes[byteIndex], threshold);

            for (var shareIndex = 1; shareIndex <= totalShares; shareIndex++)
            {
                sharePayloads[shareIndex - 1][ShareHeaderLength + byteIndex] =
                    EvaluatePolynomial(polynomial, (byte)shareIndex);
            }
        }

        IReadOnlyList<FragmentShare> shares = sharePayloads
            .Select((payload, index) => new FragmentShare(index + 1, payload, Array.Empty<byte>()))
            .ToArray();

        return Task.FromResult(shares);
    }

    public Task<byte[]> ReconstructSecretAsync(
        IReadOnlyCollection<FragmentShare> shares,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(shares);
        ct.ThrowIfCancellationRequested();

        if (shares.Count == 0)
            throw new ArgumentException("At least one share is required for reconstruction.", nameof(shares));

        var normalizedShares = NormalizeShares(shares);
        var threshold = normalizedShares[0].Payload[ThresholdOffset];

        if (normalizedShares.Count < threshold)
            throw new InvalidOperationException(
                $"At least {threshold} shares are required to reconstruct the secret.");

        var selectedShares = normalizedShares.Take(threshold).ToArray();
        var secretLength = selectedShares[0].Payload.Length - ShareHeaderLength;
        var secret = new byte[secretLength];

        for (var byteIndex = 0; byteIndex < secretLength; byteIndex++)
        {
            byte reconstructedByte = 0;

            for (var i = 0; i < selectedShares.Length; i++)
            {
                var currentShare = selectedShares[i];
                var numerator = (byte)1;
                var denominator = (byte)1;

                for (var j = 0; j < selectedShares.Length; j++)
                {
                    if (i == j)
                        continue;

                    var otherShare = selectedShares[j];
                    numerator = GaloisField256.Multiply(numerator, (byte)otherShare.Index);
                    denominator = GaloisField256.Multiply(
                        denominator,
                        GaloisField256.Add((byte)otherShare.Index, (byte)currentShare.Index));
                }

                var lagrangeBasis = GaloisField256.Divide(numerator, denominator);
                var shareValue = currentShare.Payload[ShareHeaderLength + byteIndex];
                reconstructedByte = GaloisField256.Add(
                    reconstructedByte,
                    GaloisField256.Multiply(shareValue, lagrangeBasis));
            }

            secret[byteIndex] = reconstructedByte;
        }

        return Task.FromResult(secret);
    }

    private static void ValidateGenerationParameters(byte[] secretBytes, int totalShares, int threshold)
    {
        if (secretBytes.Length == 0)
            throw new ArgumentException("Secret must not be empty.", nameof(secretBytes));

        if (totalShares is <= 0 or > 255)
            throw new ArgumentOutOfRangeException(nameof(totalShares), "Total shares must be between 1 and 255.");

        if (threshold is <= 0 or > 255)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be between 1 and 255.");

        if (threshold > totalShares)
            throw new ArgumentException("Threshold cannot be greater than total shares.", nameof(threshold));
    }

    private static byte[] BuildPolynomial(byte secretByte, int threshold)
    {
        var polynomial = new byte[threshold];
        polynomial[0] = secretByte;

        if (threshold == 1)
            return polynomial;

        RandomNumberGenerator.Fill(polynomial.AsSpan(1));

        while (polynomial[^1] == 0)
            polynomial[^1] = RandomNumberGenerator.GetBytes(1)[0];

        return polynomial;
    }

    private static byte EvaluatePolynomial(byte[] coefficients, byte x)
    {
        byte result = 0;

        for (var i = coefficients.Length - 1; i >= 0; i--)
            result = GaloisField256.Add(GaloisField256.Multiply(result, x), coefficients[i]);

        return result;
    }

    private static IReadOnlyList<(int Index, byte[] Payload)> NormalizeShares(IReadOnlyCollection<FragmentShare> shares)
    {
        var normalized = new List<(int Index, byte[] Payload)>(shares.Count);
        var seenIndexes = new HashSet<int>();

        foreach (var share in shares)
        {
            if (share.Index is <= 0 or > 255)
                throw new InvalidOperationException("Share indexes must be between 1 and 255.");

            if (!seenIndexes.Add(share.Index))
                throw new InvalidOperationException($"Duplicate share index detected: {share.Index}.");

            if (share.EncryptedShare.Length <= ShareHeaderLength)
                throw new InvalidOperationException("Share payload is invalid.");

            normalized.Add((share.Index, share.EncryptedShare));
        }

        var payloadLength = normalized[0].Payload.Length;
        var threshold = normalized[0].Payload[ThresholdOffset];

        if (threshold == 0)
            throw new InvalidOperationException("Share threshold metadata is invalid.");

        foreach (var share in normalized)
        {
            if (share.Payload.Length != payloadLength)
                throw new InvalidOperationException("All shares must have the same payload length.");

            if (share.Payload[ThresholdOffset] != threshold)
                throw new InvalidOperationException("All shares must belong to the same threshold set.");

            if (share.Payload[ShareIndexOffset] != (byte)share.Index)
                throw new InvalidOperationException("Share index metadata does not match the provided share index.");
        }

        return normalized;
    }
}
