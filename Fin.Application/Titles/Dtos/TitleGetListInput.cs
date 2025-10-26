using Fin.Domain.Global.Classes;
using Fin.Domain.Global.Enums;
using Fin.Domain.Titles.Enums;

namespace Fin.Application.Titles.Dtos;

public class TitleGetListInput: PagedFilteredAndSortedInput
{
    public List<Guid> CategoryIds { get; set; } = [];
    public MultiplyFilterOperator CategoryOperator { get; set; }
    public List<Guid> WalletIds { get; set; } = [];
    public TitleType? Type { get; set; }
}