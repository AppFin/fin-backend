using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Fin.Test;

public static class TestDbContextFactory
{
    public static FinDbContext Create(out SqliteConnection connection, out string dbFilePath, IAmbientData ambientData, bool useFile = false)
    {
        if (useFile)
        {
            dbFilePath = $"test_{Guid.NewGuid()}.db";
            connection = new SqliteConnection($"Data Source={dbFilePath}");
        }
        else
        {
            dbFilePath = null;
            connection = new SqliteConnection("DataSource=:memory:");
        }
        
        connection.Open();

        var options = new DbContextOptionsBuilder<FinDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new FinDbContext(options, ambientData);
        context.Database.EnsureCreated();
        return context;
    }

    public static void Destroy(SqliteConnection connection, string dbFilePath)
    {
        connection?.Dispose();
        
        if (!string.IsNullOrEmpty(dbFilePath) && File.Exists(dbFilePath))
        {
            try
            {
                File.Delete(dbFilePath);
            }
            catch
            {
                // Handle or log as needed
            }
        }
    }
}