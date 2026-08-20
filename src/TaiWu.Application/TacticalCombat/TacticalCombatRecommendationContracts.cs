using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.LoadoutComparisons;
using TaiWu.Domain.TacticalCombat;

namespace TaiWu.Application.TacticalCombat;

public enum TacticalCombatRecommendationStatus
{
    Success,
    PartialEvidence,
    UnsupportedChain,
    NoCandidate,
    SearchTruncated,
    SourceFailure,
    EvidenceFailure,
    ContextFailure,
    RuleFailure,
    SearchFailure,
    ScoringFailure,
    PlanningFailure,
    UnexpectedFailure
}

public sealed record TacticalCombatRecommendationRequest
{
    public TacticalCombatRecommendationRequest(
        int? playerCharacterId,
        RecommendationPolicy policy,
        TacticalLoadoutSearchReadRequest searchRequest,
        IEnumerable<TacticalLayeringProof>? layeringProofs = null,
        IEnumerable<TacticalTriggerObservability>? triggerObservations = null,
        IEnumerable<TacticalFinishPathProof>? finishProofs = null)
    {
        if (playerCharacterId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(playerCharacterId));
        }

        if (!Enum.IsDefined(policy))
        {
            throw new ArgumentOutOfRangeException(nameof(policy));
        }

        PlayerCharacterId = playerCharacterId;
        Policy = policy;
        SearchRequest = searchRequest
            ?? throw new ArgumentNullException(nameof(searchRequest));
        LayeringProofs = CopyUnique(
            layeringProofs ?? [],
            item => string.Join(':',
                item.PrimaryCandidate.SkillId,
                item.PrimaryCandidate.Direction,
                item.LayeredCandidate.SkillId,
                item.LayeredCandidate.Direction,
                item.MarginalTransition.Code),
            nameof(layeringProofs));
        TriggerObservations = CopyUnique(
            triggerObservations ?? [],
            item => item.Transition.Code,
            nameof(triggerObservations));
        FinishProofs = CopyUnique(
            finishProofs ?? [],
            item => string.Join(':',
                item.ChannelCandidate.SkillId,
                item.ChannelCandidate.Direction,
                item.FinishCandidate.SkillId,
                item.FinishCandidate.Direction,
                item.FinishTransition.Code),
            nameof(finishProofs));
    }

    public int? PlayerCharacterId { get; }

    public int TargetCharacterId => SearchRequest.ContextRequest
        .SnapshotRequest.TargetCharacterId;

    public RecommendationPolicy Policy { get; }

    public TacticalLoadoutSearchReadRequest SearchRequest { get; }

    public ImmutableArray<TacticalLayeringProof> LayeringProofs { get; }

    public ImmutableArray<TacticalTriggerObservability> TriggerObservations
    { get; }

    public ImmutableArray<TacticalFinishPathProof> FinishProofs { get; }

    private static ImmutableArray<T> CopyUnique<T>(
        IEnumerable<T> source,
        Func<T, string> key,
        string parameterName)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        var values = source.ToImmutableArray();
        if (values.Any(item => item is null)
            || values.Select(key).Distinct(StringComparer.Ordinal).Count()
                != values.Length)
        {
            throw new ArgumentException(
                "Tactical recommendation proofs must be non-null and unique.",
                parameterName);
        }

        return [.. values.OrderBy(key, StringComparer.Ordinal)];
    }
}

public sealed record TacticalRecommendationWorkCounts
{
    internal TacticalRecommendationWorkCounts(
        int snapshotReads,
        int legacyRecommendationBuilds,
        int comparisonBuilds,
        int ruleResolutions,
        int contextProjections,
        int candidateDiscoveries,
        int searches,
        int scores,
        int planCompilations)
    {
        SnapshotReads = Count(snapshotReads, nameof(snapshotReads));
        LegacyRecommendationBuilds = Count(
            legacyRecommendationBuilds,
            nameof(legacyRecommendationBuilds));
        ComparisonBuilds = Count(comparisonBuilds, nameof(comparisonBuilds));
        RuleResolutions = Count(ruleResolutions, nameof(ruleResolutions));
        ContextProjections = Count(
            contextProjections,
            nameof(contextProjections));
        CandidateDiscoveries = Count(
            candidateDiscoveries,
            nameof(candidateDiscoveries));
        Searches = Count(searches, nameof(searches));
        Scores = Count(scores, nameof(scores));
        PlanCompilations = Count(
            planCompilations,
            nameof(planCompilations));
    }

