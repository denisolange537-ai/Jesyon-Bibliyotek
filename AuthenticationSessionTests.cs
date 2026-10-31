using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class AuthenticationSessionTests
{
    [Fact]
    public void SessionExpiresAfterFifteenIdleMinutesAndActivityRestartsIdleWindow()
    {
        var clock = new TestTimeProvider();
        var session = new AuthenticationSession(clock);
        session.SignIn(CreateUser());

        clock.Advance(TimeSpan.FromMinutes(14));
        session.RecordActivity();
        clock.Advance(TimeSpan.FromMinutes(14));
        Assert.True(session.IsAuthenticated);
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.False(session.IsAuthenticated);
        Assert.Null(session.CurrentUser);
        Assert.Equal("test", session.TakeExpiredUser()?.Username);
    }

    [Fact]
    public void IdleTimeoutLocksSessionAndOnlyTheSameActiveUserCanReauthenticate()
    {
        var clock = new TestTimeProvider();
        var session = new AuthenticationSession(clock);
        var user = CreateUser();
        user.Id = 42;
        session.SignIn(user);

        clock.Advance(AuthenticationSession.IdleTimeout);

        Assert.False(session.IsAuthenticated);
        Assert.True(session.IsLocked);
        Assert.Equal("test", session.TakeExpiredUser()?.Username);
        Assert.Null(session.TakeExpiredUser());

        var otherUser = CreateUser();
        otherUser.Id = 43;
        Assert.False(session.TryReauthenticate(otherUser));
        Assert.True(session.IsLocked);

        Assert.True(session.TryReauthenticate(user));
        Assert.True(session.IsAuthenticated);
        Assert.False(session.IsLocked);
        clock.Advance(TimeSpan.FromMinutes(14));
        Assert.True(session.IsAuthenticated);
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.False(session.IsAuthenticated);
    }

    [Fact]
    public void SessionCannotExtendBeyondEightHoursEvenWithActivity()
    {
        var clock = new TestTimeProvider();
        var session = new AuthenticationSession(clock);
        session.SignIn(CreateUser());

        clock.Advance(TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(59)));
        session.RecordActivity();
        clock.Advance(TimeSpan.FromMinutes(1));

        Assert.False(session.IsAuthenticated);
        Assert.True(session.IsLocked);
        Assert.Equal("test", session.TakeExpiredUser()?.Username);
    }

    [Fact]
    public void SignOutClearsPendingLockAndExpiredUser()
    {
        var clock = new TestTimeProvider();
        var session = new AuthenticationSession(clock);
        var user = CreateUser();
        user.Id = 42;
        session.SignIn(user);
        clock.Advance(AuthenticationSession.IdleTimeout);
        Assert.True(session.IsLocked);

        session.SignOut();

        Assert.False(session.IsAuthenticated);
        Assert.False(session.IsLocked);
        Assert.Null(session.TakeExpiredUser());
        Assert.False(session.TryReauthenticate(user));
    }

    [Fact]
    public void SuccessfulAuthorizationRecordsActivity()
    {
        var clock = new TestTimeProvider();
        var session = new AuthenticationSession(clock);
        session.SignIn(CreateUser());
        var authorization = new AuthorizationService(session);

        clock.Advance(TimeSpan.FromMinutes(14));
        authorization.RequirePermission(Permissions.BooksView);
        clock.Advance(TimeSpan.FromMinutes(14));

        Assert.True(session.IsAuthenticated);
    }

    private static User CreateUser()
    {
        var role = new Role { Name = "Test" };
        role.Permissions.Add(new RolePermission { PermissionName = Permissions.BooksView });
        return new User
        {
            Username = "test",
            PasswordHash = "not-used",
            Role = role,
            Employee = new Employee
            {
                EmployeeNumber = "EMP-SESSION-1",
                FirstName = "Test",
                LastName = "User",
                EmploymentStatus = "Active"
            }
        };
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }
}
