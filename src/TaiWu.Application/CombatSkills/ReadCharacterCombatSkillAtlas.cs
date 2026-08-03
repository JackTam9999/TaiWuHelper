using System.Collections.Immutable;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Application.CombatSkills;

public sealed class ReadCharacterCombatSkillAtlas(
    ICombatSkillDefinitionSource definitionSource,
    ICombatSkillCatalogueRepository repository,
    ICharacterCombatSkillProgressReader progressReader)
{
    public async Task<CharacterCombatSkillAtlasResult> ExecuteAsync(
        CharacterCombatSkillAtlasRequest request,
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
            return Empty(
                catalogue,
                CharacterProgressReadStatus.NotRead,
                progressFailureReason: null,
                request);
        }

        CharacterCombatSkillProgressReadResult progress;
        try
        {
            progress = await progressReader.ReadAsync(
                    new CharacterCombatSkillProgressReadRequest(
                        request.CharacterId,
                        request.PreferredLanguage),
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
                catalogue,
                CharacterProgressReadStatus.SaveReadFailed,
                exception.Message,
                request);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (progress.Status != CharacterProgressReadStatus.Available)
        {
            return Empty(
                catalogue,
                progress.Status,
                progress.Reason,
                request);
        }

        IReadOnlyList<CombatSkillDefinition> definitions;
        try
        {
            definitions = await repository.QueryAsync(
                    new CombatSkillCatalogueFilter(),
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
                SearchCombatSkillDefinitions.RepositoryFailure(
                    catalogue,
                    exception.Message),
                CharacterProgressReadStatus.Available,
                progressFailureReason: null,
                request,
                progress.Metadata);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (definitions is null
            || definitions.Any(definition => definition is null)
            || definitions.GroupBy(definition => definition.SkillId)
                .Any(group => group.Count() > 1))
        {
            return Empty(
                SearchCombatSkillDefinitions.RepositoryFailure(
                    catalogue,
                    "The catalogue query returned invalid definitions."),
                CharacterProgressReadStatus.Available,
                progressFailureReason: null,
                request,
                progress.Metadata);
        }

        var bySkillId = definitions.ToDictionary(
            definition => definition.SkillId);
        var progressBySkillId = progress.Progress.ToDictionary(
            value => value.SkillId);
        var normalizedQuery = SearchCombatSkillDefinitions.NormalizeSearchText(
            request.Query);
        var candidates = bySkillId.Keys
            .Concat(progressBySkillId.Keys)
            .Distinct()
            .Select(skillId =>
            {
                bySkillId.TryGetValue(skillId, out var definition);
                progressBySkillId.TryGetValue(skillId, out var skillProgress);
                return CreateEntry(
                    skillId,
                    definition,
                    skillProgress,
                    progress.Metadata!,
                    request.PreferredLanguage);
            })
            .Where(entry => MatchesDefinitionFilter(
                entry.Definition,
                request.DefinitionFilter))
            .Where(entry => MatchesQuery(entry.Definition, normalizedQuery))
            .Where(entry => MatchesProgressFilter(
                entry,
                request.ProgressFilter))
            .Select(entry => new RankedEntry(
                entry,
                IsExactMatch(entry.Definition, normalizedQuery)))
            .ToImmutableArray();
        var ranked = OrderCandidates(candidates, request.Sort)
            .ToImmutableArray();
        var entries = ranked
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(value => value.Entry)
            .ToImmutableArray();
        var diagnostics = ranked
            .SelectMany(value => value.Entry.Diagnostics)
            .Distinct()
            .OrderBy(value => value.SkillId)
            .ThenBy(value => value.Code, StringComparer.Ordinal)
            .ToImmutableArray();
        var issues = ranked.Aggregate(
            progress.Metadata!.Warnings.Length > 0
                ? CombatSkillQueryIssue.ProgressWarnings
                : CombatSkillQueryIssue.None,
            (current, value) => current | value.Entry.Issues);

        return new CharacterCombatSkillAtlasResult(
            catalogue,
            CharacterProgressReadStatus.Available,
            ProgressFailureReason: null,
            progress.Metadata,
            ranked.Length,
            request.Offset,
            request.Limit,
            CandidateSetMayBeTruncated:
                definitions.Count
                >= CombatSkillCatalogueFilter.MaximumCandidateCount,
            issues,
            diagnostics,
            entries);
    }

    internal static CharacterCombatSkillAtlasEntry CreateEntry(
        int skillId,
        CombatSkillDefinition? definition,
        CharacterCombatSkillProgress? progress,
        CharacterCombatSkillProgressMetadata metadata,
        CatalogueLanguage preferredLanguage)
    {
        List<CombatSkillQueryDiagnostic> diagnostics = [];
        if (definition is null)
        {
            diagnostics.Add(new CombatSkillQueryDiagnostic(
                "STATIC_DEFINITION_MISSING",
                "Character progress has no matching static catalogue "
                + "definition.",
                skillId));
        }

        var displayName = definition is null
            ? new CombatSkillDisplayName(
                preferredLanguage,
                CatalogueField<LocalizedCombatSkillName>.Unavailable(
                    "The skill definition is absent from the current "
                    + "catalogue."),
                UsedFallback: false)
            : SearchCombatSkillDefinitions.ResolveName(
                definition,
                preferredLanguage);
        var learned = progress?.Learned
            ?? SkillProgressField<bool>.Available(
                false,
                new SkillProgressSource(
                    SkillProgressSourceKind.SaveSnapshot,
                    $"save:{metadata.SaveSnapshot.Sha256}",
                    $"combat-skill:{skillId}:learned-collection-absence"));

        return new CharacterCombatSkillAtlasEntry(
            skillId,
            progress,
            definition,
            displayName,
            learned,
            EffectiveGridCost(skillId, definition, progress),
            diagnostics);
    }

    private static SkillProgressField<int> EffectiveGridCost(
        int skillId,
        CombatSkillDefinition? definition,
        CharacterCombatSkillProgress? progress)
    {
        var source = new SkillProgressSource(
            SkillProgressSourceKind.VerifiedRule,
            "verified-rule:e2-002",
            $"combat-skill:{skillId}:effective-grid-cost");
        if (definition is null || !definition.BaseGridCost.IsAvailable)
        {
            return SkillProgressField<int>.Unavailable(
                "Current effective cost is unavailable because the static "
                + "base grid cost is unavailable.",
                source);
        }

        if (progress is null)
        {
            return SkillProgressField<int>.Unavailable(
                "Current effective cost is unavailable because the "
                + "character has not learned this skill.",
                source);
        }

        if (!progress.Learned.IsAvailable || !progress.Learned.Value)
        {
            return SkillProgressField<int>.Unavailable(
                "Current effective cost is unavailable because learned "
                + "membership is not confirmed.",
                source);
        }

        if (!progress.Simplified.IsAvailable)
        {
            return SkillProgressField<int>.Unavailable(
                "Current effective cost is unavailable because the "
                + "simplification state is unavailable.",
                source);
        }

        var baseCost = definition.BaseGridCost.Value.Value;
        return SkillProgressField<int>.Available(
            Math.Max(1, baseCost - (progress.Simplified.Value ? 1 : 0)),
            source);
    }

    private static bool MatchesDefinitionFilter(
        CombatSkillDefinition? definition,
        CombatSkillCatalogueFilter filter)
    {
        if (definition is null)
        {
            return filter.Category is null
                   && filter.Grade is null
                   && filter.Faction is null
                   && filter.Element is null
                   && filter.EquipmentType is null;
        }

        return Matches(definition.Category, filter.Category)
               && Matches(definition.Grade, filter.Grade)
               && Matches(definition.Faction, filter.Faction)
               && Matches(definition.Element, filter.Element)
               && Matches(definition.EquipmentType, filter.EquipmentType);
    }

    private static bool Matches<T>(
        CatalogueField<T> field,
        T? expected)
        where T : struct => expected is null
        || field.IsAvailable && EqualityComparer<T>.Default.Equals(
            field.Value,
            expected.Value);

    private static bool MatchesQuery(
        CombatSkillDefinition? definition,
        string? normalizedQuery) => normalizedQuery is null
        || definition is not null && definition.Names.Values.Any(name =>
            SearchCombatSkillDefinitions.NormalizeSearchText(name.Text)!
                .Contains(normalizedQuery, StringComparison.Ordinal));

    private static bool IsExactMatch(
        CombatSkillDefinition? definition,
        string? normalizedQuery) => normalizedQuery is not null
        && definition is not null
        && definition.Names.Values.Any(name => string.Equals(
            SearchCombatSkillDefinitions.NormalizeSearchText(name.Text),
            normalizedQuery,
            StringComparison.Ordinal));

    private static IOrderedEnumerable<RankedEntry> OrderCandidates(
        IEnumerable<RankedEntry> candidates,
        CharacterCombatSkillAtlasSort sort) => sort switch
        {
            CharacterCombatSkillAtlasSort.CategoryThenGrade => candidates
                .OrderBy(value =>
                    value.Entry.Definition?.Category.IsAvailable == true
                        ? (int)value.Entry.Definition.Category.Value
                        : int.MaxValue)
                .ThenBy(value =>
                    value.Entry.Definition?.Grade.IsAvailable == true ? 0 : 1)
                .ThenByDescending(value =>
                    value.Entry.Definition?.Grade.IsAvailable == true
                        ? value.Entry.Definition.Grade.Value.Value
                        : int.MinValue)
                .ThenBy(DisplayNameAvailability)
                .ThenBy(
                    NormalizedDisplayName,
                    StringComparer.Ordinal)
                .ThenBy(value => value.Entry.SkillId),
            _ => candidates
                .OrderByDescending(value => value.IsExactMatch)
                .ThenBy(DisplayNameAvailability)
                .ThenBy(
                    NormalizedDisplayName,
                    StringComparer.Ordinal)
                .ThenBy(value => value.Entry.SkillId)
        };

    private static int DisplayNameAvailability(RankedEntry value) =>
        value.Entry.DisplayName.Value.IsAvailable ? 0 : 1;

    private static string NormalizedDisplayName(RankedEntry value) =>
        value.Entry.DisplayName.Value.IsAvailable
            ? SearchCombatSkillDefinitions.NormalizeSearchText(
                value.Entry.DisplayName.Value.Value.Text)!
            : string.Empty;

    private static bool MatchesProgressFilter(
        CharacterCombatSkillAtlasEntry entry,
        CharacterCombatSkillProgressFilter filter)
    {
        if (!Matches(entry.Learned, filter.Learned))
        {
            return false;
        }

        var progress = entry.Progress;
        if (progress is null)
        {
            return filter.HasProficiency is null
                   && filter.StudyComplete is null
                   && filter.BreakthroughReady is null
                   && filter.BrokenThrough is null
                   && filter.ActiveDirection is null
                   && filter.AttainmentMastered is null
                   && filter.Simplified is null
                   && filter.Activated is null
                   && filter.Equipped is null;
        }

        return MatchesAvailability(
                   progress.Proficiency.Current,
                   filter.HasProficiency)
               && Matches(
                   progress.StudySummary.IsComplete,
                   filter.StudyComplete)
               && MatchesProjection(
                   progress.Breakthrough,
                   filter.BreakthroughReady,
                   value => value.CanBreakthroughNow)
               && MatchesProjection(
                   progress.Breakthrough,
                   filter.BrokenThrough,
                   value => value.IsBrokenOut)
               && Matches(progress.ActiveDirection, filter.ActiveDirection)
               && Matches(
                   progress.AttainmentMastered,
                   filter.AttainmentMastered)
               && Matches(progress.Simplified, filter.Simplified)
               && Matches(progress.Activated, filter.Activated)
               && Matches(progress.Equipped, filter.Equipped);
    }

    private static bool Matches<T>(SkillProgressField<T> field, T? expected)
        where T : struct => expected is null
        || field.IsAvailable && EqualityComparer<T>.Default.Equals(
            field.Value,
            expected.Value);

    private static bool MatchesAvailability<T>(
        SkillProgressField<T> field,
        bool? expected) => expected is null || field.IsAvailable == expected;

    private static bool MatchesProjection<T>(
        SkillProgressField<T> field,
        bool? expected,
        Func<T, bool> projection) => expected is null
        || field.IsAvailable && projection(field.Value) == expected;

    private static CharacterCombatSkillAtlasResult Empty(
        CombatSkillCatalogueStatusResult catalogue,
        CharacterProgressReadStatus progressStatus,
        string? progressFailureReason,
        CharacterCombatSkillAtlasRequest request,
        CharacterCombatSkillProgressMetadata? metadata = null) => new(
            catalogue,
            progressStatus,
            progressFailureReason,
            metadata,
            TotalMatches: 0,
            request.Offset,
            request.Limit,
            CandidateSetMayBeTruncated: false,
            Issues: CombatSkillQueryIssue.None,
            Diagnostics: [],
            Entries: []);

    private sealed record RankedEntry(
        CharacterCombatSkillAtlasEntry Entry,
        bool IsExactMatch);
}
