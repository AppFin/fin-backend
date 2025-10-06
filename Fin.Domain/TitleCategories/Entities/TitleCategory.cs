using Fin.Domain.Global.Interfaces;
using Fin.Domain.TitleCategories.Dtos;
using Fin.Domain.TitleCategories.Enums;

namespace Fin.Domain.TitleCategories.Entities;

public class TitleCategory: IAuditedTenantEntity
{
    public bool Inactivated { get; private set; }
    public string Name { get; private set; }
    public string Color { get; private set; }
    public string Icon { get; private set; }
    public TitleCategoryType Type { get; private set; }
    
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
    
    public TitleCategory()
    {
    }
    public TitleCategory(TitleCategoryInput input)
    {
        Name = input.Name;
        Color = input.Color;
        Icon = input.Icon;
        Type = input.Type;
    }
    
    public void Update(TitleCategoryInput input)
    {
        Name = input.Name;
        Color = input.Color;
        Icon = input.Icon;
        Type = input.Type;
    }
    
    public void ToggleInactivated() => Inactivated = !Inactivated;
}