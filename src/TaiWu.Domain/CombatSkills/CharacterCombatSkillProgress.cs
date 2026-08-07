using System.Collections.Immutable;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatSkills;

public sealed record SaveSnapshotIdentity
{
    public SaveSnapshotIdentity(string sha256, DateTimeOffset readAtUtc)
    {
        if (sha256 is null
            || sha256.Length != 64
            || sha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "A save snapshot requires a 64-character SHA-256 value.",
                nameof(sha256));
        }

        Sha256 = sha256.ToUpperInvariant();
        ReadAtUtc = readAtUtc.ToUniversalTime();
    }

    public string Sha256 { get; }

    public DateTimeOffset ReadAtUtc { get; }
}

public sealed class CharacterCombatSkillProgress :
    IEquatable<CharacterCombatSkillProgress>
{
    private static readonly SkillProgressSource DerivedStudySource =
        new(
            SkillProgressSourceKind.VerifiedRule,
            "verified-rule:e2-002",
            "study-completeness");

    public CharacterCombatSkillProgress(
        int characterId,
        SaveSnapshotIdentity saveSnapshot,
        int skillId,
        SkillProgressField<bool> learned,
        CombatSkillProficiencyProgress proficiency,
        CombatSkillPowerProgress power,
        IEnumerable<CombatSkillStudyDetailProgress>? studyDetails,
        SkillProgressField<BreakthroughDirectionAvailability> breakthrough,
        SkillProgressField<PracticeDirection> activeDirection,
        SkillProgressField<bool> attainmentMastered,
        SkillProgressField<bool> simplified,
        SkillProgressField<bool> activated,
        SkillProgressField<bool> equipped)
    {
        if (characterId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(characterId),
                characterId,
                "A character ID cannot be negative.");
        }

        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "A combat-skill ID cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(saveSnapshot);
        ArgumentNullException.ThrowIfNull(learned);
        ArgumentNullException.ThrowIfNull(proficiency);
        ArgumentNullException.ThrowIfNull(power);
        ArgumentNullException.ThrowIfNull(breakthrough);
        ValidateActiveDirection(activeDirection, breakthrough);
        ArgumentNullException.ThrowIfNull(attainmentMastered);
        ValidateAttainmentMastery(attainmentMastered, breakthrough);
        ArgumentNullException.ThrowIfNull(simplified);
        ArgumentNullException.ThrowIfNull(activated);
        ArgumentNullException.ThrowIfNull(equipped);

        var detailValues = (studyDetails ?? []).ToImmutableArray();
        if (detailValues.Any(detail => detail is null))
        {
            throw new ArgumentException(
                "Study details cannot contain null.",
                nameof(studyDetails));
        }

        var details = detailValues
            .OrderBy(detail => detail.DisplayOrder)
            .ToImmutableArray();

        RejectDuplicateDetails(details, studyDetails);
        ValidateActivation(details, activated);

        CharacterId = characterId;
        SaveSnapshot = saveSnapshot;
        SkillId = skillId;
        Learned = learned;
        Proficiency = proficiency;
        Power = power;
        StudyDetails = details;
        MissingStudyDetails = details
            .Where(detail =>
                detail.ReadState.IsAvailable
                && detail.ReadState.Value == CombatSkillStudyState.NotRead)
            .ToImmutableArray();
        UnavailableStudyDetails = details
            .Where(detail => !detail.ReadState.IsAvailable)
            .ToImmutableArray();
        StudySummary = Summarize(details);
        Breakthrough = breakthrough;
        ActiveDirection = activeDirection;
        AttainmentMastered = attainmentMastered;
        Simplified = simplified;
        Activated = activated;
        Equipped = equipped;
    }

    public int CharacterId { get; }

    public SaveSnapshotIdentity SaveSnapshot { get; }

    public int SkillId { get; }

    public SkillProgressField<bool> Learned { get; }

    public CombatSkillProficiencyProgress Proficiency { get; }

    public CombatSkillPowerProgress Power { get; }

    public ImmutableArray<CombatSkillStudyDetailProgress> StudyDetails { get; }

    public ImmutableArray<CombatSkillStudyDetailProgress> MissingStudyDetails
    { get; }

    public ImmutableArray<CombatSkillStudyDetailProgress>
        UnavailableStudyDetails
    { get; }

    public CombatSkillStudySummary StudySummary { get; }

    public SkillProgressField<BreakthroughDirectionAvailability> Breakthrough
    { get; }

    public SkillProgressField<PracticeDirection> ActiveDirection { get; }

    public SkillProgressField<bool> AttainmentMastered { get; }

    public SkillProgressField<bool> Simplified { get; }

    public SkillProgressField<bool> Activated { get; }

    public SkillProgressField<bool> Equipped { get; }

    public bool Equals(CharacterCombatSkillProgress? other) =>
        other is not null
        && CharacterId == other.CharacterId
        && SaveSnapshot == other.SaveSnapshot
        && SkillId == other.SkillId;

    public override bool Equals(object? obj) =>
        obj is CharacterCombatSkillProgress other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(CharacterId, SaveSnapshot, SkillId);

    public static bool operator ==(
        CharacterCombatSkillProgress? left,
        CharacterCombatSkillProgress? right) => object.Equals(left, right);

    public static bool operator !=(
        CharacterCombatSkillProgress? left,
        CharacterCombatSkillProgress? right) => !object.Equals(left, right);

    private static void ValidateActiveDirection(
        SkillProgressField<PracticeDirection> activeDirection,
        SkillProgressField<BreakthroughDirectionAvailability> breakthrough)
    {
        ArgumentNullException.ThrowIfNull(activeDirection);
        if (activeDirection.IsAvailable
            && activeDirection.Value is not PracticeDirection.Direct
                and not PracticeDirection.Reverse)
        {
            throw new ArgumentOutOfRangeException(
                nameof(activeDirection),
                activeDirection.Value,
                "An active direction must be Direct or Reverse.");
        }

        if (activeDirection.IsAvailable
            && breakthrough.IsAvailable
            && !breakthrough.Value.IsBrokenOut)
        {
            throw new ArgumentException(
                "A skill without completed breakthrough cannot have an "
                + "active practice direction.",
                nameof(activeDirection));
        }
    }

    private static void ValidateAttainmentMastery(
        SkillProgressField<bool> attainmentMastered,
        SkillProgressField<BreakthroughDirectionAvailability> breakthrough)
    {
        if (attainmentMastered.IsAvailable
            && breakthrough.IsAvailable
            && attainmentMastered.Value != breakthrough.Value.IsBrokenOut)
        {
            throw new ArgumentException(
                "Available attainment mastery must agree with the current "
                + "successful breakthrough state.",
                nameof(attainmentMastered));
        }
    }

    private static void RejectDuplicateDetails(
        ImmutableArray<CombatSkillStudyDetailProgress> details,
        IEnumerable<CombatSkillStudyDetailProgress>? source)
    {
        var duplicateId = details
            .GroupBy(detail => detail.DetailId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateId is not null)
        {
            throw new ArgumentException(
                $"Duplicate study detail ID {duplicateId.Key}.",
                nameof(source));
        }

        var duplicateOrder = details
            .GroupBy(detail => detail.DisplayOrder)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateOrder is not null)
        {
            throw new ArgumentException(
                $"Duplicate study detail order {duplicateOrder.Key}.",
                nameof(source));
        }
    }

    private static void ValidateActivation(
        ImmutableArray<CombatSkillStudyDetailProgress> details,
        SkillProgressField<bool> activated)
    {
        if (!activated.IsAvailable
            || details.Length == 0
            || details.Any(detail => !detail.IsActive.IsAvailable))
        {
            return;
        }

        var anyDetailActive = details.Any(detail => detail.IsActive.Value);
        if (activated.Value != anyDetailActive)
        {
            throw new ArgumentException(
                "The aggregate activation flag conflicts with the available "
                + "study-detail activation states.",
                nameof(activated));
        }
    }

    private static CombatSkillStudySummary Summarize(
        ImmutableArray<CombatSkillStudyDetailProgress> details)
    {
        var read = details.Count(detail =>
            detail.ReadState.IsAvailable
            && detail.ReadState.Value == CombatSkillStudyState.Read);
        var notRead = details.Count(detail =>
            detail.ReadState.IsAvailable
            && detail.ReadState.Value == CombatSkillStudyState.NotRead);
        var unavailable = details.Length - read - notRead;

        SkillProgressField<bool> isComplete;
        if (details.Length == 0)
        {
            isComplete = SkillProgressField<bool>.Unavailable(
                "No study details are available for this skill.",
                DerivedStudySource);
        }
        else if (notRead > 0)
        {
            isComplete = SkillProgressField<bool>.Available(
                false,
                DerivedStudySource);
        }
        else if (unavailable > 0)
        {
            isComplete = SkillProgressField<bool>.Unavailable(
                "Study completeness is unavailable because one or more "
                + "detail read states are unavailable or conflicting.",
                DerivedStudySource);
        }
        else
        {
            isComplete = SkillProgressField<bool>.Available(
                true,
                DerivedStudySource);
        }

        return new CombatSkillStudySummary(
            details.Length,
            read + notRead,
            read,
            notRead,
            unavailable,
            isComplete);
    }
}
