using Fin.Domain.Global.Classes;

namespace Fin.Domain.Global.Interfaces;

public interface IFilteredInput
{
    public FilteredProperty Filter { get; set; }
}