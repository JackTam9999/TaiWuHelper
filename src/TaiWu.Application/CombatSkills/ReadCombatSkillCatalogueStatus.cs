namespace TaiWu.Application.CombatSkills;

public sealed class ReadCombatSkillCatalogueStatus(
    ICombatSkillDefinitionSource definitionSource,
    ICombatSkillCatalogueRepository repository,
    CombatSkillCatalogueMaintenanceCoordinator? coordinator = null)
{
    private readonly CombatSkillCatalogueMaintenanceCoordinator _coordinator =
        coordinator ?? new CombatSkillCatalogueMaintenanceCoordinator();

    public async Task<CombatSkillCatalogueStatusResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_coordinator.IsRebuilding)
        {
            return new CombatSkillCatalogueStatusResult(
                CombatSkillCatalogueStatus.Rebuilding,
                DefinitionCount: 0,
                InstalledSource: null,
                StoredSource: null,
                BuiltAtUtc: null,
                "The helper-owned catalogue is rebuilding.");
        }

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
            return SourceFailure(
                CombatSkillCatalogueStatus.SourceReadFailed,
                exception.Message);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (installed.Status != DefinitionSourceReadStatus.Available)
        {
            return SourceFailure(
                MapSourceFailure(installed.Status),
                installed.Reason);
        }

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
            return RepositoryFailure(installed, exception.Message);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Evaluate(installed, stored);
    }

    internal static CombatSkillCatalogueStatusResult Evaluate(
        CombatSkillDefinitionSourceResult installed,
        CombatSkillCatalogueRepositorySnapshot stored)
    {
        var identity = installed.SourceIdentity
            ?? throw new ArgumentException(
                "An available definition source requires an identity.",
                nameof(installed));

        return stored.State switch
        {
            CatalogueRepositoryState.Missing => new(
                CombatSkillCatalogueStatus.Missing,
                0,
                identity,
                stored.SourceIdentity,
                stored.BuiltAtUtc,
                "The helper-owned catalogue has not been built."),
            CatalogueRepositoryState.Corrupt => new(
                CombatSkillCatalogueStatus.Corrupt,
                0,
                identity,
                stored.SourceIdentity,
                stored.BuiltAtUtc,
                stored.Reason),
            CatalogueRepositoryState.Failed =>
                RepositoryFailure(installed, stored.Reason),
            CatalogueRepositoryState.Ready
                when stored.SourceIdentity != identity
                     || stored.DefinitionCount != installed.Definitions.Length => new(
                         CombatSkillCatalogueStatus.Stale,
                         stored.DefinitionCount,
                         identity,
                         stored.SourceIdentity,
                         stored.BuiltAtUtc,
                         "The stored catalogue does not match the installed sources."),
            CatalogueRepositoryState.Ready => new(
                CombatSkillCatalogueStatus.Current,
                stored.DefinitionCount,
                identity,
                stored.SourceIdentity,
                stored.BuiltAtUtc,
                Reason: null),
            _ => throw new ArgumentOutOfRangeException(nameof(stored))
        };
    }

    internal static CombatSkillCatalogueStatus MapSourceFailure(
        DefinitionSourceReadStatus status) => status switch
        {
            DefinitionSourceReadStatus.MissingSources =>
                CombatSkillCatalogueStatus.MissingSources,
            DefinitionSourceReadStatus.UnsupportedVersion =>
                CombatSkillCatalogueStatus.UnsupportedVersion,
            DefinitionSourceReadStatus.Failed =>
                CombatSkillCatalogueStatus.SourceReadFailed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

    internal static CombatSkillCatalogueStatusResult RepositoryFailure(
        CombatSkillDefinitionSourceResult installed,
        string? reason) => new(
            CombatSkillCatalogueStatus.RepositoryFailed,
            0,
            installed.SourceIdentity,
            StoredSource: null,
            BuiltAtUtc: null,
            string.IsNullOrWhiteSpace(reason)
                ? "The helper-owned catalogue could not be read."
                : reason.Trim());

    internal static CombatSkillCatalogueStatusResult SourceFailure(
        CombatSkillCatalogueStatus status,
        string? reason) => new(
            status,
            0,
            InstalledSource: null,
            StoredSource: null,
            BuiltAtUtc: null,
            string.IsNullOrWhiteSpace(reason)
                ? "The installed catalogue sources could not be read."
                : reason.Trim());
}
