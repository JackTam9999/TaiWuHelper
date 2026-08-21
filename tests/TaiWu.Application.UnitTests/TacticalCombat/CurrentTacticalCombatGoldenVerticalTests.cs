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

    private static readonly int[] ReferenceSkillIds =
    [
        604, 616, 147, 150, 295, 303, 265, 267
    ];

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

        Assert.Equal(TacticalCombatRecommendationStatus.Success, first.Status);
        Assert.DoesNotContain(
            "UNSUPPORTED_GAME_DATA_RULE_CHAIN",
            first.ReasonIdentity,
            StringComparison.Ordinal);
        Assert.Equal(new string('D', 64), first.Identity!.SnapshotFingerprint);
        Assert.Equal(Rules.Fingerprint, first.Identity.RuleFingerprint);
        Assert.True(first.Search!.IsComplete);

        var reference = Assert.Single(first.Search.FeasibleResults, HasReference);
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
            Assert.Equal(2, step.EffectiveSlotCost);
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

        var capacityLimits = new[] { 6, 9, 7, 8, 4 };
        Assert.Equal(
            new[] { 0, 4, 4, 4, 4 },
            reference.Loadout.SlotBudgets.Values.Select(item =>
                item.Used.Value));
        Assert.All(reference.Loadout.SlotBudgets.Values, budget =>
            Assert.InRange(
                budget.Used.Value,
                0,
                capacityLimits[(int)budget.Category]));

        Assert.All(ReferenceSkillIds, skillId =>
        {
            var entry = Assert.Single(first.Discovery!.Entries, item =>
                item.SkillId == skillId && item.IsAdmitted);
            Assert.Equal(
                TacticalCandidateDecision.Admitted,
                Assert.Single(first.Search.CandidateDecisions, item =>
                    item.Identity == entry.Consideration.Identity).Decision);
        });
        var scoredReference = Assert.Single(
            first.Scoring!.RankedCandidates,
            item => item.Candidate.StableKey == reference.StableKey);
        var layering = scoredReference.Get(
            TacticalScoreComponentKind.LayeredProtection);
        Assert.True(layering.IsAvailable);
        Assert.True(layering.NormalizedValue > 0);
        Assert.Contains(layering.RawInputs, input =>
            input.Identity.Contains(
                "CURRENT_REVERSE_265_INCREASES_MIND_DEFENSE",
                StringComparison.Ordinal));

        foreach (var result in new[] { repeated, reordered })
        {
            Assert.Equal(first.Status, result.Status);
            Assert.Equal(
                first.Identity.SemanticFingerprint,
                result.Identity!.SemanticFingerprint);
            Assert.Equal(
                first.Context!.Context.SemanticFingerprint,
                result.Context!.Context.SemanticFingerprint);
            Assert.Equal(
                first.Discovery!.SemanticFingerprint,
                result.Discovery!.SemanticFingerprint);
            Assert.Equal(
                first.Search.SemanticFingerprint,
                result.Search!.SemanticFingerprint);
            Assert.Equal(
                first.Search.Coverage.Fingerprint,
                result.Search.Coverage.Fingerprint);
            Assert.Equal(
                first.Search.CandidateDecisions.Select(DecisionIdentity),
                result.Search.CandidateDecisions.Select(DecisionIdentity));
            Assert.Equal(
                first.Scoring.SemanticFingerprint,
                result.Scoring!.SemanticFingerprint);
            Assert.Equal(
                first.CompiledPlan!.SelectedLoadoutFingerprint,
                result.CompiledPlan!.SelectedLoadoutFingerprint);
            Assert.Equal(
                first.CompiledPlan.SemanticFingerprint,
                result.CompiledPlan.SemanticFingerprint);
            Assert.Equal(
                first.LegacyComparison!.ComparisonReference,
                result.LegacyComparison!.ComparisonReference);
            Assert.Equal(
                reference.StableKey,
                Assert.Single(result.Search.FeasibleResults, HasReference)
                    .StableKey);
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

    private static bool HasReference(TacticalFeasibleLoadoutResult candidate) =>
        candidate.SelectedCandidates.Select(item => item.SkillId)
            .Order()
            .SequenceEqual(ReferenceSkillIds.Order());

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
        CombatSkillSnapshot[] learned =
        [
            Skill(604, SkillCategory.Attack, PracticeDirection.Reverse, 1064),
            Skill(616, SkillCategory.Attack, PracticeDirection.Reverse, 1251),
            Skill(147, SkillCategory.Agility, PracticeDirection.Direct, 260),
            Skill(150, SkillCategory.Agility, PracticeDirection.Reverse, 989),
            Skill(295, SkillCategory.Defense, PracticeDirection.Reverse, 919),
            Skill(303, SkillCategory.Defense, PracticeDirection.Reverse, 927),
            Skill(265, SkillCategory.Assistance, PracticeDirection.Reverse, 889),
            Skill(267, SkillCategory.Assistance, PracticeDirection.Direct, 165)
        ];
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

    private static CombatSkillSnapshot Skill(
        int skillId,
        SkillCategory category,
        PracticeDirection direction,
        int effectId) => new(
        skillId,
        SnapshotValue<string>.Available("sanitized skill"),
        category,
        SnapshotValue<int>.Available(3),
        SnapshotValue<bool>.Available(true),
        SnapshotValue<PracticeDirection>.Available(direction),
        SkillSlotContribution.None,
        direction == PracticeDirection.Direct
            ? SnapshotValue<int>.Available(effectId)
            : SnapshotValue<int>.Unavailable("Opposite direction not required."),
        direction == PracticeDirection.Reverse
            ? SnapshotValue<int>.Available(effectId)
            : SnapshotValue<int>.Unavailable("Opposite direction not required."),
        breakthroughDirections: null,
        SnapshotValue<CombatSkillElement>.Available(CombatSkillElement.Water));

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
}
