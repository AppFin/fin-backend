using System;
using System.Collections.Generic;
using Fin.Domain.People.Dtos;

namespace Fin.Application.CreditCharges.Dtos;

public class CreditChargeInput
{
    public decimal Value { get; set; }
    public string Description { get; set; }
    public DateTime Date { get; set; }
    public int NumberOfInstallments { get; set; } = 1;
    public Guid CreditCardId { get; set; }
    public List<Guid> CreditChargeCategoriesIds { get; set; } = [];
    public List<CreditChargePersonInput> CreditChargePeople { get; set; } = [];
}


