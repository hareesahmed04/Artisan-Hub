using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace ArtisanHub.Data;

/// <summary>
/// On first run: creates the database (if it does not exist) and runs the embedded
/// Database/ArtisanHub_Database.sql script to build the 15 tables, views, trigger and sample data.
/// If the Users table already exists, nothing is changed.
/// </summary>
public static class DbInitializer
{
    private const string SchemaMarker = "-- @@BEGIN_SCHEMA@@";

    public static void Initialize(string connectionString)
    {
        var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("The connection string must name a database (Database=ArtisanHubDB).");
        }

        CreateDatabaseIfMissing(connectionString, databaseName);

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using (var check = connection.CreateCommand())
        {
            check.CommandText = "SELECT OBJECT_ID(N'dbo.Users', N'U')";
            var result = check.ExecuteScalar();
            if (result is not null && result is not DBNull)
            {
                Console.WriteLine($"[ArtisanHub] Database '{databaseName}' is ready.");
                return;
            }
        }

        Console.WriteLine($"[ArtisanHub] Building database '{databaseName}' (tables, views, trigger, sample data)...");
        foreach (var batch in ReadSchemaBatches())
        {
            using var command = connection.CreateCommand();
            command.CommandText = batch;
            command.CommandTimeout = 180;
            command.ExecuteNonQuery();
        }

        Console.WriteLine("[ArtisanHub] Database created. Demo logins are listed in README.md.");
    }

    private static void CreateDatabaseIfMissing(string connectionString, string databaseName)
    {
        try
        {
            var master = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
            using var connection = new SqlConnection(master.ConnectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText =
                "IF DB_ID(@name) IS NULL " +
                "BEGIN " +
                "  DECLARE @sql NVARCHAR(400) = N'CREATE DATABASE ' + QUOTENAME(@name); " +
                "  EXEC sp_executesql @sql; " +
                "END";
            command.Parameters.AddWithValue("@name", databaseName);
            command.ExecuteNonQuery();
        }
        catch (SqlException ex)
        {
            // Hosted servers (for example MonsterASP.NET) do not allow CREATE DATABASE;
            // the database already exists there, so we just carry on.
            Console.WriteLine($"[ArtisanHub] Skipped database creation check: {ex.Message.Split('\n')[0]}");
        }
    }

    private static IEnumerable<string> ReadSchemaBatches()
    {
        var assembly = typeof(DbInitializer).Assembly;
        using var stream = assembly.GetManifestResourceStream("ArtisanHub.Database.sql")
            ?? throw new InvalidOperationException("Embedded resource ArtisanHub.Database.sql was not found.");
        using var reader = new StreamReader(stream);
        var script = reader.ReadToEnd();

        var start = script.IndexOf(SchemaMarker, StringComparison.Ordinal);
        if (start >= 0)
        {
            script = script[(start + SchemaMarker.Length)..];
        }

        return Regex
            .Split(script, @"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Where(batch => !string.IsNullOrWhiteSpace(batch))
            .ToList();
    }
}
