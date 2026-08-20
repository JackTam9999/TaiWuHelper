using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.TacticalCombat;

public sealed record TacticalCandidateDiscoveryLimits
{
    public TacticalCandidateDiscoveryLimits(
        int maxLearnedSkills = 4096,
        int maxExamplesPerReason = 5)
    {
        if (maxLearnedSkills <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLearnedSkills));
        }

        if (maxExamplesPerReason <= 0 || maxExamplesPerReason > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExamplesPerReason));
        }

        MaxLearnedSkills = maxLearnedSkills;
        MaxExamplesPerReason = maxExamplesPerReason;
    }

    public int MaxLearnedSkills { get; }

    public int MaxExamplesPerReason { get; }

    public static TacticalCandidateDiscoveryLimits Default { get; } = new();
}

public sealed record TacticalCandidateRoleProjection
{
    internal TacticalCandidateRoleProjection(TacticalSkillRoleRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        Identity = rule.Identity;
        Purpose = rule.Purpose;
        Timing = rule.Timing;
        SkillId = rule.SkillId;
        Direction = rule.Direction;
        RawEffectId = rule.RawEffectId;
        RequiredMechanics = rule.RequiredMechanics;
        TargetGoalCodes = rule.TargetGoalCodes;
        TransitionIdentities = rule.Transitions;
        LimitationIdentity = rule.LimitationIdentity;
        EvidenceIdentities =
        [
            .. rule.Evidence.Select(item => item.EvidenceIdentity)
                .Order(StringComparer.Ordinal)
        ];
    }

    public TacticalRoleIdentity Identity { get; }

    public TacticalRulePurpose Purpose { get; }

    public TacticalTransitionTiming Timing { get; }

    public int SkillId { get; }

    public PracticeDirection Direction { get; }

    public int RawEffectId { get; }

    public ImmutableArray<CombatEffectMechanic> RequiredMechanics { get; }

    public ImmutableArray<string> TargetGoalCodes { get; }

    public ImmutableArray<TacticalTransitionIdentity> TransitionIdentities
    { get; }

    public string LimitationIdentity { get; }

    public ImmutableArray<string> EvidenceIdentities { get; }

    internal string SemanticKey => string.Join('|',
        Identity.StableKey,
        TacticalCombatText.EnumKey(Purpose),
        TacticalCombatText.EnumKey(Timing),
        SkillId.ToString(CultureInfo.InvariantCulture),
        TacticalCombatText.EnumKey(Direction),
        RawEffectId.ToString(CultureInfo.InvariantCulture),
        string.Join("||", RequiredMechanics.Select(
            TacticalCombatText.EnumKey)),
        string.Join("||", TargetGoalCodes),
        string.Join("||", TransitionIdentities.Select(item => item.StableKey)),
        LimitationIdentity,
        string.Join("||", EvidenceIdentities));
}

public sealed class TacticalCandidateDiscoveryEntry
{
    internal TacticalCandidateDiscoveryEntry(
        TacticalCandidateConsideration consideration,
        SkillCategory category,
        bool requiresBreakthrough,
        bool isCurrentlyEquipped,
        TacticalCandidateSupportState supportState,
        TacticalCandidateAdmissionState admissionState,
        TacticalContextFact<int> observedRawEffectId,
        TacticalContextFact<int> effectiveCost,
        TacticalCandidateRoleProjection? role,
        IEnumerable<TacticalCandidateGateResult> gates)
    {
        Consideration = consideration
            ?? throw new ArgumentNullException(nameof(consideration));
        Category = TacticalCombatText.Defined(category, nameof(category));
        RequiresBreakthrough = requiresBreakthrough;
        IsCurrentlyEquipped = isCurrentlyEquipped;
        SupportState = TacticalCombatText.Defined(
            supportState,
            nameof(supportState));
        AdmissionState = TacticalCombatText.Defined(
            admissionState,
            nameof(admissionState));
        ObservedRawEffectId = observedRawEffectId
            ?? throw new ArgumentNullException(nameof(observedRawEffectId));
        EffectiveCost = effectiveCost
            ?? throw new ArgumentNullException(nameof(effectiveCost));
        Role = role;
        Gates = TacticalCombatText.CopyUnique(
            gates,
            item => TacticalCombatText.EnumKey(item.Kind),
            "candidate gate",
            nameof(gates));
        if (Gates.Length != Enum.GetValues<TacticalCandidateGateKind>().Length)
        {
            throw new ArgumentException(
                "Every candidate consideration requires one result for every gate.",
                nameof(gates));
        }

        if ((SupportState == TacticalCandidateSupportState.VerifiedRole)
            != (Role is not null))
        {
            throw new ArgumentException(
                "Only a verified-role consideration can expose a role.",
                nameof(role));
        }

        if (Role is not null
            && (Role.SkillId != SkillId || Role.Direction != Direction))
        {
            throw new ArgumentException(
                "Candidate role identity must match its skill direction.",
                nameof(role));
        }

        var expectedDecision = AdmissionState switch
        {
            TacticalCandidateAdmissionState.Admitted =>
                TacticalCandidateDecision.Admitted,
            TacticalCandidateAdmissionState.Infeasible =>
                TacticalCandidateDecision.Rejected,
            _ => TacticalCandidateDecision.Unsupported
        };
        if (Consideration.Decision != expectedDecision)
        {
            throw new ArgumentException(
                "Candidate discovery admission must match the core decision.",
                nameof(consideration));
        }
    }

