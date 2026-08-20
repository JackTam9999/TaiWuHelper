using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.TacticalCombat;

public enum TacticalScoreComponentKind
{
    CausalValue,
    LayeredProtection,
    TimingOpportunity,
    ExecutionReliability,
    RecoveryCost,
    FinishPath
}

public enum TacticalScoreComponentState
{
    Available,
    Unavailable
}

public enum TacticalScoreInputKind
{
    ApplicableTransition,
    CoveredTransition,
    CausalTriggerState,
    CausalResultingState,
    LayeringInteraction,
    TimingWindow,
    PreparationStep,
    TriggerObservability,
    ExecutionRequirement,
    ResourceRequirement,
    SelfLock,
    RecoveryRoute,
    FinishAttackChannel,
    FinishReliabilityPercent,
    FinishTargetResistance,
    FinishCondition,
    FinishWindow,
    UnusedCapacity,
    NoTacticalAction
}

public enum TacticalLayeringKind
{
    VerifiedInteraction,
    FailureFallback,
    DifferentTimingWindow,
    SeparateMitigation
}

public enum TacticalFinishEvidenceKind
{
    AttackChannelStrength,
    HitOrCastReliabilityPercent,
    TargetDefenseOrResistance,
    ApplicableCondition,
    FinishWindow
}

public sealed record TacticalScoreRawInput
{
    public TacticalScoreRawInput(
        TacticalScoreInputKind kind,
        string identity,
        TacticalEvidenceState state,
        TacticalFactValue? value,
        string reasonIdentity,
        IEnumerable<TacticalEvidenceReference> evidence)
    {
        Kind = TacticalCombatText.Defined(kind, nameof(kind));
        Identity = TacticalCombatText.Code(identity, nameof(identity));
        State = TacticalCombatText.Defined(state, nameof(state));
        Value = value;
        ReasonIdentity = TacticalCombatText.Code(
            reasonIdentity,
            nameof(reasonIdentity));
        Evidence = TacticalCombatText.CopyUnique(
            evidence,
            item => item.StableKey,
            "score-input evidence",
            nameof(evidence));
        if (Evidence.IsEmpty
            || (State == TacticalEvidenceState.Available) != (Value is not null))
        {
            throw new ArgumentException(
                "An available score input requires a value, while an unavailable input cannot invent one.");
        }
    }

    public TacticalScoreInputKind Kind { get; }

    public string Identity { get; }

    public TacticalEvidenceState State { get; }

    public TacticalFactValue? Value { get; }

    public string ReasonIdentity { get; }

    public ImmutableArray<TacticalEvidenceReference> Evidence { get; }

    internal string StableKey =>
        $"{TacticalCombatText.EnumKey(Kind)}:{Identity}";

    internal string ContentKey => string.Join('|',
        StableKey,
        TacticalCombatText.EnumKey(State),
        Value?.StableKey ?? "NONE",
        ReasonIdentity,
        string.Join("||", Evidence.Select(item => item.StableKey)));
}

public sealed record TacticalTriggerObservability
{
    public TacticalTriggerObservability(
        TacticalTransitionIdentity transition,
        TacticalEvidenceState state,
        string reasonIdentity,
        IEnumerable<TacticalEvidenceReference> evidence,
        string limitationIdentity)
    {
        Transition = transition
            ?? throw new ArgumentNullException(nameof(transition));
        State = TacticalCombatText.Defined(state, nameof(state));
        ReasonIdentity = TacticalCombatText.Code(
            reasonIdentity,
            nameof(reasonIdentity));
        Evidence = TacticalCombatText.CopyUnique(
            evidence,
            item => item.StableKey,
            "trigger-observability evidence",
            nameof(evidence));
        LimitationIdentity = TacticalCombatText.Code(
            limitationIdentity,
            nameof(limitationIdentity));
        if (Evidence.IsEmpty)
        {
            throw new ArgumentException(
                "Trigger observability requires evidence.",
                nameof(evidence));
        }
    }

