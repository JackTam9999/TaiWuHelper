using NSubstitute;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.LoadoutComparisons;
using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed class CombatRecommendationViewModelMapperTests
{
    private const string SavePath = "local.sav";

    [Fact]
    public async Task Maps_all_styles_from_one_snapshot_and_selects_requested()
    {
        var recommendation = await RecommendAsync(
            RecommendationPolicy.Aggressive);

        var model = CombatRecommendationViewModelMapper.Map(recommendation);

        Assert.Equal(3, model.Styles.Count);
        var selected = Assert.Single(
            model.Styles,
            style => style.IsInitiallySelected);
        Assert.Equal(RecommendationPolicy.Aggressive, selected.Style);
        Assert.Equal(
            model.InitiallySelectedStyleReference,
            selected.Reference);
        Assert.All(
            model.Styles,
            style => Assert.Equal(
                model.SnapshotReference,
                style.SnapshotReference));
        Assert.Contains(
            "cannot apply",
            model.InformationOnlyNotice,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Maps_capacity_cost_direction_conditions_and_evidence()
    {
        var recommendation = await RecommendAsync(
            RecommendationPolicy.Safe);

        var model = CombatRecommendationViewModelMapper.Map(recommendation);
        var style = model.Styles.Single(
            value => value.Style == RecommendationPolicy.Safe);
        Assert.True(style.HasRecommendation);
        Assert.Equal(5, style.Categories.Count);

        var attack = style.Categories.Single(
            category => category.Category == SkillCategory.Attack);
        Assert.Equal("摧破", attack.DisplayName);
        Assert.Equal(3, attack.Capacity);
        Assert.Equal(1, attack.GenericSlots);
        var jinni = Assert.Single(
            attack.Skills,
            skill => skill.SkillId == 604);
        Assert.Equal("金猊鎮魔刀", jinni.Name);
        Assert.Equal(PracticeDirection.Reverse, jinni.CurrentDirection);
        Assert.Equal(PracticeDirection.Reverse, jinni.RequiredDirection);
        Assert.False(jinni.RequiresManualDirectionChange);
        Assert.Equal(1, jinni.Cost.ActualCost);
        Assert.Equal(1, jinni.Cost.EffectiveCost);
        Assert.Equal(
            CombatCounterActivationTiming.ActiveAttack,
            jinni.Counter.ActivationTiming);
        Assert.NotEmpty(jinni.ThreatReferences);
        Assert.NotEmpty(jinni.Reasons);
        Assert.All(
            jinni.Reasons,
            reason =>
            {
                Assert.StartsWith(jinni.Reference, reason.Reference);
                Assert.NotEmpty(reason.EvidenceReferences);
                Assert.NotEmpty(reason.ThreatReferences);
            });

        var condition = Assert.Single(
            style.Categories
                .SelectMany(category => category.Skills)
                .SelectMany(skill => skill.Conditions));
        Assert.Equal(
            RecommendationConditionKind.SkillActivation,
            condition.Kind);
        Assert.Equal(CombatRequirementStatus.Satisfied, condition.Status);
        Assert.False(string.IsNullOrWhiteSpace(condition.EvidenceReference));
        Assert.NotEmpty(model.Warnings);
        Assert.Contains(
            style.ManualChanges,
            change => change.SkillId == 604
                && change.SkillName == "金猊鎮魔刀");
        Assert.Contains(
            style.OpeningActions,
            step => step.SkillName is "金猊鎮魔刀" or "老君拂塵功");
        Assert.All(
            model.Warnings,
            warning => Assert.True(warning.Occurrences >= 1));
        Assert.Equal(
            model.Warnings.Count,
            model.Warnings
                .Select(warning => (
                    warning.Source,
                    warning.Code,
                    warning.Message))
                .Distinct()
                .Count());
    }

    [Fact]
    public async Task Maps_four_column_comparison_from_the_same_snapshot()
    {
        var recommendation = await RecommendAsync(
            RecommendationPolicy.Balanced);

        var model = CombatRecommendationViewModelMapper.Map(recommendation);
        var comparison = Assert.IsType<LoadoutComparisonViewModel>(
            model.Comparison);

        Assert.Equal(model.SnapshotReference, comparison.SnapshotReference);
        Assert.StartsWith("comparison:", comparison.Reference);
        Assert.Equal(
            [
                LoadoutComparisonColumnKind.Current,
                LoadoutComparisonColumnKind.Safe,
                LoadoutComparisonColumnKind.Balanced,
                LoadoutComparisonColumnKind.Aggressive
            ],
            comparison.Columns.Select(column => column.Kind));
        Assert.Equal(
            Enum.GetValues<SkillCategory>(),
            comparison.Categories.Select(category => category.Category));
        Assert.All(
            comparison.Categories,
            category => Assert.Contains(
                category.Capacities,
                capacity => capacity.Column
                    == LoadoutComparisonColumnKind.Current));
        Assert.Contains(
            comparison.BaselineProvenance,
            value => value.Field
                == LoadoutComparisonBaselineField.EquippedSkills
                && value.Source == SnapshotDataSource.Save);
        Assert.Contains(
            comparison.Categories.SelectMany(category => category.Skills),
            skill => skill.Cells.Any(cell => cell.Membership is
                LoadoutComparisonMembership.Added
                or LoadoutComparisonMembership.Removed));
        var policyColumns = comparison.Columns
            .Where(column => column.Policy.HasValue)
            .ToArray();
        Assert.Equal(3, policyColumns.Length);
        Assert.All(
            policyColumns,
            column =>
            {
                var tactical = Assert.IsType<
                    LoadoutComparisonTacticalViewModel>(column.Tactical);
                Assert.Equal(column.Policy, tactical.Policy);
                Assert.Equal(
                    model.Threats.Select(threat => threat.Code)
                        .Order(StringComparer.Ordinal),
                    tactical.CoveredThreats
                        .Concat(tactical.UnresolvedThreats)
                        .Select(threat => threat.Code)
                        .Order(StringComparer.Ordinal));
                Assert.NotEmpty(tactical.Scores);
                Assert.All(
                    tactical.Scores,
                    score =>
                    {
                        Assert.True(score.Weight >= 0);
                        Assert.False(string.IsNullOrWhiteSpace(
                            score.Explanation));
                        Assert.False(string.IsNullOrWhiteSpace(
                            score.EvidenceReference));
                    });
                Assert.NotEmpty(tactical.EvidenceReferences);
            });
        Assert.Contains(
            "cannot equip",
            comparison.InformationOnlyNotice,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Stable_references_repeat_without_execution_operations()
    {
        var recommendation = await RecommendAsync(
            RecommendationPolicy.Balanced);

        var first = CombatRecommendationViewModelMapper.Map(recommendation);
        var second = CombatRecommendationViewModelMapper.Map(recommendation);

        Assert.Equal(References(first), References(second));
        Assert.All(
            first.Styles.SelectMany(style => style.ManualChanges),
            change => Assert.False(string.IsNullOrWhiteSpace(change.Reference)));
        Assert.All(
            first.Styles.SelectMany(style =>
                style.OpeningActions.Concat(style.SwitchingConditions)),
            step => Assert.False(string.IsNullOrWhiteSpace(step.Reference)));
    }

    [Fact]
    public async Task Supporting_details_include_alternatives_scores_and_evidence()
    {
        var recommendation = await RecommendAsync(
            RecommendationPolicy.Safe);
        var model = CombatRecommendationViewModelMapper.Map(recommendation);
        var selected = model.Styles.Single(
            style => style.Style == RecommendationPolicy.Safe);

        var details = RecommendationSupportingDetailsBuilder.Build(
            model,
            selected);

        var alternative = Assert.Single(details.Alternatives);
        Assert.Equal(RecommendationPolicy.Aggressive, alternative.Style);
        Assert.DoesNotContain(
            details.Alternatives,
            value => value.Style == RecommendationPolicy.Balanced);
        Assert.NotEmpty(details.Scores);
        Assert.NotEmpty(details.EvidenceReferences);
        Assert.Contains("never replaces", details.UnknownValuePolicy);
        Assert.Equal(
            details.EvidenceReferences.Count,
            details.EvidenceReferences
                .Distinct(StringComparer.Ordinal)
                .Count());
    }

    private static async Task<CombatLoadoutRecommendation> RecommendAsync(
        RecommendationPolicy policy)
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(GoldenSnapshot());
        var useCase = new RecommendCombatLoadout(reader);

        return await useCase.ExecuteAsync(
            new RecommendCombatLoadoutRequest(
                SavePath,
                16317,
                policy),
            TestContext.Current.CancellationToken);
    }

    private static string[] References(CombatRecommendationViewModel model)
    {
        return
        [
            model.SnapshotReference,
            model.InitiallySelectedStyleReference,
            .. model.Threats.Select(threat => threat.Reference),
            .. model.Styles.Select(style => style.Reference),
            .. model.Styles
                .SelectMany(style => style.Categories)
                .Select(category => category.Reference),
            .. model.Styles
                .SelectMany(style => style.Categories)
                .SelectMany(category => category.Skills)
                .Select(skill => skill.Reference),
            .. model.Styles
                .SelectMany(style => style.Categories)
                .SelectMany(category => category.Skills)
                .SelectMany(skill => skill.Conditions)
                .Select(condition => condition.Reference),
            .. model.Styles
                .SelectMany(style => style.Categories)
                .SelectMany(category => category.Skills)
                .SelectMany(skill => skill.Reasons)
                .Select(reason => reason.Reference),
            .. model.Styles
                .SelectMany(style => style.Scores)
                .Select(score => score.Reference),
            .. model.Styles
                .SelectMany(style => style.ManualChanges)
                .Select(change => change.Reference),
            .. model.Styles
                .SelectMany(style =>
                    style.OpeningActions.Concat(style.SwitchingConditions))
                .Select(step => step.Reference),
            .. model.Styles
                .SelectMany(style => style.Caveats)
                .Select(caveat => caveat.Reference),
            .. model.Warnings.Select(warning => warning.Reference)
        ];
    }

    private static CombatSnapshot GoldenSnapshot()
    {
        var jinni = Skill(
            604,
            SkillCategory.Attack,
            PracticeDirection.Reverse,
            directEffectId: 338,
            reverseEffectId: 1064);
        var laojun = Skill(
            686,
            SkillCategory.Assistance,
            PracticeDirection.Reverse,
            directEffectId: 696,
            reverseEffectId: 1422);
        var targetSkill = Skill(
            719,
            SkillCategory.Attack,
            PracticeDirection.Direct,
            directEffectId: 669,
            reverseEffectId: 1669);
        var resetSkill = Skill(
            287,
            SkillCategory.Assistance,
            PracticeDirection.Reverse,
            directEffectId: 185,
            reverseEffectId: 911);

        return new CombatSnapshot(
            new CombatSnapshotMetadata(
                SavePath,
                new string('A', 64),
                DateTimeOffset.Parse("2026-07-30T12:00:00Z"),
                SnapshotValue<DateTimeOffset>.Available(
                    DateTimeOffset.Parse("2026-07-30T11:00:00Z")),
                SnapshotValue<string>.Available(
                    VerifiedCombatEffectCatalogs.GoldenGameDataVersion)),
            new PlayerCombatSnapshot(
                characterId: 21396,
                SnapshotValue<string>.Available("Taiwu"),
                [jinni, laojun],
                new CombatLoadoutSnapshot(
                    neigongSkillIds: [],
                    attackSkillIds: [],
                    agilitySkillIds: [],
                    defenseSkillIds: [],
                    assistanceSkillIds: [laojun.SkillId]),
                equipment: [],
                new SlotBudgetSet(
                [
                    new SlotBudget(SkillCategory.Neigong, 0, 6),
                    new SlotBudget(SkillCategory.Attack, 0, 3),
                    new SlotBudget(SkillCategory.Agility, 0, 2),
                    new SlotBudget(SkillCategory.Defense, 0, 2),
                    new SlotBudget(SkillCategory.Assistance, 1, 2)
                ]),
                new GenericSlotAllocation(1, 1, 0, 0, 0),
                legendaryBookCostSlots: [],
                legendaryBookCostAssignments: []),
            new TargetCombatSnapshot(
                characterId: 16317,
                SnapshotValue<string>.Available("Target"),
                SnapshotValue<int>.Available(52),
                features: [],
                [targetSkill, resetSkill],
                SnapshotValue<CombatLoadoutSnapshot>.Available(
                    new CombatLoadoutSnapshot(
                        neigongSkillIds: [],
                        attackSkillIds: [targetSkill.SkillId],
                        agilitySkillIds: [],
                        defenseSkillIds: [],
                        assistanceSkillIds: [resetSkill.SkillId])),
                equipment: []),
            [
                new SnapshotWarning(
                    "SOURCE_WARNING",
                    "Preserved source warning.")
            ]);
    }

    private static CombatSkillSnapshot Skill(
        int skillId,
        SkillCategory category,
        PracticeDirection direction,
        int directEffectId,
        int reverseEffectId)
    {
        return new CombatSkillSnapshot(
            skillId,
            SnapshotValue<string>.Available(SkillName(skillId)),
            category,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(true),
            SnapshotValue<PracticeDirection>.Available(direction),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(directEffectId),
            SnapshotValue<int>.Available(reverseEffectId));
    }

    private static string SkillName(int skillId) => skillId switch
    {
        604 => "金猊鎮魔刀",
        686 => "老君拂塵功",
        719 => "測試目標功法",
        _ => "未命名測試功法"
    };
}
