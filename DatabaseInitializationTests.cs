using GestionBibliotheque.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class DatabaseInitializationTests
{
    [Fact]
    public async Task Migration_CreatesSqliteSchemaAndSeedsRoles()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"library-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath}";

        try
        {
            var options = new DbContextOptionsBuilder<LibraryDbContext>()
                .UseSqlite(connectionString)
                .Options;

            await using (var dbContext = new LibraryDbContext(options))
            {
                await dbContext.Database.MigrateAsync();
            }

            Assert.True(File.Exists(databasePath));

            await using (var dbContext = new LibraryDbContext(options))
            {
                Assert.Equal(2, await dbContext.Roles.CountAsync());
                Assert.Empty(await dbContext.Users.ToListAsync());
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }
}
