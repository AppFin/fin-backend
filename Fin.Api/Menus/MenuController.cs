using Fin.Application.Menus;
using Fin.Domain.Global.Classes;
using Fin.Domain.Menus.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.Menus;

[Route("menus")]
[Authorize]
public class MenuController(IMenuService service): ControllerBase
{
    [HttpGet]
    public async Task<PagedOutput<MenuOutput>> GetList([FromQuery] PagedFilteredAndSortedInput input)
    {
        return await service.GetList(input);
    }
    
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MenuOutput>> Get([FromRoute] Guid id)
    {
        var menu = await service.Get(id);
        return menu != null ? Ok(menu) : NotFound();   
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MenuOutput>> Create([FromBody] MenuInput input)
    {
        var menu = await service.Create(input, autoSave: true);
        return menu != null ? Created($"menus/{menu.Id}", menu) : UnprocessableEntity();   
    }
    
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update([FromRoute] Guid id, [FromBody] MenuInput input)
    {
        var updated = await service.Update(id, input, autoSave: true);
        return updated  ? Ok() : UnprocessableEntity();   
    }
    
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete([FromRoute] Guid id)
    {
        var deleted = await service.Delete(id, autoSave: true);
        return deleted  ? Ok() : UnprocessableEntity();   
    }
}