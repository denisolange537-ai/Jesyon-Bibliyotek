using GestionBibliotheque.Data;
using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class TaxRuleServiceTests
{
    [Fact]
    public async Task TaxRulesAreVersionedAndSelectedByEffectiveDate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var service = new TaxRuleService(database.Factory, session);
        await service.AddAsync(new TaxRuleInput("Income", "Haiti", new DateTime(2026, 1, 1), new DateTime(2026, 6, 30), null, null, "Employee", false, "Needs legal validation", null, 1));
        var configured = await service.AddAsync(new TaxRuleInput("Income", "Haiti", new DateTime(2026, 7, 1), null, 0.1m, 1000m, "Employee", true, "Verified source", "official-reference", 2));

        Assert.Equal(configured.Id, (await service.GetApplicableAsync("Haiti", new DateTime(2026, 8, 1)))!.Id);
        Assert.Null(await service.GetApplicableAsync("Haiti", new DateTime(2025, 12, 1)));
        Assert.Contains(await service.GetAsync("Haiti"), rule => !rule.IsConfigured && rule.Rate is null);
    }

    [Fact]
    public async Task ConfiguredRuleRequiresSourceAndRate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var service = new TaxRuleService(database.Factory, session);
        await Assert.ThrowsAsync<ArgumentException>(() => service.AddAsync(new TaxRuleInput("Income", "Haiti", DateTime.UtcNow.Date, null, null, null, "Employee", true, null, null, 1)));
    }

    [Fact]
    public async Task TaxRuleRejectsInvalidRateThresholdAndDateRange()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new TaxRuleService(database.Factory, await database.CreateAdminSessionAsync());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.AddAsync(new TaxRuleInput("Income", "Synthetic", DateTime.UtcNow.Date, null, 1.01m, 0m, "Employee", true, "test", "test-source", 1)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.AddAsync(new TaxRuleInput("Income", "Synthetic", DateTime.UtcNow.Date, null, 0.1m, -1m, "Employee", true, "test", "test-source", 2)));
        await Assert.ThrowsAsync<ArgumentException>(() => service.AddAsync(new TaxRuleInput("Income", "Synthetic", new DateTime(2026, 6, 2), new DateTime(2026, 6, 1), 0.1m, 0m, "Employee", true, "test", "test-source", 3)));
    }

    [Fact]
    public async Task LibrarianCannotAddOrDeactivateTaxRule()
    {
        await using var database = await TestDatabase.CreateAsync();
        var administratorSession = await database.CreateAdminSessionAsync();
        var authentication = new AuthenticationService(database.Factory, new PasswordHasher<User>());
        var employee = await new EmployeeService(database.Factory, administratorSession).AddAsync(new EmployeeInput("EMP-TAX-LIB", "Lina", "Louis", null, null, null, DateTime.UtcNow.Date, "Librarian", null));
        await authentication.CreateAccountAsync(employee.Id, "tax-librarian", "correct horse battery staple", 2, new AuthorizationService(administratorSession));
        var librarianSession = new AuthenticationSession();
        librarianSession.SignIn(await authentication.AuthenticateAsync("tax-librarian", "correct horse battery staple") ?? throw new InvalidOperationException());
        var service = new TaxRuleService(database.Factory, librarianSession);

        await Assert.ThrowsAsync<AuthorizationException>(() => service.AddAsync(new TaxRuleInput("Income", "Synthetic", DateTime.UtcNow.Date, null, 0.1m, 0m, "Employee", true, "test", "test-source", 1)));
        await Assert.ThrowsAsync<AuthorizationException>(() => service.DeactivateAsync(1));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string path;
        public IDbContextFactory<LibraryDbContext> Factory { get; }
        private TestDatabase(string path, IDbContextFactory<LibraryDbContext> factory) { this.path = path; Factory = factory; }
        public static async Task<TestDatabase> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"library-tax-rule-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<LibraryDbContext>().UseSqlite($"Data Source={databasePath}").Options;
            var factory = new TestFactory(options); await using var context = await factory.CreateDbContextAsync(); await context.Database.MigrateAsync(); return new TestDatabase(databasePath, factory);
        }
        public async Task<AuthenticationSession> CreateAdminSessionAsync()
        {
            var auth = new AuthenticationService(Factory, new PasswordHasher<User>()); var session = new AuthenticationSession(); session.SignIn(await auth.CreateInitialAdminAsync("admin", "correct horse battery staple")); return session;
        }
        public async ValueTask DisposeAsync() { await using var context = await Factory.CreateDbContextAsync(); await context.Database.CloseConnectionAsync(); SqliteConnection.ClearAllPools(); File.Delete(path); }
        private sealed class TestFactory(DbContextOptions<LibraryDbContext> options) : IDbContextFactory<LibraryDbContext>
        { public LibraryDbContext CreateDbContext() => new(options); public Task<LibraryDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new LibraryDbContext(options)); }
    }
}
