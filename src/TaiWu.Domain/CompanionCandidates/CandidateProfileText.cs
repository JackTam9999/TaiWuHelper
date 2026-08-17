using System.Globalization;

namespace TaiWu.Domain.CompanionCandidates;

internal static class CandidateProfileText
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

    public static string Detail(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Diagnostic detail cannot be blank.", parameterName);
        }

        return value.Trim();
    }

    public static string EnumKey<T>(T value) where T : struct, Enum =>
        Convert.ToInt32(value, CultureInfo.InvariantCulture)
            .ToString(CultureInfo.InvariantCulture);
}
