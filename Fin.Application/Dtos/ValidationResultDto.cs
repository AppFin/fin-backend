namespace Fin.Application.Dtos;

public class ValidationResultDto<D, E> where D : class
{
    public D? Data { get; set; }
    public string Message { get; set; }
    public bool Success { get; set; }
    public E? ErrorCode { get; set; }
}

public class ValidationResultDto<D> where D : class
{
    public D? Data { get; set; }
    public string Message { get; set; }
    public bool Success { get; set; }
}