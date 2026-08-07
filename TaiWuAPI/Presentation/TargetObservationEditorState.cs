using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWuAPI.Presentation;

public enum TargetObservationEditorStatus
{
    Initial,
    Editing,
    Searching,
    Ambiguous,
    Review,
    Applying,
    Applied,
    Stale,
    Conflicting,
    Unsupported,
    PrecedenceConfirmationRequired,
    Unavailable,
    Error,
    Cleared
}

public sealed class TargetObservationEditorState
{
    private readonly List<TargetObservationCandidateViewModel> _candidates = [];
    private readonly List<TargetObservationSelectedSkillViewModel>
        _selectedSkills = [];

    public bool IsEnabled { get; private set; }

    public TargetObservationContext? Context { get; private set; }

    public TargetLoadoutCoverageKind Coverage { get; set; } =
        TargetLoadoutCoverageKind.PartialLoadout;

    public string Query { get; set; } = string.Empty;

    public bool ConfirmPrecedenceWhenSaveTimeUnavailable { get; set; }

    public TargetObservationEditorStatus Status { get; private set; } =
        TargetObservationEditorStatus.Initial;

    public string? ValidationCode { get; private set; }

    public DateTimeOffset? ObservedAtUtc { get; private set; }

    public string EvidenceReference { get; private set; } =
        "ui:target-observation";

    public IReadOnlyList<TargetObservationCandidateViewModel> Candidates =>
        _candidates;

    public IReadOnlyList<TargetObservationSelectedSkillViewModel>
        SelectedSkills => _selectedSkills;

    public bool CanEdit => IsEnabled
        && Context == TargetObservationContext.Sparring
        && Status is not TargetObservationEditorStatus.Applying;

    public bool CanReview => CanEdit
        && (Coverage == TargetLoadoutCoverageKind.CompleteCurrentLoadout
            || _selectedSkills.Count > 0);

    public bool CanApply => Status == TargetObservationEditorStatus.Review
        && ObservedAtUtc.HasValue;

    public void SetEnabled(bool enabled, bool hasInitialRecommendation)
    {
        if (!enabled)
        {
            Reset(TargetObservationEditorStatus.Initial);
            return;
        }

        if (!hasInitialRecommendation)
        {
            ValidationCode = "INITIAL_RECOMMENDATION_REQUIRED";
            Status = TargetObservationEditorStatus.Error;
            IsEnabled = false;
            return;
        }

        IsEnabled = true;
        Context = null;
        Status = TargetObservationEditorStatus.Editing;
        ValidationCode = null;
    }

    public void SetContext(TargetObservationContext context)
    {
        if (!Enum.IsDefined(context))
        {
            throw new ArgumentOutOfRangeException(
                nameof(context),
                context,
                "Unknown target-observation context.");
        }

        Context = context;
        _candidates.Clear();
        Query = string.Empty;
        ValidationCode = null;
        ObservedAtUtc = null;
        if (context == TargetObservationContext.Sparring)
        {
            Status = TargetObservationEditorStatus.Editing;
            return;
        }

        _selectedSkills.Clear();
        Status = TargetObservationEditorStatus.Unavailable;
    }

    public void SetCoverage(TargetLoadoutCoverageKind coverage)
    {
        if (!Enum.IsDefined(coverage))
        {
            throw new ArgumentOutOfRangeException(
                nameof(coverage),
                coverage,
                "Unknown target-observation coverage.");
        }

        Coverage = coverage;
        ObservedAtUtc = null;
        ValidationCode = null;
        if (Context == TargetObservationContext.Sparring)
        {
            Status = TargetObservationEditorStatus.Editing;
        }
    }

    public bool BeginSearch()
    {
        if (!CanEdit)
        {
            ValidationCode = "SPARRING_CONTEXT_REQUIRED";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Query))
        {
            ValidationCode = "VISIBLE_SKILL_NAME_REQUIRED";
            Status = TargetObservationEditorStatus.Error;
            return false;
        }

