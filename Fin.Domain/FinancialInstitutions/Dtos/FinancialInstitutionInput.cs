using System.ComponentModel.DataAnnotations;
using Fin.Domain.FinancialInstitutions.Enums;

namespace Fin.Domain.FinancialInstitutions.Dtos;

public class FinancialInstitutionInput
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; }
    
    [Required]
    [RegularExpression(@"^\d{3}$", ErrorMessage = "Code must be exactly 3 digits")]
    public string Code { get; set; }
    
    [Required]
    public FinancialInstitutionType Type { get; set; }
    
    [MaxLength(50)]
    public string Icon { get; set; }
    
    public bool Active { get; set; } = true;
}
