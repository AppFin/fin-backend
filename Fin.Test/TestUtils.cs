using Fin.Domain.CardBrands.Entities;
using Fin.Domain.FinancialInstitutions.Entities;
using Fin.Domain.FinancialInstitutions.Enums;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Entities;
using Fin.Domain.Wallets.Dtos;
using Fin.Domain.Wallets.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Database;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.DateTimes;
using Fin.Infrastructure.UnitOfWorks;
using Microsoft.Data.Sqlite;
using Moq;

namespace Fin.Test;

public abstract class TestUtils
{
    public class BaseTest
    {
        protected Mock<IDateTimeProvider> DateTimeProvider { get; } = new();
        protected AmbientData AmbientData { get; } = new();

        protected BaseTest()
        {
        }

        protected async Task ConfigureLoggedAmbientAsync(bool isAdmin = true)
        {
            AmbientData.SetData(Guids[0], Guids[1], Strings[0], isAdmin);
            await Task.CompletedTask;
        }
    }

    public class BaseTestWithContext : BaseTest, IDisposable
    {
        protected readonly FinDbContext Context;
        protected readonly UnitOfWork UnitOfWork;
        private readonly SqliteConnection _connection;
        private readonly string _dbFilePath;

        protected BaseTestWithContext()
        {
            var dateTimeProviderMockForContext = new Mock<IDateTimeProvider>();
            Context = TestDbContextFactory.Create(out _connection, out _dbFilePath, AmbientData,
                dateTimeProviderMockForContext.Object, useFile: true);
            UnitOfWork = new UnitOfWork(Context);
        }

        public void Dispose()
        {
            Context.Dispose();
            TestDbContextFactory.Destroy(_connection, _dbFilePath);
            ;
        }

        protected IRepository<T> GetRepository<T>() where T : class
        {
            return new Repository<T>(Context);
        }

        protected new async Task ConfigureLoggedAmbientAsync(bool isAdmin = true)
        {
            var user = new User
            {
                Id = Guids[0],
                Tenants = [new Tenant()],
                Credential = new UserCredential()
            };
            if (isAdmin) user.MakeAdmin();

            await Context.Users.AddAsync(user);
            await Context.SaveChangesAsync();
            AmbientData.SetData(user.Tenants.First().Id, user.Id, user.DisplayName, user.IsAdmin);
        }
    }

    public static List<Guid> Guids =>
    [
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
    ];

    public static List<string> Strings =>
    [
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
    ];
    
    public static List<decimal> Decimals =>
    [
        100.00m,
        45.50m,
        0.00m,
        -12.75m,
        99999.99m,
        1.234567m,
        1000m,
        -500.00m,
        123456.78m,
        2.5m
    ];

    public static List<DateTime> UtcDateTimes =>
    [
        new(2023, 01, 01, 0, 0, 0, DateTimeKind.Utc),
        new(2023, 06, 15, 12, 30, 0, DateTimeKind.Utc),
        new(2024, 02, 29, 23, 59, 59, DateTimeKind.Utc),
        new(2024, 12, 31, 18, 45, 0, DateTimeKind.Utc),
        new(2025, 05, 03, 14, 0, 0, DateTimeKind.Utc),
        new(2025, 10, 10, 8, 15, 0, DateTimeKind.Utc),
        new(2026, 03, 20, 6, 0, 0, DateTimeKind.Utc),
        new(2027, 07, 04, 22, 10, 0, DateTimeKind.Utc),
        new(2028, 11, 11, 11, 11, 11, DateTimeKind.Utc),
        new(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc)
    ];

    public static List<TimeSpan> TimeSpans =>
    [
        new(0, 0, 0), // 00:00:00
        new(1, 30, 0), // 01:30:00
        new(5, 45, 15), // 05:45:15
        new(12, 0, 0), // 12:00:00
        new(18, 15, 30), // 18:15:30
        new(23, 59, 59), // 23:59:59
        new(2, 0, 0), // 02:00:00
        new(0, 45, 0), // 00:45:00
        new(10, 10, 10), // 10:10:10
        new(7, 20, 5)
    ];
    
    public static List<CardBrand> CardBrands =>
    [
        new() { Name  = Strings[0], Color = Strings[1], Icon = Strings[2] },
        new() { Name  = Strings[2], Color = Strings[3], Icon = Strings[4] },
        new() { Name  = Strings[4], Color = Strings[5], Icon = Strings[6] },
        new() { Name  = Strings[6], Color = Strings[7], Icon = Strings[8] },
        new() { Name  = Strings[8], Color = Strings[9], Icon = Strings[0] }
    ];
    
    public static List<FinancialInstitution> FinancialInstitutions =>
    [
        new() { Name  = Strings[0], Color = Strings[1], Icon = Strings[2], Type = FinancialInstitutionType.Bank },
        new() { Name  = Strings[2], Color = Strings[3], Icon = Strings[4], Type = FinancialInstitutionType.DigitalBank },
        new() { Name  = Strings[4], Color = Strings[5], Icon = Strings[6], Type = FinancialInstitutionType.FoodCard },
        new() { Name  = Strings[6], Color = Strings[7], Icon = Strings[8], Type = FinancialInstitutionType.DigitalBank },
        new() { Name  = Strings[8], Color = Strings[9], Icon = Strings[0], Type = FinancialInstitutionType.Bank }
    ];
    
    public static List<WalletInput> WalletsInputs =>
    [
        new() { Name  = Strings[0], Color = Strings[1], Icon = Strings[2], InitialBalance = Decimals[0] },
        new() { Name  = Strings[2], Color = Strings[3], Icon = Strings[4], InitialBalance = Decimals[1] },
        new() { Name  = Strings[4], Color = Strings[5], Icon = Strings[6], InitialBalance = Decimals[2] },
        new() { Name  = Strings[6], Color = Strings[7], Icon = Strings[8], InitialBalance = Decimals[3] },
        new() { Name  = Strings[8], Color = Strings[9], Icon = Strings[0], InitialBalance = Decimals[4] }
    ];
    
    public static List<Wallet> Wallets =>
    [
        new(WalletsInputs[0]),
        new(WalletsInputs[1]),
        new(WalletsInputs[2]),
        new(WalletsInputs[3]),
        new(WalletsInputs[4])
    ];
}