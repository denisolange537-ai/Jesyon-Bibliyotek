using GestionBibliotheque.Data;
using GestionBibliotheque.Models;
using GestionBibliotheque.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class SettingsBackupAuditTests
{
    [Fact]
    public async Task SettingsPersistAndBackupRestorePreservesData()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var settings = new LibrarySettingsService(database.Factory, session);
        await settings.SaveAsync(new LibrarySettingsSnapshot("Bibliyotèk Tès", 21, 2.5m, "Dark"));
        var saved = await settings.GetAsync();
        Assert.Equal(21, saved.LoanDays);
        Assert.Equal(2.5m, saved.FinePerDay);
        Assert.Equal("Dark", saved.Theme);

        var backupPath = Path.Combine(Path.GetTempPath(), $"library-backup-{Guid.NewGuid():N}.db");
        try
        {
            await new BackupService(database.Path, database.Factory, session).BackupAsync(backupPath);
            await settings.SaveAsync(new LibrarySettingsSnapshot("Changed", 7, 1m, "Light"));
            await new BackupService(database.Path, database.Factory, session).RestoreAsync(backupPath);
            var restored = await settings.GetAsync();
            Assert.Equal("Bibliyotèk Tès", restored.ApplicationName);
            Assert.Equal(21, restored.LoanDays);
        }
        finally { File.Delete(backupPath); }
    }

    [Fact]
    public async Task AuditServiceReturnsRecentActionsWithoutPasswordData()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        await using var context = await database.Factory.CreateDbContextAsync();
        context.AuditLogs.Add(new AuditLog { Action = "Test", EntityName = "Book", EntityId = 1, Details = "safe detail" });
        await context.SaveChangesAsync();
        var entries = await new AuditService(database.Factory, session).GetRecentAsync();
        Assert.Contains(entries, entry => entry.Action == "Test" && entry.Details == "safe detail");
    }

    [Fact]
    public async Task BackupProducesNonEmptyIntegrityCheckedSqliteFile()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var backupPath = Path.Combine(Path.GetTempPath(), $"library-integrity-{Guid.NewGuid():N}.db");
        try
        {
            await new BackupService(database.Path, database.Factory, session).BackupAsync(backupPath);

            Assert.True(new FileInfo(backupPath).Length > 0);
            await using var connection = new SqliteConnection($"Data Source={backupPath};Mode=ReadOnly;Pooling=False");
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            Assert.Equal("ok", Convert.ToString(await command.ExecuteScalarAsync()));
        }
        finally { File.Delete(backupPath); }
    }

    [Fact]
    public async Task InvalidRestoreIsRejectedAndCurrentDatabaseRemainsUsable()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = await database.CreateAdminSessionAsync();
        var settings = new LibrarySettingsService(database.Factory, session);
        await settings.SaveAsync(new LibrarySettingsSnapshot("Before Invalid Restore", 21, 2.5m, "Dark"));
        var invalidPath = Path.Combine(Path.GetTempPath(), $"library-invalid-{Guid.NewGuid():N}.db");
        try
        {
            await File.WriteAllTextAsync(invalidPath, "this is not a sqlite database");

            await Assert.ThrowsAnyAsync<Exception>(() => new BackupService(database.Path, database.Factory, session).RestoreAsync(invalidPath));

            var current = await settings.GetAsync();
            Assert.Equal("Before Invalid Restore", current.ApplicationName);
            Assert.Equal(21, current.LoanDays);
        }
        finally { File.Delete(invalidPath); }
    }

    [Fact]
    public async Task LibrarianCannotBackupOrRestore()
    {
        await using var database = await TestDatabase.CreateAsync();
        var administratorSession = await database.CreateAdminSessionAsync();
        var authentication = new AuthenticationService(database.Factory, new Microsoft.AspNetCore.Identity.PasswordHasher<User>());
        var employee = await new EmployeeService(database.Factory, administratorSession).AddAsync(new EmployeeInput("EMP-BACKUP-LIB", "Lina", "Louis", null, null, null, DateTime.UtcNow.Date, "Librarian", null));
        await authentication.CreateAccountAsync(employee.Id, "backup-librarian", "correct horse battery staple", 2, new AuthorizationService(administratorSession));
        var librarianSession = new AuthenticationSession();
        librarianSession.SignIn(await authentication.AuthenticateAsync("backup-librarian", "correct horse battery staple") ?? throw new InvalidOperationException());
        var service = new BackupService(database.Path, database.Factory, librarianSession);
        var backupPath = Path.Combine(Path.GetTempPath(), $"library-unauthorized-{Guid.NewGuid():N}.db");
        try
        {
            await Assert.ThrowsAsync<AuthorizationException>(() => service.BackupAsync(backupPath));
            await Assert.ThrowsAsync<AuthorizationException>(() => service.RestoreAsync(database.Path + ".missing"));
            Assert.False(File.Exists(backupPath));
        }
        finally { File.Delete(backupPath); }
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string _path;
        public string Path => _path;
        public IDbContextFactory<LibraryDbContext> Factory { get; }
        private TestDatabase(string path, IDbContextFactory<LibraryDbContext> factory) { _path = path; Factory = factory; }
        public static async Task<TestDatabase> CreateAsync()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"library-settings-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<LibraryDbContext>().UseSqlite($"Data Source={path}").Options;
            var factory = new TestDbContextFactory(options);
            await using var context = await factory.CreateDbContextAsync(); await context.Database.MigrateAsync();
            return new TestDatabase(path, factory);
        }
        public async Task<AuthenticationSession> CreateAdminSessionAsync()
        {
            var auth = new AuthenticationService(Factory, new Microsoft.AspNetCore.Identity.PasswordHasher<Models.User>());
            var session = new AuthenticationSession();
            session.SignIn(await auth.CreateInitialAdminAsync("admin", "correct horse battery staple"));
            return session;
        }
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