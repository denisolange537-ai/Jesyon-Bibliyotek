using GestionBibliotheque.Data;
using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class CompanyManagementTests
{
    [Fact]
    public async Task CompanyDepartmentPositionAndEmployeeAssignmentPersistAndAudit()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var company = await new CompanyService(database.Factory, session).SaveAsync(new CompanyInput("Bibliyotèk Legal", "Bibliyotèk", null, null, "Rue 1", "Pòtoprens", null, "Haiti", null, "contact@example.com", null, null));
        var department = await new DepartmentService(database.Factory, session).AddAsync(new DepartmentInput(company.Id, "LIB", "Sèvis Liv", null));
        var position = await new PositionService(database.Factory, session).AddAsync(new PositionInput(department.Id, "LIB-01", "Bibliyotekè", null));
        var employee = await new EmployeeService(database.Factory, session).AddAsync(new EmployeeInput("EMP-P2", "Jean", "Pierre", null, null, null, DateTime.UtcNow.Date, "Bibliyotekè", "Sèvis Liv", department.Id, position.Id));

        var stored = await new EmployeeService(database.Factory, session).GetEmployeesAsync("EMP-P2");
        var audit = await new AuditService(database.Factory, session).GetRecentAsync();

        Assert.Equal(position.Id, stored.Single().PositionId);
        Assert.Equal(department.Id, stored.Single().DepartmentId);
        Assert.Contains(audit, entry => entry.EntityName == nameof(Company));
        Assert.Contains(audit, entry => entry.EntityName == nameof(Department));
        Assert.Contains(audit, entry => entry.EntityName == nameof(Position));
        Assert.Contains(audit, entry => entry.EntityName == nameof(Employee) && entry.EntityId == employee.Id);
    }

    [Fact]
    public async Task EmployeeRejectsPositionFromAnotherDepartment()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var company = await new CompanyService(database.Factory, session).SaveAsync(InputCompany());
        var firstDepartment = await new DepartmentService(database.Factory, session).AddAsync(new DepartmentInput(company.Id, "ONE", "One", null));
        var secondDepartment = await new DepartmentService(database.Factory, session).AddAsync(new DepartmentInput(company.Id, "TWO", "Two", null));
        var position = await new PositionService(database.Factory, session).AddAsync(new PositionInput(firstDepartment.Id, "P-1", "Position", null));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => new EmployeeService(database.Factory, session).AddAsync(new EmployeeInput("EMP-BAD", "Jean", "Pierre", null, null, null, DateTime.UtcNow.Date, null, null, secondDepartment.Id, position.Id)));
    }

    [Fact]
    public async Task LibrarianCannotManageCompanyStructure()
    {
        await using var database = await TestDatabase.CreateAsync();
        var administratorSession = await database.CreateAdminSessionAsync();
        var company = await new CompanyService(database.Factory, administratorSession).SaveAsync(InputCompany());
        var employee = await new EmployeeService(database.Factory, administratorSession).AddAsync(new EmployeeInput("EMP-LIB", "Lina", "Louis", null, null, null, DateTime.UtcNow.Date, "Librarian", null));
        var auth = new AuthenticationService(database.Factory, new PasswordHasher<User>());
        await auth.CreateAccountAsync(employee.Id, "lina", "correct horse battery staple", 2, new AuthorizationService(administratorSession));
        var librarianSession = new AuthenticationSession();
        librarianSession.SignIn(await auth.AuthenticateAsync("lina", "correct horse battery staple") ?? throw new InvalidOperationException());

        await Assert.ThrowsAsync<AuthorizationException>(() => new DepartmentService(database.Factory, librarianSession).AddAsync(new DepartmentInput(company.Id, "LIB", "Library", null)));
    }

    [Fact]
    public async Task DepartmentWithActivePositionCannotBeDeactivated()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var company = await new CompanyService(database.Factory, session).SaveAsync(InputCompany());
        var department = await new DepartmentService(database.Factory, session).AddAsync(new DepartmentInput(company.Id, "OPS", "Operations", null));
        await new PositionService(database.Factory, session).AddAsync(new PositionInput(department.Id, "OPS-1", "Operator", null));

        await Assert.ThrowsAsync<InvalidOperationException>(() => new DepartmentService(database.Factory, session).DeactivateAsync(department.Id));
    }

    [Fact]
    public async Task DepartmentAndPositionSupportSearchUpdateAndDeactivate()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var company = await new CompanyService(database.Factory, session).SaveAsync(InputCompany());
        var departmentService = new DepartmentService(database.Factory, session);
        var positionService = new PositionService(database.Factory, session);
        var department = await departmentService.AddAsync(new DepartmentInput(company.Id, "ADM", "Administration", null));
        var position = await positionService.AddAsync(new PositionInput(department.Id, "ADM-1", "Assistant", null));

        Assert.Single(await departmentService.GetAsync("admin"));
        Assert.Single(await positionService.GetAsync(department.Id, "assist"));
        await departmentService.UpdateAsync(department.Id, new DepartmentInput(company.Id, "ADM", "Administration Updated", null));
        await positionService.UpdateAsync(position.Id, new PositionInput(department.Id, "ADM-1", "Assistant Updated", null));
        await positionService.DeactivateAsync(position.Id);
        await departmentService.DeactivateAsync(department.Id);

        Assert.Empty(await departmentService.GetAsync());
        Assert.Empty(await positionService.GetAsync());
    }

    private static CompanyInput InputCompany() => new("Test Company", null, null, null, null, null, null, "Haiti", null, null, null, null);

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string path;
        public IDbContextFactory<LibraryDbContext> Factory { get; }
        private TestDatabase(string path, IDbContextFactory<LibraryDbContext> factory) { this.path = path; Factory = factory; }
        public static async Task<TestDatabase> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"library-company-{Guid.NewGuid():N}.db");
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
