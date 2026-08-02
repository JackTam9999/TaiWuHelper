using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed record RawCharacterCombatSkillProgress(
    int SkillId,
    bool Learned,
    int? Proficiency,
    int ReadingState,
    int ActivationState,
    bool MeetsBreakthroughReadingRequirement,
    bool Simplified,
    bool Equipped);

internal static class CombatSkillProgressMapping
{
    internal static CharacterCombatSkillProgress Map(
        int characterId,
        SaveSnapshotIdentity snapshot,
        RawCharacterCombatSkillProgress raw,
        ICollection<CharacterCombatSkillProgressWarning> warnings)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(raw);
        ArgumentNullException.ThrowIfNull(warnings);

        var saveIdentity = $"save:{snapshot.Sha256}";
        var learned = SkillProgressField<bool>.Available(
            raw.Learned,
            SaveSource(saveIdentity, raw.SkillId, "learned-membership"));
        var proficiency = MapProficiency(
            saveIdentity,
            raw,
            warnings);
        var breakthrough = MapSnapshotValue(
            CombatSnapshotMapping.MapBreakthroughDirectionAvailability(
                raw.ReadingState,
                raw.ActivationState,
                raw.MeetsBreakthroughReadingRequirement,
                raw.SkillId),
            SaveSource(saveIdentity, raw.SkillId, "breakthrough"),
            warnings,
            "BREAKTHROUGH_STATE_UNAVAILABLE");
        var direction = MapSnapshotValue(
            CombatSnapshotMapping.MapActivePracticeDirection(
                raw.ActivationState,
                raw.SkillId),
            SaveSource(saveIdentity, raw.SkillId, "active-direction"),
            warnings,
            warningCode: null);
        var activated = MapActivation(
            saveIdentity,
            raw,
            warnings);

        return new CharacterCombatSkillProgress(
            characterId,
            snapshot,
            raw.SkillId,
            learned,
            proficiency,
            studyDetails: null,
            breakthrough,
            direction,
            SkillProgressField<bool>.Unavailable(
                "The save-derived attainment mastery rule is not verified "
                + "for this version.",
                VerifiedSource(raw.SkillId, "attainment-mastered")),
            SkillProgressField<bool>.Available(
                raw.Simplified,
                SaveSource(saveIdentity, raw.SkillId, "simplified")),
            activated,
            SkillProgressField<bool>.Available(
                raw.Equipped,
                SaveSource(saveIdentity, raw.SkillId, "equipped")));
    }

    private static CombatSkillProficiencyProgress MapProficiency(
        string saveIdentity,
        RawCharacterCombatSkillProgress raw,
        ICollection<CharacterCombatSkillProgressWarning> warnings)
    {
        SkillProgressField<int> current;
        if (raw.Proficiency is null)
        {
            current = SkillProgressField<int>.Unavailable(
                "The save contains no proficiency key for this skill.",
                SaveSource(saveIdentity, raw.SkillId, "proficiency"));
        }
        else if (raw.Proficiency is < 0
                 or > CombatSkillProficiencyProgress.MaximumSupportedValue)
        {
            var reason = $"Skill {raw.SkillId} has an out-of-range persisted "
                + $"proficiency value {raw.Proficiency}.";
            warnings.Add(new CharacterCombatSkillProgressWarning(
                "PROFICIENCY_OUT_OF_RANGE",
                reason));
            current = SkillProgressField<int>.Unavailable(
                reason,
                SaveSource(saveIdentity, raw.SkillId, "proficiency"));
        }
        else
        {
            current = SkillProgressField<int>.Available(
                raw.Proficiency.Value,
                SaveSource(saveIdentity, raw.SkillId, "proficiency"));
        }

        return new CombatSkillProficiencyProgress(
            current,
            SkillProgressField<int>.Available(
                CombatSkillProficiencyProgress.MaximumSupportedValue,
                VerifiedSource(raw.SkillId, "maximum-proficiency")),
            SkillProgressField<decimal>.Unavailable(
                "The conversion from persisted proficiency to the displayed "
                + "percentage is not verified.",
                VerifiedSource(raw.SkillId, "proficiency-percentage")));
    }

    private static SkillProgressField<bool> MapActivation(
        string saveIdentity,
        RawCharacterCombatSkillProgress raw,
        ICollection<CharacterCombatSkillProgressWarning> warnings)
    {
        var details = CombatSnapshotMapping.MapStudyDetails(
            raw.ReadingState,
            raw.ActivationState,
            raw.SkillId);
        var source = SaveSource(
            saveIdentity,
            raw.SkillId,
            "activation-state");
        if (details.IsAvailable)
        {
            return SkillProgressField<bool>.Available(
                details.Value.Any(detail => detail.IsActive),
                source);
        }

        var reason = details.UnavailableReason
            ?? $"Skill {raw.SkillId} has an unsupported activation state.";
        warnings.Add(new CharacterCombatSkillProgressWarning(
            "ACTIVATION_STATE_UNSUPPORTED",
            reason));
        return SkillProgressField<bool>.Unavailable(reason, source);
    }

    private static SkillProgressField<T> MapSnapshotValue<T>(
        SnapshotValue<T> value,
        SkillProgressSource source,
        ICollection<CharacterCombatSkillProgressWarning> warnings,
        string? warningCode)
    {
        if (value.IsAvailable)
        {
            return SkillProgressField<T>.Available(value.Value, source);
        }

        var reason = value.UnavailableReason
            ?? "The save value could not be mapped.";
        if (warningCode is not null)
        {
            warnings.Add(new CharacterCombatSkillProgressWarning(
                warningCode,
                reason));
        }
        return SkillProgressField<T>.Unavailable(reason, source);
    }

    private static SkillProgressSource SaveSource(
        string saveIdentity,
        int skillId,
        string field) => new(
            SkillProgressSourceKind.SaveSnapshot,
            saveIdentity,
            $"combat-skill:{skillId}:{field}");

    private static SkillProgressSource VerifiedSource(
        int skillId,
        string field) => new(
            SkillProgressSourceKind.VerifiedRule,
            "verified-rule:e2-002",
            $"combat-skill:{skillId}:{field}");
}
