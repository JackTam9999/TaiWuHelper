using Microsoft.AspNetCore.Mvc;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWuAPI.Contracts.CombatSkills;

namespace TaiWuAPI.Controllers;

[ApiController]
[Route("api/character-skill-atlas")]
public sealed class CharacterSkillAtlasController(
    ReadCharacterCombatSkillAtlas readAtlas) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CharacterCombatSkillAtlasResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CharacterCombatSkillAtlasResponse>> Read(
        [FromQuery] int? characterId = null,
        [FromQuery] CatalogueLanguage language = CatalogueLanguage.English,
        [FromQuery] string? query = null,
        [FromQuery] CombatSkillDiscipline? category = null,
        [FromQuery] int? grade = null,
        [FromQuery] int? faction = null,
        [FromQuery] CombatSkillElement? element = null,
        [FromQuery] CombatSkillEquipmentType? equipmentType = null,
        [FromQuery] bool? learned = null,
        [FromQuery] bool? hasProficiency = null,
        [FromQuery] bool? studyComplete = null,
        [FromQuery] bool? breakthroughReady = null,
        [FromQuery] bool? brokenThrough = null,
        [FromQuery] PracticeDirection? activeDirection = null,
        [FromQuery] bool? attainmentMastered = null,
        [FromQuery] bool? simplified = null,
        [FromQuery] bool? activated = null,
        [FromQuery] bool? equipped = null,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await readAtlas.ExecuteAsync(
                    new CharacterCombatSkillAtlasRequest(
                        characterId,
                        language,
                        query,
                        CombatSkillsController.DefinitionFilter(
                            category,
                            grade,
                            faction,
                            element,
                            equipmentType),
                        new CharacterCombatSkillProgressFilter(
                            learned,
                            hasProficiency,
                            studyComplete,
                            breakthroughReady,
                            brokenThrough,
                            activeDirection,
                            attainmentMastered,
                            simplified,
                            activated,
                            equipped),
                        offset,
                        limit),
                    cancellationToken);
            return Ok(CombatSkillResponseMapper.Map(result));
        }
        catch (ArgumentException)
        {
            return Problem(
                detail: "Invalid character skill-atlas query parameters.",
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
