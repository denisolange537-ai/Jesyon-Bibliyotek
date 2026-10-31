using GestionBibliotheque.Data;
using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class AuthorizationServiceTests
{
    [Fact]
    public void UnauthenticatedSessionCannotAuthorize()
    {
        var authorization = new AuthorizationService(new AuthenticationSession());

        Assert.Throws<InvalidOperationException>(() => authorization.RequirePermission(Permissions.BooksView));
    }

    [Fact]
    public void InactiveCurrentUserCannotAuthorize()
    {
        var session = new AuthenticationSession();
        session.SignIn(new User
        {
            Username = "inactive",
            PasswordHash = "not-used",
            IsActive = false,
            Role = CreateRole("Administrator", Permissions.SettingsManage),
            Employee = new Employee
            {
                EmployeeNumber = "EMP-INACTIVE-AUTH",
                FirstName = "Inactive",
                LastName = "User",
                EmploymentStatus = "Active",
                IsActive = true
            }
        });
        var authorization = new AuthorizationService(session);

        Assert.Throws<InvalidOperationException>(() => authorization.RequirePermission(Permissions.SettingsManage));
    }

    [Fact]
    public void InactiveCurrentEmployeeCannotAuthorize()
    {
        var session = new AuthenticationSession();
        session.SignIn(new User
        {
            Username = "inactive-employee",
            PasswordHash = "not-used",
            Role = CreateRole("Administrator", Permissions.SettingsManage),
            Employee = new Employee
            {
                EmployeeNumber = "EMP-INACTIVE-EMPLOYEE-AUTH",
                FirstName = "Inactive",
                LastName = "Employee",
                EmploymentStatus = "Inactive",
                IsActive = false
            }
        });
        var authorization = new AuthorizationService(session);

        Assert.Throws<InvalidOperationException>(() => authorization.RequirePermission(Permissions.SettingsManage));
    }

    [Fact]
    public void LibrarianCannotClaimAdministratorPermission()
    {
        var session = new AuthenticationSession();
        session.SignIn(new User
        {
            Username = "librarian",
            PasswordHash = "not-used",
            Role = CreateRole("Librarian", Permissions.BooksView),
            Employee = new Employee
            {
                EmployeeNumber = "EMP-LIB-AUTH",
                FirstName = "Library",
                LastName = "User",
                EmploymentStatus = "Active"
            }
        });
        var authorization = new AuthorizationService(session);

        Assert.True(authorization.HasPermission(Permissions.BooksView));
        Assert.False(authorization.HasPermission(Permissions.UsersCreate));
        Assert.Throws<AuthorizationException>(() => authorization.RequirePermission(Permissions.UsersCreate));
    }

    [Fact]
    public void LogoutClearsAuthenticationAndAuthorizationState()
    {
        var session = new AuthenticationSession();
        session.SignIn(new User
        {
            Username = "admin",
            PasswordHash = "not-used",
            Role = CreateRole("Administrator", Permissions.SettingsManage),
            Employee = new Employee
            {
                EmployeeNumber = "EMP-LOGOUT-AUTH",
                FirstName = "Admin",
                LastName = "User",
                EmploymentStatus = "Active"
            }
        });
        var authorization = new AuthorizationService(session);
        Assert.True(authorization.HasPermission(Permissions.SettingsManage));

        session.SignOut();

        Assert.False(session.IsAuthenticated);
        Assert.Throws<InvalidOperationException>(() => authorization.RequirePermission(Permissions.SettingsManage));
    }

    [Fact]
    public async Task InactiveUserCannotAuthenticateAgainstSqlite()
    {
        await using var database = await TestDatabase.CreateAsync();
        var authentication = new AuthenticationService(database.Factory, new PasswordHasher<User>());
        var user = await authentication.CreateInitialAdminAsync("admin", "correct horse battery staple");
        await using (var context = await database.Factory.CreateDbContextAsync())
        {
            var stored = await context.Users.SingleAsync(item => item.Id == user.Id);
            stored.IsActive = false;
            await context.SaveChangesAsync();
        }

        Assert.Null(await authentication.AuthenticateAsync("admin", "correct horse battery staple"));
    }

    [Fact]
    public async Task MigratedRolePermissionsAreLoadedFromDatabaseAndUnknownRoleDeniesByDefault()
    {
        await using var database = await TestDatabase.CreateAsync();
        var authentication = new AuthenticationService(database.Factory, new PasswordHasher<User>());
        await authentication.CreateInitialAdminAsync("admin", "correct horse battery staple");
        var session = new AuthenticationSession();
        var signedInAdmin = await authentication.AuthenticateAsync("admin", "correct horse battery staple");
        Assert.NotNull(signedInAdmin);
        session.SignIn(signedInAdmin);
        var authorization = new AuthorizationService(session);

        Assert.True(authorization.HasPermission(Permissions.RolesManage));
        Assert.True(authorization.HasPermission(Permissions.PayrollApprove));

        await using var context = await database.Factory.CreateDbContextAsync();
        var customRole = new Role { Name = "Custom" };
        context.Roles.Add(customRole);
        await context.SaveChangesAsync();
        var employee = new Employee
        {
            EmployeeNumber = "EMP-CUSTOM-AUTH",
            FirstName = "Custom",
            LastName = "Role",
            EmploymentStatus = "Active"
        };
        var customUser = new User
        {
            Username = "custom",
            PasswordHash = string.Empty,
            RoleId = customRole.Id,
            Employee = employee
        };
        customUser.PasswordHash = new PasswordHasher<User>().HashPassword(customUser, "correct horse battery staple");
        context.Users.Add(customUser);
        await context.SaveChangesAsync();
        var signedIn = await authentication.AuthenticateAsync("custom", "correct horse battery staple");
        Assert.NotNull(signedIn);
        var customSession = new AuthenticationSession();
        customSession.SignIn(signedIn);
        Assert.Empty(new AuthorizationService(customSession).GetPermissions());
    }

    [Fact]
    public async Task PersistedDefaultAndCustomRolesMatchTheCompletePermissionMatrix()
    {
        await using var database = await TestDatabase.CreateAsync();
        var authentication = new AuthenticationService(database.Factory, new PasswordHasher<User>());
        await authentication.CreateInitialAdminAsync("admin", "correct horse battery staple");

        var expectedLibrarianPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Permissions.BooksView, Permissions.BooksCreate, Permissions.BooksEdit,
            Permissions.MembersView, Permissions.MembersCreate, Permissions.MembersEdit,
            Permissions.BorrowingView, Permissions.BorrowingCreate,
            Permissions.ReservationsView, Permissions.ReservationsCreate, Permissions.ReservationsCancel,
            Permissions.ReportsView
        };

        await using (var context = await database.Factory.CreateDbContextAsync())
        {
            var customRole = new Role { Name = "No grants" };
            context.Roles.Add(customRole);
            await context.SaveChangesAsync();
        }

        await using var rolesContext = await database.Factory.CreateDbContextAsync();
        var roles = await rolesContext.Roles.Include(role => role.Permissions).ToDictionaryAsync(role => role.Name);
        var adminPermissions = roles["Administrator"].Permissions.Select(permission => permission.PermissionName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var librarianPermissions = roles["Librarian"].Permissions.Select(permission => permission.PermissionName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var customPermissions = roles["No grants"].Permissions.Select(permission => permission.PermissionName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(Permissions.Catalog, adminPermissions);
        Assert.Equal(expectedLibrarianPermissions, librarianPermissions);
        Assert.Empty(customPermissions);

        AssertRoleMatrix("Administrator", adminPermissions);
        AssertRoleMatrix("Librarian", expectedLibrarianPermissions);
        AssertRoleMatrix("No grants", customPermissions);

        void AssertRoleMatrix(string roleName, IReadOnlySet<string> expectedPermissions)
        {
            var role = roles[roleName];
            var session = new AuthenticationSession();
            session.SignIn(new User
            {
                Username = $"matrix-{roleName}",
                PasswordHash = "not-used",
                Role = role,
                Employee = new Employee
                {
                    EmployeeNumber = $"EMP-MATRIX-{roleName}",
                    FirstName = "Matrix",
                    LastName = roleName,
                    EmploymentStatus = "Active"
                }
            });
            var authorization = new AuthorizationService(session);

            foreach (var permission in Permissions.Catalog)
            {
                Assert.Equal(expectedPermissions.Contains(permission), authorization.HasPermission(permission));
                if (expectedPermissions.Contains(permission))
                    authorization.RequirePermission(permission);
                else
                    Assert.Throws<AuthorizationException>(() => authorization.RequirePermission(permission));
            }
        }
    }

    [Fact]
    public async Task RoleAdministrationPersistsGrantsAuditsAndProtectsAdministratorRole()
    {
        await using var database = await TestDatabase.CreateAsync();
        var authentication = new AuthenticationService(database.Factory, new PasswordHasher<User>());
        var admin = await authentication.CreateInitialAdminAsync("admin", "correct horse battery staple");
        var session = new AuthenticationSession();
        session.SignIn(admin);
        var service = new RoleAdministrationService(database.Factory, new AuthorizationService(session), session);

        var role = await service.CreateAsync("Circulation", []);
        await service.UpdateGrantsAsync(role.Id, [Permissions.BooksView, Permissions.BorrowingView]);
        var persisted = (await service.GetRolesAsync()).Single(item => item.Id == role.Id);
        Assert.Equal(2, persisted.Permissions.Count);

        await using var context = await database.Factory.CreateDbContextAsync();
        var adminRole = await context.Roles.SingleAsync(item => item.Name == "Administrator");
        var librarianRole = await context.Roles.SingleAsync(item => item.Name == "Librarian");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateGrantsAsync(adminRole.Id, []));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateGrantsAsync(librarianRole.Id, []));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync("administrator", []));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateGrantsAsync(role.Id, ["MadeUp.Permission"]));
        Assert.Contains(await context.AuditLogs.ToListAsync(), audit => audit.Action == "RoleCreated" && audit.EntityId == role.Id);
        Assert.Contains(await context.AuditLogs.ToListAsync(), audit => audit.Action == "RoleGrantsUpdated" && audit.EntityId == role.Id);
    }

    [Fact]
    public async Task RestrictedRoleCannotBypassServiceAuthorizationByCallingServicesDirectly()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = new AuthenticationSession();
        session.SignIn(new User
        {
            Username = "circulation",
            PasswordHash = "not-used",
            Role = CreateRole("Circulation", Permissions.BooksView),
            Employee = new Employee
            {
                EmployeeNumber = "EMP-DIRECT-GATE",
                FirstName = "Circulation",
                LastName = "Operator",
                EmploymentStatus = "Active"
            }
        });

        var book = new BookService(database.Factory, session);
        var member = new MemberService(database.Factory, session);
        var borrowing = new BorrowingService(database.Factory, session);
        var reservation = new ReservationService(database.Factory, session);
        var returns = new ReturnService(database.Factory, session);
        var employees = new EmployeeService(database.Factory, session);
        var company = new CompanyService(database.Factory, session);
        var department = new DepartmentService(database.Factory, session);
        var position = new PositionService(database.Factory, session);
        var salary = new SalaryService(database.Factory, session);
        var settings = new LibrarySettingsService(database.Factory, session);
        var backup = new BackupService("unused.db", database.Factory, session);
        var audit = new AuditService(database.Factory, session);
        var taxRules = new TaxRuleService(database.Factory, session);
        var payrollPeriods = new PayrollPeriodService(database.Factory, session);
        var payrollReports = new PayrollReportService(database.Factory, session);
        var reports = new ReportService(database.Factory, session);
        var dashboard = new DashboardService(database.Factory, session);
        var authorization = new AuthorizationService(session);
        var authentication = new AuthenticationService(database.Factory, new PasswordHasher<User>(), session);
        var roleAdministration = new RoleAdministrationService(database.Factory, authorization, session);
        var payrollEntries = new PayrollEntryService(database.Factory, session);
        var payslips = new PayslipService(database.Factory, session);

        Assert.Empty(await book.GetBooksAsync());
        await Assert.ThrowsAsync<AuthorizationException>(() => book.AddAsync(new BookInput("x", "x", "x", null, null, 1)));
        await Assert.ThrowsAsync<AuthorizationException>(() => book.UpdateAsync(1, new BookInput("x", "x", "x", null, null, 1)));
        await Assert.ThrowsAsync<AuthorizationException>(() => book.DeactivateAsync(1));
        await Assert.ThrowsAsync<AuthorizationException>(() => member.AddAsync(new MemberInput("x", "x", "x", null, null, null)));
        await Assert.ThrowsAsync<AuthorizationException>(() => member.GetMembersAsync());
        await Assert.ThrowsAsync<AuthorizationException>(() => member.UpdateAsync(1, new MemberInput("x", "x", "x", null, null, null)));
        await Assert.ThrowsAsync<AuthorizationException>(() => member.DeactivateAsync(1));
        await Assert.ThrowsAsync<AuthorizationException>(() => borrowing.GetOpenBorrowingsAsync());
        await Assert.ThrowsAsync<AuthorizationException>(() => borrowing.BorrowAsync(1, 1));
        await Assert.ThrowsAsync<AuthorizationException>(() => reservation.GetOpenAsync());
        await Assert.ThrowsAsync<AuthorizationException>(() => reservation.ReserveAsync(1, 1));
        await Assert.ThrowsAsync<AuthorizationException>(() => reservation.CancelAsync(1));
        await Assert.ThrowsAsync<AuthorizationException>(() => returns.ReturnAsync(1));
        await Assert.ThrowsAsync<AuthorizationException>(() => returns.PayFineAsync(1, 1m));
        await Assert.ThrowsAsync<AuthorizationException>(() => returns.GetOutstandingFinesAsync());
        await Assert.ThrowsAsync<AuthorizationException>(() => employees.AddAsync(new EmployeeInput("x", "x", "x", null, null, null, DateTime.UtcNow, null, null)));
        await Assert.ThrowsAsync<AuthorizationException>(() => employees.UpdateAsync(1, new EmployeeInput("x", "x", "x", null, null, null, DateTime.UtcNow, null, null)));
        await Assert.ThrowsAsync<AuthorizationException>(() => employees.DeactivateAsync(1));
        await Assert.ThrowsAsync<AuthorizationException>(() => company.SaveAsync(new CompanyInput("x", null, null, null, null, null, null, "HT", null, null, null, null)));
        await Assert.ThrowsAsync<AuthorizationException>(() => department.AddAsync(new DepartmentInput(1, "x", "x", null)));
        await Assert.ThrowsAsync<AuthorizationException>(() => department.UpdateAsync(1, new DepartmentInput(1, "x", "x", null)));
        await Assert.ThrowsAsync<AuthorizationException>(() => department.DeactivateAsync(1));
        await Assert.ThrowsAsync<AuthorizationException>(() => position.AddAsync(new PositionInput(1, "x", "x", null)));
        await Assert.ThrowsAsync<AuthorizationException>(() => position.UpdateAsync(1, new PositionInput(1, "x", "x", null)));
        await Assert.ThrowsAsync<AuthorizationException>(() => position.DeactivateAsync(1));
        await Assert.ThrowsAsync<AuthorizationException>(() => salary.AddAsync(new SalaryInput(1, "x", 1, "Monthly", "HTG", DateTime.UtcNow, null)));
        await Assert.ThrowsAsync<AuthorizationException>(() => salary.GetApplicableAsync(1, DateTime.UtcNow));
        await Assert.ThrowsAsync<AuthorizationException>(() => salary.UpdateAsync(1, new SalaryInput(1, "x", 1, "Monthly", "HTG", DateTime.UtcNow, null)));
        await Assert.ThrowsAsync<AuthorizationException>(() => settings.SaveAsync(new LibrarySettingsSnapshot("x", 14, 0, "Light")));
        await Assert.ThrowsAsync<AuthorizationException>(() => backup.BackupAsync("unused-backup.db"));
        await Assert.ThrowsAsync<AuthorizationException>(() => backup.RestoreAsync("missing.db"));
        await Assert.ThrowsAsync<AuthorizationException>(() => audit.GetRecentAsync());
        await Assert.ThrowsAsync<AuthorizationException>(() => company.GetAsync());
        await Assert.ThrowsAsync<AuthorizationException>(() => department.GetAsync());
        await Assert.ThrowsAsync<AuthorizationException>(() => position.GetAsync());
        await Assert.ThrowsAsync<AuthorizationException>(() => employees.GetEmployeesAsync());
        await Assert.ThrowsAsync<AuthorizationException>(() => salary.GetHistoryAsync(1));
        await Assert.ThrowsAsync<AuthorizationException>(() => settings.GetAsync());
        await Assert.ThrowsAsync<AuthorizationException>(() => taxRules.GetAsync());
        await Assert.ThrowsAsync<AuthorizationException>(() => taxRules.GetApplicableAsync("HT", DateTime.UtcNow));
        await Assert.ThrowsAsync<AuthorizationException>(() => taxRules.AddAsync(new TaxRuleInput("x", "HT", DateTime.UtcNow, null, null, null, "Employee", false, null, null, 1)));
        await Assert.ThrowsAsync<AuthorizationException>(() => taxRules.DeactivateAsync(1));
        await Assert.ThrowsAsync<AuthorizationException>(() => payrollPeriods.GetAsync());
        await Assert.ThrowsAsync<AuthorizationException>(() => payrollPeriods.CreateAsync(new PayrollPeriodInput(DateTime.Today, DateTime.Today.AddDays(1), DateTime.Today.AddDays(1))));
        await Assert.ThrowsAsync<AuthorizationException>(() => payrollPeriods.MarkCalculatedAsync(1));
        await Assert.ThrowsAsync<AuthorizationException>(() => payrollPeriods.MarkReviewedAsync(1));
        await Assert.ThrowsAsync<AuthorizationException>(() => payrollPeriods.ApproveAsync(1));
        await Assert.ThrowsAsync<AuthorizationException>(() => payrollPeriods.MarkPaidAsync(1));
        await Assert.ThrowsAsync<AuthorizationException>(() => payrollPeriods.CloseAsync(1));
        await Assert.ThrowsAsync<AuthorizationException>(() => payrollEntries.GetForPeriodAsync(1));
        await Assert.ThrowsAsync<AuthorizationException>(() => payrollEntries.SaveCalculatedAsync(new PayrollEntryInput(1, 1, 0m, 0m, 0m, 0m, 0m, 0m, 0m, [])));
        await Assert.ThrowsAsync<AuthorizationException>(() => payrollEntries.CalculatePeriodAsync(1));
        await Assert.ThrowsAsync<AuthorizationException>(() => payrollReports.GetSummaryAsync(1));
        await Assert.ThrowsAsync<AuthorizationException>(() => payslips.GetAsync(1));
        await Assert.ThrowsAsync<AuthorizationException>(() => reports.GetLibraryReportAsync());
        await Assert.ThrowsAsync<AuthorizationException>(() => dashboard.GetSnapshotAsync());
        await Assert.ThrowsAsync<AuthorizationException>(() => authentication.ResetUserPasswordAsync(2, authorization));
        await Assert.ThrowsAsync<AuthorizationException>(() => authentication.CreateAccountAsync(1, "x", "valid password here", 2, authorization));
        await Assert.ThrowsAsync<AuthorizationException>(() => authentication.GetRolesAsync(authorization));
        await Assert.ThrowsAsync<AuthorizationException>(() => roleAdministration.GetRolesAsync());
        await Assert.ThrowsAsync<AuthorizationException>(() => roleAdministration.CreateAsync("Denied", []));
        await Assert.ThrowsAsync<AuthorizationException>(() => roleAdministration.UpdateGrantsAsync(1, []));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string path;
        public IDbContextFactory<LibraryDbContext> Factory { get; }

        private TestDatabase(string path, IDbContextFactory<LibraryDbContext> factory)
        {
            this.path = path;
            Factory = factory;
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"library-authz-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<LibraryDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            var factory = new TestFactory(options);
            await using var context = await factory.CreateDbContextAsync();
            await context.Database.MigrateAsync();
            return new TestDatabase(databasePath, factory);
        }

        public async ValueTask DisposeAsync()
        {
            await using var context = await Factory.CreateDbContextAsync();
            await context.Database.CloseConnectionAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(path);
        }

        private sealed class TestFactory(DbContextOptions<LibraryDbContext> options) : IDbContextFactory<LibraryDbContext>
        {
            public LibraryDbContext CreateDbContext() => new(options);

            public Task<LibraryDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(new LibraryDbContext(options));
        }
    }

    private static Role CreateRole(string name, params string[] permissions)
    {
        var role = new Role { Name = name };
        foreach (var permission in permissions)
            role.Permissions.Add(new RolePermission { PermissionName = permission });
        return role;
    }
}
