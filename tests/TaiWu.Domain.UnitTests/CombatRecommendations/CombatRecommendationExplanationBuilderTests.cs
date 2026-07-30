using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatRecommendations;

public sealed class CombatRecommendationExplanationBuilderTests
{
    [Fact]
    public void Every_selected_skill_has_at_least_one_reason()
    {
        var first = Skill(100, SkillCategory.Attack);
        var second = Skill(101, SkillCategory.Defense);
        var player = Player([first, second]);
        var plan = Plan(
            player,
            targetThreats: [],
            Context(),
            Option(first, "THREAT_A"),
            Option(second, "THREAT_B"));

        var explanation = CombatRecommendationExplanationBuilder.Build(
            player,
            targetThreats: [],
            plan);

        Assert.Equal(2, explanation.Skills.Length);
        Assert.All(
            explanation.Skills,
            skill => Assert.NotEmpty(skill.Reasons));
    }

    [Fact]
    public void Links_threat_counter_direction_cost_and_conditions()
    {
        var skill = Skill(100, SkillCategory.Defense);
        var player = Player([skill]);
        var threat = Threat(
            "MIND_RESONANCE_CASCADE",
            TargetThreatEvidenceConfidence.VerifiedRule);
        var requirement = new WeaponRequirement(
            weaponTypeId: 10,
            CombatRequirementCriticality.Hard,
            "evidence:weapon");
        var option = Option(
            skill,
            threat.Code,
            CombatCounterActivationTiming.ActiveDefense,
            PracticeDirection.Reverse,
            requirement);
        var plan = Plan(player, [threat], Context(weaponTypeId: 10), option);

        var explanation = CombatRecommendationExplanationBuilder.Build(
            player,
            [threat],
            plan);

        var selected = Assert.Single(explanation.Skills);
        Assert.Equal(
            threat.Code,
            Assert.Single(selected.Threats).Code);
        Assert.True(selected.Counter.IsAvailable);
        Assert.Equal(
            CombatCounterStrength.Mitigation,
            selected.Counter.Strength);
        Assert.Equal(
            CombatCounterActivationTiming.ActiveDefense,
            selected.Counter.ActivationTiming);
        Assert.Equal(
            PracticeDirection.Reverse,
            selected.Direction.RequiredDirection);
        Assert.True(selected.Direction.RequiresManualChange);
        Assert.Equal(
            skill.ReverseEffectId.Value,
            selected.Direction.ExpectedEffectId);
        Assert.Equal(1, selected.Cost.EffectiveCost.Value);
        Assert.Equal(
            SkillCategory.Defense,
            selected.Cost.CategoryBudget.Category);
        var condition = Assert.Single(selected.Conditions);
        Assert.Equal(RecommendationConditionKind.Weapon, condition.Kind);
        Assert.Equal(CombatRequirementStatus.Satisfied, condition.Status);
        Assert.Equal("evidence:weapon", condition.EvidenceReference);
    }

    [Fact]
    public void Observational_or_hypothetical_threats_are_explicit_assumptions()
    {
        var skill = Skill(100, SkillCategory.Attack);
        var player = Player([skill]);
        var threat = Threat(
            "OBSERVED_THREAT",
            TargetThreatEvidenceConfidence.CurrentScreenObservation);
        var plan = Plan(
            player,
            [threat],
            Context(),
            Option(skill, threat.Code));

        var explanation = CombatRecommendationExplanationBuilder.Build(
            player,
            [threat],
            plan);

        var assumption = Assert.Single(explanation.Assumptions);
        Assert.Equal(
            RecommendationCaveatKind.Assumption,
            assumption.Kind);
        Assert.Equal(
            "THREAT_CURRENT_SCREEN_OBSERVATION",
            assumption.Code);
        Assert.Equal(
            ["evidence:threat:OBSERVED_THREAT"],
            assumption.EvidenceReferences);
    }

