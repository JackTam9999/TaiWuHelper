namespace TaiWu.Domain.TargetProfiles;

internal static class TargetProfileText
{
    internal static string Stable(params string[] parts)
    {
        var value = new System.Text.StringBuilder();
        foreach (var part in parts)
        {
            value.Append(part.Length).Append(':').Append(part);
        }

        return value.ToString();
    }

    internal static string StableCollection(IEnumerable<string> values) =>
        Stable([.. values]);

    internal static string Code(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > 128
            || !IsAsciiLetterOrDigit(normalized[0])
            || normalized.Any(character =>
                !IsAsciiUpperOrDigit(character)
                && character is not '.' and not '_' and not '-' and not ':'))
        {
            throw new ArgumentException(
                "A stable code must start with an uppercase ASCII letter or "
                + "digit and contain only uppercase ASCII letters, digits, "
                + "periods, underscores, hyphens, or colons.",
                parameterName);
        }

        return normalized;
    }

    internal static string Version(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > 128
            || !IsAsciiLetterOrDigit(normalized[0])
            || normalized.Any(character =>
                !IsAsciiLetterOrDigit(character)
                && character is not '.' and not '_' and not '-' and not '+'))
        {
            throw new ArgumentException(
                "A source version must start with an ASCII letter or digit "
                + "and contain only ASCII letters, digits, periods, "
                + "underscores, hyphens, or plus signs.",
                parameterName);
        }

        return normalized;
    }

    internal static string ResourceKey(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > 256
            || !IsAsciiLetterOrDigit(normalized[0])
            || normalized.Any(character =>
                !IsAsciiLetterOrDigit(character)
                && character is not '.' and not '_' and not '-' and not ':'))
        {
            throw new ArgumentException(
                "A resource key must start with an ASCII letter or digit and "
                + "contain only ASCII letters, digits, periods, underscores, "
                + "hyphens, or colons.",
                parameterName);
        }

        return normalized;
    }

    internal static string Fingerprint(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length != 64
            || normalized.Any(character =>
                character is not (>= '0' and <= '9')
                    and not (>= 'A' and <= 'F')))
        {
            throw new ArgumentException(
                "A profile fingerprint must contain 64 uppercase hexadecimal "
                + "characters.",
                parameterName);
        }

        return normalized;
    }

    internal static string? OptionalDetail(
        string? value,
        string parameterName)
    {
        if (value is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "An optional detail cannot be blank when supplied.",
                parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > 2048)
        {
            throw new ArgumentException(
                "An optional detail cannot exceed 2048 characters.",
                parameterName);
        }

        return normalized;
    }

    private static bool IsAsciiLetterOrDigit(char value) =>
        value is >= 'A' and <= 'Z'
            or >= 'a' and <= 'z'
            or >= '0' and <= '9';

    private static bool IsAsciiUpperOrDigit(char value) =>
        value is >= 'A' and <= 'Z'
            or >= '0' and <= '9';
}
