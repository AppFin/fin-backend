using Fin.Infrastructure.AutoServices;

namespace Fin.Infrastructure.Services;

public interface IDateTimeProvider
{
    public DateTime UtcNow();
}

public class DateTimeProvider: IDateTimeProvider, IAutoSingleton
{
    public DateTime UtcNow()
    {
        return DateTime.UtcNow;
    }
}