using GestionBibliotheque.Data;
using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class ReservationServiceTests
{
    [Fact]
    public async Task ReservationCanBeCreatedCancelledAndAudited()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var book = await database.CreateBookAsync(session);
        var member = await database.CreateMemberAsync(session, "M-RESERVE");
        var service = new ReservationService(database.Factory, session);

        var reservation = await service.ReserveAsync(book.Id, member.Id);
        Assert.Single(await service.GetOpenAsync());
        await service.CancelAsync(reservation.Id);

        Assert.Empty(await service.GetOpenAsync());
        await using var context = await database.CreateDbContextAsync();
        Assert.Contains(await context.AuditLogs.ToListAsync(), log => log.Action == "ReservationCancelled");
    }

    [Fact]
    public async Task DuplicateReservationAndInvalidReferencesAreRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var book = await database.CreateBookAsync(session);
        var member = await database.CreateMemberAsync(session, "M-DUP");
        var service = new ReservationService(database.Factory, session);
        await service.ReserveAsync(book.Id, member.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReserveAsync(book.Id, member.Id));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ReserveAsync(999, member.Id));
    }

    [Fact]
    public async Task ReturnFulfillsFirstOpenReservation()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var book = await database.CreateBookAsync(session);
        var borrower = await database.CreateMemberAsync(session, "M-BORROWER");
        var waitingMember = await database.CreateMemberAsync(session, "M-WAITING");
        var borrowing = await new BorrowingService(database.Factory, session).BorrowAsync(book.Id, borrower.Id);
        var reservation = await new ReservationService(database.Factory, session).ReserveAsync(book.Id, waitingMember.Id);

        await new ReturnService(database.Factory, session).ReturnAsync(borrowing.Id);

        await using var context = await database.CreateDbContextAsync();
        Assert.NotNull(await context.Reservations.SingleAsync(item => item.Id == reservation.Id && item.FulfilledAtUtc != null));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string _path;
        public IDbContextFactory<LibraryDbContext> Factory { get; }
        private TestDatabase(string path, IDbContextFactory<LibraryDbContext> factory) { _path = path; Factory = factory; }
        public static async Task<TestDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"library-reservations-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<LibraryDbContext>().UseSqlite($"Data Source={path}").Options;
            var factory = new TestDbContextFactory(options);
            await using var context = await factory.CreateDbContextAsync(); await context.Database.MigrateAsync();
            return new TestDatabase(path, factory);
        }
        public async Task<AuthenticationSession> CreateAdminSessionAsync()
        {
            var auth = new AuthenticationService(Factory, new PasswordHasher<User>()); var session = new AuthenticationSession(); session.SignIn(await auth.CreateInitialAdminAsync("admin", "correct horse battery staple")); return session;
        }
        public async Task<Book> CreateBookAsync(AuthenticationSession session) => await new BookService(Factory, session).AddAsync(new BookInput("978-reserve", "Reservation Book", "Author", null, null, 1));
        public async Task<Member> CreateMemberAsync(AuthenticationSession session, string number) => await new MemberService(Factory, session).AddAsync(new MemberInput(number, "Test", "Member", null, null, null));
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
