using Fin.Domain.Global.Interfaces;
using Fin.Domain.Titles.Entities;

namespace Fin.Domain.TitleCategories.Entities;

public class TitleTitleCategory: ILoggable
{
    public Guid TitleId { get; set; }
    public virtual Title Title { get; set; }
    
    public Guid TitleCategoryId { get; set; }
    public virtual TitleCategory TitleCategory { get; set; }

    public TitleTitleCategory()
    {
    }

    public TitleTitleCategory(Guid categoryId, Guid titleId)
    {
        TitleId = titleId;
        TitleCategoryId = categoryId;
    }

    public object GetLog()
    {
        return new
        {
            TitleId,
            TitleCategoryId
        };
    }
}