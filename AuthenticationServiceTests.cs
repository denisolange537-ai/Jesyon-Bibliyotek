using GestionBibliotheque.Data;
using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public async Task InitialAdminIsHashedAndCanAuthenticateAgainstSqlite()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateAuthenticationService();

        var user = await service.CreateInitialAdminAsync(" Admin ", "correct horse battery staple");

        Assert.Equal("admin", user.Username);
        Assert.NotEqual("correct horse battery staple", user.PasswordHash);
        Assert.NotNull(await service.AuthenticateAsync("ADMIN", "correct horse battery staple"));
        Assert.Null(await service.AuthenticateAsync("admin", "wrong password"));
    }

    [Fact]
    public async Task InitialAdminCannotBeCreatedTwice()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateAuthenticationService();
        await service.CreateInitialAdminAsync("admin", "correct horse battery staple");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateInitialAdminAsync("second", "another correct password"));
    }

    [Fact]
    public async Task UnknownUsernameCannotAuthenticate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateAuthenticationService();
        await service.CreateInitialAdminAsync("admin", "correct horse battery staple");

        Assert.Null(await service.AuthenticateAsync("unknown", "correct horse battery staple"));
    }

    [Fact]
    public async Task RepeatedFailuresLockAccountAndRecordSecurityEvents()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateAuthenticationService();
        var user = await service.CreateInitialAdminAsync("admin", "correct horse battery staple");

        for (var attempt = 0; attempt < AuthenticationService.MaximumFailedLoginAttempts; attempt++)
            Assert.Null(await service.AuthenticateAsync("admin", "wrong password"));

        Assert.Null(await service.AuthenticateAsync("admin", "correct horse battery staple"));

        await using var context = await database.CreateContextAsync();
        var lockedUser = await context.Users.SingleAsync(candidate => candidate.Id == user.Id);
        Assert.Equal(AuthenticationService.MaximumFailedLoginAttempts, lockedUser.FailedLoginAttempts);
        Assert.NotNull(lockedUser.LockoutEndUtc);
        Assert.True(lockedUser.LockoutEndUtc > DateTime.UtcNow);
        Assert.Equal(1, lockedUser.ConsecutiveLockouts);
        Assert.Equal(5, await context.AuditLogs.CountAsync(log => log.Action == "AuthenticationFailed"));
        Assert.Equal(1, await context.AuditLogs.CountAsync(log => log.Action == "AccountLocked"));
        Assert.Equal(1, await context.AuditLogs.CountAsync(log => log.Action == "AuthenticationRejected"));
    }

    [Fact]
    public async Task SuccessfulLoginClearsFailedAttemptsAndLockout()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateAuthenticationService();
        var user = await service.CreateInitialAdminAsync("admin", "correct horse battery staple");

        await using (var context = await database.CreateContextAsync())
        {
            var storedUser = await context.Users.SingleAsync(candidate => candidate.Id == user.Id);
            storedUser.FailedLoginAttempts = 3;
            storedUser.FailureWindowStartedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        Assert.NotNull(await service.AuthenticateAsync("admin", "correct horse battery staple"));

        await using var verifyContext = await database.CreateContextAsync();
        var authenticatedUser = await verifyContext.Users.SingleAsync(candidate => candidate.Id == user.Id);
        Assert.Equal(0, authenticatedUser.FailedLoginAttempts);
        Assert.Null(authenticatedUser.FailureWindowStartedAtUtc);
        Assert.Null(authenticatedUser.LockoutEndUtc);
        Assert.Contains(await verifyContext.AuditLogs.ToListAsync(), log => log.Action == "AuthenticationSucceeded");
    }

    [Fact]
    public async Task UnknownUsernameUsesGenericAuditWithoutStoringUsername()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateAuthenticationService();
        await service.CreateInitialAdminAsync("admin", "correct horse battery staple");

        Assert.Null(await service.AuthenticateAsync("private-name", "wrong password"));

        await using var context = await database.CreateContextAsync();
        var entry = await context.AuditLogs.SingleAsync(log => log.Action == "AuthenticationFailed");
        Assert.Null(entry.UserId);
        Assert.Equal(0, entry.EntityId);
        Assert.Equal("Invalid credentials.", entry.Details);
        Assert.DoesNotContain("private-name", entry.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConcurrentFailuresCannotBypassLockoutThreshold()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateAuthenticationService();
        var user = await service.CreateInitialAdminAsync("admin", "correct horse battery staple");

        var attempts = await Task.WhenAll(Enumerable.Range(0, AuthenticationService.MaximumFailedLoginAttempts)
            .Select(_ => service.AuthenticateAsync("admin", "wrong password")));

        Assert.All(attempts, result => Assert.Null(result));
        await using var context = await database.CreateContextAsync();
        var lockedUser = await context.Users.SingleAsync(candidate => candidate.Id == user.Id);
        Assert.Equal(AuthenticationService.MaximumFailedLoginAttempts, lockedUser.FailedLoginAttempts);
        Assert.NotNull(lockedUser.LockoutEndUtc);
    }

    [Fact]
    public async Task RepeatedLockoutsIncreaseDelayUpToConfiguredCap()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateAuthenticationService();
        var user = await service.CreateInitialAdminAsync("admin", "correct horse battery staple");

        for (var attempt = 0; attempt < AuthenticationService.MaximumFailedLoginAttempts; attempt++)
            Assert.Null(await service.AuthenticateAsync("admin", "wrong password"));

        await using (var context = await database.CreateContextAsync())
        {
            var storedUser = await context.Users.SingleAsync(candidate => candidate.Id == user.Id);
            storedUser.LockoutEndUtc = DateTime.UtcNow.AddSeconds(-1);
            await context.SaveChangesAsync();
        }

        for (var attempt = 0; attempt < AuthenticationService.MaximumFailedLoginAttempts; attempt++)
            Assert.Null(await service.AuthenticateAsync("admin", "wrong password"));

        await using var verifyContext = await database.CreateContextAsync();
        var relockedUser = await verifyContext.Users.SingleAsync(candidate => candidate.Id == user.Id);
        Assert.Equal(2, relockedUser.ConsecutiveLockouts);
        Assert.NotNull(relockedUser.LockoutEndUtc);
        Assert.InRange(
            (relockedUser.LockoutEndUtc!.Value - relockedUser.LastLockoutAtUtc!.Value).TotalMinutes,
            29.9,
            30.1);
    }

    [Fact]
    public async Task UserCanChangeOwnPasswordAfterVerifyingCurrentPassword()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateAuthenticationService();
        var user = await service.CreateInitialAdminAsync("admin", "correct horse battery staple");
        database.Session.SignIn(user);

        await service.ChangeOwnPasswordAsync("correct horse battery staple", "new secure password phrase");

        Assert.NotNull(await service.AuthenticateAsync("admin", "new secure password phrase"));
        Assert.Null(await service.AuthenticateAsync("admin", "correct horse battery staple"));
        await using var context = await database.CreateContextAsync();
        Assert.Contains(await context.AuditLogs.ToListAsync(), log => log.Action == "PasswordChanged");
    }

    [Fact]
    public async Task AdministratorResetRequiresChangeAndDoesNotAuditTemporaryPassword()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateAuthenticationService();
        var administrator = await service.CreateInitialAdminAsync("admin", "correct horse battery staple");
        database.Session.SignIn(administrator);
        int employeeId;
        await using (var context = await database.CreateContextAsync())
        {
            var employee = new Employee { EmployeeNumber = "EMP-RESET-1", FirstName = "Test", LastName = "Person" };
            context.Employees.Add(employee);
            await context.SaveChangesAsync();
            employeeId = employee.Id;
        }
        var target = await service.CreateAccountAsync(employeeId, "worker", "worker initial password", 2, new AuthorizationService(database.Session));

        var temporaryPassword = await service.ResetUserPasswordAsync(target.Id, new AuthorizationService(database.Session));
        var targetSession = new AuthenticationSession();
        var targetAuthentication = database.CreateAuthenticationService(targetSession);
        var authenticated = await targetAuthentication.AuthenticateAsync("worker", temporaryPassword);

        Assert.NotNull(authenticated);
        Assert.True(authenticated.RequirePasswordChange);
        targetSession.SignIn(authenticated);
        Assert.Throws<InvalidOperationException>(() => new AuthorizationService(targetSession).RequirePermission(Permissions.BooksView));
        await targetAuthentication.ChangeOwnPasswordAsync(temporaryPassword, "worker new secure password");
        new AuthorizationService(targetSession).RequirePermission(Permissions.BooksView);

        await using var verifyContext = await database.CreateContextAsync();
        var auditLogs = await verifyContext.AuditLogs.ToListAsync();
        Assert.DoesNotContain(auditLogs, log => log.Details?.Contains(temporaryPassword, StringComparison.Ordinal) == true);
        var resetAudit = Assert.Single(auditLogs, log => log.Action == "PasswordReset");
        Assert.Equal(administrator.Id, resetAudit.UserId);
        Assert.Equal(target.Id, resetAudit.EntityId);
    }

    [Fact]
    public async Task AdministratorCannotUseResetWorkflowForOwnAccount()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateAuthenticationService();
        var administrator = await service.CreateInitialAdminAsync("admin", "correct horse battery staple");
        database.Session.SignIn(administrator);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ResetUserPasswordAsync(administrator.Id, new AuthorizationService(database.Session)));
    }

    [Fact]
    public async Task ShortPasswordIsRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateAuthenticationService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateInitialAdminAsync("admin", "too-short"));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string _databasePath;
        private readonly IDbContextFactory<LibraryDbContext> _dbContextFactory;

        private TestDatabase(string databasePath, IDbContextFactory<LibraryDbContext> dbContextFactory)
        {
            _databasePath = databasePath;
            _dbContextFactory = dbContextFactory;
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"library-auth-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<LibraryDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            var factory = new TestDbContextFactory(options);
            await using var dbContext = await factory.CreateDbContextAsync();
            await dbContext.Database.MigrateAsync();
            return new TestDatabase(databasePath, factory);
        }

        public AuthenticationService CreateAuthenticationService() =>
            new(_dbContextFactory, new PasswordHasher<User>(), Session);

        public AuthenticationSession Session { get; } = new();

        public AuthenticationService CreateAuthenticationService(AuthenticationSession session) =>
            new(_dbContextFactory, new PasswordHasher<User>(), session);

        public Task<LibraryDbContext> CreateContextAsync() => _dbContextFactory.CreateDbContextAsync();

        public async ValueTask DisposeAsync()
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            await dbContext.Database.CloseConnectionAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(_databasePath);
        }

        private sealed class TestDbContextFactory(DbContextOptions<LibraryDbContext> options)
            : IDbContextFactory<LibraryDbContext>
        {
            public LibraryDbContext CreateDbContext() => new(options);

            public Task<LibraryDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(new LibraryDbContext(options));
        }
    }
}
