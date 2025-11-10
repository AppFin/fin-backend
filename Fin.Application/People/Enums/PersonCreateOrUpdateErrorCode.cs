using Fin.Infrastructure.Errors;

namespace Fin.Application.People.Enums;

public enum PersonCreateOrUpdateErrorCode
{
    [ErrorMessage("Name is required")]
    NameIsRequired = 0,
    
    [ErrorMessage("Name already in use")]
    NameAlreadyInUse = 1,
    
    [ErrorMessage("Name max lenght 100")]
    NameTooLong = 2,
    
    [ErrorMessage("Name max lenght 100")]
    PersonNotFound = 4
}