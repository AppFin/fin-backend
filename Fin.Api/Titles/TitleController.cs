using Fin.Application.Titles.Dtos;
using Fin.Application.Titles.Enums;
using Fin.Application.Titles.Services;
using Fin.Domain.Global.Classes;
using Fin.Domain.Titles.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.Titles;

[Route("titles")]
[Authorize]
public class TitleController(ITitleService service) : ControllerBase
{
    [HttpGet]
    public async Task<PagedOutput<TitleOutput>> GetList([FromQuery] TitleGetListInput input)
    {
        return await service.GetList(input);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TitleOutput>> Get([FromRoute] Guid id)
    {
        var category = await service.Get(id);
        return category != null ? Ok(category) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<TitleOutput>> Create([FromBody] TitleInput input)
    {
        var validationResult = await service.Create(input, autoSave: true);
        return validationResult.Success
            ? Created($"categories/{validationResult.Data?.Id}", validationResult.Data)
            : UnprocessableEntity(validationResult);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update([FromRoute] Guid id, [FromBody] TitleInput input)
    {
        var validationResult = await service.Update(id, input, autoSave: true);
        return validationResult.Success ? Ok() :
            validationResult.ErrorCode == TitleCreateOrUpdateErrorCode.TitleNotFound ? NotFound(validationResult) :
            UnprocessableEntity(validationResult);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete([FromRoute] Guid id)
    {
        var validationResult = await service.Delete(id, autoSave: true);
        return validationResult.Success ? Ok() :
            validationResult.ErrorCode == TitleDeleteErrorCode.TitleNotFound ? NotFound(validationResult) :
            UnprocessableEntity(validationResult);
    }
}