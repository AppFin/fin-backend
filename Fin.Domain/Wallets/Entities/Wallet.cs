using Fin.Domain.CreditCards.Entities;
using Fin.Domain.FinancialInstitutions.Entities;
using Fin.Domain.Global.Interfaces;
using Fin.Domain.Titles.Entities;
using Fin.Domain.Titles.Extensions;
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
    public virtual FinancialInstitution FinancialInstitution { get; set; }
    
    public decimal InitialBalance { get; private set; }


    public virtual ICollection<CreditCard> CreditCards { get; set; } = [];
    public virtual ICollection<Title> Titles { get; set; } = [];

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

    public decimal CalculateBalanceAt(DateTime dateTime)
    {
        if (dateTime < CreatedAt) return 0;
        if (Titles.Count == 0 ) return InitialBalance;
        
        var lastTitle = Titles
            .Where(title => title.Date <= dateTime)
            .ApplyDefaultTitleOrder()
            .FirstOrDefault();

        return lastTitle?.ResultingBalance ?? InitialBalance;
    }
}