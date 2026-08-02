using System.Collections.Immutable;

namespace TaiWu.Application.CombatSkills;

public sealed class ReadCombatSkillDetails(
    ICombatSkillDefinitionSource definitionSource,
    ICombatSkillCatalogueRepository repository,
    ICharacterCombatSkillProgressReader? progressReader = null)
{
    public async Task<CombatSkillDetailsResult> ExecuteAsync(
        CombatSkillDetailsRequest request,
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
            return Empty(catalogue, request.SkillId);
        }

        Domain.CombatSkills.CombatSkillDefinition? definition;
        try
        {
            definition = await repository.GetAsync(
                    request.SkillId,
                    cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
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
                request.SkillId);
        }

        var displayName = definition is null
            ? null
            : SearchCombatSkillDefinitions.ResolveName(
                definition,
                request.PreferredLanguage);
        var staticIssues = displayName is not null
                           && (!displayName.Value.IsAvailable
                               || displayName.UsedFallback)
            ? CombatSkillQueryIssue.PartialLocalization
            : CombatSkillQueryIssue.None;
        if (progressReader is null)
        {
            return new CombatSkillDetailsResult(
                catalogue,
                request.SkillId,
                definition,
                displayName,
                CharacterProgressReadStatus.NotRead,
                ProgressFailureReason: null,
                ProgressMetadata: null,
                CharacterState: null,
                staticIssues,
                Diagnostics: []);
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
            return new CombatSkillDetailsResult(
                catalogue,
                request.SkillId,
                definition,
                displayName,
                CharacterProgressReadStatus.SaveReadFailed,
                ProgressFailureReason: string.IsNullOrWhiteSpace(
                    exception.Message)
                    ? "Character progress could not be read."
                    : exception.Message,
                ProgressMetadata: null,
                CharacterState: null,
                staticIssues,
                [
                    new CombatSkillQueryDiagnostic(
                        "PROGRESS_READ_FAILED",
                        string.IsNullOrWhiteSpace(exception.Message)
                            ? "Character progress could not be read."
                            : exception.Message,
                        request.SkillId)
                ]);
        }

        if (progress.Status != CharacterProgressReadStatus.Available)
        {
            return new CombatSkillDetailsResult(
                catalogue,
                request.SkillId,
                definition,
                displayName,
                progress.Status,
                progress.Reason,
                ProgressMetadata: null,
                CharacterState: null,
                staticIssues,
                Diagnostics: []);
        }

        var skillProgress = progress.Progress.FirstOrDefault(value =>
            value.SkillId == request.SkillId);
        if (definition is null && skillProgress is null)
        {
            return new CombatSkillDetailsResult(
                catalogue,
                request.SkillId,
                Definition: null,
                DisplayName: null,
                CharacterProgressReadStatus.Available,
                ProgressFailureReason: null,
                progress.Metadata,
                CharacterState: null,
                staticIssues,
                Diagnostics: []);
        }

        var entry = ReadCharacterCombatSkillAtlas.CreateEntry(
            request.SkillId,
            definition,
            skillProgress,
            progress.Metadata!,
            request.PreferredLanguage);
        var issues = staticIssues | entry.Issues;
        if (progress.Metadata!.Warnings.Length > 0)
        {
            issues |= CombatSkillQueryIssue.ProgressWarnings;
        }

        return new CombatSkillDetailsResult(
            catalogue,
            request.SkillId,
            definition,
            entry.DisplayName,
            CharacterProgressReadStatus.Available,
            ProgressFailureReason: null,
            progress.Metadata,
            entry,
            issues,
            entry.Diagnostics);
    }

    private static CombatSkillDetailsResult Empty(
        CombatSkillCatalogueStatusResult catalogue,
        int skillId) => new(
            catalogue,
            skillId,
            Definition: null,
            DisplayName: null,
            CharacterProgressReadStatus.NotRead,
            ProgressFailureReason: null,
            ProgressMetadata: null,
            CharacterState: null,
            Issues: CombatSkillQueryIssue.None,
            Diagnostics: ImmutableArray<CombatSkillQueryDiagnostic>.Empty);
}
