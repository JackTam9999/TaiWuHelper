using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetPlaybookComposition;
using TaiWu.Domain.TargetPlaybooks;
using TaiWu.Domain.TargetProfiles;
using Xunit;
using CompositionResult = TaiWu.Domain.TargetPlaybookComposition.TargetPlaybookComposition;

namespace TaiWu.Domain.UnitTests.TargetPlaybookCompositions;

public sealed class TargetPlaybookComposerTests
{
    [Fact]
    public void All_matched_playbooks_compose_without_duplicate_shared_facts()
    {
        var analysis = TargetPlaybookFixture.FullAnalysis();
        var composition = Compose(analysis);

        Assert.Equal(5, composition.SourcePlaybooks.Length);
        Assert.Equal(7, composition.Goals.Length);
        Assert.Equal(10, composition.Options.Length);
        Assert.Equal(7, composition.Threats.Length);
        Assert.Single(composition.KnownGaps);
        Assert.Equal(5, composition.Conflicts.Length);
        Assert.All(
            composition.Conflicts,
            conflict => Assert.Equal(
                TargetPlaybookCompositionConflictKind.ActiveRole,
                conflict.Kind));
        Assert.Empty(composition.Diagnostics);
        Assert.Equal(64, composition.StableKey.Length);

        var jinni = Assert.Single(
            composition.Options,
            option => option.StableKey == "REVERSE_JINNI_SUPPRESSION");
        Assert.Equal(
            [
                "BREAK_MIND_RESONANCE_CASCADE",
                "CONTROL_DISTRACTION_MARKS",
                "SURVIVE_MIND_DAMAGE_PRESSURE"
            ],
            jinni.SourceGoalCodes);
        Assert.Single(jinni.SourcePlaybookKeys);
    }

    [Fact]
    public void Non_matched_states_never_contribute_mechanical_goals()
    {
        var analysis = TargetPlaybookFixture.OuterOnlyAnalysis(
            includeExtraThreat: false);
        var composition = Compose(analysis);

        var playbook = Assert.Single(composition.SourcePlaybooks);
        Assert.Equal(
            "OUTER_DAMAGE_CONFIGURED",
            playbook.Identity.Archetype.Code);
        var goal = Assert.Single(composition.Goals);
        Assert.Equal("PREPARE_FOR_OUTER_DAMAGE", goal.Code);
        Assert.Equal(4, composition.Diagnostics.Length);
        Assert.All(
            composition.Diagnostics,
            diagnostic =>
            {
                Assert.Equal(
                    TargetPlaybookComposer.MatchNotConfirmedCode,
                    diagnostic.Code);
                Assert.NotEqual(
                    TargetArchetypeMatchState.Matched,
                    diagnostic.MatchState);
            });
    }

