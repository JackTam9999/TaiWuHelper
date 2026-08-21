using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;
using Xunit;

namespace TaiWu.Domain.UnitTests.TacticalCombat;

public sealed class TacticalCandidateDiscoveryTests
{
    private static readonly TacticalCombatRuleSet Rules =
        VerifiedTacticalCombatRuleSets.HistoricalMagicSound;

    [Fact]
    public void Every_learned_skill_direction_has_one_canonical_result()
    {
        var supported = Skill(604, SkillCategory.Attack);
        var unrelated = Skill(
            999,
            SkillCategory.Defense,
            direction: PracticeDirection.Direct,
            directEffectId: 77,
            reverseEffectId: 78);
        var fixture = Fixture([unrelated, supported], equippedSkillIds: [999]);

        var result = Discover(fixture);

        Assert.Equal(2, result.LearnedSkillCount);
        Assert.Equal(4, result.Entries.Length);
        Assert.Equal(
            ["604:DIRECT", "604:REVERSE", "999:DIRECT", "999:REVERSE"],
            result.Entries.Select(item => item.StableKey));
        var admitted = Entry(result, 604, PracticeDirection.Reverse);
        Assert.Equal(
            TacticalCandidateAdmissionState.Admitted,
            admitted.AdmissionState);
        Assert.Equal(TacticalCandidateDecision.Admitted, admitted.Consideration.Decision);
        Assert.Equal(1064, admitted.ObservedRawEffectId.Value);
        Assert.Equal(2, admitted.EffectiveCost.Value);

        var retained = Entry(result, 999, PracticeDirection.Direct);
        Assert.True(retained.IsCurrentlyEquipped);
        Assert.Equal(
            TacticalCandidateAdmissionState.RetainedOnly,
            retained.AdmissionState);
        Assert.Equal(
            TacticalCandidateSupportState.IrrelevantSkill,
            retained.SupportState);
        Assert.Equal(
            TacticalCandidateDecision.Irrelevant,
            retained.Consideration.Decision);
        Assert.Equal("CURRENT_RETENTION_ONLY", retained.Consideration.ReasonIdentity);
    }

    [Fact]
    public void Exact_raw_effect_mismatch_is_infeasible()
    {
        var fixture = Fixture(
            [Skill(604, SkillCategory.Attack, reverseEffectId: 9999)]);

        var entry = Entry(
            Discover(fixture),
            604,
            PracticeDirection.Reverse);

        Assert.Equal(
            TacticalCandidateSupportState.VerifiedRole,
            entry.SupportState);
        Assert.Equal(
            TacticalCandidateAdmissionState.Infeasible,
            entry.AdmissionState);
        AssertGate(
            entry,
            TacticalCandidateGateKind.RawEffect,
            TacticalCandidateGateState.Failed,
            "RAW_EFFECT_ID_MISMATCH");
    }

    [Fact]
    public void Missing_execution_context_prevents_unconditional_admission()
    {
        var fixture = Fixture([Skill(604, SkillCategory.Attack)]);
        var context = TacticalExecutionContextProjector.Project(
            fixture.Snapshot,
            fixture.Resolution,
            cancellationToken: TestContext.Current.CancellationToken);

        var entry = Entry(
            TacticalCandidateDiscovery.Discover(
                fixture.Snapshot.Player,
                context,
                fixture.Resolution,
                cancellationToken: TestContext.Current.CancellationToken),
            604,
            PracticeDirection.Reverse);

        Assert.Equal(
            TacticalCandidateAdmissionState.UnknownContext,
            entry.AdmissionState);
        AssertGate(
            entry,
            TacticalCandidateGateKind.CategoryCapacity,
            TacticalCandidateGateState.Unknown,
            "PROPOSED_CATEGORY_CAPACITY_UNKNOWN");
        AssertGate(
            entry,
            TacticalCandidateGateKind.UniversalSlots,
            TacticalCandidateGateState.Unknown,
            "PROPOSED_UNIVERSAL_SLOTS_UNKNOWN");
    }

    [Fact]
    public void Unknown_mastery_blocks_effective_cost_admission()
    {
        var fixture = Fixture(
            [Skill(604, SkillCategory.Attack, mastered: null)]);

        var entry = Entry(
            Discover(fixture),
            604,
            PracticeDirection.Reverse);

        Assert.Equal(
            TacticalCandidateAdmissionState.UnknownContext,
            entry.AdmissionState);
        AssertGate(
            entry,
            TacticalCandidateGateKind.Mastery,
            TacticalCandidateGateState.Unknown,
            "MASTERY_STATUS_UNKNOWN");
        Assert.False(entry.EffectiveCost.IsAvailable);
    }

