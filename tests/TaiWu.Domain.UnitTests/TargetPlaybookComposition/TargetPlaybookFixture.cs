using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.UnitTests.TargetPlaybookCompositions;

internal static class TargetPlaybookFixture
{
    internal const string GameVersion =
        VerifiedTargetProfileExtractionRuleSets.SupportedGameDataVersion;

    internal static TargetCombatProfileAnalysis FullAnalysis(
        IEnumerable<TargetArchetypeDefinition>? definitions = null,
        bool includeObservation = true)
    {
        var directMagicSound = Skill(
            718,
            PracticeDirection.Direct,
            directEffectId: 668,
            reverseEffectId: 1394,
            outer: true,
            poison: true);
        var reverseReset = Skill(
            287,
            PracticeDirection.Reverse,
            directEffectId: 185,
            reverseEffectId: 911,
            outer: false,
            poison: false);
        var observation = includeObservation
            ? new TargetLoadoutObservation(
                42,
                TargetObservationContext.Hostile,
                DateTimeOffset.Parse("2026-08-10T13:00:00Z"),
                "E5-COMPOSITION-OBSERVATION",
                TargetLoadoutCoverage.PartialLoadout,
                [new ObservedTargetCombatSkill(
                    718,
                    SkillCategory.Attack,
                    PracticeDirection.Direct,
                    slotIndex: 0)])
            : null;
        var snapshot = Snapshot(
            [directMagicSound, reverseReset],
            [718, 287],
            observation,
            resistance: new TargetChannelResistanceSnapshot(1200, 800),
            equipment:
            [
                new EquipmentSnapshot(
                    0,
                    SnapshotValue<long>.Available(100),
                    SnapshotValue<int>.Available(200),
                    SnapshotValue<string>.Available("Weapon"),
                    SnapshotValue<EquipmentKind>.Available(
                        EquipmentKind.Weapon),
                    SnapshotValue<int>.Available(16))
            ]);

        return TargetCombatProfileAnalyzer.Analyze(
            snapshot,
            VerifiedTargetThreatRuleSets.GoldenMagicSound,
            VerifiedTargetProfileExtractionRuleSets.Initial,
            definitions
                ?? TaiWu.Domain.TargetPlaybooks
                    .VerifiedTargetCounterPlaybooks.Initial.Archetypes);
    }

    internal static TargetCombatProfileAnalysis OuterOnlyAnalysis(
        bool includeExtraThreat)
    {
        var skill = Skill(
            900,
            PracticeDirection.Direct,
            directEffectId: 1900,
            reverseEffectId: 2900,
            outer: true,
            poison: false);
        var threatRules = includeExtraThreat
            ? ExtraThreatRules(skill)
            : new TargetThreatRuleSet(
                GameVersion,
                relevantSkillIds: [],
                rules: []);

        return TargetCombatProfileAnalyzer.Analyze(
            Snapshot(
                [skill],
                [skill.SkillId],
                observation: null,
                resistance: new TargetChannelResistanceSnapshot(100, 100),
                equipment: []),
            threatRules,
            VerifiedTargetProfileExtractionRuleSets.Initial,
            TaiWu.Domain.TargetPlaybooks
                .VerifiedTargetCounterPlaybooks.Initial.Archetypes);
    }

    internal static CombatSnapshot Snapshot(
        IEnumerable<CombatSkillSnapshot> targetSkills,
        int[] equippedAttackSkills,
        TargetLoadoutObservation? observation,
        TargetChannelResistanceSnapshot resistance,
        IEnumerable<EquipmentSnapshot> equipment) => new(
            new CombatSnapshotMetadata(
                new string('A', 64),
                DateTimeOffset.Parse("2026-08-10T12:00:00Z"),
                SnapshotValue<DateTimeOffset>.Available(
                    DateTimeOffset.Parse("2026-08-10T11:00:00Z")),
                SnapshotValue<string>.Available(GameVersion)),
            Player(),
            new TargetCombatSnapshot(
                42,
                SnapshotValue<string>.Available("Target"),
                SnapshotValue<int>.Available(40),
                features: [],
                targetSkills,
                SnapshotValue<CombatLoadoutSnapshot>.Available(
                    new CombatLoadoutSnapshot(
                        neigongSkillIds: [],
                        equippedAttackSkills,
                        agilitySkillIds: [],
                        defenseSkillIds: [],
                        assistanceSkillIds: [])),
                equipment,
                observation,
                SnapshotValue<TargetChannelResistanceSnapshot>.Available(
                    resistance)),
            warnings: []);