    [Fact]
    public void Partial_unsupported_and_conflicting_matches_are_all_excluded()
    {
        var outer = VerifiedTargetProfileExtractionRuleSets
            .Initial
            .OuterDamageFacet;
        var missing = new TargetProfileFacetIdentity(
            TargetProfileDimension.Tempo,
            "MISSING_TEMPO");
        var conflictingIdentity = new TargetProfileFacetIdentity(
            TargetProfileDimension.Control,
            "CONFLICTING_CONTROL");
        var profile = new TargetCombatProfile(
            42,
            VerifiedTargetProfileExtractionRuleSets.Initial.RuleVersion,
            [
                TargetProfileFacet.Confirmed(
                    outer,
                    TargetProfileFacetValue.Presence(
                        outer.Dimension,
                        outer.Code),
                    [Evidence("E5-OUTER")]),
                TargetProfileFacet.Conflicting(
                    conflictingIdentity,
                    [
                        new TargetProfileConflictCandidate(
                            TargetProfileFacetValue.Presence(
                                conflictingIdentity.Dimension,
                                conflictingIdentity.Code),
                            [Evidence("E5-CONFLICT-A")]),
                        new TargetProfileConflictCandidate(
                            TargetProfileFacetValue.Measured(
                                conflictingIdentity.Dimension,
                                conflictingIdentity.Code,
                                [new TargetProfileMeasurement(
                                    "RAW_VALUE",
                                    1,
                                    "RAW_GAME_UNIT")]),
                            [Evidence("E5-CONFLICT-B")])
                    ],
                    new TargetProfileUnavailableReason(
                        "CONFLICTING_EXACT_EVIDENCE"))
            ],
            diagnostics: []);
        var matched = Definition("MATCHED_ARCHETYPE", outer);
        var partial = Definition("PARTIAL_ARCHETYPE", outer, missing);
        var unsupported = Definition("UNSUPPORTED_ARCHETYPE", missing);
        var conflicting = Definition(
            "CONFLICTING_ARCHETYPE",
            conflictingIdentity);
        var matches = TargetArchetypeMatcher.Match(
            profile,
            [matched, partial, unsupported, conflicting]);
        var catalog = new TargetCounterPlaybookCatalog(
            new TargetProfileVersion(TargetPlaybookFixture.GameVersion),
            [matched],
            [
                VerifiedCombatCounterRuleSets.GoldenMagicSound,
                VerifiedCombatCounterRuleSets.Epic5TargetFamilies
            ],
            [Playbook(matched, Goal("MATCHED_GOAL"))]);

        var composition = TargetPlaybookComposer.Compose(
            matches,
            catalog,
            TargetPlaybookFixture.GameVersion);

        Assert.Single(composition.Goals);
        Assert.Equal(
            [
                TargetArchetypeMatchState.Partial,
                TargetArchetypeMatchState.Unsupported,
                TargetArchetypeMatchState.Conflicting
            ],
            composition.Diagnostics
                .Select(value => value.MatchState!.Value)
                .Order());
    }

    [Fact]
    public void Shared_goals_merge_strongest_priority_and_earliest_timing()
    {
        var first = TargetPlaybookFixture.Definition("ARCHETYPE_ALPHA");
        var second = TargetPlaybookFixture.Definition("ARCHETYPE_BETA");
        var gap = Gap("SHARED_GAP");
        var firstGoal = Goal(
            "SHARED_RESPONSE",
            sequence: 30,
            TargetResponsePriority.Normal,
            CombatCounterActivationTiming.ActiveAttack,
            gap: gap);
        var secondGoal = Goal(
            "SHARED_RESPONSE",
            sequence: 10,
            TargetResponsePriority.Critical,
            CombatCounterActivationTiming.CombatStartPassive,
            gap: gap);
        var composition = ComposeCustom(
            [first, second],
            [Playbook(first, firstGoal), Playbook(second, secondGoal)]);

        var composed = Assert.Single(composition.Goals);
        Assert.Equal(10, composed.Sequence);
        Assert.Equal(TargetResponsePriority.Critical, composed.Priority);
        Assert.Equal(
            CombatCounterActivationTiming.CombatStartPassive,
            composed.ResponseTiming);
        Assert.Equal(2, composed.SourcePlaybookKeys.Length);
        Assert.Single(composed.ProfileFacets);
        Assert.Single(composed.KnownGaps);
        Assert.Empty(composition.Conflicts);
    }

    [Theory]
    [InlineData(
        "TIMING_RESPONSE_WINDOW",
        TargetPlaybookCompositionConflictKind.Timing)]
    [InlineData(
        "REQUIREMENT_WEAPON_CONTEXT",
        TargetPlaybookCompositionConflictKind.Requirement)]
    [InlineData(
        "CAPACITY_ATTACK_SLOTS",
        TargetPlaybookCompositionConflictKind.Capacity)]
    public void Explicit_goal_conflict_groups_remain_typed_conflicts(
        string group,
        TargetPlaybookCompositionConflictKind expectedKind)
    {
        var first = TargetPlaybookFixture.Definition("ARCHETYPE_ALPHA");
        var second = TargetPlaybookFixture.Definition("ARCHETYPE_BETA");
        var composition = ComposeCustom(
            [first, second],
            [
                Playbook(first, Goal("GOAL_ALPHA", group: group)),
                Playbook(second, Goal("GOAL_BETA", group: group))
            ]);

        var conflict = Assert.Single(composition.Conflicts);
        Assert.Equal(expectedKind, conflict.Kind);
        Assert.Equal(group, conflict.ConflictGroup);
        Assert.Equal(["GOAL_ALPHA", "GOAL_BETA"], conflict.GoalCodes);
        Assert.Empty(conflict.OptionCodes);
    }