    [Fact]
    public void Active_role_requirement_must_be_explicitly_selected()
    {
        var skill = Skill(
            134,
            SkillCategory.Agility,
            reverseEffectId: 973);
        var unknown = Discover(Fixture(
            [skill],
            configureKnownActiveRoles: false));
        var selected = Discover(Fixture([skill]));

        Assert.Equal(
            TacticalCandidateAdmissionState.UnknownContext,
            Entry(unknown, 134, PracticeDirection.Reverse).AdmissionState);
        Assert.Equal(
            TacticalCandidateAdmissionState.Admitted,
            Entry(selected, 134, PracticeDirection.Reverse).AdmissionState);
    }

    [Fact]
    public void Immediate_breakthrough_is_a_separate_feasible_direction()
    {
        var skill = Skill(
            604,
            SkillCategory.Attack,
            direction: null,
            breakthrough: new BreakthroughDirectionAvailability(
                isBrokenOut: false,
                canBreakthroughNow: true,
                availableDirections: [PracticeDirection.Reverse]));
        var result = Discover(Fixture([skill]));

        var reverse = Entry(result, 604, PracticeDirection.Reverse);
        Assert.True(reverse.RequiresBreakthrough);
        Assert.Equal(
            TacticalCandidateAdmissionState.Admitted,
            reverse.AdmissionState);
        AssertGate(
            reverse,
            TacticalCandidateGateKind.Direction,
            TacticalCandidateGateState.Passed,
            "IMMEDIATE_BREAKTHROUGH_DIRECTION_CONFIRMED");
        Assert.Equal(
            2,
            result.Entries.Count(item => item.SkillId == 604));
    }

    [Fact]
    public void Completed_and_active_direction_duplicates_are_collapsed()
    {
        var skill = Skill(
            604,
            SkillCategory.Attack,
            breakthrough: new BreakthroughDirectionAvailability(
                isBrokenOut: true,
                canBreakthroughNow: false,
                availableDirections: [],
                completedDirections:
                [
                    PracticeDirection.Reverse,
                    PracticeDirection.Direct,
                    PracticeDirection.Reverse
                ]));

        var result = Discover(Fixture([skill]));

        Assert.Equal(2, result.Entries.Length);
        Assert.Equal(
            2,
            result.Entries.Select(item => item.StableKey).Distinct().Count());
        Assert.False(
            Entry(result, 604, PracticeDirection.Reverse)
                .RequiresBreakthrough);
    }

    [Fact]
    public void One_loadout_cannot_select_both_directions_of_one_skill()
    {
        var requirementContext = new CombatRequirementContext(
            equippedWeaponTypeIds: [],
            trickCounts: [],
            SnapshotValue<int>.Available(5),
            resources: [],
            unlockedWeaponTypeIds: [],
            equippedSkillIds: [604]);

        Assert.Throws<ArgumentException>(() => new ProposedCombatLoadout(
            new CombatLoadoutSnapshot([], [604], [], [], []),
            new GenericSlotAllocation(0, 0, 0, 0, 0),
            skillCandidates:
            [
                new CombatSkillCandidate(
                    604,
                    requiredDirection: PracticeDirection.Direct),
                new CombatSkillCandidate(
                    604,
                    requiredDirection: PracticeDirection.Reverse)
            ],
            requirements: [],
            requirementContext));
    }

    [Fact]
    public void Backlash_on_active_use_rejects_candidate()
    {
        var fixture = Fixture(
            [Skill(604, SkillCategory.Attack, element: CombatSkillElement.Fire)],
            backlashElement: CombatSkillElement.Fire);

        var entry = Entry(
            Discover(fixture),
            604,
            PracticeDirection.Reverse);

        Assert.Equal(
            TacticalCandidateAdmissionState.Infeasible,
            entry.AdmissionState);
        AssertGate(
            entry,
            TacticalCandidateGateKind.InnerPowerBacklash,
            TacticalCandidateGateState.Failed,
            "INNER_POWER_BACKLASH_ON_USE");
    }

