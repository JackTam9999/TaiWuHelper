using System.Security.Cryptography;
using System.Text;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.LoadoutComparisons;

namespace TaiWu.Application.LoadoutComparisons;

public static class CombatLoadoutComparisonBuilder
{
    public static LoadoutComparison Build(
        CombatLoadoutRecommendation recommendation)
    {
        ArgumentNullException.ThrowIfNull(recommendation);
        ValidateStyles(recommendation);

        var snapshotReference = BuildSnapshotReference(recommendation);
        List<LoadoutComparisonColumn> columns =
        [
            BuildCurrentColumn(recommendation.Snapshot.Player)
        ];
        foreach (var policy in Enum.GetValues<RecommendationPolicy>())
        {
            columns.Add(BuildPolicyColumn(recommendation, policy));
        }

        return new LoadoutComparison(
            BuildComparisonReference(
                recommendation,
                snapshotReference,
                columns),
            snapshotReference,
            Reference($"target:{recommendation.Snapshot.Target.CharacterId}"),
            columns,
            BuildBaselineProvenance(
                recommendation.Snapshot,
                snapshotReference));
    }

    private static void ValidateStyles(
        CombatLoadoutRecommendation recommendation)
    {
        var unknown = recommendation.Styles.FirstOrDefault(style =>
            !Enum.IsDefined(style.Policy));
        if (unknown is not null)
        {
            throw new ArgumentException(
                "The recommendation contains an unknown policy style.",
                nameof(recommendation));
        }

        var duplicate = recommendation.Styles
            .GroupBy(style => style.Policy)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"The recommendation contains duplicate {duplicate.Key} "
                + "style results.",
                nameof(recommendation));
        }
    }

    private static LoadoutComparisonColumn BuildCurrentColumn(
        PlayerCombatSnapshot player)
    {
        return new LoadoutComparisonColumn(
            LoadoutComparisonColumnKind.Current,
            LoadoutComparisonColumnStatus.Available,
            BuildCurrentLoadout(player),
            tacticalSummary: null,
            diagnostic: null);
    }

    private static LoadoutComparisonLoadout BuildCurrentLoadout(
        PlayerCombatSnapshot player)
    {
        var rows = Enum.GetValues<SkillCategory>().Select(category =>
        {
            var skills = player.EquippedSkills.Get(category)
                .Order()
                .Select(skillId => new LoadoutComparisonSkillCell(
                    SkillIdentity(player, category, skillId),
                    LoadoutComparisonValue<LoadoutComparisonMembership>
                        .Available(LoadoutComparisonMembership.Present),
                    EffectiveCost(player, skillId),
                    actions: []));
            return new LoadoutComparisonCategoryRow(
                category,
                Capacity(
                    player,
                    player.EquippedSkills,
                    player.GenericSlotAllocation,
                    player.SlotBudgets[category]),
                skills);
        });

        return new LoadoutComparisonLoadout(
            rows,
            LoadoutComparisonValue<GenericSlotAllocation>.Available(
                player.GenericSlotAllocation));
    }

    private static LoadoutComparisonColumn BuildPolicyColumn(
        CombatLoadoutRecommendation recommendation,
        RecommendationPolicy policy)
    {
        var kind = ColumnKind(policy);
        var style = recommendation.Styles.SingleOrDefault(value =>
            value.Policy == policy);
        if (style is null)
        {
            return new LoadoutComparisonColumn(
                kind,
                LoadoutComparisonColumnStatus.Unavailable,
                loadout: null,
                tacticalSummary: null,
                new LoadoutComparisonDiagnostic(
                    Reference("STYLE_RESULT_UNAVAILABLE"),
                    $"The {policy} style result is unavailable in this "
                    + "recommendation.",
                    evidenceReferences: []));
        }

        if (style.ManualPlan.Plan is null)
        {
            return new LoadoutComparisonColumn(
                kind,
                LoadoutComparisonColumnStatus.Infeasible,
                loadout: null,
                tacticalSummary: null,
                new LoadoutComparisonDiagnostic(
                    Reference($"NO_FEASIBLE_{policy.ToString().ToUpperInvariant()}"),
                    NonBlankDiagnostic(style.ManualPlan.Diagnostic, policy),
                    evidenceReferences: []));
        }

        ValidateSelectedPlan(style);
        var plan = style.ManualPlan.Plan;
        return new LoadoutComparisonColumn(
            kind,
            LoadoutComparisonColumnStatus.Available,
            BuildPolicyLoadout(recommendation.Snapshot.Player, plan),
            BuildTacticalSummary(recommendation, style, plan),
            diagnostic: null);
    }

    private static void ValidateSelectedPlan(
        CombatRecommendationStyleResult style)
    {
        var plan = style.ManualPlan.Plan!;
        if (plan.SelectedRecommendation.Policy != style.Policy)
        {
            throw new InvalidOperationException(
                $"The {style.Policy} manual plan contains a "
                + $"{plan.SelectedRecommendation.Policy} candidate.");
        }

        var first = style.Scoring.RankedCandidates.FirstOrDefault();
        if (first is null
            || !string.Equals(
                first.Candidate.StableKey,
                plan.SelectedRecommendation.Candidate.StableKey,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"The {style.Policy} manual plan does not reference its "
                + "highest-ranked candidate.");
        }
    }

    private static string NonBlankDiagnostic(
        string? diagnostic,
        RecommendationPolicy policy)
    {
        return string.IsNullOrWhiteSpace(diagnostic)
            ? $"No feasible {policy} policy winner is available."
            : diagnostic.Trim();
    }

    private static LoadoutComparisonLoadout BuildPolicyLoadout(
        PlayerCombatSnapshot player,
        ManualCombatPlan plan)
    {
        var proposal = plan.SelectedRecommendation.Candidate
            .FeasibleLoadout.Proposal;
        var budgets = plan.SelectedRecommendation.Candidate
            .FeasibleLoadout.SlotBudgets;
        var changes = plan.LoadoutChanges
            .GroupBy(change => (change.Category, change.SkillId))
            .ToDictionary(group => group.Key, group => group.ToArray());

        List<LoadoutComparisonCategoryRow> rows = [];
        foreach (var category in Enum.GetValues<SkillCategory>())
        {
            var current = player.EquippedSkills.Get(category).ToHashSet();
            var proposed = proposal.Skills.Get(category).ToHashSet();
            var expectedIds = current.Union(proposed).Order().ToArray();
            var categoryChanges = changes
                .Where(pair => pair.Key.Category == category)
                .ToDictionary(pair => pair.Key.SkillId, pair => pair.Value);

            if (!expectedIds.SequenceEqual(categoryChanges.Keys.Order()))
            {
                throw new InvalidOperationException(
                    $"The manual plan does not contain exactly one "
                    + $"membership group for every {category} skill in "
                    + "Current or the proposal.");
            }

            var cells = expectedIds.Select(skillId => BuildPolicyCell(
                player,
                category,
                skillId,
                current.Contains(skillId),
                proposed.Contains(skillId),
                categoryChanges[skillId]));
            rows.Add(
                new LoadoutComparisonCategoryRow(
                    category,
                    Capacity(
                        player,
                        proposal.Skills,
                        proposal.GenericSlotAllocation,
                        budgets[category]),
                    cells));
        }

        var extraChanges = changes.Keys.Where(key =>
            rows.All(row => row.Category != key.Category));
        if (extraChanges.Any())
        {
            throw new InvalidOperationException(
                "The manual plan contains changes for an unknown category.");
        }

        return new LoadoutComparisonLoadout(
            rows,
            LoadoutComparisonValue<GenericSlotAllocation>.Available(
                proposal.GenericSlotAllocation));
    }

    private static LoadoutComparisonSkillCell BuildPolicyCell(
        PlayerCombatSnapshot player,
        SkillCategory category,
        int skillId,
        bool isCurrent,
        bool isProposed,
        ManualLoadoutChange[] changes)
    {
        var membershipChanges = changes.Where(change => change.Kind is
            ManualLoadoutChangeKind.Add
            or ManualLoadoutChangeKind.Remove
            or ManualLoadoutChangeKind.Retain).ToArray();
        if (membershipChanges.Length != 1)
        {
            throw new InvalidOperationException(
                $"Skill {skillId} requires exactly one authoritative "
                + "Add, Remove, or Retain change.");
        }

        var membership = membershipChanges[0].Kind switch
        {
            ManualLoadoutChangeKind.Add when !isCurrent && isProposed =>
                LoadoutComparisonMembership.Added,
            ManualLoadoutChangeKind.Remove when isCurrent && !isProposed =>
                LoadoutComparisonMembership.Removed,
            ManualLoadoutChangeKind.Retain when isCurrent && isProposed =>
                LoadoutComparisonMembership.Retained,
            _ => throw new InvalidOperationException(
                $"Manual membership {membershipChanges[0].Kind} for skill "
                + $"{skillId} disagrees with Current and the proposal.")
        };

        var actionChanges = changes.Where(change => change.Kind is
            ManualLoadoutChangeKind.ChangeDirection
            or ManualLoadoutChangeKind.CompleteBreakthrough).ToArray();
        if (membership == LoadoutComparisonMembership.Removed
            && actionChanges.Length > 0)
        {
            throw new InvalidOperationException(
                $"Removed skill {skillId} cannot require a proposal action.");
        }

        var unknown = changes.FirstOrDefault(change => change.Kind is not (
            ManualLoadoutChangeKind.Add
            or ManualLoadoutChangeKind.Remove
            or ManualLoadoutChangeKind.Retain
            or ManualLoadoutChangeKind.ChangeDirection
            or ManualLoadoutChangeKind.CompleteBreakthrough));
        if (unknown is not null)
        {
            throw new InvalidOperationException(
                $"Skill {skillId} contains unknown manual change "
                + $"{unknown.Kind}.");
        }

        var actions = actionChanges
            .Select(MapAction)
            .OrderBy(action => action.Kind);
        return new LoadoutComparisonSkillCell(
            SkillIdentity(player, category, skillId),
            LoadoutComparisonValue<LoadoutComparisonMembership>.Available(
                membership),
            EffectiveCost(player, skillId),
            actions);
    }

    private static LoadoutComparisonSkillAction MapAction(
        ManualLoadoutChange change)
    {
        if (change.RequiredDirection is not (
            PracticeDirection.Direct or PracticeDirection.Reverse))
        {
            throw new InvalidOperationException(
                $"Manual change {change.Kind} for skill {change.SkillId} "
                + "requires an explicit Direct or Reverse direction.");
        }

        var kind = change.Kind switch
        {
            ManualLoadoutChangeKind.ChangeDirection =>
                LoadoutComparisonSkillActionKind.DirectionChangeRequired,
            ManualLoadoutChangeKind.CompleteBreakthrough =>
                LoadoutComparisonSkillActionKind.BreakthroughRequired,
            _ => throw new InvalidOperationException(
                $"Manual change {change.Kind} is not a comparison action.")
        };
        return new LoadoutComparisonSkillAction(
            kind,
            change.RequiredDirection.Value,
            MapReason(change.Reason));
    }

    private static LoadoutComparisonReason MapReason(
        RecommendationReason reason)
    {
        return new LoadoutComparisonReason(
            ReferenceFromRaw("reason", reason.Code),
            reason.Summary,
            References("evidence", reason.EvidenceReferences),
            References("threat", reason.ThreatCodes));
    }

    private static LoadoutComparisonCapacitySummary Capacity(
        PlayerCombatSnapshot player,
        CombatLoadoutSnapshot loadout,
        GenericSlotAllocation allocation,
        SlotBudget budget)
    {
        var category = budget.Category;
        return new LoadoutComparisonCapacitySummary(
            MapValue(budget.Used),
            LoadoutComparisonValue<int>.Available(budget.Capacity),
            MapValue(budget.Remaining),
            LoadoutComparisonValue<int>.Available(
                CategoryContribution(player, loadout, category)),
            LoadoutComparisonValue<int>.Available(
                category == SkillCategory.Neigong
                    ? 0
                    : allocation.Get(category)));
    }

    private static int CategoryContribution(
        PlayerCombatSnapshot player,
        CombatLoadoutSnapshot loadout,
        SkillCategory category)
    {
        var byId = player.LearnedSkills.ToDictionary(skill => skill.SkillId);
        return loadout.NeigongSkillIds.Sum(skillId =>
        {
            if (!byId.TryGetValue(skillId, out var skill))
            {
                throw new InvalidOperationException(
                    $"Equipped Neigong skill {skillId} is not learned.");
            }

            if (skill.Category != SkillCategory.Neigong)
            {
                throw new InvalidOperationException(
                    $"Skill {skillId} appears in Neigong but belongs to "
                    + $"{skill.Category}.");
            }

            return skill.SlotContribution.GetSpecific(category);
        });
    }

    private static LoadoutComparisonValue<int> EffectiveCost(
        PlayerCombatSnapshot player,
        int skillId)
    {
        return MapValue(
            CombatSkillCostCalculator.Calculate(player, skillId)
                .EffectiveCost);
    }

    private static LoadoutComparisonSkillIdentity SkillIdentity(
        PlayerCombatSnapshot player,
        SkillCategory category,
        int skillId)
    {
        var skill = player.LearnedSkills.SingleOrDefault(candidate =>
            candidate.SkillId == skillId)
            ?? throw new InvalidOperationException(
                $"Comparison skill {skillId} is not learned by the player.");
        if (skill.Category != category)
        {
            throw new InvalidOperationException(
                $"Comparison skill {skillId} belongs to {skill.Category}, "
                + $"not {category}.");
        }

        return new LoadoutComparisonSkillIdentity(category, skillId);
    }

    private static LoadoutComparisonTacticalSummary BuildTacticalSummary(
        CombatLoadoutRecommendation recommendation,
        CombatRecommendationStyleResult style,
        ManualCombatPlan plan)
    {
        var covered = plan.SelectedRecommendation.Candidate.ThreatCodes;
        var coveredSet = covered.ToHashSet(StringComparer.Ordinal);
        var unresolved = recommendation.ThreatAnalysis.Threats
            .Select(value => value.Threat.Code)
            .Where(code => !coveredSet.Contains(code));
        var explanation = style.Explanation;
        var conditionReferences = explanation?.Skills.SelectMany(skill =>
            skill.Conditions.Select(condition =>
                ReferenceFromRaw(
                    "condition",
                    $"{skill.SkillId}:{condition.Kind}:{condition.Status}:"
                    + condition.EvidenceReference))) ?? [];
        var caveatReferences = explanation?.Caveats.Select(caveat =>
            ReferenceFromRaw(
                "caveat",
                $"{caveat.Code}:{caveat.SkillId}")) ?? [];
        var evidence = EvidenceReferences(
            recommendation,
            style,
            plan);

        return new LoadoutComparisonTacticalSummary(
            LoadoutComparisonValue<int>.Available(
                plan.LoadoutChanges.Count(change =>
                    change.Kind != ManualLoadoutChangeKind.Retain)),
            RoleValue(
                recommendation.Snapshot.Player,
                plan.Defense.Primary,
                "No active defense is selected."),
            RoleValue(
                recommendation.Snapshot.Player,
                plan.Agility.Primary,
                "No active agility is selected."),
            References("threat", covered),
            References("threat", unresolved),
            OrderedDistinct(conditionReferences),
            OrderedDistinct(caveatReferences),
            evidence,
            style.Scoring.RankedCandidates[0].Components
                .OrderBy(component => component.Kind)
                .Select(component => new LoadoutComparisonScoreComponent(
                    component.Kind,
                    component.Weight,
                    component.Score.HasValue
                        ? LoadoutComparisonValue<decimal>.Available(
                            component.Score.Value)
                        : LoadoutComparisonValue<decimal>.Unavailable(
                            component.Explanation),
                    component.Explanation,
                    ReferenceFromRaw(
                        "evidence",
                        component.EvidenceReference))));
    }

    private static LoadoutComparisonValue<LoadoutComparisonSkillIdentity>
        RoleValue(
            PlayerCombatSnapshot player,
            CombatRoleChoice? role,
            string unavailableReason)
    {
        if (role is null)
        {
            return LoadoutComparisonValue<LoadoutComparisonSkillIdentity>
                .Unavailable(unavailableReason);
        }

        var skill = player.LearnedSkills.SingleOrDefault(candidate =>
            candidate.SkillId == role.SkillId)
            ?? throw new InvalidOperationException(
                $"Active-role skill {role.SkillId} is not learned.");
        return LoadoutComparisonValue<LoadoutComparisonSkillIdentity>
            .Available(
                new LoadoutComparisonSkillIdentity(
                    skill.Category,
                    skill.SkillId));
    }

    private static LoadoutComparisonReference[] EvidenceReferences(
        CombatLoadoutRecommendation recommendation,
        CombatRecommendationStyleResult style,
        ManualCombatPlan plan)
    {
        var manualEvidence = plan.LoadoutChanges
            .SelectMany(change => change.Reason.EvidenceReferences);
        var optionEvidence = plan.SelectedRecommendation.Candidate
            .SelectedOptions.Select(option => option.EvidenceReference);
        var scoreEvidence = plan.SelectedRecommendation.Components
            .Select(component => component.EvidenceReference);
        var explanationEvidence = style.Explanation is null
            ? []
            : style.Explanation.Skills
                .SelectMany(skill => skill.Conditions)
                .Select(condition => condition.EvidenceReference)
                .Concat(
                    style.Explanation.Caveats.SelectMany(caveat =>
                        caveat.EvidenceReferences));
        var threatEvidence = recommendation.ThreatAnalysis.Threats
            .SelectMany(value => value.Threat.Evidence)
            .Select(value => value.Reference);
        return References(
            "evidence",
            manualEvidence
                .Concat(optionEvidence)
                .Concat(scoreEvidence)
                .Concat(explanationEvidence)
                .Concat(threatEvidence));
    }

    private static LoadoutComparisonBaselineProvenance[]
        BuildBaselineProvenance(
            CombatSnapshot snapshot,
            LoadoutComparisonReference snapshotReference)
    {
        return
        [
            Provenance(
                snapshot,
                snapshotReference,
                LoadoutComparisonBaselineField.EquippedSkills,
                [CombatSnapshotObservationMerger.PlayerEquippedSkillsField]),
            Provenance(
                snapshot,
                snapshotReference,
                LoadoutComparisonBaselineField.GenericSlotAllocation,
                [CombatSnapshotObservationMerger
                    .PlayerGenericSlotAllocationField]),
            Provenance(
                snapshot,
                snapshotReference,
                LoadoutComparisonBaselineField.SlotBudgets,
                [CombatSnapshotObservationMerger.PlayerSlotBudgetsField]),
            Provenance(
                snapshot,
                snapshotReference,
                LoadoutComparisonBaselineField
                    .LegendaryBookCostAssignments,
                [
                    CombatSnapshotObservationMerger
                        .PlayerLegendaryBookCostAssignmentsField,
                    CombatSnapshotObservationMerger
                        .PlayerLegendaryBookCostSlotsField
                ])
        ];
    }

    private static LoadoutComparisonBaselineProvenance Provenance(
        CombatSnapshot snapshot,
        LoadoutComparisonReference snapshotReference,
        LoadoutComparisonBaselineField field,
        string[] fieldPaths)
    {
        var sources = snapshot.FieldSources
            .Where(source => fieldPaths.Contains(
                source.FieldPath,
                StringComparer.Ordinal))
            .OrderBy(source => source.FieldPath, StringComparer.Ordinal)
            .ToArray();
        if (sources.Length == 0)
        {
            return new LoadoutComparisonBaselineProvenance(
                field,
                SnapshotDataSource.Save,
                snapshot.Metadata.CapturedAtUtc,
                snapshotReference);
        }

        var first = sources[0];
        if (sources.Any(source =>
            source.Source != first.Source
            || source.CapturedAtUtc != first.CapturedAtUtc
            || !string.Equals(
                source.EvidenceReference,
                first.EvidenceReference,
                StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Baseline field {field} has conflicting source metadata.");
        }

        return new LoadoutComparisonBaselineProvenance(
            field,
            first.Source,
            first.CapturedAtUtc,
            ReferenceFromRaw("evidence", first.EvidenceReference));
    }

    private static LoadoutComparisonReference BuildSnapshotReference(
        CombatLoadoutRecommendation recommendation)
    {
        var snapshot = recommendation.Snapshot;
        List<string> parts =
        [
            snapshot.Metadata.SaveSha256,
            snapshot.Metadata.CapturedAtUtc.UtcTicks.ToString(),
            snapshot.Player.CharacterId.ToString(),
            snapshot.Target.CharacterId.ToString()
        ];
        parts.AddRange(snapshot.FieldSources
            .OrderBy(source => source.FieldPath, StringComparer.Ordinal)
            .Select(source =>
                $"{source.FieldPath}|{source.Source}|"
                + $"{source.CapturedAtUtc.UtcTicks}|"
                + source.EvidenceReference));
        if (snapshot.Target.LoadoutObservation is not null)
        {
            parts.Add(
                $"target-observation|"
                + $"{snapshot.Target.LoadoutObservation.ObservedAtUtc.UtcTicks}|"
                + snapshot.Target.LoadoutObservation.EvidenceReference);
        }

        return HashReference("snapshot", parts);
    }

    private static LoadoutComparisonReference BuildComparisonReference(
        CombatLoadoutRecommendation recommendation,
        LoadoutComparisonReference snapshotReference,
        IEnumerable<LoadoutComparisonColumn> columns)
    {
        List<string> parts =
        [
            snapshotReference.Value,
            recommendation.RequestedPolicy.ToString()
        ];
        parts.AddRange(recommendation.ThreatAnalysis.Threats
            .OrderBy(value => value.Threat.Code, StringComparer.Ordinal)
            .Select(value => value.Threat.Code));
        parts.AddRange(columns.Select(column =>
            column.Status == LoadoutComparisonColumnStatus.Available
                && column.Kind != LoadoutComparisonColumnKind.Current
                ? $"{column.Kind}|{column.Status}|"
                    + recommendation.Styles.Single(style =>
                        ColumnKind(style.Policy) == column.Kind)
                        .ManualPlan.Plan!.SelectedRecommendation.Candidate
                        .StableKey
                : $"{column.Kind}|{column.Status}|"
                    + column.Diagnostic?.Code.Value));
        return HashReference("comparison", parts);
    }

    private static LoadoutComparisonReference[] References(
        string fallbackPrefix,
        IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return OrderedDistinct(values.Select(value =>
            ReferenceFromRaw(fallbackPrefix, value)));
    }

    private static LoadoutComparisonReference[] OrderedDistinct(
        IEnumerable<LoadoutComparisonReference> references)
    {
        return
        [
            .. references
                .DistinctBy(reference => reference.Value, StringComparer.Ordinal)
                .OrderBy(reference => reference.Value, StringComparer.Ordinal)
        ];
    }

    private static LoadoutComparisonReference ReferenceFromRaw(
        string fallbackPrefix,
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                "A recommendation fact contains a blank logical reference.");
        }

        try
        {
            return Reference(value);
        }
        catch (ArgumentException)
        {
            return HashReference(fallbackPrefix, [value]);
        }
    }

    private static LoadoutComparisonReference HashReference(
        string prefix,
        IEnumerable<string> parts)
    {
        var canonical = string.Join('\u001f', parts);
        var digest = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        return Reference($"{prefix}:{digest}");
    }

    private static LoadoutComparisonReference Reference(string value) =>
        new(value);

    private static LoadoutComparisonValue<int> MapValue(
        SnapshotValue<int> value)
    {
        return value.IsAvailable
            ? LoadoutComparisonValue<int>.Available(value.Value)
            : LoadoutComparisonValue<int>.Unavailable(
                value.UnavailableReason!);
    }

    private static LoadoutComparisonColumnKind ColumnKind(
        RecommendationPolicy policy) => policy switch
        {
            RecommendationPolicy.Safe => LoadoutComparisonColumnKind.Safe,
            RecommendationPolicy.Balanced =>
                LoadoutComparisonColumnKind.Balanced,
            RecommendationPolicy.Aggressive =>
                LoadoutComparisonColumnKind.Aggressive,
            _ => throw new ArgumentOutOfRangeException(
                nameof(policy),
                policy,
                "Unknown recommendation policy.")
        };
}
