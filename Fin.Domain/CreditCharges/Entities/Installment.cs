using Fin.Domain.Global.Interfaces;

namespace Fin.Domain.CreditCharges.Entities;

public class Installment: IAuditedTenantEntity
{
    public decimal Value { get; private set; }
    public DateTime DueDate { get; private set; }
    public byte Order { get; private set; } = 0;
    
    public Guid CreditChargeId { get; private set; }
    public CreditCharge CreditCharge { get; set; }
    
    public Guid CardBillingId { get; private set; }
    public CardBilling CardBilling { get; set; }
    
    public Installment()
    {
    }
    
    public Installment(decimal value, DateTime dueDate, byte order, Guid creditChargeId)
    {
        Value = value;
        DueDate = dueDate;
        Order = order;
        CreditChargeId = creditChargeId;
    }
    
    public void SetCardBillingId(Guid cardBillingId)
    {
        CardBillingId = cardBillingId;
    }
    
    
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
}