using Microsoft.AspNetCore.Mvc;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWuAPI.Contracts.CombatSkills;

namespace TaiWuAPI.Controllers;

[ApiController]
[Route("api/combat-skills")]
public sealed class CombatSkillsController(
    ICombatSkillDefinitionSource definitionSource,
    ICombatSkillCatalogueRepository repository,
    ICharacterCombatSkillProgressReader progressReader,
    ICharacterCombatSkillProgressCacheMaintenance progressCacheMaintenance)
    : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType<CombatSkillCatalogueStatusResponse>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<CombatSkillCatalogueStatusResponse>> Status(
        CancellationToken cancellationToken = default)
    {
        var result = await new ReadCombatSkillCatalogueStatus(
                definitionSource,
                repository)
            .ExecuteAsync(cancellationToken);
        return Ok(CombatSkillResponseMapper.Map(result));
    }

    [HttpGet]
    [ProducesResponseType<CombatSkillSearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CombatSkillSearchResponse>> Search(
        [FromQuery] string? query = null,
        [FromQuery] CatalogueLanguage language = CatalogueLanguage.English,
        [FromQuery] CombatSkillSearchSort sort =
            CombatSkillSearchSort.DisplayName,
        [FromQuery] CombatSkillDiscipline? category = null,
        [FromQuery] int? grade = null,
        [FromQuery] int? faction = null,
        [FromQuery] CombatSkillElement? element = null,
        [FromQuery] CombatSkillEquipmentType? equipmentType = null,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await new SearchCombatSkillDefinitions(
                    definitionSource,
                    repository)
                .ExecuteAsync(
                    new CombatSkillSearchRequest(
                        language,
                        query,
                        DefinitionFilter(
                            category,
                            grade,
                            faction,
                            element,
                            equipmentType),
                        offset,
                        limit,
                        sort),
                    cancellationToken);
            return Ok(CombatSkillResponseMapper.Map(result));
        }
        catch (ArgumentException)
        {
            return InvalidQuery("Invalid combat-skill search parameters.");
        }
    }

    [HttpGet("{skillId:int}")]
    [ProducesResponseType<CombatSkillDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CombatSkillDetailsResponse>> Details(
        [FromRoute] int skillId,
        [FromQuery] CatalogueLanguage language = CatalogueLanguage.English,
        [FromQuery] int? characterId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await new ReadCombatSkillDetails(
                    definitionSource,
                    repository,
                    progressReader)
                .ExecuteAsync(
                    new CombatSkillDetailsRequest(
                        skillId,
                        language,
                        characterId),
                    cancellationToken);
            return Ok(CombatSkillResponseMapper.Map(result));
        }
        catch (ArgumentException)
        {
            return InvalidQuery("Invalid combat-skill detail parameters.");
        }
    }

    [HttpPost("catalogue-cache/rebuild")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<CombatSkillCatalogueMaintenanceResponse>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<CombatSkillCatalogueMaintenanceResponse>>
        RebuildCatalogueCache(CancellationToken cancellationToken = default)
    {
        var result = await new EnsureCombatSkillCatalogue(
                definitionSource,
                repository)
            .ExecuteAsync(cancellationToken);
        return Ok(CombatSkillResponseMapper.Map(result));
    }

    [HttpPost("progress-cache/clear")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<CharacterProgressCacheMaintenanceResponse>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<CharacterProgressCacheMaintenanceResponse>>
        ClearProgressCache(CancellationToken cancellationToken = default)
    {
        var result = await new ClearCharacterCombatSkillProgressCache(
                progressCacheMaintenance)
            .ExecuteAsync(cancellationToken);
        return Ok(CombatSkillResponseMapper.Map(result));
    }

    internal static CombatSkillCatalogueFilter DefinitionFilter(
        CombatSkillDiscipline? category,
        int? grade,
        int? faction,
        CombatSkillElement? element,
        CombatSkillEquipmentType? equipmentType) => new(
        category,
        grade is null ? null : new CombatSkillGrade(grade.Value),
        faction is null ? null : new CombatSkillFactionId(faction.Value),
        element,
        equipmentType);

    private ObjectResult InvalidQuery(string detail) => Problem(
        detail: detail,
        statusCode: StatusCodes.Status400BadRequest);
}
