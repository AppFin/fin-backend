using System.ComponentModel.DataAnnotations;

namespace Fin.Domain.Wallets.Dtos;

public class WalletInput
{
    [Required]
    public string Name { get; set; }
    
    [Required]
    public string Color { get; set; }
    
    [Required]
    public string Icon { get; set; }
    
    public Guid? FinancialInstitutionId { get; set; }
    
    [Required]
    public decimal InitialBalance { get; set; }
}