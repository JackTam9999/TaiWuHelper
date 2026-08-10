using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybooks;

public static class VerifiedTargetCounterPlaybooks
{
    public const string InitialArchetypeVersion = "1.0.0";

    public const string InitialPlaybookVersion = "1.0.0";

    public static TargetCounterPlaybookCatalog Initial { get; } =
        CreateInitial();

    private static TargetCounterPlaybookCatalog CreateInitial()
    {
        var extractionRules = VerifiedTargetProfileExtractionRuleSets.Initial;
        var threatSet = VerifiedTargetThreatTaxonomies.GoldenMagicSound;
        var familyThreats = VerifiedTargetThreatTaxonomies
            .Epic5TargetFamilies;
        var counterRules = VerifiedCombatCounterRuleSets.GoldenMagicSound;
        var familyCounterRules = VerifiedCombatCounterRuleSets
            .Epic5TargetFamilies;

        var mindDamage = Threat(threatSet, "POSITIVE_MAGIC_SOUND_MIND_DAMAGE");
        var distraction = Threat(threatSet, "DISTRACTION_MARK_ACCUMULATION");
        var resonance = Threat(threatSet, "MIND_RESONANCE_CASCADE");
        var reset = Threat(threatSet, "DEFEAT_MARK_RESET_LOOP");
        var outerPressure = Threat(
            familyThreats,
            "CONFIGURED_OUTER_DAMAGE_PRESSURE");
        var channelAsymmetry = Threat(
            familyThreats,
            "CHANNEL_RESISTANCE_ASYMMETRY");
        var poisonApplication = Threat(
            familyThreats,
            "CONFIGURED_POISON_APPLICATION");

        var mindFacet = FacetFor(
            extractionRules,
            TargetThreatKind.MindDamagePressure);
        var distractionFacet = FacetFor(
            extractionRules,
            TargetThreatKind.DistractionMarkAccumulation);
        var resonanceFacet = FacetFor(
            extractionRules,
            TargetThreatKind.MindResonanceCascade);
        var resetFacet = FacetFor(
            extractionRules,
            TargetThreatKind.DefeatMarkReset);

        var baseline = Archetype(
            "MIND_RESONANCE_BASELINE",
            "TargetArchetype.MindResonanceBaseline.Title",
            [mindFacet, distractionFacet, resonanceFacet],
            ["E5-000", "M1-001"]);
        var resetOverlay = Archetype(
            "DEFEAT_MARK_RESET_OVERLAY",
            "TargetArchetype.DefeatMarkResetOverlay.Title",
            [resetFacet],
            ["E5-011", "M1-001"]);
        var outer = Archetype(
            "OUTER_DAMAGE_CONFIGURED",
            "TargetArchetype.OuterDamageConfigured.Title",
            [extractionRules.OuterDamageFacet],
            ["E5-000"]);
        var resistance = Archetype(
            "CHANNEL_RESISTANCE_ASYMMETRY",
            "TargetArchetype.ChannelResistanceAsymmetry.Title",
            [extractionRules.ChannelResistanceFacet],
            ["E5-000"]);
        var poison = Archetype(
            "POISON_APPLICATION_CONFIGURED",
            "TargetArchetype.PoisonApplicationConfigured.Title",
            [extractionRules.PoisonApplicationFacet],
            ["E5-000"]);

        return new TargetCounterPlaybookCatalog(
            extractionRules.GameDataVersion,
            [baseline, resetOverlay, outer, resistance, poison],
            [counterRules, familyCounterRules],
            [
                MindPlaybook(
                    baseline.Identity,
                    counterRules,
                    mindFacet,
                    distractionFacet,
                    resonanceFacet,
                    mindDamage,
                    distraction,
                    resonance),
                ResetPlaybook(
                    resetOverlay.Identity,
                    counterRules,
                    resetFacet,
                    reset),
                Playbook(
                    outer.Identity,
                    [Goal(
                        "PREPARE_FOR_OUTER_DAMAGE",
                        10,
                        TargetResponsePriority.High,
                        CombatCounterActivationTiming.ActiveAttack,
                        [extractionRules.OuterDamageFacet],
                        [outerPressure],
                        Options(
                            counterRules,
                            "REVERSE_FULONG_POWER_REDUCTION"),
                        ["OUTER_DAMAGE_RESPONSE"])],
                    ["E5-011", "E5-000"]),
                Playbook(
                    resistance.Identity,
                    [Goal(
                        "EXPLOIT_LESS_RESISTED_CHANNEL",
                        10,
                        TargetResponsePriority.High,
                        CombatCounterActivationTiming.ActiveAttack,
                        [extractionRules.ChannelResistanceFacet],
                        [channelAsymmetry],
                        Options(
                            familyCounterRules,
                            "DIRECT_YINYANG_ROUTE_OUTER_TO_INNER",
                            "REVERSE_YINYANG_ROUTE_INNER_TO_OUTER"),
                        ["PRIMARY_DAMAGE_CHANNEL"])],
                    ["E5-011", "E5-000"]),
                Playbook(
                    poison.Identity,
                    [Goal(
                        "MITIGATE_CONFIGURED_POISON_APPLICATION",
                        10,
                        TargetResponsePriority.High,
                        CombatCounterActivationTiming.ActiveDefense,
                        [extractionRules.PoisonApplicationFacet],
                        [poisonApplication],
                        Options(
                            familyCounterRules,
                            "DIRECT_WUHUANG_POISON_DEFENSE",
                            "REVERSE_WUHUANG_POISON_DEFENSE"),
                        ["POISON_RESPONSE"])],
                    ["E5-011", "E5-000"])
            ]);
    }

