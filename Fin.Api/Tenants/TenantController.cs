using Fin.Application.Tenants;
using Fin.Application.Tenants.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.Tenants;

[Route("tenants")]
[Authorize]
public class TenantController(ITenantService service): ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantOutput>> Get([FromRoute] Guid id)
    {
        var menu = await service.Get(id);
        return menu != null ? Ok(menu) : NotFound();   
    }
}