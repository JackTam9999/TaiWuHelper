namespace TaiWu.Application.CombatSkills;

public sealed class ReadCombatSkillDetails(
    ICombatSkillDefinitionSource definitionSource,
    ICombatSkillCatalogueRepository repository)
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
            return new CombatSkillDetailsResult(
                catalogue,
                request.SkillId,
                Definition: null,
                DisplayName: null);
        }

        try
        {
            var definition = await repository.GetAsync(
                    request.SkillId,
                    cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return new CombatSkillDetailsResult(
                catalogue,
                request.SkillId,
                definition,
                definition is null
                    ? null
                    : SearchCombatSkillDefinitions.ResolveName(
                        definition,
                        request.PreferredLanguage));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new CombatSkillDetailsResult(
                SearchCombatSkillDefinitions.RepositoryFailure(
                    catalogue,
                    exception.Message),
                request.SkillId,
                Definition: null,
                DisplayName: null);
        }
    }
}
