using Microsoft.Data.SqlClient;

namespace ArtisanHub.Data;

/// <summary>
/// Finds a SQL Server the app can reach. It tries the connection string in appsettings.json first,
/// then the same string with each server name listed under "DatabaseServerFallbacks"
/// (.\SQLEXPRESS, localhost, LocalDB). The first one that answers is used.
/// </summary>
public static class ConnectionResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var raw = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is missing from appsettings.json.");
        }

        var configured = new SqlConnectionStringBuilder(raw).ConnectionString;

        var candidates = new List<string> { configured };
        var fallbacks = configuration.GetSection("DatabaseServerFallbacks").Get<string[]>() ?? Array.Empty<string>();
        foreach (var server in fallbacks)
        {
            var candidate = new SqlConnectionStringBuilder(configured) { DataSource = server }.ConnectionString;
            if (!candidates.Contains(candidate, StringComparer.OrdinalIgnoreCase))
            {
                candidates.Add(candidate);
            }
        }

        foreach (var candidate in candidates)
        {
            var server = new SqlConnectionStringBuilder(candidate).DataSource;
            if (CanReachServer(candidate, out var error))
            {
                Console.WriteLine($"[ArtisanHub] Connected to SQL Server '{server}'.");
                return candidate;
            }

            Console.WriteLine($"[ArtisanHub] SQL Server '{server}' not reachable: {error}");
        }

        throw new InvalidOperationException(
            "ArtisanHub could not connect to SQL Server. Open ArtisanHub/appsettings.json and set " +
            "ConnectionStrings:DefaultConnection to the server name you use in SQL Server Management Studio " +
            "(for example .\\SQLEXPRESS or localhost). See README.md for help.");
    }

    private static bool CanReachServer(string connectionString, out string error)
    {
        // First try the master database (works before ArtisanHubDB exists).
        if (TryOpen(connectionString, "master", out error, out var sqlErrorNumber))
        {
            return true;
        }

        // 4060 = cannot open database, 18456 = login failed, 916 = no access to master:
        // the server answered, so try the app's own database instead.
        var appDatabase = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (sqlErrorNumber is 4060 or 18456 or 916 && !string.IsNullOrWhiteSpace(appDatabase))
        {
            return TryOpen(connectionString, appDatabase, out error, out _);
        }

        return false;
    }

    private static bool TryOpen(string connectionString, string database, out string error, out int sqlErrorNumber)
    {
        error = string.Empty;
        sqlErrorNumber = 0;

        var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = database };
        builder.ConnectTimeout = builder.DataSource.Contains("localdb", StringComparison.OrdinalIgnoreCase) ? 20 : 5;

        try
        {
            using var connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            return true;
        }
        catch (SqlException ex)
        {
            sqlErrorNumber = ex.Number;
            error = ex.Message.Split('\n')[0].Trim();
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message.Split('\n')[0].Trim();
            return false;
        }
    }
}
