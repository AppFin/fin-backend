using Fin.Infrastructure.Errors;

namespace Fin.Application.Wallets.Enums;

public enum WalletDeleteErrorCode
{
    [ErrorMessage("Wallet not found")]
    WalletNotFound = 0,
    
    [ErrorMessage("Wallet in use by titles")]
    WalletInUseByTitles = 1,
    
    [ErrorMessage("Wallet in use by credit cards")]
    WalletInUseByCreditCards = 2,
    
    [ErrorMessage("Wallet in use by credit card and titles")]
    WalletInUseByCreditCardsAndTitle = 3,
}