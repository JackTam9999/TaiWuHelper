using System.Collections.Immutable;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Application.CombatSkills;

public sealed record TargetSkillSelectionRequest
{
    public TargetSkillSelectionRequest(
        TargetObservationContext observationContext,
        CatalogueLanguage preferredLanguage,
        string query,
        SkillCategory reportedCategory,
        int? confirmedSkillId = null,
        PracticeDirection? direction = null,
        int? slotIndex = null,
        IEnumerable<int>? targetSnapshotSkillIds = null,
        int? visiblePowerPercent = null)
    {
        if (!Enum.IsDefined(observationContext))
        {
            throw new ArgumentOutOfRangeException(
                nameof(observationContext),
                observationContext,
                "Unknown target-observation context.");
        }

        if (!Enum.IsDefined(preferredLanguage))
        {
            throw new ArgumentOutOfRangeException(
                nameof(preferredLanguage),
                preferredLanguage,
                "Unknown catalogue language.");
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException(
                "Target skill selection requires a visible skill name.",
                nameof(query));
        }

        var normalizedQuery = query.Trim();
        if (normalizedQuery.Length > CombatSkillSearchRequest.MaximumQueryLength)
        {
            throw new ArgumentException(
                "Target skill selection query exceeds the supported length.",
                nameof(query));
        }

        if (!Enum.IsDefined(reportedCategory))
        {
            throw new ArgumentOutOfRangeException(
                nameof(reportedCategory),
                reportedCategory,
                "Unknown reported skill category.");
        }

        if (confirmedSkillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(confirmedSkillId),
                confirmedSkillId,
                "A confirmed skill ID cannot be negative.");
        }

        if (direction is not null
            && direction is not PracticeDirection.Direct
                and not PracticeDirection.Reverse)
        {
            throw new ArgumentOutOfRangeException(
                nameof(direction),
                direction,
                "Only visibly verified direct or reverse directions are "
                + "supported.");
        }

        if (slotIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotIndex),
                slotIndex,
                "A reported slot index cannot be negative.");
        }

        if (visiblePowerPercent < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(visiblePowerPercent),
                visiblePowerPercent,
                "A visible target-skill power percentage cannot be negative.");
        }

        ImmutableHashSet<int>? snapshotIds = null;
        if (targetSnapshotSkillIds is not null)
        {
            var values = targetSnapshotSkillIds.ToImmutableArray();
            if (values.Any(skillId => skillId < 0))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetSnapshotSkillIds),
                    "Target snapshot skill IDs cannot be negative.");
            }

            if (values.Distinct().Count() != values.Length)
            {
                throw new ArgumentException(
                    "Target snapshot skill IDs cannot be duplicated.",
                    nameof(targetSnapshotSkillIds));
            }

            snapshotIds = values.ToImmutableHashSet();
        }

        ObservationContext = observationContext;
        PreferredLanguage = preferredLanguage;
        Query = normalizedQuery;
        ReportedCategory = reportedCategory;
        ConfirmedSkillId = confirmedSkillId;
        Direction = direction;
        SlotIndex = slotIndex;
        TargetSnapshotSkillIds = snapshotIds;
        VisiblePowerPercent = visiblePowerPercent;
    }

    public TargetObservationContext ObservationContext { get; }

    public CatalogueLanguage PreferredLanguage { get; }

    public string Query { get; }

    public SkillCategory ReportedCategory { get; }

    public int? ConfirmedSkillId { get; }

    public PracticeDirection? Direction { get; }

    public int? SlotIndex { get; }

    public ImmutableHashSet<int>? TargetSnapshotSkillIds { get; }

    public int? VisiblePowerPercent { get; }
}

public enum TargetSkillMatchKind
{
    Exact,
    Partial
}

public enum TargetSkillSnapshotPresence
{
    Unknown,
    Present,
    Absent
}

