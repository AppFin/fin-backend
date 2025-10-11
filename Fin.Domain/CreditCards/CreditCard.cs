using Fin.Domain.FinancialInstitutions.Entities;
using Fin.Domain.Global.Interfaces;
using Fin.Domain.Wallets.Entities;

namespace Fin.Domain.CreditCards;

public class CreditCard: IAuditedTenantEntity
{
    public string Name { get; set; }
    public string Color { get; set; }
    public string Icon { get; set; }
    public decimal Limit { get; set; }
    public int DueDay { get; set; }
    public int ClossingDay { get; set; }
    public bool Inactivated { get; set; }
    
    public Guid DebitWalletId { get; set; }
    public virtual  Wallet DebitWallet { get; set; }
    
    public virtual  Guid FlagId { get; set; }
    // public Flag Flag { get; set; }
    
    public Guid FinancialInstitutionId { get; set; }
    public virtual FinancialInstitution FinancialInstitution { get; set; }
    
    
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
}