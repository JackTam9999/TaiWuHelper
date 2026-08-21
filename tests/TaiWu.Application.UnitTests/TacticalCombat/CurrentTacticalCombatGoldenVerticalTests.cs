using NSubstitute;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.TacticalCombat;
using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;
using Xunit;

namespace TaiWu.Application.UnitTests.TacticalCombat;

public sealed class CurrentTacticalCombatGoldenVerticalTests
{
    private static readonly TacticalCombatRuleSet Rules =
        VerifiedTacticalCombatRuleSets.CurrentLaterMagicSound;

    private static readonly SkillSpec[] GoldenSkills =
    [
        new(604, SkillCategory.Attack, PracticeDirection.Reverse, 1064, 3,
            CombatSkillElement.Metal),
        new(616, SkillCategory.Attack, PracticeDirection.Reverse, 1251, 1,
            CombatSkillElement.Metal),
        new(147, SkillCategory.Agility, PracticeDirection.Direct, 260, 1,
            CombatSkillElement.Metal),
        new(150, SkillCategory.Agility, PracticeDirection.Reverse, 989, 1,
            CombatSkillElement.Wood),
        new(295, SkillCategory.Defense, PracticeDirection.Reverse, 919, 3,
            CombatSkillElement.Metal),
        new(303, SkillCategory.Defense, PracticeDirection.Reverse, 927, 3,
            CombatSkillElement.Wood),
        new(265, SkillCategory.Assistance, PracticeDirection.Reverse, 889, 1,
            CombatSkillElement.Water),
        new(267, SkillCategory.Assistance, PracticeDirection.Direct, 165, 1,
            CombatSkillElement.Water)
    ];

    private static readonly int[] ReferenceSkillIds =
        [.. GoldenSkills.Select(item => item.SkillId)];

    private static readonly TacticalCandidateIdentity[] ReferenceCandidates =
    [
        .. GoldenSkills.Select(item => new TacticalCandidateIdentity(
                item.SkillId,
                item.Direction))
            .OrderBy(item => item.SkillId)
            .ThenBy(item => item.Direction)
    ];

    private static readonly int[] ExpectedUsage = [0, 4, 2, 6, 2];

    private static readonly int[] AcceptedUsageLimits = [6, 9, 7, 8, 4];

    [Fact]
    public async Task Current_reference_package_is_feasible_layered_and_stable()
    {
        var ordered = Fixture(shuffle: false);
        var shuffled = Fixture(shuffle: true);
        var token = TestContext.Current.CancellationToken;

        var first = await ordered.Subject.ExecuteAsync(ordered.Request, token);
        var repeated = await ordered.Subject.ExecuteAsync(
            ordered.Request,
            token);
        var reordered = await shuffled.Subject.ExecuteAsync(
            shuffled.Request,
            token);

        AssertReferenceResult(first);

        foreach (var result in new[] { repeated, reordered })
        {
            AssertStableArtifacts(first, result);
        }
    }

    [Fact]
    public async Task Current_rules_remain_typed_when_evidence_is_incomplete()
    {
        var fixture = Fixture(shuffle: false, includeEvidence: false);

        var result = await fixture.Subject.ExecuteAsync(
            fixture.Request,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            TacticalCombatRecommendationStatus.PartialEvidence,
            result.Status);
        Assert.Equal(
            "TACTICAL_EVIDENCE_PARTIAL",
            result.ReasonIdentity);
        Assert.Null(result.CompiledPlan);
        Assert.NotNull(result.Identity);
        Assert.Equal(Rules.Fingerprint, result.Identity!.RuleFingerprint);
    }