    internal static TargetArchetypeDefinition Definition(string code) => new(
        new TargetArchetypeIdentity(
            code,
            new TargetProfileVersion("1.0.0")),
        VerifiedTargetProfileExtractionRuleSets.Initial.RuleVersion,
        $"TargetArchetype.{code}.Title",
        [new TargetArchetypeFacetPredicate(
            $"REQUIRES_{code}",
            VerifiedTargetProfileExtractionRuleSets.Initial.OuterDamageFacet,
            TargetArchetypePredicateOperator.FacetConfirmed)],
        supportingPredicates: [],
        exclusions: [],
        evidenceReferences: ["E5-005"]);

    internal static TargetArchetypeDefinition ContraryResistanceDefinition()
    {
        var facet = VerifiedTargetProfileExtractionRuleSets
            .Initial
            .ChannelResistanceFacet;
        return new TargetArchetypeDefinition(
            new TargetArchetypeIdentity(
                "CONTRARY_RESISTANCE_ARCHETYPE",
                new TargetProfileVersion("1.0.0")),
            VerifiedTargetProfileExtractionRuleSets.Initial.RuleVersion,
            "TargetArchetype.ContraryResistance.Title",
            [new TargetArchetypeFacetPredicate(
                "REQUIRES_REVERSED_RESISTANCE",
                facet,
                TargetArchetypePredicateOperator.ValueEquals,
                TargetProfileFacetValue.Measured(
                    facet.Dimension,
                    facet.Code,
                    [
                        new TargetProfileMeasurement(
                            "OUTER",
                            800,
                            "RAW_GAME_UNIT"),
                        new TargetProfileMeasurement(
                            "INNER",
                            1200,
                            "RAW_GAME_UNIT")
                    ]))],
            supportingPredicates: [],
            exclusions: [],
            evidenceReferences: ["E5-005"]);
    }

    internal static CombatSkillSnapshot Skill(
        int skillId,
        PracticeDirection direction,
        int directEffectId,
        int reverseEffectId,
        bool outer,
        bool poison) => new(
            skillId,
            SnapshotValue<string>.Available($"Skill {skillId}"),
            SkillCategory.Attack,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(false),
            SnapshotValue<PracticeDirection>.Available(direction),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(directEffectId),
            SnapshotValue<int>.Available(reverseEffectId),
            hasConfiguredOuterDamage: SnapshotValue<bool>.Available(outer),
            hasConfiguredPoisonApplication:
                SnapshotValue<bool>.Available(poison));

    private static TargetThreatRuleSet ExtraThreatRules(
        CombatSkillSnapshot skill)
    {
        var threat = new TargetThreat(
            "EXACT_REPEATED_ATTACK",
            TargetThreatKind.RepeatedAttack,
            TargetThreatSeverity.High,
            "Repeated attack",
            "Synthetic exact target threat outside the delivered playbooks.",
            TargetThreatActivationTiming.OnSkillUse,
            [new TargetThreatEvidence(
                "E5-005",
                "Synthetic reviewed threat.",
                TargetThreatEvidenceConfidence.VerifiedRule)]);
        return new TargetThreatRuleSet(
            GameVersion,
            [skill.SkillId],
            [new TargetThreatRule(
                threat,
                [new TargetThreatSkillSignature(
                    skill.SkillId,
                    PracticeDirection.Direct,
                    skill.DirectEffectId.Value)])]);
    }

    private static PlayerCombatSnapshot Player() => new(
        1,
        SnapshotValue<string>.Available("Taiwu"),
        learnedSkills: [],
        new CombatLoadoutSnapshot([], [], [], [], []),
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
