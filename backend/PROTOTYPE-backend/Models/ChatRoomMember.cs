using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Chat szoba tagok
/// </summary>
public partial class ChatRoomMember
{
    /// <summary>
    /// Szoba
    /// </summary>
    public Guid RoomId { get; set; }

    /// <summary>
    /// Tag
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Csatlakozás ideje
    /// </summary>
    public DateTime? JoinedAt { get; set; }

    public virtual ChatRoom Room { get; set; } = null!;

    public virtual AppUser User { get; set; } = null!;
}
