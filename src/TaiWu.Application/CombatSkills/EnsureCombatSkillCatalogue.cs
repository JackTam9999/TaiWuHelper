namespace TaiWu.Application.CombatSkills;

public sealed class EnsureCombatSkillCatalogue(
    ICombatSkillDefinitionSource definitionSource,
    ICombatSkillCatalogueRepository repository)
{
    public async Task<EnsureCombatSkillCatalogueResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CombatSkillDefinitionSourceResult installed;
        try
        {
            installed = await definitionSource.ReadAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Failure(
                EnsureCombatSkillCatalogueStatus.SourceReadFailed,
                exception.Message);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (installed.Status != DefinitionSourceReadStatus.Available)
        {
            return Failure(MapSourceFailure(installed.Status), installed.Reason);
        }

        var identity = installed.SourceIdentity!;
        CombatSkillCatalogueRepositorySnapshot stored;
        try
        {
            stored = await repository.ReadStateAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Failure(
                EnsureCombatSkillCatalogueStatus.RebuildFailed,
                exception.Message,
                identity);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var status = ReadCombatSkillCatalogueStatus.Evaluate(installed, stored);
        if (status.Status == CombatSkillCatalogueStatus.Current)
        {
            return new EnsureCombatSkillCatalogueResult(
                EnsureCombatSkillCatalogueStatus.Current,
                installed.Definitions.Length,
                identity,
                Reason: null);
        }

        CatalogueReplaceResult replacement;
        try
        {
            replacement = await repository.ReplaceAsync(
                    identity,
                    installed.Definitions,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Failure(
                EnsureCombatSkillCatalogueStatus.RebuildFailed,
                exception.Message,
                identity);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return replacement.Succeeded
            ? new EnsureCombatSkillCatalogueResult(
                EnsureCombatSkillCatalogueStatus.Rebuilt,
                installed.Definitions.Length,
                identity,
                Reason: null)
            : Failure(
                EnsureCombatSkillCatalogueStatus.RebuildFailed,
                replacement.Reason,
                identity);
    }

    private static EnsureCombatSkillCatalogueStatus MapSourceFailure(
        DefinitionSourceReadStatus status) => status switch
        {
            DefinitionSourceReadStatus.MissingSources =>
                EnsureCombatSkillCatalogueStatus.MissingSources,
            DefinitionSourceReadStatus.UnsupportedVersion =>
                EnsureCombatSkillCatalogueStatus.UnsupportedVersion,
            DefinitionSourceReadStatus.Failed =>
                EnsureCombatSkillCatalogueStatus.SourceReadFailed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

    private static EnsureCombatSkillCatalogueResult Failure(
        EnsureCombatSkillCatalogueStatus status,
        string? reason,
        CombatSkillCatalogueSourceIdentity? identity = null) => new(
            status,
            0,
            identity,
            string.IsNullOrWhiteSpace(reason)
                ? "The catalogue operation failed without a diagnostic."
                : reason.Trim());
}
