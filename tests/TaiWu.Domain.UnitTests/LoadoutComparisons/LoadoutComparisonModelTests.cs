using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.LoadoutComparisons;
using Xunit;

namespace TaiWu.Domain.UnitTests.LoadoutComparisons;

public sealed class LoadoutComparisonModelTests
{
    [Fact]
    public void Comparison_owns_one_current_and_typed_policy_columns()
    {
        var comparison = Comparison(
            Current(),
            Policy(LoadoutComparisonColumnKind.Safe),
            Policy(LoadoutComparisonColumnKind.Balanced),
            Policy(LoadoutComparisonColumnKind.Aggressive));

        Assert.Equal("comparison:42", comparison.ComparisonReference.Value);
        Assert.Equal("snapshot:42", comparison.SnapshotReference.Value);
        Assert.Equal("target:7", comparison.TargetReference.Value);
        Assert.Equal(
            LoadoutComparisonColumnKind.Current,
            comparison.Current.Kind);
        Assert.Equal(
            RecommendationPolicy.Safe,
            comparison.GetPolicy(RecommendationPolicy.Safe)!.Policy);
        Assert.Equal(
            RecommendationPolicy.Balanced,
            comparison.GetPolicy(RecommendationPolicy.Balanced)!.Policy);
        Assert.Equal(
            RecommendationPolicy.Aggressive,
            comparison.GetPolicy(RecommendationPolicy.Aggressive)!.Policy);
    }

    [Fact]
    public void Comparison_allows_missing_policy_columns()
    {
        var comparison = Comparison(
            Current(),
            Policy(LoadoutComparisonColumnKind.Balanced));

        Assert.Null(comparison.GetPolicy(RecommendationPolicy.Safe));
        Assert.NotNull(comparison.GetPolicy(RecommendationPolicy.Balanced));
        Assert.Null(comparison.GetPolicy(RecommendationPolicy.Aggressive));
    }

    [Fact]
    public void Comparison_requires_exactly_one_current_column()
    {
        Assert.Throws<ArgumentException>(() => Comparison(
            Policy(LoadoutComparisonColumnKind.Safe)));
        Assert.Throws<ArgumentException>(() => Comparison(
            Current(),
            Current()));
    }

    [Fact]
    public void Comparison_rejects_duplicate_or_misordered_policy_columns()
    {
        Assert.Throws<ArgumentException>(() => Comparison(
            Current(),
            Policy(LoadoutComparisonColumnKind.Safe),
            Policy(LoadoutComparisonColumnKind.Safe)));
        Assert.Throws<ArgumentException>(() => Comparison(
            Current(),
            Policy(LoadoutComparisonColumnKind.Aggressive),
            Policy(LoadoutComparisonColumnKind.Balanced)));
    }

    [Fact]
    public void Category_rows_reject_duplicate_mismatched_and_unsorted_skills()
    {
        var first = Cell(
            SkillCategory.Attack,
            10,
            LoadoutComparisonMembership.Added);
        var second = Cell(
            SkillCategory.Attack,
            11,
            LoadoutComparisonMembership.Retained);
        var wrongCategory = Cell(
            SkillCategory.Defense,
            12,
            LoadoutComparisonMembership.Added);

        Assert.Throws<ArgumentException>(() => new LoadoutComparisonCategoryRow(
            SkillCategory.Attack,
            Capacity(),
            [first, first]));
        Assert.Throws<ArgumentException>(() => new LoadoutComparisonCategoryRow(
            SkillCategory.Attack,
            Capacity(),
            [wrongCategory]));
        Assert.Throws<ArgumentException>(() => new LoadoutComparisonCategoryRow(
            SkillCategory.Attack,
            Capacity(),
            [second, first]));
    }

