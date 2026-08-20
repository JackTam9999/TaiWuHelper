using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace TaiWu.Domain.TacticalCombat;

internal static class TacticalCombatText
{
    internal static T Defined<T>(T value, string parameterName)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"{typeof(T).Name} must be defined.");
        }

        return value;
    }

    internal static string Stable(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A stable value cannot be blank.",
                parameterName);
        }

        var trimmed = value.Trim();
        if (trimmed.Contains('\r', StringComparison.Ordinal)
            || trimmed.Contains('\n', StringComparison.Ordinal)
            || trimmed.Contains('|', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A stable value cannot contain line or field separators.",
                parameterName);
        }

        return trimmed;
    }

    internal static string Code(string? value, string parameterName)
    {
        var code = Stable(value, parameterName);
        if (code.Length > 160
            || code.Any(character =>
                !char.IsAsciiLetterUpper(character)
                && !char.IsAsciiDigit(character)
                && character is not '_' and not '-' and not '.' and not ':'))
        {
            throw new ArgumentException(
                "A stable code accepts only uppercase ASCII letters, digits, underscore, hyphen, period, and colon.",
                parameterName);
        }

        return code;
    }

    internal static string EnumKey<T>(T value)
        where T : struct, Enum => value.ToString().ToUpperInvariant();

    internal static string Fingerprint(string canonical) =>
        Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(canonical)));

    internal static string ValidateFingerprint(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length != 64
            || !value.All(Uri.IsHexDigit))
        {
            throw new ArgumentException(
                "A semantic fingerprint must contain 64 hexadecimal characters.",
                parameterName);
        }

        return value.ToUpperInvariant();
    }

    internal static ImmutableArray<T> CopyUnique<T>(
        IEnumerable<T> source,
        Func<T, string> keySelector,
        string itemName,
        string parameterName)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        var copied = source.ToImmutableArray();
        if (copied.Any(item => item is null))
        {
            throw new ArgumentException(
                $"A tactical contract cannot contain a null {itemName}.",
                parameterName);
        }

        if (copied.GroupBy(keySelector, StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                $"A tactical contract cannot contain duplicate {itemName} identities.",
                parameterName);
        }

        return [.. copied.OrderBy(keySelector, StringComparer.Ordinal)];
    }
}
