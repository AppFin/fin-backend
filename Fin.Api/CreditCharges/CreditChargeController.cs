using Fin.Application.CreditCharges.Dtos;
using Fin.Application.CreditCharges.Enums;
using Fin.Application.CreditCharges.Services;
using Fin.Domain.Global.Classes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.CreditCharges;

[Route("credit-charges")]
[Authorize]
public class CreditChargeController(ICreditChargeService service) : ControllerBase
{
    [HttpGet]
    public async Task<PagedOutput<CreditChargeOutput>> GetList([FromQuery] CreditChargeGetListInput input)
    {
        return await service.GetList(input);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CreditChargeOutput>> Get([FromRoute] Guid id)
    {
        var charge = await service.Get(id);
        return charge != null ? Ok(charge) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<CreditChargeOutput>> Create([FromBody] CreditChargeInput input)
    {
        var validationResult = await service.Create(input, autoSave: true);
        return validationResult.Success
            ? Created($"credit-charges/{validationResult.Data?.Id}", validationResult.Data)
            : UnprocessableEntity(validationResult);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update([FromRoute] Guid id, [FromBody] CreditChargeInput input)
    {
        var validationResult = await service.Update(id, input, autoSave: true);
        return validationResult.Success ? Ok() :
            validationResult.ErrorCode == CreditChargeCreateOrUpdateErrorCode.CreditChargeNotFound ? NotFound(validationResult) :
            UnprocessableEntity(validationResult);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete([FromRoute] Guid id)
    {
        var validationResult = await service.Delete(id, autoSave: true);
        return validationResult.Success ? Ok() :
            validationResult.ErrorCode == CreditChargeDeleteErrorCode.CreditChargeNotFound ? NotFound(validationResult) :
            UnprocessableEntity(validationResult);
    }
}

