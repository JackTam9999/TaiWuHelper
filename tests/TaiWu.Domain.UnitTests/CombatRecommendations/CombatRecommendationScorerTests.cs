using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatRecommendations;

public sealed class CombatRecommendationScorerTests
{
    [Fact]
    public void Every_score_component_is_individually_visible()
    {
        var fixture = CreateFixture();
        var candidate = GenerateSingleton(
            fixture.Player,
            fixture.DefensiveOption);

        var scored = Score(
            fixture.Player,
            [fixture.CriticalThreat],
            [candidate],
            RecommendationPolicy.Safe).RankedCandidates[0];

        Assert.Equal(
            Enum.GetValues<RecommendationScoreComponentKind>(),
            scored.Components.Select(component => component.Kind));
        Assert.All(
            scored.Components,
            component =>
            {
                Assert.InRange(component.Weight, 1, 100);
                Assert.False(
                    string.IsNullOrWhiteSpace(component.Explanation));
                Assert.False(
                    string.IsNullOrWhiteSpace(
                        component.EvidenceReference));
            });
    }

    [Fact]
    public void Missing_damage_evidence_is_visible_and_not_guessed()
    {
        var fixture = CreateFixture();
        var candidate = GenerateSingleton(
            fixture.Player,
            fixture.DefensiveOption);

        var scored = Score(
            fixture.Player,
            [fixture.CriticalThreat],
            [candidate],
            RecommendationPolicy.Safe).RankedCandidates[0];

        var damage = scored.Get(
            RecommendationScoreComponentKind.DamagePotential);
        Assert.False(damage.IsAvailable);
        Assert.Null(damage.Score);
        Assert.Null(damage.WeightedPoints);
        Assert.Contains("excluded", damage.Explanation);
        Assert.InRange(scored.TotalScore, 0, 100);
    }

    [Fact]
    public void Policy_weight_sets_are_documented_stable_percentages()
    {
        foreach (var policy in Enum.GetValues<RecommendationPolicy>())
        {
            var weights = RecommendationPolicyWeights.For(policy);
            var total = Enum
                .GetValues<RecommendationScoreComponentKind>()
                .Sum(weights.Get);

            Assert.Equal(100, total);
        }

        Assert.True(
            RecommendationPolicyWeights.For(RecommendationPolicy.Safe)
                .Survival
            > RecommendationPolicyWeights.For(
                RecommendationPolicy.Aggressive).Survival);
        Assert.True(
            RecommendationPolicyWeights.For(
                RecommendationPolicy.Aggressive).DamagePotential
            > RecommendationPolicyWeights.For(
                RecommendationPolicy.Safe).DamagePotential);
    }

    [Fact]
    public void Safe_and_aggressive_policies_apply_different_priorities()
    {
        var fixture = CreateFixture();
        var defensive = GenerateSingleton(
            fixture.Player,
            fixture.DefensiveOption);
        var offensive = GenerateSingleton(
            fixture.Player,
            fixture.OffensiveOption);
        CandidateDamageEvidence[] damage =
        [
            new(defensive.StableKey, 5, "evidence:defensive-damage"),
            new(offensive.StableKey, 100, "evidence:offensive-damage")
        ];

        var safe = Score(
            fixture.Player,
            [fixture.CriticalThreat],
            [offensive, defensive],
            RecommendationPolicy.Safe,
            damage);
        var aggressive = Score(
            fixture.Player,
            [fixture.CriticalThreat],
            [defensive, offensive],
            RecommendationPolicy.Aggressive,
            damage);

        Assert.Equal(
            defensive.StableKey,
            safe.RankedCandidates[0].Candidate.StableKey);
        Assert.Equal(
            offensive.StableKey,
            aggressive.RankedCandidates[0].Candidate.StableKey);
    }

    [Fact]
    public void Stable_key_breaks_complete_score_ties()
    {
        var firstSkill = CreateSkill(100);
        var secondSkill = CreateSkill(101);
        var player = CreatePlayer([firstSkill, secondSkill]);
        var first = GenerateSingleton(player, Option(firstSkill));
        var second = GenerateSingleton(player, Option(secondSkill));

        var forward = Score(
            player,
            targetThreats: [],
            [second, first],
            RecommendationPolicy.Balanced);
        var reverse = Score(
            player,
            targetThreats: [],
            [first, second],
            RecommendationPolicy.Balanced);

        Assert.Equal(
            forward.RankedCandidates.Select(value =>
                value.Candidate.StableKey),
            reverse.RankedCandidates.Select(value =>
                value.Candidate.StableKey));
    }

