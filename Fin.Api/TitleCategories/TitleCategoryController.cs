using Fin.Application.TitleCategories;
using Fin.Application.TitleCategories.Dtos;
using Fin.Domain.Global.Classes;
using Fin.Domain.TitleCategories.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.TitleCategories;

[Route("title-categories")]
[Authorize]
public class TitleCategoryController(ITitleCategoryService service): ControllerBase
{
    [HttpGet]
    public async Task<PagedOutput<TitleCategoryOutput>> GetList([FromQuery] TitleCategoryGetListInput input)
    {
        return await service.GetList(input);
    }
    
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TitleCategoryOutput>> Get([FromRoute] Guid id)
    {
        var category = await service.Get(id);
        return category != null ? Ok(category) : NotFound();   
    }
    
    [HttpPost]
    public async Task<ActionResult<TitleCategoryOutput>> Create([FromBody] TitleCategoryInput input)
    {
        var validationResult = await service.Create(input, autoSave: true);
        return validationResult.Success ? Created($"categories/{validationResult.Data?.Id}",validationResult.Data) : UnprocessableEntity(validationResult);   
    }
    
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update([FromRoute] Guid id, [FromBody] TitleCategoryInput input)
    {
        var validationResult = await service.Update(id, input, autoSave: true);
        return validationResult.Success  ? Ok() : UnprocessableEntity(validationResult);   
    }
    
    [HttpPut("toggle-inactivated/{id:guid}")]
    public async Task<ActionResult> ToggleInactivated([FromRoute] Guid id)
    {
        var updated = await service.ToggleInactive(id, autoSave: true);
        return updated  ? Ok() : UnprocessableEntity();   
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete([FromRoute] Guid id)
    {
        var deleted = await service.Delete(id, autoSave: true);
        return deleted  ? Ok() : UnprocessableEntity();   
    }
}