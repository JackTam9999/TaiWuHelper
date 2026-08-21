using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;
using Xunit;

namespace TaiWu.Domain.UnitTests.TacticalCombat;

public sealed class CurrentTacticalLoadoutPackageTests
{
    private static readonly TacticalCombatRuleSet Rules =
        VerifiedTacticalCombatRuleSets.CurrentLaterMagicSound;

    [Fact]
    public void Current_roles_form_three_cast_recovery_and_active_rotations()
    {
        var fixture = Fixture(confirmWhiskTricks: true);
        var result = Search(fixture);
        var package = Assert.Single(result.FeasibleResults, candidate =>
            HasSkills(candidate, 604, 686, 295, 303, 134, 150)).Package;

        Assert.Equal(
            TacticalPackageResolutionState.Complete,
            package.Recovery.State);
        Assert.Equal(3, package.Recovery.CastSteps.Length);
        Assert.Equal([1, 2, 3], package.Recovery.CastSteps.Select(
            step => step.Sequence));
        Assert.All(package.Recovery.CastSteps, step =>
        {
            Assert.Equal(
                new TacticalCandidateIdentity(686, PracticeDirection.Reverse),
                step.Candidate);
            Assert.Equal(2, step.EffectiveSlotCost);
            Assert.Contains(
                step.Requirements,
                requirement => requirement is ManualConfirmationRequirement
                {
                    Code: "USABLE_WHISK_TRICKS"
                });
            Assert.Equal(2, step.Requirements.OfType<ResourceRequirement>()
                .Count());
        });

        Assert.Equal(
            new TacticalCandidateIdentity(295, PracticeDirection.Reverse),
            package.ActiveDefenseRotation.PrimaryCandidate);
        Assert.Equal(
            [new TacticalCandidateIdentity(303, PracticeDirection.Reverse)],
            package.ActiveDefenseRotation.BackupCandidates);
        Assert.Equal(
            new TacticalCandidateIdentity(134, PracticeDirection.Reverse),
            package.ActiveAgilityRotation.PrimaryCandidate);
        Assert.Equal(
            [new TacticalCandidateIdentity(150, PracticeDirection.Reverse)],
            package.ActiveAgilityRotation.BackupCandidates);
        Assert.Contains(
            new TacticalCandidateIdentity(295, PracticeDirection.Reverse),
            package.ScoringEligibleCandidates);
        Assert.DoesNotContain(
            new TacticalCandidateIdentity(303, PracticeDirection.Reverse),
            package.ScoringEligibleCandidates);
        Assert.Contains(
            new TacticalCandidateIdentity(134, PracticeDirection.Reverse),
            package.ScoringEligibleCandidates);
        Assert.DoesNotContain(
            new TacticalCandidateIdentity(150, PracticeDirection.Reverse),
            package.ScoringEligibleCandidates);
    }

    [Fact]
    public void Missing_recovery_confirmation_keeps_an_explicit_unresolved_branch()
    {
        var fixture = Fixture(confirmWhiskTricks: false);

        var result = Search(fixture);
        var suppressionOnly = Assert.Single(result.FeasibleResults, candidate =>
            HasSkills(candidate, 604));

        Assert.Equal(
            TacticalPackageResolutionState.Unresolved,
            suppressionOnly.Package.Recovery.State);
        Assert.Equal(
            "REVERSE_604_RECOVERY_CASTS_UNRESOLVED",
            suppressionOnly.Package.Recovery.ReasonIdentity);
        Assert.Empty(suppressionOnly.Package.Recovery.CastSteps);
        Assert.DoesNotContain(
            fixture.Discovery.Entries,
            entry => entry.SkillId == 686 && entry.IsAdmitted);
    }

