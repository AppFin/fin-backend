using Fin.Domain.Global.Interfaces;
using Fin.Domain.TitleCategories.Entities;
using Fin.Domain.Titles.Enums;
using Fin.Domain.Wallets.Entities;

namespace Fin.Domain.Titles.Entities;

public class Title: IAuditedTenantEntity
{
    public decimal Value { get; set; }
    public TitleType Type { get; set; }
    
    public string Description { get; set; }
    public decimal PreviousBalance { get; set; }
    public DateTime Date { get; set; }
    public Guid WalletId { get; set; }
    
    
    public decimal ResultingBalance => PreviousBalance + (Value * (Type == TitleType.Expense ? -1 : 1));
    
    public virtual Wallet Wallet { get; set; }
    public virtual ICollection<TitleCategory> TitleCategories { get; set; }
    
    
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
}