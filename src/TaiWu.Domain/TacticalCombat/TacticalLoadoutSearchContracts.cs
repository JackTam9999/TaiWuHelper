using System.Collections.Immutable;
using System.Text;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.TacticalCombat;

public enum TacticalPruningRuleKind
{
    IrrelevantToTarget,
    DominatedInSameContext
}

public enum TacticalDominanceDimension
{
    RoleValue,
    Timing,
    Requirements,
    EffectiveCost,
    Conflicts,
    ExecutionRisk
}

public sealed record TacticalDominanceDimensionEvidence
{
    public TacticalDominanceDimensionEvidence(
        TacticalDominanceDimension dimension,
        TacticalEvidenceReference evidence)
    {
        Dimension = TacticalCombatText.Defined(dimension, nameof(dimension));
        Evidence = evidence ?? throw new ArgumentNullException(nameof(evidence));
    }

    public TacticalDominanceDimension Dimension { get; }

    public TacticalEvidenceReference Evidence { get; }
}

public sealed record TacticalIrrelevanceProof
{
    public TacticalIrrelevanceProof(
        TacticalCandidateIdentity candidate,
        string contextSemanticFingerprint,
        IEnumerable<TacticalEvidenceReference> evidence)
    {
        Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        ContextSemanticFingerprint = TacticalCombatText.ValidateFingerprint(
            contextSemanticFingerprint,
            nameof(contextSemanticFingerprint));
        Evidence = TacticalCombatText.CopyUnique(
            evidence,
            item => item.StableKey,
            "irrelevance-proof evidence",
            nameof(evidence));
        if (Evidence.IsEmpty)
        {
            throw new ArgumentException(
                "An irrelevance proof requires evidence that no selected target role or transition applies.",
                nameof(evidence));
        }
    }

    public TacticalCandidateIdentity Candidate { get; }

    public string ContextSemanticFingerprint { get; }

    public ImmutableArray<TacticalEvidenceReference> Evidence { get; }

    internal string StableKey => Candidate.StableKey;
}

public sealed record TacticalDominanceProof
{
    public TacticalDominanceProof(
        TacticalCandidateIdentity dominated,
        TacticalCandidateIdentity dominator,
        string contextSemanticFingerprint,
        bool isStrictlyBetter,
        IEnumerable<TacticalDominanceDimensionEvidence> dimensions)
    {
        Dominated = dominated ?? throw new ArgumentNullException(nameof(dominated));
        Dominator = dominator ?? throw new ArgumentNullException(nameof(dominator));
        if (Dominated == Dominator)
        {
            throw new ArgumentException(
                "A candidate cannot dominate itself.",
                nameof(dominator));
        }

        ContextSemanticFingerprint =
            TacticalCombatText.ValidateFingerprint(
                contextSemanticFingerprint,
                nameof(contextSemanticFingerprint));
        IsStrictlyBetter = isStrictlyBetter;
        ArgumentNullException.ThrowIfNull(dimensions);
        var values = dimensions.ToImmutableArray();
        var expected = Enum.GetValues<TacticalDominanceDimension>();
        if (values.Any(item => item is null)
            || values.Length != expected.Length
            || values.Select(item => item.Dimension).Distinct().Count()
                != expected.Length)
        {
            throw new ArgumentException(
                "A dominance proof requires exactly one evidence item for every comparison dimension.",
                nameof(dimensions));
        }

        Dimensions = [.. values.OrderBy(item => item.Dimension)];
    }

    public TacticalCandidateIdentity Dominated { get; }

    public TacticalCandidateIdentity Dominator { get; }

    public string ContextSemanticFingerprint { get; }

    public bool IsStrictlyBetter { get; }

    public ImmutableArray<TacticalDominanceDimensionEvidence> Dimensions
    { get; }

    internal string StableKey => Dominated.StableKey;

    internal IEnumerable<TacticalEvidenceReference> Evidence =>
        Dimensions.Select(item => item.Evidence);
}

