using Fin.Infrastructure.AutoServices.Interfaces;

namespace Fin.Infrastructure.DateTimes;

public interface IDateTimeProvider
{
    public DateTime UtcNow();
    public DateOnly CurrentDate();
}

public class DateTimeProvider: IDateTimeProvider, IAutoSingleton
{
    public DateTime UtcNow()
    {
        return DateTime.UtcNow;
    }

    public DateOnly CurrentDate()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow);       
    }
}