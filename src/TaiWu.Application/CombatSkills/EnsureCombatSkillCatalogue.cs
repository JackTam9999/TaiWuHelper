namespace TaiWu.Application.CombatSkills;

public sealed class EnsureCombatSkillCatalogue(
    ICombatSkillDefinitionSource definitionSource,
    ICombatSkillCatalogueRepository repository)
{
    private static readonly SemaphoreSlim EnsureGate = new(1, 1);

    public async Task<EnsureCombatSkillCatalogueResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ExecuteControlledAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            EnsureGate.Release();
        }
    }

    private async Task<EnsureCombatSkillCatalogueResult> ExecuteControlledAsync(
        CancellationToken cancellationToken)
    {
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
            return RepositoryUnavailableFailure(exception.Message, identity);
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

        if (stored.State == CatalogueRepositoryState.Failed)
        {
            return RebuildFailure(stored.Reason, identity, stored);
        }

        CatalogueReplaceResult replacement;
        try
        {
            replacement = await repository.ReplaceAsync(
                    identity,
                    installed.Definitions,
                    installed.Diagnostics,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return RebuildFailure(exception.Message, identity, stored);
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
                identity,
                stored);
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
        CombatSkillCatalogueSourceIdentity? identity = null,
        CombatSkillCatalogueRepositorySnapshot? retained = null)
    {
        if (status != EnsureCombatSkillCatalogueStatus.RebuildFailed
            || retained is null)
        {
            return new EnsureCombatSkillCatalogueResult(
                status,
                0,
                identity,
                NormalizeReason(reason));
        }

        return RebuildFailure(reason, identity!, retained);
    }

    private static EnsureCombatSkillCatalogueResult RebuildFailure(
        string? reason,
        CombatSkillCatalogueSourceIdentity identity,
        CombatSkillCatalogueRepositorySnapshot retained)
    {
        var recovery = retained.State switch
        {
            CatalogueRepositoryState.Ready =>
                CatalogueRecoveryStatus.StaleCataloguePreserved,
            CatalogueRepositoryState.Corrupt =>
                CatalogueRecoveryStatus.CorruptCatalogueRemains,
            CatalogueRepositoryState.Failed =>
                CatalogueRecoveryStatus.RepositoryUnavailable,
            CatalogueRepositoryState.Missing => CatalogueRecoveryStatus.None,
            _ => throw new ArgumentOutOfRangeException(nameof(retained))
        };
        var recoveryReason = recovery switch
        {
            CatalogueRecoveryStatus.StaleCataloguePreserved =>
                " The previously committed catalogue remains available but is stale.",
            CatalogueRecoveryStatus.CorruptCatalogueRemains =>
                " The corrupt helper-owned catalogue still requires recovery.",
            CatalogueRecoveryStatus.RepositoryUnavailable =>
                " The helper-owned catalogue is unavailable; no rebuild was attempted.",
            _ => string.Empty
        };

        return new EnsureCombatSkillCatalogueResult(
            EnsureCombatSkillCatalogueStatus.RebuildFailed,
            0,
            identity,
            NormalizeReason(reason) + recoveryReason,
            recovery,
            retained.State == CatalogueRepositoryState.Ready
                ? retained.SourceIdentity
                : null,
            retained.State == CatalogueRepositoryState.Ready
                ? retained.DefinitionCount
                : 0,
            retained.State == CatalogueRepositoryState.Ready
                ? retained.BuiltAtUtc
                : null);
    }

    private static EnsureCombatSkillCatalogueResult RepositoryUnavailableFailure(
        string? reason,
        CombatSkillCatalogueSourceIdentity identity) => new(
            EnsureCombatSkillCatalogueStatus.RebuildFailed,
            0,
            identity,
            NormalizeReason(reason)
            + " The helper-owned catalogue is unavailable; no rebuild was attempted.",
            CatalogueRecoveryStatus.RepositoryUnavailable);

    private static string NormalizeReason(string? reason) =>
        string.IsNullOrWhiteSpace(reason)
            ? "The catalogue operation failed without a diagnostic."
            : reason.Trim();
}
