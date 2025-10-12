namespace Fin.Application.Globals.Dtos;

public class ValidationResultDto<D, E>
{
    public D? Data { get; set; }
    public string Message { get; set; }
    public bool Success { get; set; }
    public E? ErrorCode { get; set; }
}

public class ValidationResultDto<D> 
{
    public D? Data { get; set; }
    public string Message { get; set; }
    public bool Success { get; set; }
    public Enum? ErrorCode { get; set; }
}

