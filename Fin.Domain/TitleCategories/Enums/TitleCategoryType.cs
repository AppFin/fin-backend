using Fin.Domain.Titles.Enums;

namespace Fin.Domain.TitleCategories.Enums;

public enum TitleCategoryType: byte
{
    Expense = 0,
    Income = 1,
    Both = 2
}

public static class TitleCategoryTypeExtension
{
    public static bool IsCompatible(this TitleCategoryType titleCategoryType, TitleType titleType)
    {
        return titleCategoryType switch
        {
            TitleCategoryType.Expense => titleType == TitleType.Expense,
            TitleCategoryType.Income => titleType == TitleType.Income,
            TitleCategoryType.Both => true,
            _ => throw new ArgumentOutOfRangeException(nameof(titleCategoryType), titleCategoryType, null)
        };
    }
}