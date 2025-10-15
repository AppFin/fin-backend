using Fin.Application.CardBrands;
using Fin.Domain.Global.Classes;
using Fin.Domain.CardBrands.Dtos;
using Fin.Infrastructure.Authentications.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.CardBrands;

[Route("card-brand")]
[Authorize]
public class CardBrandController(ICardBrandService service): ControllerBase
{
    [HttpGet]
    public async Task<PagedOutput<CardBrandOutput>> GetList([FromQuery] PagedFilteredAndSortedInput input)
    {
        return await service.GetList(input);
    }
    
    
    [HttpGet("{id:guid}")]
    [Authorize(Roles = AuthenticationRoles.Admin)]
    public async Task<ActionResult<CardBrandOutput>> Get([FromRoute] Guid id)
    {
        var menu = await service.Get(id);
        return menu != null ? Ok(menu) : NotFound();   
    }
    
    [HttpPost]
    [Authorize(Roles = AuthenticationRoles.Admin)]
    public async Task<ActionResult<CardBrandOutput>> Create([FromBody] CardBrandInput input)
    {
        var menu = await service.Create(input, autoSave: true);
        return menu != null ? Created($"card-brand/{menu.Id}", menu) : UnprocessableEntity();   
    }
    
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AuthenticationRoles.Admin)]
    public async Task<ActionResult> Update([FromRoute] Guid id, [FromBody] CardBrandInput input)
    {
        var updated = await service.Update(id, input, autoSave: true);
        return updated  ? Ok() : UnprocessableEntity();   
    }
    
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AuthenticationRoles.Admin)]
    public async Task<ActionResult> Delete([FromRoute] Guid id)
    {
        var deleted = await service.Delete(id, autoSave: true);
        return deleted  ? Ok() : UnprocessableEntity();   
    }
}