using Fin.Domain.TitleCategories.Entities;
using Fin.Domain.TitleCategories.Enums;

namespace Fin.Domain.TitleCategories.Dtos;

public class TitleCategoryOutput(TitleCategory titleCategory)
{
    public Guid Id { get; } = titleCategory.Id;
    public bool Inactivated { get; } = titleCategory.Inactivated;
    public string Name { get; } = titleCategory.Name;
    public string Color { get; } = titleCategory.Color;
    public string Icon { get; } = titleCategory.Icon;
    public TitleCategoryType Type { get; } = titleCategory.Type;
}