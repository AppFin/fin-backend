using System.ComponentModel.DataAnnotations;
using Fin.Domain.FinancialInstitutions.Enums;

namespace Fin.Domain.FinancialInstitutions.Dtos;

public class FinancialInstitutionInput
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
    
    [MaxLength(15)]
    public string Code { get; set; }
    
    [Required]
    public FinancialInstitutionType Type { get; set; }

    [MaxLength(20)]
    [Required]
    public string Icon { get; set; }
    
    [MaxLength(20)]
    [Required]
    public string Color { get; set; }
    }