    [Fact]
    public void Membership_and_composite_actions_remain_separate()
    {
        LoadoutComparisonSkillAction[] actions =
        [
            Action(
                LoadoutComparisonSkillActionKind.DirectionChangeRequired,
                PracticeDirection.Reverse),
            Action(
                LoadoutComparisonSkillActionKind.BreakthroughRequired,
                PracticeDirection.Direct)
        ];
        var cell = Cell(
            SkillCategory.Attack,
            25,
            LoadoutComparisonMembership.Added,
            actions);

        Assert.Equal(
            LoadoutComparisonMembership.Added,
            cell.Membership.Value);
        Assert.Collection(
            cell.Actions,
            action => Assert.Equal(
                LoadoutComparisonSkillActionKind.DirectionChangeRequired,
                action.Kind),
            action => Assert.Equal(
                LoadoutComparisonSkillActionKind.BreakthroughRequired,
                action.Kind));
        Assert.True(cell.HasRequiredChange);
    }

    [Fact]
    public void Current_and_policy_columns_enforce_distinct_memberships()
    {
        var added = Cell(
            SkillCategory.Attack,
            25,
            LoadoutComparisonMembership.Added);
        var present = Cell(
            SkillCategory.Attack,
            25,
            LoadoutComparisonMembership.Present);

        Assert.Throws<ArgumentException>(() => new LoadoutComparisonColumn(
            LoadoutComparisonColumnKind.Current,
            LoadoutComparisonColumnStatus.Available,
            Loadout(added),
            tacticalSummary: null,
            diagnostic: null));
        Assert.Throws<ArgumentException>(() => new LoadoutComparisonColumn(
            LoadoutComparisonColumnKind.Safe,
            LoadoutComparisonColumnStatus.Available,
            Loadout(present),
            Tactical(),
            diagnostic: null));
    }

    [Fact]
    public void Infeasible_policy_requires_diagnostic_and_forbids_loadout()
    {
        var diagnostic = Diagnostic();

        var column = new LoadoutComparisonColumn(
            LoadoutComparisonColumnKind.Safe,
            LoadoutComparisonColumnStatus.Infeasible,
            loadout: null,
            tacticalSummary: null,
            diagnostic);

        Assert.Null(column.Loadout);
        Assert.Equal("NO_FEASIBLE_POLICY", column.Diagnostic!.Code.Value);
        Assert.Throws<ArgumentException>(() => new LoadoutComparisonColumn(
            LoadoutComparisonColumnKind.Safe,
            LoadoutComparisonColumnStatus.Infeasible,
            Loadout(),
            tacticalSummary: null,
            diagnostic));
        Assert.Throws<ArgumentException>(() => new LoadoutComparisonColumn(
            LoadoutComparisonColumnKind.Safe,
            LoadoutComparisonColumnStatus.Unavailable,
            loadout: null,
            tacticalSummary: null,
            diagnostic: null));
    }

    [Fact]
    public void Unavailable_values_require_and_preserve_a_reason()
    {
        var unavailable = LoadoutComparisonValue<int>.Unavailable(
            "Effective cost is unavailable.");

        Assert.False(unavailable.IsAvailable);
        Assert.Equal(
            "Effective cost is unavailable.",
            unavailable.UnavailableReason);
        Assert.Throws<InvalidOperationException>(() => unavailable.Value);
        Assert.Throws<ArgumentException>(
            () => LoadoutComparisonValue<int>.Unavailable("  "));
    }

    [Fact]
    public void Available_zero_is_not_an_unavailable_numeric_value()
    {
        var zero = LoadoutComparisonValue<int>.Available(0);
        var unavailable = LoadoutComparisonValue<int>.Unavailable(
            "Used slots were not established.");

        Assert.True(zero.IsAvailable);
        Assert.Equal(0, zero.Value);
        Assert.False(unavailable.IsAvailable);
    }