    [Fact]
    public void Conditional_untyped_role_retains_exact_unknown_requirement()
    {
        var fixture = Fixture(
            [Skill(611, SkillCategory.Attack, reverseEffectId: 1165)]);

        var entry = Entry(
            Discover(fixture),
            611,
            PracticeDirection.Reverse);

        Assert.Equal(
            TacticalCandidateAdmissionState.UnknownContext,
            entry.AdmissionState);
        AssertGate(
            entry,
            TacticalCandidateGateKind.ExecutionRequirements,
            TacticalCandidateGateState.Unknown,
            "EXECUTION_REQUIREMENTS_NOT_TYPED");
    }

    [Fact]
    public void Unsupported_version_exposes_no_historical_candidates()
    {
        var fixture = Fixture(
            [Skill(604, SkillCategory.Attack)],
            gameDataVersion: "1.0.0+current");

        var result = Discover(fixture);

        Assert.All(
            result.Entries,
            entry =>
            {
                Assert.Equal(
                    TacticalCandidateSupportState.UnsupportedGameDataVersion,
                    entry.SupportState);
                Assert.False(entry.IsAdmitted);
                Assert.Null(entry.Role);
            });
        Assert.Equal(0, result.SupportedRoleCount);
        Assert.Equal(2, result.UnsupportedCount);
    }

    [Fact]
    public void Wrong_direction_does_not_borrow_opposite_role()
    {
        var result = Discover(Fixture([Skill(
            604,
            SkillCategory.Attack,
            direction: PracticeDirection.Direct)]));

        var direct = Entry(result, 604, PracticeDirection.Direct);

        Assert.Equal(
            TacticalCandidateSupportState.UnsupportedEffect,
            direct.SupportState);
        AssertGate(
            direct,
            TacticalCandidateGateKind.TacticalRole,
            TacticalCandidateGateState.Unsupported,
            "TACTICAL_ROLE_WRONG_DIRECTION");
    }

    [Fact]
    public void Current_loadout_uses_saved_legendary_cost_assignment()
    {
        var skill = Skill(
            604,
            SkillCategory.Attack,
            mastered: false);
        var slot = new LegendaryBookCostSlot(
            "book:slot:shouzhi",
            new LegendaryBookCostRule(
                LegendaryBookCostEffect.Shouzhi,
                SnapshotDataSource.Save,
                "save:legendary-book:rule"));
        var assignment = new LegendaryBookCostAssignment(
            slot,
            skill.SkillId,
            skill.Category,
            LegendaryBookAssignmentOrigin.Save,
            "save:legendary-book:assignment");
        var fixture = Fixture(
            [skill],
            equippedSkillIds: [skill.SkillId],
            useCurrentLoadoutBaseline: true,
            legendaryBookCostSlots: [slot],
            legendaryBookCostAssignments: [assignment]);

        var entry = Entry(
            Discover(fixture),
            skill.SkillId,
            PracticeDirection.Reverse);

        Assert.True(entry.EffectiveCost.IsAvailable);
        Assert.Equal(1, entry.EffectiveCost.Value);
        AssertGate(
            entry,
            TacticalCandidateGateKind.EffectiveCost,
            TacticalCandidateGateState.Passed,
            "EFFECTIVE_COST_VERIFIED");
    }

    [Fact]
    public void Enumeration_order_and_display_text_do_not_change_result()
    {
        var first = Fixture(
        [
            Skill(604, SkillCategory.Attack, displayName: "localized A"),
            Skill(
                999,
                SkillCategory.Defense,
                direction: PracticeDirection.Direct,
                directEffectId: 77,
                reverseEffectId: 78,
                displayName: "localized B")
        ]);
        var second = Fixture(
        [
            Skill(
                999,
                SkillCategory.Defense,
                direction: PracticeDirection.Direct,
                directEffectId: 77,
                reverseEffectId: 78,
                displayName: "totally different"),
            Skill(604, SkillCategory.Attack, displayName: "renamed")
        ]);

        Assert.Equal(
            Discover(first).SemanticFingerprint,
            Discover(second).SemanticFingerprint);
    }

    [Fact]
    public void Rejection_summaries_bound_examples_without_losing_counts()
    {
        var skills = Enumerable.Range(1000, 6)
            .Select(id => Skill(
                id,
                SkillCategory.Defense,
                direction: PracticeDirection.Direct,
                directEffectId: id,
                reverseEffectId: id + 100))
            .ToArray();
        var fixture = Fixture(skills);

        var result = TacticalCandidateDiscovery.Discover(
            fixture.Snapshot.Player,
            fixture.Context,
            fixture.Resolution,
            new TacticalCandidateDiscoveryLimits(
                maxLearnedSkills: 10,
                maxExamplesPerReason: 2),
            TestContext.Current.CancellationToken);

        var unsupported = Assert.Single(
            result.RejectionSummaries,
            item => item.ReasonIdentity == "TACTICAL_EFFECT_UNSUPPORTED");
        Assert.Equal(12, unsupported.Count);
        Assert.Equal(2, unsupported.ExampleConsiderationKeys.Length);
    }

