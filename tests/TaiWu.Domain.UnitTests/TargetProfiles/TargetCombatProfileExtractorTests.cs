using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetProfiles;
using Xunit;

namespace TaiWu.Domain.UnitTests.TargetProfiles;

public sealed class TargetCombatProfileExtractorTests
{
    [Fact]
    public void Saved_active_mechanics_resistance_and_weapon_create_independent_facets()
    {
        var skill = Skill(100, outer: true, poison: true);
        var snapshot = Snapshot(
            [skill],
            Equipped(attack: [skill.SkillId]),
            equipment: [Weapon(slot: 0, templateId: 500, subtype: 16)],
            resistance: AvailableResistance(1200, 800));

        var profile = Extract(snapshot);

        var weapon = AssertConfirmed(
            profile,
            TargetProfileDimension.AttackFamily,
            "WEAPON_SUBTYPE:16");
        var outer = AssertConfirmed(
            profile,
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED");
        var resistance = AssertConfirmed(
            profile,
            TargetProfileDimension.Resilience,
            "CHANNEL_RESISTANCE_ASYMMETRY");
        var poison = AssertConfirmed(
            profile,
            TargetProfileDimension.Control,
            "POISON_APPLICATION_CONFIGURED");

        Assert.Equal(TargetProfileFacetValueKind.Presence, weapon.Value!.Kind);
        Assert.Equal(
            [
                TargetProfileEvidenceSourceKind.SavedEquippedMembership,
                TargetProfileEvidenceSourceKind.InstalledConfiguration
            ],
            outer.Evidence.Select(evidence => evidence.SourceKind));
        Assert.Equal(
            ["INNER", "OUTER"],
            resistance.Value!.Measurements.Select(value => value.Code));
        Assert.Equal(TargetProfileFacetValueKind.Presence, poison.Value!.Kind);
    }

    [Fact]
    public void Learned_only_skill_never_creates_an_active_mechanic_facet()
    {
        var learned = Skill(100, outer: true, poison: true);
        var snapshot = Snapshot(
            [learned],
            Equipped(),
            resistance: AvailableResistance(100, 100));

        var profile = Extract(snapshot);

        Assert.Null(profile.FindFacet(
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED"));
        Assert.Null(profile.FindFacet(
            TargetProfileDimension.Control,
            "POISON_APPLICATION_CONFIGURED"));
    }

    [Fact]
    public void Missing_active_binding_is_incomplete_instead_of_zero_or_negative()
    {
        var learned = Skill(100, outer: true, poison: true);
        var snapshot = Snapshot(
            [learned],
            SnapshotValue<CombatLoadoutSnapshot>.Unavailable(
                "The saved target loadout is unavailable."),
            resistance: UnavailableResistance());

        var profile = Extract(snapshot);

        var outer = Assert.IsType<TargetProfileFacet>(profile.FindFacet(
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED"));
        var poison = Assert.IsType<TargetProfileFacet>(profile.FindFacet(
            TargetProfileDimension.Control,
            "POISON_APPLICATION_CONFIGURED"));
        var resistance = Assert.IsType<TargetProfileFacet>(profile.FindFacet(
            TargetProfileDimension.Resilience,
            "CHANNEL_RESISTANCE_ASYMMETRY"));
        Assert.Equal(TargetProfileEvidenceState.Incomplete, outer.State);
        Assert.Equal(TargetProfileEvidenceState.Incomplete, poison.State);
        Assert.Equal(TargetProfileEvidenceState.Incomplete, resistance.State);
        Assert.Null(outer.Value);
        Assert.Null(poison.Value);
        Assert.Null(resistance.Value);
    }

    [Fact]
    public void Complete_current_screen_observation_replaces_saved_active_binding()
    {
        var saved = Skill(100, outer: true, poison: false);
        var observed = Skill(101, outer: false, poison: true);
        var baseline = Snapshot(
            [saved, observed],
            Equipped(attack: [saved.SkillId]),
            resistance: AvailableResistance(100, 100));
        var observation = Observation(
            TargetLoadoutCoverage.CompleteCurrentLoadout(
                TargetLoadoutCompletenessEvidence.FromE3000(GameVersion)),
            TargetObservationContext.Sparring,
            new ObservedTargetCombatSkill(
                observed.SkillId,
                SkillCategory.Attack,
                PracticeDirection.Direct,
                slotIndex: 0));

        var merge = TargetLoadoutObservationMerger.Merge(
            baseline,
            observation);
        var baselineProfile = Extract(baseline);
        var observedProfile = Extract(merge.Snapshot);

        AssertConfirmed(
            baselineProfile,
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED");
        Assert.Null(baselineProfile.FindFacet(
            TargetProfileDimension.Control,
            "POISON_APPLICATION_CONFIGURED"));
        Assert.Null(observedProfile.FindFacet(
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED"));
        var poison = AssertConfirmed(
            observedProfile,
            TargetProfileDimension.Control,
            "POISON_APPLICATION_CONFIGURED");
        Assert.Contains(
            poison.Evidence,
            evidence => evidence.SourceKind ==
                TargetProfileEvidenceSourceKind.CurrentScreenObservation);
        Assert.Contains(
            observedProfile.Diagnostics,
            diagnostic => diagnostic.Code ==
                CombatSnapshotWarningCodes.TargetObservationSaveConflict);
    }

    [Fact]
    public void Partial_hostile_observation_adds_visible_active_skill_without_erasing_save()
    {
        var saved = Skill(100, outer: true, poison: false);
        var observed = Skill(101, outer: false, poison: true);
        var baseline = Snapshot(
            [saved, observed],
            Equipped(attack: [saved.SkillId]),
            resistance: AvailableResistance(100, 100));
        var observation = Observation(
            TargetLoadoutCoverage.PartialLoadout,
            TargetObservationContext.Hostile,
            new ObservedTargetCombatSkill(
                observed.SkillId,
                SkillCategory.Attack,
                PracticeDirection.Direct));

        var merged = TargetLoadoutObservationMerger.Merge(
            baseline,
            observation).Snapshot;
        var profile = Extract(merged);

        AssertConfirmed(
            profile,
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED");
        AssertConfirmed(
            profile,
            TargetProfileDimension.Control,
            "POISON_APPLICATION_CONFIGURED");
        Assert.Contains(
            profile.Diagnostics,
            diagnostic => diagnostic.Code ==
                CombatSnapshotWarningCodes.TargetObservationPartial);
    }

    [Fact]
    public void Repeated_observation_is_idempotent_and_clear_restores_baseline()
    {
        var saved = Skill(100, outer: true, poison: false);
        var observed = Skill(101, outer: false, poison: true);
        var baseline = Snapshot(
            [saved, observed],
            Equipped(attack: [saved.SkillId]),
            resistance: AvailableResistance(100, 100));
        var observation = Observation(
            TargetLoadoutCoverage.CompleteCurrentLoadout(
                TargetLoadoutCompletenessEvidence.FromE3000(GameVersion)),
            TargetObservationContext.Sparring,
            new ObservedTargetCombatSkill(
                observed.SkillId,
                SkillCategory.Attack,
                PracticeDirection.Direct,
                slotIndex: 0));

        var first = TargetLoadoutObservationMerger.Merge(
            baseline,
            observation).Snapshot;
        var repeatedFromBaseline = TargetLoadoutObservationMerger.Merge(
            baseline,
            observation).Snapshot;
        var repeatedFromMerged = TargetLoadoutObservationMerger.Merge(
            first,
            observation).Snapshot;

        var baselineProfile = Extract(baseline);
        var firstProfile = Extract(first);
        var repeatedBaselineProfile = Extract(repeatedFromBaseline);
        var repeatedMergedProfile = Extract(repeatedFromMerged);
        var clearedProfile = Extract(baseline);
        TargetArchetypeDefinition[] definitions =
        [
            Definition(
                "OUTER_DAMAGE_PRESSURE",
                TargetProfileDimension.Pressure,
                "OUTER_DAMAGE_CONFIGURED"),
            Definition(
                "POISON_APPLICATION",
                TargetProfileDimension.Control,
                "POISON_APPLICATION_CONFIGURED")
        ];
        var baselineMatches = TargetArchetypeMatcher.Match(
            baselineProfile,
            definitions);
        var firstMatches = TargetArchetypeMatcher.Match(
            firstProfile,
            definitions);
        var repeatedBaselineMatches = TargetArchetypeMatcher.Match(
            repeatedBaselineProfile,
            definitions);
        var repeatedMergedMatches = TargetArchetypeMatcher.Match(
            repeatedMergedProfile,
            definitions);
        var clearedMatches = TargetArchetypeMatcher.Match(
            clearedProfile,
            definitions);

        Assert.Equal(firstProfile.Fingerprint, repeatedBaselineProfile.Fingerprint);
        Assert.Equal(firstProfile.Fingerprint, repeatedMergedProfile.Fingerprint);
        Assert.Equal(baselineProfile.Fingerprint, clearedProfile.Fingerprint);
        Assert.NotEqual(baselineProfile.Fingerprint, firstProfile.Fingerprint);
        Assert.Equal(firstMatches.StableKey, repeatedBaselineMatches.StableKey);
        Assert.Equal(firstMatches.StableKey, repeatedMergedMatches.StableKey);
        Assert.Equal(baselineMatches.StableKey, clearedMatches.StableKey);
        Assert.NotEqual(baselineMatches.StableKey, firstMatches.StableKey);
    }

    [Fact]
    public void Stale_observation_preserves_save_profile_and_diagnostic()
    {
        var saved = Skill(100, outer: true, poison: false);
        var observed = Skill(101, outer: false, poison: true);
        var baseline = Snapshot(
            [saved, observed],
            Equipped(attack: [saved.SkillId]),
            resistance: AvailableResistance(100, 100));
        var stale = new TargetLoadoutObservation(
            TargetId,
            TargetObservationContext.Sparring,
            DateTimeOffset.Parse("2026-08-09T10:00:00Z"),
            "E5-STALE-001",
            TargetLoadoutCoverage.PartialLoadout,
            [new ObservedTargetCombatSkill(
                observed.SkillId,
                SkillCategory.Attack,
                PracticeDirection.Direct)]);

        var merge = TargetLoadoutObservationMerger.Merge(baseline, stale);
        var baselineProfile = Extract(baseline);
        var staleProfile = Extract(merge.Snapshot);

        Assert.Equal(
            baselineProfile.Facets.Select(facet => facet.Identity),
            staleProfile.Facets.Select(facet => facet.Identity));
        Assert.Contains(
            staleProfile.Diagnostics,
            diagnostic => diagnostic.Code ==
                CombatSnapshotWarningCodes.TargetObservationNotNewer);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Unsupported_or_unavailable_version_emits_no_partial_facets(
        bool unavailable)
    {
        var snapshot = Snapshot(
            [Skill(100, outer: true, poison: true)],
            Equipped(attack: [100]),
            resistance: AvailableResistance(1200, 800),
            gameDataVersion: unavailable
                ? SnapshotValue<string>.Unavailable("Version unavailable.")
                : SnapshotValue<string>.Available("9.9.9"));

        var profile = Extract(snapshot);

        Assert.Empty(profile.Facets);
        Assert.Single(profile.Diagnostics);
        Assert.Equal(
            unavailable
                ? TargetCombatProfileExtractor.GameDataVersionUnavailableCode
                : TargetCombatProfileExtractor.GameDataVersionUnsupportedCode,
            profile.Diagnostics[0].Code);
    }

    [Fact]
    public void Active_typed_threat_creates_facet_but_learned_only_threat_does_not()
    {
        var skill = Skill(
            200,
            outer: false,
            poison: false,
            effectId: 900);
        var threatRules = ThreatRules(
            skill,
            TargetThreatKind.MindDamagePressure,
            "MIND_DAMAGE");
        var activeSnapshot = Snapshot(
            [skill],
            Equipped(attack: [skill.SkillId]),
            resistance: AvailableResistance(100, 100));
        var learnedSnapshot = Snapshot(
            [skill],
            Equipped(),
            resistance: AvailableResistance(100, 100));

        var active = TargetCombatProfileExtractor.Extract(
            activeSnapshot,
            TargetThreatAnalyzer.Analyze(activeSnapshot, threatRules),
            Rules);
        var learned = TargetCombatProfileExtractor.Extract(
            learnedSnapshot,
            TargetThreatAnalyzer.Analyze(learnedSnapshot, threatRules),
            Rules);

        AssertConfirmed(
            active,
            TargetProfileDimension.Pressure,
            "MIND_DAMAGE_PRESSURE");
        Assert.Null(learned.FindFacet(
            TargetProfileDimension.Pressure,
            "MIND_DAMAGE_PRESSURE"));
        Assert.Contains(
            learned.Diagnostics,
            diagnostic => diagnostic.Code ==
                TargetCombatProfileExtractor.LearnedThreatNotActiveCode);
    }

    [Fact]
    public void Analyzer_extracts_then_evaluates_every_definition()
    {
        var skill = Skill(100, outer: true, poison: true);
        var snapshot = Snapshot(
            [skill],
            Equipped(attack: [skill.SkillId]),
            resistance: AvailableResistance(100, 100));
        var outer = Definition(
            "OUTER_DAMAGE_PRESSURE",
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED");
        var poison = Definition(
            "POISON_APPLICATION",
            TargetProfileDimension.Control,
            "POISON_APPLICATION_CONFIGURED");

        var analysis = TargetCombatProfileAnalyzer.Analyze(
            snapshot,
            EmptyThreatRules,
            Rules,
            [poison, outer]);

        Assert.Equal(analysis.Profile.Fingerprint,
            analysis.ArchetypeMatches.ProfileFingerprint);
        Assert.Equal(2, analysis.ArchetypeMatches.Matched.Length);
        Assert.Equal(
            ["OUTER_DAMAGE_PRESSURE", "POISON_APPLICATION"],
            analysis.ArchetypeMatches.Matches.Select(match =>
                match.Definition.Identity.Code));
    }

    [Fact]
    public void Display_names_do_not_influence_profile_or_matching()
    {
        var firstSkill = Skill(
            100,
            outer: true,
            poison: false,
            displayName: "Localized skill A");
        var secondSkill = Skill(
            100,
            outer: true,
            poison: false,
            displayName: "完全不同的名稱");
        var first = Snapshot(
            [firstSkill],
            Equipped(attack: [100]),
            targetName: "First target",
            resistance: AvailableResistance(100, 100));
        var second = Snapshot(
            [secondSkill],
            Equipped(attack: [100]),
            targetName: "第二目標",
            resistance: AvailableResistance(100, 100));

        var firstProfile = Extract(first);
        var secondProfile = Extract(second);

        Assert.Equal(firstProfile.Fingerprint, secondProfile.Fingerprint);
    }

    [Fact]
    public void Extraction_and_definition_reordering_are_deterministic()
    {
        var outerSkill = Skill(100, outer: true, poison: false);
        var poisonSkill = Skill(101, outer: false, poison: true);
        var first = Snapshot(
            [outerSkill, poisonSkill],
            Equipped(attack: [outerSkill.SkillId, poisonSkill.SkillId]),
            equipment:
            [
                Weapon(1, 501, 17),
                Weapon(0, 500, 16)
            ],
            resistance: AvailableResistance(1200, 800));
        var second = Snapshot(
            [poisonSkill, outerSkill],
            Equipped(attack: [poisonSkill.SkillId, outerSkill.SkillId]),
            equipment:
            [
                Weapon(0, 500, 16),
                Weapon(1, 501, 17)
            ],
            resistance: AvailableResistance(1200, 800));
        var definitions = new[]
        {
            Definition(
                "OUTER_DAMAGE_PRESSURE",
                TargetProfileDimension.Pressure,
                "OUTER_DAMAGE_CONFIGURED"),
            Definition(
                "POISON_APPLICATION",
                TargetProfileDimension.Control,
                "POISON_APPLICATION_CONFIGURED")
        };

        var firstResult = TargetCombatProfileAnalyzer.Analyze(
            first,
            EmptyThreatRules,
            Rules,
            definitions);
        var secondResult = TargetCombatProfileAnalyzer.Analyze(
            second,
            EmptyThreatRules,
            Rules,
            definitions.Reverse());

        Assert.Equal(
            firstResult.Profile.Fingerprint,
            secondResult.Profile.Fingerprint);
        Assert.Equal(
            firstResult.ArchetypeMatches.StableKey,
            secondResult.ArchetypeMatches.StableKey);
    }

    [Fact]
    public void New_snapshot_facts_enforce_positive_and_unavailable_boundaries()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TargetChannelResistanceSnapshot(0, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EquipmentSnapshot(
            0,
            SnapshotValue<long>.Available(1),
            SnapshotValue<int>.Available(1),
            SnapshotValue<string>.Available("Weapon"),
            SnapshotValue<EquipmentKind>.Available(EquipmentKind.Weapon),
            SnapshotValue<int>.Available(0)));
        Assert.Throws<ArgumentException>(() => new EquipmentSnapshot(
            0,
            SnapshotValue<long>.Available(1),
            SnapshotValue<int>.Available(1),
            SnapshotValue<string>.Available("Armor"),
            SnapshotValue<EquipmentKind>.Available(EquipmentKind.Armor),
            SnapshotValue<int>.Available(16)));
        var legacySkill = Skill(
            999,
            outer: null,
            poison: null);
        Assert.False(legacySkill.HasConfiguredOuterDamage.IsAvailable);
        Assert.False(legacySkill.HasConfiguredPoisonApplication.IsAvailable);
    }

    private const int TargetId = 42;

    private const string GameVersion =
        VerifiedTargetProfileExtractionRuleSets.SupportedGameDataVersion;

    private static TargetProfileExtractionRuleSet Rules =>
        VerifiedTargetProfileExtractionRuleSets.Initial;

    private static TargetThreatRuleSet EmptyThreatRules => new(
        GameVersion,
        relevantSkillIds: [],
        rules: []);

    private static TargetCombatProfile Extract(CombatSnapshot snapshot)
    {
        var threats = TargetThreatAnalyzer.Analyze(snapshot, EmptyThreatRules);
        return TargetCombatProfileExtractor.Extract(snapshot, threats, Rules);
    }

    private static TargetProfileFacet AssertConfirmed(
        TargetCombatProfile profile,
        TargetProfileDimension dimension,
        string code)
    {
        var facet = Assert.IsType<TargetProfileFacet>(
            profile.FindFacet(dimension, code));
        Assert.Equal(TargetProfileEvidenceState.Confirmed, facet.State);
        Assert.NotNull(facet.Value);
        return facet;
    }

    private static CombatSnapshot Snapshot(
        IEnumerable<CombatSkillSnapshot> targetSkills,
        SnapshotValue<CombatLoadoutSnapshot> equipped,
        IEnumerable<EquipmentSnapshot>? equipment = null,
        SnapshotValue<TargetChannelResistanceSnapshot>? resistance = null,
        SnapshotValue<string>? gameDataVersion = null,
        string targetName = "Target",
        IEnumerable<SnapshotWarning>? warnings = null) => new(
            new CombatSnapshotMetadata(
                @"C:\local\save.sav",
                new string('A', 64),
                DateTimeOffset.Parse("2026-08-10T12:00:00Z"),
                SnapshotValue<DateTimeOffset>.Available(
                    DateTimeOffset.Parse("2026-08-10T11:00:00Z")),
                gameDataVersion
                    ?? SnapshotValue<string>.Available(GameVersion)),
            Player(),
            new TargetCombatSnapshot(
                TargetId,
                SnapshotValue<string>.Available(targetName),
                SnapshotValue<int>.Available(40),
                features: [],
                targetSkills,
                equipped,
                equipment ?? [],
                loadoutObservation: null,
                resistance ?? AvailableResistance(100, 100)),
            warnings ?? []);

    private static CombatSkillSnapshot Skill(
        int skillId,
        bool? outer,
        bool? poison,
        int effectId = 1000,
        string? displayName = null) => new(
            skillId,
            SnapshotValue<string>.Available(
                displayName ?? $"Skill {skillId}"),
            SkillCategory.Attack,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(false),
            SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Direct),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(effectId),
            SnapshotValue<int>.Available(effectId + 1),
            hasConfiguredOuterDamage: outer.HasValue
                ? SnapshotValue<bool>.Available(outer.Value)
                : null,
            hasConfiguredPoisonApplication: poison.HasValue
                ? SnapshotValue<bool>.Available(poison.Value)
                : null);

    private static SnapshotValue<CombatLoadoutSnapshot> Equipped(
        int[]? attack = null) => SnapshotValue<CombatLoadoutSnapshot>.Available(
            new CombatLoadoutSnapshot(
                neigongSkillIds: [],
                attack ?? [],
                agilitySkillIds: [],
                defenseSkillIds: [],
                assistanceSkillIds: []));

    private static EquipmentSnapshot Weapon(
        int slot,
        int templateId,
        int subtype) => new(
            slot,
            SnapshotValue<long>.Available(slot + 1),
            SnapshotValue<int>.Available(templateId),
            SnapshotValue<string>.Available("Weapon"),
            SnapshotValue<EquipmentKind>.Available(EquipmentKind.Weapon),
            SnapshotValue<int>.Available(subtype));

    private static SnapshotValue<TargetChannelResistanceSnapshot>
        AvailableResistance(int outer, int inner) =>
            SnapshotValue<TargetChannelResistanceSnapshot>.Available(
                new TargetChannelResistanceSnapshot(outer, inner));

    private static SnapshotValue<TargetChannelResistanceSnapshot>
        UnavailableResistance() =>
            SnapshotValue<TargetChannelResistanceSnapshot>.Unavailable(
                "Base resistance is unavailable.");

    private static TargetLoadoutObservation Observation(
        TargetLoadoutCoverage coverage,
        TargetObservationContext context,
        params ObservedTargetCombatSkill[] skills) => new(
            TargetId,
            context,
            DateTimeOffset.Parse("2026-08-10T12:30:00Z"),
            "E5-OBSERVATION-001",
            coverage,
            skills);

    private static TargetThreatRuleSet ThreatRules(
        CombatSkillSnapshot skill,
        TargetThreatKind kind,
        string code)
    {
        var threat = new TargetThreat(
            code,
            kind,
            TargetThreatSeverity.Critical,
            title: code,
            explanation: "Verified synthetic threat.",
            TargetThreatActivationTiming.OnSkillUse,
            [
                new TargetThreatEvidence(
                    "E5-THREAT-001",
                    "Verified synthetic rule.",
                    TargetThreatEvidenceConfidence.VerifiedRule)
            ]);
        return new TargetThreatRuleSet(
            GameVersion,
            [skill.SkillId],
            [
                new TargetThreatRule(
                    threat,
                    [new TargetThreatSkillSignature(
                        skill.SkillId,
                        PracticeDirection.Direct,
                        skill.DirectEffectId.Value)])
            ]);
    }

    private static TargetArchetypeDefinition Definition(
        string code,
        TargetProfileDimension dimension,
        string facetCode) => new(
            new TargetArchetypeIdentity(
                code,
                new TargetProfileVersion("1.0.0")),
            new TargetProfileVersion(
                VerifiedTargetProfileExtractionRuleSets.InitialRuleVersion),
            $"TargetArchetype.{code}.Title",
            [
                new TargetArchetypeFacetPredicate(
                    $"REQUIRES_{facetCode}",
                    new TargetProfileFacetIdentity(dimension, facetCode),
                    TargetArchetypePredicateOperator.FacetConfirmed)
            ],
            supportingPredicates: [],
            exclusions: [],
            evidenceReferences: ["E5-000"]);

    private static PlayerCombatSnapshot Player() => new(
        characterId: 1,
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
