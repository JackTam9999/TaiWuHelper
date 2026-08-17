using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using GameBehaviorType = GameData.Domains.Character.BehaviorType;

namespace TaiWu.Infrastructure.Catalogue;

internal sealed class TaiwuCombatSkillFactionProfileSource
    : ICombatSkillFactionProfileSource
{
    private static readonly object ConfigurationGate = new();

    public Task<IReadOnlyList<CombatSkillFactionProfile>> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<CombatSkillFactionProfile> profiles;
        lock (ConfigurationGate)
        {
            var configuration = Config.Organization.Instance;
            if (configuration.Count == 0)
            {
                configuration.Init();
            }

            profiles = configuration
                .GetAllKeys()
                .Order()
                .Select(key => Map(key, configuration.GetItem(key)))
                .Where(profile => profile is not null)
                .Select(profile => profile!)
                .ToArray();
        }

        return Task.FromResult(profiles);
    }

    private static CombatSkillFactionProfile? Map(
        short factionId,
        Config.OrganizationItem? item)
    {
        if (item is null)
        {
            return null;
        }

        return new CombatSkillFactionProfile(
            new CombatSkillFactionId(factionId),
            MapElement(item.FiveElementsType),
            MapMorality(item.MainMorality));
    }

    internal static CombatSkillFactionAlignment? MapMorality(short morality)
    {
        try
        {
            return MapAlignment(GameBehaviorType.GetBehaviorType(morality));
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    internal static CombatSkillElement? MapElement(int value) => value switch
    {
        0 => CombatSkillElement.Metal,
        1 => CombatSkillElement.Wood,
        2 => CombatSkillElement.Water,
        3 => CombatSkillElement.Fire,
        4 => CombatSkillElement.Earth,
        5 => CombatSkillElement.Mixed,
        _ => null
    };

    internal static CombatSkillFactionAlignment? MapAlignment(int value)
    {
        if (value == GameBehaviorType.Just)
        {
            return CombatSkillFactionAlignment.Just;
        }

        if (value == GameBehaviorType.Kind)
        {
            return CombatSkillFactionAlignment.Kind;
        }

        if (value == GameBehaviorType.Even)
        {
            return CombatSkillFactionAlignment.Even;
        }

        if (value == GameBehaviorType.Rebel)
        {
            return CombatSkillFactionAlignment.Rebel;
        }

        if (value == GameBehaviorType.Egoistic)
        {
            return CombatSkillFactionAlignment.Egoistic;
        }

        return null;
    }
}
