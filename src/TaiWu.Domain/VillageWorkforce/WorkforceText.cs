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

    public static string SemanticVersion(string value, string parameterName)
    {
        var normalized = Stable(value, parameterName);
        var buildParts = normalized.Split('+');
        if (buildParts.Length > 2
            || (buildParts.Length == 2
                && !ValidIdentifiers(buildParts[1], allowLeadingZero: true)))
        {
            throw InvalidSemanticVersion(parameterName);
        }

        var precedence = buildParts[0];
        var prereleaseSeparator = precedence.IndexOf('-');
        var core = prereleaseSeparator < 0
            ? precedence
            : precedence[..prereleaseSeparator];
        var prerelease = prereleaseSeparator < 0
            ? null
            : precedence[(prereleaseSeparator + 1)..];
        if (prerelease is not null
            && !ValidIdentifiers(prerelease, allowLeadingZero: false))
        {
            throw InvalidSemanticVersion(parameterName);
        }

        var coreParts = core.Split('.');
        if (coreParts.Length != 3
            || coreParts.Any(part => !ValidNumericIdentifier(part)))
        {
            throw InvalidSemanticVersion(parameterName);
        }

        return normalized;
    }

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

    private static bool ValidIdentifiers(
        string value,
        bool allowLeadingZero) =>
        value.Length > 0
        && value.Split('.').All(identifier =>
            identifier.Length > 0
            && identifier.All(character =>
                char.IsAsciiLetterOrDigit(character) || character == '-')
            && (allowLeadingZero
                || !identifier.All(char.IsAsciiDigit)
                || ValidNumericIdentifier(identifier)));

    private static bool ValidNumericIdentifier(string value) =>
        value.Length > 0
        && value.All(char.IsAsciiDigit)
        && (value.Length == 1 || value[0] != '0');

    private static ArgumentException InvalidSemanticVersion(
        string parameterName) =>
        new(
            "A rule version must be a valid MAJOR.MINOR.PATCH semantic version.",
            parameterName);
}
