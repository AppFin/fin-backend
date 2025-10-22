using Fin.Domain.Titles.Enums;

namespace Fin.Domain.Titles.Dtos;

public class TitleOutput
{
    public decimal Value { get; set; }
    public decimal PreviousBalance { get; set; }
    public TitleType Type { get; set; }
    public string Description { get; set; }
    public DateTime Date { get; set; }
    public Guid WalletId { get; set; }
    public List<Guid> TitleCategoriesIds { get; set; }
}