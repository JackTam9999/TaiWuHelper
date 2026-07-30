using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatThreats;

public static class VerifiedTargetThreatRuleSets
{
    public static TargetThreatRuleSet GoldenMagicSound { get; } =
        CreateGoldenMagicSound();

    private static TargetThreatRuleSet CreateGoldenMagicSound()
    {
        TargetThreatSkillSignature[] directMagicSoundSignatures =
        [
            Direct(718, 668),
            Direct(719, 669),
            Direct(720, 670),
            Direct(721, 671),
            Direct(722, 672),
            Direct(723, 673),
            Direct(724, 674),
            Direct(725, 349),
            Direct(726, 350),
            Direct(727, 351),
            Direct(728, 352),
            Direct(729, 353),
            Direct(730, 354),
            Direct(731, 355),
            Direct(732, 356),
            Direct(733, 357)
        ];
        var taxonomy = VerifiedTargetThreatTaxonomies.GoldenMagicSound;

        return new TargetThreatRuleSet(
            VerifiedCombatEffectCatalogs.GoldenGameDataVersion,
            directMagicSoundSignatures.Select(
                signature => signature.SkillId),
            taxonomy.Threats.Select(
                threat => new TargetThreatRule(
                    threat,
                    directMagicSoundSignatures)),
            taxonomy.Warnings.Select(warning => warning.Mechanic));
    }

    private static TargetThreatSkillSignature Direct(
        int skillId,
        int effectId)
    {
        return new TargetThreatSkillSignature(
            skillId,
            PracticeDirection.Direct,
            effectId);
    }
}
