using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;

namespace TaiWu.Application.CombatRecommendations;

internal static class TargetObservationRecommendationImpactAnalyzer
{
    private const string CurrentScreenPrecedenceRule =
        "NEWER_CURRENT_SCREEN_FIELD_PRECEDENCE";

    public static TargetObservationRecommendationImpact Compare(
        CombatLoadoutRecommendation baseline,
        CombatLoadoutRecommendation observed,
        TargetLoadoutObservationMergeResult merge)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(observed);
        ArgumentNullException.ThrowIfNull(merge);

        return new TargetObservationRecommendationImpact(
            [.. CompareThreats(baseline, observed)],
            [.. CompareRecommendations(baseline, observed)],
            [.. CompareUnsupportedEvidence(baseline, observed)],
            merge.Observation.Coverage.Kind
                == TargetLoadoutCoverageKind.PartialLoadout,
            [.. CollectConflicts(merge)]);
    }

    private static TargetThreatImpact[] CompareThreats(
        CombatLoadoutRecommendation baseline,
        CombatLoadoutRecommendation observed)
    {
        var before = baseline.ThreatAnalysis.Threats.ToDictionary(
            value => value.Threat.Code,
            StringComparer.Ordinal);
        var after = observed.ThreatAnalysis.Threats.ToDictionary(
            value => value.Threat.Code,
            StringComparer.Ordinal);

        return before.Keys
            .Union(after.Keys, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(code =>
            {
                before.TryGetValue(code, out var oldValue);
                after.TryGetValue(code, out var newValue);
                var selected = newValue ?? oldValue!;
                var kind = ClassifyThreat(oldValue, newValue);
                return new TargetThreatImpact(
                    code,
                    selected.Threat.Title,
                    kind,
                    selected.Threat.Severity,
                    [.. selected.Sources
                        .Select(source => source.Kind)
                        .Distinct()
                        .Order()],
                    [.. EvidenceReferences(selected)]);
            })
            .ToArray();
    }

    private static TargetThreatImpactKind ClassifyThreat(
        AnalyzedTargetThreat? before,
        AnalyzedTargetThreat? after)
    {
        if (before is null)
        {
            return TargetThreatImpactKind.Added;
        }

        if (after is null)
        {
            return TargetThreatImpactKind.Removed;
        }

        if (before.Sources.Any(source =>
                source.Scope is TargetThreatSourceScope.Equipped
                    or TargetThreatSourceScope.BattleVisibleActiveEffect)
            && after.Sources.All(source =>
                source.Scope == TargetThreatSourceScope.LearnedUnequipped))
        {
            return TargetThreatImpactKind.Demoted;
        }

        if (after.Sources.Any(source =>
                source.Kind is TargetThreatSourceKind.ObservedEquipped
                    or TargetThreatSourceKind.ObservedActiveEffect)
            && before.Sources.All(source =>
                source.Kind is not TargetThreatSourceKind.ObservedEquipped
                    and not TargetThreatSourceKind.ObservedActiveEffect))
        {
            return TargetThreatImpactKind.Confirmed;
        }

        return TargetThreatImpactKind.Unchanged;
    }

    private static string[] EvidenceReferences(
        AnalyzedTargetThreat finding) => finding.Threat.Evidence
        .Select(value => value.Reference)
        .Concat(finding.Sources.Select(value => value.EvidenceReference))
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToArray();

    private static TargetRecommendationImpact[] CompareRecommendations(
        CombatLoadoutRecommendation baseline,
        CombatLoadoutRecommendation observed)
    {
        var baselineFeasible = FeasibleOptionKeys(baseline);
        var observedFeasible = FeasibleOptionKeys(observed);
        List<TargetRecommendationImpact> changes = [];
        foreach (var policy in Enum.GetValues<RecommendationPolicy>())
        {
            var before = SelectedOptions(baseline, policy);
            var after = SelectedOptions(observed, policy);
            foreach (var key in after.Keys
                         .Except(before.Keys)
                         .OrderBy(value => value.SkillId)
                         .ThenBy(value => value.Direction))
            {
                var option = after[key];
                changes.Add(CreateRecommendationImpact(
                    observed,
                    policy,
                    TargetRecommendationImpactKind.Added,
                    baselineFeasible.Contains(key)
                        ? TargetRecommendationChangeCause.Scoring
                        : TargetRecommendationChangeCause.Feasibility,
                    option));
            }

            foreach (var key in before.Keys
                         .Except(after.Keys)
                         .OrderBy(value => value.SkillId)
                         .ThenBy(value => value.Direction))
            {
                var option = before[key];
                changes.Add(CreateRecommendationImpact(
                    baseline,
                    policy,
                    TargetRecommendationImpactKind.Removed,
                    observedFeasible.Contains(key)
                        ? TargetRecommendationChangeCause.Scoring
                        : TargetRecommendationChangeCause.Feasibility,
                    option));
            }
        }

        return [.. changes
            .OrderBy(value => value.Policy)
            .ThenBy(value => value.Cause)
            .ThenBy(value => value.Kind)
            .ThenBy(value => value.SkillId)
            .ThenBy(value => value.RequiredDirection)];
    }

    private static Dictionary<OptionKey, CombatLoadoutOption> SelectedOptions(
        CombatLoadoutRecommendation recommendation,
        RecommendationPolicy policy)
    {
        var plan = recommendation.Styles
            .Single(style => style.Policy == policy)
            .ManualPlan.Plan;
        return plan is null
            ? []
            : plan.SelectedRecommendation.Candidate.SelectedOptions
                .ToDictionary(OptionKey.From);
    }

    private static HashSet<OptionKey> FeasibleOptionKeys(
        CombatLoadoutRecommendation recommendation) =>
        recommendation.Generation.Candidates
            .SelectMany(candidate => candidate.SelectedOptions)
            .Select(OptionKey.From)
            .ToHashSet();

    private static TargetRecommendationImpact CreateRecommendationImpact(
        CombatLoadoutRecommendation recommendation,
        RecommendationPolicy policy,
        TargetRecommendationImpactKind kind,
        TargetRecommendationChangeCause cause,
        CombatLoadoutOption option)
    {
        var skill = recommendation.Snapshot.Player.LearnedSkills.Single(value =>
            value.SkillId == option.Candidate.SkillId);
        var linkedThreats = recommendation.RecommendationThreats
            .Where(value => option.ThreatCodes.Contains(value.Code))
            .ToArray();
        var threatEvidence = linkedThreats.SelectMany(value =>
            value.Evidence.Select(evidence => evidence.Reference));
        return new TargetRecommendationImpact(
            policy,
            kind,
            cause,
            skill.SkillId,
            skill.Category,
            option.Candidate.RequiredDirection,
            [.. option.ThreatCodes.Order(StringComparer.Ordinal)],
            [.. linkedThreats
                .Select(value => value.Title)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)],
            [.. threatEvidence
                .Append(option.EvidenceReference)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)]);
    }

    private static TargetUnresolvedEvidenceImpact[] CompareUnsupportedEvidence(
        CombatLoadoutRecommendation baseline,
        CombatLoadoutRecommendation observed)
    {
        var before = baseline.ThreatAnalysis.Warnings
            .Select(WarningKey.From)
            .ToHashSet();
        return [.. observed.ThreatAnalysis.Warnings
            .Select(warning => new TargetUnresolvedEvidenceImpact(
                warning.Code,
                before.Contains(WarningKey.From(warning)),
                warning.Mechanic.EvidenceReference,
                warning.Mechanic.SourceSkillId,
                warning.Mechanic.RawEffectId))
            .OrderBy(value => value.Code, StringComparer.Ordinal)
            .ThenBy(value => value.SkillId)
            .ThenBy(value => value.RawEffectId)
            .ThenBy(value => value.EvidenceReference, StringComparer.Ordinal)];
    }

    private static TargetObservationConflictImpact[] CollectConflicts(
        TargetLoadoutObservationMergeResult merge)
    {
        List<TargetObservationConflictImpact> conflicts = [];
        AddConflict(merge.LoadoutEvidence, conflicts);
        foreach (var direction in merge.DirectionEvidence)
        {
            AddConflict(direction.Evidence, conflicts);
        }

        return [.. conflicts.OrderBy(value => value.Field, StringComparer.Ordinal)];
    }

    private static void AddConflict<T>(
        SnapshotEvidenceField<T> evidence,
        List<TargetObservationConflictImpact> conflicts)
    {
        if (evidence.Status != SnapshotEvidenceStatus.Conflicting)
        {
            return;
        }

        var sources = evidence.Observations
            .Select(value => value.Source)
            .ToArray();
        conflicts.Add(new TargetObservationConflictImpact(
            sources[0].FieldPath,
            evidence.ReasonCode!,
            CurrentScreenPrecedenceRule,
            [.. sources.Select(source => new TargetObservationConflictSource(
                source.Source,
                source.CapturedAtUtc,
                source.EvidenceReference))]));
    }

    private readonly record struct OptionKey(
        int SkillId,
        PracticeDirection? Direction)
    {
        public static OptionKey From(CombatLoadoutOption option) => new(
            option.Candidate.SkillId,
            option.Candidate.RequiredDirection);
    }

    private readonly record struct WarningKey(
        string Code,
        int? SkillId,
        int? RawEffectId)
    {
        public static WarningKey From(TargetThreatWarning warning) => new(
            warning.Code,
            warning.Mechanic.SourceSkillId,
            warning.Mechanic.RawEffectId);
    }
}
