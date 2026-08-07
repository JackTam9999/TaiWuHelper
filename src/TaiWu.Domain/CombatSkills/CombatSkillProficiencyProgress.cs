namespace TaiWu.Domain.CombatSkills;

public sealed record CombatSkillProficiencyProgress
{
    public const int MaximumSupportedValue = 999999999;

    public CombatSkillProficiencyProgress(
        SkillProgressField<int> current,
        SkillProgressField<int> maximum)
    {
        Current = ValidateCurrent(current, nameof(current));
        Maximum = ValidateMaximum(maximum, nameof(maximum));

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

}
