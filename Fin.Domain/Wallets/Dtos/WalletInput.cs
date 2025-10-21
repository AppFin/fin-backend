using System.ComponentModel.DataAnnotations;

namespace Fin.Domain.Wallets.Dtos;

public class WalletInput
{
    public string Name { get; set; }
    public string Color { get; set; }
    public string Icon { get; set; }
    public Guid? FinancialInstitutionId { get; set; }
    public decimal InitialBalance { get; set; }
}