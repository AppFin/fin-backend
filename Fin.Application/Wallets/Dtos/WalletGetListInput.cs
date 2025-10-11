using Fin.Domain.Global.Classes;

namespace Fin.Application.Wallets.Dtos;

public class WalletGetListInput: PagedFilteredAndSortedInput
{
    public bool? Inactivated { get; set; }
}