    [Fact]
    public void Switch_only_backups_enter_search_but_do_not_score_as_simultaneous()
    {
        var fixture = Fixture(confirmWhiskTricks: true);
        Assert.All(
            new[] { 134, 150, 295, 303 },
            skillId => Assert.Contains(
                fixture.Discovery.Entries,
                entry => entry.SkillId == skillId && entry.IsAdmitted));

        var request = Request(fixture);
        var search = TacticalLoadoutSearch.Search(
            request,
            cancellationToken: TestContext.Current.CancellationToken);
        var scoring = TacticalCombatScorer.Score(
            new TacticalCombatScoringRequest(
                RecommendationPolicy.Balanced,
                request,
                search),
            TestContext.Current.CancellationToken);
        var primaryOnly = Assert.Single(scoring.RankedCandidates, candidate =>
            HasSkills(candidate.Candidate, 134));
        var withBackup = Assert.Single(scoring.RankedCandidates, candidate =>
            HasSkills(candidate.Candidate, 134, 150));

        Assert.Equal(primaryOnly.TotalScore, withBackup.TotalScore);
        Assert.All(
            scoring.RankedCandidates.TakeWhile(candidate =>
                candidate.Candidate.Package.Recovery.State
                    != TacticalPackageResolutionState.Unresolved),
            candidate => Assert.NotEqual(
                TacticalPackageResolutionState.Unresolved,
                candidate.Candidate.Package.Recovery.State));
        var firstUnresolved = Array.FindIndex(
            scoring.RankedCandidates.ToArray(),
            candidate => candidate.Candidate.Package.Recovery.State
                == TacticalPackageResolutionState.Unresolved);
        var lastResolved = Array.FindLastIndex(
            scoring.RankedCandidates.ToArray(),
            candidate => candidate.Candidate.Package.Recovery.State
                != TacticalPackageResolutionState.Unresolved);
        Assert.True(firstUnresolved < 0 || lastResolved < firstUnresolved);
    }

