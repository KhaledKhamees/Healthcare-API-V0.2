using Healthcare_API_V0._2_Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Healthcare_API_V0._2.Tests.Helpers;

public static class TestDbContextFactory
{
    public static (HealthcareDbContext Context, SqliteConnection Connection) Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<HealthcareDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new HealthcareDbContext(options);
        context.Database.EnsureCreated();
        return (context, connection);
    }

    public static HealthcareDbContext CreateDisposable(out SqliteConnection connection)
    {
        var (ctx, conn) = Create();
        connection = conn;
        return ctx;
    }
}