    [Fact]
    public void Logical_references_reject_local_paths_and_compare_by_value()
    {
        Assert.Equal(Ref("snapshot:42"), Ref("snapshot:42"));
        Assert.Throws<ArgumentException>(
            () => Ref(@"C:\saves\tw.dat"));
        Assert.Throws<ArgumentException>(
            () => Ref("../save.dat"));
        Assert.Throws<ArgumentException>(() => Ref(" "));
    }

    [Fact]
    public void Collections_are_copied_and_cannot_follow_caller_mutation()
    {
        List<LoadoutComparisonSkillCell> skills =
        [
            Cell(
                SkillCategory.Attack,
                10,
                LoadoutComparisonMembership.Present)
        ];
        var row = new LoadoutComparisonCategoryRow(
            SkillCategory.Attack,
            Capacity(),
            skills);
        skills.Clear();

        List<LoadoutComparisonColumn> columns = [Current()];
        var comparison = Comparison(columns);
        columns.Add(Policy(LoadoutComparisonColumnKind.Safe));

        Assert.Single(row.Skills);
        Assert.Single(comparison.Columns);
    }

    [Fact]
    public void Loadout_requires_all_categories_in_canonical_order()
    {
        var categories = Categories().ToList();
        categories.Reverse();

        Assert.Throws<ArgumentException>(() => new LoadoutComparisonLoadout(
            categories,
            LoadoutComparisonValue<GenericSlotAllocation>.Available(
                new GenericSlotAllocation(0, 0, 0, 0, 0))));
    }

    [Fact]
    public void Capacity_preserves_unavailable_values_and_validates_arithmetic()
    {
        var summary = new LoadoutComparisonCapacitySummary(
            LoadoutComparisonValue<int>.Unavailable(
                "Used slots are unavailable."),
            LoadoutComparisonValue<int>.Available(6),
            LoadoutComparisonValue<int>.Unavailable(
                "Used slots are unavailable."),
            LoadoutComparisonValue<int>.Available(0),
            LoadoutComparisonValue<int>.Available(0));

        Assert.False(summary.Used.IsAvailable);
        Assert.False(summary.Remaining.IsAvailable);
        Assert.Throws<ArgumentException>(() =>
            new LoadoutComparisonCapacitySummary(
                LoadoutComparisonValue<int>.Available(2),
                LoadoutComparisonValue<int>.Available(6),
                LoadoutComparisonValue<int>.Available(5),
                LoadoutComparisonValue<int>.Available(0),
                LoadoutComparisonValue<int>.Available(0)));
    }

    [Fact]
    public void Provenance_fields_are_typed_unique_and_ordered()
    {
        var first = Provenance(
            LoadoutComparisonBaselineField.EquippedSkills,
            SnapshotDataSource.CurrentScreenObservation);
        var second = Provenance(
            LoadoutComparisonBaselineField.SlotBudgets,
            SnapshotDataSource.Save);

        var comparison = Comparison([Current()], [first, second]);

        Assert.Equal(
            SnapshotDataSource.CurrentScreenObservation,
            comparison.BaselineProvenance[0].Source);
        Assert.Throws<ArgumentException>(() =>
            Comparison([Current()], [first, first]));
        Assert.Throws<ArgumentException>(() =>
            Comparison([Current()], [second, first]));
    }

    private static LoadoutComparison Comparison(
        params LoadoutComparisonColumn[] columns) =>
        Comparison(columns, []);

    private static LoadoutComparison Comparison(
        IEnumerable<LoadoutComparisonColumn> columns,
        IEnumerable<LoadoutComparisonBaselineProvenance>? provenance = null)
    {
        return new LoadoutComparison(
            Ref("comparison:42"),
            Ref("snapshot:42"),
            Ref("target:7"),
            columns,
            provenance ?? []);
    }

    private static LoadoutComparisonColumn Current()
    {
        return new LoadoutComparisonColumn(
            LoadoutComparisonColumnKind.Current,
            LoadoutComparisonColumnStatus.Available,
            Loadout(
                Cell(
                    SkillCategory.Attack,
                    10,
                    LoadoutComparisonMembership.Present)),
            tacticalSummary: null,
            diagnostic: null);
    }

