using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWuAPI.Contracts.CombatSkills;

public sealed record CatalogueSourceIdentityResponse(
    string GameDataVersion,
    int ImporterVersion,
    string GameDataFingerprint,
    string TraditionalChineseFingerprint,
    string EnglishFingerprint,
    string TraditionalChineseSpecialEffectFingerprint,
    string EnglishSpecialEffectFingerprint);

public sealed record CombatSkillCatalogueStatusResponse(
    CombatSkillCatalogueStatus Status,
    int DefinitionCount,
    CatalogueSourceIdentityResponse? InstalledSource,
    CatalogueSourceIdentityResponse? StoredSource,
    DateTimeOffset? BuiltAtUtc,
    string? Reason);

public sealed record CombatSkillCatalogueMaintenanceResponse(
    EnsureCombatSkillCatalogueStatus Status,
    int DefinitionCount,
    CatalogueSourceIdentityResponse? Source,
    string? Reason,
    CatalogueRecoveryStatus RecoveryStatus,
    CatalogueSourceIdentityResponse? RetainedSource,
    int RetainedDefinitionCount,
    DateTimeOffset? RetainedBuiltAtUtc);

public sealed record CharacterProgressCacheMaintenanceResponse(
    ClearCharacterCombatSkillProgressCacheStatus Status,
    int ClearedSnapshotCount,
    string? Reason);

public sealed record CatalogueSourceReferenceResponse(
    CatalogueSourceKind Kind,
    string SourceIdentity,
    string RecordIdentity);

public sealed record SkillProgressSourceResponse(
    SkillProgressSourceKind Kind,
    string SourceIdentity,
    string FieldIdentity);

public sealed record ValueObservationResponse(
    object? Value,
    SkillProgressSourceResponse Source);

public sealed record CatalogueFieldResponse(
    CatalogueFieldStatus Status,
    object? Value,
    string? Reason,
    CatalogueSourceReferenceResponse? Source);

public sealed record ProgressFieldResponse(
    SkillProgressFieldStatus Status,
    object? Value,
    string? Reason,
    SkillProgressSourceResponse? Source,
    IReadOnlyList<ValueObservationResponse> Observations);

public sealed record LocalizedCombatSkillNameResponse(
    CatalogueLanguage Language,
    string Text,
    CatalogueSourceReferenceResponse Source);

public sealed record CombatSkillDisplayNameResponse(
    CatalogueLanguage PreferredLanguage,
    CatalogueFieldResponse Value,
    bool UsedFallback);

public sealed record CombatSkillDefinitionSummaryResponse(
    string Reference,
    int SkillId,
    IReadOnlyList<LocalizedCombatSkillNameResponse> Names,
    CatalogueFieldResponse Category,
    CatalogueFieldResponse Grade,
    CatalogueFieldResponse Faction,
    CatalogueFieldResponse Element,
    CatalogueFieldResponse EquipmentType,
    CatalogueFieldResponse BaseGridCost);

public sealed record SkillSlotContributionResponse(
    int Attack,
    int Agility,
    int Defense,
    int Assistance,
    int Generic);

public sealed record CombatSkillRequirementResponse(
    string RequirementId,
    CatalogueFieldResponse RequiredValue,
    CatalogueSourceReferenceResponse Source);

public sealed record CombatSkillTimingResponse(
    CatalogueFieldResponse PreparationProgress,
    CatalogueFieldResponse BreathStanceCost,
    CatalogueFieldResponse CastSpeed);

public sealed record CombatSkillEffectsResponse(
    CatalogueFieldResponse Direct,
    CatalogueFieldResponse Reverse,
    CatalogueFieldResponse Neutral);

public sealed record RawCombatSkillDescriptionResponse(
    RawCombatSkillDescriptionKind Kind,
    CatalogueLanguage Language,
    string Text,
    bool IsVerifiedMechanic,
    CatalogueSourceReferenceResponse Source);

public sealed record CombatSkillDefinitionResponse(
    CombatSkillDefinitionSummaryResponse Summary,
    CatalogueFieldResponse SlotContribution,
    IReadOnlyList<CombatSkillRequirementResponse> Requirements,
    CombatSkillTimingResponse Timing,
    CombatSkillEffectsResponse Effects,
    IReadOnlyList<RawCombatSkillDescriptionResponse> RawDescriptions,
    CatalogueSourceReferenceResponse SourceRecord);

public sealed record CombatSkillQueryDiagnosticResponse(
    string Code,
    string Reason,
    int? SkillId);

