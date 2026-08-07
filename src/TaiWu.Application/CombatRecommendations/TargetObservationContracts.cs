using System.Collections.Immutable;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Application.CombatRecommendations;

public sealed record TargetObservationRequest
{
    public TargetObservationRequest(
        TargetObservationContext context,
        DateTimeOffset observedAt,
        string evidenceReference,
        TargetLoadoutCoverageKind coverage,
        IEnumerable<TargetObservedSkillRequest> selectedSkills,
        bool confirmPrecedenceWhenSaveTimeUnavailable = false)
    {
        if (!Enum.IsDefined(context))
        {
            throw new ArgumentOutOfRangeException(
                nameof(context),
                context,
                "Unknown target-observation context.");
        }

        if (context != TargetObservationContext.Sparring)
        {
            throw new ArgumentException(
                "Current-screen target observations are available only "
                + "for sparring opponents.",
                nameof(context));
        }

        if (observedAt == default)
        {
            throw new ArgumentException(
                "A target observation requires a capture time.",
                nameof(observedAt));
        }

        if (!Enum.IsDefined(coverage))
        {
            throw new ArgumentOutOfRangeException(
                nameof(coverage),
                coverage,
                "Unknown target-observation coverage.");
        }

        ArgumentNullException.ThrowIfNull(selectedSkills);
        var skillValues = selectedSkills.ToImmutableArray();
        if (skillValues.Any(skill => skill is null))
        {
            throw new ArgumentException(
                "Selected target skills cannot contain null entries.",
                nameof(selectedSkills));
        }

        Context = context;
        ObservedAtUtc = observedAt.ToUniversalTime();
        EvidenceReference = SnapshotFieldSource.NormalizeEvidenceReference(
            evidenceReference);
        Coverage = coverage;
        SelectedSkills = skillValues;
        ConfirmPrecedenceWhenSaveTimeUnavailable =
            confirmPrecedenceWhenSaveTimeUnavailable;
    }

    public TargetObservationContext Context { get; }

    public DateTimeOffset ObservedAtUtc { get; }

    public string EvidenceReference { get; }

    public TargetLoadoutCoverageKind Coverage { get; }

    public ImmutableArray<TargetObservedSkillRequest> SelectedSkills { get; }

    public bool ConfirmPrecedenceWhenSaveTimeUnavailable { get; }
}

public sealed record TargetObservedSkillRequest
{
    public TargetObservedSkillRequest(
        string visibleName,
        SkillCategory category,
        int? confirmedSkillId = null,
        PracticeDirection? direction = null,
        int? slotIndex = null)
    {
        if (string.IsNullOrWhiteSpace(visibleName))
        {
            throw new ArgumentException(
                "A selected target skill requires its visible name.",
                nameof(visibleName));
        }

        var normalizedName = visibleName.Trim();
        if (normalizedName.Length
            > CombatSkillSearchRequest.MaximumQueryLength)
        {
            throw new ArgumentException(
                "The visible target skill name is too long.",
                nameof(visibleName));
        }

        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "Unknown selected target-skill category.");
        }

        if (confirmedSkillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(confirmedSkillId),
                confirmedSkillId,
                "A confirmed target skill ID cannot be negative.");
        }

        if (direction is not null
            && direction is not PracticeDirection.Direct
                and not PracticeDirection.Reverse)
        {
            throw new ArgumentOutOfRangeException(
                nameof(direction),
                direction,
                "Only a visibly verified direct or reverse direction is "
                + "supported.");
        }

        if (slotIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotIndex),
                slotIndex,
                "A selected target-skill slot cannot be negative.");
        }

        VisibleName = normalizedName;
        Category = category;
        ConfirmedSkillId = confirmedSkillId;
        Direction = direction;
        SlotIndex = slotIndex;
    }

    public string VisibleName { get; }

    public SkillCategory Category { get; }

    public int? ConfirmedSkillId { get; }

    public PracticeDirection? Direction { get; }

    public int? SlotIndex { get; }
}

public sealed record TargetObservationProcessingResult
{
    public TargetObservationProcessingResult(
        TargetLoadoutObservationMergeResult merge,
        IEnumerable<ResolvedTargetSkillSelection> resolvedSkills)
    {
        Merge = merge ?? throw new ArgumentNullException(nameof(merge));
        ArgumentNullException.ThrowIfNull(resolvedSkills);
        var values = resolvedSkills.ToImmutableArray();
        if (values.Any(value => value is null))
        {
            throw new ArgumentException(
                "Resolved target skills cannot contain null entries.",
                nameof(resolvedSkills));
        }

        ResolvedSkills = values;
    }

    public TargetLoadoutObservationMergeResult Merge { get; }

    public ImmutableArray<ResolvedTargetSkillSelection> ResolvedSkills
    {
        get;
    }
}

public sealed class TargetObservationResolutionException : Exception
{
    public TargetObservationResolutionException(
        TargetSkillSelectionStatus status,
        int selectionIndex,
        IEnumerable<TargetSkillResolutionCandidate>? candidates = null)
        : base("The target observation could not be resolved.")
    {
        if (!Enum.IsDefined(status)
            || status == TargetSkillSelectionStatus.Resolved)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "A resolution failure requires a non-resolved status.");
        }

        if (selectionIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(selectionIndex),
                selectionIndex,
                "A selection index cannot be negative.");
        }

        var candidateValues = (candidates ?? []).ToImmutableArray();
        if (candidateValues.Any(candidate => candidate is null))
        {
            throw new ArgumentException(
                "Resolution candidates cannot contain null entries.",
                nameof(candidates));
        }

        Status = status;
        SelectionIndex = selectionIndex;
        Candidates = candidateValues;
    }

    public TargetSkillSelectionStatus Status { get; }

    public int SelectionIndex { get; }

    public ImmutableArray<TargetSkillResolutionCandidate> Candidates
    {
        get;
    }
}
