namespace SecretsClient.Infrastructure.Shamir;

internal static class GaloisField256
{
    private const int IrreduciblePolynomial = 0x11B;

    public static byte Add(byte left, byte right) => (byte)(left ^ right);

    public static byte Multiply(byte left, byte right)
    {
        int a = left;
        int b = right;
        int product = 0;

        while (b > 0)
        {
            if ((b & 1) != 0)
                product ^= a;

            a <<= 1;

            if ((a & 0x100) != 0)
                a ^= IrreduciblePolynomial;

            b >>= 1;
        }

        return (byte)product;
    }

    public static byte Divide(byte dividend, byte divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide by zero in GF(256).");

        if (dividend == 0)
            return 0;

        return Multiply(dividend, Inverse(divisor));
    }

    public static byte Inverse(byte value)
    {
        if (value == 0)
            throw new DivideByZeroException("Zero does not have a multiplicative inverse in GF(256).");

        return Pow(value, 254);
    }

    private static byte Pow(byte value, int exponent)
    {
        var result = (byte)1;
        var baseValue = value;
        var remaining = exponent;

        while (remaining > 0)
        {
            if ((remaining & 1) != 0)
                result = Multiply(result, baseValue);

            baseValue = Multiply(baseValue, baseValue);
            remaining >>= 1;
        }

        return result;
    }
}
