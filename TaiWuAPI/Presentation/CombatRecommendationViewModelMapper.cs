using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.LoadoutComparisons;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.LoadoutComparisons;

namespace TaiWuAPI.Presentation;

public static class CombatRecommendationViewModelMapper
{
    public const string InformationOnlyNotice =
        "Information only — TaiWu Helper cannot apply, equip, or execute "
        + "this recommendation.";

    public static CombatRecommendationViewModel Map(
        CombatLoadoutRecommendation recommendation)
    {
        ArgumentNullException.ThrowIfNull(recommendation);

        var comparison = CombatLoadoutComparisonBuilder.Build(recommendation);
        var snapshotReference = comparison.SnapshotReference.Value;
        var skillNames = recommendation.Snapshot.Player.LearnedSkills
            .ToDictionary(
                skill => skill.SkillId,
                skill => skill.DisplayName.IsAvailable
                    ? skill.DisplayName.Value
                    : "Unnamed skill");
        var targetSkillNames = recommendation.Snapshot.Target.LearnedSkills
            .ToDictionary(
                skill => skill.SkillId,
                skill => skill.DisplayName.IsAvailable
                    ? skill.DisplayName.Value
                    : "Unnamed skill");
        var styles = recommendation.Styles
            .Select(style => MapStyle(
                snapshotReference,
                recommendation.RequestedPolicy,
                style,
                skillNames,
                recommendation.Snapshot.Player.GenericSlotAllocation))
            .ToArray();
        var threats = recommendation.ThreatAnalysis.Threats
            .Select(value => new ThreatViewModel(
                ThreatReference(value.Threat.Code),
                value.Threat.Code,
                UiEntityText.UseNames(value.Threat.Title, skillNames),
                UiEntityText.UseNames(
                    value.Threat.Explanation,
                    skillNames),
                value.Threat.Kind,
                value.Threat.Severity,
                value.Threat.ActivationTiming,
                [.. value.Threat.Evidence.Select(evidence =>
                    evidence.Reference)]))
            .ToArray();
        var warnings = MapWarnings(recommendation, skillNames);

        return new CombatRecommendationViewModel(
            snapshotReference,
            recommendation.Snapshot.Metadata.CapturedAtUtc,
            recommendation.Snapshot.Metadata.SaveLastWriteTimeUtc.IsAvailable
                ? recommendation.Snapshot.Metadata.SaveLastWriteTimeUtc.Value
                : null,
            recommendation.Snapshot.Metadata.GameDataVersion.IsAvailable
                ? recommendation.Snapshot.Metadata.GameDataVersion.Value
                : null,
            recommendation.RequestedPolicy,
            StyleReference(snapshotReference, recommendation.RequestedPolicy),
            InformationOnlyNotice,
            threats,
            styles,
            warnings,
            MapInnerPowerState(recommendation.Snapshot.Player),
            MapTargetObservationImpact(
                recommendation,
                skillNames,
                targetSkillNames),
            MapComparison(
                comparison,
                recommendation.Snapshot.Player,
                skillNames,
                threats,
                styles,
                warnings));
    }

    private static LoadoutComparisonViewModel MapComparison(
        LoadoutComparison comparison,
        PlayerCombatSnapshot player,
        IReadOnlyDictionary<int, string> skillNames,
        IReadOnlyList<ThreatViewModel> threats,
        IReadOnlyList<RecommendationStyleViewModel> styles,
        IReadOnlyList<RecommendationWarningViewModel> warnings)
    {
        var currentAllocation = comparison.Current.Loadout!
            .GenericSlotAllocation;
        var columns = comparison.Columns
            .Select(column => MapComparisonColumn(
                comparison.SnapshotReference.Value,
                column,
                currentAllocation,
                skillNames,
                threats,
                styles.SingleOrDefault(style =>
                    style.Style == column.Policy)))
            .ToArray();
        var categories = Enum.GetValues<SkillCategory>()
            .Select(category => MapComparisonCategory(
                category,
                comparison.Columns,
                player,
                skillNames))
            .ToArray();

        return new LoadoutComparisonViewModel(
            comparison.ComparisonReference.Value,
            comparison.SnapshotReference.Value,
            columns,
            categories,
            [.. comparison.BaselineProvenance.Select(value =>
                new LoadoutComparisonProvenanceViewModel(
                    value.Field,
                    value.Source,
                    value.CapturedAtUtc))],
            "TaiWu Helper cannot equip, redirect, or break through skills. "
            + "Follow these instructions manually in the game.",
            [.. warnings
                .Where(warning => warning.IsCritical
                    || warning.Kind
                        == PresentationWarningKind.UnverifiedMechanic)
                .Select(warning =>
                    new LoadoutComparisonUnsupportedViewModel(
                        warning.IsCritical,
                        warning.Message,
                        warning.EffectOnRecommendation,
                        warning.EvidenceReferences))]);
    }

