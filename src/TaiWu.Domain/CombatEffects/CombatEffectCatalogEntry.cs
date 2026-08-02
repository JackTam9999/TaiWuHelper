using System.Collections.Immutable;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatEffects;

public sealed record CombatEffectCatalogEntry
{
    public CombatEffectCatalogEntry(
        int skillId,
        string skillName,
        PracticeDirection direction,
        int rawEffectId,
        string rawSourceText,
        string sourceReference,
        IEnumerable<CombatEffectMechanic> mechanics)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "Skill ID cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(skillName))
        {
            throw new ArgumentException(
                "Skill name cannot be blank.",
                nameof(skillName));
        }

        ValidateDirection(direction);
        if (rawEffectId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rawEffectId),
                rawEffectId,
                "Effect ID cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(rawSourceText))
        {
            throw new ArgumentException(
                "Raw effect source text cannot be blank.",
                nameof(rawSourceText));
        }

        if (string.IsNullOrWhiteSpace(sourceReference))
        {
            throw new ArgumentException(
                "Effect source reference cannot be blank.",
                nameof(sourceReference));
        }

        ArgumentNullException.ThrowIfNull(mechanics);
        var mechanicValues = mechanics.ToImmutableArray();
        if (mechanicValues.Any(mechanic => !Enum.IsDefined(mechanic)))
        {
            throw new ArgumentOutOfRangeException(
                nameof(mechanics),
                "Effect mechanics contain an unknown value.");
        }

        if (mechanicValues.Distinct().Count() != mechanicValues.Length)
        {
            throw new ArgumentException(
                "Effect mechanics cannot be duplicated.",
                nameof(mechanics));
        }

        SkillId = skillId;
        SkillName = skillName.Trim();
        Direction = direction;
        RawEffectId = rawEffectId;
        RawSourceText = rawSourceText.Trim();
        SourceReference = sourceReference.Trim();
        Mechanics = mechanicValues;
    }

    public int SkillId { get; }

    public string SkillName { get; }

    public PracticeDirection Direction { get; }

    public int RawEffectId { get; }

    public string RawSourceText { get; }

    public string SourceReference { get; }

    public ImmutableArray<CombatEffectMechanic> Mechanics { get; }

    public bool HasTypedMechanics => !Mechanics.IsEmpty;

    private static void ValidateDirection(PracticeDirection direction)
    {
        if (direction is not (
            PracticeDirection.Direct or PracticeDirection.Reverse))
        {
            throw new ArgumentOutOfRangeException(
                nameof(direction),
                direction,
                "Catalog effects must be Direct or Reverse.");
        }
    }
}
