using Fin.Domain.Menus.Entities;
using Fin.Domain.Menus.Enums;

namespace Fin.Domain.Menus.Dtos;

public class MenuOutput
{
    public Guid Id { get; set; }
    public string FrontRoute { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public string Color { get; set; }
    public string KeyWords { get; set; }
    public bool OnlyForAdmin { get; set; }
    public MenuPosition Position { get; set; }

    public MenuOutput()
    {
    }

    public MenuOutput(Menu input)
    {
        Id = input.Id;
        FrontRoute = input.FrontRoute;
        Name = input.Name;
        Icon = input.Icon;
        Color = input.Color;
        KeyWords = input.KeyWords;
        OnlyForAdmin = input.OnlyForAdmin;
        Position = input.Position;
    }
}