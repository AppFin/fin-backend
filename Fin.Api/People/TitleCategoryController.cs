using Fin.Application.People;
using Fin.Application.People.Dtos;
using Fin.Application.People.Enums;
using Fin.Domain.Global.Classes;
using Fin.Domain.People.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.People;

[Route("people")]
[Authorize]
public class PersonController(IPersonService service) : ControllerBase
{
    [HttpGet]
    public async Task<PagedOutput<PersonOutput>> GetList([FromQuery] PersonGetListInput input)
    {
        return await service.GetList(input);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PersonOutput>> Get([FromRoute] Guid id)
    {
        var category = await service.Get(id);
        return category != null ? Ok(category) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<PersonOutput>> Create([FromBody] PersonInput input)
    {
        var validationResult = await service.Create(input, autoSave: true);
        return validationResult.Success
            ? Created($"categories/{validationResult.Data?.Id}", validationResult.Data)
            : UnprocessableEntity(validationResult);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update([FromRoute] Guid id, [FromBody] PersonInput input)
    {
        var validationResult = await service.Update(id, input, autoSave: true);
        return validationResult.Success
            ? Ok()
            : validationResult.ErrorCode == PersonCreateOrUpdateErrorCode.PersonNotFound
                ? NotFound(validationResult)
                : UnprocessableEntity(validationResult);
    }

    [HttpPut("toggle-inactivated/{id:guid}")]
    public async Task<ActionResult> ToggleInactivated([FromRoute] Guid id)
    {
        var updated = await service.ToggleInactive(id, autoSave: true);
        return updated ? Ok() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete([FromRoute] Guid id)
    {
        var validation = await service.Delete(id, autoSave: true);
        return validation.Success ? NoContent() : validation.ErrorCode == PersonDeleteErrorCode.PersonNotFound ? NotFound(validation):  UnprocessableEntity(validation);
    }
}