namespace Fin.Infrastructure.Services;

public interface IDateTimeProvider
{
    public DateTime UtcNow();
}

public class DateTimeProvider: IDateTimeProvider
{
    public DateTime UtcNow()
    {
        return DateTime.UtcNow;
    }
}