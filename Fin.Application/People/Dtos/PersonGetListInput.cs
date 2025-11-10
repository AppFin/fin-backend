using Fin.Domain.Global.Classes;

namespace Fin.Application.People.Dtos;

public class PersonGetListInput: PagedFilteredAndSortedInput
{
    public bool? Inactivated { get; set; }
}