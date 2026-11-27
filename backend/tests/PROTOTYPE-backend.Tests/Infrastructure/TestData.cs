namespace PROTOTYPE_backend.Tests.Infrastructure;

/// <summary>A seed.sql-ben szereplő azonosítók és belépési adatok.</summary>
public static class TestData
{
    public const string AdminEmail = "marcel.toth@example.com";
    public const string AdminPassword = "Marcel2026!Admin";
    public static readonly Guid AdminId = Guid.Parse("01000000-0000-4000-8000-000000000001");

    public const string UserPassword = "Teszt1234!";

    // WS1 Fejlesztő szerepkör, nincs 2FA
    public const string DevEmail = "david.horvath@example.com";
    public static readonly Guid DevId = Guid.Parse("01000000-0000-4000-8000-000000000005");

    // WS1 Viewer
    public const string ViewerEmail = "zsofia.molnar@example.com";
    public static readonly Guid ViewerId = Guid.Parse("01000000-0000-4000-8000-000000000008");

    // Nem tagja a WS1-nek (csak WS3)
    public const string OutsiderEmail = "nora.papp@example.com";
    public static readonly Guid OutsiderId = Guid.Parse("01000000-0000-4000-8000-000000000012");

    public const string SuspendedEmail = "felfuggesztett.teszt@example.com";
    public const string TwoFactorEmail = "anna.nagy@example.com";

    public static readonly Guid Ws1 = Guid.Parse("02000000-0000-4000-8000-000000000001");
    public static readonly Guid Ws3 = Guid.Parse("02000000-0000-4000-8000-000000000003");
    public static readonly Guid Project1 = Guid.Parse("05000000-0000-4000-8000-000000000001");

    public static readonly Guid RoleOwner = Guid.Parse("04000000-0000-4000-8000-000000000001");
    public static readonly Guid RoleAdmin = Guid.Parse("04000000-0000-4000-8000-000000000002");
    public static readonly Guid RoleMember = Guid.Parse("04000000-0000-4000-8000-000000000003");
    public static readonly Guid RoleViewer = Guid.Parse("04000000-0000-4000-8000-000000000004");

    public static readonly Guid Missing = Guid.Parse("ffffffff-0000-4000-8000-ffffffffffff");
}