    public int SnapshotReads { get; }

    public int LegacyRecommendationBuilds { get; }

    public int ComparisonBuilds { get; }

    public int RuleResolutions { get; }

    public int ContextProjections { get; }

    public int CandidateDiscoveries { get; }

    public int Searches { get; }

    public int Scores { get; }

    public int PlanCompilations { get; }

    private static int Count(int value, string parameterName) =>
        value is >= 0 and <= 1
            ? value
            : throw new ArgumentOutOfRangeException(parameterName);
}

public sealed record TacticalCombatRecommendationIdentity
{
    internal TacticalCombatRecommendationIdentity(
        string snapshotFingerprint,
        string observationFingerprint,
        string targetChainFingerprint,
        string ruleFingerprint,
        string? candidateFingerprint,
        string boundFingerprint,
        string policyFingerprint,
        string? selectedLoadoutFingerprint,
        string? planFingerprint)
    {
        SnapshotFingerprint = Fingerprint(snapshotFingerprint,
            nameof(snapshotFingerprint));
        ObservationFingerprint = Fingerprint(observationFingerprint,
            nameof(observationFingerprint));
        TargetChainFingerprint = Fingerprint(targetChainFingerprint,
            nameof(targetChainFingerprint));
        RuleFingerprint = Fingerprint(ruleFingerprint, nameof(ruleFingerprint));
        CandidateFingerprint = Optional(candidateFingerprint,
            nameof(candidateFingerprint));
        BoundFingerprint = Fingerprint(boundFingerprint,
            nameof(boundFingerprint));
        PolicyFingerprint = Fingerprint(policyFingerprint,
            nameof(policyFingerprint));
        SelectedLoadoutFingerprint = Optional(selectedLoadoutFingerprint,
            nameof(selectedLoadoutFingerprint));
        PlanFingerprint = Optional(planFingerprint, nameof(planFingerprint));
        SemanticFingerprint = Hash(string.Join('|',
            "TACTICAL_RECOMMENDATION_IDENTITY_V1",
            SnapshotFingerprint,
            ObservationFingerprint,
            TargetChainFingerprint,
            RuleFingerprint,
            CandidateFingerprint ?? "NONE",
            BoundFingerprint,
            PolicyFingerprint,
            SelectedLoadoutFingerprint ?? "NONE",
            PlanFingerprint ?? "NONE"));
    }

    public string SnapshotFingerprint { get; }

    public string ObservationFingerprint { get; }

    public string TargetChainFingerprint { get; }

    public string RuleFingerprint { get; }

    public string? CandidateFingerprint { get; }

    public string BoundFingerprint { get; }

    public string PolicyFingerprint { get; }

    public string? SelectedLoadoutFingerprint { get; }

    public string? PlanFingerprint { get; }

    public string SemanticFingerprint { get; }

    internal static string TargetChain(
        TacticalCombatRecommendationRequest request,
        TacticalCombatRuleResolution resolution,
        int playerCharacterId)
    {
        var canonical = new StringBuilder("TACTICAL_TARGET_CHAIN_V1\n")
            .Append(playerCharacterId).Append('|')
            .Append(request.TargetCharacterId).Append('\n')
            .AppendJoin('|', request.SearchRequest.ContextRequest.TargetGoalCodes)
            .Append('\n');
        foreach (var state in resolution.Transitions)
        {
            canonical.Append("TRANSITION|")
                .Append(state.Rule.Identity.Code).Append('|')
                .Append(state.Applicability).Append('|')
                .AppendJoin(',', state.UnmetEvidence.Select(item => item.Code))
                .Append('\n');
        }

        foreach (var state in resolution.Roles)
        {
            canonical.Append("ROLE|")
                .Append(state.Rule.Identity.Code).Append('|')
                .Append(state.Applicability).Append('|')
                .AppendJoin(',', state.UnmetEvidence.Select(item => item.Code))
                .Append('\n');
        }

        return Hash(canonical.ToString());
    }

