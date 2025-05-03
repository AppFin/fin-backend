using Fin.Infrastructure.DateTimes;
using Moq;

namespace Fin.Test;

public class TestUtils
{
    public class BaseTest
    {
        public Mock<IDateTimeProvider> DateTimeProvider { get; } = new Mock<IDateTimeProvider>();
    }
    
    public static List<string> Strings() => new List<string>
    {
        "alpha-923",
        "John Doe",
        "sample@test.com",
        "lorem ipsum",
        "token_ABC123",
        "password123!",
        "Order#987654",
        "Hello, World!",
        "A1B2C3D4",
        "Zebra@Night"
    };
    
    public static List<DateTime> UtcDateTimes() => new List<DateTime>
    {
        new DateTime(2023, 01, 01, 0, 0, 0, DateTimeKind.Utc),
        new DateTime(2023, 06, 15, 12, 30, 0, DateTimeKind.Utc),
        new DateTime(2024, 02, 29, 23, 59, 59, DateTimeKind.Utc),
        new DateTime(2024, 12, 31, 18, 45, 0, DateTimeKind.Utc),
        new DateTime(2025, 05, 03, 14, 0, 0, DateTimeKind.Utc),
        new DateTime(2025, 10, 10, 8, 15, 0, DateTimeKind.Utc),
        new DateTime(2026, 03, 20, 6, 0, 0, DateTimeKind.Utc),
        new DateTime(2027, 07, 04, 22, 10, 0, DateTimeKind.Utc),
        new DateTime(2028, 11, 11, 11, 11, 11, DateTimeKind.Utc),
        new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc)
    };
}