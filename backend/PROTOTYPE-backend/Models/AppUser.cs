using System;
using System.Collections.Generic;

namespace PROTOTYPE_backend.Models;

/// <summary>
/// Alkalmazás felhasználói és 2FA beállításaik
/// </summary>
public partial class AppUser
{
    /// <summary>
    /// Felhasználó azonosító (UUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Megjelenített név
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Bejelentkezési e-mail cím (egyedi)
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Jelszó hash
    /// </summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// Profilkép URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Létrehozás ideje
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Utolsó módosítás ideje
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 1 = User, 2 = SystemAdmin
    /// </summary>
    public sbyte GlobalRole { get; set; }

    /// <summary>
    /// Soft delete: a törlés ideje, NULL = aktív felhasználó
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Be van-e kapcsolva a kétfaktoros hitelesítés
    /// </summary>
    public bool Is2faEnabled { get; set; }

    /// <summary>
    /// 0 = None, 1 = Authenticator, 2 = Email
    /// </summary>
    public sbyte TwoFactorType { get; set; }

    /// <summary>
    /// 2FA titkos kulcs (Authenticator)
    /// </summary>
    public string? TwoFactorSecret { get; set; }

    /// <summary>
    /// 2FA helyreállító kódok
    /// </summary>
    public string? TwoFactorRecoveryCodes { get; set; }

    public virtual ICollection<AppInvitation> AppInvitationInvitedByNavigations { get; set; } = new List<AppInvitation>();

    public virtual ICollection<AppInvitation> AppInvitationInvitedUsers { get; set; } = new List<AppInvitation>();

    public virtual ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual ICollection<ChatRoomMember> ChatRoomMembers { get; set; } = new List<ChatRoomMember>();

    public virtual ICollection<ChatRoom> ChatRooms { get; set; } = new List<ChatRoom>();

    public virtual ICollection<JwtBlacklistedToken> JwtBlacklistedTokens { get; set; } = new List<JwtBlacklistedToken>();

    public virtual ICollection<ProjectActivityLog> ProjectActivityLogs { get; set; } = new List<ProjectActivityLog>();

    public virtual ICollection<ProjectComment> ProjectComments { get; set; } = new List<ProjectComment>();

    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

    public virtual ICollection<ProjectTask> ProjectTaskAssigneeUsers { get; set; } = new List<ProjectTask>();

    public virtual ICollection<ProjectTask> ProjectTaskCreatedByNavigations { get; set; } = new List<ProjectTask>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<StorageNode> StorageNodeCreatedByNavigations { get; set; } = new List<StorageNode>();

    public virtual ICollection<StorageNode> StorageNodeUpdatedByNavigations { get; set; } = new List<StorageNode>();

    public virtual ICollection<User2faCode> User2faCodes { get; set; } = new List<User2faCode>();

    public virtual ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();

    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();

    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();

    public virtual ICollection<Workspace> Workspaces { get; set; } = new List<Workspace>();
}