    [Fact]
    public void Pre_cancelled_discovery_stops_before_enumeration()
    {
        var fixture = Fixture([Skill(604, SkillCategory.Attack)]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            TacticalCandidateDiscovery.Discover(
                fixture.Snapshot.Player,
                fixture.Context,
                fixture.Resolution,
                cancellationToken: cancellation.Token));
    }

    private static TacticalCandidateDiscoveryResult Discover(FixtureData fixture) =>
        TacticalCandidateDiscovery.Discover(
            fixture.Snapshot.Player,
            fixture.Context,
            fixture.Resolution,
            cancellationToken: TestContext.Current.CancellationToken);

    private static TacticalCandidateDiscoveryEntry Entry(
        TacticalCandidateDiscoveryResult result,
        int skillId,
        PracticeDirection direction) => Assert.Single(
        result.Entries,
        item => item.SkillId == skillId && item.Direction == direction);

    private static void AssertGate(
        TacticalCandidateDiscoveryEntry entry,
        TacticalCandidateGateKind kind,
        TacticalCandidateGateState state,
        string reason)
    {
        var gate = Assert.Single(entry.Gates, item => item.Kind == kind);
        Assert.Equal(state, gate.State);
        Assert.Equal(reason, gate.ReasonIdentity);
    }

    private static FixtureData Fixture(
        IEnumerable<CombatSkillSnapshot> skills,
        IEnumerable<int>? equippedSkillIds = null,
        CombatSkillElement? backlashElement = CombatSkillElement.Fire,
        string? gameDataVersion = null,
        bool configureKnownActiveRoles = true,
        bool useCurrentLoadoutBaseline = false,
        IEnumerable<LegendaryBookCostSlot>? legendaryBookCostSlots = null,
        IEnumerable<LegendaryBookCostAssignment>?
            legendaryBookCostAssignments = null)
    {
        var skillValues = skills.ToArray();
        var equipped = (equippedSkillIds ?? []).ToHashSet();
        var loadout = new CombatLoadoutSnapshot(
            skillValues.Where(item => equipped.Contains(item.SkillId)
                    && item.Category == SkillCategory.Neigong)
                .Select(item => item.SkillId),
            skillValues.Where(item => equipped.Contains(item.SkillId)
                    && item.Category == SkillCategory.Attack)
                .Select(item => item.SkillId),
            skillValues.Where(item => equipped.Contains(item.SkillId)
                    && item.Category == SkillCategory.Agility)
                .Select(item => item.SkillId),
            skillValues.Where(item => equipped.Contains(item.SkillId)
                    && item.Category == SkillCategory.Defense)
                .Select(item => item.SkillId),
            skillValues.Where(item => equipped.Contains(item.SkillId)
                    && item.Category == SkillCategory.Assistance)
                .Select(item => item.SkillId));
        var version = gameDataVersion
            ?? VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion;
        var player = new PlayerCombatSnapshot(
            1,
            SnapshotValue<string>.Available("display-only player"),
            skillValues,
            loadout,
            equipment: [],
            Budgets(),
            new GenericSlotAllocation(2, 1, 1, 0, 0),
            legendaryBookCostSlots: legendaryBookCostSlots ?? [],
            legendaryBookCostAssignments:
                legendaryBookCostAssignments ?? [],
            SnapshotValue<InnerPowerStateSnapshot>.Available(
                new InnerPowerStateSnapshot(
                    1,
                    SnapshotValue<string>.Available("display-only inner"),
                    SnapshotValue<string>.Available("raw description"),
                    ElementAdjustmentSet.None,
                    ElementAdjustmentSet.None,
                    backlashElement)));
        var snapshot = new CombatSnapshot(
            new CombatSnapshotMetadata(
                new string('C', 64),
                DateTimeOffset.Parse("2026-08-20T12:00:00Z"),
                SnapshotValue<DateTimeOffset>.Available(
                    DateTimeOffset.Parse("2026-08-20T11:00:00Z")),
                SnapshotValue<string>.Available(version)),
            player,
            new TargetCombatSnapshot(
                2,
                SnapshotValue<string>.Unavailable("Not required."),
                SnapshotValue<int>.Unavailable("Not required."),
                features: [],
                learnedSkills: [],
                SnapshotValue<CombatLoadoutSnapshot>.Unavailable(
                    "Not required."),
                equipment: []),
            warnings: []);
        var resolution = Rules.Resolve(
            version,
            Rules.SupportedTargetGoalCodes,
            string.Equals(
                version,
                VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion,
                StringComparison.Ordinal)
                ? ConfirmedEvidence()
                : []);
        var proposal = new TacticalExecutionProposal(
            new CombatRequirementContext(
                equippedWeaponTypeIds: [],
                trickCounts: [],
                SnapshotValue<int>.Available(5),
                resources: [],
                unlockedWeaponTypeIds: [],
                equippedSkillIds: skillValues.Select(item => item.SkillId),
                activeAgilitySkillId: configureKnownActiveRoles
                    && skillValues.Any(item => item.SkillId == 134)
                    ? 134
                    : null),
            Budgets(),
            new GenericSlotAllocation(2, 1, 1, 0, 0),
            legendaryCostAssignments: []);
        var context = useCurrentLoadoutBaseline
            ? TacticalExecutionContextProjector.ProjectCurrentLoadout(
                snapshot,
                resolution,
                TestContext.Current.CancellationToken)
            : TacticalExecutionContextProjector.Project(
                snapshot,
                resolution,
                proposal,
                TestContext.Current.CancellationToken);
        return new FixtureData(snapshot, resolution, context);
    }