    private static LoadoutComparisonColumnViewModel MapComparisonColumn(
        string snapshotReference,
        LoadoutComparisonColumn column,
        LoadoutComparisonValue<GenericSlotAllocation> currentAllocation,
        IReadOnlyDictionary<int, string> skillNames,
        IReadOnlyList<ThreatViewModel> threats,
        RecommendationStyleViewModel? style)
    {
        var allocation = column.Loadout?.GenericSlotAllocation;
        var tactical = column.TacticalSummary;
        return new LoadoutComparisonColumnViewModel(
            column.Kind,
            column.Status,
            column.Policy,
            column.Policy.HasValue
                ? StyleReference(snapshotReference, column.Policy.Value)
                : null,
            allocation?.IsAvailable == true
                ? new LoadoutComparisonGenericSlotsViewModel(
                    allocation.Value.TotalSlots,
                    allocation.Value.Attack,
                    allocation.Value.Agility,
                    allocation.Value.Defense,
                    allocation.Value.Assistance)
                : null,
            column.Policy.HasValue
                && allocation?.IsAvailable == true
                && currentAllocation.IsAvailable
                && allocation.Value != currentAllocation.Value,
            tactical?.ManualActionCount.IsAvailable == true
                ? tactical.ManualActionCount.Value
                : null,
            tactical is not null
                && !tactical.ManualActionCount.IsAvailable
                    ? tactical.ManualActionCount.UnavailableReason
                    : null,
            column.Diagnostic is null
                ? allocation is not null && !allocation.IsAvailable
                    ? UiEntityText.UseNames(
                        allocation.UnavailableReason!,
                        skillNames)
                    : null
                : UiEntityText.UseNames(
                    column.Diagnostic.Summary,
                    skillNames),
            MapTactical(
                column.TacticalSummary,
                style,
                threats,
                skillNames));
    }

    private static LoadoutComparisonTacticalViewModel? MapTactical(
        LoadoutComparisonTacticalSummary? tactical,
        RecommendationStyleViewModel? style,
        IReadOnlyList<ThreatViewModel> threats,
        IReadOnlyDictionary<int, string> skillNames)
    {
        if (tactical is null)
        {
            return null;
        }

        if (style is null || !style.HasRecommendation)
        {
            throw new InvalidOperationException(
                "An available comparison tactical summary requires its "
                + "mapped recommendation style.");
        }

        return new LoadoutComparisonTacticalViewModel(
            style.Style,
            MapRole(tactical.ActiveDefense, skillNames),
            MapRole(tactical.ActiveAgility, skillNames),
            MapTacticalThreats(tactical.CoveredThreats, threats),
            MapTacticalThreats(tactical.UnresolvedThreats, threats),
            [.. style.Categories
                .SelectMany(category => category.Skills)
                .SelectMany(skill => skill.Conditions.Select(condition =>
                    new LoadoutComparisonConditionSummaryViewModel(
                        skill.Name ?? "Unnamed skill",
                        condition.Kind,
                        condition.Criticality,
                        condition.Status,
                        condition.Evaluation,
                        condition.EvidenceReference)))],
            [.. style.Caveats.Select(caveat =>
                new LoadoutComparisonCaveatSummaryViewModel(
                    caveat.Kind,
                    caveat.Explanation,
                    caveat.SkillId.HasValue
                        ? SkillName(skillNames, caveat.SkillId.Value)
                        : null,
                    caveat.EvidenceReferences))],
            [.. tactical.ScoreComponents.Select(component =>
                new LoadoutComparisonScoreSummaryViewModel(
                    component.Kind,
                    component.Weight,
                    component.Score.IsAvailable
                        ? component.Score.Value
                        : null,
                    component.Score.IsAvailable
                        ? null
                        : UiEntityText.UseNames(
                            component.Score.UnavailableReason!,
                            skillNames),
                    UiEntityText.UseNames(
                        component.Explanation,
                        skillNames),
                    component.EvidenceReference.Value))],
            [.. tactical.EvidenceReferences.Select(value => value.Value)]);
    }

