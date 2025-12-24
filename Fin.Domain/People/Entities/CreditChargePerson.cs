using Fin.Domain.CreditCharges.Entities;
using Fin.Domain.Global.Interfaces;
using Fin.Domain.People.Dtos;

namespace Fin.Domain.People.Entities;

public class CreditChargePerson: ITenant, IAudited, ILoggable
{
    public Guid PersonId { get; private set; }
    public virtual Person Person { get; set; }
    
    public Guid CreditChargeId { get; private  set; }
    public virtual CreditCharge CreditCharge { get; set; }
    
    public decimal Percentage {get; private  set;}

    public CreditChargePerson()
    {
    }
    
    public CreditChargePerson(Guid creditChargeId, CreditChargePersonInput creditChargePerson)
    {
        CreditChargeId = creditChargeId;
        PersonId = creditChargePerson.PersonId;
        Percentage = creditChargePerson.Percentage;
    }

    public void Update(decimal percentage)
    {
        Percentage = percentage;
    }
    
    public Guid TenantId { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public object GetLog()
    {
        return new
        {
            CreatedAt,
            CreatedBy,
            UpdatedAt,
            UpdatedBy,
            TenantId,
            PersonId,
            CreditChargeId,
            Percentage
        };
    }
}