    public TacticalCandidateConsideration Consideration { get; }

    public int SkillId => Consideration.Identity.SkillId;

    public SkillCategory Category { get; }

    public PracticeDirection Direction => Consideration.Identity.Direction;

    public bool RequiresBreakthrough { get; }

    public bool IsCurrentlyEquipped { get; }

    public TacticalCandidateSupportState SupportState { get; }

    public TacticalCandidateAdmissionState AdmissionState { get; }

    public bool IsAdmitted =>
        AdmissionState == TacticalCandidateAdmissionState.Admitted;

    public TacticalContextFact<int> ObservedRawEffectId { get; }

    public TacticalContextFact<int> EffectiveCost { get; }

    public TacticalCandidateRoleProjection? Role { get; }

    public ImmutableArray<TacticalCandidateGateResult> Gates { get; }

    public string StableKey => Consideration.StableKey;

    internal string SemanticKey => string.Join('|',
        Consideration.ContentKey,
        TacticalCombatText.EnumKey(Category),
        RequiresBreakthrough ? "BREAKTHROUGH" : "NO_BREAKTHROUGH",
        IsCurrentlyEquipped ? "EQUIPPED" : "NOT_EQUIPPED",
        TacticalCombatText.EnumKey(SupportState),
        TacticalCombatText.EnumKey(AdmissionState),
        ObservedRawEffectId.SemanticKey(
            ObservedRawEffectId.IsAvailable
                ? ObservedRawEffectId.Value.ToString(CultureInfo.InvariantCulture)
                : "NONE"),
        EffectiveCost.SemanticKey(
            EffectiveCost.IsAvailable
                ? EffectiveCost.Value.ToString(CultureInfo.InvariantCulture)
                : "NONE"),
        Role?.SemanticKey ?? "NO_ROLE",
        string.Join("||", Gates.Select(item => item.SemanticKey)));
}

public sealed record TacticalCandidateCount
{
    public TacticalCandidateCount(
        TacticalCandidateAdmissionState state,
        int count)
    {
        State = TacticalCombatText.Defined(state, nameof(state));
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        Count = count;
    }

    public TacticalCandidateAdmissionState State { get; }

    public int Count { get; }
}

public sealed record TacticalCandidateRejectionSummary
{
    internal TacticalCandidateRejectionSummary(
        string reasonIdentity,
        int count,
        IEnumerable<string> exampleConsiderationKeys)
    {
        ReasonIdentity = TacticalCombatText.Code(
            reasonIdentity,
            nameof(reasonIdentity));
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        Count = count;
        ExampleConsiderationKeys =
        [
            .. exampleConsiderationKeys.Order(StringComparer.Ordinal)
        ];
        if (ExampleConsiderationKeys.IsEmpty
            || ExampleConsiderationKeys.Distinct(StringComparer.Ordinal).Count()
                != ExampleConsiderationKeys.Length)
        {
            throw new ArgumentException(
                "Rejection examples must be unique and non-empty.",
                nameof(exampleConsiderationKeys));
        }
    }

    public string ReasonIdentity { get; }

    public int Count { get; }

    public ImmutableArray<string> ExampleConsiderationKeys { get; }
}