public sealed record TacticalLoadoutSearchRequest
{
    public TacticalLoadoutSearchRequest(
        PlayerCombatSnapshot player,
        TacticalExecutionContext context,
        TacticalCombatRuleResolution ruleResolution,
        TacticalCandidateDiscoveryResult discovery,
        TacticalSearchBounds bounds,
        IEnumerable<TacticalIrrelevanceProof>? irrelevanceProofs = null,
        IEnumerable<TacticalDominanceProof>? dominanceProofs = null)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        RuleResolution = ruleResolution
            ?? throw new ArgumentNullException(nameof(ruleResolution));
        Discovery = discovery
            ?? throw new ArgumentNullException(nameof(discovery));
        Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
        IrrelevanceProofs = TacticalCombatText.CopyUnique(
            irrelevanceProofs ?? [],
            item => item.StableKey,
            "irrelevance proof",
            nameof(irrelevanceProofs));
        DominanceProofs = TacticalCombatText.CopyUnique(
            dominanceProofs ?? [],
            item => item.StableKey,
            "dominance proof",
            nameof(dominanceProofs));
    }

    public PlayerCombatSnapshot Player { get; }

    public TacticalExecutionContext Context { get; }

    public TacticalCombatRuleResolution RuleResolution { get; }

    public TacticalCandidateDiscoveryResult Discovery { get; }

    public TacticalSearchBounds Bounds { get; }

    public ImmutableArray<TacticalIrrelevanceProof> IrrelevanceProofs { get; }

    public ImmutableArray<TacticalDominanceProof> DominanceProofs { get; }
}

public sealed record TacticalPrunedCandidate
{
    internal TacticalPrunedCandidate(
        TacticalCandidateIdentity candidate,
        TacticalPruningRuleKind rule,
        string reasonIdentity,
        IEnumerable<TacticalEvidenceReference> evidence,
        TacticalCandidateIdentity? dominator = null)
    {
        Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        Rule = TacticalCombatText.Defined(rule, nameof(rule));
        ReasonIdentity = TacticalCombatText.Code(
            reasonIdentity,
            nameof(reasonIdentity));
        Evidence = TacticalCombatText.CopyUnique(
            evidence,
            item => item.StableKey,
            "pruning evidence",
            nameof(evidence));
        Dominator = dominator;
        if (Evidence.IsEmpty
            || (Rule == TacticalPruningRuleKind.DominatedInSameContext)
                != (Dominator is not null))
        {
            throw new ArgumentException(
                "A pruned candidate requires evidence and a dominator only for dominance.");
        }
    }

    public TacticalCandidateIdentity Candidate { get; }

    public TacticalPruningRuleKind Rule { get; }

    public string ReasonIdentity { get; }

    public ImmutableArray<TacticalEvidenceReference> Evidence { get; }

    public TacticalCandidateIdentity? Dominator { get; }

    internal string StableKey => Candidate.StableKey;
}

public sealed record TacticalFeasibleLoadoutResult
{
    internal TacticalFeasibleLoadoutResult(
        IEnumerable<TacticalCandidateIdentity> selectedCandidates,
        FeasibleCombatLoadout loadout,
        TacticalLoadoutPackage package)
    {
        ArgumentNullException.ThrowIfNull(selectedCandidates);
        Loadout = loadout ?? throw new ArgumentNullException(nameof(loadout));
        Package = package ?? throw new ArgumentNullException(nameof(package));
        SelectedCandidates = TacticalCombatText.CopyUnique(
            selectedCandidates,
            item => item.StableKey,
            "selected tactical candidate",
            nameof(selectedCandidates));
        StableKey = SelectedCandidates.IsEmpty
            ? "EMPTY"
            : string.Join('+', SelectedCandidates.Select(item => item.StableKey));
    }

    public ImmutableArray<TacticalCandidateIdentity> SelectedCandidates
    { get; }

    public FeasibleCombatLoadout Loadout { get; }

    public TacticalLoadoutPackage Package { get; }

    public string StableKey { get; }

    internal string SemanticKey => string.Join('|',
        StableKey,
        Package.SemanticKey);
}

