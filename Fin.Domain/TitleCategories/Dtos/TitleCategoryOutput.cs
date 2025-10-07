using Fin.Domain.TitleCategories.Entities;
using Fin.Domain.TitleCategories.Enums;

namespace Fin.Domain.TitleCategories.Dtos;

public class TitleCategoryOutput(TitleCategory titleCategory)
{
    public Guid Id { get; set; } = titleCategory.Id;
    public bool Inactivated { get; set; } = titleCategory.Inactivated;
    public string Name { get; set; } = titleCategory.Name;
    public string Color { get; set; } = titleCategory.Color;
    public string Icon { get; set; } = titleCategory.Icon;
    public TitleCategoryType Type { get; set; } = titleCategory.Type;
    
    public TitleCategoryOutput(): this(new TitleCategory())
    {
    }

}