using System.ComponentModel.DataAnnotations;

namespace Fin.Domain.Wallets.Dtos;

public class WalletInput
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Color { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Icon { get; set; }
    
    public Guid? FinancialInstitutionId { get; set; }
    
    [Required]
    public decimal InitialBalance { get; set; }
}