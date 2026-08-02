namespace TaiWu.Domain.CombatSkills;

public sealed record CombatSkillProficiencyProgress
{
    public const int MaximumSupportedValue = 999999999;

    public CombatSkillProficiencyProgress(
        SkillProgressField<int> current,
        SkillProgressField<int> maximum,
        SkillProgressField<decimal> percentage)
    {
        Current = ValidateCurrent(current, nameof(current));
        Maximum = ValidateMaximum(maximum, nameof(maximum));
        Percentage = ValidatePercentage(percentage, nameof(percentage));

        if (Current.IsAvailable
            && Maximum.IsAvailable
            && Current.Value > Maximum.Value)
        {
            throw new ArgumentException(
                "Current proficiency cannot exceed maximum proficiency.",
                nameof(current));
        }
    }

    public SkillProgressField<int> Current { get; }

    public SkillProgressField<int> Maximum { get; }

    public SkillProgressField<decimal> Percentage { get; }

    private static SkillProgressField<int> ValidateCurrent(
        SkillProgressField<int> field,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(field, parameterName);
        if (field.IsAvailable
            && field.Value is < 0 or > MaximumSupportedValue)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                field.Value,
                $"Current proficiency must be 0..{MaximumSupportedValue}.");
        }

        return field;
    }

    private static SkillProgressField<int> ValidateMaximum(
        SkillProgressField<int> field,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(field, parameterName);
        if (field.IsAvailable
            && field.Value is <= 0 or > MaximumSupportedValue)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                field.Value,
                $"Maximum proficiency must be 1..{MaximumSupportedValue}.");
        }

        return field;
    }

    private static SkillProgressField<decimal> ValidatePercentage(
        SkillProgressField<decimal> field,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(field, parameterName);
        if (field.IsAvailable && field.Value is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                field.Value,
                "Proficiency percentage must be 0..100.");
        }

        return field;
    }
}