    public TacticalTransitionIdentity Transition { get; }

    public TacticalEvidenceState State { get; }

    public string ReasonIdentity { get; }

    public ImmutableArray<TacticalEvidenceReference> Evidence { get; }

    public string LimitationIdentity { get; }

    internal string StableKey => Transition.StableKey;
}

public sealed record TacticalLayeringProof
{
    public TacticalLayeringProof(
        TacticalCandidateIdentity primaryCandidate,
        TacticalCandidateIdentity layeredCandidate,
        TacticalTransitionIdentity marginalTransition,
        TacticalLayeringKind kind,
        string contextSemanticFingerprint,
        IEnumerable<TacticalEvidenceReference> evidence,
        string limitationIdentity)
    {
        PrimaryCandidate = primaryCandidate
            ?? throw new ArgumentNullException(nameof(primaryCandidate));
        LayeredCandidate = layeredCandidate
            ?? throw new ArgumentNullException(nameof(layeredCandidate));
        if (PrimaryCandidate == LayeredCandidate)
        {
            throw new ArgumentException(
                "A layering proof requires two different candidates.",
                nameof(layeredCandidate));
        }

        MarginalTransition = marginalTransition
            ?? throw new ArgumentNullException(nameof(marginalTransition));
        Kind = TacticalCombatText.Defined(kind, nameof(kind));
        ContextSemanticFingerprint = TacticalCombatText.ValidateFingerprint(
            contextSemanticFingerprint,
            nameof(contextSemanticFingerprint));
        Evidence = TacticalCombatText.CopyUnique(
            evidence,
            item => item.StableKey,
            "layering evidence",
            nameof(evidence));
        LimitationIdentity = TacticalCombatText.Code(
            limitationIdentity,
            nameof(limitationIdentity));
        if (Evidence.IsEmpty)
        {
            throw new ArgumentException(
                "A layering proof requires evidence.",
                nameof(evidence));
        }
    }

    public TacticalCandidateIdentity PrimaryCandidate { get; }

    public TacticalCandidateIdentity LayeredCandidate { get; }

    public TacticalTransitionIdentity MarginalTransition { get; }

    public TacticalLayeringKind Kind { get; }

    public string ContextSemanticFingerprint { get; }

    public ImmutableArray<TacticalEvidenceReference> Evidence { get; }

    public string LimitationIdentity { get; }

    internal string StableKey => string.Join(':',
        PrimaryCandidate.StableKey,
        LayeredCandidate.StableKey,
        TacticalCombatText.EnumKey(Kind),
        MarginalTransition.StableKey);
}

public sealed record TacticalFinishEvidenceInput
{
    public TacticalFinishEvidenceInput(
        TacticalFinishEvidenceKind kind,
        string identity,
        TacticalFactValue value,
        IEnumerable<TacticalEvidenceReference> evidence)
    {
        Kind = TacticalCombatText.Defined(kind, nameof(kind));
        Identity = TacticalCombatText.Code(identity, nameof(identity));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Evidence = TacticalCombatText.CopyUnique(
            evidence,
            item => item.StableKey,
            "finish-input evidence",
            nameof(evidence));
        if (Evidence.IsEmpty)
        {
            throw new ArgumentException(
                "A finish input requires evidence.",
                nameof(evidence));
        }
    }

    public TacticalFinishEvidenceKind Kind { get; }

    public string Identity { get; }

    public TacticalFactValue Value { get; }

    public ImmutableArray<TacticalEvidenceReference> Evidence { get; }

    internal string StableKey => TacticalCombatText.EnumKey(Kind);
}

