using GestionBibliotheque.Data;
using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class PayslipServiceTests
{
    [Fact]
    public async Task PayslipReadsCompanyEmployeePeriodAndFinalizedEntry()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var company = await new CompanyService(database.Factory, session).SaveAsync(new CompanyInput("Company Payslip", null, null, null, null, null, null, "Haiti", null, null, null, null));
        var employee = await new EmployeeService(database.Factory, session).AddAsync(new EmployeeInput("EMP-PAYSLIP", "Jean", "Pierre", null, null, null, DateTime.UtcNow.Date, null, null));
        await new SalaryService(database.Factory, session).AddAsync(new SalaryInput(employee.Id, "Base", 1000m, "Monthly", "HTG", new DateTime(2026, 5, 1), null));
        var period = await new PayrollPeriodService(database.Factory, session).CreateAsync(new PayrollPeriodInput(new DateTime(2026, 5, 1), new DateTime(2026, 5, 31), new DateTime(2026, 6, 5)));
        var entry = await new PayrollEntryService(database.Factory, session).SaveCalculatedAsync(new PayrollEntryInput(period.Id, employee.Id, 1000m, 1100m, 1100m, 0m, 100m, 50m, 1000m, [new PayrollComponentInput("Allowance", "Transport", 100m, null), new PayrollComponentInput("Deduction", "Advance", 100m, null)]));
        var periods = new PayrollPeriodService(database.Factory, session);
        await periods.MarkCalculatedAsync(period.Id); await periods.MarkReviewedAsync(period.Id);
        Assert.True(await new AuthenticationService(database.Factory, new PasswordHasher<User>(), session)
            .ReauthenticateForPayrollApprovalAsync("correct horse battery staple"));
        await periods.ApproveAsync(period.Id); await periods.MarkPaidAsync(period.Id);

        var payslip = await new PayslipService(database.Factory, session).GetAsync(entry.Id);

        Assert.Equal(company.LegalName, payslip.CompanyLegalName);
        Assert.Equal(employee.FullName, payslip.EmployeeName);
        Assert.Equal(100m, payslip.EarningsAmount);
        Assert.Equal(1000m, payslip.NetAmount);
    }

    [Fact]
    public async Task PayslipCannotReadDraftEntry()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var employee = await new EmployeeService(database.Factory, session).AddAsync(new EmployeeInput("EMP-PAYSLIP2", "Jean", "Pierre", null, null, null, DateTime.UtcNow.Date, null, null));
        await new SalaryService(database.Factory, session).AddAsync(new SalaryInput(employee.Id, "Base", 100m, "Monthly", "HTG", new DateTime(2026, 7, 1), null));
        var period = await new PayrollPeriodService(database.Factory, session).CreateAsync(new PayrollPeriodInput(new DateTime(2026, 7, 1), new DateTime(2026, 7, 31), new DateTime(2026, 8, 5)));
        var entry = await new PayrollEntryService(database.Factory, session).SaveCalculatedAsync(new PayrollEntryInput(period.Id, employee.Id, 100m, 100m, 100m, 0m, 0m, 0m, 100m, []));

        await Assert.ThrowsAsync<InvalidOperationException>(() => new PayslipService(database.Factory, session).GetAsync(entry.Id));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string path;
        public IDbContextFactory<LibraryDbContext> Factory { get; }
        private TestDatabase(string path, IDbContextFactory<LibraryDbContext> factory) { this.path = path; Factory = factory; }
        public static async Task<TestDatabase> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"library-payslip-{Guid.NewGuid():N}.db");
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
