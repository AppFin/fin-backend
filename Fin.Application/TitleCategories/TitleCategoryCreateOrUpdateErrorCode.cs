namespace Fin.Application.TitleCategories;

public enum TitleCategoryCreateOrUpdateErrorCode
{
    NameIsRequired = 1,
    NameAlreadyInUse = 2,
    NameTooLong = 3,
    ColorIsRequired = 4,
    ColorTooLong = 5,
    IconIsRequired = 6,
    IconTooLong = 7,
    TitleCategoryNotFound = 8,
}