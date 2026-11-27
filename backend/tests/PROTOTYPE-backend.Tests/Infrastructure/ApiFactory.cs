using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MySqlConnector;
using PROTOTYPE_backend.Services.Email;

namespace PROTOTYPE_backend.Tests.Infrastructure;

/// <summary>
/// Teljes alkalmazást indít egy eldobható MySQL adatbázison (tmd_integration_test).
/// Az adatbázist induláskor eldobja, a sémát (adatbázis.sql) és a tesztadatokat (seed.sql)
/// a DatabaseInitializer tölti be (Development környezetben).
/// A MySQL szerver elérhetősége felülírható a TMD_TEST_MYSQL környezeti változóval
/// (pl. "Server=127.0.0.1;user=root;password=mysql;").
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string TestDatabase = "tmd_integration_test";
    private const string DefaultServer = "Server=127.0.0.1;user=root;password=mysql;";

    private static MySqlConnectionStringBuilder ServerConnection()
    {
        var b = new MySqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("TMD_TEST_MYSQL") ?? DefaultServer);
        b.Database = string.Empty;
        return b;
    }

    private static void DropDatabase()
    {
        using var conn = new MySqlConnection(ServerConnection().ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"DROP DATABASE IF EXISTS `{TestDatabase}`;";
        cmd.ExecuteNonQuery();
    }

    public ApiFactory()
    {
        DropDatabase();

        var cs = ServerConnection();
        cs.Database = TestDatabase;
        // A Program.cs a builder létrehozásakor olvassa a connection stringet -> környezeti változóval írjuk felül.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", cs.ConnectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development: ekkor fut le a DatabaseInitializer (séma + seed betöltése).
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            // Nincs valódi SMTP a tesztekben.
            services.RemoveAll<IEmailService>();
            services.AddScoped<IEmailService, NoopEmailService>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        try { DropDatabase(); }
        catch { /* takarítás best-effort */ }
    }

    private sealed class NoopEmailService : IEmailService
    {
        public Task SendPasswordResetEmailAsync(string toEmail, string resetToken) => Task.CompletedTask;
        public Task SendTwoFactorCodeEmailAsync(string email, string code) => Task.CompletedTask;
    }
}