public sealed class TacticalLoadoutSearchResult
{
    internal TacticalLoadoutSearchResult(
        string contextSemanticFingerprint,
        IEnumerable<TacticalCandidateConsideration> candidateDecisions,
        IEnumerable<TacticalPrunedCandidate> prunedCandidates,
        IEnumerable<TacticalFeasibleLoadoutResult> feasibleResults,
        TacticalSearchCoverage coverage)
    {
        ContextSemanticFingerprint =
            TacticalCombatText.ValidateFingerprint(
                contextSemanticFingerprint,
                nameof(contextSemanticFingerprint));
        CandidateDecisions = TacticalCombatText.CopyUnique(
            candidateDecisions,
            item => item.StableKey,
            "search candidate decision",
            nameof(candidateDecisions));
        PrunedCandidates = TacticalCombatText.CopyUnique(
            prunedCandidates,
            item => item.StableKey,
            "pruned candidate",
            nameof(prunedCandidates));
        FeasibleResults = TacticalCombatText.CopyUnique(
            feasibleResults,
            item => item.StableKey,
            "feasible tactical loadout",
            nameof(feasibleResults));
        Coverage = coverage ?? throw new ArgumentNullException(nameof(coverage));
        if (Coverage.CandidateUniverseCount != CandidateDecisions.Length
            || Coverage.RoleSupportedCount
                != CandidateDecisions.Count(item => !item.Roles.IsEmpty)
            || Coverage.AdmittedCount
                != DecisionCount(TacticalCandidateDecision.Admitted)
            || Coverage.RejectedCount
                != DecisionCount(TacticalCandidateDecision.Rejected)
            || Coverage.UnsupportedCount
                != DecisionCount(TacticalCandidateDecision.Unsupported)
            || Coverage.IrrelevantCount
                != DecisionCount(TacticalCandidateDecision.Irrelevant)
            || Coverage.DominatedCount
                != DecisionCount(TacticalCandidateDecision.Dominated)
            || Coverage.RetainedResultCount != FeasibleResults.Length
            || PrunedCandidates.Length
                != Coverage.IrrelevantCount + Coverage.DominatedCount)
        {
            throw new ArgumentException(
                "Search output collections must match coverage accounting.");
        }

        foreach (var pruned in PrunedCandidates)
        {
            var decision = CandidateDecisions.Single(item =>
                item.Identity == pruned.Candidate);
            var expected = pruned.Rule
                == TacticalPruningRuleKind.IrrelevantToTarget
                    ? TacticalCandidateDecision.Irrelevant
                    : TacticalCandidateDecision.Dominated;
            if (decision.Decision != expected
                || decision.DominatedBy != pruned.Dominator)
            {
                throw new ArgumentException(
                    "Each pruned candidate must have one matching terminal decision.");
            }
        }

        SemanticFingerprint = CreateFingerprint();

        int DecisionCount(TacticalCandidateDecision decision) =>
            CandidateDecisions.Count(item => item.Decision == decision);
    }

    public ImmutableArray<TacticalCandidateConsideration> CandidateDecisions
    { get; }

    public string ContextSemanticFingerprint { get; }

    public ImmutableArray<TacticalPrunedCandidate> PrunedCandidates { get; }

    public ImmutableArray<TacticalFeasibleLoadoutResult> FeasibleResults
    { get; }

    public TacticalSearchCoverage Coverage { get; }

    public bool IsComplete => Coverage.IsComplete;

    public bool IsOptimal => false;

    public string SemanticFingerprint { get; }

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("TACTICAL_LOADOUT_SEARCH_V1\n")
            .Append(ContextSemanticFingerprint).Append('\n')
            .Append(Coverage.Fingerprint).Append('\n');
        foreach (var decision in CandidateDecisions)
        {
            canonical.Append("CANDIDATE|")
                .Append(decision.ContentKey).Append('\n');
        }

        foreach (var result in FeasibleResults)
        {
            canonical.Append("RESULT|").Append(result.SemanticKey).Append('\n');
        }

        return TacticalCombatText.Fingerprint(canonical.ToString());
    }
}
