using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatThreats;

public sealed class ExactTargetEncounterPhaseResolverTests
{
    private static readonly ExactTargetEncounterPhaseRuleSet Rule =
        VerifiedExactTargetEncounterRuleSets.CurrentLaterMagicSound;

    [Fact]
    public void Exact_later_phase_resolves_complete()
    {
        var result = ExactTargetEncounterPhaseResolver.Resolve(
            Rule,
            Observation());

        Assert.Equal(TargetEncounterBindingStatus.Complete, result.Status);
        Assert.Equal(["EXACT_TARGET_PHASE_COMPLETE"], result.Diagnostics);
        Assert.Equal(34, Rule.ExpectedEquippedSkills.Length);
        Assert.All(
            Rule.ExpectedEquippedSkills,
            item => Assert.Equal(PracticeDirection.Direct, item.Direction));
        Assert.Equal(
            new[] { 726, 727, 728, 729, 732, 733 },
            Rule.DirectMagicSoundSkillIds);
        Assert.True(Rule.Reverse604CoversEveryEquippedSkill);
        Assert.Equal(
            "A60918FED854795294AC53FAEFC0E0DFE349E9F67BB30D978C1F47109DBD554D",
            Rule.Fingerprint);
        Assert.Equal(
            Rule.Fingerprint,
            VerifiedExactTargetEncounterRuleSets.CurrentLaterMagicSound
                .Fingerprint);
    }

    [Fact]
    public void Missing_or_partial_loadout_resolves_partial()
    {
        var missing = ExactTargetEncounterPhaseResolver.Resolve(
            Rule,
            Observation(includeLoadout: false));
        var partial = ExactTargetEncounterPhaseResolver.Resolve(
            Rule,
            Observation(
                coverage: TargetLoadoutCoverageKind.PartialLoadout,
                signatures: Rule.ExpectedEquippedSkills.Take(2)));

        Assert.Equal(TargetEncounterBindingStatus.Partial, missing.Status);
        Assert.Equal(
            ["TARGET_LOADOUT_EVIDENCE_MISSING"],
            missing.Diagnostics);
        Assert.Equal(TargetEncounterBindingStatus.Partial, partial.Status);
        Assert.Equal(
            ["TARGET_LOADOUT_COVERAGE_PARTIAL"],
            partial.Diagnostics);
    }

    [Fact]
    public void Multiple_phase_templates_resolve_conflicting()
    {
        var result = ExactTargetEncounterPhaseResolver.Resolve(
            Rule,
            Observation(addConflictingPhase: true));

        Assert.Equal(TargetEncounterBindingStatus.Conflicting, result.Status);
        Assert.Equal(
            ["TARGET_PHASE_EVIDENCE_CONFLICT"],
            result.Diagnostics);
    }

    [Fact]
    public void Mismatched_source_version_resolves_conflicting()
    {
        var result = ExactTargetEncounterPhaseResolver.Resolve(
            Rule,
            Observation(evidenceGameDataVersion: "older-version"));

        Assert.Equal(
            TargetEncounterBindingStatus.Conflicting,
            result.Status);
        Assert.Equal(
            ["TARGET_EVIDENCE_VERSION_CONFLICT"],
            result.Diagnostics);
    }

    [Fact]
    public void Different_template_or_signature_resolves_wrong_phase()
    {
        var differentTemplate = ExactTargetEncounterPhaseResolver.Resolve(
            Rule,
            Observation(targetTemplateId: 718));
        var differentSignature = ExactTargetEncounterPhaseResolver.Resolve(
            Rule,
            Observation(signatures:
            [
                .. Rule.ExpectedEquippedSkills.SkipLast(1),
                new TargetThreatSkillSignature(
                    733,
                    PracticeDirection.Direct,
                    999)
            ]));

        Assert.Equal(
            TargetEncounterBindingStatus.WrongPhase,
            differentTemplate.Status);
        Assert.Equal(
            ["TARGET_PHASE_DOES_NOT_MATCH_RULE"],
            differentTemplate.Diagnostics);
        Assert.Equal(
            TargetEncounterBindingStatus.WrongPhase,
            differentSignature.Status);
        Assert.Equal(
            ["TARGET_LOADOUT_DOES_NOT_MATCH_PHASE"],
            differentSignature.Diagnostics);
    }

