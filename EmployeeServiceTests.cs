using GestionBibliotheque.Data;
using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class EmployeeServiceTests
{
    [Fact]
    public async Task EmployeeCrudAndAccountIdentityWorkAgainstSqlite()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var service = new EmployeeService(database.Factory, session);
        var employee = await service.AddAsync(new EmployeeInput("EMP-001", "Marie", "Joseph", "555-0101", "marie@example.com", "Rue 1", DateTime.UtcNow.Date, "Bibliothécaire", "Service public"));

        Assert.Equal("Marie Joseph", employee.FullName);
        Assert.Single(await service.GetEmployeesAsync("marie"));
        await service.UpdateAsync(employee.Id, new EmployeeInput("EMP-001", "Maria", "Joseph", null, null, null, DateTime.UtcNow.Date, "Responsable", null));
        Assert.Equal("Maria", (await service.GetEmployeesAsync()).Single(candidate => candidate.EmployeeNumber == "EMP-001").FirstName);

        await service.DeactivateAsync(employee.Id);
        Assert.DoesNotContain(await service.GetEmployeesAsync(), candidate => candidate.EmployeeNumber == "EMP-001");
        await using var context = await database.CreateDbContextAsync();
        Assert.False(await context.Employees.Where(candidate => candidate.Id == employee.Id).Select(candidate => candidate.IsActive).SingleAsync());
    }

    [Fact]
    public async Task DuplicateEmployeeNumberAndInvalidDateAreRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new EmployeeService(database.Factory, await database.CreateAdminSessionAsync());
        await service.AddAsync(new EmployeeInput("EMP-002", "Jean", "Louis", null, null, null, DateTime.UtcNow.Date, null, null));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddAsync(new EmployeeInput("EMP-002", "Other", "Person", null, null, null, DateTime.UtcNow.Date, null, null)));
        await Assert.ThrowsAsync<ArgumentException>(() => service.AddAsync(new EmployeeInput("EMP-003", "Other", "Person", null, null, null, DateTime.UtcNow.Date.AddDays(1), null, null)));
    }

    [Fact]
    public async Task ServiceAuthorizationRejectsUnauthenticatedWrite()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new EmployeeService(database.Factory, new AuthenticationSession());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddAsync(new EmployeeInput("EMP-003", "Other", "Person", null, null, null, DateTime.UtcNow.Date, null, null)));
    }

    [Fact]
    public async Task InitialAdministratorIsLinkedToAnEmployee()
    {
        await using var database = await TestDatabase.CreateAsync();
        var auth = new AuthenticationService(database.Factory, new PasswordHasher<User>());
        var user = await auth.CreateInitialAdminAsync("admin", "correct horse battery staple");
        Assert.NotNull(user.Employee);
        Assert.Equal("Administrator", user.Employee!.LastName);
        await using var context = await database.CreateDbContextAsync();
        Assert.Equal(user.EmployeeId, await context.Users.Where(candidate => candidate.Id == user.Id).Select(candidate => candidate.EmployeeId).SingleAsync());
    }

    [Fact]
    public async Task AdministratorCanCreateAccountForAnActiveEmployee()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var employee = await new EmployeeService(database.Factory, session).AddAsync(new EmployeeInput("EMP-004", "Paul", "Jean", null, null, null, DateTime.UtcNow.Date, "Assistant", null));
        var auth = new AuthenticationService(database.Factory, new PasswordHasher<User>());
        var account = await auth.CreateAccountAsync(employee.Id, "paul", "correct horse battery staple", 2, new AuthorizationService(session));

        Assert.Equal(employee.Id, account.EmployeeId);
        Assert.NotNull(await auth.AuthenticateAsync("paul", "correct horse battery staple"));
    }

    [Fact]
    public async Task InactiveEmployeeCannotAuthenticate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var employee = await new EmployeeService(database.Factory, session).AddAsync(new EmployeeInput("EMP-005", "Paul", "Jean", null, null, null, DateTime.UtcNow.Date, "Assistant", null));
        var auth = new AuthenticationService(database.Factory, new PasswordHasher<User>());
        await auth.CreateAccountAsync(employee.Id, "paul", "correct horse battery staple", 2, new AuthorizationService(session));
        await new EmployeeService(database.Factory, session).DeactivateAsync(employee.Id);

        Assert.Null(await auth.AuthenticateAsync("paul", "correct horse battery staple"));
    }

    [Fact]
    public async Task DeactivatedCurrentEmployeeSessionCannotPerformActions()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        await new EmployeeService(database.Factory, session).DeactivateAsync(session.CurrentUser!.EmployeeId!.Value);

        await Assert.ThrowsAsync<InvalidOperationException>(() => new BookService(database.Factory, session).GetBooksAsync());
    }

    [Fact]
    public async Task LibrarianCannotCreateEmployee()
    {
        await using var database = await TestDatabase.CreateAsync();
        var adminSession = await database.CreateAdminSessionAsync();
        var auth = new AuthenticationService(database.Factory, new PasswordHasher<User>());
        var librarianEmployee = await new EmployeeService(database.Factory, adminSession).AddAsync(new EmployeeInput("EMP-006", "Lina", "Louis", null, null, null, DateTime.UtcNow.Date, "Librarian", null));
        var librarian = await auth.CreateAccountAsync(librarianEmployee.Id, "lina", "correct horse battery staple", 2, new AuthorizationService(adminSession));
        var librarianSession = new AuthenticationSession(); librarianSession.SignIn(await auth.AuthenticateAsync("lina", "correct horse battery staple") ?? librarian);

        await Assert.ThrowsAsync<AuthorizationException>(() => new EmployeeService(database.Factory, librarianSession).AddAsync(new EmployeeInput("EMP-007", "Other", "Person", null, null, null, DateTime.UtcNow.Date, null, null)));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string path;
        public IDbContextFactory<LibraryDbContext> Factory { get; }
        private TestDatabase(string path, IDbContextFactory<LibraryDbContext> factory) { this.path = path; Factory = factory; }
        public static async Task<TestDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"library-employees-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<LibraryDbContext>().UseSqlite($"Data Source={path}").Options;
            var factory = new TestFactory(options);
            await using var context = await factory.CreateDbContextAsync(); await context.Database.MigrateAsync();
            return new TestDatabase(path, factory);
        }
        public async Task<AuthenticationSession> CreateAdminSessionAsync()
        {
            var auth = new AuthenticationService(Factory, new PasswordHasher<User>());
            var session = new AuthenticationSession(); session.SignIn(await auth.CreateInitialAdminAsync("admin", "correct horse battery staple")); return session;
        }
        public Task<LibraryDbContext> CreateDbContextAsync() => Factory.CreateDbContextAsync();
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