    private static void AssertReferenceResult(
        TacticalCombatRecommendationResult result)
    {
        Assert.Equal(TacticalCombatRecommendationStatus.Success, result.Status);
        Assert.Equal(new string('D', 64), result.Identity!.SnapshotFingerprint);
        Assert.Equal(Rules.Fingerprint, result.Identity.RuleFingerprint);
        Assert.True(result.Search!.IsComplete);

        var reference = Reference(result);
        Assert.Equal(
            TacticalPackageResolutionState.Complete,
            reference.Package.Recovery.State);
        Assert.Equal(
            new TacticalCandidateIdentity(604, PracticeDirection.Reverse),
            reference.Package.Recovery.SuppressionCandidate);
        Assert.Equal(3, reference.Package.Recovery.CastSteps.Length);
        Assert.All(reference.Package.Recovery.CastSteps, step =>
        {
            Assert.Equal(
                new TacticalCandidateIdentity(616, PracticeDirection.Reverse),
                step.Candidate);
            Assert.Equal(1, step.EffectiveSlotCost);
        });
        Assert.Equal(
            new TacticalCandidateIdentity(147, PracticeDirection.Direct),
            reference.Package.ActiveAgilityRotation.PrimaryCandidate);
        Assert.Equal(
            [new TacticalCandidateIdentity(150, PracticeDirection.Reverse)],
            reference.Package.ActiveAgilityRotation.BackupCandidates);
        Assert.Equal(
            new TacticalCandidateIdentity(295, PracticeDirection.Reverse),
            reference.Package.ActiveDefenseRotation.PrimaryCandidate);
        Assert.Equal(
            [new TacticalCandidateIdentity(303, PracticeDirection.Reverse)],
            reference.Package.ActiveDefenseRotation.BackupCandidates);

        Assert.Equal(
            ExpectedUsage,
            reference.Loadout.SlotBudgets.Values.Select(item =>
                item.Used.Value));
        Assert.All(reference.Loadout.SlotBudgets.Values, budget =>
            Assert.InRange(
                budget.Used.Value,
                0,
                AcceptedUsageLimits[(int)budget.Category]));

        Assert.All(ReferenceCandidates, candidate =>
        {
            var entry = Assert.Single(result.Discovery!.Entries, item =>
                item.Consideration.Identity == candidate && item.IsAdmitted);
            Assert.Equal(
                TacticalCandidateDecision.Admitted,
                Assert.Single(result.Search.CandidateDecisions, item =>
                    item.Identity == entry.Consideration.Identity).Decision);
        });
        var scoredReference = Assert.Single(
            result.Scoring!.RankedCandidates,
            item => item.Candidate.StableKey == reference.StableKey);
        var layering = scoredReference.Get(
            TacticalScoreComponentKind.LayeredProtection);
        Assert.True(layering.IsAvailable);
        Assert.True(layering.NormalizedValue > 0);
        Assert.Contains(layering.RawInputs, input =>
            input.Identity.Contains(
                "CURRENT_REVERSE_265_INCREASES_MIND_DEFENSE",
                StringComparison.Ordinal));
    }

    private static void AssertStableArtifacts(
        TacticalCombatRecommendationResult expected,
        TacticalCombatRecommendationResult actual)
    {
        Assert.Equal(expected.Status, actual.Status);
        Assert.Equal(
            expected.Identity!.SemanticFingerprint,
            actual.Identity!.SemanticFingerprint);
        Assert.Equal(
            expected.Context!.Context.SemanticFingerprint,
            actual.Context!.Context.SemanticFingerprint);
        Assert.Equal(
            expected.Discovery!.SemanticFingerprint,
            actual.Discovery!.SemanticFingerprint);
        Assert.Equal(
            expected.Search!.SemanticFingerprint,
            actual.Search!.SemanticFingerprint);
        Assert.Equal(
            expected.Search.Coverage.Fingerprint,
            actual.Search.Coverage.Fingerprint);
        Assert.Equal(
            expected.Search.CandidateDecisions.Select(DecisionIdentity),
            actual.Search.CandidateDecisions.Select(DecisionIdentity));
        Assert.Equal(
            expected.Scoring!.SemanticFingerprint,
            actual.Scoring!.SemanticFingerprint);
        Assert.Equal(
            expected.CompiledPlan!.SelectedLoadoutFingerprint,
            actual.CompiledPlan!.SelectedLoadoutFingerprint);
        Assert.Equal(
            expected.CompiledPlan.SemanticFingerprint,
            actual.CompiledPlan.SemanticFingerprint);
        Assert.Equal(
            expected.LegacyComparison!.ComparisonReference,
            actual.LegacyComparison!.ComparisonReference);
        Assert.Equal(
            Reference(expected).StableKey,
            Reference(actual).StableKey);
    }

