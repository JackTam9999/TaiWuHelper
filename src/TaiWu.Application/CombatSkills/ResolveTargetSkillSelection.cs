using System.Collections.Immutable;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Application.CombatSkills;

public sealed class ResolveTargetSkillSelection(
    ICombatSkillDefinitionSource definitionSource,
    ICombatSkillCatalogueRepository repository,
    CombatSkillCatalogueMaintenanceCoordinator? coordinator = null)
    : IResolveTargetSkillSelection
{
    public async Task<TargetSkillSelectionResult> ExecuteAsync(
        TargetSkillSelectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var search = await new SearchCombatSkillDefinitions(
                definitionSource,
                repository,
                coordinator)
            .ExecuteAsync(
                new CombatSkillSearchRequest(
                    request.PreferredLanguage,
                    request.Query,
                    new CombatSkillCatalogueFilter(
                        candidateLimit:
                            CombatSkillCatalogueFilter.MaximumCandidateCount),
                    offset: 0,
                    limit: CombatSkillSearchRequest.MaximumPageSize,
                    CombatSkillSearchSort.DisplayName),
                cancellationToken)
            .ConfigureAwait(false);

        if (search.Catalogue.Status != CombatSkillCatalogueStatus.Current)
        {
            return new TargetSkillSelectionResult(
                TargetSkillSelectionResult.MapCatalogueStatus(
                    search.Catalogue.Status),
                search.Catalogue,
                CandidateSetMayBeTruncated: false,
                Candidates: [],
                ResolvedSelection: null);
        }

        var candidates = search.Items
            .Select(item => CreateCandidate(item, request))
            .ToImmutableArray();
        if (candidates.Length == 0)
        {
            return Result(
                TargetSkillSelectionStatus.DefinitionMissing,
                search,
                candidates);
        }

        if (request.ConfirmedSkillId is null)
        {
            return Result(
                candidates.Length == 1
                    ? TargetSkillSelectionStatus.ConfirmationRequired
                    : TargetSkillSelectionStatus.Ambiguous,
                search,
                candidates);
        }

        var selected = candidates.SingleOrDefault(candidate =>
            candidate.SkillId == request.ConfirmedSkillId.Value);
        if (selected is null)
        {
            return Result(
                TargetSkillSelectionStatus.ConfirmationInvalid,
                search,
                candidates);
        }

        if (selected.StaticFacts is null)
        {
            return Result(
                TargetSkillSelectionStatus.DefinitionUnsupported,
                search,
                candidates);
        }

        if (selected.StaticFacts.Category != request.ReportedCategory)
        {
            return Result(
                TargetSkillSelectionStatus.CategoryMismatch,
                search,
                candidates);
        }

        var observation = new ObservedTargetCombatSkill(
            selected.SkillId,
            selected.StaticFacts.Category,
            request.Direction,
            request.SlotIndex,
            request.VisiblePowerPercent);
        return new TargetSkillSelectionResult(
            TargetSkillSelectionStatus.Resolved,
            search.Catalogue,
            search.CandidateSetMayBeTruncated,
            candidates,
            new ResolvedTargetSkillSelection(
                observation,
                selected.StaticFacts,
                selected.SnapshotPresence));
    }

    private static TargetSkillResolutionCandidate CreateCandidate(
        CombatSkillSearchItem item,
        TargetSkillSelectionRequest request)
    {
        var definition = item.Definition;
        var category = ResolveCategory(definition.EquipmentType);
        var facts = category is null
                    || !definition.SlotContribution.IsAvailable
            ? null
            : new TargetSkillStaticFacts(
                definition.SkillId,
                item.DisplayName,
                category.Value,
                definition.BaseGridCost,
                definition.SlotContribution,
                definition.Element,
                definition.Effects.Direct,
                definition.Effects.Reverse);
        var presence = request.TargetSnapshotSkillIds is null
            ? TargetSkillSnapshotPresence.Unknown
            : request.TargetSnapshotSkillIds.Contains(definition.SkillId)
                ? TargetSkillSnapshotPresence.Present
                : TargetSkillSnapshotPresence.Absent;

        return new TargetSkillResolutionCandidate(
            definition.SkillId,
            item.DisplayName,
            SearchCombatSkillDefinitions.IsExactMatch(
                definition,
                SearchCombatSkillDefinitions.NormalizeSearchText(
                    request.Query))
                ? TargetSkillMatchKind.Exact
                : TargetSkillMatchKind.Partial,
            presence,
            facts);
    }

    private static SkillCategory? ResolveCategory(
        CatalogueField<CombatSkillEquipmentType> equipmentType)
    {
        if (!equipmentType.IsAvailable)
        {
            return null;
        }

        return equipmentType.Value switch
        {
            CombatSkillEquipmentType.Neigong => SkillCategory.Neigong,
            CombatSkillEquipmentType.Attack => SkillCategory.Attack,
            CombatSkillEquipmentType.Agility => SkillCategory.Agility,
            CombatSkillEquipmentType.Defense => SkillCategory.Defense,
            CombatSkillEquipmentType.Assistance => SkillCategory.Assistance,
            _ => null
        };
    }

    private static TargetSkillSelectionResult Result(
        TargetSkillSelectionStatus status,
        CombatSkillSearchResult search,
        ImmutableArray<TargetSkillResolutionCandidate> candidates) => new(
            status,
            search.Catalogue,
            search.CandidateSetMayBeTruncated,
            candidates,
            ResolvedSelection: null);
}
