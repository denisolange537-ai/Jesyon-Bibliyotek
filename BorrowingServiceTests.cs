using GestionBibliotheque.Data;
using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class BorrowingServiceTests
{
    [Fact]
    public async Task BorrowingUpdatesAvailabilityAndCreatesAuditAgainstSqlite()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var book = await database.CreateBookAsync(session);
        var member = await database.CreateMemberAsync(session);
        var service = new BorrowingService(database.Factory, session);

        var borrowing = await service.BorrowAsync(book.Id, member.Id);

        Assert.Equal(BorrowingService.DefaultLoanDays, (borrowing.DueAtUtc - borrowing.BorrowedAtUtc).Days);
        await using var context = await database.CreateDbContextAsync();
        Assert.Equal(0, await context.Books.Where(item => item.Id == book.Id).Select(item => item.AvailableCopies).SingleAsync());
        Assert.Single(await service.GetOpenBorrowingsAsync());
        Assert.Contains(await context.AuditLogs.ToListAsync(), audit => audit.EntityName == nameof(Borrowing) && audit.Action == "Borrowed");
    }

    [Fact]
    public async Task BorrowingWithoutCopiesOrAuthenticationIsRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var book = await database.CreateBookAsync(session);
        var member = await database.CreateMemberAsync(session);
        var service = new BorrowingService(database.Factory, session);
        await service.BorrowAsync(book.Id, member.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BorrowAsync(book.Id, member.Id));
        var unauthenticated = new BorrowingService(database.Factory, new AuthenticationSession());
        await Assert.ThrowsAsync<InvalidOperationException>(() => unauthenticated.BorrowAsync(book.Id, member.Id));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string _path;
        public IDbContextFactory<LibraryDbContext> Factory { get; }
        private TestDatabase(string path, IDbContextFactory<LibraryDbContext> factory) { _path = path; Factory = factory; }
        public static async Task<TestDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"library-borrowing-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<LibraryDbContext>().UseSqlite($"Data Source={path}").Options;
            var factory = new TestDbContextFactory(options);
            await using var context = await factory.CreateDbContextAsync(); await context.Database.MigrateAsync();
            return new TestDatabase(path, factory);
        }
        public async Task<AuthenticationSession> CreateAdminSessionAsync()
        {
            var auth = new AuthenticationService(Factory, new PasswordHasher<User>());
            var session = new AuthenticationSession(); session.SignIn(await auth.CreateInitialAdminAsync("admin", "correct horse battery staple")); return session;
        }
        public async Task<Book> CreateBookAsync(AuthenticationSession session) => await new BookService(Factory, session).AddAsync(new BookInput("978-borrow", "Borrowing Book", "Author", null, null, 1));
        public async Task<Member> CreateMemberAsync(AuthenticationSession session) => await new MemberService(Factory, session).AddAsync(new MemberInput("M-BORROW", "Borrow", "Member", null, null, null));
        public Task<LibraryDbContext> CreateDbContextAsync() => Factory.CreateDbContextAsync();
        public async ValueTask DisposeAsync()
        {
            await using var context = await Factory.CreateDbContextAsync(); await context.Database.CloseConnectionAsync();
            SqliteConnection.ClearAllPools(); File.Delete(_path);
        }
        private sealed class TestDbContextFactory(DbContextOptions<LibraryDbContext> options) : IDbContextFactory<LibraryDbContext>
        {
            public LibraryDbContext CreateDbContext() => new(options);
            public Task<LibraryDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new LibraryDbContext(options));
        }
    }
}
