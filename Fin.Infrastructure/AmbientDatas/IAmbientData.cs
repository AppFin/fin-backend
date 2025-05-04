namespace Fin.Infrastructure.AmbientDatas;

public interface IAmbientData
{
    public Guid? TenantId { get; }
    public Guid? UserId { get; }
    public string? DisplayName { get; }
    public bool IsAdmin { get; }
    public bool IsLogged { get; }

    public void SetData(Guid tenantId, Guid userId, string displayName, bool isAdmin);
}