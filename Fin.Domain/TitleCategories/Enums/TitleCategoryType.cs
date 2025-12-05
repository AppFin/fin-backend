using Fin.Domain.Global.Decorators;
using Fin.Domain.Titles.Enums;

namespace Fin.Domain.TitleCategories.Enums;

public enum TitleCategoryType: byte
{
    [FrontTranslateKey("finCore.features.titleCategory.type.expense")]
    Expense = 0,
    [FrontTranslateKey("finCore.features.titleCategory.type.income")]
    Income = 1,
    [FrontTranslateKey("finCore.features.titleCategory.type.both")]
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