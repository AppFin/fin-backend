using Fin.Domain.Titles.Entities;
using Fin.Domain.Titles.Enums;

namespace Fin.Domain.Titles.Dtos;

public class TitleOutput(Title title)
{
    public Guid Id { get; set; } = title.Id;
    public string Description { get; set; } = title.Description;
    public decimal Value { get; set; } = title.Value;
    public decimal PreviousBalance { get; set; } = title.PreviousBalance;
    public decimal ResultingBalance { get; set; } = title.ResultingBalance;
    public TitleType Type { get; set; } = title.Type;
    public DateTime Date { get; set; } = title.Date;
    public Guid WalletId { get; set; } = title.WalletId;
    public List<Guid> TitleCategoriesIds { get; set; } = title.TitleCategories
        .Select(x => x.Id).ToList();

    public TitleOutput(): this(new Title())
    {
        
    }
}