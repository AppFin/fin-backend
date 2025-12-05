using Fin.Domain.Global.Decorators;

namespace Fin.Domain.FinancialInstitutions.Enums
{
    public enum FinancialInstitutionType
    {
        [FrontTranslateKey("finCore.features.financialInstitutions.type.bank")]
        Bank = 0,
        [FrontTranslateKey("finCore.features.financialInstitutions.type.digitalBank")]
        DigitalBank = 1,
        [FrontTranslateKey("finCore.features.financialInstitutions.type.foodCard")]
        FoodCard = 2,
    }
}