    private static LoadoutComparisonRoleViewModel MapRole(
        LoadoutComparisonValue<LoadoutComparisonSkillIdentity> role,
        IReadOnlyDictionary<int, string> skillNames) => role.IsAvailable
            ? new(
                SkillName(skillNames, role.Value.SkillId),
                UnavailableReason: null)
            : new(
                SkillName: null,
                UiEntityText.UseNames(
                    role.UnavailableReason!,
                    skillNames));

    private static LoadoutComparisonThreatViewModel[] MapTacticalThreats(
        IReadOnlyList<LoadoutComparisonReference> references,
        IReadOnlyList<ThreatViewModel> threats) =>
    [
        .. references.Select(reference =>
        {
            var threat = threats.SingleOrDefault(value =>
                string.Equals(
                    value.Code,
                    reference.Value,
                    StringComparison.Ordinal)
                || string.Equals(
                    value.Reference,
                    reference.Value,
                    StringComparison.Ordinal));
            if (threat is null)
            {
                throw new InvalidOperationException(
                    $"Comparison threat {reference.Value} has no mapped "
                    + "typed threat fact.");
            }

            return new LoadoutComparisonThreatViewModel(
                threat.Reference,
                threat.Code,
                threat.Title,
                threat.Severity,
                threat.EvidenceReferences);
        })
    ];

    private static LoadoutComparisonCategoryViewModel MapComparisonCategory(
        SkillCategory category,
        IReadOnlyList<LoadoutComparisonColumn> columns,
        PlayerCombatSnapshot player,
        IReadOnlyDictionary<int, string> skillNames)
    {
        var categoryRows = columns
            .Where(column => column.Loadout is not null)
            .Select(column => (
                column.Kind,
                Row: column.Loadout!.Categories.Single(value =>
                    value.Category == category)))
            .ToArray();
        var skillIds = categoryRows
            .SelectMany(value => value.Row.Skills)
            .Select(cell => cell.Identity.SkillId)
            .Distinct()
            .Order()
            .ToArray();
        var learned = player.LearnedSkills.ToDictionary(skill => skill.SkillId);

        return new LoadoutComparisonCategoryViewModel(
            category,
            CategoryDisplayName(category),
            [.. categoryRows.Select(value => MapCapacity(
                value.Kind,
                value.Row.Capacity))],
            [.. skillIds.Select(skillId =>
            {
                if (!learned.TryGetValue(skillId, out var skill)
                    || skill.Category != category)
                {
                    throw new InvalidOperationException(
                        $"Comparison skill {skillId} is unavailable in "
                        + $"the matching {category} snapshot.");
                }

                return new LoadoutComparisonSkillRowViewModel(
                    category,
                    skillId,
                    skill.DisplayName.IsAvailable
                        ? skill.DisplayName.Value
                        : null,
                    skill.DisplayName.IsAvailable
                        ? null
                        : skill.DisplayName.UnavailableReason,
                    [.. categoryRows
                        .Select(value => (
                            value.Kind,
                            Cell: value.Row.Skills.SingleOrDefault(cell =>
                                cell.Identity.SkillId == skillId)))
                        .Where(value => value.Cell is not null)
                        .Select(value => MapComparisonSkill(
                            value.Kind,
                            value.Cell!,
                            skill,
                            skillNames))]);
            })]);
    }

    private static LoadoutComparisonCapacityCellViewModel MapCapacity(
        LoadoutComparisonColumnKind column,
        LoadoutComparisonCapacitySummary capacity) => new(
            column,
            Available(capacity.Used),
            Unavailable(capacity.Used),
            Available(capacity.Capacity),
            Unavailable(capacity.Capacity),
            Available(capacity.Remaining),
            Unavailable(capacity.Remaining),
            Available(capacity.CategoryContribution),
            Unavailable(capacity.CategoryContribution),
            Available(capacity.GenericContribution),
            Unavailable(capacity.GenericContribution));