public sealed record TacticalFinishPathProof
{
    public TacticalFinishPathProof(
        TacticalCandidateIdentity channelCandidate,
        TacticalRoleIdentity channelRole,
        TacticalCandidateIdentity finishCandidate,
        TacticalRoleIdentity finishRole,
        TacticalTransitionIdentity finishTransition,
        string contextSemanticFingerprint,
        IEnumerable<TacticalFinishEvidenceInput> inputs,
        string limitationIdentity)
    {
        ChannelCandidate = channelCandidate
            ?? throw new ArgumentNullException(nameof(channelCandidate));
        ChannelRole = channelRole
            ?? throw new ArgumentNullException(nameof(channelRole));
        FinishCandidate = finishCandidate
            ?? throw new ArgumentNullException(nameof(finishCandidate));
        FinishRole = finishRole
            ?? throw new ArgumentNullException(nameof(finishRole));
        FinishTransition = finishTransition
            ?? throw new ArgumentNullException(nameof(finishTransition));
        ContextSemanticFingerprint = TacticalCombatText.ValidateFingerprint(
            contextSemanticFingerprint,
            nameof(contextSemanticFingerprint));
        Inputs = TacticalCombatText.CopyUnique(
            inputs,
            item => item.StableKey,
            "finish evidence input",
            nameof(inputs));
        LimitationIdentity = TacticalCombatText.Code(
            limitationIdentity,
            nameof(limitationIdentity));
        if (Inputs.Length != Enum.GetValues<TacticalFinishEvidenceKind>().Length)
        {
            throw new ArgumentException(
                "A finish proof requires attack, reliability, resistance, condition, and window evidence.",
                nameof(inputs));
        }

        ValidateInput(TacticalFinishEvidenceKind.AttackChannelStrength, 1, null);
        ValidateInput(
            TacticalFinishEvidenceKind.HitOrCastReliabilityPercent,
            0,
            100);
        ValidateInput(
            TacticalFinishEvidenceKind.TargetDefenseOrResistance,
            0,
            null);
        ValidateBoolean(TacticalFinishEvidenceKind.ApplicableCondition);
        ValidateBoolean(TacticalFinishEvidenceKind.FinishWindow);
    }

    public TacticalCandidateIdentity ChannelCandidate { get; }

    public TacticalRoleIdentity ChannelRole { get; }

    public TacticalCandidateIdentity FinishCandidate { get; }

    public TacticalRoleIdentity FinishRole { get; }

    public TacticalTransitionIdentity FinishTransition { get; }

    public string ContextSemanticFingerprint { get; }

    public ImmutableArray<TacticalFinishEvidenceInput> Inputs { get; }

    public string LimitationIdentity { get; }

    internal string StableKey => string.Join(':',
        ChannelCandidate.StableKey,
        FinishCandidate.StableKey,
        FinishTransition.StableKey);

    internal long Integer(TacticalFinishEvidenceKind kind) =>
        long.Parse(
            Inputs.Single(item => item.Kind == kind).Value.CanonicalValue,
            CultureInfo.InvariantCulture);

    private void ValidateInput(
        TacticalFinishEvidenceKind kind,
        long minimum,
        long? maximum)
    {
        var input = Inputs.Single(item => item.Kind == kind);
        if (input.Value.Kind != TacticalFactValueKind.Integer
            || !long.TryParse(
                input.Value.CanonicalValue,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var value)
            || value < minimum
            || maximum.HasValue && value > maximum.Value)
        {
            throw new ArgumentException(
                $"Finish input {kind} is outside its typed range.");
        }
    }

    private void ValidateBoolean(TacticalFinishEvidenceKind kind)
    {
        var input = Inputs.Single(item => item.Kind == kind);
        if (input.Value.Kind != TacticalFactValueKind.Boolean
            || !string.Equals(
                input.Value.CanonicalValue,
                "TRUE",
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Finish input {kind} must be explicitly true.");
        }
    }
}

public sealed record TacticalUnusedCapacityEntry
{
    internal TacticalUnusedCapacityEntry(
        SkillCategory category,
        int remaining,
        int capacity)
    {
        Category = TacticalCombatText.Defined(category, nameof(category));
        if (remaining < 0 || capacity < 0 || remaining > capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(remaining));
        }