    internal static string Bounds(TacticalSearchBounds bounds) => Hash(
        string.Join('|',
            "TACTICAL_SEARCH_BOUNDS_V1",
            bounds.MaximumOptions.ToString(CultureInfo.InvariantCulture),
            bounds.MaximumExploredCombinations.ToString(
                CultureInfo.InvariantCulture),
            bounds.MaximumElapsed.Ticks.ToString(CultureInfo.InvariantCulture),
            bounds.MaximumResults.ToString(CultureInfo.InvariantCulture)));

    internal static string Policy(RecommendationPolicy policy) => Hash(
        $"TACTICAL_POLICY_V1|{policy.ToString().ToUpperInvariant()}");

    internal static string Hash(string value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Fingerprint(string value, string parameterName) =>
        value.Length == 64 && value.All(Uri.IsHexDigit)
            ? value.ToUpperInvariant()
            : throw new ArgumentException(
                "A recommendation identity component must be a SHA-256 fingerprint.",
                parameterName);

    private static string? Optional(string? value, string parameterName) =>
        value is null ? null : Fingerprint(value, parameterName);
}

public sealed class TacticalCombatRecommendationResult
{
    internal TacticalCombatRecommendationResult(
        TacticalCombatRecommendationStatus status,
        string reasonIdentity,
        TacticalRecommendationWorkCounts workCounts,
        CombatLoadoutRecommendation? legacyRecommendation = null,
        LoadoutComparison? legacyComparison = null,
        TacticalExecutionContextReadResult? context = null,
        TacticalCombatRuleResolution? ruleResolution = null,
        TacticalCandidateDiscoveryResult? discovery = null,
        TacticalLoadoutSearchResult? search = null,
        TacticalCombatScoringResult? scoring = null,
        TacticalCompiledCombatPlan? compiledPlan = null,
        TacticalCombatRecommendationIdentity? identity = null)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        if (string.IsNullOrWhiteSpace(reasonIdentity))
        {
            throw new ArgumentException(
                "A tactical result requires a stable reason identity.",
                nameof(reasonIdentity));
        }

        Status = status;
        ReasonIdentity = reasonIdentity.Trim();
        WorkCounts = workCounts
            ?? throw new ArgumentNullException(nameof(workCounts));
        LegacyRecommendation = legacyRecommendation;
        LegacyComparison = legacyComparison;
        Context = context;
        RuleResolution = ruleResolution;
        Discovery = discovery;
        Search = search;
        Scoring = scoring;
        CompiledPlan = compiledPlan;
        Identity = identity;
        ValidateInvariant();
    }

    public TacticalCombatRecommendationStatus Status { get; }

    public string ReasonIdentity { get; }

    public TacticalRecommendationWorkCounts WorkCounts { get; }

    public CombatLoadoutRecommendation? LegacyRecommendation { get; }

    public LoadoutComparison? LegacyComparison { get; }

    public TacticalExecutionContextReadResult? Context { get; }

    public TacticalCombatRuleResolution? RuleResolution { get; }

    public TacticalCandidateDiscoveryResult? Discovery { get; }

    public TacticalLoadoutSearchResult? Search { get; }

    public TacticalCombatScoringResult? Scoring { get; }

    public TacticalCompiledCombatPlan? CompiledPlan { get; }

    public TacticalCombatRecommendationIdentity? Identity { get; }

    public bool HasTacticalPlan => CompiledPlan is not null;

    private void ValidateInvariant()
    {
        if ((LegacyRecommendation is null) != (LegacyComparison is null))
        {
            throw new ArgumentException(
                "Legacy recommendation and comparison must be retained together.");
        }

        if (CompiledPlan is not null
            && (Context is null
                || RuleResolution is null
                || Discovery is null
                || Search is null
                || Scoring is null
                || Identity is null))
        {
            throw new ArgumentException(
                "A compiled tactical plan requires every coherent pipeline artifact and identity.");
        }

        if (Status == TacticalCombatRecommendationStatus.Success
            && CompiledPlan is null)
        {
            throw new ArgumentException(
                "A successful tactical result requires a compiled plan.");
        }

        if (Status == TacticalCombatRecommendationStatus.SourceFailure
            && (Context is not null || Identity is not null))
        {
            throw new ArgumentException(
                "A source failure cannot expose a fabricated context or identity.");
        }
    }
}