public sealed record CombatSkillSearchItemResponse(
    string Reference,
    CombatSkillDisplayNameResponse DisplayName,
    CombatSkillDefinitionSummaryResponse Definition,
    IReadOnlyList<CombatSkillQueryIssue> Issues);

public sealed record CombatSkillSearchResponse(
    CombatSkillCatalogueStatusResponse Catalogue,
    int TotalMatches,
    int Offset,
    int Limit,
    bool CandidateSetMayBeTruncated,
    IReadOnlyList<CombatSkillQueryIssue> Issues,
    IReadOnlyList<CombatSkillSearchItemResponse> Items);

public sealed record CharacterProgressWarningResponse(
    string Code,
    string Reason);

public sealed record CharacterProgressMetadataResponse(
    string SaveSha256,
    DateTimeOffset SaveReadAtUtc,
    string GameDataVersion,
    IReadOnlyList<CharacterProgressWarningResponse> Warnings);

public sealed record CombatSkillProficiencyResponse(
    ProgressFieldResponse Current,
    ProgressFieldResponse Maximum,
    ProgressFieldResponse Percentage);

public sealed record CombatSkillStudySummaryResponse(
    int TotalCount,
    int AvailableCount,
    int ReadCount,
    int NotReadCount,
    int UnavailableCount,
    ProgressFieldResponse IsComplete);

public sealed record CombatSkillStudyDetailResponse(
    string Reference,
    string DetailId,
    int DisplayOrder,
    CombatSkillStudyDetailGroup Group,
    CatalogueFieldResponse Label,
    ProgressFieldResponse ReadState,
    ProgressFieldResponse IsActive);

public sealed record BreakthroughAvailabilityResponse(
    bool IsBrokenOut,
    bool CanBreakthroughNow,
    IReadOnlyList<PracticeDirection> AvailableDirections,
    IReadOnlyList<PracticeDirection> CompletedDirections);

public sealed record CharacterCombatSkillProgressResponse(
    int CharacterId,
    int SkillId,
    ProgressFieldResponse Learned,
    CombatSkillProficiencyResponse Proficiency,
    CombatSkillStudySummaryResponse StudySummary,
    IReadOnlyList<CombatSkillStudyDetailResponse> StudyDetails,
    ProgressFieldResponse Breakthrough,
    ProgressFieldResponse ActiveDirection,
    ProgressFieldResponse AttainmentMastered,
    ProgressFieldResponse Simplified,
    ProgressFieldResponse Activated,
    ProgressFieldResponse Equipped);

public sealed record CharacterCombatSkillAtlasEntryResponse(
    string Reference,
    int SkillId,
    CombatSkillDisplayNameResponse DisplayName,
    ProgressFieldResponse Learned,
    CatalogueFieldResponse BaseGridCost,
    ProgressFieldResponse CurrentEffectiveGridCost,
    CombatSkillDefinitionSummaryResponse? Definition,
    CharacterCombatSkillProgressResponse? Progress,
    IReadOnlyList<CombatSkillQueryIssue> Issues,
    IReadOnlyList<CombatSkillQueryDiagnosticResponse> Diagnostics);

public sealed record CharacterCombatSkillAtlasResponse(
    CombatSkillCatalogueStatusResponse Catalogue,
    CharacterProgressReadStatus ProgressStatus,
    string? ProgressFailureReason,
    CharacterProgressMetadataResponse? ProgressMetadata,
    int TotalMatches,
    int Offset,
    int Limit,
    bool CandidateSetMayBeTruncated,
    IReadOnlyList<CombatSkillQueryIssue> Issues,
    IReadOnlyList<CombatSkillQueryDiagnosticResponse> Diagnostics,
    IReadOnlyList<CharacterCombatSkillAtlasEntryResponse> Entries);

public sealed record CombatSkillDetailsResponse(
    CombatSkillCatalogueStatusResponse Catalogue,
    string Reference,
    int SkillId,
    bool DefinitionFound,
    CombatSkillDefinitionResponse? Definition,
    CombatSkillDisplayNameResponse? DisplayName,
    CharacterProgressReadStatus ProgressStatus,
    string? ProgressFailureReason,
    CharacterProgressMetadataResponse? ProgressMetadata,
    CharacterCombatSkillAtlasEntryResponse? CharacterState,
    IReadOnlyList<CombatSkillQueryIssue> Issues,
    IReadOnlyList<CombatSkillQueryDiagnosticResponse> Diagnostics);
