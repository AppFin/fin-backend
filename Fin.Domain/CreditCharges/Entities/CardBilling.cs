using Fin.Domain.CreditCards.Entities;
using Fin.Domain.Global.Interfaces;
using Fin.Domain.Titles.Entities;

namespace Fin.Domain.CreditCharges.Entities;

public class CardBilling: IAuditedTenantEntity
{
    public decimal Value { get; set; }
    
    public Guid CreditCardId { get; set; }
    public virtual CreditCard CreditCard { get; set; }
    
    public Guid PaymentTitleId { get; set; }
    public virtual Title PaymentTitle { get; set; }
    
    public DateTime PaymentDate { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    
    public ICollection<Installment> Installments { get; set; }
    
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
}