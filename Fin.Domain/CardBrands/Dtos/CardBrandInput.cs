using System.ComponentModel.DataAnnotations;
using Fin.Domain.Menus.Enums;

namespace Fin.Domain.CardBrands.Dtos;

public class CardBrandInput
{
    [Required]
    public string Name { get; set; }
    
    [Required]
    public string Icon { get; set; }
    public string Color { get; set; }

}