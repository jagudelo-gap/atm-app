using Microsoft.Data.Sqlite;

namespace Atm.Api.Seeding;

/// <summary>Resolves the SQLite connection string and the data folder (database and logs live there).</summary>
public sealed record Storage(string ConnectionString, string DataFolder)
{
    public const string DefaultConnectionString = "Data Source=data/atm.db";

    /// <summary>Seconds a writer waits for the SQLite lock before failing with "database is locked".</summary>
    private const int DefaultBusyTimeoutSeconds = 5;

    public static Storage From(IConfiguration config)
    {
        var raw = config.GetConnectionString("Atm");
        var builder = new SqliteConnectionStringBuilder(string.IsNullOrWhiteSpace(raw) ? DefaultConnectionString : raw);
        if (!builder.ConnectionString.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
            builder.DefaultTimeout = DefaultBusyTimeoutSeconds;

        var dataSource = builder.DataSource;
        var isFile = !string.IsNullOrEmpty(dataSource) && dataSource != ":memory:" && builder.Mode != SqliteOpenMode.Memory;
        var folder = isFile
            ? Path.GetDirectoryName(Path.GetFullPath(dataSource))!
            : Path.GetFullPath("data");
        Directory.CreateDirectory(folder);

        return new Storage(builder.ToString(), folder);
    }
}
