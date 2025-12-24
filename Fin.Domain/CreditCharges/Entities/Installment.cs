using Fin.Domain.Global.Interfaces;

namespace Fin.Domain.CreditCharges.Entities;

public class Installment: IAuditedTenantEntity
{
    public decimal Value { get; set; }
    public DateTime DueDate { get; set; }
    public byte Order { get; set; } = 0;
    
    public Guid CreditChargeId { get; set; }
    public CreditCharge CreditCharge { get; set; }
    
    public Guid CardBillingId { get; set; }
    public CardBilling CardBilling { get; set; }
    
    
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
}