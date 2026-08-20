using System.Collections.Immutable;
using System.Globalization;

namespace TaiWu.Domain.TacticalCombat;

public sealed record TacticalSemanticVersion
{
    public TacticalSemanticVersion(int major, int minor, int patch)
    {
        if (major < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(major));
        }

        if (minor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minor));
        }

        if (patch < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(patch));
        }

        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public int Major { get; }

    public int Minor { get; }

    public int Patch { get; }

    public override string ToString() => string.Join('.',
        Major.ToString(CultureInfo.InvariantCulture),
        Minor.ToString(CultureInfo.InvariantCulture),
        Patch.ToString(CultureInfo.InvariantCulture));

    internal string StableKey => ToString();
}

public sealed record TacticalRuleEvidenceIdentity
{
    public TacticalRuleEvidenceIdentity(string code) =>
        Code = TacticalCombatText.Code(code, nameof(code));

    public string Code { get; }

    internal string StableKey => Code;
}

public sealed record TacticalRuleEvidenceRequirement
{
    public TacticalRuleEvidenceRequirement(
        TacticalRuleEvidenceIdentity identity,
        TacticalRuleEvidenceScope scope,
        TacticalEvidenceSourceKind source,
        TacticalRuleEvidenceDisposition requiredDisposition =
            TacticalRuleEvidenceDisposition.Confirmed)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Scope = TacticalCombatText.Defined(scope, nameof(scope));
        Source = TacticalCombatText.Defined(source, nameof(source));
        RequiredDisposition = TacticalCombatText.Defined(
            requiredDisposition,
            nameof(requiredDisposition));
        if (RequiredDisposition != TacticalRuleEvidenceDisposition.Confirmed)
        {
            throw new ArgumentException(
                "Version-1 tactical rules can require only confirmed evidence.",
                nameof(requiredDisposition));
        }
    }

    public TacticalRuleEvidenceIdentity Identity { get; }

    public TacticalRuleEvidenceScope Scope { get; }

    public TacticalEvidenceSourceKind Source { get; }

    public TacticalRuleEvidenceDisposition RequiredDisposition { get; }

    internal string StableKey => string.Join('|',
        Identity.StableKey,
        TacticalCombatText.EnumKey(Scope),
        TacticalCombatText.EnumKey(Source),
        TacticalCombatText.EnumKey(RequiredDisposition));
}

public sealed record TacticalRuleEvidenceObservation
{
    public TacticalRuleEvidenceObservation(
        TacticalRuleEvidenceIdentity identity,
        TacticalRuleEvidenceScope scope,
        TacticalEvidenceSourceKind source,
        TacticalRuleEvidenceDisposition disposition,
        TacticalEvidenceReference evidence)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Scope = TacticalCombatText.Defined(scope, nameof(scope));
        Source = TacticalCombatText.Defined(source, nameof(source));
        Disposition = TacticalCombatText.Defined(
            disposition,
            nameof(disposition));
        Evidence = evidence
            ?? throw new ArgumentNullException(nameof(evidence));
        if (Evidence.Source != Source)
        {
            throw new ArgumentException(
                "Rule evidence observation source must match its evidence reference.",
                nameof(evidence));
        }
    }

    public TacticalRuleEvidenceIdentity Identity { get; }

    public TacticalRuleEvidenceScope Scope { get; }

    public TacticalEvidenceSourceKind Source { get; }

    public TacticalRuleEvidenceDisposition Disposition { get; }

    public TacticalEvidenceReference Evidence { get; }

    internal string StableKey => string.Join('|',
        Identity.StableKey,
        TacticalCombatText.EnumKey(Scope),
        TacticalCombatText.EnumKey(Source),
        TacticalCombatText.EnumKey(Disposition),
        Evidence.StableKey);
}

internal static class TacticalRuleCollections
{
    internal static ImmutableArray<string> Versions(
        IEnumerable<string> versions,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(versions);
        var copied = versions
            .Select(value => TacticalCombatText.Stable(value, parameterName))
            .ToImmutableArray();
        if (copied.IsEmpty
            || copied.Distinct(StringComparer.Ordinal).Count() != copied.Length)
        {
            throw new ArgumentException(
                "A tactical rule requires unique supported source versions.",
                parameterName);
        }

        return [.. copied.Order(StringComparer.Ordinal)];
    }

    internal static ImmutableArray<string> Goals(
        IEnumerable<string> goals,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(goals);
        var copied = goals
            .Select(value => TacticalCombatText.Code(value, parameterName))
            .ToImmutableArray();
        if (copied.IsEmpty
            || copied.Distinct(StringComparer.Ordinal).Count() != copied.Length)
        {
            throw new ArgumentException(
                "A tactical rule requires unique exact-target goals.",
                parameterName);
        }

        return [.. copied.Order(StringComparer.Ordinal)];
    }
}
