using Fin.Application.CreditCards.Dtos;
using Fin.Application.CreditCards.Enums;
using Fin.Application.CreditCards.Services;
using Fin.Domain.Global.Classes;
using Fin.Domain.CreditCards.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.CreditCards;

[Route("credit-cards")]
[Authorize]
public class CreditCardController(ICreditCardService service) : ControllerBase
{
    [HttpGet]
    public async Task<PagedOutput<CreditCardOutput>> GetList([FromQuery] CreditCardGetListInput input)
    {
        return await service.GetList(input);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CreditCardOutput>> Get([FromRoute] Guid id)
    {
        var category = await service.Get(id);
        return category != null ? Ok(category) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<CreditCardOutput>> Create([FromBody] CreditCardInput input)
    {
        var validationResult = await service.Create(input, autoSave: true);
        return validationResult.Success
            ? Created($"categories/{validationResult.Data?.Id}", validationResult.Data)
            : UnprocessableEntity(validationResult);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update([FromRoute] Guid id, [FromBody] CreditCardInput input)
    {
        var validationResult = await service.Update(id, input, autoSave: true);
        return validationResult.Success ? Ok() :
            validationResult.ErrorCode == CreditCardCreateOrUpdateErrorCode.CreditCardNotFound ? NotFound(validationResult) :
            UnprocessableEntity(validationResult);
    }

    [HttpPut("toggle-inactivated/{id:guid}")]
    public async Task<ActionResult> ToggleInactivated([FromRoute] Guid id)
    {
        var validationResult = await service.ToggleInactive(id, autoSave: true);
        return validationResult.Success ? Ok() :
            validationResult.ErrorCode == CreditCardToggleInactiveErrorCode.CreditCardNotFound ? NotFound(validationResult) :
            UnprocessableEntity(validationResult);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete([FromRoute] Guid id)
    {
        var validationResult = await service.Delete(id, autoSave: true);
        return validationResult.Success ? Ok() :
            validationResult.ErrorCode == CreditCardDeleteErrorCode.CreditCardNotFound ? NotFound(validationResult) :
            UnprocessableEntity(validationResult);
    }
}