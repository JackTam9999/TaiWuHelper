using System.Collections.Immutable;
using System.Globalization;
using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetPlaybooks;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybookComposition;

public sealed class TargetPlaybookAdjustmentEvidence
{
    internal TargetPlaybookAdjustmentEvidence(
        TargetPlaybookAdjustmentEvidenceKind kind,
        TargetPlaybookAdjustmentEvidenceState state,
        string identity,
        IEnumerable<string> evidenceReferences)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        Identity = TargetProfileText.Code(identity, nameof(identity));
        ArgumentNullException.ThrowIfNull(evidenceReferences);
        var references = evidenceReferences
            .Select(value =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(
                    value,
                    nameof(evidenceReferences));
                return value.Trim();
            })
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        if (references.Length == 0)
        {
            throw new ArgumentException(
                "Adjustment evidence requires a source reference.",
                nameof(evidenceReferences));
        }

        Kind = kind;
        State = state;
        EvidenceReferences = references;
    }

    public TargetPlaybookAdjustmentEvidenceKind Kind { get; }

    public TargetPlaybookAdjustmentEvidenceState State { get; }

    public string Identity { get; }

    public ImmutableArray<string> EvidenceReferences { get; }

    public string StableKey => TargetProfileText.Stable(
        ((int)Kind).ToString(CultureInfo.InvariantCulture),
        ((int)State).ToString(CultureInfo.InvariantCulture),
        Identity,
        TargetProfileText.StableCollection(EvidenceReferences));

    internal static TargetPlaybookAdjustmentEvidence FromFacet(
        TargetProfileFacet facet) => new(
            TargetPlaybookAdjustmentEvidenceKind.ProfileFacet,
            EvidenceState(facet.State),
            FacetIdentity(facet.Identity),
            facet.Evidence.Select(evidence => evidence.Reference));

    internal static IEnumerable<TargetPlaybookAdjustmentEvidence>
        FromFacetSources(TargetProfileFacet facet)
    {
        foreach (var evidence in facet.Evidence)
        {
            var kind = evidence.SourceKind switch
            {
                TargetProfileEvidenceSourceKind.CurrentScreenObservation =>
                    TargetPlaybookAdjustmentEvidenceKind.Observation,
                TargetProfileEvidenceSourceKind.SavedEquippedMembership
                    when facet.Identity.Dimension
                        == TargetProfileDimension.AttackFamily =>
                    TargetPlaybookAdjustmentEvidenceKind.Equipment,
                TargetProfileEvidenceSourceKind.SavedLoadoutSource =>
                    TargetPlaybookAdjustmentEvidenceKind.Equipment,
                TargetProfileEvidenceSourceKind.SavedEquippedMembership =>
                    TargetPlaybookAdjustmentEvidenceKind.Skill,
                TargetProfileEvidenceSourceKind.InstalledConfiguration =>
                    TargetPlaybookAdjustmentEvidenceKind.Effect,
                _ => TargetPlaybookAdjustmentEvidenceKind.ProfileFacet
            };
            yield return new TargetPlaybookAdjustmentEvidence(
                kind,
                EvidenceState(facet.State),
                $"{kind.ToString().ToUpperInvariant()}:"
                + FacetIdentity(facet.Identity),
                [evidence.Reference]);
        }
    }

    internal static TargetPlaybookAdjustmentEvidence FromThreat(
        AnalyzedTargetThreat threat) => new(
            TargetPlaybookAdjustmentEvidenceKind.Threat,
            TargetPlaybookAdjustmentEvidenceState.Confirmed,
            $"THREAT:{threat.Threat.Code}",
            threat.Sources.Select(source => source.EvidenceReference));

    internal static TargetPlaybookAdjustmentEvidence FromThreatSource(
        TargetThreatSource source,
        TargetPlaybookAdjustmentEvidenceKind kind)
    {
        if (kind is not (
            TargetPlaybookAdjustmentEvidenceKind.Skill
            or TargetPlaybookAdjustmentEvidenceKind.Effect))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        var identity = kind == TargetPlaybookAdjustmentEvidenceKind.Skill
            ? $"SKILL:{source.SkillId}"
            : $"EFFECT:{source.SkillId}:{(int)source.Direction}:"
                + source.RawEffectId;
        return new TargetPlaybookAdjustmentEvidence(
            kind,
            TargetPlaybookAdjustmentEvidenceState.Confirmed,
            identity,
            [source.EvidenceReference]);
    }

    internal static TargetPlaybookAdjustmentEvidence FromGap(
        TargetCounterPlaybookGap gap) => new(
            TargetPlaybookAdjustmentEvidenceKind.Gap,
            TargetPlaybookAdjustmentEvidenceState.Incomplete,
            $"GAP:{gap.Code}",
            gap.EvidenceReferences);

    internal static TargetPlaybookAdjustmentEvidence FromMatch(
        TargetArchetypeMatch match) => new(
            TargetPlaybookAdjustmentEvidenceKind.ArchetypeMatch,
            match.State switch
            {
                TargetArchetypeMatchState.Matched =>
                    TargetPlaybookAdjustmentEvidenceState.Confirmed,
                TargetArchetypeMatchState.NotMatched =>
                    TargetPlaybookAdjustmentEvidenceState.Contrary,
                _ => TargetPlaybookAdjustmentEvidenceState.Incomplete
            },
            $"ARCHETYPE:{match.Definition.Identity.Code}",
            match.Definition.EvidenceReferences);

    internal static string FacetIdentity(
        TargetProfileFacetIdentity facet) =>
        $"FACET:{(int)facet.Dimension}:{facet.Code}";

    private static TargetPlaybookAdjustmentEvidenceState EvidenceState(
        TargetProfileEvidenceState state) => state switch
        {
            TargetProfileEvidenceState.Confirmed =>
                TargetPlaybookAdjustmentEvidenceState.Confirmed,
            TargetProfileEvidenceState.Incomplete
                or TargetProfileEvidenceState.Unsupported
                or TargetProfileEvidenceState.Conflicting =>
                TargetPlaybookAdjustmentEvidenceState.Incomplete,
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };
}