    [Fact]
    public void Breakthrough_prerequisite_reduces_execution_reliability()
    {
        var skill = new CombatSkillSnapshot(
            686,
            SnapshotValue<string>.Available("老君拂塵功"),
            SkillCategory.Assistance,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(false),
            SnapshotValue<PracticeDirection>.Unavailable(
                "Breakthrough is incomplete."),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(422),
            SnapshotValue<int>.Available(1422),
            SnapshotValue<BreakthroughDirectionAvailability>.Available(
                new BreakthroughDirectionAvailability(
                    isBrokenOut: false,
                    canBreakthroughNow: true,
                    [PracticeDirection.Reverse])));
        var player = CreatePlayer([skill]);
        var option = new CombatLoadoutOption(
            new CombatSkillCandidate(
                skill.SkillId,
                requiredDirection: PracticeDirection.Reverse,
                allowBreakthrough: true),
            requirements: [],
            threatCodes: [],
            isCurrentlyEquipped: false,
            "evidence:breakthrough",
            CombatCounterStrength.Mitigation,
            CombatCounterActivationTiming.EquippedPassive,
            expectedEffectId: 1422);
        var candidate = GenerateSingleton(player, option);

        var scored = Assert.Single(
            Score(
                player,
                targetThreats: [],
                [candidate],
                RecommendationPolicy.Balanced).RankedCandidates);

        Assert.Equal(
            85,
            scored.Get(
                RecommendationScoreComponentKind.ExecutionReliability).Score);
    }

    [Fact]
    public void Conditional_warning_lowers_visible_risk_component()
    {
        var safeSkill = CreateSkill(100);
        var riskySkill = CreateSkill(101);
        var player = CreatePlayer([safeSkill, riskySkill]);
        var safe = GenerateSingleton(player, Option(safeSkill));
        var risky = GenerateSingleton(
            player,
            Option(
                riskySkill,
                requirements:
                [
                    new WeaponRequirement(
                        weaponTypeId: 10,
                        CombatRequirementCriticality.Conditional,
                        "evidence:conditional-weapon")
                ]));

        var result = Score(
            player,
            targetThreats: [],
            [risky, safe],
            RecommendationPolicy.Balanced);

        Assert.True(
            result.RankedCandidates
                .Single(value => value.Candidate == safe)
                .Get(RecommendationScoreComponentKind.ConditionalRisk)
                .Score
            > result.RankedCandidates
                .Single(value => value.Candidate == risky)
                .Get(RecommendationScoreComponentKind.ConditionalRisk)
                .Score);
    }

    [Fact]
    public void Compatibility_measures_share_of_current_loadout_retained()
    {
        var retained = CreateSkill(100);
        var removed = CreateSkill(101);
        var player = CreatePlayer(
            [retained, removed],
            new CombatLoadoutSnapshot(
                neigongSkillIds: [],
                attackSkillIds: [retained.SkillId, removed.SkillId],
                agilitySkillIds: [],
                defenseSkillIds: [],
                assistanceSkillIds: []));
        var candidate = GenerateSingleton(
            player,
            Option(retained, isCurrentlyEquipped: true));

        var result = Score(
            player,
            targetThreats: [],
            [candidate],
            RecommendationPolicy.Balanced);

        Assert.Equal(
            50,
            result.RankedCandidates[0]
                .Get(
                    RecommendationScoreComponentKind
                        .CurrentLoadoutCompatibility)
                .Score);
    }

    [Fact]
    public void Safe_golden_ranking_prefers_verified_hard_coverage()
    {
        var fixture = CreateFixture();
        var hardCounter = GenerateSingleton(
            fixture.Player,
            fixture.DefensiveOption);
        var mitigation = GenerateSingleton(
            fixture.Player,
            Option(
                fixture.OffensiveSkill,
                threatCodes: [fixture.CriticalThreat.Code],
                strength: CombatCounterStrength.Mitigation));

        var result = Score(
            fixture.Player,
            [fixture.CriticalThreat],
            [mitigation, hardCounter],
            RecommendationPolicy.Safe);

        Assert.Equal(
            hardCounter.StableKey,
            result.RankedCandidates[0].Candidate.StableKey);
        Assert.Equal(
            100,
            result.RankedCandidates[0]
                .Get(RecommendationScoreComponentKind.Survival)
                .Score);
    }

