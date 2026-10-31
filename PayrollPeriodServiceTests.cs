using GestionBibliotheque.Data;
using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class PayrollPeriodServiceTests
{
    [Fact]
    public async Task PeriodFollowsAuditedLifecycle()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var service = new PayrollPeriodService(database.Factory, session);
        var period = await service.CreateAsync(new PayrollPeriodInput(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), new DateTime(2026, 2, 5)));

        await service.MarkCalculatedAsync(period.Id);
        await service.MarkReviewedAsync(period.Id);
        await ReauthenticateForPayrollApprovalAsync(database, session);
        await service.ApproveAsync(period.Id);
        await service.MarkPaidAsync(period.Id);
        await service.CloseAsync(period.Id);

        await using var context = await database.Factory.CreateDbContextAsync();
        var stored = await context.PayrollPeriods.SingleAsync(item => item.Id == period.Id);
        Assert.Equal(PayrollStatuses.Closed, stored.Status);
        Assert.Equal(6L, stored.Version);
        Assert.NotNull(stored.ClosedAtUtc);
        Assert.Equal(session.CurrentUser!.Id, stored.ClosedByUserId);
        Assert.True(await context.AuditLogs.CountAsync(item => item.EntityName == nameof(PayrollPeriod)) >= 6);
    }

    [Fact]
    public async Task PayrollApprovalRequiresOneTimeReauthentication()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var service = new PayrollPeriodService(database.Factory, session);
        var period = await service.CreateAsync(new PayrollPeriodInput(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), new DateTime(2026, 2, 5)));
        await service.MarkCalculatedAsync(period.Id);
        await service.MarkReviewedAsync(period.Id);

        await Assert.ThrowsAsync<ReauthenticationRequiredException>(() => service.ApproveAsync(period.Id));
        Assert.True(await new AuthenticationService(database.Factory, new PasswordHasher<User>(), session)
            .ReauthenticateForPayrollApprovalAsync("correct horse battery staple"));
        await service.ApproveAsync(period.Id);
        await Assert.ThrowsAsync<ReauthenticationRequiredException>(() => service.ApproveAsync(period.Id));

        await using var context = await database.Factory.CreateDbContextAsync();
        Assert.Equal(PayrollStatuses.Approved, (await context.PayrollPeriods.SingleAsync(item => item.Id == period.Id)).Status);
    }

    [Fact]
    public async Task PayrollApprovalReauthenticationRejectsWrongPasswordAndDoesNotLogIt()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var authentication = new AuthenticationService(database.Factory, new PasswordHasher<User>(), session);

        Assert.False(await authentication.ReauthenticateForPayrollApprovalAsync("wrong secret"));
        await Assert.ThrowsAsync<ReauthenticationRequiredException>(() => new PayrollPeriodService(database.Factory, session)
            .ApproveAsync(1));

        var audits = await database.ReadAuditAsync();
        var audit = Assert.Single(audits, item => item.Action == "ReauthenticationFailed");
        Assert.Contains("Purpose=PayrollApprove", audit.Details);
        Assert.DoesNotContain("wrong secret", string.Join(" ", audits.Select(item => item.Details)));
    }

    [Fact]
    public async Task InvalidTransitionAndDuplicatePeriodAreRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var service = new PayrollPeriodService(database.Factory, session);
        var input = new PayrollPeriodInput(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28), new DateTime(2026, 3, 5));
        var period = await service.CreateAsync(input);

        await ReauthenticateForPayrollApprovalAsync(database, session);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(period.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(input));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CloseAsync(period.Id));
    }

    [Fact]
    public async Task PeriodRequiresAuthentication()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new PayrollPeriodService(database.Factory, new AuthenticationSession());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAsync());
    }

    [Fact]
    public async Task LifecycleTransitionRollsBackPeriodEntryAndAuditWhenAuditInsertFails()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var employee = await new EmployeeService(database.Factory, session).AddAsync(new EmployeeInput("EMP-ROLLBACK", "Jean", "Pierre", null, null, null, DateTime.UtcNow.Date, null, null));
        await new SalaryService(database.Factory, session).AddAsync(new SalaryInput(employee.Id, "Base", 1000m, "Monthly", "HTG", new DateTime(2026, 3, 1), null));
        var period = await new PayrollPeriodService(database.Factory, session).CreateAsync(new PayrollPeriodInput(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31), new DateTime(2026, 4, 5)));
        var payrollEntryService = new PayrollEntryService(database.Factory, session);
        await payrollEntryService.CalculatePeriodAsync(period.Id);
        await using (var triggerContext = await database.Factory.CreateDbContextAsync())
        {
            await triggerContext.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailReviewedAudit BEFORE INSERT ON AuditLogs WHEN NEW.Action = 'Reviewed' BEGIN SELECT RAISE(ABORT, 'forced audit failure'); END;");
        }

        await Assert.ThrowsAnyAsync<Exception>(() => new PayrollPeriodService(database.Factory, session).MarkReviewedAsync(period.Id));

        await using var context = await database.Factory.CreateDbContextAsync();
        var storedPeriod = await context.PayrollPeriods.SingleAsync(item => item.Id == period.Id);
        var storedEntry = await context.PayrollEntries.SingleAsync(item => item.PayrollPeriodId == period.Id && item.EmployeeId == employee.Id);
        Assert.Equal(PayrollStatuses.Calculated, storedPeriod.Status);
        Assert.Equal(PayrollStatuses.Calculated, storedEntry.Status);
        Assert.DoesNotContain(await context.AuditLogs.Where(item => item.EntityName == nameof(PayrollPeriod) && item.EntityId == period.Id).ToListAsync(), audit => audit.Action == "Reviewed");
    }

    [Fact]
    public async Task StalePayrollPeriodVersionCannotBeCommittedTwice()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var period = await new PayrollPeriodService(database.Factory, session).CreateAsync(new PayrollPeriodInput(new DateTime(2026, 9, 1), new DateTime(2026, 9, 30), new DateTime(2026, 10, 5)));

        await using var firstContext = await database.Factory.CreateDbContextAsync();
        await using var secondContext = await database.Factory.CreateDbContextAsync();
        var first = await firstContext.PayrollPeriods.SingleAsync(item => item.Id == period.Id);
        var second = await secondContext.PayrollPeriods.SingleAsync(item => item.Id == period.Id);
        first.Status = PayrollStatuses.Calculated;
        first.Version++;
        second.Status = PayrollStatuses.Calculated;
        second.Version++;

        await firstContext.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());

        await using var verificationContext = await database.Factory.CreateDbContextAsync();
        var stored = await verificationContext.PayrollPeriods.SingleAsync(item => item.Id == period.Id);
        Assert.Equal(PayrollStatuses.Calculated, stored.Status);
        Assert.Equal(2L, stored.Version);
    }

    [Fact]
    public async Task LibrarianCannotExecutePayrollLifecycleMutations()
    {
        await using var database = await TestDatabase.CreateAsync();
        var administratorSession = await database.CreateAdminSessionAsync();
        var authentication = new AuthenticationService(database.Factory, new PasswordHasher<User>());
        var employee = await new EmployeeService(database.Factory, administratorSession).AddAsync(new EmployeeInput("EMP-LIFECYCLE-LIB", "Lina", "Louis", null, null, null, DateTime.UtcNow.Date, "Librarian", null));
        await authentication.CreateAccountAsync(employee.Id, "lifecycle-librarian", "correct horse battery staple", 2, new AuthorizationService(administratorSession));
        var librarianSession = new AuthenticationSession();
        librarianSession.SignIn(await authentication.AuthenticateAsync("lifecycle-librarian", "correct horse battery staple") ?? throw new InvalidOperationException());
        var service = new PayrollPeriodService(database.Factory, administratorSession);
        var librarianService = new PayrollPeriodService(database.Factory, librarianSession);
        var period = await service.CreateAsync(new PayrollPeriodInput(new DateTime(2026, 11, 1), new DateTime(2026, 11, 30), new DateTime(2026, 12, 5)));

        await Assert.ThrowsAsync<AuthorizationException>(() => librarianService.MarkCalculatedAsync(period.Id));
        await service.MarkCalculatedAsync(period.Id);
        await Assert.ThrowsAsync<AuthorizationException>(() => librarianService.MarkReviewedAsync(period.Id));
        await service.MarkReviewedAsync(period.Id);
        await Assert.ThrowsAsync<AuthorizationException>(() => librarianService.ApproveAsync(period.Id));
        await ReauthenticateForPayrollApprovalAsync(database, administratorSession);
        await service.ApproveAsync(period.Id);
        await Assert.ThrowsAsync<AuthorizationException>(() => librarianService.MarkPaidAsync(period.Id));
        await service.MarkPaidAsync(period.Id);
        await Assert.ThrowsAsync<AuthorizationException>(() => librarianService.CloseAsync(period.Id));
    }

    private static async Task ReauthenticateForPayrollApprovalAsync(TestDatabase database, AuthenticationSession session)
    {
        var authentication = new AuthenticationService(database.Factory, new PasswordHasher<User>(), session);
        Assert.True(await authentication.ReauthenticateForPayrollApprovalAsync("correct horse battery staple"));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string path;
        public IDbContextFactory<LibraryDbContext> Factory { get; }
        private TestDatabase(string path, IDbContextFactory<LibraryDbContext> factory) { this.path = path; Factory = factory; }
        public static async Task<TestDatabase> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"library-payroll-period-{Guid.NewGuid():N}.db");
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
        public async Task<IReadOnlyList<AuditLog>> ReadAuditAsync()
        {
            await using var context = await Factory.CreateDbContextAsync();
            return await context.AuditLogs.AsNoTracking().ToListAsync();
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
