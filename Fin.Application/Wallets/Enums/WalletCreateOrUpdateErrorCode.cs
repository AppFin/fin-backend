namespace Fin.Application.Wallets.Enums;

public enum WalletCreateOrUpdateErrorCode
{
    NameIsRequired = 0,
    NameAlreadyInUse = 1,
    NameTooLong = 2,
    ColorIsRequired = 3,
    ColorTooLong = 4,
    IconIsRequired = 5,
    IconTooLong = 6,
    WalletNotFound = 7
}