    [Fact]
    public void Distinct_required_active_options_remain_an_active_role_conflict()
    {
        var first = TargetPlaybookFixture.Definition("ARCHETYPE_ALPHA");
        var second = TargetPlaybookFixture.Definition("ARCHETYPE_BETA");
        var jinni = CounterOption("REVERSE_JINNI_SUPPRESSION");
        var fulong = CounterOption("REVERSE_FULONG_POWER_REDUCTION");
        var composition = ComposeCustom(
            [first, second],
            [
                Playbook(first, Goal(
                    "GOAL_ALPHA",
                    option: jinni,
                    threat: MindThreat())),
                Playbook(second, Goal(
                    "GOAL_BETA",
                    option: fulong,
                    threat: MindThreat()))
            ]);

        var conflict = Assert.Single(composition.Conflicts);
        Assert.Equal(
            TargetPlaybookCompositionConflictKind.ActiveRole,
            conflict.Kind);
        Assert.Equal("ACTIVE_ATTACK_ROLE", conflict.ConflictGroup);
        Assert.Equal(
            ["REVERSE_FULONG_POWER_REDUCTION", "REVERSE_JINNI_SUPPRESSION"],
            conflict.OptionCodes);
    }

    [Fact]
    public void One_shared_active_option_satisfies_both_goals_without_conflict()
    {
        var first = TargetPlaybookFixture.Definition("ARCHETYPE_ALPHA");
        var second = TargetPlaybookFixture.Definition("ARCHETYPE_BETA");
        var shared = CounterOption("REVERSE_JINNI_SUPPRESSION");
        var composition = ComposeCustom(
            [first, second],
            [
                Playbook(first, Goal(
                    "GOAL_ALPHA",
                    option: shared,
                    threat: MindThreat())),
                Playbook(second, Goal(
                    "GOAL_BETA",
                    option: shared,
                    threat: MindThreat()))
            ]);

        Assert.Empty(composition.Conflicts);
        var option = Assert.Single(composition.Options);
        Assert.Equal(2, option.SourceGoalCodes.Length);
    }

    [Fact]
    public void Equivalent_reordered_catalogues_produce_the_same_composition()
    {
        var analysis = TargetPlaybookFixture.FullAnalysis();
        var catalog = VerifiedTargetCounterPlaybooks.Initial;
        var reordered = new TargetCounterPlaybookCatalog(
            catalog.GameDataVersion,
            catalog.Archetypes.Reverse(),
            [
                VerifiedCombatCounterRuleSets.Epic5TargetFamilies,
                VerifiedCombatCounterRuleSets.GoldenMagicSound
            ],
            catalog.Playbooks.Reverse());

        var first = TargetPlaybookComposer.Compose(
            analysis.ArchetypeMatches,
            catalog,
            TargetPlaybookFixture.GameVersion);
        var second = TargetPlaybookComposer.Compose(
            analysis.ArchetypeMatches,
            reordered,
            TargetPlaybookFixture.GameVersion);

        Assert.Equal(first.StableKey, second.StableKey);
        Assert.Equal(
            first.Goals.Select(goal => goal.Code),
            second.Goals.Select(goal => goal.Code));
        Assert.Equal(
            first.Options.Select(option => option.StableKey),
            second.Options.Select(option => option.StableKey));
    }

    [Fact]
    public void Unsupported_catalog_version_keeps_every_match_diagnostic()
    {
        var analysis = TargetPlaybookFixture.FullAnalysis();

        var composition = TargetPlaybookComposer.Compose(
            analysis.ArchetypeMatches,
            VerifiedTargetCounterPlaybooks.Initial,
            TargetPlaybookFixture.GameVersion + ".changed");

        Assert.Empty(composition.SourcePlaybooks);
        Assert.Empty(composition.Goals);
        Assert.Equal(5, composition.Diagnostics.Length);
        Assert.All(
            composition.Diagnostics,
            diagnostic => Assert.Equal(
                TargetCounterPlaybookResolutionStatus
                    .UnsupportedGameDataVersion,
                diagnostic.ResolutionStatus));
    }

