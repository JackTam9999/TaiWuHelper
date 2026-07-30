using System.Collections.Immutable;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatEffects;

public sealed class CombatEffectCatalog
{
    private readonly ImmutableDictionary<
        (int SkillId, PracticeDirection Direction),
        CombatEffectCatalogEntry> _entriesBySkillAndDirection;

    public CombatEffectCatalog(
        string gameDataVersion,
        IEnumerable<CombatEffectCatalogEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(gameDataVersion))
        {
            throw new ArgumentException(
                "GameData version cannot be blank.",
                nameof(gameDataVersion));
        }

        ArgumentNullException.ThrowIfNull(entries);
        Entries = [.. entries];
        if (Entries.Any(entry => entry is null))
        {
            throw new ArgumentException(
                "Effect catalog cannot contain null entries.",
                nameof(entries));
        }

        var duplicate = Entries
            .GroupBy(entry => (entry.SkillId, entry.Direction))
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Duplicate effect catalog entry for skill "
                + $"{duplicate.Key.SkillId} "
                + $"{duplicate.Key.Direction}.",
                nameof(entries));
        }

        GameDataVersion = gameDataVersion.Trim();
        _entriesBySkillAndDirection = Entries.ToImmutableDictionary(
            entry => (entry.SkillId, entry.Direction));
    }

    public string GameDataVersion { get; }

    public ImmutableArray<CombatEffectCatalogEntry> Entries { get; }

    public CombatEffectResolution Resolve(
        string observedGameDataVersion,
        int skillId,
        PracticeDirection direction,
        int rawEffectId)
    {
        if (string.IsNullOrWhiteSpace(observedGameDataVersion))
        {
            throw new ArgumentException(
                "Observed GameData version cannot be blank.",
                nameof(observedGameDataVersion));
        }

        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "Skill ID cannot be negative.");
        }

        if (direction is not (
            PracticeDirection.Direct or PracticeDirection.Reverse))
        {
            throw new ArgumentOutOfRangeException(
                nameof(direction),
                direction,
                "Resolved effects must be Direct or Reverse.");
        }

        if (rawEffectId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rawEffectId),
                rawEffectId,
                "Effect ID cannot be negative.");
        }

        var observedVersion = observedGameDataVersion.Trim();
        if (!string.Equals(
                GameDataVersion,
                observedVersion,
                StringComparison.Ordinal))
        {
            return Result(
                observedVersion,
                skillId,
                direction,
                rawEffectId,
                CombatEffectResolutionStatus.VersionMismatch,
                catalogEntry: null);
        }

        if (!_entriesBySkillAndDirection.TryGetValue(
                (skillId, direction),
                out var entry))
        {
            return Result(
                observedVersion,
                skillId,
                direction,
                rawEffectId,
                CombatEffectResolutionStatus.Unrecognized,
                catalogEntry: null);
        }

        if (entry.RawEffectId != rawEffectId)
        {
            return Result(
                observedVersion,
                skillId,
                direction,
                rawEffectId,
                CombatEffectResolutionStatus.EffectIdMismatch,
                entry);
        }

        return Result(
            observedVersion,
            skillId,
            direction,
            rawEffectId,
            entry.HasTypedMechanics
                ? CombatEffectResolutionStatus.Recognized
                : CombatEffectResolutionStatus.Unrecognized,
            entry);
    }

    private static CombatEffectResolution Result(
        string observedGameDataVersion,
        int skillId,
        PracticeDirection direction,
        int rawEffectId,
        CombatEffectResolutionStatus status,
        CombatEffectCatalogEntry? catalogEntry)
    {
        return new CombatEffectResolution(
            observedGameDataVersion,
            skillId,
            direction,
            rawEffectId,
            status,
            catalogEntry);
    }
}
