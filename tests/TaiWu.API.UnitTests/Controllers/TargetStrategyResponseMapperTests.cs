using NSubstitute;
using System.Reflection;
using System.Text.Json;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.Localization;
using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetPlaybookComposition;
using TaiWu.Domain.TargetPlaybooks;
using TaiWu.Domain.TargetProfiles;
using TaiWuAPI.Contracts.CombatRecommendations;
using Xunit;

namespace TaiWu.API.UnitTests.Controllers;

public sealed class TargetStrategyResponseMapperTests
{
    [Fact]
    public async Task Complete_strategy_maps_in_stable_bilingual_order()
    {
        var snapshot = FullBaselineSnapshot(playerOwnsCounter: true);
        var recommendation = await Recommend(snapshot);

        var english = CombatRecommendationResponseMapper.Map(
            recommendation,
            TaiwuLanguage.English).TargetStrategy!;
        var chinese = CombatRecommendationResponseMapper.Map(
            recommendation,
            TaiwuLanguage.Chinese).TargetStrategy!;

        Assert.Equal(
            VerifiedTargetProfileExtractionRuleSets.Initial
                .RuleVersion.Value,
            english.Profile.RuleVersion);
        Assert.Equal(
            Enum.GetValues<TargetProfileDimension>()
                .Order()
                .Where(dimension => english.Profile.Facets.Any(facet =>
                    facet.Dimension == dimension)),
            english.Profile.Facets
                .Select(facet => facet.Dimension)
                .Distinct());
        Assert.All(
            english.Profile.Facets,
            facet => Assert.NotEmpty(facet.Evidence));
        Assert.Equal(
            [
                "CHANNEL_RESISTANCE_ASYMMETRY",
                "DEFEAT_MARK_RESET_OVERLAY",
                "MIND_RESONANCE_BASELINE",
                "OUTER_DAMAGE_CONFIGURED",
                "POISON_APPLICATION_CONFIGURED"
            ],
            english.Archetypes.Select(value => value.Code));
        var baseline = Assert.Single(
            english.Archetypes,
            value => value.Code == "MIND_RESONANCE_BASELINE");
        Assert.Equal(TargetArchetypeMatchState.Matched, baseline.State);
        Assert.NotEqual(
            baseline.Title,
            Assert.Single(
                chinese.Archetypes,
                value => value.Code == baseline.Code).Title);

        Assert.Equal(
            ["DEFEAT_MARK_RESET_OVERLAY", baseline.Code],
            english.Playbook.Sources.Select(source => source.ArchetypeCode));
        Assert.Equal(
            [
                "SURVIVE_MIND_DAMAGE_PRESSURE",
                "CONTROL_DISTRACTION_MARKS",
                "BREAK_MIND_RESONANCE_CASCADE",
                "PRESSURE_DEFEAT_MARK_RESET"
            ],
            english.Playbook.Goals.Select(value => value.Code));
        Assert.All(english.Playbook.Goals, goal => Assert.True(goal.IsEligible));
        var markGoal = Assert.Single(
            english.Playbook.Goals,
            goal => goal.Code == "CONTROL_DISTRACTION_MARKS");
        Assert.NotEqual(
            markGoal.Title,
            Assert.Single(
                chinese.Playbook.Goals,
                goal => goal.Code == markGoal.Code).Title);
        Assert.All(
            markGoal.ThreatReferences,
            reference => Assert.StartsWith("threat:", reference));
        var laojun = Assert.Single(
            markGoal.Options,
            option => option.Code == "REVERSE_LAOJUN_MARK_CLEAR");
        var requirement = Assert.Single(laojun.Requirements);
        Assert.Equal(
            TargetCombatRequirementKind.SkillActivation,
            requirement.Kind);
        Assert.Equal(SkillActivationState.EquippedPassive,
            requirement.RequiredSkillState);

        Assert.Contains(
            english.Adjustments.Items,
            item => item.Action == TargetPlaybookAdjustmentAction.Retained
                && item.OriginalResponse?.Kind
                    == TargetPlaybookResponseReferenceKind.Goal);
        Assert.Contains(
            english.Adjustments.Items,
            item => item.Action == TargetPlaybookAdjustmentAction.Unresolved
                && item.OriginalResponse?.Kind
                    == TargetPlaybookResponseReferenceKind.Gap);
        Assert.Equal(6, english.CounterAvailability.Count);
        Assert.Contains(
            english.CounterAvailability,
            counter => counter.CounterCode == "REVERSE_JINNI_SUPPRESSION"
                && counter.State
                    == TargetPlaybookCounterAvailabilityState.Feasible
                && counter.Gap is null);
        var unavailable = Assert.Single(
            english.CounterAvailability,
            counter => counter.CounterCode
                == "REVERSE_QILUN_TRUE_QI_DRAIN");
        Assert.Equal(
            TargetPlaybookCounterAvailabilityState.Inaccessible,
            unavailable.State);
        Assert.NotEmpty(unavailable.AccessIssues);
        Assert.NotNull(unavailable.Gap);
        Assert.NotEqual(
            unavailable.Gap!.Message,
            Assert.Single(
                chinese.CounterAvailability,
                counter => counter.CounterCode
                    == unavailable.CounterCode).Gap!.Message);

        var json = JsonSerializer.Serialize(
            CombatRecommendationResponseMapper.Map(recommendation));
        Assert.DoesNotContain(snapshot.Metadata.SavePath, json);
        Assert.DoesNotContain(
            "開始施展此功法時",
            json,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Split_baseline_composes_without_reset_overlay()
    {
        var partial = CombatRecommendationResponseMapper.Map(
            await Recommend(PartialSnapshot()),
            TaiwuLanguage.English).TargetStrategy!;
        var unsupported = CombatRecommendationResponseMapper.Map(
            await Recommend(PartialSnapshot("9.9.9-unsupported")),
            TaiwuLanguage.English).TargetStrategy!;

        Assert.Equal(
            TargetArchetypeMatchState.Matched,
            Assert.Single(
                partial.Archetypes,
                value => value.Code
                    == "MIND_RESONANCE_BASELINE").State);
        Assert.Equal(
            TargetArchetypeMatchState.Unsupported,
            Assert.Single(
                partial.Archetypes,
                value => value.Code
                    == "DEFEAT_MARK_RESET_OVERLAY").State);
        Assert.Equal(
            ["MIND_RESONANCE_BASELINE"],
            partial.Playbook.Sources.Select(source => source.ArchetypeCode));
        Assert.DoesNotContain(
            partial.Playbook.Goals,
            goal => goal.Code == "PRESSURE_DEFEAT_MARK_RESET");
        Assert.All(
            unsupported.Archetypes,
            value => Assert.Equal(
                TargetArchetypeMatchState.Unsupported,
                value.State));
        Assert.Empty(unsupported.Profile.Facets);
        Assert.Contains(
            unsupported.Profile.Diagnostics,
            value => value.Severity
                == TargetProfileDiagnosticSeverity.Error);
        Assert.Empty(unsupported.Playbook.Sources);
    }

    [Fact]
    public void Conflicting_and_multi_match_profiles_survive_projection()
    {
        var rules = VerifiedTargetProfileExtractionRuleSets.Initial;
        var conflictEvidence = new[]
        {
            Evidence("SYNTHETIC:CONFLICT:A"),
            Evidence("SYNTHETIC:CONFLICT:B")
        };
        var conflictingFacet = TargetProfileFacet.Conflicting(
            rules.OuterDamageFacet,
            [
                new TargetProfileConflictCandidate(
                    Measured(rules.OuterDamageFacet, "A"),
                    [conflictEvidence[0]]),
                new TargetProfileConflictCandidate(
                    Measured(rules.OuterDamageFacet, "B"),
                    [conflictEvidence[1]])
            ],
            new TargetProfileUnavailableReason(
                "SYNTHETIC_CONFLICT"));
        var conflicting = StrategyForProfile(
            new TargetCombatProfile(
                16317,
                rules.RuleVersion,
                [conflictingFacet],
                diagnostics: []));

        var conflictResponse = TargetStrategyResponseMapper.Map(
            conflicting,
            EmptySnapshot().Player,
            TaiwuLanguage.English);
        var mappedFacet = Assert.Single(conflictResponse.Profile.Facets);
        Assert.Equal(TargetProfileEvidenceState.Conflicting,
            mappedFacet.State);
        Assert.Equal(2, mappedFacet.ConflictCandidates.Count);
        Assert.NotNull(mappedFacet.UnavailableReason);
        Assert.Equal(
            TargetArchetypeMatchState.Conflicting,
            Assert.Single(
                conflictResponse.Archetypes,
                value => value.Code
                    == "OUTER_DAMAGE_CONFIGURED").State);

        var requiredFacets = VerifiedTargetCounterPlaybooks.Initial.Archetypes
            .SelectMany(archetype => archetype.RequiredPredicates)
            .Select(predicate => predicate.Facet)
            .DistinctBy(facet => (facet.Dimension, facet.Code))
            .Select(facet => TargetProfileFacet.Confirmed(
                facet,
                TargetProfileFacetValue.Presence(
                    facet.Dimension,
                    facet.Code),
                [Evidence($"SYNTHETIC:{facet.Code}")]))
            .ToArray();
        var multiple = StrategyForProfile(new TargetCombatProfile(
            16317,
            rules.RuleVersion,
            requiredFacets,
            diagnostics: []));
        var multipleResponse = TargetStrategyResponseMapper.Map(
            multiple,
            EmptySnapshot().Player,
            TaiwuLanguage.Chinese);

        Assert.Equal(5, multipleResponse.Archetypes.Count(value =>
            value.State == TargetArchetypeMatchState.Matched));
        Assert.Equal(5, multipleResponse.Playbook.Sources.Count);
        Assert.Equal(7, multipleResponse.Playbook.Goals.Count);
        Assert.Single(multipleResponse.Playbook.Gaps);
        Assert.All(
            multipleResponse.Playbook.Goals,
            goal =>
            {
                Assert.True(goal.IsEligible);
                Assert.All(
                    goal.Options,
                    option => Assert.Subset(
                        goal.ThreatReferences.ToHashSet(
                            StringComparer.Ordinal),
                        option.ThreatReferences.ToHashSet(
                            StringComparer.Ordinal)));
            });
    }

    [Fact]
    public async Task Every_adjustment_action_and_evidence_state_is_projected()
    {
        var snapshot = FullBaselineSnapshot(playerOwnsCounter: true);
        var recommendation = await Recommend(snapshot);
        var baseline = recommendation.TargetPlaybook!;
        var confirmed = Enum.GetValues<TargetPlaybookAdjustmentEvidenceKind>()
            .Select(kind => AdjustmentEvidence(
                kind,
                TargetPlaybookAdjustmentEvidenceState.Confirmed))
            .ToArray();
        var contrary = AdjustmentEvidence(
            TargetPlaybookAdjustmentEvidenceKind.ArchetypeMatch,
            TargetPlaybookAdjustmentEvidenceState.Contrary);
        var incomplete = AdjustmentEvidence(
            TargetPlaybookAdjustmentEvidenceKind.Gap,
            TargetPlaybookAdjustmentEvidenceState.Incomplete);
        var goals = baseline.Composition.Goals
            .Select(goal => new TargetPlaybookResponseReference(
                TargetPlaybookResponseReferenceKind.Goal,
                goal.Code))
            .ToArray();
        var gap = new TargetPlaybookResponseReference(
            TargetPlaybookResponseReferenceKind.Gap,
            baseline.Gaps[0].Code);
        var items = new[]
        {
            Adjustment("MAP_RETAINED",
                TargetPlaybookAdjustmentAction.Retained,
                goals[0], null, confirmed,
                "EXACT_TARGET_SUPPORTS_RESPONSE"),
            Adjustment("MAP_ELEVATED",
                TargetPlaybookAdjustmentAction.Elevated,
                goals[1], null, [confirmed[0]],
                "CURRENT_OBSERVATION_CONFIRMS_RESPONSE"),
            Adjustment("MAP_REDUCED",
                TargetPlaybookAdjustmentAction.Reduced,
                goals[2], null, [contrary],
                "EXACT_TARGET_EVIDENCE_INCOMPLETE"),
            Adjustment("MAP_REPLACED",
                TargetPlaybookAdjustmentAction.Replaced,
                goals[3], new TargetPlaybookResponseReference(
                    TargetPlaybookResponseReferenceKind.Option,
                    "CUSTOM_REPLACEMENT"), [confirmed[0]],
                "EXACT_TARGET_SUPPORTS_RESPONSE"),
            Adjustment("MAP_ADDED",
                TargetPlaybookAdjustmentAction.Added,
                null, new TargetPlaybookResponseReference(
                    TargetPlaybookResponseReferenceKind.Threat,
                    "CUSTOM_EXACT_THREAT"), [confirmed[0]],
                "EXACT_TARGET_THREAT_OUTSIDE_PLAYBOOK"),
            Adjustment("MAP_UNRESOLVED",
                TargetPlaybookAdjustmentAction.Unresolved,
                gap, null, [incomplete],
                "PLAYBOOK_GAP_REMAINS_UNRESOLVED")
        };
        var adjustments = AdjustmentSet(
            baseline.Analysis.Profile.Fingerprint,
            baseline.Composition.StableKey,
            [.. confirmed, contrary, incomplete],
            items);
        var personalized = Personalization(
            baseline.Analysis,
            baseline.Composition,
            adjustments,
            baseline.Counters);

        var english = TargetStrategyResponseMapper.Map(
            personalized,
            snapshot.Player,
            TaiwuLanguage.English);
        var chinese = TargetStrategyResponseMapper.Map(
            personalized,
            snapshot.Player,
            TaiwuLanguage.Chinese);
        var mapped = english.Adjustments.Items
            .Where(item => item.RuleCode.StartsWith(
                "MAP_",
                StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(
            Enum.GetValues<TargetPlaybookAdjustmentAction>().Order(),
            mapped.Select(item => item.Action).Order());
        Assert.Equal(
            Enum.GetValues<TargetPlaybookAdjustmentEvidenceState>().Order(),
            mapped.SelectMany(item => item.Evidence)
                .Select(value => value.State)
                .Distinct()
                .Order());
        Assert.Equal(
            Enum.GetValues<TargetPlaybookAdjustmentEvidenceKind>().Order(),
            mapped.SelectMany(item => item.Evidence)
                .Select(value => value.Kind)
                .Distinct()
                .Order());
        Assert.All(mapped, item => Assert.NotEqual(
            item.Reason,
            Assert.Single(
                chinese.Adjustments.Items,
                chineseItem => chineseItem.RuleCode == item.RuleCode).Reason));
    }

    private static TargetPlaybookPersonalization StrategyForProfile(
        TargetCombatProfile profile)
    {
        var snapshot = EmptySnapshot();
        var threats = TargetThreatAnalyzer.Analyze(
            snapshot,
            VerifiedTargetThreatRuleSets.GoldenMagicSound);
        var matches = TargetArchetypeMatcher.Match(
            profile,
            VerifiedTargetCounterPlaybooks.Initial.Archetypes);
        var analysis = new TargetCombatProfileAnalysis(
            threats,
            profile,
            matches);
        var composition = TargetPlaybookComposer.Compose(
            matches,
            VerifiedTargetCounterPlaybooks.Initial,
            VerifiedCombatEffectCatalogs.GoldenGameDataVersion);
        var adjustments = TargetSpecificPlaybookAdjuster.Apply(
            composition,
            analysis);
        var eligibleGoalCodes = adjustments.Adjustments
            .Where(value => value.Action is
                TargetPlaybookAdjustmentAction.Retained
                or TargetPlaybookAdjustmentAction.Elevated)
            .Select(value => value.OriginalResponse)
            .Where(value => value?.Kind
                == TargetPlaybookResponseReferenceKind.Goal)
            .Select(value => value!.StableCode)
            .ToHashSet(StringComparer.Ordinal);
        return Personalization(
            analysis,
            composition,
            adjustments,
            Array.Empty<TargetPlaybookCounterAvailability>(),
            eligibleGoalCodes);
    }

    private static TargetPlaybookAdjustmentEvidence AdjustmentEvidence(
        TargetPlaybookAdjustmentEvidenceKind kind,
        TargetPlaybookAdjustmentEvidenceState state)
    {
        var constructor = typeof(TargetPlaybookAdjustmentEvidence)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single();
        return (TargetPlaybookAdjustmentEvidence)constructor.Invoke(
        [
            kind,
            state,
            $"MAPPER_{kind.ToString().ToUpperInvariant()}_"
                + state.ToString().ToUpperInvariant(),
            new[] { $"E5-007:{kind}:{state}" }
        ]);
    }

    private static TargetPlaybookAdjustment Adjustment(
        string code,
        TargetPlaybookAdjustmentAction action,
        TargetPlaybookResponseReference? original,
        TargetPlaybookResponseReference? result,
        IReadOnlyList<TargetPlaybookAdjustmentEvidence> evidence,
        string reasonCode)
    {
        var constructor = typeof(TargetPlaybookAdjustment)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single();
        return (TargetPlaybookAdjustment)constructor.Invoke(
        [
            code,
            action,
            original,
            result,
            reasonCode,
            evidence
        ]);
    }

    private static TargetPlaybookAdjustmentSet AdjustmentSet(
        string profileFingerprint,
        string compositionKey,
        IReadOnlyList<TargetPlaybookAdjustmentEvidence> evidence,
        IReadOnlyList<TargetPlaybookAdjustment> adjustments)
    {
        var constructor = typeof(TargetPlaybookAdjustmentSet)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single();
        return (TargetPlaybookAdjustmentSet)constructor.Invoke(
        [
            profileFingerprint,
            compositionKey,
            evidence,
            adjustments,
            Array.Empty<TargetPlaybookAdjustmentDiagnostic>()
        ]);
    }

    private static TargetPlaybookPersonalization Personalization(
        TargetCombatProfileAnalysis analysis,
        TargetPlaybookComposition composition,
        TargetPlaybookAdjustmentSet adjustments,
        IReadOnlyList<TargetPlaybookCounterAvailability> counters,
        IReadOnlySet<string>? eligibleGoalCodes = null)
    {
        eligibleGoalCodes ??= adjustments.Adjustments
            .Where(value => value.Action is
                TargetPlaybookAdjustmentAction.Retained
                or TargetPlaybookAdjustmentAction.Elevated)
            .Select(value => value.OriginalResponse)
            .Where(value => value?.Kind
                == TargetPlaybookResponseReferenceKind.Goal)
            .Select(value => value!.StableCode)
            .ToHashSet(StringComparer.Ordinal);
        var constructor = typeof(TargetPlaybookPersonalization)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(value => value.GetParameters().Length == 5);
        return (TargetPlaybookPersonalization)constructor.Invoke(
        [
            analysis,
            composition,
            adjustments,
            composition.Goals.Where(goal =>
                eligibleGoalCodes.Contains(goal.Code)).ToArray(),
            counters
        ]);
    }

    private static TargetProfileFacetValue Measured(
        TargetProfileFacetIdentity facet,
        string measurementCode) => TargetProfileFacetValue.Measured(
        facet.Dimension,
        facet.Code,
        [new TargetProfileMeasurement(
            measurementCode,
            value: 1,
            "RAW_GAME_UNIT")]);

    private static TargetProfileEvidence Evidence(string reference) => new(
        reference,
        TargetProfileEvidenceSourceKind.SyntheticFixture,
        reference,
        new TargetProfileVersion("1.0.0-test"));

    private static async Task<CombatLoadoutRecommendation> Recommend(
        CombatSnapshot snapshot)
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);
        return await new RecommendCombatLoadout(reader).ExecuteAsync(
            new RecommendCombatLoadoutRequest(
                snapshot.Metadata.SavePath,
                snapshot.Target.CharacterId,
                RecommendationPolicy.Balanced),
            TestContext.Current.CancellationToken);
    }

    private static CombatSnapshot FullBaselineSnapshot(
        bool playerOwnsCounter)
    {
        var playerSkills = playerOwnsCounter
            ? new[]
            {
                Skill(
                    604,
                    SkillCategory.Attack,
                    PracticeDirection.Reverse,
                    338,
                    1064)
            }
            : [];
        var magic = Skill(
            719,
            SkillCategory.Attack,
            PracticeDirection.Direct,
            669,
            1669);
        var reset = Skill(
            287,
            SkillCategory.Assistance,
            PracticeDirection.Reverse,
            185,
            911);
        return Snapshot(
            playerSkills,
            [magic, reset],
            new CombatLoadoutSnapshot(
                [],
                [magic.SkillId],
                [],
                [],
                [reset.SkillId]),
            VerifiedCombatEffectCatalogs.GoldenGameDataVersion);
    }

    private static CombatSnapshot PartialSnapshot(
        string? gameDataVersion = null)
    {
        var magic = Skill(
            719,
            SkillCategory.Attack,
            PracticeDirection.Direct,
            669,
            1669);
        return Snapshot(
            playerSkills: [],
            [magic],
            new CombatLoadoutSnapshot([], [magic.SkillId], [], [], []),
            gameDataVersion
                ?? VerifiedCombatEffectCatalogs.GoldenGameDataVersion);
    }

    private static CombatSnapshot EmptySnapshot() => Snapshot(
        playerSkills: [],
        targetSkills: [],
        new CombatLoadoutSnapshot([], [], [], [], []),
        VerifiedCombatEffectCatalogs.GoldenGameDataVersion);

    private static CombatSnapshot Snapshot(
        CombatSkillSnapshot[] playerSkills,
        CombatSkillSnapshot[] targetSkills,
        CombatLoadoutSnapshot targetLoadout,
        string gameDataVersion) => new(
        new CombatSnapshotMetadata(
            @"C:\private\never-expose\local.sav",
            new string('A', 64),
            DateTimeOffset.Parse("2026-08-10T12:00:00Z"),
            SnapshotValue<DateTimeOffset>.Available(
                DateTimeOffset.Parse("2026-08-10T11:00:00Z")),
            SnapshotValue<string>.Available(gameDataVersion)),
        new PlayerCombatSnapshot(
            1,
            SnapshotValue<string>.Available("Taiwu"),
            playerSkills,
            new CombatLoadoutSnapshot([], [], [], [], []),
            equipment: [],
            new SlotBudgetSet(Enum.GetValues<SkillCategory>().Select(
                category => new SlotBudget(category, 0, 10))),
            new GenericSlotAllocation(0, 0, 0, 0, 0),
            legendaryBookCostSlots: [],
            legendaryBookCostAssignments: []),
        new TargetCombatSnapshot(
            16317,
            SnapshotValue<string>.Available("Target"),
            SnapshotValue<int>.Available(52),
            features: [],
            targetSkills,
            SnapshotValue<CombatLoadoutSnapshot>.Available(targetLoadout),
            equipment: []),
        warnings: []);

    private static CombatSkillSnapshot Skill(
        int skillId,
        SkillCategory category,
        PracticeDirection direction,
        int directEffectId,
        int reverseEffectId) => new(
        skillId,
        SnapshotValue<string>.Available($"Skill {skillId}"),
        category,
        SnapshotValue<int>.Available(1),
        SnapshotValue<bool>.Available(true),
        SnapshotValue<PracticeDirection>.Available(direction),
        SkillSlotContribution.None,
        SnapshotValue<int>.Available(directEffectId),
        SnapshotValue<int>.Available(reverseEffectId));
}