    private static TacticalFeasibleLoadoutResult Reference(
        TacticalCombatRecommendationResult result) => Assert.Single(
        result.Search!.FeasibleResults,
        HasReference);

    private static bool HasReference(TacticalFeasibleLoadoutResult candidate) =>
        candidate.SelectedCandidates
            .OrderBy(item => item.SkillId)
            .ThenBy(item => item.Direction)
            .SequenceEqual(ReferenceCandidates);

    private static string DecisionIdentity(
        TacticalCandidateConsideration item) => string.Join('|',
        item.Identity.SkillId,
        item.Identity.Direction,
        item.Decision,
        item.ReasonIdentity,
        item.DominatedBy?.SkillId,
        item.DominatedBy?.Direction,
        string.Join("||", item.Roles.Select(role =>
            $"{role.Kind}:{role.Code}")));

    private static TestFixture Fixture(
        bool shuffle,
        bool includeEvidence = true)
    {
        var snapshot = Snapshot(shuffle);
        var snapshotRequest = new CombatSnapshotReadRequest(
            "sanitized-current.sav",
            snapshot.Target.CharacterId);
        var evidence = includeEvidence ? ConfirmedEvidence() : [];
        if (shuffle)
        {
            evidence = [.. evidence.Reverse()];
        }

        var resolution = Rules.Resolve(
            VerifiedCombatEffectCatalogs.CurrentAntiMagic.GameDataVersion,
            Rules.SupportedTargetGoalCodes,
            evidence);
        var proposal = Proposal();
        var context = TacticalExecutionContextProjector.Project(
            snapshot,
            resolution,
            proposal);
        var layeringProofs = includeEvidence
            ? new[]
            {
                new TacticalLayeringProof(
                    new TacticalCandidateIdentity(
                        267,
                        PracticeDirection.Direct),
                    new TacticalCandidateIdentity(
                        265,
                        PracticeDirection.Reverse),
                    new TacticalTransitionIdentity(
                        "CURRENT_REVERSE_265_INCREASES_MIND_DEFENSE"),
                    TacticalLayeringKind.SeparateMitigation,
                    context.SemanticFingerprint,
                    [Evidence("E8-F07-LAYERED-MIND-PROTECTION")],
                    "SEPARATE_MITIGATIONS_ARE_NOT_INVULNERABILITY")
            }
            : [];
        var request = new TacticalCombatRecommendationRequest(
            snapshot.Player.CharacterId,
            RecommendationPolicy.Balanced,
            new TacticalLoadoutSearchReadRequest(
                new TacticalExecutionContextReadRequest(
                    snapshotRequest,
                    shuffle
                        ? Rules.SupportedTargetGoalCodes.Reverse()
                        : Rules.SupportedTargetGoalCodes,
                    evidence,
                    proposal),
                new TacticalSearchBounds(
                    maximumOptions: 8,
                    maximumExploredCombinations: 256,
                    maximumElapsed: TimeSpan.FromSeconds(30),
                    maximumResults: 256)),
            shuffle ? layeringProofs.Reverse() : layeringProofs);
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(snapshotRequest, Arg.Any<CancellationToken>())
            .Returns(snapshot);
        return new TestFixture(
            new RecommendTacticalCombat(
                reader,
                new SearchTacticalLoadoutsTests.ZeroElapsedTimeProvider(),
                Substitute.For<ITacticalCombatRecommendationFaultReporter>()),
            request);
    }

