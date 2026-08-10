using System.Reflection;
using NSubstitute;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.LoadoutComparisons;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.LoadoutComparisons;
using TaiWu.Domain.CombatThreats;
using Xunit;

namespace TaiWu.Application.UnitTests.LoadoutComparisons;

public sealed class CombatLoadoutComparisonBuilderTests
{
    [Fact]
    public async Task Builds_current_and_all_policy_columns_from_one_result()
    {
        var recommendation = await Recommend(GoldenSnapshot(
            counterDirection: PracticeDirection.Reverse));

        var comparison = CombatLoadoutComparisonBuilder.Build(recommendation);

        Assert.Collection(
            comparison.Columns,
            column => Assert.Equal(
                LoadoutComparisonColumnKind.Current,
                column.Kind),
            column => Assert.Equal(
                LoadoutComparisonColumnKind.Safe,
                column.Kind),
            column => Assert.Equal(
                LoadoutComparisonColumnKind.Balanced,
                column.Kind),
            column => Assert.Equal(
                LoadoutComparisonColumnKind.Aggressive,
                column.Kind));
        Assert.All(
            comparison.Columns.Skip(1),
            column => Assert.Equal(
                LoadoutComparisonColumnStatus.Available,
                column.Status));
        Assert.Equal(
            recommendation.Snapshot.Target.CharacterId.ToString(),
            comparison.TargetReference.Value.Split(':')[1]);
    }

    [Fact]
    public void Preserves_add_plus_direction_change_as_a_composite()
    {
        var recommendation = DirectionChangeRecommendation();

        var comparison = CombatLoadoutComparisonBuilder.Build(recommendation);

        var safe = comparison.GetPolicy(RecommendationPolicy.Safe)!;
        var cell = Skill(safe, SkillCategory.Attack, 604);
        Assert.Equal(
            LoadoutComparisonMembership.Added,
            cell.Membership.Value);
        var action = Assert.Single(cell.Actions);
        Assert.Equal(
            LoadoutComparisonSkillActionKind.DirectionChangeRequired,
            action.Kind);
        Assert.Equal(PracticeDirection.Reverse, action.RequiredDirection);
        Assert.NotEmpty(action.Reason.EvidenceReferences);
    }

    [Fact]
    public async Task Preserves_add_plus_breakthrough_as_a_composite()
    {
        var counter = UnbrokenSkill(
            686,
            SkillCategory.Assistance,
            [PracticeDirection.Reverse],
            directEffectId: 422,
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
        var recommendation = await Recommend(Snapshot(
            [counter],
            [targetSkill, resetSkill],
            new CombatLoadoutSnapshot([], [], [], [], []),
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                new CombatLoadoutSnapshot(
                    [],
                    [targetSkill.SkillId],
                    [],
                    [],
                    [resetSkill.SkillId]))));

        var comparison = CombatLoadoutComparisonBuilder.Build(recommendation);

        var cell = Skill(
            comparison.GetPolicy(RecommendationPolicy.Safe)!,
            SkillCategory.Assistance,
            counter.SkillId);
        Assert.Equal(
            LoadoutComparisonMembership.Added,
            cell.Membership.Value);
        var action = Assert.Single(cell.Actions);
        Assert.Equal(
            LoadoutComparisonSkillActionKind.BreakthroughRequired,
            action.Kind);
        Assert.Equal(PracticeDirection.Reverse, action.RequiredDirection);
    }

    [Fact]
    public async Task Manual_changes_and_comparison_actions_have_exact_parity()
    {
        var current = Skill(
            900,
            SkillCategory.Attack,
            PracticeDirection.Direct,
            directEffectId: 1900,
            reverseEffectId: 2900);
        var counter = Skill(
            604,
            SkillCategory.Attack,
            PracticeDirection.Direct,
            directEffectId: 338,
            reverseEffectId: 1064);
        var target = Skill(
            719,
            SkillCategory.Attack,
            PracticeDirection.Direct,
            directEffectId: 669,
            reverseEffectId: 1669);
        var recommendation = await Recommend(Snapshot(
            [current, counter],
            [target],
            new CombatLoadoutSnapshot([], [current.SkillId], [], [], []),
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                new CombatLoadoutSnapshot(
                    [],
                    [target.SkillId],
                    [],
                    [],
                    [])),
            budgets: Budgets(attackUsed: 1, attackCapacity: 1)));

