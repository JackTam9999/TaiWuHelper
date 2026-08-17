namespace TaiWu.Domain.CompanionRoles;

internal static class CompanionRoleText
{
    public static string Stable(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A stable role identity cannot be blank.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > 160
            || normalized.IndexOfAny(['|', '/', '\\', '\r', '\n']) >= 0)
        {
            throw new ArgumentException(
                "A stable role identity cannot contain delimiters, path separators, or line breaks.",
                parameterName);
        }

        return normalized;
    }

    public static string EnumKey<T>(T value) where T : struct, Enum =>
        Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
}
