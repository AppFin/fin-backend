using Fin.Domain.Wallets.Entities;

namespace Fin.Domain.Wallets.Dtos;

public class WalletOutput(Wallet wallet)
{
    public Guid Id { get; set; } = wallet.Id;
    public string Name { get; set; } = wallet.Name;
    public string Color { get; set; } = wallet.Color;
    public string Icon { get; set; } = wallet.Icon;
    public bool Inactivated { get; set; } = wallet.Inactivated;
    public Guid? FinancialInstitutionId { get; set; } = wallet.FinancialInstitutionId;
    public decimal InitialBalance { get; set; } = wallet.InitialBalance;
    public decimal CurrentBalance { get; set; } = wallet.CurrentBalance;

    public WalletOutput(): this(new Wallet())
    {
    }
}