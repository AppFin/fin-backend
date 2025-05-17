using Fin.Application.Notifications;
using Fin.Domain.Global.Classes;
using Fin.Domain.Notifications.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.Notifications;

[Route("notifications")]
[Authorize]
public class NotificationController(INotificationService service): ControllerBase
{
    [HttpGet]
    public async Task<PagedOutput<NotificationOutput>> GetList([FromQuery] PagedFilteredAndSortedInput input)
    {
        return await service.GetList(input);
    }
    
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NotificationOutput>> Get([FromRoute] Guid id)
    {
        var notification = await service.Get(id);
        return notification != null ? Ok(notification) : NotFound();   
    }
    
    [HttpPost]
    public async Task<ActionResult<NotificationOutput>> Create([FromBody] NotificationInput input)
    {
        var notification = await service.Create(input, true);
        return notification != null ? Created($"notifications/{notification.Id}", notification) : UnprocessableEntity();   
    }
    
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update([FromRoute] Guid id, [FromBody] NotificationInput input)
    {
        var updated = await service.Update(id, input, true);
        return updated  ? Ok() : UnprocessableEntity();   
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete([FromRoute] Guid id)
    {
        var deleted = await service.Delete(id, true);
        return deleted  ? Ok() : UnprocessableEntity();   
    }
}