namespace Falah.Infrastructure.Backup;

using Falah.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Independent audit 2026-11-23, F-03: which database schema a backup was taken at.
///
/// Production starts with Database:MigrateOnStartup off, so a restore of a backup taken before the last
/// update leaves the running server on an older schema than its code: queries fail at once, and the next
/// service start refuses to run until someone applies the update script - in the middle of the incident
/// the restore was meant to end. A .bak cannot be read without restoring it, so every backup now records
/// its newest applied migration in a small file beside it (<c>&lt;backup&gt;.schema</c>). Restore refuses a
/// backup whose recorded schema differs from this build's, before anything is destroyed; an older backup
/// with no such file is checked right after the restore instead, and the operator is told exactly what to do.
/// </summary>
public static class BackupSchemaStamp
{
    public const string Extension = ".schema";

    public static string PathFor(string backupFilePath) => backupFilePath + Extension;

    /// <summary>The newest migration this build knows - what a database must carry for this server to run on it.</summary>
    public static string? CodeVersion(FalahDbContext db) =>
        db.Database.GetMigrations().OrderBy(m => m, StringComparer.Ordinal).LastOrDefault();

    /// <summary>The newest migration applied to the database being backed up.</summary>
    public static async Task<string?> AppliedVersionAsync(FalahDbContext db, CancellationToken ct) =>
        (await db.Database.GetAppliedMigrationsAsync(ct)).OrderBy(m => m, StringComparer.Ordinal).LastOrDefault();

    /// <summary>Writes the stamp beside a backup. Returns false (never throws for I/O) if it could not.</summary>
    public static bool TryWrite(string backupFilePath, string? version)
    {
        if (string.IsNullOrWhiteSpace(version)) return false;
        try
        {
            File.WriteAllText(PathFor(backupFilePath), version);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>The recorded schema of a backup, or null when it has none (taken before stamps existed, or the
    /// file was copied without it).</summary>
    public static string? Read(string backupFilePath)
    {
        try
        {
            var path = PathFor(backupFilePath);
            return File.Exists(path) ? File.ReadAllText(path).Trim() is { Length: > 0 } v ? v : null : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>The migrations this build has that the (just restored) database at <paramref name="connectionString"/>
    /// does not. Empty when they match.</summary>
    public static async Task<IReadOnlyList<string>> MissingMigrationsAsync(FalahDbContext db, string connectionString, CancellationToken ct)
    {
        var applied = new HashSet<string>(StringComparer.Ordinal);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NOT NULL SELECT [MigrationId] FROM [__EFMigrationsHistory]";
        await using (var reader = await command.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) applied.Add(reader.GetString(0));
        return db.Database.GetMigrations().Where(m => !applied.Contains(m)).OrderBy(m => m, StringComparer.Ordinal).ToList();
    }
}