    [Fact]
    public void Recovery_candidates_cannot_be_pruned_away_from_admitted_604()
    {
        var fixture = Fixture(confirmWhiskTricks: true);
        var recovery = Candidate(fixture, 686);
        var proof = new TacticalIrrelevanceProof(
            recovery,
            fixture.Context.SemanticFingerprint,
            [Evidence("ATTEMPTED_RECOVERY_PRUNE")]);

        var exception = Assert.Throws<ArgumentException>(() =>
            TacticalLoadoutSearch.Search(
                Request(fixture, [proof]),
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains(
            "RECOVERY_PACKAGE_MEMBER_CANNOT_BE_PRUNED_WHILE_REVERSE_604_IS_ADMITTED",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Manual_conditions_are_satisfied_only_by_explicit_codes()
    {
        var requirement = new ManualConfirmationRequirement(
            "USABLE_WHISK_TRICKS",
            CombatRequirementCriticality.Hard,
            "manual:test");
        var unknown = RequirementContext(confirmedCodes: null);
        var confirmed = RequirementContext(["USABLE_WHISK_TRICKS"]);

        Assert.False(CombatRequirementEvaluator.Evaluate(
            [requirement], unknown).IsAccepted);
        Assert.True(CombatRequirementEvaluator.Evaluate(
            [requirement], confirmed).IsAccepted);
    }

    [Fact]
    public void Current_package_compiles_exact_manifest_changes_and_battle_order()
    {
        var fixture = Fixture(confirmWhiskTricks: true);
        var request = Request(fixture);
        var search = TacticalLoadoutSearch.Search(
            request,
            cancellationToken: TestContext.Current.CancellationToken);
        var scoringRequest = new TacticalCombatScoringRequest(
            RecommendationPolicy.Balanced,
            request,
            search);
        var scoring = TacticalCombatScorer.Score(
            scoringRequest,
            TestContext.Current.CancellationToken);
        var selected = Assert.Single(scoring.RankedCandidates, candidate =>
            HasSkills(candidate.Candidate, 604, 686, 295, 303, 134, 150));

        var compiled = TacticalCombatPlanCompiler.Compile(
            new TacticalPlanCompilationRequest(
                scoringRequest,
                scoring,
                selected.Candidate.StableKey),
            TestContext.Current.CancellationToken);

        Assert.Equal(5, compiled.SelectedLoadoutPlan.Categories.Length);
        Assert.All(compiled.SelectedLoadoutPlan.Categories, category =>
            Assert.InRange(category.Used, 0, category.Capacity));
        Assert.Equal(
            TacticalLoadoutAssignmentKind.MainActiveAttack,
            SkillPlan(compiled, 604).Assignment);
        Assert.Equal(3, SkillPlan(compiled, 686).RecoveryCastCount);
        Assert.Equal(
            TacticalLoadoutAssignmentKind.ActiveDefensePrimary,
            SkillPlan(compiled, 295).Assignment);
        Assert.Equal(
            TacticalLoadoutAssignmentKind.SwitchOnlyBackup,
            SkillPlan(compiled, 303).Assignment);
        Assert.Equal(
            TacticalLoadoutAssignmentKind.ActiveAgilityPrimary,
            SkillPlan(compiled, 134).Assignment);
        Assert.Equal(
            TacticalLoadoutAssignmentKind.SwitchOnlyBackup,
            SkillPlan(compiled, 150).Assignment);

        Assert.Equal(5, compiled.PreparationChecks.Count(check =>
            check.Kind == TacticalPreparationCheckKind.Capacity));
        Assert.Contains(compiled.PreparationChecks, check =>
            check.ManualActionIdentity == "EQUIP_WEAPON_TYPE_6");
        Assert.Contains(compiled.PreparationChecks, check =>
            check.ManualActionIdentity == "EQUIP_WEAPON_TYPE_9");
        Assert.Contains(compiled.PreparationChecks, check =>
            check.ManualActionIdentity == "CONFIRM_USABLE_WHISK_TRICKS");
        Assert.Contains(compiled.PreparationChecks, check =>
            check.ManualActionIdentity.StartsWith(
                "CONFIRM_UNIVERSAL_SLOTS_",
                StringComparison.Ordinal));

        var recovery = Stage(compiled, TacticalPlanStage.Recovery);
        Assert.Equal(TacticalPlanStageState.Supported, recovery.State);
        Assert.Equal([686, 686, 686], recovery.Steps.Select(step =>
            step.SkillId!.Value));
        Assert.Equal(
            [
                "CAST_REVERSE_SKILL_686_FOR_LOCK_LAYER_1",
                "CAST_REVERSE_SKILL_686_FOR_LOCK_LAYER_2",
                "CAST_REVERSE_SKILL_686_FOR_LOCK_LAYER_3"
            ],
            recovery.Steps.Select(step => step.ManualActionIdentity));
        var response = Stage(
            compiled,
            TacticalPlanStage.TargetStateResponse);
        Assert.Contains(response.Steps, step =>
            step.ManualActionIdentity
                == "SWITCH_ACTIVE_DEFENSE_TO_SKILL_303");
        Assert.Contains(response.Steps, step =>
            step.ManualActionIdentity
                == "SWITCH_ACTIVE_AGILITY_TO_SKILL_150");
        Assert.DoesNotContain(
            compiled.Plan.Transitions,
            transition => transition.ExpectedPurposeIdentity
                == "DEFEAT_MARK_RESET");
        Assert.Equal(
            TacticalPlanStageState.Unsupported,
            Stage(compiled, TacticalPlanStage.Finish).State);
        Assert.Equal(search.SemanticFingerprint,
            compiled.SearchSemanticFingerprint);
        Assert.Equal(scoring.SemanticFingerprint,
            compiled.ScoringSemanticFingerprint);
        Assert.Equal(search.CandidateDecisions.Length,
            compiled.Plan.Candidates.Length);
        Assert.Equal(selected.Candidate.StableKey,
            compiled.SelectedLoadout.Candidate.StableKey);

        var alternativeSelection = Assert.Single(
            scoring.RankedCandidates,
            candidate => HasSkills(
                candidate.Candidate,
                604,
                686,
                295,
                134));
        var withAlternatives = TacticalCombatPlanCompiler.Compile(
            new TacticalPlanCompilationRequest(
                scoringRequest,
                scoring,
                alternativeSelection.Candidate.StableKey),
            TestContext.Current.CancellationToken);
        Assert.Contains(
            withAlternatives.SelectedLoadoutPlan.OptionalAlternatives,
            skill => skill.Candidate.SkillId == 303
                && skill.Assignment
                    == TacticalLoadoutAssignmentKind.OptionalAlternative);
        Assert.Contains(
            withAlternatives.SelectedLoadoutPlan.OptionalAlternatives,
            skill => skill.Candidate.SkillId == 150
                && skill.Assignment
                    == TacticalLoadoutAssignmentKind.OptionalAlternative);
    }

    private static TacticalLoadoutSkillPlan SkillPlan(
        TacticalCompiledCombatPlan compiled,
        int skillId) => Assert.Single(
        compiled.SelectedLoadoutPlan.SelectedSkills,
        skill => skill.Candidate.SkillId == skillId);

    private static TacticalPlanStageDefinition Stage(
        TacticalCompiledCombatPlan compiled,
        TacticalPlanStage stage) => compiled.Plan.Stages.Single(item =>
        item.Stage == stage);

    private static TacticalLoadoutSearchResult Search(FixtureData fixture) =>
        TacticalLoadoutSearch.Search(
            Request(fixture),
            cancellationToken: TestContext.Current.CancellationToken);

    private static TacticalLoadoutSearchRequest Request(
        FixtureData fixture,
        IEnumerable<TacticalIrrelevanceProof>? irrelevanceProofs = null) => new(
        fixture.Snapshot.Player,
        fixture.Context,
        fixture.Resolution,
        fixture.Discovery,
        new TacticalSearchBounds(
            maximumOptions: 8,
            maximumExploredCombinations: 256,
            maximumElapsed: TimeSpan.FromSeconds(30),
            maximumResults: 256),
        irrelevanceProofs);

    private static bool HasSkills(
        TacticalFeasibleLoadoutResult candidate,
        params int[] exactSkillIds) => candidate.SelectedCandidates
        .Select(item => item.SkillId)
        .Order()
        .SequenceEqual(exactSkillIds.Order());

    private static TacticalCandidateIdentity Candidate(
        FixtureData fixture,
        int skillId) => Assert.Single(
            fixture.Discovery.Entries,
            entry => entry.SkillId == skillId && entry.IsAdmitted)
        .Consideration.Identity;

    private static FixtureData Fixture(bool confirmWhiskTricks)
    {
        CombatSkillSnapshot[] skills =
        [
            Skill(604, SkillCategory.Attack, 1064),
            Skill(686, SkillCategory.Attack, 1422),
            Skill(134, SkillCategory.Agility, 973),
            Skill(150, SkillCategory.Agility, 989),
            Skill(295, SkillCategory.Defense, 919),
            Skill(303, SkillCategory.Defense, 927)
        ];
        var budgets = Budgets();
        var generic = new GenericSlotAllocation(0, 0, 0, 0, 0);
        var player = new PlayerCombatSnapshot(
            1,
            SnapshotValue<string>.Available("display-only player"),
            skills,
            new CombatLoadoutSnapshot([], [], [], [], []),
            equipment: [],
            budgets,
            generic,
            legendaryBookCostSlots: [],
            legendaryBookCostAssignments: [],
            SnapshotValue<InnerPowerStateSnapshot>.Available(
                new InnerPowerStateSnapshot(
                    1,
                    SnapshotValue<string>.Available("display-only inner"),
                    SnapshotValue<string>.Available("display-only raw"),
                    ElementAdjustmentSet.None,
                    ElementAdjustmentSet.None,
                    CombatSkillElement.Fire)));
        var snapshot = new CombatSnapshot(
            new CombatSnapshotMetadata(
                new string('D', 64),
                DateTimeOffset.Parse("2026-08-21T10:00:00Z"),
                SnapshotValue<DateTimeOffset>.Available(
                    DateTimeOffset.Parse("2026-08-21T09:00:00Z")),
                SnapshotValue<string>.Available(
                    VerifiedCombatEffectCatalogs.CurrentAntiMagic
                        .GameDataVersion)),
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
            VerifiedCombatEffectCatalogs.CurrentAntiMagic.GameDataVersion,
            Rules.SupportedTargetGoalCodes,
            ConfirmedEvidence());
        var codes = confirmWhiskTricks
            ? new[] { "USABLE_BLADE_TRICKS", "USABLE_WHISK_TRICKS" }
            : ["USABLE_BLADE_TRICKS"];
        var proposal = new TacticalExecutionProposal(
            RequirementContext(codes),
            budgets,
            generic,
            legendaryCostAssignments: []);
        var context = TacticalExecutionContextProjector.Project(
            snapshot,
            resolution,
            proposal);
        var discovery = TacticalCandidateDiscovery.Discover(
            player,
            context,
            resolution);
        return new FixtureData(snapshot, resolution, context, discovery);
    }

    private static CombatRequirementContext RequirementContext(
        IEnumerable<string>? confirmedCodes) => new(
        equippedWeaponTypeIds: [6, 9],
        trickCounts: [],
        SnapshotValue<int>.Available(4),
        resources:
        [
            Resource(CombatResourceKind.Stance, 100),
            Resource(CombatResourceKind.Breath, 100),
            Resource(CombatResourceKind.DefenseTrueQi, 3)
        ],
        unlockedWeaponTypeIds: [6, 9],
        equippedSkillIds: [604, 686, 134, 150, 295, 303],
        activeDefenseSkillId: 295,
        activeAgilitySkillId: 134,
        confirmedManualConditionCodes: confirmedCodes);

    private static TacticalRuleEvidenceObservation[] ConfirmedEvidence() =>
        Rules.Transitions
            .SelectMany(item => item.EvidenceRequirements)
            .Concat(Rules.Roles.SelectMany(item => item.EvidenceRequirements))
            .DistinctBy(item => (item.Identity.Code, item.Scope, item.Source))
            .Select((item, index) => new TacticalRuleEvidenceObservation(
                item.Identity,
                item.Scope,
                item.Source,
                TacticalRuleEvidenceDisposition.Confirmed,
                new TacticalEvidenceReference(
                    item.Source,
                    $"CURRENT_CONFIRMED_{index:000}",
                    VerifiedCombatEffectCatalogs.CurrentAntiMagic
                        .GameDataVersion,
                    VerifiedTacticalCombatRuleSets.RuleVersion,
                    item.Scope == TacticalRuleEvidenceScope.ExactTarget
                        ? "EXACT_TARGET"
                        : "BROAD_RULE")))
            .ToArray();

    private static TacticalEvidenceReference Evidence(string identity) => new(
        TacticalEvidenceSourceKind.VerifiedRule,
        identity,
        VerifiedCombatEffectCatalogs.CurrentAntiMagic.GameDataVersion,
        VerifiedTacticalCombatRuleSets.RuleVersion,
        "EXACT_TARGET");

    private static CombatSkillSnapshot Skill(
        int skillId,
        SkillCategory category,
        int reverseEffectId) => new(
        skillId,
        SnapshotValue<string>.Available("display-only skill"),
        category,
        SnapshotValue<int>.Available(3),
        SnapshotValue<bool>.Available(true),
        SnapshotValue<PracticeDirection>.Available(PracticeDirection.Reverse),
        SkillSlotContribution.None,
        SnapshotValue<int>.Available(0),
        SnapshotValue<int>.Available(reverseEffectId),
        breakthroughDirections: null,
        SnapshotValue<CombatSkillElement>.Available(CombatSkillElement.Water));

    private static CombatResourceAmount Resource(
        CombatResourceKind kind,
        int amount) => new(kind, SnapshotValue<int>.Available(amount));

    private static SlotBudgetSet Budgets() => new(
    [
        new SlotBudget(SkillCategory.Neigong, 0, 6),
        new SlotBudget(SkillCategory.Attack, 0, 10),
        new SlotBudget(SkillCategory.Agility, 0, 7),
        new SlotBudget(SkillCategory.Defense, 0, 9),
        new SlotBudget(SkillCategory.Assistance, 0, 4)
    ]);

    private sealed record FixtureData(
        CombatSnapshot Snapshot,
        TacticalCombatRuleResolution Resolution,
        TacticalExecutionContext Context,
        TacticalCandidateDiscoveryResult Discovery);
}