    private static CompositionResult Compose(
        TargetCombatProfileAnalysis analysis) =>
        TargetPlaybookComposer.Compose(
            analysis.ArchetypeMatches,
            VerifiedTargetCounterPlaybooks.Initial,
            TargetPlaybookFixture.GameVersion);

    private static CompositionResult ComposeCustom(
        TargetArchetypeDefinition[] definitions,
        TargetCounterPlaybook[] playbooks)
    {
        var analysis = TargetPlaybookFixture.FullAnalysis(
            definitions,
            includeObservation: false);
        var catalog = new TargetCounterPlaybookCatalog(
            new TargetProfileVersion(TargetPlaybookFixture.GameVersion),
            definitions,
            [VerifiedCombatCounterRuleSets.GoldenMagicSound],
            playbooks);
        return TargetPlaybookComposer.Compose(
            analysis.ArchetypeMatches,
            catalog,
            TargetPlaybookFixture.GameVersion);
    }

    private static TargetCounterPlaybook Playbook(
        TargetArchetypeDefinition definition,
        TargetCounterPlaybookGoal goal) => new(
            new TargetCounterPlaybookIdentity(
                definition.Identity,
                new TargetProfileVersion("1.0.0")),
            [goal],
            ["E5-005"]);

    private static TargetArchetypeDefinition Definition(
        string code,
        params TargetProfileFacetIdentity[] requiredFacets) => new(
            new TargetArchetypeIdentity(
                code,
                new TargetProfileVersion("1.0.0")),
            VerifiedTargetProfileExtractionRuleSets.Initial.RuleVersion,
            $"TargetArchetype.{code}.Title",
            requiredFacets.Select((facet, index) =>
                new TargetArchetypeFacetPredicate(
                    $"REQUIRES_{index + 1}_{facet.Code}",
                    facet,
                    TargetArchetypePredicateOperator.FacetConfirmed)),
            supportingPredicates: [],
            exclusions: [],
            evidenceReferences: ["E5-005"]);

    private static TargetCounterPlaybookGoal Goal(
        string code,
        int sequence = 10,
        TargetResponsePriority priority = TargetResponsePriority.High,
        CombatCounterActivationTiming timing =
            CombatCounterActivationTiming.ActiveAttack,
        string? group = null,
        TargetCounterPlaybookOption? option = null,
        TargetThreat? threat = null,
        TargetCounterPlaybookGap? gap = null) => new(
            code,
            sequence,
            priority,
            timing,
            [VerifiedTargetProfileExtractionRuleSets.Initial.OuterDamageFacet],
            threat is null ? [] : [threat],
            option is null ? [] : [option],
            group is null ? [] : [group],
            ["E5-005"],
            option is null ? [gap ?? Gap($"GAP_{code}")] : []);

    private static TargetCounterPlaybookGap Gap(string code) => new(
        code,
        TargetCounterPlaybookGapKind.NoVerifiedOption,
        $"TargetPlaybook.Gap.{code}",
        ["E5-005"]);

    private static TargetCounterPlaybookOption CounterOption(string code) =>
        new(
            VerifiedCombatCounterRuleSets.GoldenMagicSound.Rules.Single(
                rule => rule.Code == code),
            ["ACTIVE_ATTACK_ROLE"]);

    private static TargetThreat MindThreat() =>
        VerifiedTargetThreatTaxonomies.GoldenMagicSound.Threats.Single(
            threat => threat.Code == "POSITIVE_MAGIC_SOUND_MIND_DAMAGE");

    private static TargetProfileEvidence Evidence(string reference) => new(
        reference,
        TargetProfileEvidenceSourceKind.SyntheticFixture,
        "E5-005:FIXTURE",
        new TargetProfileVersion(TargetPlaybookFixture.GameVersion));
}
