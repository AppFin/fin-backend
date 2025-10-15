using Fin.Application.Wallets.Dtos;
using Fin.Application.Wallets.Enums;
using Fin.Application.Wallets.Services;
using Fin.Domain.Global.Classes;
using Fin.Domain.Wallets.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.Wallets;

[Route("wallets")]
[Authorize]
public class WalletController(IWalletService service) : ControllerBase
{
    [HttpGet]
    public async Task<PagedOutput<WalletOutput>> GetList([FromQuery] WalletGetListInput input)
    {
        return await service.GetList(input);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WalletOutput>> Get([FromRoute] Guid id)
    {
        var category = await service.Get(id);
        return category != null ? Ok(category) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<WalletOutput>> Create([FromBody] WalletInput input)
    {
        var validationResult = await service.Create(input, autoSave: true);
        return validationResult.Success
            ? Created($"categories/{validationResult.Data?.Id}", validationResult.Data)
            : UnprocessableEntity(validationResult);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update([FromRoute] Guid id, [FromBody] WalletInput input)
    {
        var validationResult = await service.Update(id, input, autoSave: true);
        return validationResult.Success ? Ok() :
            validationResult.ErrorCode == WalletCreateOrUpdateErrorCode.WalletNotFound ? NotFound(validationResult) :
            UnprocessableEntity(validationResult);
    }

    [HttpPut("toggle-inactivated/{id:guid}")]
    public async Task<ActionResult> ToggleInactivated([FromRoute] Guid id)
    {
        var validationResult = await service.ToggleInactive(id, autoSave: true);
        return validationResult.Success ? Ok() :
            validationResult.ErrorCode == WalletToggleInactiveErrorCode.WalletNotFound ? NotFound(validationResult) :
            UnprocessableEntity(validationResult);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete([FromRoute] Guid id)
    {
        var validationResult = await service.Delete(id, autoSave: true);
        return validationResult.Success ? Ok() :
            validationResult.ErrorCode == WalletDeleteErrorCode.WalletNotFound ? NotFound(validationResult) :
            UnprocessableEntity(validationResult);
    }
}