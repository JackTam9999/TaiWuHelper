using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWuAPI.Contracts.CombatSkills;

public static class CombatSkillResponseMapper
{
    public static CombatSkillCatalogueStatusResponse Map(
        CombatSkillCatalogueStatusResult result) => new(
        result.Status,
        result.DefinitionCount,
        Map(result.InstalledSource),
        Map(result.StoredSource),
        result.BuiltAtUtc,
        PublicCatalogueReason(result.Status));

    public static CombatSkillCatalogueMaintenanceResponse Map(
        EnsureCombatSkillCatalogueResult result) => new(
        result.Status,
        result.DefinitionCount,
        Map(result.SourceIdentity),
        PublicMaintenanceReason(result.Status),
        result.RecoveryStatus,
        Map(result.RetainedSourceIdentity),
        result.RetainedDefinitionCount,
        result.RetainedBuiltAtUtc);

    public static CombatSkillSearchResponse Map(
        CombatSkillSearchResult result) => new(
        Map(result.Catalogue),
        result.TotalMatches,
        result.Offset,
        result.Limit,
        result.CandidateSetMayBeTruncated,
        Issues(result.Issues),
        [.. result.Items.Select(item => new CombatSkillSearchItemResponse(
            item.StableKey,
            Map(item.DisplayName),
            MapSummary(item.Definition),
            Issues(item.Issues)))]);

    public static CombatSkillDetailsResponse Map(
        CombatSkillDetailsResult result) => new(
        Map(result.Catalogue),
        $"combat-skill:{result.SkillId}",
        result.SkillId,
        result.Found,
        result.Definition is null ? null : MapDefinition(result.Definition),
        result.DisplayName is null ? null : Map(result.DisplayName),
        result.ProgressStatus,
        PublicProgressReason(result.ProgressStatus),
        Map(result.ProgressMetadata),
        result.CharacterState is null ? null : Map(result.CharacterState),
        Issues(result.Issues),
        [.. result.Diagnostics.Select(Map)]);

    public static CharacterCombatSkillAtlasResponse Map(
        CharacterCombatSkillAtlasResult result) => new(
        Map(result.Catalogue),
        result.ProgressStatus,
        PublicProgressReason(result.ProgressStatus),
        Map(result.ProgressMetadata),
        result.TotalMatches,
        result.Offset,
        result.Limit,
        result.CandidateSetMayBeTruncated,
        Issues(result.Issues),
        [.. result.Diagnostics.Select(Map)],
        [.. result.Entries.Select(Map)]);

    private static CharacterCombatSkillAtlasEntryResponse Map(
        CharacterCombatSkillAtlasEntry entry) => new(
        entry.StableKey,
        entry.SkillId,
        Map(entry.DisplayName),
        Map(entry.Learned, value => value),
        Map(entry.BaseGridCost, value => value.Value),
        Map(entry.CurrentEffectiveGridCost, value => value),
        entry.Definition is null ? null : MapSummary(entry.Definition),
        entry.Progress is null ? null : Map(entry.Progress),
        Issues(entry.Issues),
        [.. entry.Diagnostics.Select(Map)]);

    private static CharacterCombatSkillProgressResponse Map(
        CharacterCombatSkillProgress progress) => new(
        progress.CharacterId,
        progress.SkillId,
        Map(progress.Learned, value => value),
        new CombatSkillProficiencyResponse(
            Map(progress.Proficiency.Current, value => value),
            Map(progress.Proficiency.Maximum, value => value),
            Map(progress.Proficiency.Percentage, value => value)),
        new CombatSkillStudySummaryResponse(
            progress.StudySummary.TotalCount,
            progress.StudySummary.AvailableCount,
            progress.StudySummary.ReadCount,
            progress.StudySummary.NotReadCount,
            progress.StudySummary.UnavailableCount,
            Map(progress.StudySummary.IsComplete, value => value)),
        [.. progress.StudyDetails.Select(detail =>
            new CombatSkillStudyDetailResponse(
                $"combat-skill:{progress.SkillId}:study:{detail.DetailId}",
                detail.DetailId,
                detail.DisplayOrder,
                detail.Group,
                Map(detail.Label, value => value),
                Map(detail.ReadState, value => value.ToString()),
                Map(detail.IsActive, value => value)))],
        Map(
            progress.Breakthrough,
            value => new BreakthroughAvailabilityResponse(
                value.IsBrokenOut,
                value.CanBreakthroughNow,
                value.AvailableDirections)),
        Map(progress.ActiveDirection, value => value.ToString()),
        Map(progress.AttainmentMastered, value => value),
        Map(progress.Simplified, value => value),
        Map(progress.Activated, value => value),
        Map(progress.Equipped, value => value));

