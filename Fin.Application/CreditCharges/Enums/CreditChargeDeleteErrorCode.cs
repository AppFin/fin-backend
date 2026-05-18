using Fin.Infrastructure.Errors;

namespace Fin.Application.CreditCharges.Enums;

public enum CreditChargeDeleteErrorCode
{
    [ErrorMessage("Credit charge not found")]
    CreditChargeNotFound = 0,
}

