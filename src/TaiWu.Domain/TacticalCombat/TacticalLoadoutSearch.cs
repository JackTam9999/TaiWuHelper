using System.Collections.Immutable;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.TacticalCombat;

public static class TacticalLoadoutSearch
{
    private const string CandidateCache = "CANDIDATE_PROJECTION_CACHE";
    private const string FeasibilityCache = "FEASIBILITY_CACHE";

    public static TacticalLoadoutSearchResult Search(
        TacticalLoadoutSearchRequest request,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var clock = timeProvider ?? TimeProvider.System;
        ValidateCoherence(request);
        var start = clock.GetTimestamp();
        var pruning = Prune(request);
        var options = request.Discovery.Entries
            .Where(item => item.AdmissionState
                == TacticalCandidateAdmissionState.Admitted)
            .Where(item => !pruning.ByCandidate.ContainsKey(item.StableKey))
            .OrderBy(OptionOrderKey, StringComparer.Ordinal)
            .ToArray();
        var searchedOptions = options
            .Take(request.Bounds.MaximumOptions)
            .ToArray();
        var firstTerminator = options.Length > searchedOptions.Length
            ? TacticalSearchTerminator.OptionLimit
            : TacticalSearchTerminator.None;
        var fixedRetentionIds = request.Discovery.Entries
            .Where(item => item.IsCurrentlyEquipped
                && item.AdmissionState
                    == TacticalCandidateAdmissionState.RetainedOnly)
            .Select(item => item.SkillId)
            .Distinct()
            .Order()
            .ToImmutableArray();
        var state = new SearchState(
            request,
            clock,
            start,
            firstTerminator,
            fixedRetentionIds);

        Explore(searchedOptions, index: 0, selected: [], state,
            cancellationToken);
        var elapsed = clock.GetElapsedTime(start);
        var decisions = Decisions(request, pruning);
        var coverage = new TacticalSearchCoverage(
            request.Bounds,
            request.Discovery.Entries.Length,
            request.Discovery.Entries.Count(item => item.Role is not null),
            decisions.Count(item =>
                item.Decision == TacticalCandidateDecision.Admitted),
            decisions.Count(item =>
                item.Decision == TacticalCandidateDecision.Rejected),
            decisions.Count(item =>
                item.Decision == TacticalCandidateDecision.Unsupported),
            decisions.Count(item =>
                item.Decision == TacticalCandidateDecision.Irrelevant),
            decisions.Count(item =>
                item.Decision == TacticalCandidateDecision.Dominated),
            searchedOptions.Length,
            state.ExploredCount,
            state.FeasibleCount,
            state.Results.Count,
            state.FirstTerminator,
            elapsed,
            [
                new TacticalCacheReuseDiagnostic(
                    CandidateCache,
                    state.CandidateCacheHits,
                    state.CandidateCacheMisses),
                new TacticalCacheReuseDiagnostic(
                    FeasibilityCache,
                    state.FeasibilityCacheHits,
                    state.FeasibilityCacheMisses)
            ]);
        return new TacticalLoadoutSearchResult(
            request.Context.SemanticFingerprint,
            decisions,
            pruning.Values,
            state.Results,
            coverage);
    }

    private static void Explore(
        TacticalCandidateDiscoveryEntry[] options,
        int index,
        List<TacticalCandidateDiscoveryEntry> selected,
        SearchState state,
        CancellationToken cancellationToken)
    {
        if (state.Stop)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            state.Terminate(TacticalSearchTerminator.Cancelled);
            return;
        }

        if (index == options.Length)
        {
            state.ExploreCombination(selected);
            return;
        }

        var option = options[index];
        if (selected.All(item => item.SkillId != option.SkillId))
        {
            selected.Add(option);
            Explore(options, index + 1, selected, state, cancellationToken);
            selected.RemoveAt(selected.Count - 1);
        }

        if (state.Stop)
        {
            return;
        }