    private static CombatSkillDefinitionResponse MapDefinition(
        CombatSkillDefinition definition) => new(
        MapSummary(definition),
        Map(
            definition.SlotContribution,
            value => new SkillSlotContributionResponse(
                value.Attack,
                value.Agility,
                value.Defense,
                value.Assistance,
                value.Generic)),
        [.. definition.Requirements.Select(requirement =>
            new CombatSkillRequirementResponse(
                requirement.RequirementId.Value,
                Map(requirement.RequiredValue, value => value),
                Map(requirement.Source)))],
        new CombatSkillTimingResponse(
            Map(definition.Timing.PreparationProgress, value => value),
            Map(definition.Timing.BreathStanceCost, value => value),
            Map(definition.Timing.CastSpeed, value => value)),
        new CombatSkillEffectsResponse(
            Map(definition.Effects.Direct, value => value.Value),
            Map(definition.Effects.Reverse, value => value.Value),
            Map(definition.Effects.Neutral, value => value.Value)),
        [.. definition.RawDescriptions.Select(description =>
            new RawCombatSkillDescriptionResponse(
                description.Kind,
                description.Language,
                description.Text,
                description.IsVerifiedMechanic,
                Map(description.Source)))],
        Map(definition.SourceRecord));

    private static CombatSkillDefinitionSummaryResponse MapSummary(
        CombatSkillDefinition definition) => new(
        $"combat-skill:{definition.SkillId}",
        definition.SkillId,
        [.. definition.Names.Values.Select(Map)],
        Map(definition.Category, value => value.ToString()),
        Map(definition.Grade, value => value.Value),
        Map(definition.Faction, value => value.Value),
        Map(definition.Element, value => value.ToString()),
        Map(definition.EquipmentType, value => value.ToString()),
        Map(definition.BaseGridCost, value => value.Value));

    private static CombatSkillDisplayNameResponse Map(
        CombatSkillDisplayName displayName) => new(
        displayName.PreferredLanguage,
        Map(displayName.Value, Map),
        displayName.UsedFallback);

    private static LocalizedCombatSkillNameResponse Map(
        LocalizedCombatSkillName value) => new(
        value.Language,
        value.Text,
        Map(value.Source));

    private static CharacterProgressMetadataResponse? Map(
        CharacterCombatSkillProgressMetadata? metadata) => metadata is null
        ? null
        : new CharacterProgressMetadataResponse(
            metadata.SaveSnapshot.Sha256,
            metadata.SaveSnapshot.ReadAtUtc,
            metadata.GameDataVersion,
            [.. metadata.Warnings.Select(warning =>
                new CharacterProgressWarningResponse(
                    warning.Code,
                    SafeDiagnostic(
                        warning.Reason,
                        "Character progress is partially unavailable.")))]);

    private static CombatSkillQueryDiagnosticResponse Map(
        CombatSkillQueryDiagnostic diagnostic) => new(
        diagnostic.Code,
        SafeDiagnostic(
            diagnostic.Reason,
            "Combat-skill data is partially unavailable."),
        diagnostic.SkillId);

    private static CatalogueSourceIdentityResponse? Map(
        CombatSkillCatalogueSourceIdentity? source) => source is null
        ? null
        : new CatalogueSourceIdentityResponse(
            source.GameDataVersion,
            source.ImporterVersion,
            source.GameDataFingerprint,
            source.TraditionalChineseFingerprint,
            source.EnglishFingerprint);

    private static CatalogueSourceReferenceResponse Map(
        CatalogueSourceReference source) => new(
        source.Kind,
        source.SourceIdentity,
        source.RecordIdentity);

