using GameData.Domains.Character;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed record TaiwuReportContext(
    LegacyReportWriter Writer,
    int TaiwuId,
    Character Taiwu,
    CombatSkillEquipment Equipment,
    HashSet<short> EquippedSkillIds,
    int? TargetCharacterId);