    private static LoadoutComparisonSkillCellViewModel MapComparisonSkill(
        LoadoutComparisonColumnKind column,
        LoadoutComparisonSkillCell cell,
        CombatSkillSnapshot skill,
        IReadOnlyDictionary<int, string> skillNames) => new(
            column,
            cell.Membership.IsAvailable ? cell.Membership.Value : null,
            cell.Membership.IsAvailable
                ? null
                : cell.Membership.UnavailableReason,
            skill.Direction.IsAvailable ? skill.Direction.Value : null,
            skill.Direction.IsAvailable
                ? null
                : skill.Direction.UnavailableReason,
            Available(cell.EffectiveCost),
            Unavailable(cell.EffectiveCost),
            [.. cell.Actions.Select(action =>
                new LoadoutComparisonSkillActionViewModel(
                    action.Kind,
                    action.RequiredDirection,
                    UiEntityText.UseNames(
                        action.Reason.Summary,
                        skillNames)))]);

    private static int? Available(LoadoutComparisonValue<int> value) =>
        value.IsAvailable ? value.Value : null;

    private static string? Unavailable(LoadoutComparisonValue<int> value) =>
        value.IsAvailable ? null : value.UnavailableReason;

    private static TargetObservationImpactViewModel? MapTargetObservationImpact(
        CombatLoadoutRecommendation recommendation,
        IReadOnlyDictionary<int, string> skillNames,
        IReadOnlyDictionary<int, string> targetSkillNames)
    {
        var impact = recommendation.TargetObservationImpact;
        if (impact is null)
        {
            return null;
        }

        return new TargetObservationImpactViewModel(
            [.. impact.Threats.Select(value =>
                new TargetThreatImpactViewModel(
                    value.ThreatCode,
                    UiEntityText.UseNames(value.Title, skillNames),
                    value.Kind,
                    value.Severity,
                    value.SourceKinds,
                    value.EvidenceReferences))],
            [.. impact.FeasibilityChanges.Select(value =>
                MapRecommendationImpact(value, skillNames))],
            [.. impact.ScoringChanges.Select(value =>
                MapRecommendationImpact(value, skillNames))],
            [.. impact.UnsupportedEvidence.Select(value =>
                new TargetUnsupportedEvidenceViewModel(
                    value.Code,
                    value.WasPresentBefore,
                    value.EvidenceReference,
                    value.SkillId,
                    value.SkillId.HasValue
                        && targetSkillNames.TryGetValue(
                            value.SkillId.Value,
                            out var skillName)
                            ? skillName
                            : null))],
            impact.PartialCoverageLeavesUnknown,
            [.. impact.Conflicts.Select(value =>
                new TargetObservationConflictViewModel(
                    value.Field,
                    value.ReasonCode,
                    value.PrecedenceRule,
                    [.. value.Sources.Select(source =>
                        new TargetObservationConflictSourceViewModel(
                            source.Source,
                            source.CapturedAtUtc,
                            source.EvidenceReference))]))],
            "Evidence confidence describes provenance, not a win probability.");
    }

    private static TargetRecommendationImpactViewModel MapRecommendationImpact(
        TargetRecommendationImpact value,
        IReadOnlyDictionary<int, string> skillNames) => new(
            value.Policy,
            value.Kind,
            value.Cause,
            value.SkillId,
            skillNames.TryGetValue(value.SkillId, out var name)
                ? name
                : "Unnamed skill",
            value.Category,
            value.RequiredDirection,
            value.ThreatCodes,
            [.. value.ThreatTitles.Select(title =>
                UiEntityText.UseNames(title, skillNames))],
            value.EvidenceReferences);

    private static InnerPowerStateViewModel? MapInnerPowerState(
        PlayerCombatSnapshot player)
    {
        if (!player.InnerPowerState.IsAvailable)
        {
            return null;
        }

        var state = player.InnerPowerState.Value;
        return new InnerPowerStateViewModel(
            state.DisplayName.IsAvailable
                ? state.DisplayName.Value
                : "Unavailable",
            state.EffectDescription.IsAvailable
                ? state.EffectDescription.Value
                : null,
            state.BacklashOnUseElement);
    }