    private static SkillProgressSourceResponse Map(
        SkillProgressSource source) => new(
        source.Kind,
        source.SourceIdentity,
        source.FieldIdentity);

    private static CatalogueFieldResponse Map<T>(
        CatalogueField<T> field,
        Func<T, object?> project) => new(
        field.Status,
        field.IsAvailable ? project(field.Value) : null,
        field.IsAvailable
            ? null
            : SafeDiagnostic(field.Reason, "Field data is unavailable."),
        field.Source is null ? null : Map(field.Source));

    private static ProgressFieldResponse Map<T>(
        SkillProgressField<T> field,
        Func<T, object?> project) => new(
        field.Status,
        field.IsAvailable ? project(field.Value) : null,
        field.IsAvailable
            ? null
            : SafeDiagnostic(field.Reason, "Progress data is unavailable."),
        field.Source is null ? null : Map(field.Source),
        [.. field.Observations.Select(observation =>
            new ValueObservationResponse(
                project(observation.Value),
                Map(observation.Source)))]);

    private static IReadOnlyList<CombatSkillQueryIssue> Issues(
        CombatSkillQueryIssue issues) =>
        [.. Enum.GetValues<CombatSkillQueryIssue>()
            .Where(value => value != CombatSkillQueryIssue.None
                && issues.HasFlag(value))];

    private static string? PublicCatalogueReason(
        CombatSkillCatalogueStatus status) => status switch
        {
            CombatSkillCatalogueStatus.Current => null,
            CombatSkillCatalogueStatus.Missing =>
                "The helper catalogue has not been built.",
            CombatSkillCatalogueStatus.Stale =>
                "The helper catalogue does not match installed sources.",
            CombatSkillCatalogueStatus.MissingSources =>
                "Installed combat-skill sources are unavailable.",
            CombatSkillCatalogueStatus.UnsupportedVersion =>
                "The installed combat-skill source version is unsupported.",
            CombatSkillCatalogueStatus.SourceReadFailed =>
                "Installed combat-skill sources could not be read.",
            CombatSkillCatalogueStatus.RepositoryFailed =>
                "The helper catalogue could not be read.",
            CombatSkillCatalogueStatus.Corrupt =>
                "The helper catalogue is corrupt.",
            CombatSkillCatalogueStatus.Rebuilding =>
                "The helper catalogue is rebuilding.",
            _ => "The helper catalogue is unavailable."
        };

    private static string? PublicProgressReason(
        CharacterProgressReadStatus status) => status switch
        {
            CharacterProgressReadStatus.NotRead => null,
            CharacterProgressReadStatus.Available => null,
            CharacterProgressReadStatus.SaveMissing =>
                "The configured save is unavailable.",
            CharacterProgressReadStatus.SaveReadFailed =>
                "The configured save could not be read.",
            CharacterProgressReadStatus.UnsupportedVersion =>
                "The configured save version is unsupported.",
            _ => "Character progress is unavailable."
        };

    private static string? PublicMaintenanceReason(
        EnsureCombatSkillCatalogueStatus status) => status switch
        {
            EnsureCombatSkillCatalogueStatus.Current => null,
            EnsureCombatSkillCatalogueStatus.Rebuilt => null,
            EnsureCombatSkillCatalogueStatus.MissingSources =>
                "Installed combat-skill sources are unavailable.",
            EnsureCombatSkillCatalogueStatus.UnsupportedVersion =>
                "The installed combat-skill source version is unsupported.",
            EnsureCombatSkillCatalogueStatus.SourceReadFailed =>
                "Installed combat-skill sources could not be read.",
            EnsureCombatSkillCatalogueStatus.RebuildFailed =>
                "The helper catalogue could not be rebuilt.",
            _ => "The helper catalogue could not be maintained."
        };

    private static string SafeDiagnostic(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var text = value.Trim();
        return text.Contains(":\\", StringComparison.Ordinal)
               || text.Contains(":/", StringComparison.Ordinal)
               || text.Contains("\\\\", StringComparison.Ordinal)
               || text.Contains(".sav", StringComparison.OrdinalIgnoreCase)
               || text.Contains("file://", StringComparison.OrdinalIgnoreCase)
            ? fallback
            : text;
    }
}
