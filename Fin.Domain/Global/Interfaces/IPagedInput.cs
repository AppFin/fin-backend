namespace Fin.Domain.Global.Interfaces;

public interface IPagedInput
{
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; }
}