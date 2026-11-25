using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace PROTOTYPE_backend.Data;

/// <summary>
/// Fejlesztői környezetben létrehozza az adatbázist és betölti a sémát / tesztadatokat,
/// ha az adatbázis még nem létezik vagy üres. Meglévő adatot soha nem töröl
/// (a DROP DATABASE / CREATE DATABASE / USE sorokat kihagyja a szkriptekből).
/// </summary>
public static class DatabaseInitializer
{
    private const string SchemaFile = "adatbázis.sql";
    private const string SeedFile = "seed.sql";

    public static async Task InitializeAsync(IServiceProvider services, IHostEnvironment env)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
        var context = services.GetRequiredService<AppDbContext>();

        var connectionString = context.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("Adatbázis inicializálás kihagyva: nincs connection string.");
            return;
        }

        var builder = new MySqlConnectionStringBuilder(connectionString);
        var dbName = builder.Database;
        if (string.IsNullOrWhiteSpace(dbName))
        {
            logger.LogWarning("Adatbázis inicializálás kihagyva: a connection string nem tartalmaz adatbázisnevet.");
            return;
        }

        var databaseDir = FindDatabaseDirectory(env.ContentRootPath);

        // 1. Adatbázis létrehozása, ha nem létezik (IF NOT EXISTS: meglévőt nem érint)
        var serverBuilder = new MySqlConnectionStringBuilder(connectionString) { Database = string.Empty };
        await using (var serverConn = new MySqlConnection(serverBuilder.ConnectionString))
        {
            await serverConn.OpenAsync();
            await using var cmd = serverConn.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{dbName.Replace("`", "``")}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
            await cmd.ExecuteNonQueryAsync();
        }

        // AllowUserVariables: a szkriptekben szereplő @ jelek ne legyenek paraméterként értelmezve
        var dbBuilder = new MySqlConnectionStringBuilder(connectionString) { AllowUserVariables = true };
        await using var conn = new MySqlConnection(dbBuilder.ConnectionString);
        await conn.OpenAsync();

        // 2. Séma betöltése csak akkor, ha az adatbázisban még nincs egyetlen tábla sem
        if (await ScalarAsync(conn, "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE();") == 0)
        {
            if (databaseDir == null)
            {
                logger.LogWarning("Az adatbázis üres, de a 'database' mappa nem található – a séma nem töltődött be.");
                return;
            }

            logger.LogInformation("Üres adatbázis: séma létrehozása ({File}).", SchemaFile);
            await RunScriptAsync(conn, Path.Combine(databaseDir, SchemaFile));
        }

        // 3. Tesztadatok csak akkor, ha az app_user tábla létezik és üres
        var hasUserTable = await ScalarAsync(conn,
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'app_user';") > 0;

        if (hasUserTable && await ScalarAsync(conn, "SELECT COUNT(*) FROM app_user;") == 0)
        {
            if (databaseDir == null)
            {
                logger.LogWarning("Az app_user tábla üres, de a 'database' mappa nem található – a seed nem futott le.");
                return;
            }

            logger.LogInformation("Üres app_user tábla: tesztadatok betöltése ({File}).", SeedFile);
            await RunScriptAsync(conn, Path.Combine(databaseDir, SeedFile));
        }
    }

    private static async Task<long> ScalarAsync(MySqlConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }

    private static async Task RunScriptAsync(MySqlConnection conn, string path)
    {
        var sql = await File.ReadAllTextAsync(path);

        // Biztonsági szűrés: soha ne töröljön / hozzon létre / váltson adatbázist
        sql = Regex.Replace(sql, @"^\s*DROP\s+DATABASE[^;]*;", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        sql = Regex.Replace(sql, @"^\s*CREATE\s+DATABASE[^;]*;", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        sql = Regex.Replace(sql, @"^\s*USE\s+[^;]*;", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 300;
        await cmd.ExecuteNonQueryAsync();
    }

    private static string? FindDatabaseDirectory(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "database");
            if (File.Exists(Path.Combine(candidate, SchemaFile)) && File.Exists(Path.Combine(candidate, SeedFile)))
                return candidate;
            dir = dir.Parent;
        }
        return null;
    }
}