        Remaining = remaining;
        Capacity = capacity;
    }

    public SkillCategory Category { get; }

    public int Remaining { get; }

    public int Capacity { get; }

    internal string StableKey => TacticalCombatText.EnumKey(Category);

    internal string ContentKey => $"{StableKey}:{Remaining}:{Capacity}";
}

public sealed class TacticalUnusedCapacityFact
{
    internal TacticalUnusedCapacityFact(
        IEnumerable<TacticalUnusedCapacityEntry> categories,
        IEnumerable<string> evidenceIdentities)
    {
        Categories = TacticalCombatText.CopyUnique(
            categories,
            item => item.StableKey,
            "unused-capacity category",
            nameof(categories));
        ArgumentNullException.ThrowIfNull(evidenceIdentities);
        EvidenceIdentities =
        [
            .. evidenceIdentities.Select(item => TacticalCombatText.Code(
                    item,
                    nameof(evidenceIdentities)))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
        ];
        if (Categories.Length != Enum.GetValues<SkillCategory>().Length
            || EvidenceIdentities.IsEmpty)
        {
            throw new ArgumentException(
                "Unused capacity requires every category and source evidence.");
        }
    }

    public ImmutableArray<TacticalUnusedCapacityEntry> Categories { get; }

    public ImmutableArray<string> EvidenceIdentities { get; }

    public bool HasDocumentedMarginalValue => false;

    internal string ContentKey => string.Join('|',
        "UNUSED_CAPACITY_NEUTRAL_V1",
        string.Join("||", Categories.Select(item => item.ContentKey)),
        string.Join("||", EvidenceIdentities));
}

public sealed class TacticalScoringPolicyWeights
{
    private TacticalScoringPolicyWeights(
        RecommendationPolicy policy,
        int causalValue,
        int layeredProtection,
        int timingOpportunity,
        int executionReliability,
        int recoveryCost,
        int finishPath)
    {
        Policy = policy;
        CausalValue = causalValue;
        LayeredProtection = layeredProtection;
        TimingOpportunity = timingOpportunity;
        ExecutionReliability = executionReliability;
        RecoveryCost = recoveryCost;
        FinishPath = finishPath;
        if (Enum.GetValues<TacticalScoreComponentKind>().Sum(Get) != 100)
        {
            throw new ArgumentException(
                "Tactical scoring policy weights must total 100.");
        }
    }

    public RecommendationPolicy Policy { get; }

    public int CausalValue { get; }

    public int LayeredProtection { get; }

    public int TimingOpportunity { get; }

    public int ExecutionReliability { get; }

    public int RecoveryCost { get; }

    public int FinishPath { get; }

    public string LimitationIdentity => Policy switch
    {
        RecommendationPolicy.Safe => "SAFE_IS_NOT_GUARANTEED_SURVIVAL",
        RecommendationPolicy.Balanced => "BALANCED_IS_NOT_OUTCOME_PREDICTION",
        RecommendationPolicy.Aggressive =>
            "AGGRESSIVE_IS_NOT_VICTORY_OR_DAMAGE_PREDICTION",
        _ => throw new ArgumentOutOfRangeException(nameof(Policy))
    };

