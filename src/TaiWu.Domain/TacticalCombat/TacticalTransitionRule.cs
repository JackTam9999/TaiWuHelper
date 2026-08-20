using System.Collections.Immutable;

namespace TaiWu.Domain.TacticalCombat;

public sealed class TacticalTransitionRule
{
    public TacticalTransitionRule(
        TacticalTransitionIdentity identity,
        TacticalSemanticVersion semanticVersion,
        IEnumerable<string> supportedGameDataVersions,
        TacticalRulePurpose purpose,
        TacticalTransitionTiming timing,
        IEnumerable<TacticalFactIdentity> triggerFacts,
        IEnumerable<TacticalFactIdentity> resultingFacts,
        IEnumerable<string> targetGoalCodes,
        IEnumerable<TacticalRuleEvidenceRequirement> evidenceRequirements,
        string limitationIdentity,
        IEnumerable<TacticalEvidenceReference> evidence)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        SemanticVersion = semanticVersion
            ?? throw new ArgumentNullException(nameof(semanticVersion));
        SupportedGameDataVersions = TacticalRuleCollections.Versions(
            supportedGameDataVersions,
            nameof(supportedGameDataVersions));
        Purpose = TacticalCombatText.Defined(purpose, nameof(purpose));
        Timing = TacticalCombatText.Defined(timing, nameof(timing));
        TriggerFacts = TacticalCombatText.CopyUnique(
            triggerFacts,
            item => item.StableKey,
            "transition-rule trigger fact",
            nameof(triggerFacts));
        ResultingFacts = TacticalCombatText.CopyUnique(
            resultingFacts,
            item => item.StableKey,
            "transition-rule resulting fact",
            nameof(resultingFacts));
        TargetGoalCodes = TacticalRuleCollections.Goals(
            targetGoalCodes,
            nameof(targetGoalCodes));
        EvidenceRequirements = TacticalCombatText.CopyUnique(
            evidenceRequirements,
            item => item.StableKey,
            "transition-rule evidence requirement",
            nameof(evidenceRequirements));
        LimitationIdentity = TacticalCombatText.Code(
            limitationIdentity,
            nameof(limitationIdentity));
        Evidence = TacticalCombatText.CopyUnique(
            evidence,
            item => item.StableKey,
            "transition-rule evidence",
            nameof(evidence));
        if (TriggerFacts.IsEmpty
            || ResultingFacts.IsEmpty
            || EvidenceRequirements.IsEmpty
            || Evidence.IsEmpty)
        {
            throw new ArgumentException(
                "A tactical transition rule requires trigger facts, resulting facts, evidence requirements, and evidence.");
        }

        ValidateEvidenceVersions();
    }

    public TacticalTransitionIdentity Identity { get; }

    public TacticalSemanticVersion SemanticVersion { get; }

    public ImmutableArray<string> SupportedGameDataVersions { get; }

    public TacticalRulePurpose Purpose { get; }

    public TacticalTransitionTiming Timing { get; }

    public ImmutableArray<TacticalFactIdentity> TriggerFacts { get; }

    public ImmutableArray<TacticalFactIdentity> ResultingFacts { get; }

    public ImmutableArray<string> TargetGoalCodes { get; }

    public ImmutableArray<TacticalRuleEvidenceRequirement> EvidenceRequirements
    { get; }

    public string LimitationIdentity { get; }

    public ImmutableArray<TacticalEvidenceReference> Evidence { get; }

    internal string StableKey => Identity.StableKey;

    internal string ContentKey => string.Join('|',
        StableKey,
        SemanticVersion.StableKey,
        TacticalCombatText.EnumKey(Purpose),
        TacticalCombatText.EnumKey(Timing),
        LimitationIdentity,
        string.Join("||", SupportedGameDataVersions),
        string.Join("||", TriggerFacts.Select(item => item.StableKey)),
        string.Join("||", ResultingFacts.Select(item => item.StableKey)),
        string.Join("||", TargetGoalCodes),
        string.Join("||", EvidenceRequirements.Select(item => item.StableKey)),
        string.Join("||", Evidence.Select(item => item.StableKey)));

    private void ValidateEvidenceVersions()
    {
        var supported = SupportedGameDataVersions.ToHashSet(
            StringComparer.Ordinal);
        if (Evidence.Any(item => !supported.Contains(item.GameDataVersion)))
        {
            throw new ArgumentException(
                "Transition-rule evidence must use a supported GameData version.",
                nameof(Evidence));
        }

        var expectedRuleVersion =
            $"TACTICAL_COMBAT_RULES@{SemanticVersion}";
        if (Evidence.Any(item => !string.Equals(
                item.RuleVersion,
                expectedRuleVersion,
                StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "Transition-rule evidence must match its semantic rule version.",
                nameof(Evidence));
        }
    }
}
