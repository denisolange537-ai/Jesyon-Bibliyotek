using GestionBibliotheque.Data;
using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task SnapshotReadsRealCountsAndOutstandingFine()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var book = await new BookService(database.Factory, session).AddAsync(new BookInput("978-dashboard", "Dashboard Book", "Author", null, null, 2));
        var member = await new MemberService(database.Factory, session).AddAsync(new MemberInput("M-DASH", "Dashboard", "Member", null, null, null));
        var borrowing = await new BorrowingService(database.Factory, session).BorrowAsync(book.Id, member.Id);
        await using (var context = await database.CreateDbContextAsync())
        {
            borrowing = await context.Borrowings.SingleAsync(item => item.Id == borrowing.Id);
            borrowing.DueAtUtc = DateTime.UtcNow.AddDays(-3);
            context.Fines.Add(new Fine { BorrowingId = borrowing.Id, Amount = 3m, PaidAmount = 1m });
            await context.SaveChangesAsync();
        }

        var snapshot = await new DashboardService(database.Factory, session).GetSnapshotAsync();

        Assert.Equal(1, snapshot.ActiveBooks);
        Assert.Equal(1, snapshot.ActiveMembers);
        Assert.Equal(1, snapshot.OpenBorrowings);
        Assert.Equal(1, snapshot.OverdueBorrowings);
        Assert.Equal(2m, snapshot.OutstandingFines);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string _path;
        public IDbContextFactory<LibraryDbContext> Factory { get; }
        private TestDatabase(string path, IDbContextFactory<LibraryDbContext> factory) { _path = path; Factory = factory; }
        public static async Task<TestDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"library-dashboard-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<LibraryDbContext>().UseSqlite($"Data Source={path}").Options;
            var factory = new TestDbContextFactory(options);
            await using var context = await factory.CreateDbContextAsync(); await context.Database.MigrateAsync();
            return new TestDatabase(path, factory);
        }
        public async Task<AuthenticationSession> CreateAdminSessionAsync()
        {
            var auth = new AuthenticationService(Factory, new PasswordHasher<User>()); var session = new AuthenticationSession(); session.SignIn(await auth.CreateInitialAdminAsync("admin", "correct horse battery staple")); return session;
        }
        public Task<LibraryDbContext> CreateDbContextAsync() => Factory.CreateDbContextAsync();
        public async ValueTask DisposeAsync()
        {
            await using var context = await Factory.CreateDbContextAsync(); await context.Database.CloseConnectionAsync(); SqliteConnection.ClearAllPools(); File.Delete(_path);
        }
        private sealed class TestDbContextFactory(DbContextOptions<LibraryDbContext> options) : IDbContextFactory<LibraryDbContext>
        {
            public LibraryDbContext CreateDbContext() => new(options);
            public Task<LibraryDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new LibraryDbContext(options));
        }
    }
}
