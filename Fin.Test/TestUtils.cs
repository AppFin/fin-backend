using Fin.Domain.Global.Interfaces;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Database;
using Fin.Infrastructure.Database.IRepositories;
using Fin.Infrastructure.DateTimes;
using Microsoft.Data.Sqlite;
using Moq;

namespace Fin.Test;

public class TestUtils
{
    public class BaseTest
    {
        public Mock<IDateTimeProvider> DateTimeProvider { get; } = new();
        public AmbientData AmbientData { get; } = new();

        protected BaseTest()
        {
            AmbientData.SetData(Guids[0], Guids[1], Strings[0], true);
        }
    }
    
    public class BaseTestWithContext: BaseTest, IDisposable
    {
        protected readonly FinDbContext Context;
        private readonly SqliteConnection _connection;
        private readonly string _dbFilePath;
        
        protected BaseTestWithContext()
        {
            Context = TestDbContextFactory.Create(out _connection, out _dbFilePath, AmbientData, useFile: true);
        }

        public void Dispose()
        {
            Context.Dispose();
            TestDbContextFactory.Destroy(_connection, _dbFilePath);;
        }
        
        public IRepository<T> GetRepository<T>() where T : class, IEntity
        {
            return new Repository<T>(Context);
        }
    }
    
    public static List<Guid> Guids => new List<Guid>
    {
        Guid.Parse("3f5e2a76-9c4d-45f7-b798-8412ad4cfb6d"),
        Guid.Parse("c39f1d86-e37b-456f-9f4c-729e267f4ef2"),
        Guid.Parse("b1d9c6f3-1c38-41aa-b027-33b45a7b59a8"),
        Guid.Parse("9d7ae29f-3d99-4382-a9f1-8c8fa79f56d9"),
        Guid.Parse("e4fa2b9d-92d1-4427-a5e6-5a7f5d108b79"),
        Guid.Parse("52dc11b0-e831-4e49-bcfc-24d8898bfc38"),
        Guid.Parse("a89ef3f4-4fd9-48aa-a09d-d5bece9fd1e5"),
        Guid.Parse("77db60c9-e65c-434c-8af8-d3c0323f2b10"),
        Guid.Parse("fd933db3-74d9-423b-bdb5-9c16fa91d1d7"),
        Guid.Parse("6f4d9ef4-3211-46b2-abe1-c87ef6a39db7")
    };
    
    public static List<string> Strings => new List<string>
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
    
    public static List<DateTime> UtcDateTimes => new List<DateTime>
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