    [Fact]
    public void Different_game_version_resolves_unsupported()
    {
        var result = ExactTargetEncounterPhaseResolver.Resolve(
            Rule,
            Observation(gameDataVersion: "future-version"));

        Assert.Equal(
            TargetEncounterBindingStatus.UnsupportedVersion,
            result.Status);
        Assert.Equal(
            ["UNSUPPORTED_GAME_DATA_VERSION"],
            result.Diagnostics);
    }

    [Fact]
    public void Phase_contract_separates_verified_absent_and_live_facts()
    {
        var reset = Assert.Single(Rule.Facts, item =>
            item.Code == "DEFEAT_MARK_RESET_NOT_PRESENT");
        Assert.Equal(TargetEncounterFactState.NotPresent, reset.State);
        Assert.Equal([287], reset.SourceSkillIds);

        var suppression = Assert.Single(Rule.Facts, item =>
            item.Code == "REVERSE_604_FULL_DIRECT_COVERAGE");
        Assert.Equal(TargetEncounterFactState.Confirmed, suppression.State);
        Assert.Equal(34, suppression.SourceSkillIds.Length);

        var liveKinds = new[]
        {
            TargetEncounterFactKind.ActiveInnerPowerState,
            TargetEncounterFactKind.LiveMarkCount,
            TargetEncounterFactKind.LiveRhythmCount,
            TargetEncounterFactKind.LiveTemporaryLayers,
            TargetEncounterFactKind.CurrentDistance,
            TargetEncounterFactKind.CurrentResourceState,
            TargetEncounterFactKind.ActiveAgility
        };
        Assert.All(
            liveKinds,
            kind => Assert.Contains(
                Rule.Facts,
                item => item.Kind == kind
                    && item.State
                        == TargetEncounterFactState.ManualObservationRequired));
    }

    private static TargetEncounterPhaseObservation Observation(
        string gameDataVersion =
            VerifiedExactTargetEncounterRuleSets.CurrentGameDataVersion,
        int targetTemplateId =
            VerifiedExactTargetEncounterRuleSets
                .LaterMagicSoundTargetTemplateId,
        bool includeLoadout = true,
        bool addConflictingPhase = false,
        TargetLoadoutCoverageKind coverage =
            TargetLoadoutCoverageKind.CompleteCurrentLoadout,
        IEnumerable<TargetThreatSkillSignature>? signatures = null,
        string? evidenceGameDataVersion = null)
    {
        evidenceGameDataVersion ??= gameDataVersion;
        TargetEncounterPhaseEvidence[] phases = addConflictingPhase
            ?
            [
                Phase(targetTemplateId, evidenceGameDataVersion),
                Phase(targetTemplateId - 1, evidenceGameDataVersion)
            ]
            : [Phase(targetTemplateId, evidenceGameDataVersion)];
        if (!includeLoadout)
        {
            return new TargetEncounterPhaseObservation(
                gameDataVersion,
                phases);
        }

        return new TargetEncounterPhaseObservation(
            gameDataVersion,
            phases,
            coverage,
            signatures ?? Rule.ExpectedEquippedSkills,
            Evidence(TargetEncounterEvidenceSource.SavedEquippedLoadout,
                evidenceGameDataVersion));
    }

    private static TargetEncounterPhaseEvidence Phase(
        int targetTemplateId,
        string gameDataVersion) => new(
        targetTemplateId,
        Evidence(TargetEncounterEvidenceSource.SavedStoryTemplate,
            gameDataVersion));

    private static TargetEncounterEvidence Evidence(
        TargetEncounterEvidenceSource source,
        string gameDataVersion) => new(
        source,
        "SYNTHETIC-EXACT-TARGET-FIXTURE",
        gameDataVersion);
}
