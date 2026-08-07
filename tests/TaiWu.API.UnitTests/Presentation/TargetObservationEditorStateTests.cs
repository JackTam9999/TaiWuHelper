using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed class TargetObservationEditorStateTests
{
    private static readonly DateTimeOffset ObservedAt = DateTimeOffset.Parse(
        "2026-08-07T21:00:00Z");

    [Fact]
    public void Editor_starts_disabled_and_hidden_context_is_unavailable()
    {
        var state = new TargetObservationEditorState();

        state.SetEnabled(enabled: true, hasInitialRecommendation: false);

        Assert.False(state.IsEnabled);
        Assert.Equal(TargetObservationEditorStatus.Error, state.Status);
        Assert.Equal(
            "INITIAL_RECOMMENDATION_REQUIRED",
            state.ValidationCode);

        state.SetEnabled(enabled: true, hasInitialRecommendation: true);
        state.SetContext(TargetObservationContext.Story);

        Assert.True(state.IsEnabled);
        Assert.Equal(TargetObservationEditorStatus.Unavailable, state.Status);
        Assert.False(state.CanEdit);
        Assert.Empty(state.SelectedSkills);
    }

    [Fact]
    public void Ambiguous_search_preserves_verified_candidate_details()
    {
        var state = EditingState();
        state.Query = "Target Art";
        Assert.True(state.BeginSearch());

        state.SetSearchResult(new TargetSkillSelectionResult(
            TargetSkillSelectionStatus.Ambiguous,
            CurrentCatalogue(),
            CandidateSetMayBeTruncated: false,
            [Candidate(719, "Target Art"), Candidate(720, "Target Art Advanced")],
            ResolvedSelection: null));

        Assert.Equal(TargetObservationEditorStatus.Ambiguous, state.Status);
        Assert.Equal("AMBIGUOUS_SKILL", state.ValidationCode);
        Assert.Equal([719, 720], state.Candidates.Select(value => value.SkillId));
        Assert.All(
            state.Candidates,
            candidate => Assert.Equal(SkillCategory.Attack, candidate.Category));
    }

    [Fact]
    public void Resolved_skill_builds_reviewed_typed_request()
    {
        var state = EditingState();
        var facts = Facts(719, "Target Art");
        state.AddResolved(new ResolvedTargetSkillSelection(
            new ObservedTargetCombatSkill(
                719,
                SkillCategory.Attack,
                direction: null,
                slotIndex: 0),
            facts,
            TargetSkillSnapshotPresence.Present));
        state.SetDirection(719, PracticeDirection.Reverse);

        Assert.True(state.BeginReview(ObservedAt));
        var request = state.BuildRequest();

        Assert.Equal(TargetObservationEditorStatus.Review, state.Status);
        Assert.Equal(ObservedAt, request.ObservedAtUtc);
        Assert.Equal(TargetObservationContext.Sparring, request.Context);
        Assert.Equal(
            TargetLoadoutCoverageKind.PartialLoadout,
            request.Coverage);
        var selected = Assert.Single(request.SelectedSkills);
        Assert.Equal("Target Art", selected.VisibleName);
        Assert.Equal(719, selected.ConfirmedSkillId);
        Assert.Equal(PracticeDirection.Reverse, selected.Direction);
        Assert.Equal(SkillCategory.Attack, selected.Category);
    }

    [Fact]
    public void Complete_review_can_explicitly_confirm_an_empty_loadout()
    {
        var state = EditingState();
        state.SetCoverage(
            TargetLoadoutCoverageKind.CompleteCurrentLoadout);

        Assert.True(state.BeginReview(ObservedAt));
        Assert.Empty(state.BuildRequest().SelectedSkills);
    }

    [Theory]
    [InlineData(
        TargetLoadoutMergeStatus.Stale,
        SnapshotEvidenceStatus.Stale,
        TargetObservationEditorStatus.Stale)]
    [InlineData(
        TargetLoadoutMergeStatus.UnsupportedVersion,
        SnapshotEvidenceStatus.Unavailable,
        TargetObservationEditorStatus.Unsupported)]
    [InlineData(
        TargetLoadoutMergeStatus.PrecedenceConfirmationRequired,
        SnapshotEvidenceStatus.Unavailable,
        TargetObservationEditorStatus.PrecedenceConfirmationRequired)]
    [InlineData(
        TargetLoadoutMergeStatus.Applied,
        SnapshotEvidenceStatus.Available,
        TargetObservationEditorStatus.Applied)]
    [InlineData(
        TargetLoadoutMergeStatus.Applied,
        SnapshotEvidenceStatus.Conflicting,
        TargetObservationEditorStatus.Conflicting)]
    public void Merge_status_maps_to_explicit_editor_state(
        TargetLoadoutMergeStatus mergeStatus,
        SnapshotEvidenceStatus evidenceStatus,
        TargetObservationEditorStatus expected)
    {
        var state = EditingState();

        state.MarkResult(MergeResult(mergeStatus, evidenceStatus));

        Assert.Equal(expected, state.Status);
    }

    [Fact]
    public void Clear_removes_session_observation_state()
    {
        var state = EditingState();
        state.Query = "Target Art";

        state.Clear();

        Assert.False(state.IsEnabled);
        Assert.Equal(TargetObservationEditorStatus.Cleared, state.Status);
        Assert.Empty(state.SelectedSkills);
        Assert.Empty(state.Candidates);
        Assert.Null(state.ObservedAtUtc);
    }

    private static TargetObservationEditorState EditingState()
    {
        var state = new TargetObservationEditorState();
        state.SetEnabled(enabled: true, hasInitialRecommendation: true);
        state.SetContext(TargetObservationContext.Sparring);
        return state;
    }

    private static TargetSkillResolutionCandidate Candidate(
        int skillId,
        string name) => new(
            skillId,
            Facts(skillId, name).DisplayName,
            skillId == 719
                ? TargetSkillMatchKind.Exact
                : TargetSkillMatchKind.Partial,
            TargetSkillSnapshotPresence.Unknown,
            Facts(skillId, name));

    private static TargetSkillStaticFacts Facts(int skillId, string name)
    {
        var source = new CatalogueSourceReference(
            CatalogueSourceKind.GameData,
            "gamedata:test",
            $"combat-skill:{skillId}");
        var localized = new LocalizedCombatSkillName(
            CatalogueLanguage.English,
            name,
            new CatalogueSourceReference(
                CatalogueSourceKind.EnglishLanguageResource,
                "language-en:test",
                $"combat-skill-name:{skillId}"));
        return new TargetSkillStaticFacts(
            skillId,
            new CombatSkillDisplayName(
                CatalogueLanguage.English,
                CatalogueField<LocalizedCombatSkillName>.Available(
                    localized,
                    localized.Source),
                UsedFallback: false),
            SkillCategory.Attack,
            CatalogueField<CombatSkillGridCost>.Available(
                new CombatSkillGridCost(2),
                source),
            CatalogueField<SkillSlotContribution>.Available(
                new SkillSlotContribution(2, 0, 0, 0, 1),
                source),
            CatalogueField<CombatSkillElement>.Available(
                CombatSkillElement.Wood,
                source),
            CatalogueField<CombatSkillEffectId>.Available(
                new CombatSkillEffectId(1000 + skillId),
                source),
            CatalogueField<CombatSkillEffectId>.Available(
                new CombatSkillEffectId(2000 + skillId),
                source));
    }

    private static CombatSkillCatalogueStatusResult CurrentCatalogue() => new(
        CombatSkillCatalogueStatus.Current,
        DefinitionCount: 2,
        InstalledSource: null,
        StoredSource: null,
        BuiltAtUtc: ObservedAt,
        Reason: null);

    private static TargetLoadoutObservationMergeResult MergeResult(
        TargetLoadoutMergeStatus status,
        SnapshotEvidenceStatus evidenceStatus)
    {
        var loadout = new CombatLoadoutSnapshot([], [], [], [], []);
        var evidence = evidenceStatus switch
        {
            SnapshotEvidenceStatus.Available =>
                SnapshotEvidenceField<CombatLoadoutSnapshot>.Available(
                    loadout,
                    ScreenSource()),
            SnapshotEvidenceStatus.Stale =>
                SnapshotEvidenceField<CombatLoadoutSnapshot>.Stale(
                    "OBSERVATION_NOT_NEWER_THAN_SAVE",
                    [new SnapshotFieldObservation<CombatLoadoutSnapshot>(
                        loadout,
                        ScreenSource())]),
            SnapshotEvidenceStatus.Conflicting =>
                SnapshotEvidenceField<CombatLoadoutSnapshot>.Conflicting(
                    "SAVE_SCREEN_CONFLICT",
                    [
                        new SnapshotFieldObservation<CombatLoadoutSnapshot>(
                            loadout,
                            ScreenSource()),
                        new SnapshotFieldObservation<CombatLoadoutSnapshot>(
                            new CombatLoadoutSnapshot([], [719], [], [], []),
                            new SnapshotFieldSource(
                                "target.equippedSkills",
                                SnapshotDataSource.Save,
                                ObservedAt.AddMinutes(-1),
                                "save:test"))
                    ]),
            _ => SnapshotEvidenceField<CombatLoadoutSnapshot>.Unavailable(
                "OBSERVATION_UNAVAILABLE")
        };
        return new TargetLoadoutObservationMergeResult(
            status,
            Snapshot(),
            new TargetLoadoutObservation(
                16317,
                TargetObservationContext.Sparring,
                ObservedAt,
                "ui:target-observation",
                TargetLoadoutCoverage.PartialLoadout,
                []),
            evidence);
    }

    private static SnapshotFieldSource ScreenSource() => new(
        "target.equippedSkills",
        SnapshotDataSource.CurrentScreenObservation,
        ObservedAt,
        "ui:target-observation");

    private static CombatSnapshot Snapshot() => new(
        new CombatSnapshotMetadata(
            "local.sav",
            new string('A', 64),
            ObservedAt,
            SnapshotValue<DateTimeOffset>.Available(
                ObservedAt.AddMinutes(-1)),
            SnapshotValue<string>.Available(
                TargetLoadoutCompletenessEvidence.E3000GameDataVersion)),
        new PlayerCombatSnapshot(
            1,
            SnapshotValue<string>.Available("Taiwu"),
            learnedSkills: [],
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
            learnedSkills: [],
            SnapshotValue<CombatLoadoutSnapshot>.Available(
                new CombatLoadoutSnapshot([], [], [], [], [])),
            equipment: []),
        warnings: []);
}
