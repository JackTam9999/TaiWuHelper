using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatThreats;

public sealed class TargetThreatAnalyzerTests
{
    private const string Version = "1.0.0+test";
    private const string Evidence = "docs/evidence/target-rule.md";

    [Fact]
    public void Equipped_sources_are_analyzed_before_learned_sources()
    {
        var equipped = CreateSkill(100, effectId: 1000);
        var learned = CreateSkill(101, effectId: 1001);
        var ruleSet = CreateRuleSet(
            [
                Signature(equipped),
                Signature(learned)
            ]);
        var snapshot = CreateSnapshot(
            [learned, equipped],
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                CreateLoadout(attack: [equipped.SkillId])));

        var result = TargetThreatAnalyzer.Analyze(snapshot, ruleSet);

        var threat = Assert.Single(result.Threats);
        Assert.Equal(2, threat.Sources.Length);
        Assert.Equal(
            TargetThreatSourceScope.Equipped,
            threat.Sources[0].Scope);
        Assert.Equal(
            TargetThreatSourceKind.SaveEquipped,
            threat.Sources[0].Kind);
        Assert.Equal(
            $"save:{snapshot.Metadata.SaveSha256}",
            threat.Sources[0].EvidenceReference);
        Assert.Equal(equipped.SkillId, threat.Sources[0].SkillId);
        Assert.Equal(
            TargetThreatSourceScope.LearnedUnequipped,
            threat.Sources[1].Scope);
        Assert.Equal(
            TargetThreatSourceKind.LearnedUnconfirmed,
            threat.Sources[1].Kind);
        Assert.Equal(learned.SkillId, threat.Sources[1].SkillId);
        Assert.All(
            threat.Threat.Evidence,
            evidence => Assert.Equal(
                TargetThreatEvidenceConfidence.VerifiedRule,
                evidence.Confidence));
    }

    [Fact]
    public void Partial_observation_prioritizes_confirmed_equipped_membership()
    {
        var observed = CreateSkill(100, effectId: 1000);
        var learned = CreateSkill(101, effectId: 1001);
        var observation = Observation(
            TargetLoadoutCoverage.PartialLoadout,
            new ObservedTargetCombatSkill(
                observed.SkillId,
                observed.Category,
                direction: null,
                slotIndex: 0));
        var snapshot = CreateSnapshot(
            [learned, observed],
            SnapshotValue<CombatLoadoutSnapshot>.Unavailable(
                "The disk save does not expose the active target loadout."),
            observation: observation);

        var result = TargetThreatAnalyzer.Analyze(
            snapshot,
            CreateRuleSet([Signature(observed), Signature(learned)]));

        var sources = Assert.Single(result.Threats).Sources;
        Assert.Equal([observed.SkillId, learned.SkillId],
            sources.Select(source => source.SkillId));
        Assert.Equal(
            TargetThreatSourceKind.ObservedEquipped,
            sources[0].Kind);
        Assert.Equal(observation.EvidenceReference,
            sources[0].EvidenceReference);
        Assert.Equal(
            TargetThreatSourceKind.LearnedUnconfirmed,
            sources[1].Kind);
    }

    [Theory]
    [InlineData(TargetObservationContext.Hostile)]
    [InlineData(TargetObservationContext.Story)]
    public void Battle_visible_effect_is_confirmed_without_equipped_claim_or_power_scoring(
        TargetObservationContext context)
    {
        var skill = CreateSkill(
            274,
            effectId: 898,
            PracticeDirection.Reverse);
        var firstObservation = Observation(
            TargetLoadoutCoverage.PartialLoadout,
            context,
            new ObservedTargetCombatSkill(
                skill.SkillId,
                skill.Category,
                PracticeDirection.Reverse,
                visiblePowerPercent: 142));
        var secondObservation = Observation(
            TargetLoadoutCoverage.PartialLoadout,
            context,
            new ObservedTargetCombatSkill(
                skill.SkillId,
                skill.Category,
                PracticeDirection.Reverse,
                visiblePowerPercent: 204));
        var ruleSet = CreateRuleSet([Signature(skill)]);

        var first = TargetThreatAnalyzer.Analyze(
            CreateSnapshot(
                [skill],
                SnapshotValue<CombatLoadoutSnapshot>.Available(
                    CreateLoadout()),
                observation: firstObservation),
            ruleSet);
        var second = TargetThreatAnalyzer.Analyze(
            CreateSnapshot(
                [skill],
                SnapshotValue<CombatLoadoutSnapshot>.Available(
                    CreateLoadout()),
                observation: secondObservation),
            ruleSet);

        var source = Assert.Single(Assert.Single(first.Threats).Sources);
        Assert.Equal(
            TargetThreatSourceKind.ObservedActiveEffect,
            source.Kind);
        Assert.Equal(
            TargetThreatSourceScope.BattleVisibleActiveEffect,
            source.Scope);
        Assert.Equal(firstObservation.EvidenceReference,
            source.EvidenceReference);
        Assert.Equal(
            AnalysisFingerprint(first),
            AnalysisFingerprint(second));
    }

    [Fact]
    public void Complete_observation_demotes_stale_saved_membership_and_retains_conflict()
    {
        var saved = CreateSkill(100, effectId: 1000);
        var observed = CreateSkill(101, effectId: 1001);
        var snapshot = CreateSnapshot(
            [saved, observed],
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                CreateLoadout(attack: [saved.SkillId])),
            SnapshotValue<string>.Available(
                TargetLoadoutCompletenessEvidence.E3000GameDataVersion));
        var observation = Observation(
            TargetLoadoutCoverage.CompleteCurrentLoadout(
                TargetLoadoutCompletenessEvidence.FromE3000(
                    TargetLoadoutCompletenessEvidence.E3000GameDataVersion)),
            new ObservedTargetCombatSkill(
                observed.SkillId,
                observed.Category,
                direction: null,
                slotIndex: 0));

        var merge = TargetLoadoutObservationMerger.Merge(
            snapshot,
            observation);
        var result = TargetThreatAnalyzer.Analyze(
            merge.Snapshot,
            new TargetThreatRuleSet(
                TargetLoadoutCompletenessEvidence.E3000GameDataVersion,
                [saved.SkillId, observed.SkillId],
                [Rule(CreateThreat(), Signature(saved), Signature(observed))]));

        Assert.Equal(
            SnapshotEvidenceStatus.Conflicting,
            merge.LoadoutEvidence.Status);
        Assert.Equal(
            [SnapshotDataSource.Save, SnapshotDataSource.CurrentScreenObservation],
            merge.LoadoutEvidence.Observations.Select(value =>
                value.Source.Source));
        var sources = Assert.Single(result.Threats).Sources;
        Assert.Equal([observed.SkillId, saved.SkillId],
            sources.Select(source => source.SkillId));
        Assert.Equal(
            TargetThreatSourceKind.ObservedEquipped,
            sources[0].Kind);
        Assert.Equal(
            TargetThreatSourceKind.LearnedUnconfirmed,
            sources[1].Kind);
    }

    [Fact]
    public void Observed_direction_applies_only_when_available_and_version_matched()
    {
        var skill = CreateSkill(100, effectId: 1000);
        var version = TargetLoadoutCompletenessEvidence.E3000GameDataVersion;
        TargetThreatRule[] rules =
        [
            Rule(
                CreateThreat("DIRECT_THREAT"),
                new TargetThreatSkillSignature(
                    skill.SkillId,
                    PracticeDirection.Direct,
                    1000)),
            Rule(
                CreateThreat("REVERSE_THREAT"),
                new TargetThreatSkillSignature(
                    skill.SkillId,
                    PracticeDirection.Reverse,
                    2000))
        ];
        var ruleSet = new TargetThreatRuleSet(
            version,
            [skill.SkillId],
            rules);
        var snapshot = CreateSnapshot(
            [skill],
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                CreateLoadout(attack: [skill.SkillId])),
            SnapshotValue<string>.Available(version));

        var reversed = TargetLoadoutObservationMerger.Merge(
            snapshot,
            Observation(
                TargetLoadoutCoverage.PartialLoadout,
                new ObservedTargetCombatSkill(
                    skill.SkillId,
                    skill.Category,
                    PracticeDirection.Reverse,
                    slotIndex: 0)));
        var directionOmitted = TargetLoadoutObservationMerger.Merge(
            snapshot,
            Observation(
                TargetLoadoutCoverage.PartialLoadout,
                new ObservedTargetCombatSkill(
                    skill.SkillId,
                    skill.Category,
                    direction: null,
                    slotIndex: 0)));

        var reversedAnalysis = TargetThreatAnalyzer.Analyze(
            reversed.Snapshot,
            ruleSet);
        var unchangedAnalysis = TargetThreatAnalyzer.Analyze(
            directionOmitted.Snapshot,
            ruleSet);
        Assert.Equal(
            ["REVERSE_THREAT"],
            reversedAnalysis.Threats.Select(value => value.Threat.Code));
        Assert.Equal(
            ["DIRECT_THREAT"],
            unchangedAnalysis.Threats.Select(value => value.Threat.Code));
        Assert.Equal(
            AnalysisFingerprint(reversedAnalysis),
            AnalysisFingerprint(TargetThreatAnalyzer.Analyze(
                reversed.Snapshot,
                ruleSet)));
        Assert.Equal(
            AnalysisFingerprint(unchangedAnalysis),
            AnalysisFingerprint(TargetThreatAnalyzer.Analyze(
                directionOmitted.Snapshot,
                ruleSet)));

        var unsupportedSnapshot = CreateSnapshot(
            [skill],
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                CreateLoadout(attack: [skill.SkillId])),
            SnapshotValue<string>.Available(Version));
        var unsupported = TargetLoadoutObservationMerger.Merge(
            unsupportedSnapshot,
            Observation(
                TargetLoadoutCoverage.PartialLoadout,
                new ObservedTargetCombatSkill(
                    skill.SkillId,
                    skill.Category,
                    PracticeDirection.Reverse,
                    slotIndex: 0)));
        var unsupportedRuleSet = new TargetThreatRuleSet(
            Version,
            [skill.SkillId],
            rules);

        Assert.Equal(
            TargetLoadoutMergeStatus.UnsupportedVersion,
            unsupported.Status);
        var unsupportedAnalysis = TargetThreatAnalyzer.Analyze(
            unsupported.Snapshot,
            unsupportedRuleSet);
        Assert.Equal(
            ["DIRECT_THREAT"],
            unsupportedAnalysis.Threats.Select(value => value.Threat.Code));
        Assert.Equal(
            AnalysisFingerprint(unsupportedAnalysis),
            AnalysisFingerprint(TargetThreatAnalyzer.Analyze(
                unsupported.Snapshot,
                unsupportedRuleSet)));
    }

    [Fact]
    public void Combat_start_always_and_active_timing_remain_distinct()
    {
        var skill = CreateSkill(100, effectId: 1000);
        TargetThreatRule[] rules =
        [
            Rule(
                CreateThreat(
                    "COMBAT_START",
                    TargetThreatActivationTiming.CombatStart),
                Signature(skill)),
            Rule(
                CreateThreat(
                    "ALWAYS_EQUIPPED",
                    TargetThreatActivationTiming.Always),
                Signature(skill)),
            Rule(
                CreateThreat(
                    "ACTIVE_EFFECT",
                    TargetThreatActivationTiming.OnSkillUse),
                Signature(skill))
        ];

        var result = TargetThreatAnalyzer.Analyze(
            CreateSnapshot([skill]),
            CreateRuleSet([skill.SkillId], rules));

        Assert.Contains(
            result.Threats,
            value => value.Threat.ActivationTiming
                == TargetThreatActivationTiming.CombatStart);
        Assert.Contains(
            result.Threats,
            value => value.Threat.ActivationTiming
                == TargetThreatActivationTiming.Always);
        Assert.Contains(
            result.Threats,
            value => value.Threat.ActivationTiming
                == TargetThreatActivationTiming.OnSkillUse);
    }

    [Fact]
    public void Ranking_is_severity_then_source_scope_then_stable_code()
    {
        var equipped = CreateSkill(100, effectId: 1000);
        var learned = CreateSkill(101, effectId: 1001);
        TargetThreatRule[] rules =
        [
            Rule(
                CreateThreat(
                    "Z_HIGH_LEARNED",
                    severity: TargetThreatSeverity.High),
                Signature(learned)),
            Rule(
                CreateThreat(
                    "Z_CRITICAL_EQUIPPED",
                    severity: TargetThreatSeverity.Critical),
                Signature(equipped)),
            Rule(
                CreateThreat(
                    "A_CRITICAL_EQUIPPED",
                    severity: TargetThreatSeverity.Critical),
                Signature(equipped)),
            Rule(
                CreateThreat(
                    "A_CRITICAL_LEARNED",
                    severity: TargetThreatSeverity.Critical),
                Signature(learned))
        ];
        var snapshot = CreateSnapshot(
            [learned, equipped],
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                CreateLoadout(attack: [equipped.SkillId])));

        var first = TargetThreatAnalyzer.Analyze(
            snapshot,
            CreateRuleSet(
                [equipped.SkillId, learned.SkillId],
                rules));
        var second = TargetThreatAnalyzer.Analyze(
            snapshot,
            CreateRuleSet(
                [equipped.SkillId, learned.SkillId],
                [.. rules.Reverse()]));

        string[] expected =
        [
            "A_CRITICAL_EQUIPPED",
            "Z_CRITICAL_EQUIPPED",
            "A_CRITICAL_LEARNED",
            "Z_HIGH_LEARNED"
        ];
        Assert.Equal(
            expected,
            first.Threats.Select(value => value.Threat.Code));
        Assert.Equal(
            expected,
            second.Threats.Select(value => value.Threat.Code));
    }

    [Fact]
    public void Missing_equipped_loadout_warns_and_uses_learned_evidence()
    {
        var skill = CreateSkill(100, effectId: 1000);
        var snapshot = CreateSnapshot(
            [skill],
            SnapshotValue<CombatLoadoutSnapshot>.Unavailable(
                "Target loadout was absent from the disk snapshot."));

        var result = TargetThreatAnalyzer.Analyze(
            snapshot,
            CreateRuleSet([Signature(skill)]));

        var finding = Assert.Single(result.Threats);
        Assert.Equal(
            TargetThreatSourceScope.LearnedUnequipped,
            Assert.Single(finding.Sources).Scope);
        Assert.Contains(
            result.Warnings,
            warning => warning.Code
                == TargetThreatAnalyzer
                    .EquippedSkillsUnavailableWarningCode);
    }

    [Fact]
    public void Snapshot_loadout_warning_prevents_duplicate_threat_warning()
    {
        var skill = CreateSkill(100, effectId: 1000);
        var snapshot = CreateSnapshot(
            [skill],
            SnapshotValue<CombatLoadoutSnapshot>.Unavailable(
                "Target loadout is selected during combat preparation."),
            warnings:
            [
                new SnapshotWarning(
                    CombatSnapshotWarningCodes.TargetLoadoutNotPersisted,
                    "The disk snapshot does not contain the active loadout.")
            ]);

        var result = TargetThreatAnalyzer.Analyze(
            snapshot,
            CreateRuleSet([Signature(skill)]));

        Assert.Single(result.Threats);
        Assert.DoesNotContain(
            result.Warnings,
            warning => warning.Code
                == TargetThreatAnalyzer
                    .EquippedSkillsUnavailableWarningCode);
    }

    [Fact]
    public void Unrecognized_relevant_effect_generates_warning()
    {
        var skill = CreateSkill(100, effectId: 9999);
        var result = TargetThreatAnalyzer.Analyze(
            CreateSnapshot([skill]),
            CreateRuleSet(
                relevantSkillIds: [skill.SkillId],
                rules: [Rule(CreateThreat(), new TargetThreatSkillSignature(
                    skill.SkillId,
                    PracticeDirection.Direct,
                    rawEffectId: 1000))]));

        Assert.Empty(result.Threats);
        var warning = Assert.Single(
            result.Warnings,
            value => value.Code
                == TargetThreatAnalyzer.UnrecognizedEffectWarningCode);
        Assert.Equal(skill.SkillId, warning.Mechanic.SourceSkillId);
        Assert.Equal(9999, warning.Mechanic.RawEffectId);
    }

    [Fact]
    public void Neutral_relevant_skill_has_no_directional_effect()
    {
        var skill = CreateSkill(
            100,
            effectId: 1000,
            direction: PracticeDirection.Neutral);

        var result = TargetThreatAnalyzer.Analyze(
            CreateSnapshot([skill]),
            CreateRuleSet(
                relevantSkillIds: [skill.SkillId],
                rules: [Rule(CreateThreat(), new TargetThreatSkillSignature(
                    skill.SkillId,
                    PracticeDirection.Direct,
                    rawEffectId: 1000))]));

        Assert.Empty(result.Threats);
        Assert.DoesNotContain(
            result.Warnings,
            warning => warning.Code
                == TargetThreatAnalyzer.UnrecognizedEffectWarningCode);
    }

    [Fact]
    public void Unsupported_version_blocks_stale_rules()
    {
        var skill = CreateSkill(100, effectId: 1000);
        var result = TargetThreatAnalyzer.Analyze(
            CreateSnapshot(
                [skill],
                gameDataVersion:
                    SnapshotValue<string>.Available("1.0.0+new")),
            CreateRuleSet([Signature(skill)]));

        Assert.Empty(result.Threats);
        Assert.Contains(
            result.Warnings,
            warning => warning.Code
                == TargetThreatAnalyzer
                    .UnsupportedGameDataVersionWarningCode);
    }

    [Fact]
    public void Golden_snapshot_matches_manual_magic_sound_analysis()
    {
        var goldenSkills = new[]
        {
            CreateSkill(719, 669),
            CreateSkill(721, 671),
            CreateSkill(722, 672),
            CreateSkill(724, 674),
            CreateSkill(725, 349),
            CreateSkill(727, 351),
            CreateSkill(731, 355),
            CreateSkill(733, 357),
            CreateSkill(287, 911, PracticeDirection.Reverse)
        };
        var snapshot = CreateSnapshot(
            goldenSkills,
            SnapshotValue<CombatLoadoutSnapshot>.Unavailable(
                "The golden target has no equipped disk loadout."),
            SnapshotValue<string>.Available(
                VerifiedCombatEffectCatalogs.GoldenGameDataVersion));

        var result = TargetThreatAnalyzer.Analyze(
            snapshot,
            VerifiedTargetThreatRuleSets.GoldenMagicSound);

        Assert.Equal(
            [
                "DEFEAT_MARK_RESET_LOOP",
                "DISTRACTION_MARK_ACCUMULATION",
                "MIND_RESONANCE_CASCADE",
                "POSITIVE_MAGIC_SOUND_MIND_DAMAGE"
            ],
            result.Threats.Select(value => value.Threat.Code));
        Assert.All(
            result.Threats,
            finding =>
            {
                var expectedSourceCount = finding.Threat.Code
                    == "DEFEAT_MARK_RESET_LOOP"
                    ? 1
                    : 8;
                Assert.Equal(expectedSourceCount, finding.Sources.Length);
                Assert.All(
                    finding.Sources,
                    source => Assert.Equal(
                        TargetThreatSourceScope.LearnedUnequipped,
                        source.Scope));
            });
        Assert.Contains(
            result.Warnings,
            warning => warning.Code
                == TargetThreatAnalyzer
                    .EquippedSkillsUnavailableWarningCode);
        Assert.DoesNotContain(
            result.Warnings,
            warning => warning.Code
                == TargetThreatTaxonomy
                    .UnrecognizedMechanicWarningCode);
    }

    [Fact]
    public void Rule_set_rejects_signatures_outside_relevant_scope()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateRuleSet(
                relevantSkillIds: [100],
                rules:
                [
                    Rule(
                        CreateThreat(),
                        new TargetThreatSkillSignature(
                            101,
                            PracticeDirection.Direct,
                            1001))
                ]));

        Assert.Contains("relevant skill", exception.Message);
    }

    private static TargetThreatSkillSignature Signature(
        CombatSkillSnapshot skill)
    {
        return new TargetThreatSkillSignature(
            skill.SkillId,
            skill.Direction.Value,
            skill.Direction.Value == PracticeDirection.Reverse
                ? skill.ReverseEffectId.Value
                : skill.DirectEffectId.Value);
    }

    private static TargetThreatRule Rule(
        TargetThreat threat,
        params TargetThreatSkillSignature[] signatures)
    {
        return new TargetThreatRule(threat, signatures);
    }

    private static TargetThreatRuleSet CreateRuleSet(
        TargetThreatSkillSignature[] signatures)
    {
        return CreateRuleSet(
            [.. signatures.Select(signature => signature.SkillId)],
            [Rule(CreateThreat(), signatures)]);
    }

    private static TargetThreatRuleSet CreateRuleSet(
        int[] relevantSkillIds,
        TargetThreatRule[] rules)
    {
        return new TargetThreatRuleSet(
            Version,
            relevantSkillIds,
            rules);
    }

    private static TargetThreat CreateThreat(
        string code = "MIND_RESONANCE",
        TargetThreatActivationTiming timing =
            TargetThreatActivationTiming.OnSkillUse,
        TargetThreatSeverity severity = TargetThreatSeverity.Critical)
    {
        return new TargetThreat(
            code,
            TargetThreatKind.MindResonanceCascade,
            severity,
            title: code,
            explanation: "Verified target mechanic.",
            timing,
            [
                new TargetThreatEvidence(
                    Evidence,
                    "Verified rule.",
                    TargetThreatEvidenceConfidence.VerifiedRule)
            ]);
    }

    private static CombatSkillSnapshot CreateSkill(
        int skillId,
        int effectId,
        PracticeDirection direction = PracticeDirection.Direct)
    {
        return new CombatSkillSnapshot(
            skillId,
            SnapshotValue<string>.Available($"Skill {skillId}"),
            SkillCategory.Attack,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(false),
            SnapshotValue<PracticeDirection>.Available(direction),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(
                direction == PracticeDirection.Direct
                    ? effectId
                    : effectId - 1),
            SnapshotValue<int>.Available(
                direction == PracticeDirection.Reverse
                    ? effectId
                    : effectId + 1000));
    }

    private static CombatSnapshot CreateSnapshot(
        CombatSkillSnapshot[] targetSkills,
        SnapshotValue<CombatLoadoutSnapshot>? equippedSkills = null,
        SnapshotValue<string>? gameDataVersion = null,
        SnapshotWarning[]? warnings = null,
        TargetLoadoutObservation? observation = null)
    {
        return new CombatSnapshot(
            new CombatSnapshotMetadata(
                new string('A', 64),
                DateTimeOffset.Parse("2026-07-30T12:00:00Z"),
                SnapshotValue<DateTimeOffset>.Available(
                    DateTimeOffset.Parse("2026-07-30T11:00:00Z")),
                gameDataVersion
                    ?? SnapshotValue<string>.Available(Version)),
            CreatePlayer(),
            new TargetCombatSnapshot(
                characterId: 16317,
                SnapshotValue<string>.Available("Target"),
                SnapshotValue<int>.Available(52),
                features: [],
                targetSkills,
                equippedSkills
                    ?? SnapshotValue<CombatLoadoutSnapshot>.Available(
                        CreateLoadout()),
                equipment: [],
                observation),
            warnings ?? []);
    }

    private static TargetLoadoutObservation Observation(
        TargetLoadoutCoverage coverage,
        params ObservedTargetCombatSkill[] skills) => new(
            targetCharacterId: 16317,
            TargetObservationContext.Sparring,
            DateTimeOffset.Parse("2026-07-30T12:30:00Z"),
            "E3-000-CAP-002",
            coverage,
            skills);

    private static TargetLoadoutObservation Observation(
        TargetLoadoutCoverage coverage,
        TargetObservationContext context,
        params ObservedTargetCombatSkill[] skills) => new(
            targetCharacterId: 16317,
            context,
            DateTimeOffset.Parse("2026-07-30T12:30:00Z"),
            "E3-012-CAP-001",
            coverage,
            skills);

    private static string[] AnalysisFingerprint(
        TargetThreatAnalysis analysis) =>
    [
        .. analysis.Threats.Select(finding =>
            $"{finding.Threat.Code}:"
            + string.Join(",", finding.Sources.Select(source =>
                $"{source.SkillId}/{source.Kind}/{source.Direction}/"
                + $"{source.RawEffectId}/{source.EvidenceReference}"))),
        .. analysis.Warnings.Select(warning =>
            $"warning:{warning.Code}/{warning.Mechanic.EvidenceReference}")
    ];

    private static PlayerCombatSnapshot CreatePlayer()
    {
        return new PlayerCombatSnapshot(
            characterId: 1,
            SnapshotValue<string>.Available("Taiwu"),
            learnedSkills: [],
            CreateLoadout(),
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

    private static CombatLoadoutSnapshot CreateLoadout(
        int[]? attack = null)
    {
        return new CombatLoadoutSnapshot(
            neigongSkillIds: [],
            attack ?? [],
            agilitySkillIds: [],
            defenseSkillIds: [],
            assistanceSkillIds: []);
    }
}