    private static TargetCounterPlaybook MindPlaybook(
        TargetArchetypeIdentity identity,
        CombatCounterRuleSet counterRules,
        TargetProfileFacetIdentity mindFacet,
        TargetProfileFacetIdentity distractionFacet,
        TargetProfileFacetIdentity resonanceFacet,
        TargetThreat mindDamage,
        TargetThreat distraction,
        TargetThreat resonance)
    {
        return Playbook(
            identity,
            [
                Goal(
                    "SURVIVE_MIND_DAMAGE_PRESSURE",
                    10,
                    TargetResponsePriority.Critical,
                    CombatCounterActivationTiming.CombatStartPassive,
                    [mindFacet],
                    [mindDamage],
                    Options(
                        counterRules,
                        "REVERSE_JINNI_SUPPRESSION",
                        "REVERSE_FULONG_POWER_REDUCTION"),
                    ["MIND_DAMAGE_RESPONSE"]),
                Goal(
                    "CONTROL_DISTRACTION_MARKS",
                    20,
                    TargetResponsePriority.Critical,
                    CombatCounterActivationTiming.CombatStartPassive,
                    [distractionFacet],
                    [distraction],
                    Options(
                        counterRules,
                        "REVERSE_JINNI_SUPPRESSION",
                        "REVERSE_LAOJUN_MARK_CLEAR",
                        "DIRECT_MOYU_MARK_DURATION",
                        "REVERSE_FULONG_POWER_REDUCTION"),
                    ["DISTRACTION_MARK_RESPONSE"]),
                Goal(
                    "BREAK_MIND_RESONANCE_CASCADE",
                    30,
                    TargetResponsePriority.Critical,
                    CombatCounterActivationTiming.CombatStartPassive,
                    [resonanceFacet],
                    [resonance],
                    Options(
                        counterRules,
                        "REVERSE_JINNI_SUPPRESSION",
                        "REVERSE_LAOJUN_MARK_CLEAR",
                        "REVERSE_WANHUA_RESONANCE",
                        "DIRECT_MOYU_MARK_DURATION"),
                    ["MIND_RESONANCE_RESPONSE"])
            ],
            ["E5-000", "M1-001"]);
    }

