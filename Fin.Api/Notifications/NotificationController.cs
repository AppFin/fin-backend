using Fin.Application.Notifications.Services.CrudServices;
using Fin.Application.Notifications.Services.DeliveryServices;
using Fin.Domain.Global.Classes;
using Fin.Domain.Notifications.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.Notifications;

[Route("notifications")]
[Authorize]
public class NotificationController(
    INotificationService service,
    INotificationDeliveryService deliveryService
    ): ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<PagedOutput<NotificationOutput>> GetList([FromQuery] PagedFilteredAndSortedInput input)
    {
        return await service.GetList(input);
    }
    
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<NotificationOutput>> Get([FromRoute] Guid id)
    {
        var notification = await service.Get(id);
        return notification != null ? Ok(notification) : NotFound();   
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<NotificationOutput>> Create([FromBody] NotificationInput input)
    {
        var notification = await service.Create(input, autoSave: true);
        return notification != null ? Created($"notifications/{notification.Id}", notification) : UnprocessableEntity();   
    }
    
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update([FromRoute] Guid id, [FromBody] NotificationInput input)
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

    [HttpPut("mark-visualized/{notificationId:guid}")]
    public async Task<ActionResult> MarkVisualized([FromRoute] Guid notificationId)
    {
        var marked = await deliveryService.MarkAsVisualized(notificationId, autoSave: true);
        return marked  ? Ok() : NotFound();
    }

    [HttpPut("get-unvisualized-notifications")]
    public async Task<ActionResult<List<NotifyUserDto>>> GetUnvisualizedNotification()
    {
        return Ok(await deliveryService.GetUnvisualizedNotifications(autoSave: true));
    }
}