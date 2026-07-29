using GameData.Domains.Character;
using GameData.Domains.Item;
using System.Reflection;
using System.Text;

namespace TaiWu.Infrastructure.SaveGames;

internal static class GameDataText
{
    public static T PrivateField<T>(object value, string name)
    {
        var field = value.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);

        return field is null
            ? throw new MissingFieldException(value.GetType().FullName, name)
            : (T)field.GetValue(value)!;
    }

    public static string JoinNumbers<T>(IEnumerable<T> values)
    {
        var text = new StringBuilder();
        foreach (var value in values)
        {
            if (text.Length > 0)
            {
                text.Append(',');
            }

            text.Append(value);
        }

        return text.ToString();
    }

    public static string SafeText<T>(Func<T> getter)
    {
        try
        {
            var value = getter();
            return value is null ? "(null)" : value.ToString()!;
        }
        catch (Exception exception)
        {
            return $"ERR:{exception.GetType().Name}";
        }
    }

    public static string TrickText(Config.CombatSkillItem item) =>
        JoinNumbers(item.TrickCost.Select(trick => trick.TrickType));

    public static string SkillName(short id)
    {
        try
        {
            return Config.CombatSkill.Instance.GetItem(id)?.Name ?? "(unknown)";
        }
        catch
        {
            return "(unknown)";
        }
    }

    public static string WeaponName(ItemKey key)
    {
        if (!key.HasTemplate)
        {
            return "(empty)";
        }

        try
        {
            return Config.Weapon.Instance.GetItem(key.TemplateId)?.Name
                ?? "(unknown)";
        }
        catch
        {
            return "(not-weapon-or-unknown)";
        }
    }

    public static void WriteSkillList(
        LegacyReportWriter writer,
        string category,
        sbyte equipType,
        CombatSkillEquipment equipment)
    {
        List<short> values = [];
        equipment.GetValidSkills(equipType, values);
        for (var index = 0; index < values.Count; index++)
        {
            var id = values[index];
            writer.Write(
                "EQUIP|{0}|{1}|{2}|{3}",
                category,
                index,
                id,
                SkillName(id));
        }
    }
}
