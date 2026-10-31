using GestionBibliotheque.Data;
using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class PayrollEntryServiceTests
{
    [Fact]
    public async Task CalculatedEntryPersistsComponentsAndAmounts()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var employee = await new EmployeeService(database.Factory, session).AddAsync(new EmployeeInput("EMP-ENTRY", "Jean", "Pierre", null, null, null, DateTime.UtcNow.Date, null, null));
        await new SalaryService(database.Factory, session).AddAsync(new SalaryInput(employee.Id, "Base", 1000m, "Monthly", "HTG", new DateTime(2026, 3, 1), null));
        var period = await new PayrollPeriodService(database.Factory, session).CreateAsync(new PayrollPeriodInput(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31), new DateTime(2026, 4, 5)));
        var service = new PayrollEntryService(database.Factory, session);

        var entry = await service.SaveCalculatedAsync(new PayrollEntryInput(period.Id, employee.Id, 1000m, 1100m, 1100m, 0m, 100m, 50m, 1000m, [new PayrollComponentInput("Earning", "Allowance", 100m, "Configured allowance"), new PayrollComponentInput("Deduction", "Advance", 100m, "Configured deduction") ]));
        var stored = (await service.GetForPeriodAsync(period.Id)).Single();

        Assert.Equal(entry.Id, stored.Id);
        Assert.Equal(1000m, stored.NetAmount);
        Assert.Equal(2, stored.Components.Count);
        await using (var periodContext = await database.Factory.CreateDbContextAsync())
            Assert.Equal(2L, (await periodContext.PayrollPeriods.SingleAsync(item => item.Id == period.Id)).Version);
        Assert.Contains(await database.ReadAuditAsync(), audit => audit.EntityName == nameof(PayrollEntry) && audit.Action == "Calculated");
    }

    [Fact]
    public async Task CalculatedEntryRejectsInvalidNetAndClosedPeriodModification()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var employee = await new EmployeeService(database.Factory, session).AddAsync(new EmployeeInput("EMP-ENTRY2", "Jean", "Pierre", null, null, null, DateTime.UtcNow.Date, null, null));
        await new SalaryService(database.Factory, session).AddAsync(new SalaryInput(employee.Id, "Base", 1000m, "Monthly", "HTG", new DateTime(2026, 4, 1), null));
        var periodService = new PayrollPeriodService(database.Factory, session);
        var period = await periodService.CreateAsync(new PayrollPeriodInput(new DateTime(2026, 4, 1), new DateTime(2026, 4, 30), new DateTime(2026, 5, 5)));
        var service = new PayrollEntryService(database.Factory, session);
        var invalid = new PayrollEntryInput(period.Id, employee.Id, 1000m, 1000m, 1000m, 0m, 100m, 0m, 950m, [new PayrollComponentInput("Deduction", "Advance", 100m, null)]);

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveCalculatedAsync(invalid));
        await service.SaveCalculatedAsync(invalid with { NetAmount = 900m });
        await periodService.MarkCalculatedAsync(period.Id); await periodService.MarkReviewedAsync(period.Id);
        Assert.True(await new AuthenticationService(database.Factory, new PasswordHasher<User>(), session)
            .ReauthenticateForPayrollApprovalAsync("correct horse battery staple"));
        await periodService.ApproveAsync(period.Id); await periodService.MarkPaidAsync(period.Id); await periodService.CloseAsync(period.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveCalculatedAsync(invalid with { NetAmount = 1100m, GrossAmount = 1200m }));
    }

    [Fact]
    public async Task CalculatePeriodUsesApplicableSalaryAndActiveEmployeesOnly()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var active = await new EmployeeService(database.Factory, session).AddAsync(new EmployeeInput("EMP-CALC", "Jean", "Pierre", null, null, null, DateTime.UtcNow.Date, null, null));
        var inactive = await new EmployeeService(database.Factory, session).AddAsync(new EmployeeInput("EMP-INACTIVE", "Marie", "Pierre", null, null, null, DateTime.UtcNow.Date, null, null));
        await new SalaryService(database.Factory, session).AddAsync(new SalaryInput(active.Id, "Base", 1400m, "Monthly", "HTG", new DateTime(2026, 6, 1), null));
        await new SalaryService(database.Factory, session).AddAsync(new SalaryInput(inactive.Id, "Base", 900m, "Monthly", "HTG", new DateTime(2026, 6, 1), null));
        await new EmployeeService(database.Factory, session).DeactivateAsync(inactive.Id);
        var period = await new PayrollPeriodService(database.Factory, session).CreateAsync(new PayrollPeriodInput(new DateTime(2026, 6, 1), new DateTime(2026, 6, 30), new DateTime(2026, 7, 5)));

        var entries = await new PayrollEntryService(database.Factory, session).CalculatePeriodAsync(period.Id);

        var entry = Assert.Single(entries);
        Assert.Equal(active.Id, entry.EmployeeId);
        Assert.Equal(1400m, entry.BaseSalary);
        Assert.Equal(1400m, entry.GrossAmount);
        Assert.Equal(1400m, entry.NetAmount);
        Assert.Equal(PayrollStatuses.Calculated, (await new PayrollPeriodService(database.Factory, session).GetAsync()).Single().Status);
        Assert.Contains(await database.ReadAuditAsync(), audit => audit.EntityName == nameof(PayrollPeriod) && audit.Action == "Calculated" && audit.EntityId == period.Id);
    }

    [Fact]
    public async Task RecalculationRemovesStaleEntriesAndDoesNotDuplicateWhenEmployeeReturns()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var employee = await new EmployeeService(database.Factory, session).AddAsync(new EmployeeInput("EMP-STALE", "Jean", "Pierre", null, null, null, DateTime.UtcNow.Date, null, null));
        await new SalaryService(database.Factory, session).AddAsync(new SalaryInput(employee.Id, "Base", 1200m, "Monthly", "HTG", new DateTime(2026, 6, 1), null));
        var period = await new PayrollPeriodService(database.Factory, session).CreateAsync(new PayrollPeriodInput(new DateTime(2026, 6, 1), new DateTime(2026, 6, 30), new DateTime(2026, 7, 5)));
        var service = new PayrollEntryService(database.Factory, session);

        Assert.Single(await service.CalculatePeriodAsync(period.Id));
        await new EmployeeService(database.Factory, session).DeactivateAsync(employee.Id);
        Assert.Empty(await service.CalculatePeriodAsync(period.Id));
        await using (var inactiveContext = await database.Factory.CreateDbContextAsync())
            Assert.Empty(await inactiveContext.PayrollEntries.Where(entry => entry.PayrollPeriodId == period.Id).ToListAsync());

        await using (var activeContext = await database.Factory.CreateDbContextAsync())
        {
            var storedEmployee = await activeContext.Employees.SingleAsync(item => item.Id == employee.Id);
            storedEmployee.IsActive = true;
            await activeContext.SaveChangesAsync();
        }
        Assert.Single(await service.CalculatePeriodAsync(period.Id));
        await using var finalContext = await database.Factory.CreateDbContextAsync();
        Assert.Equal(1, await finalContext.PayrollEntries.CountAsync(entry => entry.PayrollPeriodId == period.Id && entry.EmployeeId == employee.Id));
    }

    [Fact]
    public async Task CalculatePeriodUsesConfiguredEffectiveTaxRuleWithoutHaitiDefaults()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        await new CompanyService(database.Factory, session).SaveAsync(new CompanyInput("Tax Company", null, null, null, null, null, null, "Haiti", null, null, null, null));
        var employee = await new EmployeeService(database.Factory, session).AddAsync(new EmployeeInput("EMP-TAX", "Jean", "Pierre", null, null, null, DateTime.UtcNow.Date, null, null));
        await new SalaryService(database.Factory, session).AddAsync(new SalaryInput(employee.Id, "Base", 1000m, "Monthly", "HTG", new DateTime(2026, 6, 1), null));
        var taxRules = new TaxRuleService(database.Factory, session);
        await taxRules.AddAsync(new TaxRuleInput("Income", "Haiti", new DateTime(2026, 1, 1), new DateTime(2026, 6, 30), null, null, "Employee", false, "Not legally verified", null, 1));
        await taxRules.AddAsync(new TaxRuleInput("Income", "Haiti", new DateTime(2026, 7, 1), null, 0.1m, 100m, "Employee", true, "Configured test rule", "test-reference", 2));
        var period = await new PayrollPeriodService(database.Factory, session).CreateAsync(new PayrollPeriodInput(new DateTime(2026, 7, 1), new DateTime(2026, 7, 31), new DateTime(2026, 8, 5)));

        var entries = await new PayrollEntryService(database.Factory, session).CalculatePeriodAsync(period.Id);

        var entry = Assert.Single(entries);
        Assert.Equal(90m, entry.EmployeeTaxAmount);
        Assert.Equal(910m, entry.NetAmount);
    }

    [Fact]
    public async Task SaveCalculatedRejectsCallerSuppliedSalaryGrossAndTaxValues()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        await new CompanyService(database.Factory, session).SaveAsync(new CompanyInput("Tax Company", null, null, null, null, null, null, "Haiti", null, null, null, null));
        await new TaxRuleService(database.Factory, session).AddAsync(new TaxRuleInput("Income", "Haiti", new DateTime(2026, 7, 1), null, 0.1m, 100m, "Employee", true, "Configured test rule", "test-reference", 1));
        var employee = await new EmployeeService(database.Factory, session).AddAsync(new EmployeeInput("EMP-TAMPER", "Jean", "Pierre", null, null, null, DateTime.UtcNow.Date, null, null));
        await new SalaryService(database.Factory, session).AddAsync(new SalaryInput(employee.Id, "Base", 1000m, "Monthly", "HTG", new DateTime(2026, 7, 1), null));
        var period = await new PayrollPeriodService(database.Factory, session).CreateAsync(new PayrollPeriodInput(new DateTime(2026, 7, 1), new DateTime(2026, 7, 31), new DateTime(2026, 8, 5)));
        var service = new PayrollEntryService(database.Factory, session);
        var valid = new PayrollEntryInput(period.Id, employee.Id, 1000m, 1100m, 1100m, 100m, 100m, 0m, 900m, [new PayrollComponentInput("Earning", "Allowance", 100m, null), new PayrollComponentInput("Deduction", "Advance", 100m, null)]);

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveCalculatedAsync(valid with { BaseSalary = 900m }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveCalculatedAsync(valid with { GrossAmount = 1200m }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveCalculatedAsync(valid with { EmployeeTaxAmount = 99m, NetAmount = 901m }));
    }

    [Fact]
    public async Task LibrarianCannotCalculateOrSavePayrollEntry()
    {
        await using var database = await TestDatabase.CreateAsync();
        var administratorSession = await database.CreateAdminSessionAsync();
        var authentication = new AuthenticationService(database.Factory, new PasswordHasher<User>());
        var employee = await new EmployeeService(database.Factory, administratorSession).AddAsync(new EmployeeInput("EMP-PAYROLL-LIB", "Lina", "Louis", null, null, null, DateTime.UtcNow.Date, "Librarian", null));
        await authentication.CreateAccountAsync(employee.Id, "payroll-librarian", "correct horse battery staple", 2, new AuthorizationService(administratorSession));
        var librarianSession = new AuthenticationSession();
        librarianSession.SignIn(await authentication.AuthenticateAsync("payroll-librarian", "correct horse battery staple") ?? throw new InvalidOperationException());
        var period = await new PayrollPeriodService(database.Factory, administratorSession).CreateAsync(new PayrollPeriodInput(new DateTime(2026, 12, 1), new DateTime(2026, 12, 31), new DateTime(2027, 1, 5)));
        var service = new PayrollEntryService(database.Factory, librarianSession);

        await Assert.ThrowsAsync<AuthorizationException>(() => service.CalculatePeriodAsync(period.Id));
        await Assert.ThrowsAsync<AuthorizationException>(() => service.SaveCalculatedAsync(new PayrollEntryInput(period.Id, employee.Id, 0m, 0m, 0m, 0m, 0m, 0m, 0m, [])));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string path;
        public IDbContextFactory<LibraryDbContext> Factory { get; }
        private TestDatabase(string path, IDbContextFactory<LibraryDbContext> factory) { this.path = path; Factory = factory; }
        public static async Task<TestDatabase> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"library-payroll-entry-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<LibraryDbContext>().UseSqlite($"Data Source={databasePath}").Options;
            var factory = new TestFactory(options); await using var context = await factory.CreateDbContextAsync(); await context.Database.MigrateAsync(); return new TestDatabase(databasePath, factory);
        }
        public async Task<AuthenticationSession> CreateAdminSessionAsync()
        {
            var auth = new AuthenticationService(Factory, new PasswordHasher<User>()); var session = new AuthenticationSession(); session.SignIn(await auth.CreateInitialAdminAsync("admin", "correct horse battery staple")); return session;
        }
        public async Task<IReadOnlyList<AuditLog>> ReadAuditAsync()
        {
            await using var context = await Factory.CreateDbContextAsync(); return await context.AuditLogs.AsNoTracking().ToListAsync();
        }
        public async ValueTask DisposeAsync() { await using var context = await Factory.CreateDbContextAsync(); await context.Database.CloseConnectionAsync(); SqliteConnection.ClearAllPools(); File.Delete(path); }
        private sealed class TestFactory(DbContextOptions<LibraryDbContext> options) : IDbContextFactory<LibraryDbContext>
        { public LibraryDbContext CreateDbContext() => new(options); public Task<LibraryDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new LibraryDbContext(options)); }
    }
}
