namespace Fin.Application.TitleCategories.Enums;

public enum TitleCategoryCreateOrUpdateErrorCode
{
    NameIsRequired = 0,
    NameAlreadyInUse = 1,
    NameTooLong = 2,
    ColorIsRequired = 3,
    ColorTooLong = 4,
    IconIsRequired = 5,
    IconTooLong = 6,
    TitleCategoryNotFound = 7
}