    private static RecommendationStyleViewModel MapStyle(
        string snapshotReference,
        RecommendationPolicy requestedPolicy,
        CombatRecommendationStyleResult style,
        IReadOnlyDictionary<int, string> skillNames,
        GenericSlotAllocation currentGenericSlots)
    {
        var styleReference = StyleReference(snapshotReference, style.Policy);
        var plan = style.ManualPlan.Plan;
        if (plan is null)
        {
            return new RecommendationStyleViewModel(
                styleReference,
                snapshotReference,
                style.Policy,
                style.Policy == requestedPolicy,
                HasRecommendation: false,
                CandidateReference: null,
                TotalScore: null,
                Scores: [],
                Categories: [],
                ManualChanges: [],
                OpeningActions: [],
                SwitchingConditions: [],
                Caveats: [],
                style.ManualPlan.Diagnostic is null
                    ? null
                    : UiEntityText.UseNames(
                        style.ManualPlan.Diagnostic,
                        skillNames));
        }

        var candidate = plan.SelectedRecommendation.Candidate;
        var candidateReference = $"candidate:{candidate.StableKey}";
        var explanation = style.Explanation!;
        var skills = explanation.Skills
            .Select(skill => MapSkill(
                candidateReference,
                skill,
                skillNames))
            .ToArray();

        return new RecommendationStyleViewModel(
            styleReference,
            snapshotReference,
            style.Policy,
            style.Policy == requestedPolicy,
            HasRecommendation: true,
            candidateReference,
            plan.SelectedRecommendation.TotalScore,
            [.. plan.SelectedRecommendation.Components
                .Select(component => new RecommendationScoreViewModel(
                    $"{candidateReference}:score:{component.Kind}",
                    component.Kind,
                    component.Weight,
                    component.Score,
                    component.WeightedPoints,
                    UiEntityText.UseNames(
                        component.Explanation,
                        skillNames),
                    component.EvidenceReference))],
            MapCategories(
                candidateReference,
                candidate,
                skills,
                currentGenericSlots),
            [.. plan.LoadoutChanges.Select(change => MapChange(
                candidateReference,
                change,
                skillNames))],
            [.. plan.OpeningActions
                .Select(action => MapStep(
                    candidateReference,
                    "opening",
                    action,
                    skillNames))],
            [.. plan.SwitchingConditions
                .Select(action => MapStep(
                    candidateReference,
                    "switch",
                    action,
                    skillNames))],
            [.. explanation.Caveats
                .Select((caveat, index) => new RecommendationCaveatViewModel(
                    $"{candidateReference}:caveat:{caveat.Code}:{index + 1}",
                    caveat.Kind,
                    caveat.Code,
                    UiEntityText.UseNames(caveat.Explanation, skillNames),
                    caveat.SkillId,
                    caveat.EvidenceReferences))],
            Diagnostic: null);
    }

    private static LoadoutCategoryViewModel[] MapCategories(
        string candidateReference,
        GeneratedCombatLoadout candidate,
        RecommendedSkillViewModel[] skills,
        GenericSlotAllocation currentGenericSlots)
    {
        var proposal = candidate.FeasibleLoadout.Proposal;
        return [.. Enum.GetValues<SkillCategory>()
            .Select(category =>
            {
                var budget = candidate.FeasibleLoadout.SlotBudgets[category];
                var remaining = budget.Remaining;
                return new LoadoutCategoryViewModel(
                    $"{candidateReference}:category:{category}",
                    category,
                    CategoryDisplayName(category),
                    budget.Used.IsAvailable ? budget.Used.Value : null,
                    budget.Used.IsAvailable
                        ? null
                        : budget.Used.UnavailableReason,
                    budget.Capacity,
                    remaining.IsAvailable ? remaining.Value : null,
                    remaining.IsAvailable
                        ? null
                        : remaining.UnavailableReason,
                    category == SkillCategory.Neigong
                        ? 0
                        : proposal.GenericSlotAllocation.Get(category),
                    [.. skills.Where(skill => skill.Category == category)],
                    category == SkillCategory.Neigong
                        ? 0
                        : currentGenericSlots.Get(category));
            })];
    }

