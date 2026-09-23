using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class ChatRoomMember
{
    public Guid RoomId { get; set; }

    public Guid UserId { get; set; }

    public Guid? LastReadMessageId { get; set; }

    public DateTime? JoinedAt { get; set; }

    public virtual E2eChatMessage? LastReadMessage { get; set; }

    public virtual ChatRoom Room { get; set; } = null!;

    public virtual AppUser User { get; set; } = null!;
}
