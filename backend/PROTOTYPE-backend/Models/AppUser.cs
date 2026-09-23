using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

public partial class AppUser
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();

    public virtual ICollection<Calendar> Calendars { get; set; } = new List<Calendar>();

    public virtual ICollection<ChatRoomMember> ChatRoomMembers { get; set; } = new List<ChatRoomMember>();

    public virtual ICollection<E2eChatMessage> E2eChatMessages { get; set; } = new List<E2eChatMessage>();

    public virtual ICollection<E2eRoomKey> E2eRoomKeys { get; set; } = new List<E2eRoomKey>();

    public virtual ICollection<E2eUserKey> E2eUserKeys { get; set; } = new List<E2eUserKey>();

    public virtual ICollection<JwtBlacklistedToken> JwtBlacklistedTokens { get; set; } = new List<JwtBlacklistedToken>();

    public virtual ICollection<ProjectActivityLog> ProjectActivityLogs { get; set; } = new List<ProjectActivityLog>();

    public virtual ICollection<ProjectComment> ProjectComments { get; set; } = new List<ProjectComment>();

    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

    public virtual ICollection<ProjectTask> ProjectTaskAssignees { get; set; } = new List<ProjectTask>();

    public virtual ICollection<ProjectTask> ProjectTaskReporters { get; set; } = new List<ProjectTask>();

    public virtual ICollection<StorageFileVersion> StorageFileVersions { get; set; } = new List<StorageFileVersion>();

    public virtual ICollection<StorageNode> StorageNodes { get; set; } = new List<StorageNode>();

    public virtual ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();

    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();

    public virtual ICollection<UserWorkspace> UserWorkspaces { get; set; } = new List<UserWorkspace>();

    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
}
