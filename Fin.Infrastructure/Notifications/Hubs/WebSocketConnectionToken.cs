namespace Fin.Infrastructure.Notifications.Hubs;

public class WebSocketConnectionToken
{
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    
    public Guid UserId { get; set; }
    public Guid TenantId { get;  set; }
    public string DisplayName { get;  set; }
    public bool IsAdmin { get;  set; }
}