    [Fact]
    public void Damage_evidence_must_reference_known_unique_candidate()
    {
        var fixture = CreateFixture();
        var candidate = GenerateSingleton(
            fixture.Player,
            fixture.DefensiveOption);
        var evidence = new CandidateDamageEvidence(
            candidate.StableKey,
            50,
            "evidence:damage");

        Assert.Throws<ArgumentException>(
            () => new CombatRecommendationScoringRequest(
                fixture.Player,
                [fixture.CriticalThreat],
                [candidate],
                RecommendationPolicy.Safe,
                [evidence, evidence]));
        Assert.Throws<ArgumentException>(
            () => new CombatRecommendationScoringRequest(
                fixture.Player,
                [fixture.CriticalThreat],
                [candidate],
                RecommendationPolicy.Safe,
                [
                    new CandidateDamageEvidence(
                        "unknown",
                        50,
                        "evidence:damage")
                ]));
    }

    private static CombatRecommendationScoringResult Score(
        PlayerCombatSnapshot player,
        TargetThreat[] targetThreats,
        GeneratedCombatLoadout[] candidates,
        RecommendationPolicy policy,
        CandidateDamageEvidence[]? damageEvidence = null)
    {
        return CombatRecommendationScorer.Score(
            new CombatRecommendationScoringRequest(
                player,
                targetThreats,
                candidates,
                policy,
                damageEvidence));
    }

    private static GeneratedCombatLoadout GenerateSingleton(
        PlayerCombatSnapshot player,
        CombatLoadoutOption option)
    {
        var result = CombatLoadoutGenerator.Generate(
            new CombatLoadoutGenerationRequest(
                player,
                [option],
                CreateContext(),
                player.GenericSlotAllocation));
        return Assert.Single(result.Candidates);
    }

    private static Fixture CreateFixture()
    {
        var defensiveSkill = CreateSkill(100);
        var offensiveSkill = CreateSkill(101);
        var player = CreatePlayer([defensiveSkill, offensiveSkill]);
        var threat = CreateThreat();
        return new Fixture(
            player,
            threat,
            defensiveSkill,
            offensiveSkill,
            Option(
                defensiveSkill,
                threatCodes: [threat.Code],
                strength: CombatCounterStrength.HardCounter),
            Option(offensiveSkill));
    }

    private static TargetThreat CreateThreat()
    {
        return new TargetThreat(
            "MIND_RESONANCE_CASCADE",
            TargetThreatKind.MindResonanceCascade,
            TargetThreatSeverity.Critical,
            "Mind resonance",
            "Repeated mind-loss pressure.",
            TargetThreatActivationTiming.OnMarkApplied,
            [
                new TargetThreatEvidence(
                    "evidence:threat",
                    "Verified rule.",
                    TargetThreatEvidenceConfidence.VerifiedRule)
            ]);
    }

    private static CombatLoadoutOption Option(
        CombatSkillSnapshot skill,
        string[]? threatCodes = null,
        CombatCounterStrength? strength = null,
        CombatRequirement[]? requirements = null,
        bool isCurrentlyEquipped = false)
    {
        return new CombatLoadoutOption(
            new CombatSkillCandidate(skill.SkillId),
            requirements ?? [],
            threatCodes ?? [],
            isCurrentlyEquipped,
            $"snapshot:skill:{skill.SkillId}",
            strength,
            strength.HasValue
                ? CombatCounterActivationTiming.ActiveAttack
                : null);
    }

    private static CombatSkillSnapshot CreateSkill(int skillId)
    {
        return new CombatSkillSnapshot(
            skillId,
            SnapshotValue<string>.Available($"Skill {skillId}"),
            SkillCategory.Attack,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(false),
            SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Direct),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(1000 + skillId),
            SnapshotValue<int>.Available(2000 + skillId));
    }

    private static CombatRequirementContext CreateContext()
    {
        return new CombatRequirementContext(
            equippedWeaponTypeIds: [],
            trickCounts: [],
            SnapshotValue<int>.Available(0),
            resources: [],
            unlockedWeaponTypeIds: [],
            equippedSkillIds: []);
    }

    private static PlayerCombatSnapshot CreatePlayer(
        CombatSkillSnapshot[] skills,
        CombatLoadoutSnapshot? loadout = null)
    {
        return new PlayerCombatSnapshot(
            characterId: 1,
            SnapshotValue<string>.Available("Taiwu"),
            skills,
            loadout ?? new CombatLoadoutSnapshot([], [], [], [], []),
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

    private sealed record Fixture(
        PlayerCombatSnapshot Player,
        TargetThreat CriticalThreat,
        CombatSkillSnapshot DefensiveSkill,
        CombatSkillSnapshot OffensiveSkill,
        CombatLoadoutOption DefensiveOption,
        CombatLoadoutOption OffensiveOption);
}
