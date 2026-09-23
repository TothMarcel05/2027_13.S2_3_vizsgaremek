using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class E2eRoomKey
{
    public Guid RoomId { get; set; }

    public Guid UserId { get; set; }

    public int KeyVersion { get; set; }

    public string EncryptedKey { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ChatRoom Room { get; set; } = null!;

    public virtual AppUser User { get; set; } = null!;
}
