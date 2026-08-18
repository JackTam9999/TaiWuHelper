using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TaiWu.Domain.VillageWorkforce;

internal static class WorkforceText
{
    public static string Stable(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A stable identity cannot be blank.",
                parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > 160
            || normalized.IndexOfAny(['|', '/', '\\', '\r', '\n']) >= 0)
        {
            throw new ArgumentException(
                "A stable identity must be at most 160 characters and cannot contain delimiters, path separators, or line breaks.",
                parameterName);
        }

        return normalized;
    }

    public static string Version(string value, string parameterName) =>
        Stable(value, parameterName);

    public static string EnumKey<T>(T value) where T : struct, Enum =>
        Convert.ToInt32(value, CultureInfo.InvariantCulture)
            .ToString(CultureInfo.InvariantCulture);

    public static string Number<T>(T value) where T : IFormattable =>
        value.ToString(null, CultureInfo.InvariantCulture);

    public static string Fingerprint(string canonical) =>
        Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(canonical)));

    public static void Defined<T>(T value, string parameterName)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Unknown {typeof(T).Name} value.");
        }
    }

    public static string Sha256(string value, string parameterName)
    {
        if (value is null
            || value.Length != 64
            || value.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "A SHA-256 identity must contain exactly 64 hexadecimal characters.",
                parameterName);
        }

        return value.ToUpperInvariant();
    }
}
