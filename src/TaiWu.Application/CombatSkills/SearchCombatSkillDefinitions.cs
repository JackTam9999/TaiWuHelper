using System.Collections.Immutable;
using System.Text;
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

        var normalizedQuery = NormalizeSearchText(request.Query);
        var ranked = candidates
            .Where(definition => Matches(definition, normalizedQuery))
            .Select(definition => new RankedItem(
                new CombatSkillSearchItem(
                    definition,
                    ResolveName(definition, request.PreferredLanguage)),
                IsExactMatch(definition, normalizedQuery)))
            .ToImmutableArray();
        var matches = Order(ranked, request.Sort)
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

    private static IOrderedEnumerable<RankedItem> Order(
        IEnumerable<RankedItem> values,
        CombatSkillSearchSort sort)
    {
        var ranked = values.OrderByDescending(value => value.IsExactMatch);
        return sort switch
        {
            CombatSkillSearchSort.DisplayName => ranked
                .ThenBy(value =>
                    value.Item.DisplayName.Value.IsAvailable ? 0 : 1)
                .ThenBy(
                    value => value.Item.DisplayName.Value.IsAvailable
                        ? NormalizeSearchText(
                            value.Item.DisplayName.Value.Value.Text)
                        : string.Empty,
                    StringComparer.Ordinal)
                .ThenBy(value => value.Item.Definition.SkillId),
            CombatSkillSearchSort.SkillId => ranked
                .ThenBy(value => value.Item.Definition.SkillId),
            CombatSkillSearchSort.Grade => ranked
                .ThenBy(value =>
                    value.Item.Definition.Grade.IsAvailable ? 0 : 1)
                .ThenBy(value =>
                    value.Item.Definition.Grade.IsAvailable
                        ? value.Item.Definition.Grade.Value.Value
                        : int.MaxValue)
                .ThenBy(value => value.Item.Definition.SkillId),
            _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, null)
        };
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
        string? normalizedQuery) => normalizedQuery is null
        || definition.Names.Values.Any(name =>
            NormalizeSearchText(name.Text)!.Contains(
                normalizedQuery,
                StringComparison.Ordinal));

    private static bool IsExactMatch(
        CombatSkillDefinition definition,
        string? normalizedQuery) => normalizedQuery is not null
        && definition.Names.Values.Any(name => string.Equals(
            NormalizeSearchText(name.Text),
            normalizedQuery,
            StringComparison.Ordinal));

    internal static string? NormalizeSearchText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var compatibilityNormalized = value
            .Normalize(NormalizationForm.FormKC)
            .ToUpperInvariant();
        return string.Join(
            ' ',
            compatibilityNormalized.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries
                | StringSplitOptions.TrimEntries));
    }

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
