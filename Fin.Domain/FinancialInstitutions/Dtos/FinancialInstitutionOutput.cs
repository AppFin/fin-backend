using Fin.Domain.FinancialInstitutions.Entities;
using Fin.Domain.FinancialInstitutions.Enums;

namespace Fin.Domain.FinancialInstitutions.Dtos;

public class FinancialInstitutionOutput
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public FinancialInstitutionType Type { get; set; }
    public string Icon { get; set; }
    public string Color { get; set; }
    public bool Inactive { get; set; }

    public FinancialInstitutionOutput()
    {
    }

    public FinancialInstitutionOutput(FinancialInstitution input)
    {
        Id = input.Id;
        Name = input.Name;
        Code = input.Code;
        Type = input.Type;
        Icon = input.Icon;
        Color = input.Color;
        Inactive = input.Inactive;
    }
}
