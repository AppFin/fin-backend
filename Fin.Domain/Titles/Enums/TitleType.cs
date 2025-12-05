using System.ComponentModel;
using Fin.Domain.Global.Decorators;

namespace Fin.Domain.Titles.Enums;

public enum TitleType: byte
{
    [FrontTranslateKey("finCore.features.title.type.expense")]
    Expense = 0,
    
    [FrontTranslateKey("finCore.features.title.type.income")]
    Income = 1
}