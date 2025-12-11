using Fin.Domain.CreditCards.Entities;
using Fin.Domain.FinancialInstitutions.Dtos;
using Fin.Domain.FinancialInstitutions.Enums;
using Fin.Domain.Global.Decorators;
using Fin.Domain.Global.Interfaces;
using Fin.Domain.Wallets.Entities;

namespace Fin.Domain.FinancialInstitutions.Entities;

public class FinancialInstitution : IAuditedEntity, ILoggable
{
    public string Name { get; set; }
    public string Code { get; set; }
    public FinancialInstitutionType Type { get; set; }
    public string Icon { get; set; }
    public string Color { get; set; }
    public bool Inactive { get; set; }

    public virtual ICollection<Wallet> Wallets { get; set; }
    public virtual ICollection<CreditCard> CreditCards { get; set; }
    
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public FinancialInstitution()
    {
    }

    public FinancialInstitution(FinancialInstitutionInput input)
    {
        Name = input.Name;
        Code = input.Code;
        Type = input.Type;
        Icon = input.Icon;
        Color = input.Color;
    }

    public void Update(FinancialInstitutionInput input)
    {
        Name = input.Name;
        Code = input.Code;
        Type = input.Type;
        Icon = input.Icon;
        Color = input.Color;
    }

   public void ToggleInactive() => Inactive = !Inactive;
   public object GetLog()
   {
       return new
       {
           Id,
           CreatedAt,
           CreatedBy,
           UpdatedAt,
           UpdatedBy,
           Code,
           Icon,
           Color,
           Inactive,
           Type,
           TypeDescription = Type.GetTranslateKey(),
       };
   }
}
