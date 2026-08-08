using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.LoadoutComparisons;

public sealed record LoadoutComparisonSkillAction
{
    public LoadoutComparisonSkillAction(
        LoadoutComparisonSkillActionKind kind,
        PracticeDirection requiredDirection,
        LoadoutComparisonReason reason)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unknown comparison skill action.");
        }

        if (requiredDirection is not PracticeDirection.Direct
            and not PracticeDirection.Reverse)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiredDirection),
                requiredDirection,
                "A comparison action requires Direct or Reverse practice.");
        }

        Kind = kind;
        RequiredDirection = requiredDirection;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }

    public LoadoutComparisonSkillActionKind Kind { get; }

    public PracticeDirection RequiredDirection { get; }

    public LoadoutComparisonReason Reason { get; }
}
