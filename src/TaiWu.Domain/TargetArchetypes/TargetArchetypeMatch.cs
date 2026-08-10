using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetArchetypes;

public sealed class TargetArchetypeMatch
{
    internal TargetArchetypeMatch(
        TargetArchetypeDefinition definition,
        string profileFingerprint,
        TargetArchetypeMatchState state,
        IEnumerable<TargetProfileFacetIdentity> supportingFacets,
        IEnumerable<TargetProfileFacetIdentity> missingFacets,
        IEnumerable<TargetProfileFacetIdentity> excludingFacets,
        IEnumerable<TargetProfileFacetIdentity> conflictingFacets,
        IEnumerable<TargetArchetypeMatchDiagnostic> diagnostics)
    {
        Definition = definition
            ?? throw new ArgumentNullException(nameof(definition));
        ProfileFingerprint = TargetProfileText.Fingerprint(
            profileFingerprint,
            nameof(profileFingerprint));
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "Unknown target-archetype match state.");
        }

        SupportingFacets = CopyFacets(
            supportingFacets,
            nameof(supportingFacets));
        MissingFacets = CopyFacets(missingFacets, nameof(missingFacets));
        ExcludingFacets = CopyFacets(
            excludingFacets,
            nameof(excludingFacets));
        ConflictingFacets = CopyFacets(
            conflictingFacets,
            nameof(conflictingFacets));
        Diagnostics = CopyDiagnostics(diagnostics);
        EnsureDisjointFacetReferences();
        EnsureStateInvariants(state);
        State = state;
        StableKey = CreateStableKey();
    }

    public TargetArchetypeDefinition Definition { get; }

    public string ProfileFingerprint { get; }

    public TargetArchetypeMatchState State { get; }

    public ImmutableArray<TargetProfileFacetIdentity> SupportingFacets { get; }

    public ImmutableArray<TargetProfileFacetIdentity> MissingFacets { get; }

    public ImmutableArray<TargetProfileFacetIdentity> ExcludingFacets { get; }

    public ImmutableArray<TargetProfileFacetIdentity> ConflictingFacets { get; }

    public ImmutableArray<TargetArchetypeMatchDiagnostic> Diagnostics { get; }

    public string StableKey { get; }

    private static ImmutableArray<TargetProfileFacetIdentity> CopyFacets(
        IEnumerable<TargetProfileFacetIdentity> values,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        var facets = values.ToImmutableArray();
        if (facets.Any(facet => facet is null))
        {
            throw new ArgumentException(
                "Match facet references cannot contain null entries.",
                parameterName);
        }

        if (facets.Select(facet => facet.StableKey)
            .Distinct(StringComparer.Ordinal)
            .Count() != facets.Length)
        {
            throw new ArgumentException(
                "Match facet references must be unique.",
                parameterName);
        }

        return [.. facets
            .OrderBy(facet => facet.Dimension)
            .ThenBy(facet => facet.Code, StringComparer.Ordinal)];
    }

    private static ImmutableArray<TargetArchetypeMatchDiagnostic>
        CopyDiagnostics(IEnumerable<TargetArchetypeMatchDiagnostic> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var diagnostics = values.ToImmutableArray();
        if (diagnostics.Any(diagnostic => diagnostic is null))
        {
            throw new ArgumentException(
                "Match diagnostics cannot contain null entries.",
                nameof(values));
        }

        if (diagnostics.Select(diagnostic => diagnostic.StableKey)
            .Distinct(StringComparer.Ordinal)
            .Count() != diagnostics.Length)
        {
            throw new ArgumentException(
                "Match diagnostics must be unique.",
                nameof(values));
        }

        return [.. diagnostics.OrderBy(
            diagnostic => diagnostic.StableKey,
            StringComparer.Ordinal)];
    }

    private void EnsureDisjointFacetReferences()
    {
        var total = SupportingFacets.Length
            + MissingFacets.Length
            + ExcludingFacets.Length
            + ConflictingFacets.Length;
        var unique = SupportingFacets
            .Concat(MissingFacets)
            .Concat(ExcludingFacets)
            .Concat(ConflictingFacets)
            .Select(facet => facet.StableKey)
            .Distinct(StringComparer.Ordinal)
            .Count();
        if (unique != total)
        {
            throw new ArgumentException(
                "A facet cannot occupy more than one match-result role.");
        }
    }

    private void EnsureStateInvariants(TargetArchetypeMatchState state)
    {
        switch (state)
        {
            case TargetArchetypeMatchState.Matched:
                if (SupportingFacets.IsEmpty
                    || !MissingFacets.IsEmpty
                    || !ExcludingFacets.IsEmpty
                    || !ConflictingFacets.IsEmpty)
                {
                    throw new ArgumentException(
                        "A matched archetype requires support and no blocking "
                        + "facet references.");
                }

                break;
            case TargetArchetypeMatchState.Partial:
                if (SupportingFacets.IsEmpty
                    || MissingFacets.IsEmpty
                    || !ExcludingFacets.IsEmpty
                    || !ConflictingFacets.IsEmpty)
                {
                    throw new ArgumentException(
                        "A partial archetype requires supporting and missing "
                        + "facets with no contrary or conflicting facts.");
                }

                break;
            case TargetArchetypeMatchState.NotMatched:
                if (ExcludingFacets.IsEmpty
                    || !ConflictingFacets.IsEmpty)
                {
                    throw new ArgumentException(
                        "A not-matched archetype requires sufficient contrary "
                        + "evidence and no unresolved conflict.");
                }

                break;
            case TargetArchetypeMatchState.Unsupported:
                if (!SupportingFacets.IsEmpty
                    || !ExcludingFacets.IsEmpty
                    || !ConflictingFacets.IsEmpty)
                {
                    throw new ArgumentException(
                        "An unsupported archetype cannot contain supporting, "
                        + "excluding, or conflicting facts.");
                }

                break;
            case TargetArchetypeMatchState.Conflicting:
                if (ConflictingFacets.IsEmpty)
                {
                    throw new ArgumentException(
                        "A conflicting archetype requires a conflicting facet.");
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

        if (state != TargetArchetypeMatchState.Matched && Diagnostics.IsEmpty)
        {
            throw new ArgumentException(
                "A non-matched result requires a typed diagnostic.");
        }
    }

    private string CreateStableKey()
    {
        var canonical = TargetProfileText.Stable(
            "TARGET_ARCHETYPE_MATCH_V1",
            Definition.StableKey,
            ProfileFingerprint,
            ((int)State).ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            TargetProfileText.StableCollection(
                SupportingFacets.Select(facet => facet.StableKey)),
            TargetProfileText.StableCollection(
                MissingFacets.Select(facet => facet.StableKey)),
            TargetProfileText.StableCollection(
                ExcludingFacets.Select(facet => facet.StableKey)),
            TargetProfileText.StableCollection(
                ConflictingFacets.Select(facet => facet.StableKey)),
            TargetProfileText.StableCollection(
                Diagnostics.Select(diagnostic => diagnostic.StableKey)));
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