        _candidates.Clear();
        ValidationCode = null;
        Status = TargetObservationEditorStatus.Searching;
        return true;
    }

    public void SetSearchResult(TargetSkillSelectionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _candidates.Clear();
        _candidates.AddRange(result.Candidates
            .Where(candidate => candidate.StaticFacts is not null)
            .Select(TargetObservationCandidateViewModel.From)
            .OrderBy(candidate => candidate.Match)
            .ThenBy(candidate => candidate.Name, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.SkillId));

        ValidationCode = result.Status switch
        {
            TargetSkillSelectionStatus.ConfirmationRequired => null,
            TargetSkillSelectionStatus.Ambiguous => "AMBIGUOUS_SKILL",
            TargetSkillSelectionStatus.DefinitionMissing =>
                "SKILL_DEFINITION_MISSING",
            TargetSkillSelectionStatus.CatalogueMissing =>
                "CATALOGUE_MISSING",
            TargetSkillSelectionStatus.CatalogueStale => "CATALOGUE_STALE",
            TargetSkillSelectionStatus.CatalogueRebuilding =>
                "CATALOGUE_REBUILDING",
            TargetSkillSelectionStatus.CatalogueUnsupportedVersion =>
                "CATALOGUE_UNSUPPORTED",
            TargetSkillSelectionStatus.CatalogueUnavailable =>
                "CATALOGUE_UNAVAILABLE",
            _ => "SKILL_SELECTION_INVALID"
        };
        Status = result.Status == TargetSkillSelectionStatus.Ambiguous
            ? TargetObservationEditorStatus.Ambiguous
            : _candidates.Count > 0
                ? TargetObservationEditorStatus.Editing
                : TargetObservationEditorStatus.Error;
    }

    public void AddResolved(ResolvedTargetSkillSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        var display = selection.StaticFacts.DisplayName;
        var name = display.Value.IsAvailable
            ? display.Value.Value.Text
            : $"combat-skill:{selection.Observation.SkillId}";
        _selectedSkills.RemoveAll(skill =>
            skill.SkillId == selection.Observation.SkillId);
        _selectedSkills.Add(new TargetObservationSelectedSkillViewModel(
            selection.Observation.SkillId,
            name,
            selection.Observation.Category,
            selection.Observation.Direction,
            selection.Observation.SlotIndex,
            selection.SnapshotPresence));
        _selectedSkills.Sort((left, right) =>
        {
            var category = left.Category.CompareTo(right.Category);
            return category != 0
                ? category
                : string.Compare(left.Name, right.Name, StringComparison.Ordinal);
        });
        _candidates.Clear();
        Query = string.Empty;
        ValidationCode = null;
        Status = TargetObservationEditorStatus.Editing;
    }

    public void RemoveSkill(int skillId)
    {
        _selectedSkills.RemoveAll(skill => skill.SkillId == skillId);
        ObservedAtUtc = null;
        Status = TargetObservationEditorStatus.Editing;
    }

    public void SetDirection(int skillId, PracticeDirection? direction)
    {
        if (direction is not null
            && direction is not PracticeDirection.Direct
                and not PracticeDirection.Reverse)
        {
            throw new ArgumentOutOfRangeException(
                nameof(direction),
                direction,
                "Only direct, reverse, or unavailable direction is valid.");
        }

        var index = _selectedSkills.FindIndex(skill =>
            skill.SkillId == skillId);
        if (index < 0)
        {
            throw new ArgumentException(
                "The selected target skill was not found.",
                nameof(skillId));
        }

        _selectedSkills[index] = _selectedSkills[index] with
        {
            Direction = direction
        };
        ObservedAtUtc = null;
        Status = TargetObservationEditorStatus.Editing;
    }

    public bool BeginReview(DateTimeOffset observedAt)
    {
        if (!CanReview)
        {
            ValidationCode = Coverage
                == TargetLoadoutCoverageKind.PartialLoadout
                ? "PARTIAL_SKILL_REQUIRED"
                : "OBSERVATION_NOT_READY";
            Status = TargetObservationEditorStatus.Error;
            return false;
        }

        ObservedAtUtc = observedAt.ToUniversalTime();
        ValidationCode = null;
        Status = TargetObservationEditorStatus.Review;
        return true;
    }

    public TargetObservationRequest BuildRequest()
    {
        if (!CanApply || Context is null)
        {
            throw new InvalidOperationException(
                "The target observation is not ready to apply.");
        }

        return new TargetObservationRequest(
            Context.Value,
            ObservedAtUtc!.Value,
            EvidenceReference,
            Coverage,
            _selectedSkills.Select(skill => new TargetObservedSkillRequest(
                skill.Name,
                skill.Category,
                skill.SkillId,
                skill.Direction,
                skill.SlotIndex)),
            ConfirmPrecedenceWhenSaveTimeUnavailable);
    }

    public void MarkApplying()
    {
        if (!CanApply)
        {
            throw new InvalidOperationException(
                "The target observation is not ready to apply.");
        }

        Status = TargetObservationEditorStatus.Applying;
    }

    public void MarkResult(TargetLoadoutObservationMergeResult merge)
    {
        ArgumentNullException.ThrowIfNull(merge);
        Status = merge.Status switch
        {
            TargetLoadoutMergeStatus.Stale =>
                TargetObservationEditorStatus.Stale,
            TargetLoadoutMergeStatus.UnsupportedVersion =>
                TargetObservationEditorStatus.Unsupported,
            TargetLoadoutMergeStatus.PrecedenceConfirmationRequired =>
                TargetObservationEditorStatus.PrecedenceConfirmationRequired,
            TargetLoadoutMergeStatus.Applied
                when merge.LoadoutEvidence.Status
                    == SnapshotEvidenceStatus.Conflicting
                    || merge.DirectionEvidence.Any(value =>
                        value.Evidence.Status
                            == SnapshotEvidenceStatus.Conflicting) =>
                TargetObservationEditorStatus.Conflicting,
            TargetLoadoutMergeStatus.Applied =>
                TargetObservationEditorStatus.Applied,
            _ => TargetObservationEditorStatus.Error
        };
        ValidationCode = null;
    }

    public void MarkError(string validationCode)
    {
        if (string.IsNullOrWhiteSpace(validationCode))
        {
            throw new ArgumentException(
                "An editor error requires a stable validation code.",
                nameof(validationCode));
        }

        ValidationCode = validationCode.Trim();
        Status = TargetObservationEditorStatus.Error;
    }

    public void Clear() => Reset(TargetObservationEditorStatus.Cleared);

    public void ResetForTarget() => Reset(TargetObservationEditorStatus.Initial);

    private void Reset(TargetObservationEditorStatus status)
    {
        IsEnabled = false;
        Context = null;
        Coverage = TargetLoadoutCoverageKind.PartialLoadout;
        Query = string.Empty;
        ConfirmPrecedenceWhenSaveTimeUnavailable = false;
        Status = status;
        ValidationCode = null;
        ObservedAtUtc = null;
        EvidenceReference = "ui:target-observation";
        _candidates.Clear();
        _selectedSkills.Clear();
    }
}

public sealed record TargetObservationCandidateViewModel(
    int SkillId,
    string Name,
    SkillCategory Category,
    int? BaseGridCost,
    TargetSkillMatchKind Match,
    TargetSkillSnapshotPresence SnapshotPresence)
{
    internal static TargetObservationCandidateViewModel From(
        TargetSkillResolutionCandidate candidate)
    {
        var facts = candidate.StaticFacts
            ?? throw new ArgumentException(
                "A UI candidate requires resolved static facts.",
                nameof(candidate));
        return new TargetObservationCandidateViewModel(
            candidate.SkillId,
            candidate.DisplayName.Value.IsAvailable
                ? candidate.DisplayName.Value.Value.Text
                : $"combat-skill:{candidate.SkillId}",
            facts.Category,
            facts.BaseGridCost.IsAvailable
                ? facts.BaseGridCost.Value.Value
                : null,
            candidate.MatchKind,
            candidate.SnapshotPresence);
    }
}

public sealed record TargetObservationSelectedSkillViewModel(
    int SkillId,
    string Name,
    SkillCategory Category,
    PracticeDirection? Direction,
    int? SlotIndex,
    TargetSkillSnapshotPresence SnapshotPresence);