public sealed class TacticalCandidateDiscoveryResult
{
    internal TacticalCandidateDiscoveryResult(
        string contextSemanticFingerprint,
        int learnedSkillCount,
        int supportedRoleCount,
        IEnumerable<TacticalCandidateDiscoveryEntry> considerations,
        TacticalCandidateDiscoveryLimits limits)
    {
        ContextSemanticFingerprint =
            TacticalCombatText.ValidateFingerprint(
                contextSemanticFingerprint,
                nameof(contextSemanticFingerprint));
        if (learnedSkillCount < 0 || supportedRoleCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(learnedSkillCount));
        }

        ArgumentNullException.ThrowIfNull(limits);
        LearnedSkillCount = learnedSkillCount;
        SupportedRoleCount = supportedRoleCount;
        Entries = TacticalCombatText.CopyUnique(
            considerations,
            item => item.StableKey,
            "candidate consideration",
            nameof(considerations));
        if (Entries.Length != checked(learnedSkillCount * 2)
            || Entries.GroupBy(item => item.SkillId).Any(group =>
                group.Count() != 2
                || !group.Select(item => item.Direction).Order()
                    .SequenceEqual(new[]
                    {
                        PracticeDirection.Reverse,
                        PracticeDirection.Direct
                    })))
        {
            throw new ArgumentException(
                "Discovery requires exactly one Direct and Reverse consideration per learned skill.",
                nameof(considerations));
        }
        AdmissionCounts =
        [
            .. Enum.GetValues<TacticalCandidateAdmissionState>()
                .Select(state => new TacticalCandidateCount(
                    state,
                    Entries.Count(item =>
                        item.AdmissionState == state)))
        ];
        ConsideredVerifiedRoleCount = Entries
            .Where(item => item.Role is not null)
            .Select(item => item.Role!.Identity.StableKey)
            .Distinct(StringComparer.Ordinal)
            .Count();
        AdmittedVerifiedRoleCount = Entries
            .Where(item => item.IsAdmitted)
            .Select(item => item.Role!.Identity.StableKey)
            .Distinct(StringComparer.Ordinal)
            .Count();
        UnsupportedCount = Entries.Count(item =>
            item.SupportState != TacticalCandidateSupportState.VerifiedRole);
        RejectionSummaries = CreateSummaries(Entries, limits);
        SemanticFingerprint = CreateFingerprint();
    }

    public int LearnedSkillCount { get; }

    public string ContextSemanticFingerprint { get; }

    public int SupportedRoleCount { get; }

    public int ConsideredVerifiedRoleCount { get; }

    public int AdmittedVerifiedRoleCount { get; }

    public int UnsupportedCount { get; }

    public ImmutableArray<TacticalCandidateDiscoveryEntry> Entries
    { get; }

    public ImmutableArray<TacticalCandidateCount> AdmissionCounts { get; }

    public ImmutableArray<TacticalCandidateRejectionSummary> RejectionSummaries
    { get; }

    public string SemanticFingerprint { get; }

    private static ImmutableArray<TacticalCandidateRejectionSummary>
        CreateSummaries(
            ImmutableArray<TacticalCandidateDiscoveryEntry> considerations,
            TacticalCandidateDiscoveryLimits limits) =>
        [
            .. considerations
                .SelectMany(consideration => consideration.Gates
                    .Where(gate => gate.State is
                        TacticalCandidateGateState.Failed
                        or TacticalCandidateGateState.Unknown
                        or TacticalCandidateGateState.Conflicting
                        or TacticalCandidateGateState.Unsupported)
                    .Select(gate => new
                    {
                        gate.ReasonIdentity,
                        consideration.StableKey
                    }))
                .GroupBy(item => item.ReasonIdentity, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new TacticalCandidateRejectionSummary(
                    group.Key,
                    group.Count(),
                    group.Select(item => item.StableKey)
                        .Distinct(StringComparer.Ordinal)
                        .Order(StringComparer.Ordinal)
                        .Take(limits.MaxExamplesPerReason)))
        ];

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("TACTICAL_CANDIDATE_DISCOVERY_V1\n")
            .Append(ContextSemanticFingerprint).Append('\n')
            .Append(LearnedSkillCount).Append('|')
            .Append(SupportedRoleCount).Append('\n');
        foreach (var consideration in Entries)
        {
            canonical.Append(consideration.SemanticKey).Append('\n');
        }

        return TacticalCombatText.Fingerprint(canonical.ToString());
    }
}
