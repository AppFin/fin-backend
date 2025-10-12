namespace Fin.Application.Wallets.Enums;

public enum WalletDeleteErrorCode
{
    WalletNotFound = 0,
    WalletInUseByTitles = 1,
    WalletInUseByCreditCards = 2,
    WalletInUseByCreditCardsAndTitle = 3,
}