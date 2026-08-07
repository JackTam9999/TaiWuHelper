using System.Collections.Immutable;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Infrastructure.Catalogue;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed record CombatSkillStudyDetailDecodeResult(
    bool IsVersionSupported,
    bool IsReadingStateSupported,
    bool IsActivationStateSupported,
    ImmutableArray<CombatSkillStudyDetailProgress> Details,
    string? UnavailableReason);

internal static class CombatSkillStudyDetailDecoder
{
    internal const string SupportedGameDataVersion =
        "1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a";

    internal static CombatSkillStudyDetailDecodeResult Decode(
        string gameDataVersion,
        string saveIdentity,
        int skillId,
        int readingState,
        int activationState,
        CombatSkillStudyDetailLabelSet labels,
        ICollection<CharacterCombatSkillProgressWarning> warnings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDataVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(saveIdentity);
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(warnings);
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "Skill ID cannot be negative.");
        }

        if (!string.Equals(
                gameDataVersion,
                SupportedGameDataVersion,
                StringComparison.Ordinal))
        {
            var reason = $"GameData version {gameDataVersion} has no verified "
                + "study-detail decoder.";
            warnings.Add(new CharacterCombatSkillProgressWarning(
                "STUDY_DETAIL_VERSION_UNSUPPORTED",
                reason));
            return new CombatSkillStudyDetailDecodeResult(
                IsVersionSupported: false,
                IsReadingStateSupported: false,
                IsActivationStateSupported: false,
                Details: [],
                reason);
        }

        var reading = CombatSnapshotMapping.MapStudyDetails(
            readingState,
            activationState: 0,
            skillId);
        var activation = CombatSnapshotMapping.MapStudyDetails(
            readingState: 0,
            activationState,
            skillId);
        var definitions = CombatSnapshotMapping.MapStudyDetails(
                readingState: 0,
                activationState: 0,
                skillId)
            .Value;
        if (!reading.IsAvailable)
        {
            warnings.Add(new CharacterCombatSkillProgressWarning(
                "STUDY_DETAIL_READING_STATE_UNSUPPORTED",
                reading.UnavailableReason
                ?? $"Skill {skillId} has an unsupported reading state."));
        }

        if (!activation.IsAvailable)
        {
            warnings.Add(new CharacterCombatSkillProgressWarning(
                "STUDY_DETAIL_ACTIVATION_STATE_UNSUPPORTED",
                activation.UnavailableReason
                ?? $"Skill {skillId} has an unsupported activation state."));
        }

        List<CombatSkillStudyDetailProgress> details = [];
        for (var index = 0; index < definitions.Count; index++)
        {
            var definition = definitions[index];
            var label = labels.Resolve(definition.LocalizationKey);
            if (!label.IsAvailable)
            {
                var labelReason = label.Reason
                    ?? $"The label {definition.LocalizationKey} is "
                        + "unavailable.";
                if (!warnings.Any(warning =>
                        warning.Code == "STUDY_DETAIL_LABEL_UNAVAILABLE"
                        && warning.Reason == labelReason))
                {
                    warnings.Add(new CharacterCombatSkillProgressWarning(
                        "STUDY_DETAIL_LABEL_UNAVAILABLE",
                        labelReason));
                }
            }

            details.Add(new CombatSkillStudyDetailProgress(
                definition.StableId,
                definition.WheelOrder,
                MapGroup(definition.Group),
                label,
                reading.IsAvailable
                    ? SkillProgressField<CombatSkillStudyState>.Available(
                        reading.Value[index].IsRead
                            ? CombatSkillStudyState.Read
                            : CombatSkillStudyState.NotRead,
                        SaveSource(
                            saveIdentity,
                            skillId,
                            $"study-detail:{definition.StableId}:read"))
                    : SkillProgressField<CombatSkillStudyState>.Unavailable(
                        reading.UnavailableReason
                        ?? $"Skill {skillId} has an unsupported reading "
                            + "state.",
                        SaveSource(
                            saveIdentity,
                            skillId,
                            $"study-detail:{definition.StableId}:read")),
                activation.IsAvailable
                    ? SkillProgressField<bool>.Available(
                        activation.Value[index].IsActive,
                        SaveSource(
                            saveIdentity,
                            skillId,
                            $"study-detail:{definition.StableId}:active"))
                    : SkillProgressField<bool>.Unavailable(
                        activation.UnavailableReason
                        ?? $"Skill {skillId} has an unsupported activation "
                            + "state.",
                        SaveSource(
                            saveIdentity,
                            skillId,
                            $"study-detail:{definition.StableId}:active"))));
        }

        return new CombatSkillStudyDetailDecodeResult(
            IsVersionSupported: true,
            IsReadingStateSupported: reading.IsAvailable,
            IsActivationStateSupported: activation.IsAvailable,
            details.OrderBy(detail => detail.DisplayOrder).ToImmutableArray(),
            reading.UnavailableReason ?? activation.UnavailableReason);
    }

    private static Domain.CombatSkills.CombatSkillStudyDetailGroup MapGroup(
        CombatSkillStudyDetailGroup group) => group switch
        {
            CombatSkillStudyDetailGroup.Outline =>
                Domain.CombatSkills.CombatSkillStudyDetailGroup.Outline,
            CombatSkillStudyDetailGroup.Direct =>
                Domain.CombatSkills.CombatSkillStudyDetailGroup.Direct,
            CombatSkillStudyDetailGroup.Reverse =>
                Domain.CombatSkills.CombatSkillStudyDetailGroup.Reverse,
            _ => throw new ArgumentOutOfRangeException(
                nameof(group),
                group,
                "Unknown study-detail group.")
        };

    private static SkillProgressSource SaveSource(
        string saveIdentity,
        int skillId,
        string field) => new(
            SkillProgressSourceKind.SaveSnapshot,
            saveIdentity,
            $"combat-skill:{skillId}:{field}");
}
