using GameData.Domains.Character;
using GameData.Domains.CombatSkill;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed record TaiwuReportContext(
    LegacyReportWriter Writer,
    int TaiwuId,
    Character Taiwu,
    CombatSkillEquipment Equipment,
    HashSet<short> EquippedSkillIds,
    int? TargetCharacterId);