    public int Get(TacticalScoreComponentKind kind) => kind switch
    {
        TacticalScoreComponentKind.CausalValue => CausalValue,
        TacticalScoreComponentKind.LayeredProtection => LayeredProtection,
        TacticalScoreComponentKind.TimingOpportunity => TimingOpportunity,
        TacticalScoreComponentKind.ExecutionReliability =>
            ExecutionReliability,
        TacticalScoreComponentKind.RecoveryCost => RecoveryCost,
        TacticalScoreComponentKind.FinishPath => FinishPath,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static TacticalScoringPolicyWeights For(
        RecommendationPolicy policy) => policy switch
        {
            RecommendationPolicy.Safe => new(
                policy,
                causalValue: 28,
                layeredProtection: 24,
                timingOpportunity: 10,
                executionReliability: 20,
                recoveryCost: 15,
                finishPath: 3),
            RecommendationPolicy.Balanced => new(
                policy,
                causalValue: 29,
                layeredProtection: 18,
                timingOpportunity: 16,
                executionReliability: 16,
                recoveryCost: 13,
                finishPath: 8),
            RecommendationPolicy.Aggressive => new(
                policy,
                causalValue: 28,
                layeredProtection: 10,
                timingOpportunity: 24,
                executionReliability: 12,
                recoveryCost: 8,
                finishPath: 18),
            _ => throw new ArgumentOutOfRangeException(nameof(policy))
        };
}

public sealed class TacticalScoreComponent
{
    internal TacticalScoreComponent(
        TacticalScoreComponentKind kind,
        TacticalScoreComponentState state,
        IEnumerable<TacticalScoreRawInput> rawInputs,
        string normalizationIdentity,
        decimal? normalizedValue,
        int baseWeight,
        decimal? appliedWeight,
        decimal? contribution,
        IEnumerable<TacticalEvidenceReference> evidence,
        IEnumerable<string> limitations)
    {
        Kind = TacticalCombatText.Defined(kind, nameof(kind));
        State = TacticalCombatText.Defined(state, nameof(state));
        RawInputs = TacticalCombatText.CopyUnique(
            rawInputs,
            item => item.StableKey,
            "score raw input",
            nameof(rawInputs));
        NormalizationIdentity = TacticalCombatText.Code(
            normalizationIdentity,
            nameof(normalizationIdentity));
        if (baseWeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseWeight));
        }

        BaseWeight = baseWeight;
        AppliedWeight = appliedWeight;
        NormalizedValue = normalizedValue;
        Contribution = contribution;
        Evidence = TacticalCombatText.CopyUnique(
            evidence,
            item => item.StableKey,
            "score-component evidence",
            nameof(evidence));
        ArgumentNullException.ThrowIfNull(limitations);
        Limitations =
        [
            .. limitations.Select(item => TacticalCombatText.Code(
                    item,
                    nameof(limitations)))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
        ];
        ValidateInvariant();
    }

    public TacticalScoreComponentKind Kind { get; }

    public TacticalScoreComponentState State { get; }

    public ImmutableArray<TacticalScoreRawInput> RawInputs { get; }

    public string NormalizationIdentity { get; }

    public decimal? NormalizedValue { get; }

    public int BaseWeight { get; }

    public decimal? AppliedWeight { get; }

    public decimal? Contribution { get; }

    public ImmutableArray<TacticalEvidenceReference> Evidence { get; }

    public ImmutableArray<string> Limitations { get; }

    public bool IsAvailable => State == TacticalScoreComponentState.Available;

    internal string StableKey => TacticalCombatText.EnumKey(Kind);

    internal string ContentKey => string.Join('|',
        StableKey,
        TacticalCombatText.EnumKey(State),
        NormalizationIdentity,
        NormalizedValue?.ToString(CultureInfo.InvariantCulture) ?? "NONE",
        BaseWeight.ToString(CultureInfo.InvariantCulture),
        AppliedWeight?.ToString(CultureInfo.InvariantCulture) ?? "NONE",
        Contribution?.ToString(CultureInfo.InvariantCulture) ?? "NONE",
        string.Join("||", RawInputs.Select(item => item.ContentKey)),
        string.Join("||", Evidence.Select(item => item.StableKey)),
        string.Join("||", Limitations));

    private void ValidateInvariant()
    {
        if (RawInputs.IsEmpty || Evidence.IsEmpty)
        {
            throw new ArgumentException(
                "Every score component requires raw inputs and evidence.");
        }

        if (IsAvailable)
        {
            if (!NormalizedValue.HasValue
                || NormalizedValue is < 0 or > 100
                || !AppliedWeight.HasValue
                || AppliedWeight is <= 0 or > 1
                || !Contribution.HasValue
                || Contribution is < 0 or > 100
                || Contribution.Value != decimal.Round(
                    NormalizedValue.Value * AppliedWeight.Value,
                    4,
                    MidpointRounding.AwayFromZero))
            {
                throw new ArgumentException(
                    "An available score component requires normalized value, applied weight, and contribution.");
            }
        }
        else if (NormalizedValue.HasValue
            || AppliedWeight.HasValue
            || Contribution.HasValue
            || Limitations.IsEmpty)
        {
            throw new ArgumentException(
                "An unavailable component has no numeric value and requires a limitation.");
        }
    }
}

