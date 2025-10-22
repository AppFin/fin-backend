using Fin.Domain.Titles.Entities;

namespace Fin.Domain.TitleCategories.Entities;

public class TitleTitleCategory
{
    public Guid TitleId { get; set; }
    public virtual Title Title { get; set; }
    
    public Guid TitleCategoryId { get; set; }
    public virtual TitleCategory TitleCategory { get; set; }
}