using System.Collections.Immutable;
using TaiWu.Domain.CombatSkills;

namespace TaiWu.Application.CombatSkills;

public sealed class SearchCombatSkillDefinitions(
    ICombatSkillDefinitionSource definitionSource,
    ICombatSkillCatalogueRepository repository)
{
    public async Task<CombatSkillSearchResult> ExecuteAsync(
        CombatSkillSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var catalogue = await new ReadCombatSkillCatalogueStatus(
                definitionSource,
                repository)
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        if (catalogue.Status != CombatSkillCatalogueStatus.Current)
        {
            return Empty(catalogue, request);
        }

        IReadOnlyList<CombatSkillDefinition> candidates;
        try
        {
            candidates = await repository.QueryAsync(
                    request.Filter,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Empty(
                RepositoryFailure(catalogue, exception.Message),
                request);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (candidates is null
            || candidates.Any(candidate => candidate is null)
            || candidates.GroupBy(candidate => candidate.SkillId)
                .Any(group => group.Count() > 1))
        {
            return Empty(
                RepositoryFailure(
                    catalogue,
                    "The catalogue query returned invalid definitions."),
                request);
        }

        var matches = candidates
            .Where(definition => Matches(definition, request.Query))
            .Select(definition => new RankedItem(
                new CombatSkillSearchItem(
                    definition,
                    ResolveName(definition, request.PreferredLanguage)),
                IsExactMatch(definition, request.Query)))
            .OrderByDescending(item => item.IsExactMatch)
            .ThenBy(
                item => item.Item.DisplayName.Value.IsAvailable ? 0 : 1)
            .ThenBy(
                item => item.Item.DisplayName.Value.IsAvailable
                    ? item.Item.DisplayName.Value.Value.Text
                    : string.Empty,
                StringComparer.Ordinal)
            .ThenBy(item => item.Item.Definition.SkillId)
            .Select(item => item.Item)
            .ToImmutableArray();
        var page = matches
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToImmutableArray();

        return new CombatSkillSearchResult(
            catalogue,
            matches.Length,
            request.Offset,
            request.Limit,
            candidates.Count >= request.Filter.CandidateLimit,
            page);
    }

    internal static CombatSkillDisplayName ResolveName(
        CombatSkillDefinition definition,
        CatalogueLanguage preferredLanguage)
    {
        var resolved = definition.Names.Resolve(preferredLanguage);
        return new CombatSkillDisplayName(
            preferredLanguage,
            resolved,
            resolved.IsAvailable
            && resolved.Value.Language != preferredLanguage);
    }

    internal static CombatSkillCatalogueStatusResult RepositoryFailure(
        CombatSkillCatalogueStatusResult current,
        string? reason) => current with
        {
            Status = CombatSkillCatalogueStatus.RepositoryFailed,
            DefinitionCount = 0,
            Reason = string.IsNullOrWhiteSpace(reason)
                ? "The helper-owned catalogue query failed."
                : reason.Trim()
        };

    private static bool Matches(
        CombatSkillDefinition definition,
        string? query) => query is null || definition.Names.Values.Any(
            name => name.Text.Contains(
                query,
                StringComparison.OrdinalIgnoreCase));

    private static bool IsExactMatch(
        CombatSkillDefinition definition,
        string? query) => query is not null && definition.Names.Values.Any(
            name => string.Equals(
                name.Text,
                query,
                StringComparison.OrdinalIgnoreCase));

    private static CombatSkillSearchResult Empty(
        CombatSkillCatalogueStatusResult catalogue,
        CombatSkillSearchRequest request) => new(
            catalogue,
            0,
            request.Offset,
            request.Limit,
            CandidateSetMayBeTruncated: false,
            Items: []);

    private sealed record RankedItem(
        CombatSkillSearchItem Item,
        bool IsExactMatch);
}
