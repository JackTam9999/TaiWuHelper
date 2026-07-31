using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed class ManualRecommendationViewModelBuilderTests
{
    [Fact]
    public void Checklist_covers_changes_generic_weapon_and_resources()
    {
        var checklist = ManualSetupChecklistBuilder.Build(Style());

        Assert.Equal(
            Enum.GetValues<ManualChecklistItemKind>(),
            checklist.Select(item => item.Kind).Distinct().Order());
        Assert.All(
            checklist,
            item =>
            {
                Assert.False(string.IsNullOrWhiteSpace(item.Reference));
                Assert.False(string.IsNullOrWhiteSpace(item.Instruction));
                Assert.True(
                    item.ReasonReference is not null
                    || item.EvidenceReferences.Count > 0);
            });
        Assert.Contains(
            checklist,
            item => item.Kind == ManualChecklistItemKind.ConfirmResource
                    && item.Instruction.Contains(
                        "Neili",
                        StringComparison.Ordinal));
        Assert.Contains(
            checklist,
            item => item.Instruction.Contains(
                "金猊鎮魔刀",
                StringComparison.Ordinal));
        Assert.DoesNotContain(
            checklist,
            item => item.SubjectName == "老君拂塵功");
        Assert.All(
            checklist,
            item => Assert.DoesNotMatch(
                @"\bskill \d+\b",
                item.Instruction));
    }

    [Fact]
    public void Battle_plan_populates_all_supported_phases()
    {
        var phases = BattlePlanViewModelBuilder.Build(Style());

        Assert.Equal(
            Enum.GetValues<BattlePlanPhaseKind>(),
            phases.Select(phase => phase.Phase));
        Assert.All(phases, phase => Assert.NotEmpty(phase.Items));
        Assert.All(
            phases.SelectMany(phase => phase.Items),
            item =>
            {
                Assert.False(string.IsNullOrWhiteSpace(item.Reference));
                Assert.False(string.IsNullOrWhiteSpace(item.Instruction));
                Assert.True(
                    item.ReasonReference is not null
                    || item.EvidenceReferences.Count > 0);
            });
        var instructions = phases
            .SelectMany(phase => phase.Items)
            .Select(item => item.Instruction)
            .ToArray();
        Assert.Contains(
            instructions,
            instruction => instruction.Contains(
                "老君拂塵功",
                StringComparison.Ordinal));
        Assert.Contains(
            instructions,
            instruction => instruction.Contains(
                "金猊鎮魔刀",
                StringComparison.Ordinal));
        Assert.Equal(instructions.Length, instructions.Distinct().Count());
    }

    [Fact]
    public void Checklist_and_plan_references_are_deterministic()
    {
        var style = Style();

        var firstChecklist = ManualSetupChecklistBuilder.Build(style);
        var secondChecklist = ManualSetupChecklistBuilder.Build(style);
        var firstPlan = BattlePlanViewModelBuilder.Build(style);
        var secondPlan = BattlePlanViewModelBuilder.Build(style);

        Assert.Equal(
            firstChecklist.Select(item => item.Reference),
            secondChecklist.Select(item => item.Reference));
        Assert.Equal(
            firstPlan.SelectMany(phase => phase.Items)
                .Select(item => item.Reference),
            secondPlan.SelectMany(phase => phase.Items)
                .Select(item => item.Reference));
    }

    private static RecommendationStyleViewModel Style()
    {
        const string candidateReference = "candidate:test";
        var reason = new RecommendationReasonViewModel(
            $"{candidateReference}:skill:604:reason:COUNTER",
            "COUNTER",
            "Counters the target mechanic.",
            ["evidence:counter"],
            ["threat:MAGIC_SOUND"]);
        var attack = Skill(
            candidateReference,
            604,
            SkillCategory.Attack,
            CombatCounterActivationTiming.ActiveAttack,
            reason,
            [
                new SkillConditionViewModel(
                    $"{candidateReference}:skill:604:condition:Weapon:1",
                    RecommendationConditionKind.Weapon,
                    CombatRequirementCriticality.Hard,
                    CombatRequirementStatus.Satisfied,
                    "Equip the required blade.",
                    "evidence:weapon"),
                new SkillConditionViewModel(
                    $"{candidateReference}:skill:604:condition:Resource:2",
                    RecommendationConditionKind.Resource,
                    CombatRequirementCriticality.Hard,
                    CombatRequirementStatus.Satisfied,
                    "Neili must be at least 10.",
                    "evidence:neili")
            ]);
        var defense = Skill(
            candidateReference,
            500,
            SkillCategory.Defense,
            CombatCounterActivationTiming.ActiveDefense,
            reason,
            conditions: []);

        return new RecommendationStyleViewModel(
            "snapshot:test:style:Safe",
            "snapshot:test",
            RecommendationPolicy.Safe,
            IsInitiallySelected: true,
            HasRecommendation: true,
            candidateReference,
            TotalScore: 100,
            Scores: [],
            Categories:
            [
                new LoadoutCategoryViewModel(
                    $"{candidateReference}:category:Attack",
                    SkillCategory.Attack,
                    "摧破",
                    UsedSlots: 1,
                    UsedSlotsUnavailableReason: null,
                    Capacity: 3,
                    RemainingSlots: 2,
                    RemainingSlotsUnavailableReason: null,
                    GenericSlots: 1,
                    [attack]),
                new LoadoutCategoryViewModel(
                    $"{candidateReference}:category:Defense",
                    SkillCategory.Defense,
                    "護體",
                    UsedSlots: 1,
                    UsedSlotsUnavailableReason: null,
                    Capacity: 2,
                    RemainingSlots: 1,
                    RemainingSlotsUnavailableReason: null,
                    GenericSlots: 0,
                    [defense])
            ],
            ManualChanges:
            [
                Change(
                    candidateReference,
                    ManualLoadoutChangeKind.Remove,
                    999,
                    reason),
                Change(
                    candidateReference,
                    ManualLoadoutChangeKind.Add,
                    604,
                    reason),
                Change(
                    candidateReference,
                    ManualLoadoutChangeKind.Retain,
                    686,
                    reason),
                Change(
                    candidateReference,
                    ManualLoadoutChangeKind.ChangeDirection,
                    604,
                    reason,
                    PracticeDirection.Reverse),
                Change(
                    candidateReference,
                    ManualLoadoutChangeKind.CompleteBreakthrough,
                    500,
                    reason,
                    PracticeDirection.Reverse)
            ],
            OpeningActions:
            [
                Step(
                    candidateReference,
                    1,
                    BattlePlanInstructionKind.ConfirmEquipped,
                    686,
                    "Confirm passive skill 686.",
                    reason),
                Step(
                    candidateReference,
                    2,
                    BattlePlanInstructionKind.ActivateSkill,
                    604,
                    "Open with skill 604.",
                    reason)
            ],
            SwitchingConditions:
            [
                Step(
                    candidateReference,
                    3,
                    BattlePlanInstructionKind.SwitchBeforeCombat,
                    500,
                    "Switch defense when pressure rises.",
                    reason)
            ],
            Caveats: [],
            Diagnostic: null);
    }

    private static RecommendedSkillViewModel Skill(
        string candidateReference,
        int skillId,
        SkillCategory category,
        CombatCounterActivationTiming timing,
        RecommendationReasonViewModel reason,
        IReadOnlyList<SkillConditionViewModel> conditions)
    {
        return new RecommendedSkillViewModel(
            $"{candidateReference}:skill:{skillId}",
            skillId,
            TestSkillName(skillId),
            category,
            PracticeDirection.Reverse,
            PracticeDirection.Reverse,
            RequiresManualDirectionChange: false,
            new SkillCostViewModel(
                ActualCost: 1,
                ActualCostUnavailableReason: null,
                EffectiveCost: 1,
                EffectiveCostUnavailableReason: null,
                MasteryReduction: 0,
                LegendaryBookReduction: 0,
                ["evidence:cost"]),
            new SkillCounterViewModel(
                IsAvailable: true,
                CombatCounterStrength.Mitigation,
                timing,
                "evidence:counter",
                UnavailableReason: null),
            ["threat:MAGIC_SOUND"],
            conditions,
            [reason]);
    }

    private static ManualLoadoutChangeViewModel Change(
        string candidateReference,
        ManualLoadoutChangeKind kind,
        int skillId,
        RecommendationReasonViewModel reason,
        PracticeDirection? direction = null)
    {
        return new ManualLoadoutChangeViewModel(
            $"{candidateReference}:change:{kind}:Attack:{skillId}",
            kind,
            SkillCategory.Attack,
            skillId,
            TestSkillName(skillId),
            direction,
            reason);
    }

    private static BattlePlanStepViewModel Step(
        string candidateReference,
        int sequence,
        BattlePlanInstructionKind kind,
        int skillId,
        string condition,
        RecommendationReasonViewModel reason)
    {
        return new BattlePlanStepViewModel(
            $"{candidateReference}:plan:{sequence}",
            kind,
            skillId,
            TestSkillName(skillId),
            AlternativeSkillId: null,
            AlternativeSkillName: null,
            condition,
            reason);
    }

    private static string TestSkillName(int skillId) => skillId switch
    {
        500 => "曼荼羅真言",
        604 => "金猊鎮魔刀",
        686 => "老君拂塵功",
        999 => "舊功法",
        _ => "測試功法"
    };
}
