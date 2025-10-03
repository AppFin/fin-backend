using Fin.Domain.FinancialInstitutions.Dtos;
using Fin.Domain.FinancialInstitutions.Enums;
using Fin.Domain.Global.Interfaces;

namespace Fin.Domain.FinancialInstitutions.Entities;

public class FinancialInstitution : IAuditedEntity, ITenantEntity
{
    public string Name { get; set; }
    public string Code { get; set; }
    public FinancialInstitutionType Type { get; set; }
    public string Icon { get; set; }
    public bool Active { get; set; }

    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid TenantId { get; set; }

    public FinancialInstitution()
    {
    }

    public FinancialInstitution(FinancialInstitutionInput input)
    {
        Name = input.Name;
        Code = input.Code;
        Type = input.Type;
        Icon = input.Icon;
        Active = input.Active;
    }

    public void Update(FinancialInstitutionInput input)
    {
        Name = input.Name;
        Code = input.Code;
        Type = input.Type;
        Icon = input.Icon;
        Active = input.Active;
    }

    public void Activate() => Active = true;
    
    public void Deactivate() => Active = false;
}