    private static TargetCounterPlaybook ResetPlaybook(
        TargetArchetypeIdentity identity,
        CombatCounterRuleSet counterRules,
        TargetProfileFacetIdentity resetFacet,
        TargetThreat reset)
    {
        return Playbook(
            identity,
            [Goal(
                "PRESSURE_DEFEAT_MARK_RESET",
                40,
                TargetResponsePriority.Critical,
                CombatCounterActivationTiming.EquippedPassive,
                [resetFacet],
                [reset],
                Options(
                    counterRules,
                    "REVERSE_QILUN_TRUE_QI_DRAIN"),
                ["DEFEAT_RESET_RESPONSE"],
                [new TargetCounterPlaybookGap(
                    "NO_GUARANTEED_RESET_LOCKOUT",
                    TargetCounterPlaybookGapKind.IncompleteEvidence,
                    "TargetPlaybook.Gap.NoGuaranteedResetLockout",
                    ["M1-001"])])],
            ["E5-011", "M1-001"]);
    }

    private static TargetArchetypeDefinition Archetype(
        string code,
        string titleKey,
        TargetProfileFacetIdentity[] requiredFacets,
        string[] evidenceReferences)
    {
        return new TargetArchetypeDefinition(
            new TargetArchetypeIdentity(
                code,
                new TargetProfileVersion(InitialArchetypeVersion)),
            VerifiedTargetProfileExtractionRuleSets.Initial.RuleVersion,
            titleKey,
            requiredFacets.Select((facet, index) =>
                new TargetArchetypeFacetPredicate(
                    $"REQUIRES_{index + 1}_{facet.Code}",
                    facet,
                    TargetArchetypePredicateOperator.FacetConfirmed)),
            supportingPredicates: [],
            exclusions: [],
            evidenceReferences);
    }

    private static TargetCounterPlaybook Playbook(
        TargetArchetypeIdentity identity,
        TargetCounterPlaybookGoal[] goals,
        string[] evidenceReferences)
    {
        return new TargetCounterPlaybook(
            new TargetCounterPlaybookIdentity(
                identity,
                new TargetProfileVersion(InitialPlaybookVersion)),
            goals,
            evidenceReferences);
    }

    private static TargetCounterPlaybookGoal Goal(
        string code,
        int sequence,
        TargetResponsePriority priority,
        CombatCounterActivationTiming timing,
        TargetProfileFacetIdentity[] facets,
        TargetThreat[] threats,
        TargetCounterPlaybookOption[] options,
        string[] conflictGroups,
        TargetCounterPlaybookGap[]? gaps = null)
    {
        return new TargetCounterPlaybookGoal(
            code,
            sequence,
            priority,
            timing,
            facets,
            threats,
            options,
            conflictGroups,
            ["E5-000"],
            gaps ?? []);
    }

    private static TargetCounterPlaybookOption[] Options(
        CombatCounterRuleSet ruleSet,
        params string[] codes)
    {
        return
        [
            .. codes.Select(code =>
            {
                var rule = ruleSet.Rules.Single(rule => rule.Code == code);
                var conflictGroups = rule.ActivationTiming switch
                {
                    CombatCounterActivationTiming.ActiveAttack =>
                        new[] { "ACTIVE_ATTACK_ROLE" },
                    CombatCounterActivationTiming.ActiveDefense =>
                        ["ACTIVE_DEFENSE_ROLE"],
                    CombatCounterActivationTiming.ActiveAgility =>
                        ["ACTIVE_AGILITY_ROLE"],
                    _ => []
                };
                return new TargetCounterPlaybookOption(
                    rule,
                    conflictGroups);
            })
        ];
    }

    private static TargetProfileFacetIdentity FacetFor(
        TargetProfileExtractionRuleSet rules,
        TargetThreatKind kind)
    {
        return rules.ThreatFacetRules.Single(rule =>
            rule.ThreatKind == kind).Facet;
    }

    private static TargetThreat Threat(
        TargetThreatSet threats,
        string code)
    {
        return threats.Threats.Single(threat => threat.Code == code);
    }
}
