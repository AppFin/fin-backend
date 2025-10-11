using System.ComponentModel.DataAnnotations;
using Fin.Domain.TitleCategories.Enums;

namespace Fin.Domain.TitleCategories.Dtos;

public class TitleCategoryInput
{
    [Required]
    public string Name { get; set; }
    
    [Required]
    public string Color { get; set; }
    [Required]
    
    public string Icon { get; set; }
    
    [Required]
    public TitleCategoryType Type { get; set; }
}