    private static LoadoutComparisonColumn Policy(
        LoadoutComparisonColumnKind kind)
    {
        return new LoadoutComparisonColumn(
            kind,
            LoadoutComparisonColumnStatus.Available,
            Loadout(
                Cell(
                    SkillCategory.Attack,
                    10,
                    LoadoutComparisonMembership.Retained)),
            Tactical(),
            diagnostic: null);
    }

    private static LoadoutComparisonLoadout Loadout(
        params LoadoutComparisonSkillCell[] cells)
    {
        return new LoadoutComparisonLoadout(
            Categories(cells),
            LoadoutComparisonValue<GenericSlotAllocation>.Available(
                new GenericSlotAllocation(0, 0, 0, 0, 0)));
    }

    private static LoadoutComparisonCategoryRow[] Categories(
        params LoadoutComparisonSkillCell[] cells)
    {
        return
        [
            .. Enum.GetValues<SkillCategory>().Select(category =>
                new LoadoutComparisonCategoryRow(
                    category,
                    Capacity(),
                    cells
                        .Where(cell => cell.Identity.Category == category)
                        .OrderBy(cell => cell.Identity.SkillId)))
        ];
    }

    private static LoadoutComparisonCapacitySummary Capacity()
    {
        return new LoadoutComparisonCapacitySummary(
            LoadoutComparisonValue<int>.Available(0),
            LoadoutComparisonValue<int>.Available(6),
            LoadoutComparisonValue<int>.Available(6),
            LoadoutComparisonValue<int>.Available(0),
            LoadoutComparisonValue<int>.Available(0));
    }

    private static LoadoutComparisonSkillCell Cell(
        SkillCategory category,
        int skillId,
        LoadoutComparisonMembership membership,
        params LoadoutComparisonSkillAction[] actions)
    {
        return new LoadoutComparisonSkillCell(
            new LoadoutComparisonSkillIdentity(category, skillId),
            LoadoutComparisonValue<LoadoutComparisonMembership>.Available(
                membership),
            LoadoutComparisonValue<int>.Available(1),
            actions);
    }

    private static LoadoutComparisonSkillAction Action(
        LoadoutComparisonSkillActionKind kind,
        PracticeDirection direction)
    {
        return new LoadoutComparisonSkillAction(
            kind,
            direction,
            new LoadoutComparisonReason(
                Ref($"reason:{kind}"),
                "Synthetic comparison reason.",
                [Ref("evidence:1")],
                [Ref("threat:1")]));
    }

    private static LoadoutComparisonTacticalSummary Tactical()
    {
        return new LoadoutComparisonTacticalSummary(
            LoadoutComparisonValue<int>.Available(0),
            LoadoutComparisonValue<LoadoutComparisonSkillIdentity>.Unavailable(
                "No active defense is selected."),
            LoadoutComparisonValue<LoadoutComparisonSkillIdentity>.Unavailable(
                "No active agility is selected."),
            coveredThreats: [],
            unresolvedThreats: [],
            conditions: [],
            caveats: [],
            evidenceReferences: [],
            scoreComponents: []);
    }

    private static LoadoutComparisonDiagnostic Diagnostic()
    {
        return new LoadoutComparisonDiagnostic(
            Ref("NO_FEASIBLE_POLICY"),
            "No feasible policy winner is available.",
            [Ref("evidence:generation")]);
    }

    private static LoadoutComparisonBaselineProvenance Provenance(
        LoadoutComparisonBaselineField field,
        SnapshotDataSource source)
    {
        return new LoadoutComparisonBaselineProvenance(
            field,
            source,
            DateTimeOffset.Parse("2026-08-08T12:00:00Z"),
            Ref($"evidence:{field}"));
    }

    private static LoadoutComparisonReference Ref(string value) => new(value);
}
