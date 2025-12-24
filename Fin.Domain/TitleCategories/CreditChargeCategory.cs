using Fin.Domain.CreditCharges.Entities;
using Fin.Domain.Global.Interfaces;
using Fin.Domain.TitleCategories.Entities;

namespace Fin.Domain.TitleCategories;

public class CreditChargeCategory: ILoggable
{
    public Guid CreditChargeId { get; set; }
    public virtual CreditCharge CreditCharge { get; set; }
    
    public Guid TitleCategoryId { get; set; }
    public virtual TitleCategory TitleCategory { get; set; }

    public CreditChargeCategory()
    {
    }

    public CreditChargeCategory(Guid categoryId, Guid creditChargeId)
    {
        CreditChargeId = creditChargeId;
        TitleCategoryId = categoryId;
    }

    public object GetLog()
    {
        return new
        {
            CreditChargeId,
            TitleCategoryId
        };
    }
}