using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record BreakthroughDirectionAvailability
{
    public BreakthroughDirectionAvailability(
        bool isBrokenOut,
        bool canBreakthroughNow,
        IEnumerable<PracticeDirection> availableDirections)
    {
        ArgumentNullException.ThrowIfNull(availableDirections);

        var directions = availableDirections
            .Distinct()
            .ToImmutableArray();
        if (directions.Any(direction =>
                direction is not PracticeDirection.Direct
                    and not PracticeDirection.Reverse))
        {
            throw new ArgumentException(
                "Breakthrough directions must be Direct or Reverse.",
                nameof(availableDirections));
        }

        if (isBrokenOut && canBreakthroughNow)
        {
            throw new ArgumentException(
                "A skill that has completed breakthrough cannot be ready "
                + "to break through again.",
                nameof(canBreakthroughNow));
        }

        if (!canBreakthroughNow && !directions.IsEmpty)
        {
            throw new ArgumentException(
                "Unavailable breakthrough directions must be empty.",
                nameof(availableDirections));
        }

        if (canBreakthroughNow && directions.IsEmpty)
        {
            throw new ArgumentException(
                "An immediately available breakthrough requires at least "
                + "one achievable direction.",
                nameof(availableDirections));
        }

        IsBrokenOut = isBrokenOut;
        CanBreakthroughNow = canBreakthroughNow;
        AvailableDirections = directions;
    }

    public bool IsBrokenOut { get; }

    public bool CanBreakthroughNow { get; }

    public ImmutableArray<PracticeDirection> AvailableDirections { get; }

    public bool Includes(PracticeDirection direction)
    {
        return CanBreakthroughNow
            && AvailableDirections.Contains(direction);
    }
}
