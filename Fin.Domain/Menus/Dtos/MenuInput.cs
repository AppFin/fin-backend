using System.ComponentModel.DataAnnotations;
using Fin.Domain.Menus.Enums;

namespace Fin.Domain.Menus.Dtos;

public class MenuInput
{
    [Required]
    public string FrontRoute { get; set; }
    
    [Required]
    public string Name { get; set; }
    
    [Required]
    public string Icon { get; set; }
    public string Color { get; set; }
    public string KeyWords { get; set; }
    public bool OnlyForAdmin { get; set; }
    public MenuPosition Position { get; set; }
}