    [Fact]
    public void Missing_damage_name_and_condition_data_are_explicit()
    {
        var skill = Skill(
            100,
            SkillCategory.Attack,
            nameAvailable: false);
        var player = Player([skill]);
        var threat = Threat(
            "RANGE_THREAT",
            TargetThreatEvidenceConfidence.VerifiedRule);
        var requirement = new RangeRequirement(
            minimumInclusive: 1,
            maximumInclusive: 5,
            CombatRequirementCriticality.Conditional,
            "evidence:range");
        var context = Context(
            distance: SnapshotValue<int>.Unavailable(
                "Current combat distance was not supplied."));
        var plan = Plan(
            player,
            [threat],
            context,
            Option(
                skill,
                threat.Code,
                requirements: [requirement]));

        var explanation = CombatRecommendationExplanationBuilder.Build(
            player,
            [threat],
            plan);

        Assert.Contains(
            explanation.UnavailableData,
            caveat => caveat.Code == "DAMAGE_EVIDENCE_UNAVAILABLE");
        Assert.Contains(
            explanation.UnavailableData,
            caveat => caveat.Code == "SKILL_NAME_UNAVAILABLE"
                && caveat.SkillId == skill.SkillId);
        Assert.Contains(
            explanation.UnavailableData,
            caveat => caveat.Code == "CONDITION_STATUS_UNAVAILABLE"
                && caveat.SkillId == skill.SkillId);
        Assert.Equal(
            CombatRequirementStatus.Unknown,
            Assert.Single(explanation.Skills).Conditions[0].Status);
    }

    [Fact]
    public void Missing_structured_threat_is_reported_without_invention()
    {
        var skill = Skill(100, SkillCategory.Attack);
        var player = Player([skill]);
        var plan = Plan(
            player,
            targetThreats: [],
            Context(),
            Option(skill, "MISSING_THREAT"));

        var explanation = CombatRecommendationExplanationBuilder.Build(
            player,
            targetThreats: [],
            plan);

        Assert.Empty(Assert.Single(explanation.Skills).Threats);
        Assert.Contains(
            explanation.UnavailableData,
            caveat => caveat.Code == "THREAT_DETAILS_UNAVAILABLE"
                && caveat.Explanation.Contains("MISSING_THREAT"));
    }

    [Fact]
    public void Compatibility_selection_states_that_counter_mapping_is_absent()
    {
        var skill = Skill(100, SkillCategory.Attack);
        var player = Player([skill], attack: [skill.SkillId]);
        var option = CombatLoadoutOption.RetainCurrentSkill(
            skill.SkillId,
            "snapshot:current-loadout");
        var plan = Plan(
            player,
            targetThreats: [],
            Context(),
            option);

        var explanation = CombatRecommendationExplanationBuilder.Build(
            player,
            targetThreats: [],
            plan);

        var selected = Assert.Single(explanation.Skills);
        Assert.False(selected.Counter.IsAvailable);
        Assert.Contains(
            "No verified counter mapping",
            selected.Counter.UnavailableReason);
        Assert.NotEmpty(selected.Reasons);
    }

    [Fact]
    public void Duplicate_threat_inputs_are_rejected()
    {
        var skill = Skill(100, SkillCategory.Attack);
        var player = Player([skill]);
        var threat = Threat(
            "DUPLICATE_THREAT",
            TargetThreatEvidenceConfidence.VerifiedRule);
        var plan = Plan(
            player,
            [threat],
            Context(),
            Option(skill, threat.Code));

        Assert.Throws<ArgumentException>(
            () => CombatRecommendationExplanationBuilder.Build(
                player,
                [threat, threat],
                plan));
    }

    [Fact]
    public void Explanation_builder_has_no_model_service_dependency()
    {
        var references = typeof(CombatRecommendationExplanationBuilder)
            .Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(
            references,
            name => name.Contains("OpenAI", StringComparison.OrdinalIgnoreCase)
                || name.Contains(
                    "SemanticKernel",
                    StringComparison.OrdinalIgnoreCase));
        Assert.Empty(
            typeof(CombatRecommendationExplanationBuilder)
                .GetConstructors());
    }

