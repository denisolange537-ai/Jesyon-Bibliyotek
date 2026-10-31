using GestionBibliotheque.Data;
using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class ReturnServiceTests
{
    [Fact]
    public async Task LateReturnRestoresCopyCreatesFineAndRecordsPayment()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var book = await database.CreateBookAsync(session);
        var member = await database.CreateMemberAsync(session);
        var borrowing = await new BorrowingService(database.Factory, session).BorrowAsync(book.Id, member.Id);
        await database.SetDueDateAsync(borrowing.Id, DateTime.UtcNow.Date.AddDays(-2));
        var service = new ReturnService(database.Factory, session);

        var result = await service.ReturnAsync(borrowing.Id);

        Assert.Equal(2m, result.FineAmount);
        await using var context = await database.CreateDbContextAsync();
        var fine = await context.Fines.SingleAsync();
        Assert.Equal(2m, fine.Amount);
        Assert.Equal(1, await context.Books.Where(item => item.Id == book.Id).Select(item => item.AvailableCopies).SingleAsync());
        await service.PayFineAsync(fine.Id, 2m);
        await context.Entry(fine).ReloadAsync();
        fine = await context.Fines.SingleAsync();
        Assert.Equal(fine.Amount, fine.PaidAmount);
        Assert.NotNull(fine.PaidAtUtc);
    }

    [Fact]
    public async Task SecondReturnAndOverpaymentAreRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var book = await database.CreateBookAsync(session);
        var member = await database.CreateMemberAsync(session);
        var borrowing = await new BorrowingService(database.Factory, session).BorrowAsync(book.Id, member.Id);
        await database.SetDueDateAsync(borrowing.Id, DateTime.UtcNow.Date.AddDays(-1));
        var service = new ReturnService(database.Factory, session);
        await service.ReturnAsync(borrowing.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReturnAsync(borrowing.Id));
        await using var context = await database.CreateDbContextAsync();
        var fine = await context.Fines.SingleAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => service.PayFineAsync(fine.Id, fine.Amount + 1));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string _path;
        public IDbContextFactory<LibraryDbContext> Factory { get; }
        private TestDatabase(string path, IDbContextFactory<LibraryDbContext> factory) { _path = path; Factory = factory; }
        public static async Task<TestDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"library-returns-{Guid.NewGuid():N}.db");
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
        public async Task<Book> CreateBookAsync(AuthenticationSession session) => await new BookService(Factory, session).AddAsync(new BookInput("978-return", "Return Book", "Author", null, null, 1));
        public async Task<Member> CreateMemberAsync(AuthenticationSession session) => await new MemberService(Factory, session).AddAsync(new MemberInput("M-RETURN", "Return", "Member", null, null, null));
        public async Task SetDueDateAsync(int borrowingId, DateTime dueDate)
        {
            await using var context = await Factory.CreateDbContextAsync(); var borrowing = await context.Borrowings.SingleAsync(item => item.Id == borrowingId); borrowing.DueAtUtc = dueDate; await context.SaveChangesAsync();
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
