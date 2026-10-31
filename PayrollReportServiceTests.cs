using GestionBibliotheque.Data;
using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class PayrollReportServiceTests
{
    [Fact]
    public async Task SummaryAggregatesStoredPayrollValues()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        await new CompanyService(database.Factory, session).SaveAsync(new CompanyInput("Tax Company", null, null, null, null, null, null, "Haiti", null, null, null, null));
        await new TaxRuleService(database.Factory, session).AddAsync(new TaxRuleInput("Income", "Haiti", new DateTime(2026, 8, 1), null, 0.1m, 0m, "Employee", true, "Configured test rule", "test-reference", 1));
        var employee = await new EmployeeService(database.Factory, session).AddAsync(new EmployeeInput("EMP-REPORT", "Jean", "Pierre", null, null, null, DateTime.UtcNow.Date, null, null));
        await new SalaryService(database.Factory, session).AddAsync(new SalaryInput(employee.Id, "Base", 1000m, "Monthly", "HTG", new DateTime(2026, 8, 1), null));
        var period = await new PayrollPeriodService(database.Factory, session).CreateAsync(new PayrollPeriodInput(new DateTime(2026, 8, 1), new DateTime(2026, 8, 31), new DateTime(2026, 9, 5)));
        await new PayrollEntryService(database.Factory, session).SaveCalculatedAsync(new PayrollEntryInput(period.Id, employee.Id, 1000m, 1200m, 1200m, 100m, 50m, 60m, 1050m, [new PayrollComponentInput("Earning", "Allowance", 200m, null), new PayrollComponentInput("Deduction", "Advance", 50m, null)]));

        var summary = await new PayrollReportService(database.Factory, session).GetSummaryAsync(period.Id);

        Assert.Equal(1, summary.EntryCount);
        Assert.Equal(1200m, summary.GrossAmount);
        Assert.Equal(100m, summary.EmployeeTaxAmount);
        Assert.Equal(50m, summary.EmployeeDeductionAmount);
        Assert.Equal(1050m, summary.NetAmount);
        Assert.Equal(60m, summary.EmployerContributionAmount);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string path;
        public IDbContextFactory<LibraryDbContext> Factory { get; }
        private TestDatabase(string path, IDbContextFactory<LibraryDbContext> factory) { this.path = path; Factory = factory; }
        public static async Task<TestDatabase> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"library-payroll-report-{Guid.NewGuid():N}.db");
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
