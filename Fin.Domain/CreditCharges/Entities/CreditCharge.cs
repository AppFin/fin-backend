using Fin.Domain.CreditCards.Entities;
using Fin.Domain.Global.Interfaces;
using Fin.Domain.People.Entities;
using Fin.Domain.TitleCategories;
using Fin.Domain.TitleCategories.Entities;

namespace Fin.Domain.CreditCharges.Entities;

public partial class CreditCharge: IAuditedTenantEntity
{
    public decimal Value { get; set; }
    public string Description { get; set; }
    public DateTime Date { get; set; }
    
    public Guid CreditCardId { get; set; }
    public virtual CreditCard CreditCard { get; set; }
    
    public ICollection<TitleCategory> TitleCategories { get; set; } = [];
    public ICollection<CreditChargeCategory> CreditChargeCategories { get; set; } = [];
    
    public ICollection<Person> People { get; set; } = [];
    public ICollection<CreditChargePerson> CreditChargePeople { get; set; } = [];
    
    public ICollection<Installment> Installments { get; set; }
    
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid TenantId { get; set; }
}