public sealed record TargetSkillStaticFacts
{
    public TargetSkillStaticFacts(
        int skillId,
        CombatSkillDisplayName displayName,
        SkillCategory category,
        CatalogueField<CombatSkillGridCost> baseGridCost,
        CatalogueField<SkillSlotContribution> slotContribution,
        CatalogueField<CombatSkillElement> element,
        CatalogueField<CombatSkillEffectId> directEffect,
        CatalogueField<CombatSkillEffectId> reverseEffect)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "Resolved skill ID cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(displayName);
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "Unknown resolved skill category.");
        }

        SkillId = skillId;
        DisplayName = displayName;
        Category = category;
        BaseGridCost = baseGridCost
            ?? throw new ArgumentNullException(nameof(baseGridCost));
        SlotContribution = slotContribution
            ?? throw new ArgumentNullException(nameof(slotContribution));
        Element = element ?? throw new ArgumentNullException(nameof(element));
        DirectEffect = directEffect
            ?? throw new ArgumentNullException(nameof(directEffect));
        ReverseEffect = reverseEffect
            ?? throw new ArgumentNullException(nameof(reverseEffect));
    }

    public int SkillId { get; }

    public CombatSkillDisplayName DisplayName { get; }

    public SkillCategory Category { get; }

    public CatalogueField<CombatSkillGridCost> BaseGridCost { get; }

    public CatalogueField<SkillSlotContribution> SlotContribution { get; }

    public CatalogueField<CombatSkillElement> Element { get; }

    public CatalogueField<CombatSkillEffectId> DirectEffect { get; }

    public CatalogueField<CombatSkillEffectId> ReverseEffect { get; }

    public CombatSkillSnapshot CreateSnapshot(
        ObservedTargetCombatSkill observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        if (observation.SkillId != SkillId
            || observation.Category != Category)
        {
            throw new ArgumentException(
                "The observation must identify these resolved static facts.",
                nameof(observation));
        }

        if (!SlotContribution.IsAvailable)
        {
            throw new InvalidOperationException(
                "A resolved observed skill requires verified slot "
                + "contribution facts.");
        }

        return new CombatSkillSnapshot(
            SkillId,
            MapDisplayName(DisplayName),
            Category,
            MapGridCost(BaseGridCost),
            SnapshotValue<bool>.Unavailable(
                "Mastery was not observed for this target skill."),
            observation.Direction is null
                ? SnapshotValue<PracticeDirection>.Unavailable(
                    "Practice direction was not observed.")
                : SnapshotValue<PracticeDirection>.Available(
                    observation.Direction.Value),
            SlotContribution.Value,
            MapEffect(DirectEffect),
            MapEffect(ReverseEffect),
            element: MapElement(Element));
    }

    private static SnapshotValue<string> MapDisplayName(
        CombatSkillDisplayName displayName) =>
        displayName.Value.IsAvailable
            ? SnapshotValue<string>.Available(displayName.Value.Value.Text)
            : SnapshotValue<string>.Unavailable(
                displayName.Value.Reason ?? "Display name is unavailable.");

    private static SnapshotValue<int> MapGridCost(
        CatalogueField<CombatSkillGridCost> value) => value.IsAvailable
            ? SnapshotValue<int>.Available(value.Value.Value)
            : SnapshotValue<int>.Unavailable(
                value.Reason ?? "Grid cost is unavailable.");

    private static SnapshotValue<int> MapEffect(
        CatalogueField<CombatSkillEffectId> value) => value.IsAvailable
            ? SnapshotValue<int>.Available(value.Value.Value)
            : SnapshotValue<int>.Unavailable(
                value.Reason ?? "Effect ID is unavailable.");

    private static SnapshotValue<CombatSkillElement> MapElement(
        CatalogueField<CombatSkillElement> value) => value.IsAvailable
            ? SnapshotValue<CombatSkillElement>.Available(value.Value)
            : SnapshotValue<CombatSkillElement>.Unavailable(
                value.Reason ?? "Element is unavailable.");
}

public sealed record TargetSkillResolutionCandidate(
    int SkillId,
    CombatSkillDisplayName DisplayName,
    TargetSkillMatchKind MatchKind,
    TargetSkillSnapshotPresence SnapshotPresence,
    TargetSkillStaticFacts? StaticFacts)
{
    public string StableKey => $"combat-skill:{SkillId}";
}

public sealed record ResolvedTargetSkillSelection(
    ObservedTargetCombatSkill Observation,
    TargetSkillStaticFacts StaticFacts,
    TargetSkillSnapshotPresence SnapshotPresence);

public enum TargetSkillSelectionStatus
{
    Resolved,
    ConfirmationRequired,
    Ambiguous,
    DefinitionMissing,
    ConfirmationInvalid,
    CategoryMismatch,
    DefinitionUnsupported,
    CatalogueMissing,
    CatalogueStale,
    CatalogueRebuilding,
    CatalogueUnsupportedVersion,
    CatalogueUnavailable
}

public sealed record TargetSkillSelectionResult(
    TargetSkillSelectionStatus Status,
    CombatSkillCatalogueStatusResult Catalogue,
    bool CandidateSetMayBeTruncated,
    ImmutableArray<TargetSkillResolutionCandidate> Candidates,
    ResolvedTargetSkillSelection? ResolvedSelection)
{
    public static TargetSkillSelectionStatus MapCatalogueStatus(
        CombatSkillCatalogueStatus status) => status switch
        {
            CombatSkillCatalogueStatus.Missing
                or CombatSkillCatalogueStatus.MissingSources =>
                TargetSkillSelectionStatus.CatalogueMissing,
            CombatSkillCatalogueStatus.Stale =>
                TargetSkillSelectionStatus.CatalogueStale,
            CombatSkillCatalogueStatus.Rebuilding =>
                TargetSkillSelectionStatus.CatalogueRebuilding,
            CombatSkillCatalogueStatus.UnsupportedVersion =>
                TargetSkillSelectionStatus.CatalogueUnsupportedVersion,
            CombatSkillCatalogueStatus.Current => throw new ArgumentException(
                "A current catalogue does not map to an unavailable status.",
                nameof(status)),
            _ => TargetSkillSelectionStatus.CatalogueUnavailable
        };
}
