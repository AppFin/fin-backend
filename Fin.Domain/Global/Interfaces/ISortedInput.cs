using Fin.Domain.Global.Classes;

namespace Fin.Domain.Global.Interfaces;

public interface ISortedInput
{
    public List<SortedProperty> Sorts { get; set; } 
}