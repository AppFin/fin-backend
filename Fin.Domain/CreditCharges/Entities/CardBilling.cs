using Fin.Domain.CreditCards.Entities;
using Fin.Domain.Global.Interfaces;
using Fin.Domain.Titles.Entities;

namespace Fin.Domain.CreditCharges.Entities;

public class CardBilling: IAuditedTenantEntity
{
    public decimal Value { get; private set; }
    
    public Guid CreditCardId { get; private set; }
    public virtual CreditCard CreditCard { get; set; }
    
    public Guid PaymentTitleId { get; private set; }
    public virtual Title PaymentTitle { get; set; }
    
    public DateTime PaymentDate { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    
    public ICollection<Installment> Installments { get; set; }
    
    public CardBilling()
    {
    }
    
    public CardBilling(decimal value, Guid creditCardId, Guid paymentTitleId, DateTime paymentDate, DateOnly periodStart, DateOnly periodEnd)
    {
        Value = value;
        CreditCardId = creditCardId;
        PaymentTitleId = paymentTitleId;
        PaymentDate = paymentDate;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
    }
    
    public void UpdateValue(decimal value)
    {
        Value = value;
    }
    
    public void UpdatePaymentTitle(Guid paymentTitleId)
    {
        PaymentTitleId = paymentTitleId;
    }
    
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
}