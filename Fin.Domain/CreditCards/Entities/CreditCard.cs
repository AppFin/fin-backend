using Fin.Domain.CardBrands.Entities;
using Fin.Domain.CreditCards.Dtos;
using Fin.Domain.FinancialInstitutions.Entities;
using Fin.Domain.Global.Interfaces;
using Fin.Domain.Wallets.Entities;

namespace Fin.Domain.CreditCards.Entities;

public class CreditCard: IAuditedTenantEntity
{
    public string Name { get; private set; }
    public string Color { get; private set; }
    public string Icon { get; private set; }
    public decimal Limit { get; private set; }
    public int DueDay { get; private set; }
    public int ClosingDay { get; private set; }
    public bool Inactivated { get; private set; }
    
    public Guid DebitWalletId { get; private set; }
    public virtual  Wallet DebitWallet { get; set; }
    
    public Guid CardBrandId { get; private set; }
    public virtual CardBrand CardBrand { get; set; }
    
    public Guid FinancialInstitutionId { get; set; }
    public virtual FinancialInstitution FinancialInstitution { get; set; }
    
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
    
    public CreditCard()
    {
    }

    public CreditCard(CreditCardInput input)
    {
        Name = input.Name;
        Color = input.Color;
        Icon = input.Icon;
        Limit = input.Limit;
        DueDay = input.DueDay;
        ClosingDay = input.ClosingDay;
        DebitWalletId = input.DebitWalletId;
        CardBrandId = input.CardBrandId;
        FinancialInstitutionId = input.FinancialInstitutionId;
    }
    
    public void Update(CreditCardInput input)
    {
        Name = input.Name;
        Color = input.Color;
        Icon = input.Icon;
        Limit = input.Limit;
        DueDay = input.DueDay;
        ClosingDay = input.ClosingDay;
        DebitWalletId = input.DebitWalletId;
        CardBrandId = input.CardBrandId;
        FinancialInstitutionId = input.FinancialInstitutionId;
    }
    
    public void ToggleInactivated() => Inactivated = !Inactivated;
}