    private static ManualCombatPlan Plan(
        PlayerCombatSnapshot player,
        TargetThreat[] targetThreats,
        CombatRequirementContext context,
        params CombatLoadoutOption[] options)
    {
        var generation = CombatLoadoutGenerator.Generate(
            new CombatLoadoutGenerationRequest(
                player,
                options,
                context,
                player.GenericSlotAllocation));
        var expectedIds = options
            .Select(option => option.Candidate.SkillId)
            .Order()
            .ToArray();
        var candidate = generation.Candidates.Single(value =>
            value.SelectedOptions
                .Select(option => option.Candidate.SkillId)
                .Order()
                .SequenceEqual(expectedIds));
        var scoring = CombatRecommendationScorer.Score(
            new CombatRecommendationScoringRequest(
                player,
                targetThreats,
                [candidate],
                RecommendationPolicy.Balanced));
        return ManualCombatPlanBuilder.Build(player, scoring).Plan!;
    }

    private static CombatLoadoutOption Option(
        CombatSkillSnapshot skill,
        string threatCode,
        CombatCounterActivationTiming timing =
            CombatCounterActivationTiming.ActiveAttack,
        PracticeDirection? requiredDirection = null,
        params CombatRequirement[] requirements)
    {
        return new CombatLoadoutOption(
            new CombatSkillCandidate(
                skill.SkillId,
                requiredDirection: requiredDirection,
                allowDirectionChange: requiredDirection.HasValue),
            requirements,
            [threatCode],
            isCurrentlyEquipped: false,
            $"evidence:counter:{skill.SkillId}",
            CombatCounterStrength.Mitigation,
            timing,
            expectedEffectId: requiredDirection == PracticeDirection.Direct
                ? skill.DirectEffectId.Value
                : requiredDirection == PracticeDirection.Reverse
                    ? skill.ReverseEffectId.Value
                    : null);
    }

    private static TargetThreat Threat(
        string code,
        TargetThreatEvidenceConfidence confidence)
    {
        return new TargetThreat(
            code,
            TargetThreatKind.MindResonanceCascade,
            TargetThreatSeverity.Critical,
            $"Threat {code}",
            "Evidence-backed test threat.",
            TargetThreatActivationTiming.OnMarkApplied,
            [
                new TargetThreatEvidence(
                    $"evidence:threat:{code}",
                    "Test evidence.",
                    confidence)
            ]);
    }

    private static CombatSkillSnapshot Skill(
        int skillId,
        SkillCategory category,
        bool nameAvailable = true)
    {
        return new CombatSkillSnapshot(
            skillId,
            nameAvailable
                ? SnapshotValue<string>.Available($"Skill {skillId}")
                : SnapshotValue<string>.Unavailable(
                    "Localized skill name was not available."),
            category,
            SnapshotValue<int>.Available(2),
            SnapshotValue<bool>.Available(true),
            SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Direct),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(1000 + skillId),
            SnapshotValue<int>.Available(2000 + skillId));
    }

    private static PlayerCombatSnapshot Player(
        CombatSkillSnapshot[] skills,
        int[]? attack = null)
    {
        return new PlayerCombatSnapshot(
            characterId: 1,
            SnapshotValue<string>.Available("Taiwu"),
            skills,
            new CombatLoadoutSnapshot(
                neigongSkillIds: [],
                attack ?? [],
                agilitySkillIds: [],
                defenseSkillIds: [],
                assistanceSkillIds: []),
            equipment: [],
            new SlotBudgetSet(
            [
                new SlotBudget(SkillCategory.Neigong, 0, 6),
                new SlotBudget(SkillCategory.Attack, 0, 2),
                new SlotBudget(SkillCategory.Agility, 0, 2),
                new SlotBudget(SkillCategory.Defense, 0, 2),
                new SlotBudget(SkillCategory.Assistance, 0, 2)
            ]),
            new GenericSlotAllocation(0, 0, 0, 0, 0),
            legendaryBookCostSlots: [],
            legendaryBookCostAssignments: []);
    }

    private static CombatRequirementContext Context(
        int? weaponTypeId = null,
        SnapshotValue<int>? distance = null)
    {
        return new CombatRequirementContext(
            equippedWeaponTypeIds: weaponTypeId.HasValue
                ? [weaponTypeId.Value]
                : [],
            trickCounts: [],
            distance ?? SnapshotValue<int>.Available(0),
            resources: [],
            unlockedWeaponTypeIds: [],
            equippedSkillIds: []);
    }
}
