using Fin.Infrastructure.Errors;

namespace Fin.Application.People.Enums;

public enum PersonDeleteErrorCode
{
    [ErrorMessage("Person in use.")]
    PersonInUse = 0,
    
    [ErrorMessage("Person not found.")]
    PersonNotFound = 1
}