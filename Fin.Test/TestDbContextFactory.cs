using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Fin.Test;

public static class TestDbContextFactory
{
    public static FinDbContext Create(out SqliteConnection connection, IAmbientData ambientData, bool useFile = false)
    {
        connection = new SqliteConnection(useFile
            ? $"DataSource=test_{Guid.NewGuid()}.db"
            : "DataSource=:memory:");
        
        connection.Open();

        var options = new DbContextOptionsBuilder<FinDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new FinDbContext(options, ambientData);
        context.Database.EnsureCreated();
        return context;
    }

    public static void Destroy(SqliteConnection connection)
    {
        connection?.Dispose();
    }
}