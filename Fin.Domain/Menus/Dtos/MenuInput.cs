using System.ComponentModel.DataAnnotations;
using Fin.Domain.Menus.Enums;
using Microsoft.AspNetCore.Http;

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

    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(FrontRoute))
            throw new BadHttpRequestException("FrontRoute is required");
        if (string.IsNullOrWhiteSpace(Name))
            throw new BadHttpRequestException("Name is required");
        if (string.IsNullOrWhiteSpace(Icon))
            throw new BadHttpRequestException("Icon is required");
    }
}