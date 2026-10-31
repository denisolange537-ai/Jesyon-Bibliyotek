using GestionBibliotheque.Data;
using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class BookServiceTests
{
    [Fact]
    public async Task BookCrudSearchDeactivateAndAuditWorkAgainstSqlite()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var service = database.CreateBookService(session);

        var book = await service.AddAsync(new BookInput("978-1", "Clean Code", "Robert Martin", "Prentice Hall", 2008, 3));
        Assert.Equal(3, book.AvailableCopies);
        Assert.Single(await service.GetBooksAsync("clean"));

        await service.UpdateAsync(book.Id, new BookInput("978-1", "Clean Code Revised", "Robert Martin", null, 2009, 4));
        var updated = Assert.Single(await service.GetBooksAsync());
        Assert.Equal("Clean Code Revised", updated.Title);
        Assert.Equal(4, updated.AvailableCopies);

        await service.DeactivateAsync(book.Id);
        Assert.Empty(await service.GetBooksAsync());

        await using var dbContext = await database.CreateDbContextAsync();
        Assert.Equal(3, await dbContext.AuditLogs.CountAsync());
        Assert.False(await dbContext.Books.Where(candidate => candidate.Id == book.Id).Select(candidate => candidate.IsActive).SingleAsync());
    }

    [Fact]
    public async Task DuplicateIsbnAndInvalidCopyCountAreRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateBookService(await database.CreateAdminSessionAsync());
        await service.AddAsync(new BookInput("978-2", "Domain-Driven Design", "Eric Evans", null, null, 1));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddAsync(new BookInput("978-2", "Another Book", "Another Author", null, null, 1)));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAsync(new BookInput("978-3", "Invalid", "Author", null, null, 0)));
    }

    [Fact]
    public async Task UpdatingBelowBorrowedCopiesIsRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateBookService(await database.CreateAdminSessionAsync());
        var book = await service.AddAsync(new BookInput("978-4", "The Pragmatic Programmer", "Andrew Hunt", null, 1999, 3));

        await using var dbContext = await database.CreateDbContextAsync();
        var storedBook = await dbContext.Books.SingleAsync(candidate => candidate.Id == book.Id);
        storedBook.AvailableCopies = 1;
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateAsync(book.Id, new BookInput("978-4", "The Pragmatic Programmer", "Andrew Hunt", null, 1999, 1)));
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
            var databasePath = Path.Combine(Path.GetTempPath(), $"library-books-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<LibraryDbContext>().UseSqlite($"Data Source={databasePath}").Options;
            var factory = new TestDbContextFactory(options);
            await using var dbContext = await factory.CreateDbContextAsync();
            await dbContext.Database.MigrateAsync();
            return new TestDatabase(databasePath, factory);
        }

        public async Task<AuthenticationSession> CreateAdminSessionAsync()
        {
            var authenticationService = new AuthenticationService(_dbContextFactory, new PasswordHasher<User>());
            var user = await authenticationService.CreateInitialAdminAsync("admin", "correct horse battery staple");
            var session = new AuthenticationSession();
            session.SignIn(user);
            return session;
        }

        public BookService CreateBookService(AuthenticationSession session) => new(_dbContextFactory, session);

        public Task<LibraryDbContext> CreateDbContextAsync() => _dbContextFactory.CreateDbContextAsync();

        public async ValueTask DisposeAsync()
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            await dbContext.Database.CloseConnectionAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(_databasePath);
        }

        private sealed class TestDbContextFactory(DbContextOptions<LibraryDbContext> options) : IDbContextFactory<LibraryDbContext>
        {
            public LibraryDbContext CreateDbContext() => new(options);
            public Task<LibraryDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new LibraryDbContext(options));
        }
    }
}
