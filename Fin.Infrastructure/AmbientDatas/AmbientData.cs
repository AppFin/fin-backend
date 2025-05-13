using Fin.Infrastructure.AutoServices.Interfaces;

namespace Fin.Infrastructure.AmbientDatas;

public class AmbientData: IAmbientData, IAutoScoped
{
    public Guid? TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public string DisplayName { get; private set; }
    public bool IsAdmin { get; private set; }
    public bool IsLogged => UserId.HasValue && TenantId.HasValue;   

    public void SetData(Guid tenantId, Guid userId, string displayName, bool isAdmin)
    {
        TenantId = tenantId;
        UserId = userId;
        DisplayName = displayName;       
        IsAdmin = isAdmin;       
    }

    public void SetNotLogged()
    {
        TenantId = null;
        UserId = null;
        DisplayName = null;       
        IsAdmin = false; 
    }
}