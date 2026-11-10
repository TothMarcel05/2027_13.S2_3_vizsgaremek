using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// ASP.NET Core DataProtection főkulcsok perzisztens tárolása
/// </summary>
public partial class DataProtectionKey
{
    /// <summary>
    /// Elsődleges kulcs
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Opcionális barátságos név
    /// </summary>
    public string? FriendlyName { get; set; }

    /// <summary>
    /// A titkosító főkulcs XML formátumban
    /// </summary>
    public string? Xml { get; set; }
}