    private static CombatSnapshot Snapshot(bool shuffle)
    {
        var learned = GoldenSkills.Select(Skill).ToArray();
        if (shuffle)
        {
            learned = [.. learned.Reverse()];
        }

        return new CombatSnapshot(
            new CombatSnapshotMetadata(
                new string('D', 64),
                DateTimeOffset.Parse("2026-08-21T10:00:00Z"),
                SnapshotValue<DateTimeOffset>.Available(
                    DateTimeOffset.Parse("2026-08-21T09:00:00Z")),
                SnapshotValue<string>.Available(
                    VerifiedCombatEffectCatalogs.CurrentAntiMagic
                        .GameDataVersion)),
            new PlayerCombatSnapshot(
                1,
                SnapshotValue<string>.Available("sanitized player"),
                learned,
                new CombatLoadoutSnapshot([], [], [], [], []),
                equipment: [],
                Budgets(),
                new GenericSlotAllocation(0, 0, 0, 0, 0),
                legendaryBookCostSlots: [],
                legendaryBookCostAssignments: [],
                SnapshotValue<InnerPowerStateSnapshot>.Available(
                    new InnerPowerStateSnapshot(
                        1,
                        SnapshotValue<string>.Available("sanitized inner"),
                        SnapshotValue<string>.Available("sanitized raw"),
                        ElementAdjustmentSet.None,
                        ElementAdjustmentSet.None,
                        CombatSkillElement.Fire))),
            new TargetCombatSnapshot(
                2,
                SnapshotValue<string>.Available("sanitized target"),
                SnapshotValue<int>.Unavailable("Not required."),
                features: [],
                learnedSkills: [],
                SnapshotValue<CombatLoadoutSnapshot>.Unavailable(
                    "Not required."),
                equipment: []),
            warnings: []);
    }

    private static TacticalExecutionProposal Proposal() => new(
        new CombatRequirementContext(
            equippedWeaponTypeIds: [6, 9],
            trickCounts: [],
            SnapshotValue<int>.Available(5),
            resources:
            [
                Resource(CombatResourceKind.Stance, 100),
                Resource(CombatResourceKind.Breath, 100),
                Resource(CombatResourceKind.DefenseTrueQi, 3)
            ],
            unlockedWeaponTypeIds: [6, 9],
            equippedSkillIds: ReferenceSkillIds,
            activeDefenseSkillId: 295,
            activeAgilitySkillId: 147,
            confirmedManualConditionCodes:
            [
                "USABLE_BLADE_TRICKS",
                "CHARM_INPUT_AVAILABLE"
            ]),
        Budgets(),
        new GenericSlotAllocation(0, 0, 0, 0, 0),
        legendaryCostAssignments: []);

    private static CombatSkillSnapshot Skill(SkillSpec spec) => new(
        spec.SkillId,
        SnapshotValue<string>.Available("sanitized skill"),
        spec.Category,
        SnapshotValue<int>.Available(spec.GridCost),
        SnapshotValue<bool>.Available(false),
        SnapshotValue<PracticeDirection>.Available(spec.Direction),
        SkillSlotContribution.None,
        spec.Direction == PracticeDirection.Direct
            ? SnapshotValue<int>.Available(spec.EffectId)
            : SnapshotValue<int>.Unavailable("Opposite direction not required."),
        spec.Direction == PracticeDirection.Reverse
            ? SnapshotValue<int>.Available(spec.EffectId)
            : SnapshotValue<int>.Unavailable("Opposite direction not required."),
        breakthroughDirections: null,
        SnapshotValue<CombatSkillElement>.Available(spec.Element));

    private static TacticalRuleEvidenceObservation[] ConfirmedEvidence() =>
        Rules.Transitions.SelectMany(item => item.EvidenceRequirements)
            .Concat(Rules.Roles.SelectMany(item => item.EvidenceRequirements))
            .DistinctBy(item => (item.Identity.Code, item.Scope, item.Source))
            .Select((item, index) => new TacticalRuleEvidenceObservation(
                item.Identity,
                item.Scope,
                item.Source,
                TacticalRuleEvidenceDisposition.Confirmed,
                new TacticalEvidenceReference(
                    item.Source,
                    $"CURRENT_GOLDEN_{index:000}",
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

    private sealed record TestFixture(
        RecommendTacticalCombat Subject,
        TacticalCombatRecommendationRequest Request);

    private sealed record SkillSpec(
        int SkillId,
        SkillCategory Category,
        PracticeDirection Direction,
        int EffectId,
        int GridCost,
        CombatSkillElement Element);
}
