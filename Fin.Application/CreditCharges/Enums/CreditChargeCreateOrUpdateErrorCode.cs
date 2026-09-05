using Fin.Infrastructure.Errors;

namespace Fin.Application.CreditCharges.Enums;

public enum CreditChargeCreateOrUpdateErrorCode
{
    [ErrorMessage("Credit charge not found")]
    CreditChargeNotFound = 0,
    
    [ErrorMessage("Description must have less than 100 characters.")]
    DescriptionTooLong = 1,
    
    [ErrorMessage("Description is required.")]
    DescriptionIsRequired = 2,
    
    [ErrorMessage("Credit card not found")]
    CreditCardNotFound = 3,
    
    [ErrorMessage("Credit card is inactive")]
    CreditCardInactive = 4,
    
    [ErrorMessage("Value must be greater than zero.")]
    ValueMustBeGreaterThanZero = 5,
    
    [ErrorMessage("Number of installments must be at least 1.")]
    NumberOfInstallmentsMustBePositive = 6,
    
    [ErrorMessage("Some categories was not found")]
    SomeCategoriesNotFound = 7,
    
    [ErrorMessage("Some categories is inactive")]
    SomeCategoriesInactive = 8,
    
    [ErrorMessage("Some people was not found")]
    SomePeopleNotFound = 9,
    
    [ErrorMessage("Some people is inactive")]
    SomePeopleInactive = 10,
    
    [ErrorMessage("Financial split between people must be greater than or equal to 0 and less than or equal to 100")]
    PeopleSplitRange = 11,
}

