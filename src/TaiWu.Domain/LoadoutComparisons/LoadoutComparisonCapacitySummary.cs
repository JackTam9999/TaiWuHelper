namespace TaiWu.Domain.LoadoutComparisons;

public sealed record LoadoutComparisonCapacitySummary
{
    public LoadoutComparisonCapacitySummary(
        LoadoutComparisonValue<int> used,
        LoadoutComparisonValue<int> capacity,
        LoadoutComparisonValue<int> remaining,
        LoadoutComparisonValue<int> categoryContribution,
        LoadoutComparisonValue<int> genericContribution)
    {
        Used = ValidateNonNegative(used, nameof(used));
        Capacity = ValidateNonNegative(capacity, nameof(capacity));
        Remaining = ValidateNonNegative(remaining, nameof(remaining));
        CategoryContribution = categoryContribution
            ?? throw new ArgumentNullException(nameof(categoryContribution));
        GenericContribution = ValidateNonNegative(
            genericContribution,
            nameof(genericContribution));

        if (Used.IsAvailable
            && Capacity.IsAvailable
            && Used.Value > Capacity.Value)
        {
            throw new ArgumentException(
                "Used comparison slots cannot exceed capacity.",
                nameof(used));
        }

        if (Used.IsAvailable
            && Capacity.IsAvailable
            && Remaining.IsAvailable
            && Remaining.Value != Capacity.Value - Used.Value)
        {
            throw new ArgumentException(
                "Available remaining slots must equal capacity minus used.",
                nameof(remaining));
        }
    }

    public LoadoutComparisonValue<int> Used { get; }

    public LoadoutComparisonValue<int> Capacity { get; }

    public LoadoutComparisonValue<int> Remaining { get; }

    public LoadoutComparisonValue<int> CategoryContribution { get; }

    public LoadoutComparisonValue<int> GenericContribution { get; }

    private static LoadoutComparisonValue<int> ValidateNonNegative(
        LoadoutComparisonValue<int> value,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        if (value.IsAvailable && value.Value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value.Value,
                "An available comparison value cannot be negative.");
        }

        return value;
    }
}