public sealed class TacticalScoredLoadout
{
    internal TacticalScoredLoadout(
        TacticalFeasibleLoadoutResult candidate,
        RecommendationPolicy policy,
        IEnumerable<TacticalScoreComponent> components,
        TacticalUnusedCapacityFact unusedCapacity)
    {
        Candidate = candidate
            ?? throw new ArgumentNullException(nameof(candidate));
        Policy = TacticalCombatText.Defined(policy, nameof(policy));
        Components = TacticalCombatText.CopyUnique(
            components,
            item => item.StableKey,
            "score component",
            nameof(components));
        UnusedCapacity = unusedCapacity
            ?? throw new ArgumentNullException(nameof(unusedCapacity));
        if (Components.Length != Enum.GetValues<TacticalScoreComponentKind>().Length)
        {
            throw new ArgumentException(
                "A scored tactical loadout requires every score component.",
                nameof(components));
        }

        var weights = TacticalScoringPolicyWeights.For(Policy);
        var availableBaseWeight = Components.Where(item => item.IsAvailable)
            .Sum(item => item.BaseWeight);
        if (availableBaseWeight == 0
            || Components.Any(item => item.BaseWeight != weights.Get(item.Kind))
            || Components.Where(item => item.IsAvailable).Any(item =>
                item.AppliedWeight != (decimal)item.BaseWeight
                    / availableBaseWeight))
        {
            throw new ArgumentException(
                "Tactical score components must use the published policy weights normalized across available components.",
                nameof(components));
        }

        TotalScore = decimal.Round(
            Components.Where(item => item.IsAvailable)
                .Sum(item => item.Contribution!.Value),
            4,
            MidpointRounding.AwayFromZero);
    }

    public TacticalFeasibleLoadoutResult Candidate { get; }

    public RecommendationPolicy Policy { get; }

    public ImmutableArray<TacticalScoreComponent> Components { get; }

    public TacticalUnusedCapacityFact UnusedCapacity { get; }

    public decimal TotalScore { get; }

    public TacticalScoreComponent Get(TacticalScoreComponentKind kind) =>
        Components.Single(item => item.Kind == kind);

    internal string StableKey => Candidate.StableKey;

    internal string ContentKey => string.Join('|',
        StableKey,
        TacticalCombatText.EnumKey(Policy),
        TotalScore.ToString(CultureInfo.InvariantCulture),
        UnusedCapacity.ContentKey,
        string.Join("||", Components.Select(item => item.ContentKey)));
}

