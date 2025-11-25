using Fin.Domain.People.Dtos;
using Fin.Domain.Titles.Enums;

namespace Fin.Domain.Titles.Dtos;

public class TitleInput
{
    public decimal Value { get; set; }
    public TitleType Type { get; set; }
    public string Description { get; set; }
    public DateTime Date { get; set; }
    public Guid WalletId { get; set; }
    public List<Guid> TitleCategoriesIds { get; set; } = [];
    public List<TitlePersonInput> TitlePeople { get; set; } = [];
}