namespace TaiWu.Domain.CombatSkills;

public enum CombatSkillStudyDetailGroup
{
    Outline = 0,
    Direct = 1,
    Reverse = 2
}

public enum CombatSkillStudyState
{
    Read = 0,
    NotRead = 1
}

public sealed record CombatSkillStudyDetailProgress
{
    public CombatSkillStudyDetailProgress(
        string detailId,
        int displayOrder,
        CombatSkillStudyDetailGroup group,
        CatalogueField<string> label,
        SkillProgressField<CombatSkillStudyState> readState,
        SkillProgressField<bool> isActive)
    {
        if (string.IsNullOrWhiteSpace(detailId))
        {
            throw new ArgumentException(
                "A study detail ID cannot be blank.",
                nameof(detailId));
        }

        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder),
                displayOrder,
                "Study-detail display order cannot be negative.");
        }

        if (!Enum.IsDefined(group))
        {
            throw new ArgumentOutOfRangeException(
                nameof(group),
                group,
                "Unknown study-detail group.");
        }

        ArgumentNullException.ThrowIfNull(label);
        if (label.IsAvailable && string.IsNullOrWhiteSpace(label.Value))
        {
            throw new ArgumentException(
                "An available study-detail label cannot be blank.",
                nameof(label));
        }

        ArgumentNullException.ThrowIfNull(readState);
        if (readState.IsAvailable && !Enum.IsDefined(readState.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(readState),
                readState.Value,
                "Unknown study-detail read state.");
        }

        ArgumentNullException.ThrowIfNull(isActive);
        DetailId = detailId.Trim();
        DisplayOrder = displayOrder;
        Group = group;
        Label = label;
        ReadState = readState;
        IsActive = isActive;
    }

    public string DetailId { get; }

    public int DisplayOrder { get; }

    public CombatSkillStudyDetailGroup Group { get; }

    public CatalogueField<string> Label { get; }

    public SkillProgressField<CombatSkillStudyState> ReadState { get; }

    public SkillProgressField<bool> IsActive { get; }
}

public sealed record CombatSkillStudySummary
{
    internal CombatSkillStudySummary(
        int totalCount,
        int availableCount,
        int readCount,
        int notReadCount,
        int unavailableCount,
        SkillProgressField<bool> isComplete)
    {
        TotalCount = totalCount;
        AvailableCount = availableCount;
        ReadCount = readCount;
        NotReadCount = notReadCount;
        UnavailableCount = unavailableCount;
        IsComplete = isComplete;
    }

    public int TotalCount { get; }

    public int AvailableCount { get; }

    public int ReadCount { get; }

    public int NotReadCount { get; }

    public int UnavailableCount { get; }

    public SkillProgressField<bool> IsComplete { get; }
}
