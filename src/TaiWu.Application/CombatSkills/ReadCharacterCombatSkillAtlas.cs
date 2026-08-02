using System.Collections.Immutable;
using TaiWu.Domain.CombatSkills;

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
                progressFailureReason: null);
        }

        CharacterCombatSkillProgressReadResult progress;
        try
        {
            progress = await progressReader.ReadAsync(
                    new CharacterCombatSkillProgressReadRequest(
                        request.CharacterId),
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
                exception.Message);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (progress.Status != CharacterProgressReadStatus.Available)
        {
            return Empty(catalogue, progress.Status, progress.Reason);
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
                progressFailureReason: null);
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
                progressFailureReason: null);
        }

        var bySkillId = definitions.ToDictionary(
            definition => definition.SkillId);
        var entries = progress.Progress
            .Select(value =>
            {
                bySkillId.TryGetValue(value.SkillId, out var definition);
                return new CharacterCombatSkillAtlasEntry(
                    value,
                    definition,
                    definition is null
                        ? new CombatSkillDisplayName(
                            request.PreferredLanguage,
                            CatalogueField<LocalizedCombatSkillName>.Unavailable(
                                "The skill definition is absent from the current catalogue."),
                            UsedFallback: false)
                        : SearchCombatSkillDefinitions.ResolveName(
                            definition,
                            request.PreferredLanguage));
            })
            .OrderBy(entry => entry.Progress.SkillId)
            .ToImmutableArray();

        return new CharacterCombatSkillAtlasResult(
            catalogue,
            CharacterProgressReadStatus.Available,
            ProgressFailureReason: null,
            entries);
    }

    private static CharacterCombatSkillAtlasResult Empty(
        CombatSkillCatalogueStatusResult catalogue,
        CharacterProgressReadStatus progressStatus,
        string? progressFailureReason) => new(
            catalogue,
            progressStatus,
            progressFailureReason,
            Entries: []);
}