    private static TacticalRuleEvidenceObservation[] ConfirmedEvidence() =>
        Rules.Transitions
            .SelectMany(item => item.EvidenceRequirements)
            .Concat(Rules.Roles.SelectMany(item => item.EvidenceRequirements))
            .DistinctBy(item => (
                item.Identity.Code,
                item.Scope,
                item.Source))
            .Select((item, index) => new TacticalRuleEvidenceObservation(
                item.Identity,
                item.Scope,
                item.Source,
                TacticalRuleEvidenceDisposition.Confirmed,
                new TacticalEvidenceReference(
                    item.Source,
                    $"CONFIRMED_{index:000}",
                    VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion,
                    VerifiedTacticalCombatRuleSets.RuleVersion,
                    item.Scope == TacticalRuleEvidenceScope.ExactTarget
                        ? "EXACT_TARGET"
                        : "BROAD_RULE")))
            .ToArray();

    private static CombatSkillSnapshot Skill(
        int skillId,
        SkillCategory category,
        PracticeDirection? direction = PracticeDirection.Reverse,
        int directEffectId = 338,
        int reverseEffectId = 1064,
        CombatSkillElement element = CombatSkillElement.Water,
        BreakthroughDirectionAvailability? breakthrough = null,
        string displayName = "display-only skill",
        bool? mastered = true) => new(
        skillId,
        SnapshotValue<string>.Available(displayName),
        category,
        SnapshotValue<int>.Available(3),
        mastered.HasValue
            ? SnapshotValue<bool>.Available(mastered.Value)
            : SnapshotValue<bool>.Unavailable("Mastery was not captured."),
        direction.HasValue
            ? SnapshotValue<PracticeDirection>.Available(direction.Value)
            : SnapshotValue<PracticeDirection>.Unavailable(
                "Breakthrough is pending."),
        SkillSlotContribution.None,
        SnapshotValue<int>.Available(directEffectId),
        SnapshotValue<int>.Available(reverseEffectId),
        breakthrough is null
            ? null
            : SnapshotValue<BreakthroughDirectionAvailability>.Available(
                breakthrough),
        SnapshotValue<CombatSkillElement>.Available(element));

    private static SlotBudgetSet Budgets() => new(
    [
        new SlotBudget(SkillCategory.Neigong, 0, 6),
        new SlotBudget(SkillCategory.Attack, 0, 10),
        new SlotBudget(SkillCategory.Agility, 0, 8),
        new SlotBudget(SkillCategory.Defense, 0, 8),
        new SlotBudget(SkillCategory.Assistance, 0, 2)
    ]);

    private sealed record FixtureData(
        CombatSnapshot Snapshot,
        TacticalCombatRuleResolution Resolution,
        TacticalExecutionContext Context);
}
