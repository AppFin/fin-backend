using Fin.Domain.Global.Interfaces;
using Fin.Domain.People.Dtos;
using Fin.Domain.People.Entities;
using Fin.Domain.TitleCategories;

namespace Fin.Domain.CreditCharges.Entities;

public partial  class CreditCharge: ILoggable
{
    public  List<CreditChargeCategory> SyncCategoriesAndReturnToRemove(List<Guid> newCategoryIds)
    {
        var updatedCategories = newCategoryIds.Select(userId => new CreditChargeCategory(userId, Id)).ToList();
        
        var categoriesToDelete = new List<CreditChargeCategory>();
        foreach (var currentDelivery in CreditChargeCategories)
        {
            var index = updatedCategories.FindIndex(c => c.TitleCategoryId == currentDelivery.TitleCategoryId);
            if (index != -1) continue;
            categoriesToDelete.Add(currentDelivery);
        }

        foreach (var currentDelivery in categoriesToDelete)
        {
            CreditChargeCategories.Remove(currentDelivery);
        }

        foreach (var updatedDelivery in updatedCategories)
        {
            var index = CreditChargeCategories.ToList().FindIndex(c => c.TitleCategoryId == updatedDelivery.TitleCategoryId);
            if (index != -1) continue;
            CreditChargeCategories.Add(updatedDelivery);
        }

        return categoriesToDelete;
    }
    
    public  List<CreditChargePerson> SyncPeopleAndReturnToRemove(List<CreditChargePersonInput> creditChargePersonInputs)
    {
        var updatedPeople = creditChargePersonInputs.Select(creditChargePerson => new CreditChargePerson(Id, creditChargePerson)).ToList();
        
        var creditChargePeopleToDelete = new List<CreditChargePerson>();
        foreach (var currentPerson in CreditChargePeople)
        {
            var index = updatedPeople.FindIndex(c => c.PersonId == currentPerson.PersonId);
            if (index != -1)
            {
                currentPerson.Update(updatedPeople[index].Percentage);
                continue;
            }
            creditChargePeopleToDelete.Add(currentPerson);
        }

        foreach (var currentDelivery in creditChargePeopleToDelete)
        {
            CreditChargePeople.Remove(currentDelivery);
        }

        foreach (var updatePerson in updatedPeople)
        {
            var index = CreditChargePeople.ToList().FindIndex(c => c.PersonId == updatePerson.PersonId);
            if (index != -1) continue;
            CreditChargePeople.Add(updatePerson);
        }

        return creditChargePeopleToDelete;
    }
    
    public object GetLog()
    {
        return new
        {
            Id = Id,
            Date = Date,
            Description = Description,
            
            Value = Value,
            
            CreditCardId = CreditCardId,

            TenantId = TenantId,
            CreatedBy = CreatedBy,
            CreatedAt = CreatedAt,
            
            UpdatedBy = UpdatedBy,
            UpdatedAt = UpdatedAt
        };
    }
}