        Explore(options, index + 1, selected, state, cancellationToken);
    }

    private static string OptionOrderKey(
        TacticalCandidateDiscoveryEntry entry) => string.Join('|',
        string.Join("||", entry.Role!.TargetGoalCodes),
        TacticalCombatText.EnumKey(entry.Role.Purpose),
        TacticalCombatText.EnumKey(entry.Role.Timing),
        entry.EffectiveCost.Value.ToString(
            System.Globalization.CultureInfo.InvariantCulture),
        entry.StableKey);

    private static void ValidateCoherence(TacticalLoadoutSearchRequest request)
    {
        if (!string.Equals(
                request.Context.RuleSetFingerprint,
                request.RuleResolution.RuleSetFingerprint,
                StringComparison.Ordinal)
            || request.Context.RuleResolutionStatus
                != request.RuleResolution.Status
            || !string.Equals(
                request.Context.SemanticFingerprint,
                request.Discovery.ContextSemanticFingerprint,
                StringComparison.Ordinal)
            || request.Discovery.LearnedSkillCount
                != request.Player.LearnedSkills.Length)
        {
            throw new ArgumentException(
                "Search inputs must come from one tactical projection.",
                nameof(request));
        }

        var learnedIds = request.Player.LearnedSkills
            .Select(item => item.SkillId)
            .Order()
            .ToArray();
        if (!learnedIds.SequenceEqual(
            request.Discovery.Entries.Select(item => item.SkillId)
                .Distinct().Order()))
        {
            throw new ArgumentException(
                "Candidate discovery must cover the supplied player atlas.",
                nameof(request));
        }
    }

    private static PruningResult Prune(TacticalLoadoutSearchRequest request)
    {
        var admitted = request.Discovery.Entries
            .Where(item => item.AdmissionState
                == TacticalCandidateAdmissionState.Admitted)
            .ToDictionary(item => item.StableKey, StringComparer.Ordinal);
        var pruned = request.Discovery.Entries
            .Where(item => item.Consideration.Decision
                == TacticalCandidateDecision.Irrelevant)
            .ToDictionary(
                item => item.StableKey,
                item => new TacticalPrunedCandidate(
                    item.Consideration.Identity,
                    TacticalPruningRuleKind.IrrelevantToTarget,
                    item.Consideration.ReasonIdentity,
                    item.Consideration.Evidence),
                StringComparer.Ordinal);
        foreach (var proof in request.IrrelevanceProofs)
        {
            ValidateProofContext(
                proof.ContextSemanticFingerprint,
                proof.Evidence,
                request);
            if (!admitted.ContainsKey(proof.Candidate.StableKey))
            {
                throw new ArgumentException(
                    "Irrelevance can prune only a hard-gate-admitted candidate.",
                    nameof(request));
            }

            pruned.Add(
                proof.Candidate.StableKey,
                new TacticalPrunedCandidate(
                    proof.Candidate,
                    TacticalPruningRuleKind.IrrelevantToTarget,
                    "NO_APPLICABLE_TARGET_ROLE_OR_TRANSITION",
                    proof.Evidence));
        }

        var dominanceKeys = request.DominanceProofs
            .Select(item => item.Dominated.StableKey)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var proof in request.DominanceProofs)
        {
            ValidateProofContext(
                proof.ContextSemanticFingerprint,
                proof.Evidence,
                request);
            if (!admitted.ContainsKey(proof.Dominated.StableKey)
                || !admitted.ContainsKey(proof.Dominator.StableKey))
            {
                throw new ArgumentException(
                    "Dominance can compare only hard-gate-admitted candidates.",
                    nameof(request));
            }

            if (pruned.ContainsKey(proof.Dominated.StableKey)
                || pruned.ContainsKey(proof.Dominator.StableKey)
                || dominanceKeys.Contains(proof.Dominator.StableKey))
            {
                throw new ArgumentException(
                    "A dominance proof requires one unpruned root dominator and one pruning rule.",
                    nameof(request));
            }

            if (!proof.IsStrictlyBetter
                && string.CompareOrdinal(
                    proof.Dominator.StableKey,
                    proof.Dominated.StableKey) >= 0)
            {
                throw new ArgumentException(
                    "Equivalent dominance ties must retain the smaller canonical identity.",
                    nameof(request));
            }

            pruned.Add(
                proof.Dominated.StableKey,
                new TacticalPrunedCandidate(
                    proof.Dominated,
                    TacticalPruningRuleKind.DominatedInSameContext,
                    proof.IsStrictlyBetter
                        ? "EXPLICIT_SAME_CONTEXT_DOMINANCE"
                        : "EXPLICIT_SAME_CONTEXT_TIE_BREAK",
                    proof.Evidence,
                    proof.Dominator));
        }

        return new PruningResult(pruned);
    }

    private static void ValidateProofContext(
        string contextFingerprint,
        IEnumerable<TacticalEvidenceReference> evidence,
        TacticalLoadoutSearchRequest request)
    {
        if (!string.Equals(
                contextFingerprint,
                request.Context.SemanticFingerprint,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A pruning proof must use this search context.",
                nameof(request));
        }

        if (evidence.Any(item => !string.Equals(
                    item.GameDataVersion,
                    request.RuleResolution.GameDataVersion,
                    StringComparison.Ordinal)
                || !string.Equals(
                    item.RuleVersion,
                    VerifiedTacticalCombatRuleSets.RuleVersion,
                    StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "Pruning evidence must match the search versions.",
                nameof(request));
        }
    }

    private static ImmutableArray<TacticalCandidateConsideration> Decisions(
        TacticalLoadoutSearchRequest request,
        PruningResult pruning) =>
    [
        .. request.Discovery.Entries.Select(entry =>
        {
            if (!pruning.ByCandidate.TryGetValue(
                    entry.StableKey,
                    out var pruned))
            {
                return entry.Consideration;
            }

            return new TacticalCandidateConsideration(
                entry.Consideration.Identity,
                pruned.Rule == TacticalPruningRuleKind.IrrelevantToTarget
                    ? TacticalCandidateDecision.Irrelevant
                    : TacticalCandidateDecision.Dominated,
                entry.Consideration.Roles,
                entry.Consideration.Requirements,
                pruned.ReasonIdentity,
                pruned.Evidence,
                pruned.Dominator);
        })
    ];

    private sealed class SearchState
    {
        private readonly TacticalLoadoutSearchRequest _request;
        private readonly TimeProvider _clock;
        private readonly long _start;
        private readonly ImmutableArray<int> _fixedRetentionIds;
        private readonly Dictionary<string, CandidateProjection>
            _candidateCache = new(StringComparer.Ordinal);
        private readonly Dictionary<string, CombatLoadoutFeasibilityResult>
            _feasibilityCache = new(StringComparer.Ordinal);

        internal SearchState(
            TacticalLoadoutSearchRequest request,
            TimeProvider clock,
            long start,
            TacticalSearchTerminator firstTerminator,
            ImmutableArray<int> fixedRetentionIds)
        {
            _request = request;
            _clock = clock;
            _start = start;
            _fixedRetentionIds = fixedRetentionIds;
            FirstTerminator = firstTerminator;
        }

        internal int ExploredCount { get; private set; }

        internal int FeasibleCount { get; private set; }

        internal int CandidateCacheHits { get; private set; }

        internal int CandidateCacheMisses { get; private set; }

        internal int FeasibilityCacheHits { get; private set; }

        internal int FeasibilityCacheMisses { get; private set; }

        internal TacticalSearchTerminator FirstTerminator { get; private set; }

        internal bool Stop { get; private set; }

        internal List<TacticalFeasibleLoadoutResult> Results { get; } = [];

        internal void ExploreCombination(
            IReadOnlyList<TacticalCandidateDiscoveryEntry> selected)
        {
            if (_clock.GetElapsedTime(_start) >= _request.Bounds.MaximumElapsed)
            {
                Terminate(TacticalSearchTerminator.TimeLimit);
                return;
            }

            if (ExploredCount
                == _request.Bounds.MaximumExploredCombinations)
            {
                Terminate(TacticalSearchTerminator.ExplorationLimit);
                return;
            }

            ExploredCount++;
            var key = selected.Count == 0
                ? "EMPTY"
                : string.Join('+', selected.Select(item => item.StableKey)
                    .Order(StringComparer.Ordinal));
            if (!_feasibilityCache.TryGetValue(key, out var validation))
            {
                FeasibilityCacheMisses++;
                validation = Validate(selected);
                _feasibilityCache.Add(key, validation);
            }
            else
            {
                FeasibilityCacheHits++;
            }

            if (!validation.IsFeasible)
            {
                return;
            }

            FeasibleCount++;
            if (Results.Count < _request.Bounds.MaximumResults)
            {
                Results.Add(new TacticalFeasibleLoadoutResult(
                    selected.Select(item => item.Consideration.Identity),
                    validation.FeasibleLoadout!));
                return;
            }

            Terminate(TacticalSearchTerminator.ResultLimit);
        }

        internal void Terminate(TacticalSearchTerminator terminator)
        {
            if (FirstTerminator == TacticalSearchTerminator.None)
            {
                FirstTerminator = terminator;
            }

            Stop = true;
        }

        private CombatLoadoutFeasibilityResult Validate(
            IReadOnlyList<TacticalCandidateDiscoveryEntry> selected)
        {
            var selectedIds = selected.Select(item => item.SkillId)
                .Concat(_fixedRetentionIds)
                .Distinct()
                .ToHashSet();
            var learned = _request.Player.LearnedSkills.ToDictionary(
                item => item.SkillId);
            var loadout = new CombatLoadoutSnapshot(
                Skills(SkillCategory.Neigong),
                Skills(SkillCategory.Attack),
                Skills(SkillCategory.Agility),
                Skills(SkillCategory.Defense),
                Skills(SkillCategory.Assistance));
            var projections = selected.Select(Project).ToArray();
            var candidates = projections.Select(item => item.Candidate)
                .Concat(_fixedRetentionIds.Select(id =>
                    new CombatSkillCandidate(id)))
                .ToArray();
            var requirements = projections.SelectMany(item => item.Requirements)
                .DistinctBy(RequirementKey, StringComparer.Ordinal)
                .ToArray();
            var facts = _request.Context.Proposed;
            var context = new CombatRequirementContext(
                facts.EquippedWeaponTypeIds.IsAvailable
                    ? facts.EquippedWeaponTypeIds.Value
                    : [],
                trickCounts: [],
                facts.Distance.IsAvailable
                    ? SnapshotValue<int>.Available(facts.Distance.Value)
                    : SnapshotValue<int>.Unavailable(
                        facts.Distance.ReasonIdentity),
                facts.Resources.IsAvailable ? facts.Resources.Value : [],
                facts.UnlockedWeaponTypeIds.IsAvailable
                    ? facts.UnlockedWeaponTypeIds.Value
                    : [],
                selectedIds,
                Active(facts.ActiveDefenseSkillId),
                Active(facts.ActiveAgilitySkillId));
            var generic = facts.UniversalSlotAllocation.IsAvailable
                ? facts.UniversalSlotAllocation.Value
                : _request.Player.GenericSlotAllocation;
            var proposal = new ProposedCombatLoadout(
                loadout,
                generic,
                candidates,
                requirements,
                context,
                facts.LegendaryCostAssignments.IsAvailable
                    ? facts.LegendaryCostAssignments.Value
                    : null);
            return CombatLoadoutFeasibilityValidator.Validate(
                _request.Player,
                proposal);

            IEnumerable<int> Skills(SkillCategory category) => selectedIds
                .Where(id => learned[id].Category == category)
                .Order();

            int? Active(TacticalContextFact<int> fact) =>
                fact.IsAvailable && selectedIds.Contains(fact.Value)
                    ? fact.Value
                    : null;
        }

        private CandidateProjection Project(
            TacticalCandidateDiscoveryEntry entry)
        {
            if (_candidateCache.TryGetValue(entry.StableKey, out var cached))
            {
                CandidateCacheHits++;
                return cached;
            }

            CandidateCacheMisses++;
            var skill = _request.Player.LearnedSkills.Single(item =>
                item.SkillId == entry.SkillId);
            var match = _request.RuleResolution.Roles.Single(item =>
                item.Rule.Identity == entry.Role!.Identity);
            var directionChange = skill.Direction.IsAvailable
                && skill.Direction.Value != entry.Direction
                && skill.BreakthroughDirections.IsAvailable
                && skill.BreakthroughDirections.Value.HasCompleted(
                    entry.Direction);
            var value = new CandidateProjection(
                new CombatSkillCandidate(
                    entry.SkillId,
                    requiredDirection: entry.Direction,
                    allowDirectionChange: directionChange,
                    allowBreakthrough: entry.RequiresBreakthrough),
                match.Rule.SharedCounter?.Requirements ?? []);
            _candidateCache.Add(entry.StableKey, value);
            return value;
        }

        private static string RequirementKey(CombatRequirement requirement) =>
            string.Join('|',
                requirement.GetType().Name,
                requirement.EvidenceReference);
    }

    private sealed record CandidateProjection(
        CombatSkillCandidate Candidate,
        ImmutableArray<CombatRequirement> Requirements);

    private sealed class PruningResult(
        Dictionary<string, TacticalPrunedCandidate> values)
    {
        internal IReadOnlyDictionary<string, TacticalPrunedCandidate>
            ByCandidate => values;

        internal IEnumerable<TacticalPrunedCandidate> Values => values.Values;
    }
}
