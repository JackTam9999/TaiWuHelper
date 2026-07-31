using TaiWu.Application.CombatRecommendations;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;

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

        var snapshotReference =
            $"snapshot:{recommendation.Snapshot.Metadata.CapturedAtUtc:O}";
        var skillNames = recommendation.Snapshot.Player.LearnedSkills
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
                skillNames))
            .ToArray();

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
            [.. recommendation.ThreatAnalysis.Threats
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
                    [.. value.Threat.Evidence.Select(evidence => evidence.Reference)]))],
            styles,
            MapWarnings(recommendation, skillNames));
    }

    private static RecommendationStyleViewModel MapStyle(
        string snapshotReference,
        RecommendationPolicy requestedPolicy,
        CombatRecommendationStyleResult style,
        IReadOnlyDictionary<int, string> skillNames)
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
            MapCategories(candidateReference, candidate, skills),
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
        RecommendedSkillViewModel[] skills)
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
                    [.. skills.Where(skill => skill.Category == category)]);
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
