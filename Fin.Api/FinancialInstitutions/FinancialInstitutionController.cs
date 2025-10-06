using Fin.Application.FinancialInstitutions;
using Fin.Domain.FinancialInstitutions.Dtos;
using Fin.Domain.Global.Classes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.FinancialInstitutions;

[Route("financial-institutions")]
[Authorize]
public class FinancialInstitutionController(IFinancialInstitutionService service): ControllerBase
{
    [HttpGet]
    public async Task<PagedOutput<FinancialInstitutionOutput>> GetList([FromQuery] PagedFilteredAndSortedInput input)
    {
        return await service.GetList(input);
    }
    
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FinancialInstitutionOutput>> Get([FromRoute] Guid id)
    {
        var institution = await service.Get(id);
        return institution != null ? Ok(institution) : NotFound();   
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FinancialInstitutionOutput>> Create([FromBody] FinancialInstitutionInput input)
    {
        var institution = await service.Create(input, autoSave: true);
        return institution != null ? Created($"financial-institutions/{institution.Id}", institution) : UnprocessableEntity();
    }
    
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update([FromRoute] Guid id, [FromBody] FinancialInstitutionInput input)
    {
        var updated = await service.Update(id, input, autoSave: true);
        return updated ? Ok() : NotFound();
    }
    
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete([FromRoute] Guid id)
    {
        var deleted = await service.Delete(id, autoSave: true);
        return deleted ? Ok() : NotFound();   
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Activate([FromRoute] Guid id)
    {
        var activated = await service.Activate(id, autoSave: true);
        return activated ? Ok() : NotFound();
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Deactivate([FromRoute] Guid id)
    {
        var deactivated = await service.Deactivate(id, autoSave: true);
        return deactivated ? Ok() : NotFound();
    }
}