    private static RecommendedSkillViewModel MapSkill(
        string candidateReference,
        SkillRecommendationExplanation skill,
        IReadOnlyDictionary<int, string> skillNames)
    {
        var skillReference = SkillReference(
            candidateReference,
            skill.SkillId);
        return new RecommendedSkillViewModel(
            skillReference,
            skill.SkillId,
            skill.DisplayName.IsAvailable
                ? skill.DisplayName.Value
                : null,
            skill.Category,
            skill.Direction.CurrentDirection.IsAvailable
                ? skill.Direction.CurrentDirection.Value
                : null,
            skill.Direction.RequiredDirection,
            skill.Direction.RequiresManualDirectionChange,
            new SkillCostViewModel(
                skill.Cost.BaseCost.IsAvailable
                    ? skill.Cost.BaseCost.Value
                    : null,
                skill.Cost.BaseCost.IsAvailable
                    ? null
                    : UseNames(
                        skill.Cost.BaseCost.UnavailableReason,
                        skillNames),
                skill.Cost.EffectiveCost.IsAvailable
                    ? skill.Cost.EffectiveCost.Value
                    : null,
                skill.Cost.EffectiveCost.IsAvailable
                    ? null
                    : UseNames(
                        skill.Cost.EffectiveCost.UnavailableReason,
                        skillNames),
                skill.Cost.MasteryReduction.IsAvailable
                    ? skill.Cost.MasteryReduction.Value
                    : null,
                skill.Cost.LegendaryBookReduction.IsAvailable
                    ? skill.Cost.LegendaryBookReduction.Value
                    : null,
                skill.Cost.EvidenceReferences),
            new SkillCounterViewModel(
                skill.Counter.IsAvailable,
                skill.Counter.Strength,
                skill.Counter.ActivationTiming,
                skill.Counter.EvidenceReference,
                skill.Counter.UnavailableReason is null
                    ? null
                    : UiEntityText.UseNames(
                        skill.Counter.UnavailableReason,
                        skillNames)),
            [.. skill.Threats.Select(threat => ThreatReference(threat.Code))],
            [.. skill.Conditions
                .Select((condition, index) =>
                    new SkillConditionViewModel(
                        $"{skillReference}:condition:"
                        + $"{condition.Kind}:{index + 1}",
                        condition.Kind,
                        condition.Criticality,
                        condition.Status,
                        UiEntityText.UseNames(
                            condition.Evaluation,
                            skillNames),
                        condition.EvidenceReference))],
            [.. skill.Reasons
                .Select(reason => MapReason(
                    candidateReference,
                    skill.SkillId,
                    reason,
                    skillNames))],
            skill.Direction.RequiresBreakthrough);
    }

    private static ManualLoadoutChangeViewModel MapChange(
        string candidateReference,
        ManualLoadoutChange change,
        IReadOnlyDictionary<int, string> skillNames)
    {
        return new ManualLoadoutChangeViewModel(
            $"{candidateReference}:change:{change.Kind}:"
            + $"{change.Category}:{change.SkillId}",
            change.Kind,
            change.Category,
            change.SkillId,
            SkillName(skillNames, change.SkillId),
            change.RequiredDirection,
            MapReason(
                candidateReference,
                change.SkillId,
                change.Reason,
                skillNames));
    }

    private static BattlePlanStepViewModel MapStep(
        string candidateReference,
        string phase,
        BattlePlanInstruction instruction,
        IReadOnlyDictionary<int, string> skillNames)
    {
        var reasonSkillId = instruction.AlternativeSkillId
            ?? instruction.SkillId;
        return new BattlePlanStepViewModel(
            $"{candidateReference}:plan:{phase}:{instruction.Sequence}",
            instruction.Kind,
            instruction.SkillId,
            SkillName(skillNames, instruction.SkillId),
            instruction.AlternativeSkillId,
            instruction.AlternativeSkillId is int alternativeSkillId
                ? SkillName(skillNames, alternativeSkillId)
                : null,
            instruction.Condition,
            MapReason(
                candidateReference,
                reasonSkillId,
                instruction.Reason,
                skillNames));
    }

    private static string SkillName(
        IReadOnlyDictionary<int, string> skillNames,
        int skillId) =>
        skillNames.TryGetValue(skillId, out var name)
        && !string.IsNullOrWhiteSpace(name)
            ? name
            : "Unnamed skill";

