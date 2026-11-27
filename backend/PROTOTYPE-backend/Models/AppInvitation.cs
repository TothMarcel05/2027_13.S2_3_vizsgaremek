using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Meghívások kezelése státuszkövetéssel
/// </summary>
public partial class AppInvitation
{
    /// <summary>
    /// Meghívó azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 1 = workspace, 2 = project
    /// </summary>
    public sbyte Type { get; set; }

    /// <summary>
    /// A workspace_id vagy project_id azonosítója
    /// </summary>
    public Guid TargetId { get; set; }

    /// <summary>
    /// A meghívott e-mail címe
    /// </summary>
    public string InvitedEmail { get; set; } = null!;

    /// <summary>
    /// A meghívott felhasználó azonosítója (ha regisztrált)
    /// </summary>
    public Guid? InvitedUserId { get; set; }

    /// <summary>
    /// A felajánlott szerepkör azonosítója
    /// </summary>
    public Guid? RoleId { get; set; }

    /// <summary>
    /// A meghívást kiküldő felhasználó
    /// </summary>
    public Guid InvitedBy { get; set; }

    /// <summary>
    /// 1 = pending, 2 = accepted, 3 = refused, 4 = expired
    /// </summary>
    public sbyte Status { get; set; }

    /// <summary>
    /// Egyedi biztonsági token
    /// </summary>
    public string Token { get; set; } = null!;

    /// <summary>
    /// A meghívó lejárati ideje
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Kiküldés ideje
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// A válaszadás ideje
    /// </summary>
    public DateTime? RespondedAt { get; set; }

    public virtual AppUser InvitedByNavigation { get; set; } = null!;

    public virtual AppUser? InvitedUser { get; set; }

    public virtual AppRole? Role { get; set; }
}
