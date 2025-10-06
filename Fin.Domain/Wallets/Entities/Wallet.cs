using Fin.Domain.Global.Interfaces;
using Fin.Domain.Wallets.Dtos;

namespace Fin.Domain.Wallets.Entities;

public class Wallet: IAuditedTenantEntity
{
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
    
    public string Name { get; private set; }
    public string Color { get; private set; }
    public string Icon { get; private set; }
    public bool Inactivated { get; private set; }
    
    public Guid? FinancialInstitutionId { get; private set; }
    
    public decimal InitialBalance { get; private set; }
    public decimal CurrentBalance { get; set; }

    public Wallet()
    {
    }

    public Wallet(WalletInput wallet)
    {
        Name = wallet.Name;
        Color = wallet.Color;
        Icon = wallet.Icon;
        FinancialInstitutionId = wallet.FinancialInstitutionId;
        InitialBalance = wallet.InitialBalance;
    }

    public void Update(WalletInput wallet)
    {
        Name = wallet.Name;
        Color = wallet.Color;
        Icon = wallet.Icon;
        FinancialInstitutionId = wallet.FinancialInstitutionId;
        InitialBalance = wallet.InitialBalance;
    }
    
    public void ToggleInactivated() => Inactivated = !Inactivated;
}