    private static string? UseNames(
        string? text,
        IReadOnlyDictionary<int, string> skillNames) =>
        text is null ? null : UiEntityText.UseNames(text, skillNames);

    private static RecommendationReasonViewModel MapReason(
        string candidateReference,
        int skillId,
        RecommendationReason reason,
        IReadOnlyDictionary<int, string> skillNames)
    {
        return new RecommendationReasonViewModel(
            $"{SkillReference(candidateReference, skillId)}:"
            + $"reason:{reason.Code}",
            reason.Code,
            UiEntityText.UseNames(reason.Summary, skillNames),
            reason.EvidenceReferences,
            [.. reason.ThreatCodes.Select(ThreatReference)]);
    }

    private static RecommendationWarningViewModel[] MapWarnings(
        CombatLoadoutRecommendation recommendation,
        IReadOnlyDictionary<int, string> skillNames)
    {
        var snapshotWarnings = recommendation.SnapshotWarnings
            .Select((warning, index) => MapWarning(
                $"warning:snapshot:{warning.Code}:{index + 1}",
                "Snapshot",
                warning.Code,
                warning.Message,
                evidenceReferences: [],
                occurrences: 1,
                skillNames));
        var threatWarnings = recommendation.ThreatAnalysis.Warnings
            .Select((warning, index) => MapWarning(
                $"warning:threat:{warning.Code}:{index + 1}",
                "ThreatAnalysis",
                warning.Code,
                warning.Message,
                [warning.Mechanic.EvidenceReference],
                occurrences: 1,
                skillNames));
        var generationWarnings = recommendation.Generation.Diagnostics
            .Select((warning, index) => MapWarning(
                $"warning:generation:{warning.Code}:{index + 1}",
                "CandidateGeneration",
                warning.Code.ToString(),
                GenerationWarningMessage(warning),
                evidenceReferences: [],
                warning.Occurrences,
                skillNames));

        RecommendationWarningViewModel[] warnings =
        [
            .. snapshotWarnings,
            .. threatWarnings,
            .. generationWarnings
        ];
        return
        [
            .. warnings
                .GroupBy(warning => (
                    warning.Source,
                    warning.Code,
                    warning.Kind,
                    warning.IsCritical,
                    warning.Message,
                    warning.EffectOnRecommendation))
                .Select(group => group.First() with
                {
                    Occurrences = group.Sum(warning => warning.Occurrences),
                    EvidenceReferences =
                    [
                        .. group
                            .SelectMany(warning => warning.EvidenceReferences)
                            .Distinct(StringComparer.Ordinal)
                    ]
                })
        ];
    }

    private static string GenerationWarningMessage(
        CombatLoadoutGenerationDiagnostic warning) =>
        warning.Occurrences == 1
            ? warning.Reason
            : $"{warning.Reason} Occurred in {warning.Occurrences} explored "
              + "combinations.";

    private static RecommendationWarningViewModel MapWarning(
        string reference,
        string source,
        string code,
        string message,
        IReadOnlyList<string> evidenceReferences,
        int occurrences,
        IReadOnlyDictionary<int, string> skillNames)
    {
        var classification =
            RecommendationWarningPresentation.Classify(source, code);
        return new RecommendationWarningViewModel(
            reference,
            source,
            code,
            classification.Kind,
            classification.IsCritical,
            occurrences,
            UiEntityText.UseNames(message, skillNames),
            classification.EffectOnRecommendation,
            evidenceReferences);
    }

    private static string CategoryDisplayName(
        SkillCategory category) =>
        category switch
        {
            SkillCategory.Neigong => "內功",
            SkillCategory.Attack => "摧破",
            SkillCategory.Agility => "輕靈",
            SkillCategory.Defense => "護體",
            SkillCategory.Assistance => "奇竅",
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };

    private static string StyleReference(
        string snapshotReference,
        RecommendationPolicy style) =>
        $"{snapshotReference}:style:{style}";

    private static string ThreatReference(string code) =>
        $"threat:{code}";

    private static string SkillReference(
        string candidateReference,
        int skillId) =>
        $"{candidateReference}:skill:{skillId}";
}