        var comparison = CombatLoadoutComparisonBuilder.Build(recommendation);

        foreach (var style in recommendation.Styles)
        {
            var plan = Assert.IsType<ManualCombatPlan>(style.ManualPlan.Plan);
            var column = comparison.GetPolicy(style.Policy)!;
            var cells = column.Loadout!.Categories
                .SelectMany(category => category.Skills)
                .ToDictionary(cell => (
                    cell.Identity.Category,
                    cell.Identity.SkillId));
            foreach (var change in plan.LoadoutChanges)
            {
                var cell = cells[(change.Category, change.SkillId)];
                if (change.Kind is ManualLoadoutChangeKind.Add
                    or ManualLoadoutChangeKind.Remove
                    or ManualLoadoutChangeKind.Retain)
                {
                    Assert.Equal(Membership(change.Kind), cell.Membership.Value);
                }
                else
                {
                    Assert.Contains(
                        cell.Actions,
                        action => action.Kind == ActionKind(change.Kind)
                            && action.RequiredDirection
                                == change.RequiredDirection);
                }
            }

            Assert.Equal(
                plan.LoadoutChanges.Length,
                cells.Values.Sum(cell => 1 + cell.Actions.Length));
        }
    }

    [Fact]
    public async Task Carries_validated_capacity_and_reallocated_generic_slots()
    {
        var inner = Skill(
            901,
            SkillCategory.Neigong,
            PracticeDirection.Direct,
            directEffectId: 1901,
            reverseEffectId: 2901,
            slotContribution: new SkillSlotContribution(0, 0, 0, 0, 1));
        var counter = Skill(
            604,
            SkillCategory.Attack,
            PracticeDirection.Reverse,
            directEffectId: 338,
            reverseEffectId: 1064,
            gridCost: SnapshotValue<int>.Available(3),
            mastered: false);
        var target = Skill(
            719,
            SkillCategory.Attack,
            PracticeDirection.Direct,
            directEffectId: 669,
            reverseEffectId: 1669);
        var reset = Skill(
            287,
            SkillCategory.Assistance,
            PracticeDirection.Reverse,
            directEffectId: 185,
            reverseEffectId: 911);
        var recommendation = await Recommend(Snapshot(
            [inner, counter],
            [target, reset],
            new CombatLoadoutSnapshot([inner.SkillId], [], [], [], []),
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                new CombatLoadoutSnapshot(
                    [],
                    [target.SkillId],
                    [],
                    [],
                    [reset.SkillId])),
            budgets: new SlotBudgetSet(
            [
                new SlotBudget(SkillCategory.Neigong, 1, 6),
                new SlotBudget(SkillCategory.Attack, 0, 2),
                new SlotBudget(SkillCategory.Agility, 0, 2),
                new SlotBudget(SkillCategory.Defense, 0, 3),
                new SlotBudget(SkillCategory.Assistance, 0, 2)
            ]),
            genericSlots: new GenericSlotAllocation(1, 0, 0, 1, 0)));

        var comparison = CombatLoadoutComparisonBuilder.Build(recommendation);

        var current = comparison.Current.Loadout!;
        var safe = comparison.GetPolicy(RecommendationPolicy.Safe)!.Loadout!;
        Assert.Equal(1, current.GenericSlotAllocation.Value.Defense);
        Assert.Equal(0, current.GenericSlotAllocation.Value.Attack);
        Assert.Equal(1, safe.GenericSlotAllocation.Value.Attack);
        Assert.Equal(0, safe.GenericSlotAllocation.Value.Defense);
        var attack = safe.Categories.Single(row =>
            row.Category == SkillCategory.Attack);
        Assert.Equal(3, attack.Capacity.Capacity.Value);
        Assert.Equal(3, attack.Capacity.Used.Value);
        Assert.Equal(1, attack.Capacity.GenericContribution.Value);
    }

    [Fact]
    public async Task Infeasible_styles_remain_diagnostic_columns()
    {
        var recommendation = await Recommend(Snapshot(
            playerSkills: [],
            targetSkills: [],
            new CombatLoadoutSnapshot([], [], [], [], []),
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                new CombatLoadoutSnapshot([], [], [], [], []))));

        var comparison = CombatLoadoutComparisonBuilder.Build(recommendation);

        Assert.All(
            comparison.Columns.Skip(1),
            column =>
            {
                Assert.Equal(
                    LoadoutComparisonColumnStatus.Infeasible,
                    column.Status);
                Assert.Null(column.Loadout);
                Assert.NotNull(column.Diagnostic);
                Assert.False(string.IsNullOrWhiteSpace(
                    column.Diagnostic!.Summary));
            });
    }

    [Fact]
    public async Task All_retained_loadout_stays_unchanged_in_every_policy()
    {
        var current = Skill(
            900,
            SkillCategory.Attack,
            PracticeDirection.Direct,
            directEffectId: 1900,
            reverseEffectId: 2900);
        var recommendation = await Recommend(Snapshot(
            [current],
            targetSkills: [],
            new CombatLoadoutSnapshot([], [current.SkillId], [], [], []),
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                new CombatLoadoutSnapshot([], [], [], [], [])),
            budgets: Budgets(attackUsed: 1, attackCapacity: 2)));

        var comparison = CombatLoadoutComparisonBuilder.Build(recommendation);

        Assert.All(
            comparison.Columns.Skip(1),
            column =>
            {
                Assert.Equal(
                    LoadoutComparisonColumnStatus.Available,
                    column.Status);
                var cell = Skill(
                    column,
                    SkillCategory.Attack,
                    current.SkillId);
                Assert.Equal(
                    LoadoutComparisonMembership.Retained,
                    cell.Membership.Value);
                Assert.Empty(cell.Actions);
            });
    }

    [Fact]
    public async Task Tactical_summary_retains_policy_local_facts()
    {
        var recommendation = await Recommend(GoldenSnapshot(
            counterDirection: PracticeDirection.Reverse));

        var comparison = CombatLoadoutComparisonBuilder.Build(recommendation);

        foreach (var style in recommendation.Styles)
        {
            var summary = comparison.GetPolicy(style.Policy)!.TacticalSummary!;
            Assert.Equal(
                style.Scoring.RankedCandidates[0].Components.Length,
                summary.ScoreComponents.Length);
            Assert.Equal(
                style.Scoring.RankedCandidates[0].Components
                    .OrderBy(component => component.Kind)
                    .Select(component => (component.Kind, component.Weight)),
                summary.ScoreComponents.Select(component =>
                    (component.Kind, component.Weight)));
            Assert.Equal(
                style.ManualPlan.Plan!.SelectedRecommendation.Candidate
                    .ThreatCodes,
                summary.CoveredThreats.Select(value => value.Value));
            Assert.Equal(
                style.ManualPlan.Plan.LoadoutChanges.Count(change =>
                    change.Kind != ManualLoadoutChangeKind.Retain),
                summary.ManualActionCount.Value);
        }
    }

    [Fact]
    public async Task Current_unavailable_cost_and_slot_usage_keep_reasons()
    {
        var current = Skill(
            900,
            SkillCategory.Attack,
            PracticeDirection.Direct,
            directEffectId: 1900,
            reverseEffectId: 2900,
            gridCost: SnapshotValue<int>.Unavailable(
                "Grid cost was not captured."));
        var recommendation = await Recommend(Snapshot(
            [current],
            targetSkills: [],
            new CombatLoadoutSnapshot([], [current.SkillId], [], [], []),
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                new CombatLoadoutSnapshot([], [], [], [], [])),
            budgets: Budgets(
                attackUsed: null,
                attackCapacity: 2,
                unavailableReason: "Used slots are unavailable.")));

        var comparison = CombatLoadoutComparisonBuilder.Build(recommendation);

        var cell = Skill(
            comparison.Current,
            SkillCategory.Attack,
            current.SkillId);
        Assert.False(cell.EffectiveCost.IsAvailable);
        Assert.Contains(
            "GridCost",
            cell.EffectiveCost.UnavailableReason);
        var attack = comparison.Current.Loadout!.Categories.Single(row =>
            row.Category == SkillCategory.Attack);
        Assert.False(attack.Capacity.Used.IsAvailable);
        Assert.Equal(
            "Used slots are unavailable.",
            attack.Capacity.Used.UnavailableReason);
        Assert.False(attack.Capacity.Remaining.IsAvailable);
    }

    [Fact]
    public async Task Current_provenance_distinguishes_observed_and_save_fields()
    {
        var observedAt = DateTimeOffset.Parse("2026-08-08T12:30:00Z");
        SnapshotFieldSource[] sources =
        [
            new SnapshotFieldSource(
                CombatSnapshotObservationMerger.PlayerEquippedSkillsField,
                SnapshotDataSource.CurrentScreenObservation,
                observedAt,
                "observation:current"),
            new SnapshotFieldSource(
                CombatSnapshotObservationMerger
                    .PlayerGenericSlotAllocationField,
                SnapshotDataSource.CurrentScreenObservation,
                observedAt,
                "observation:current")
        ];
        var recommendation = await Recommend(Snapshot(
            playerSkills: [],
            targetSkills: [],
            new CombatLoadoutSnapshot([], [], [], [], []),
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                new CombatLoadoutSnapshot([], [], [], [], [])),
            fieldSources: sources));

        var comparison = CombatLoadoutComparisonBuilder.Build(recommendation);

        Assert.Equal(
            SnapshotDataSource.CurrentScreenObservation,
            comparison.BaselineProvenance.Single(value => value.Field
                == LoadoutComparisonBaselineField.EquippedSkills).Source);
        Assert.Equal(
            SnapshotDataSource.CurrentScreenObservation,
            comparison.BaselineProvenance.Single(value => value.Field
                == LoadoutComparisonBaselineField.GenericSlotAllocation)
                .Source);
        Assert.Equal(
            SnapshotDataSource.Save,
            comparison.BaselineProvenance.Single(value => value.Field
                == LoadoutComparisonBaselineField.SlotBudgets).Source);
    }

    [Fact]
    public async Task Identical_input_produces_identical_references_and_order()
    {
        var recommendation = await Recommend(GoldenSnapshot(
            counterDirection: PracticeDirection.Reverse));

        var first = CombatLoadoutComparisonBuilder.Build(recommendation);
        var second = CombatLoadoutComparisonBuilder.Build(recommendation);

        Assert.Equal(first.ComparisonReference, second.ComparisonReference);
        Assert.Equal(first.SnapshotReference, second.SnapshotReference);
        Assert.Equal(Projection(first), Projection(second));
    }

    [Fact]
    public async Task Stable_skill_order_does_not_follow_snapshot_input_order()
    {
        var high = Skill(
            902,
            SkillCategory.Attack,
            PracticeDirection.Direct,
            directEffectId: 1902,
            reverseEffectId: 2902);
        var low = Skill(
            901,
            SkillCategory.Attack,
            PracticeDirection.Direct,
            directEffectId: 1901,
            reverseEffectId: 2901);
        var recommendation = await Recommend(Snapshot(
            [high, low],
            targetSkills: [],
            new CombatLoadoutSnapshot(
                [],
                [high.SkillId, low.SkillId],
                [],
                [],
                []),
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                new CombatLoadoutSnapshot([], [], [], [], [])),
            budgets: Budgets(attackUsed: 2, attackCapacity: 2)));

        var comparison = CombatLoadoutComparisonBuilder.Build(recommendation);

        Assert.Equal(
            [low.SkillId, high.SkillId],
            comparison.Current.Loadout!.Categories
                .Single(row => row.Category == SkillCategory.Attack)
                .Skills.Select(cell => cell.Identity.SkillId));
    }

    [Fact]
    public async Task Public_evidence_references_never_expose_source_paths()
    {
        var recommendation = await Recommend(GoldenSnapshot(
            counterDirection: PracticeDirection.Reverse));

        var comparison = CombatLoadoutComparisonBuilder.Build(recommendation);

        var references = comparison.Columns
            .Where(column => column.TacticalSummary is not null)
            .SelectMany(column => column.TacticalSummary!.EvidenceReferences);
        Assert.NotEmpty(references);
        Assert.All(
            references,
            reference =>
            {
                Assert.DoesNotContain('/', reference.Value);
                Assert.DoesNotContain('\\', reference.Value);
            });
    }

    private static async Task<CombatLoadoutRecommendation> Recommend(
        CombatSnapshot snapshot)
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);
        return await new RecommendCombatLoadout(reader).ExecuteAsync(
            new RecommendCombatLoadoutRequest(
                snapshot.Metadata.SavePath,
                snapshot.Target.CharacterId,
                RecommendationPolicy.Balanced),
            TestContext.Current.CancellationToken);
    }

    private static CombatLoadoutRecommendation
        DirectionChangeRecommendation()
    {
        var snapshot = GoldenSnapshot(PracticeDirection.Direct);
        var analysis = TargetThreatAnalyzer.Analyze(
            snapshot,
            VerifiedTargetThreatRuleSets.GoldenMagicSound);
        var option = new CombatLoadoutOption(
            new CombatSkillCandidate(
                skillId: 604,
                requiredDirection: PracticeDirection.Reverse,
                allowDirectionChange: true),
            requirements: [],
            analysis.Threats.Select(value => value.Threat.Code),
            isCurrentlyEquipped: false,
            evidenceReference: "evidence:direction-change",
            CombatCounterStrength.Mitigation,
            CombatCounterActivationTiming.ActiveAttack,
            expectedEffectId: 1064);
        var context = new CombatRequirementContext(
            equippedWeaponTypeIds: [],
            trickCounts: [],
            SnapshotValue<int>.Available(0),
            resources: [],
            unlockedWeaponTypeIds: [],
            equippedSkillIds: []);
        var generation = CombatLoadoutGenerator.Generate(
            new CombatLoadoutGenerationRequest(
                snapshot.Player,
                [option],
                context,
                snapshot.Player.GenericSlotAllocation));
        var threats = analysis.Threats
            .Select(value => value.Threat)
            .ToArray();
        var styles = Enum.GetValues<RecommendationPolicy>()
            .Select(policy =>
            {
                var scoring = CombatRecommendationScorer.Score(
                    new CombatRecommendationScoringRequest(
                        snapshot.Player,
                        threats,
                        generation.Candidates,
                        policy));
                var plan = ManualCombatPlanBuilder.Build(
                    snapshot.Player,
                    scoring);
                var explanation = plan.Plan is null
                    ? null
                    : CombatRecommendationExplanationBuilder.Build(
                        snapshot.Player,
                        threats,
                        plan.Plan);
                return CreateNonPublic<CombatRecommendationStyleResult>(
                    policy,
                    scoring,
                    plan,
                    explanation);
            })
            .ToArray();
        return CreateNonPublic<CombatLoadoutRecommendation>(
            snapshot,
            analysis,
            generation,
            RecommendationPolicy.Safe,
            styles,
            null,
            null);
    }

    private static T CreateNonPublic<T>(params object?[] arguments)
    {
        var constructor = typeof(T).GetConstructors(
                BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(value => value.GetParameters().Length == arguments.Length);
        return (T)constructor.Invoke(arguments);
    }

    private static CombatSnapshot GoldenSnapshot(
        PracticeDirection counterDirection)
    {
        var counter = Skill(
            604,
            SkillCategory.Attack,
            counterDirection,
            directEffectId: 338,
            reverseEffectId: 1064);
        var target = Skill(
            719,
            SkillCategory.Attack,
            PracticeDirection.Direct,
            directEffectId: 669,
            reverseEffectId: 1669);
        var reset = Skill(
            287,
            SkillCategory.Assistance,
            PracticeDirection.Reverse,
            directEffectId: 185,
            reverseEffectId: 911);
        return Snapshot(
            [counter],
            [target, reset],
            new CombatLoadoutSnapshot([], [], [], [], []),
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                new CombatLoadoutSnapshot(
                    [],
                    [target.SkillId],
                    [],
                    [],
                    [reset.SkillId])));
    }

    private static CombatSnapshot Snapshot(
        CombatSkillSnapshot[] playerSkills,
        CombatSkillSnapshot[] targetSkills,
        CombatLoadoutSnapshot playerLoadout,
        SnapshotValue<CombatLoadoutSnapshot> targetLoadout,
        SlotBudgetSet? budgets = null,
        GenericSlotAllocation? genericSlots = null,
        SnapshotFieldSource[]? fieldSources = null)
    {
        return new CombatSnapshot(
            new CombatSnapshotMetadata(
                @"C:\Taiwu\local.sav",
                new string('A', 64),
                DateTimeOffset.Parse("2026-07-30T12:00:00Z"),
                SnapshotValue<DateTimeOffset>.Available(
                    DateTimeOffset.Parse("2026-07-30T11:00:00Z")),
                SnapshotValue<string>.Available(
                    VerifiedCombatEffectCatalogs.GoldenGameDataVersion)),
            new PlayerCombatSnapshot(
                characterId: 1,
                SnapshotValue<string>.Available("Taiwu"),
                playerSkills,
                playerLoadout,
                equipment: [],
                budgets ?? Budgets(),
                genericSlots ?? new GenericSlotAllocation(0, 0, 0, 0, 0),
                legendaryBookCostSlots: [],
                legendaryBookCostAssignments: []),
            new TargetCombatSnapshot(
                characterId: 16317,
                SnapshotValue<string>.Available("Target"),
                SnapshotValue<int>.Available(52),
                features: [],
                targetSkills,
                targetLoadout,
                equipment: []),
            warnings: [],
            fieldSources);
    }

    private static SlotBudgetSet Budgets(
        int? attackUsed = 0,
        int attackCapacity = 2,
        string unavailableReason = "Attack usage is unavailable.")
    {
        var used = attackUsed.HasValue
            ? SnapshotValue<int>.Available(attackUsed.Value)
            : SnapshotValue<int>.Unavailable(unavailableReason);
        return new SlotBudgetSet(
        [
            new SlotBudget(SkillCategory.Neigong, 0, 6),
            new SlotBudget(SkillCategory.Attack, used, attackCapacity),
            new SlotBudget(SkillCategory.Agility, 0, 2),
            new SlotBudget(SkillCategory.Defense, 0, 2),
            new SlotBudget(SkillCategory.Assistance, 0, 2)
        ]);
    }

    private static CombatSkillSnapshot Skill(
        int skillId,
        SkillCategory category,
        PracticeDirection direction,
        int directEffectId,
        int reverseEffectId,
        SkillSlotContribution? slotContribution = null,
        SnapshotValue<int>? gridCost = null,
        bool mastered = true)
    {
        return new CombatSkillSnapshot(
            skillId,
            SnapshotValue<string>.Available($"Skill {skillId}"),
            category,
            gridCost ?? SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(mastered),
            SnapshotValue<PracticeDirection>.Available(direction),
            slotContribution ?? SkillSlotContribution.None,
            SnapshotValue<int>.Available(directEffectId),
            SnapshotValue<int>.Available(reverseEffectId));
    }

    private static CombatSkillSnapshot UnbrokenSkill(
        int skillId,
        SkillCategory category,
        PracticeDirection[] availableDirections,
        int directEffectId,
        int reverseEffectId)
    {
        return new CombatSkillSnapshot(
            skillId,
            SnapshotValue<string>.Available($"Skill {skillId}"),
            category,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(true),
            SnapshotValue<PracticeDirection>.Unavailable(
                "The skill has not completed breakthrough."),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(directEffectId),
            SnapshotValue<int>.Available(reverseEffectId),
            SnapshotValue<BreakthroughDirectionAvailability>.Available(
                new BreakthroughDirectionAvailability(
                    isBrokenOut: false,
                    canBreakthroughNow: true,
                    availableDirections)));
    }

    private static LoadoutComparisonSkillCell Skill(
        LoadoutComparisonColumn column,
        SkillCategory category,
        int skillId)
    {
        return column.Loadout!.Categories
            .Single(row => row.Category == category)
            .Skills.Single(cell => cell.Identity.SkillId == skillId);
    }

    private static LoadoutComparisonMembership Membership(
        ManualLoadoutChangeKind kind) => kind switch
        {
            ManualLoadoutChangeKind.Add => LoadoutComparisonMembership.Added,
            ManualLoadoutChangeKind.Remove =>
                LoadoutComparisonMembership.Removed,
            ManualLoadoutChangeKind.Retain =>
                LoadoutComparisonMembership.Retained,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    private static LoadoutComparisonSkillActionKind ActionKind(
        ManualLoadoutChangeKind kind) => kind switch
        {
            ManualLoadoutChangeKind.ChangeDirection =>
                LoadoutComparisonSkillActionKind.DirectionChangeRequired,
            ManualLoadoutChangeKind.CompleteBreakthrough =>
                LoadoutComparisonSkillActionKind.BreakthroughRequired,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    private static string Projection(LoadoutComparison comparison)
    {
        return string.Join(
            "|",
            comparison.Columns.SelectMany(column =>
                column.Loadout?.Categories.SelectMany(category =>
                    category.Skills.Select(cell =>
                        $"{column.Kind}:{category.Category}:"
                        + $"{cell.Identity.SkillId}:"
                        + $"{cell.Membership.Value}:"
                        + string.Join(
                            ',',
                            cell.Actions.Select(action =>
                                $"{action.Kind}:{action.RequiredDirection}"))))
                ?? [$"{column.Kind}:{column.Status}:"
                    + column.Diagnostic?.Code.Value]));
    }
}
