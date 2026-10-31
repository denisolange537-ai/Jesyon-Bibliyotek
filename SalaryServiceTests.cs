using GestionBibliotheque.Data;
using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class SalaryServiceTests
{
    [Fact]
    public async Task SalaryHistoryAndApplicableDateUseRealEmployeeData()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var employee = await new EmployeeService(database.Factory, session).AddAsync(new EmployeeInput("EMP-SAL", "Jean", "Pierre", null, null, null, DateTime.UtcNow.Date, null, null));
        var service = new SalaryService(database.Factory, session);
        var first = await service.AddAsync(new SalaryInput(employee.Id, "Base", 1000m, "Monthly", "HTG", new DateTime(2026, 1, 1), new DateTime(2026, 5, 31)));
        var second = await service.AddAsync(new SalaryInput(employee.Id, "Base", 1200m, "Monthly", "HTG", new DateTime(2026, 6, 1), null));

        Assert.Equal(2, (await service.GetHistoryAsync(employee.Id)).Count);
        Assert.Equal(first.Id, (await service.GetApplicableAsync(employee.Id, new DateTime(2026, 3, 1)))!.Id);
        Assert.Equal(second.Id, (await service.GetApplicableAsync(employee.Id, new DateTime(2026, 7, 1)))!.Id);
    }

    [Fact]
    public async Task OverlappingSalaryAndInactiveEmployeeAreRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var employee = await new EmployeeService(database.Factory, session).AddAsync(new EmployeeInput("EMP-SAL2", "Jean", "Pierre", null, null, null, DateTime.UtcNow.Date, null, null));
        var service = new SalaryService(database.Factory, session);
        await service.AddAsync(new SalaryInput(employee.Id, "Base", 1000m, "Monthly", "HTG", new DateTime(2026, 1, 1), null));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddAsync(new SalaryInput(employee.Id, "Base", 1200m, "Monthly", "HTG", new DateTime(2026, 2, 1), null)));
        await new EmployeeService(database.Factory, session).DeactivateAsync(employee.Id);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AddAsync(new SalaryInput(employee.Id, "Base", 1200m, "Monthly", "HTG", new DateTime(2027, 1, 1), null)));
    }

    [Fact]
    public async Task SalaryReadRequiresPermission()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new SalaryService(database.Factory, new AuthenticationSession());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetHistoryAsync(1));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string path;
        public IDbContextFactory<LibraryDbContext> Factory { get; }
        private TestDatabase(string path, IDbContextFactory<LibraryDbContext> factory) { this.path = path; Factory = factory; }
        public static async Task<TestDatabase> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"library-salary-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<LibraryDbContext>().UseSqlite($"Data Source={databasePath}").Options;
            var factory = new TestFactory(options);
            await using var context = await factory.CreateDbContextAsync(); await context.Database.MigrateAsync();
            return new TestDatabase(databasePath, factory);
        }
        public async Task<AuthenticationSession> CreateAdminSessionAsync()
        {
            var auth = new AuthenticationService(Factory, new PasswordHasher<User>());
            var session = new AuthenticationSession(); session.SignIn(await auth.CreateInitialAdminAsync("admin", "correct horse battery staple")); return session;
        }
        public async ValueTask DisposeAsync()
        {
            await using var context = await Factory.CreateDbContextAsync(); await context.Database.CloseConnectionAsync(); SqliteConnection.ClearAllPools(); File.Delete(path);
        }
        private sealed class TestFactory(DbContextOptions<LibraryDbContext> options) : IDbContextFactory<LibraryDbContext>
        {
            public LibraryDbContext CreateDbContext() => new(options);
            public Task<LibraryDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new LibraryDbContext(options));
        }
    }
}
