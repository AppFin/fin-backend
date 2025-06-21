using Fin.Domain.Global.Interfaces;
using Fin.Domain.Menus.Dtos;
using Fin.Domain.Menus.Enums;

namespace Fin.Domain.Menus.Entities;

public class Menu: IAuditedEntity
{
    public string FrontRoute { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public string Color { get; set; }
    public string KeyWords { get; set; }
    public bool OnlyForAdmin { get; set; }
    public MenuPosition Position { get; set; }

    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Menu()
    {
    }

    public Menu(MenuInput input)
    {
        FrontRoute = input.FrontRoute;
        Name = input.Name;
        Icon = input.Icon;
        Color = input.Color;
        KeyWords = input.KeyWords;
        OnlyForAdmin = input.OnlyForAdmin;
        Position = input.Position;
    }

    public void Update(MenuInput input)
    {
        FrontRoute = input.FrontRoute;
        Name = input.Name;
        Icon = input.Icon;
        Color = input.Color;
        KeyWords = input.KeyWords;
        OnlyForAdmin = input.OnlyForAdmin;
        Position = input.Position;
    }
}