public sealed class TacticalCombatScoringResult
{
    internal TacticalCombatScoringResult(
        string searchSemanticFingerprint,
        TacticalScoringPolicyWeights weights,
        IEnumerable<TacticalScoredLoadout> rankedCandidates)
    {
        SearchSemanticFingerprint = TacticalCombatText.ValidateFingerprint(
            searchSemanticFingerprint,
            nameof(searchSemanticFingerprint));
        Weights = weights ?? throw new ArgumentNullException(nameof(weights));
        ArgumentNullException.ThrowIfNull(rankedCandidates);
        RankedCandidates = rankedCandidates.ToImmutableArray();
        if (RankedCandidates.Any(item => item is null)
            || RankedCandidates.Select(item => item.StableKey)
                .Distinct(StringComparer.Ordinal).Count()
                != RankedCandidates.Length
            || RankedCandidates.Any(item => item.Policy != Weights.Policy))
        {
            throw new ArgumentException(
                "Ranked tactical candidates must be unique and use one policy.",
                nameof(rankedCandidates));
        }

        SemanticFingerprint = CreateFingerprint();
    }

    public string SearchSemanticFingerprint { get; }

    public string ScoringVersion => TacticalCombatScorer.ScoringVersion;

    public TacticalScoringPolicyWeights Weights { get; }

    public string PolicyLimitationIdentity => Weights.LimitationIdentity;

    public ImmutableArray<TacticalScoredLoadout> RankedCandidates { get; }

    public string SemanticFingerprint { get; }

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("TACTICAL_SCORING_V1\n")
            .Append(SearchSemanticFingerprint).Append('\n')
            .Append(TacticalCombatText.EnumKey(Weights.Policy)).Append('\n');
        foreach (var kind in Enum.GetValues<TacticalScoreComponentKind>())
        {
            canonical.Append("WEIGHT|")
                .Append(TacticalCombatText.EnumKey(kind)).Append('|')
                .Append(Weights.Get(kind)).Append('\n');
        }

        foreach (var candidate in RankedCandidates)
        {
            canonical.Append("CANDIDATE|")
                .Append(candidate.ContentKey).Append('\n');
        }

        return TacticalCombatText.Fingerprint(canonical.ToString());
    }
}

public sealed record TacticalCombatScoringRequest
{
    public TacticalCombatScoringRequest(
        RecommendationPolicy policy,
        TacticalLoadoutSearchRequest searchRequest,
        TacticalLoadoutSearchResult searchResult,
        IEnumerable<TacticalLayeringProof>? layeringProofs = null,
        IEnumerable<TacticalTriggerObservability>? triggerObservations = null,
        IEnumerable<TacticalFinishPathProof>? finishProofs = null)
    {
        Policy = TacticalCombatText.Defined(policy, nameof(policy));
        SearchRequest = searchRequest
            ?? throw new ArgumentNullException(nameof(searchRequest));
        SearchResult = searchResult
            ?? throw new ArgumentNullException(nameof(searchResult));
        if (!string.Equals(
                SearchRequest.Context.SemanticFingerprint,
                SearchResult.ContextSemanticFingerprint,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Scoring requires the exact search request context.",
                nameof(searchResult));
        }

        LayeringProofs = TacticalCombatText.CopyUnique(
            layeringProofs ?? [],
            item => item.StableKey,
            "layering proof",
            nameof(layeringProofs));
        TriggerObservations = TacticalCombatText.CopyUnique(
            triggerObservations ?? [],
            item => item.StableKey,
            "trigger observation",
            nameof(triggerObservations));
        FinishProofs = TacticalCombatText.CopyUnique(
            finishProofs ?? [],
            item => item.StableKey,
            "finish proof",
            nameof(finishProofs));
        if (LayeringProofs.Length > 2048
            || TriggerObservations.Length > 1024
            || FinishProofs.Length > 1024)
        {
            throw new ArgumentException(
                "Tactical scoring proof collections exceed their request bounds.");
        }
    }

    public RecommendationPolicy Policy { get; }

    public TacticalLoadoutSearchRequest SearchRequest { get; }

    public TacticalLoadoutSearchResult SearchResult { get; }

    public ImmutableArray<TacticalLayeringProof> LayeringProofs { get; }

    public ImmutableArray<TacticalTriggerObservability> TriggerObservations
    { get; }

    public ImmutableArray<TacticalFinishPathProof> FinishProofs { get; }
}
