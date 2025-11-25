using Fin.Infrastructure.Errors;

namespace Fin.Application.Titles.Enums;

public enum TitleCreateOrUpdateErrorCode
{
    [ErrorMessage("Title not found")]
    TitleNotFound = 0,
    
    [ErrorMessage("Description must have less than 100 characters.")]
    DescriptionTooLong = 1,
    
    [ErrorMessage("Description is required.")]
    DescriptionIsRequired = 2,
    
    [ErrorMessage("Wallet not found")]
    WalletNotFound = 3,
    
    [ErrorMessage("Wallet is inactive")]
    WalletInactive = 4,
    
    [ErrorMessage("Title date must be equal or after wallet creation date.")]
    TitleDateMustBeEqualOrAfterWalletCreation = 5,
    
    [ErrorMessage("Some categories was not found")]
    SomeCategoriesNotFound = 6,
    
    [ErrorMessage("Some categories is inactive")]
    SomeCategoriesInactive = 7,
    
    [ErrorMessage("Some categories has incompatible types")]
    SomeCategoriesHasIncompatibleTypes = 8,
    
    [ErrorMessage("Value must be greater than zero.")]
    ValueMustBeGraterThanZero = 9,
    
    [ErrorMessage("Duplicated title in same date time until minute.")]
    DuplicateTitleInSameDateTimeMinute = 10,
    
    [ErrorMessage("Some people was not found")]
    SomePeopleNotFound = 11,
    
    [ErrorMessage("Some people is inactive")]
    SomePeopleInactive = 12,
    
    [ErrorMessage("Financial split between people must be greater than or equal to 0 and less than or equal to 100")]
    PeopleSplitRange = 13,
    
}