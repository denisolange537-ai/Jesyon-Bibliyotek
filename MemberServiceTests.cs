using GestionBibliotheque.Data;
using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class MemberServiceTests
{
    [Fact]
    public async Task MemberCrudSearchDeactivateAndAuditWorkAgainstSqlite()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateMemberService(await database.CreateAdminSessionAsync());
        var member = await service.AddAsync(new MemberInput("M-001", "Ana", "Pierre", "ana@example.com", "555-0101", "Rue 1"));
        Assert.Single(await service.GetMembersAsync("ana"));

        await service.UpdateAsync(member.Id, new MemberInput("M-001", "Anna", "Pierre", "anna@example.com", null, null));
        Assert.Equal("Anna", Assert.Single(await service.GetMembersAsync()).FirstName);
        await service.DeactivateAsync(member.Id);

        Assert.Empty(await service.GetMembersAsync());
        await using var dbContext = await database.CreateDbContextAsync();
        Assert.Equal(3, await dbContext.AuditLogs.CountAsync());
        Assert.False(await dbContext.Members.Where(candidate => candidate.Id == member.Id).Select(candidate => candidate.IsActive).SingleAsync());
    }

    [Fact]
    public async Task DuplicateNumberAndInvalidEmailAreRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateMemberService(await database.CreateAdminSessionAsync());
        await service.AddAsync(new MemberInput("M-002", "Jean", "Louis", null, null, null));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddAsync(new MemberInput("M-002", "Other", "Person", null, null, null)));
        await Assert.ThrowsAsync<ArgumentException>(() => service.AddAsync(new MemberInput("M-003", "Other", "Person", "invalid", null, null)));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string _databasePath;
        private readonly IDbContextFactory<LibraryDbContext> _factory;
        private TestDatabase(string databasePath, IDbContextFactory<LibraryDbContext> factory) { _databasePath = databasePath; _factory = factory; }
        public static async Task<TestDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"library-members-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<LibraryDbContext>().UseSqlite($"Data Source={path}").Options;
            var factory = new TestDbContextFactory(options);
            await using var context = await factory.CreateDbContextAsync(); await context.Database.MigrateAsync();
            return new TestDatabase(path, factory);
        }
        public async Task<AuthenticationSession> CreateAdminSessionAsync()
        {
            var auth = new AuthenticationService(_factory, new PasswordHasher<User>());
            var session = new AuthenticationSession(); session.SignIn(await auth.CreateInitialAdminAsync("admin", "correct horse battery staple")); return session;
        }
        public MemberService CreateMemberService(AuthenticationSession session) => new(_factory, session);
        public Task<LibraryDbContext> CreateDbContextAsync() => _factory.CreateDbContextAsync();
        public async ValueTask DisposeAsync()
        {
            await using var context = await _factory.CreateDbContextAsync(); await context.Database.CloseConnectionAsync();
            SqliteConnection.ClearAllPools(); File.Delete(_databasePath);
        }
        private sealed class TestDbContextFactory(DbContextOptions<LibraryDbContext> options) : IDbContextFactory<LibraryDbContext>
        {
            public LibraryDbContext CreateDbContext() => new(options);
            public Task<LibraryDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new LibraryDbContext(options));
        }
    }
}
