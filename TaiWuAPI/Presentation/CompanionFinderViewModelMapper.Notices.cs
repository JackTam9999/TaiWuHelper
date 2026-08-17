using TaiWu.Application.CombatSkills;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Application.Localization;
using TaiWuAPI.Localization;

namespace TaiWuAPI.Presentation;

public static partial class CompanionFinderViewModelMapper
{
    public static CompanionFinderEnrichmentViewModel MapEnrichment(
        CompanionCandidateEnrichmentStatus status,
        CombatSkillCatalogueStatus catalogueStatus,
        TaiwuLanguage language)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown companion enrichment status.");
        }

        if (!Enum.IsDefined(catalogueStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(catalogueStatus),
                catalogueStatus,
                "Unknown combat-skill catalogue status.");
        }

        var (title, message) = (status, catalogueStatus) switch
        {
            (CompanionCandidateEnrichmentStatus.Complete,
                CombatSkillCatalogueStatus.Current) => (
                CompanionFinderUiTextKey.EnrichmentCurrentTitle,
                CompanionFinderUiTextKey.EnrichmentCurrentMessage),
            (CompanionCandidateEnrichmentStatus.Partial,
                CombatSkillCatalogueStatus.Current) => (
                CompanionFinderUiTextKey.CandidateEvidencePartialTitle,
                CompanionFinderUiTextKey.CandidateEvidencePartialMessage),
            (CompanionCandidateEnrichmentStatus.CatalogueMissing,
                CombatSkillCatalogueStatus.MissingSources) => (
                    CompanionFinderUiTextKey.CatalogueSourcesMissingTitle,
                    CompanionFinderUiTextKey.CatalogueSourcesMissingMessage),
            (CompanionCandidateEnrichmentStatus.CatalogueMissing,
                CombatSkillCatalogueStatus.Missing) => (
                CompanionFinderUiTextKey.CatalogueMissingTitle,
                CompanionFinderUiTextKey.CatalogueMissingMessage),
            (CompanionCandidateEnrichmentStatus.CatalogueStale,
                CombatSkillCatalogueStatus.Stale) => (
                CompanionFinderUiTextKey.CatalogueStaleTitle,
                CompanionFinderUiTextKey.CatalogueStaleMessage),
            (CompanionCandidateEnrichmentStatus.CatalogueRebuilding,
                CombatSkillCatalogueStatus.Rebuilding) => (
                CompanionFinderUiTextKey.CatalogueRebuildingTitle,
                CompanionFinderUiTextKey.CatalogueRebuildingMessage),
            (CompanionCandidateEnrichmentStatus.CatalogueUnsupported,
                CombatSkillCatalogueStatus.UnsupportedVersion) => (
                CompanionFinderUiTextKey.CatalogueUnsupportedTitle,
                CompanionFinderUiTextKey.CatalogueUnsupportedMessage),
            (CompanionCandidateEnrichmentStatus.CatalogueFailed,
                CombatSkillCatalogueStatus.SourceReadFailed) => (
                    CompanionFinderUiTextKey.CatalogueSourceReadFailedTitle,
                    CompanionFinderUiTextKey.CatalogueSourceReadFailedMessage),
            (CompanionCandidateEnrichmentStatus.CatalogueFailed,
                CombatSkillCatalogueStatus.RepositoryFailed) => (
                    CompanionFinderUiTextKey.CatalogueRepositoryFailedTitle,
                    CompanionFinderUiTextKey.CatalogueRepositoryFailedMessage),
            (CompanionCandidateEnrichmentStatus.CatalogueFailed,
                CombatSkillCatalogueStatus.Corrupt) => (
                    CompanionFinderUiTextKey.CatalogueCorruptTitle,
                    CompanionFinderUiTextKey.CatalogueCorruptMessage),
            _ => throw new ArgumentException(
                "The enrichment and catalogue states are not a supported presentation combination.",
                nameof(status))
        };
        return new CompanionFinderEnrichmentViewModel(
            status,
            catalogueStatus,
            Text(language, title),
            Text(language, message),
            status != CompanionCandidateEnrichmentStatus.Complete);
    }

    public static CompanionFinderNoticeViewModel MapFailure(
        CompanionFinderStatus status,
        TaiwuLanguage language)
    {
        var (title, message, retry) = status switch
        {
            CompanionFinderStatus.SaveUnavailable => (
                CompanionFinderUiTextKey.SaveUnavailableTitle,
                CompanionFinderUiTextKey.SaveUnavailableMessage,
                true),
            CompanionFinderStatus.UnsupportedSourceVersion
                or CompanionFinderStatus.UnsupportedRoleVersion => (
                CompanionFinderUiTextKey.UnsupportedSourceTitle,
                CompanionFinderUiTextKey.UnsupportedSourceMessage,
                false),
            CompanionFinderStatus.ChangedRevision => (
                CompanionFinderUiTextKey.ChangedRevisionTitle,
                CompanionFinderUiTextKey.ChangedRevisionMessage,
                true),
            _ => (
                CompanionFinderUiTextKey.ReadFailedTitle,
                CompanionFinderUiTextKey.ReadFailedMessage,
                true)
        };
        return new CompanionFinderNoticeViewModel(
            CompanionFinderNoticeStatus.Failure,
            Text(language, title),
            Text(language, message),
            retry);
    }
}
