using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace PROTOTYPE_backend.Data;

public partial class AppDbContext : DbContext, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AppInvitation> AppInvitations { get; set; }

    public virtual DbSet<AppRole> AppRoles { get; set; }

    public virtual DbSet<AppUser> AppUsers { get; set; }

    public virtual DbSet<CalendarEvent> CalendarEvents { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<ChatMessageAttachment> ChatMessageAttachments { get; set; }

    public virtual DbSet<ChatRoom> ChatRooms { get; set; }

    public virtual DbSet<ChatRoomMember> ChatRoomMembers { get; set; }

    public virtual DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    public virtual DbSet<EnumLookup> EnumLookups { get; set; }

    public virtual DbSet<JwtBlacklistedToken> JwtBlacklistedTokens { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectActivityLog> ProjectActivityLogs { get; set; }

    public virtual DbSet<ProjectComment> ProjectComments { get; set; }

    public virtual DbSet<ProjectLabel> ProjectLabels { get; set; }

    public virtual DbSet<ProjectMember> ProjectMembers { get; set; }

    public virtual DbSet<ProjectTask> ProjectTasks { get; set; }

    public virtual DbSet<StorageNode> StorageNodes { get; set; }

    public virtual DbSet<StorageNodeLabel> StorageNodeLabels { get; set; }

    public virtual DbSet<User2faCode> User2faCodes { get; set; }

    public virtual DbSet<UserNotification> UserNotifications { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    public virtual DbSet<Workspace> Workspaces { get; set; }

    public virtual DbSet<WorkspaceMember> WorkspaceMembers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<AppInvitation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("app_invitation", tb => tb.HasComment("Meghívások kezelése státuszkövetéssel"));

            entity.HasIndex(e => e.InvitedBy, "fk_inv_invited_by");

            entity.HasIndex(e => e.RoleId, "fk_inv_role");

            entity.HasIndex(e => new { e.InvitedEmail, e.Status }, "idx_inv_email_status");

            entity.HasIndex(e => new { e.InvitedUserId, e.Status }, "idx_inv_user_status");

            entity.HasIndex(e => e.Token, "token").IsUnique();

            entity.Property(e => e.Id)
                .HasComment("Meghívó azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Kiküldés ideje")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasComment("A meghívó lejárati ideje")
                .HasColumnType("timestamp")
                .HasColumnName("expires_at");
            entity.Property(e => e.InvitedBy)
                .HasComment("A meghívást kiküldő felhasználó")
                .HasColumnName("invited_by");
            entity.Property(e => e.InvitedEmail)
                .HasComment("A meghívott e-mail címe")
                .HasColumnName("invited_email");
            entity.Property(e => e.InvitedUserId)
                .HasComment("A meghívott felhasználó azonosítója (ha regisztrált)")
                .HasColumnName("invited_user_id");
            entity.Property(e => e.RespondedAt)
                .HasComment("A válaszadás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("responded_at");
            entity.Property(e => e.RoleId)
                .HasComment("A felajánlott szerepkör azonosítója")
                .HasColumnName("role_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'1'")
                .HasComment("1 = pending, 2 = accepted, 3 = refused, 4 = expired")
                .HasColumnName("status");
            entity.Property(e => e.TargetId)
                .HasComment("A workspace_id vagy project_id azonosítója")
                .HasColumnName("target_id");
            entity.Property(e => e.Token)
                .HasComment("Egyedi biztonsági token")
                .HasColumnName("token");
            entity.Property(e => e.Type)
                .HasComment("1 = workspace, 2 = project")
                .HasColumnName("type");

            entity.HasOne(d => d.InvitedByNavigation).WithMany(p => p.AppInvitationInvitedByNavigations)
                .HasForeignKey(d => d.InvitedBy)
                .HasConstraintName("fk_inv_invited_by");

            entity.HasOne(d => d.InvitedUser).WithMany(p => p.AppInvitationInvitedUsers)
                .HasForeignKey(d => d.InvitedUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_inv_invited_user");

            entity.HasOne(d => d.Role).WithMany(p => p.AppInvitations)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_inv_role");
        });

        modelBuilder.Entity<AppRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("app_role", tb => tb.HasComment("Globális szerepkörök"));

            entity.HasIndex(e => e.Name, "name").IsUnique();

            entity.Property(e => e.Id)
                .HasComment("Szerepkör azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Létrehozás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasComment("Leírás")
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.IsSystem)
                .HasComment("Beépített szerepkör (nem törölhető)")
                .HasColumnName("is_system");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasComment("Szerepkör neve (globálisan egyedi)")
                .HasColumnName("name");

            entity.HasMany(d => d.Permissions).WithMany(p => p.Roles)
                .UsingEntity<Dictionary<string, object>>(
                    "RolePermission",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .HasConstraintName("fk_rp_permission"),
                    l => l.HasOne<AppRole>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("fk_rp_role"),
                    j =>
                    {
                        j.HasKey("RoleId", "PermissionId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("role_permission", tb => tb.HasComment("Szerepkörökhöz rendelt jogosultságok"));
                        j.HasIndex(new[] { "PermissionId" }, "fk_rp_permission");
                        j.IndexerProperty<Guid>("RoleId")
                            .HasComment("Szerepkör")
                            .HasColumnName("role_id");
                        j.IndexerProperty<Guid>("PermissionId")
                            .HasComment("Jogosultság")
                            .HasColumnName("permission_id");
                    });
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("app_user", tb => tb.HasComment("Alkalmazás felhasználói és 2FA beállításaik"));

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.Property(e => e.Id)
                .HasComment("Felhasználó azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .HasComment("Profilkép URL")
                .HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Létrehozás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt)
                .HasComment("Soft delete: a törlés ideje, NULL = aktív felhasználó")
                .HasColumnType("timestamp")
                .HasColumnName("deleted_at");
            entity.Property(e => e.Email)
                .HasComment("Bejelentkezési e-mail cím (egyedi)")
                .HasColumnName("email");
            entity.Property(e => e.GlobalRole)
                .HasDefaultValueSql("'1'")
                .HasComment("1 = User, 2 = SystemAdmin")
                .HasColumnName("global_role");
            entity.Property(e => e.Is2faEnabled)
                .HasComment("Be van-e kapcsolva a kétfaktoros hitelesítés")
                .HasColumnName("is_2fa_enabled");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasComment("Megjelenített név")
                .HasColumnName("name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasComment("Jelszó hash")
                .HasColumnName("password_hash");
            entity.Property(e => e.TwoFactorRecoveryCodes)
                .HasComment("2FA helyreállító kódok")
                .HasColumnType("text")
                .HasColumnName("two_factor_recovery_codes");
            entity.Property(e => e.TwoFactorSecret)
                .HasMaxLength(255)
                .HasComment("2FA titkos kulcs (Authenticator)")
                .HasColumnName("two_factor_secret");
            entity.Property(e => e.TwoFactorType)
                .HasComment("0 = None, 1 = Authenticator, 2 = Email")
                .HasColumnName("two_factor_type");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Utolsó módosítás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<CalendarEvent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("calendar_event", tb => tb.HasComment("Naptár események"));

            entity.HasIndex(e => e.CreatedBy, "fk_ce_creator");

            entity.HasIndex(e => new { e.StartTime, e.EndTime }, "idx_ce_dates");

            entity.HasIndex(e => new { e.ProjectId, e.StartTime }, "idx_ce_project_start");

            entity.Property(e => e.Id)
                .HasComment("Esemény azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedBy)
                .HasComment("Létrehozó")
                .HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasComment("Leírás")
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.EndTime)
                .HasComment("Befejezés")
                .HasColumnType("datetime")
                .HasColumnName("end_time");
            entity.Property(e => e.IsAllDay)
                .HasComment("Egész napos-e")
                .HasColumnName("is_all_day");
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .HasComment("Helyszín")
                .HasColumnName("location");
            entity.Property(e => e.ProjectId)
                .HasComment("Projekt")
                .HasColumnName("project_id");
            entity.Property(e => e.StartTime)
                .HasComment("Kezdés")
                .HasColumnType("datetime")
                .HasColumnName("start_time");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasComment("Esemény címe")
                .HasColumnName("title");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CalendarEvents)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_ce_creator");

            entity.HasOne(d => d.Project).WithMany(p => p.CalendarEvents)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_ce_project");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("chat_message", tb => tb.HasComment("Chat üzenetek"));

            entity.HasIndex(e => e.ParentId, "fk_cm_parent");

            entity.HasIndex(e => e.SenderId, "fk_cm_sender");

            entity.HasIndex(e => new { e.RoomId, e.CreatedAt }, "idx_cm_room_created");

            entity.Property(e => e.Id)
                .HasComment("Üzenet azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Küldés ideje")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.MessageText)
                .HasComment("Üzenet szövege")
                .HasColumnType("text")
                .HasColumnName("message_text");
            entity.Property(e => e.ParentId)
                .HasComment("Szülő üzenet")
                .HasColumnName("parent_id");
            entity.Property(e => e.RoomId)
                .HasComment("Szoba")
                .HasColumnName("room_id");
            entity.Property(e => e.SenderId)
                .HasComment("Feladó")
                .HasColumnName("sender_id");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_cm_parent");

            entity.HasOne(d => d.Room).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("fk_cm_room");

            entity.HasOne(d => d.Sender).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_cm_sender");
        });

        modelBuilder.Entity<ChatMessageAttachment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("chat_message_attachment", tb => tb.HasComment("Chat üzenet mellékletek"));

            entity.HasIndex(e => e.MessageId, "fk_cma_message");

            entity.HasIndex(e => e.StorageNodeId, "fk_cma_storage");

            entity.Property(e => e.Id)
                .HasComment("Melléklet azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .HasComment("Fájl neve")
                .HasColumnName("file_name");
            entity.Property(e => e.FileUrl)
                .HasMaxLength(500)
                .HasComment("Fájl URL")
                .HasColumnName("file_url");
            entity.Property(e => e.MessageId)
                .HasComment("Üzenet")
                .HasColumnName("message_id");
            entity.Property(e => e.StorageNodeId)
                .HasComment("Kapcsolódó storage node")
                .HasColumnName("storage_node_id");

            entity.HasOne(d => d.Message).WithMany(p => p.ChatMessageAttachments)
                .HasForeignKey(d => d.MessageId)
                .HasConstraintName("fk_cma_message");

            entity.HasOne(d => d.StorageNode).WithMany(p => p.ChatMessageAttachments)
                .HasForeignKey(d => d.StorageNodeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_cma_storage");
        });

        modelBuilder.Entity<ChatRoom>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("chat_room", tb => tb.HasComment("Chat szobák"));

            entity.HasIndex(e => e.CreatedBy, "fk_cr_creator");

            entity.HasIndex(e => e.ProjectId, "fk_cr_project");

            entity.HasIndex(e => e.WorkspaceId, "fk_cr_workspace");

            entity.Property(e => e.Id)
                .HasComment("Szoba azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Létrehozás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("Létrehozó")
                .HasColumnName("created_by");
            entity.Property(e => e.IsPrivate)
                .HasComment("Privát-e")
                .HasColumnName("is_private");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasComment("Szoba neve")
                .HasColumnName("name");
            entity.Property(e => e.ProjectId)
                .HasComment("Projekt")
                .HasColumnName("project_id");
            entity.Property(e => e.Type)
                .HasDefaultValueSql("'2'")
                .HasComment("1 = direct, 2 = group, 3 = project_channel")
                .HasColumnName("type");
            entity.Property(e => e.WorkspaceId)
                .HasComment("Workspace")
                .HasColumnName("workspace_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ChatRooms)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_cr_creator");

            entity.HasOne(d => d.Project).WithMany(p => p.ChatRooms)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_cr_project");

            entity.HasOne(d => d.Workspace).WithMany(p => p.ChatRooms)
                .HasForeignKey(d => d.WorkspaceId)
                .HasConstraintName("fk_cr_workspace");
        });

        modelBuilder.Entity<ChatRoomMember>(entity =>
        {
            entity.HasKey(e => new { e.RoomId, e.UserId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("chat_room_member", tb => tb.HasComment("Chat szoba tagok"));

            entity.HasIndex(e => e.UserId, "fk_crm_user");

            entity.Property(e => e.RoomId)
                .HasComment("Szoba")
                .HasColumnName("room_id");
            entity.Property(e => e.UserId)
                .HasComment("Tag")
                .HasColumnName("user_id");
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Csatlakozás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("joined_at");

            entity.HasOne(d => d.Room).WithMany(p => p.ChatRoomMembers)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("fk_crm_room");

            entity.HasOne(d => d.User).WithMany(p => p.ChatRoomMembers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_crm_user");
        });

        modelBuilder.Entity<DataProtectionKey>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("data_protection_keys", tb => tb.HasComment("ASP.NET Core DataProtection főkulcsok perzisztens tárolása"));

            entity.Property(e => e.Id)
                .HasComment("Elsődleges kulcs")
                .HasColumnName("id");
            entity.Property(e => e.FriendlyName)
                .HasComment("Opcionális barátságos név")
                .HasColumnName("friendly_name");
            entity.Property(e => e.Xml)
                .HasComment("A titkosító főkulcs XML formátumban")
                .HasColumnName("xml");
        });

        modelBuilder.Entity<EnumLookup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("enum_lookup", tb => tb.HasComment("Rendszerszintű enum számszerű értékek szótára"));

            entity.HasIndex(e => new { e.Category, e.Code }, "uq_category_code").IsUnique();

            entity.Property(e => e.Id)
                .HasComment("Elsődleges kulcs")
                .HasColumnName("id");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasComment("Kategória neve, pl. global_role, project_status")
                .HasColumnName("category");
            entity.Property(e => e.Code)
                .HasComment("Számkód (1, 2, 3...)")
                .HasColumnName("code");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasComment("Opcionális leírás")
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasComment("Megjelenített név / kód név")
                .HasColumnName("name");
        });

        modelBuilder.Entity<JwtBlacklistedToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("jwt_blacklisted_token", tb => tb.HasComment("Visszavont JWT tokenek feketelistája"));

            entity.HasIndex(e => e.UserId, "fk_blacklist_user");

            entity.HasIndex(e => e.ExpiresAt, "idx_blacklist_expires");

            entity.HasIndex(e => e.Jti, "idx_blacklist_jti").IsUnique();

            entity.Property(e => e.Id)
                .HasComment("Bejegyzés azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Feketelistára kerülés ideje")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasComment("A token lejárata")
                .HasColumnType("timestamp")
                .HasColumnName("expires_at");
            entity.Property(e => e.Jti)
                .HasComment("JWT jti claim")
                .HasColumnName("jti");
            entity.Property(e => e.UserId)
                .HasComment("Felhasználó")
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.JwtBlacklistedTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_blacklist_user");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("permission", tb => tb.HasComment("Jogosultságok katalógusa"));

            entity.HasIndex(e => e.Code, "code").IsUnique();

            entity.Property(e => e.Id)
                .HasComment("Jogosultság azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .HasComment("Gépi kód, pl. workspace.manage, project.task.create")
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Létrehozás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasComment("Leírás")
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasComment("Olvasható név")
                .HasColumnName("name");
            entity.Property(e => e.Scope)
                .HasDefaultValueSql("'1'")
                .HasComment("1 = workspace, 2 = project")
                .HasColumnName("scope");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("project", tb => tb.HasComment("Projektek"));

            entity.HasIndex(e => e.OwnerId, "fk_project_owner");

            entity.HasIndex(e => e.WorkspaceId, "idx_project_workspace");

            entity.Property(e => e.Id)
                .HasComment("Projekt azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Létrehozás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Deadline)
                .HasComment("Határidő")
                .HasColumnName("deadline");
            entity.Property(e => e.Description)
                .HasComment("Projekt leírása")
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasComment("Projekt neve")
                .HasColumnName("name");
            entity.Property(e => e.OwnerId)
                .HasComment("Projekt felelőse")
                .HasColumnName("owner_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'1'")
                .HasComment("1 = planned, 2 = active, 3 = on_hold, 4 = completed, 5 = archived")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Utolsó módosítás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");
            entity.Property(e => e.WorkspaceId)
                .HasComment("Workspace, amelyhez a projekt tartozik")
                .HasColumnName("workspace_id");

            entity.HasOne(d => d.Owner).WithMany(p => p.Projects)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_project_owner");

            entity.HasOne(d => d.Workspace).WithMany(p => p.Projects)
                .HasForeignKey(d => d.WorkspaceId)
                .HasConstraintName("fk_project_workspace");
        });

        modelBuilder.Entity<ProjectActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("project_activity_log", tb => tb.HasComment("Projekt tevékenységnapló"));

            entity.HasIndex(e => e.TaskId, "fk_act_task");

            entity.HasIndex(e => e.UserId, "fk_act_user");

            entity.Property(e => e.Id)
                .HasComment("Naplóbejegyzés azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .HasComment("Művelet kódja")
                .HasColumnName("action");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Művelet ideje")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Details)
                .HasComment("Részletek")
                .HasColumnType("json")
                .HasColumnName("details");
            entity.Property(e => e.TaskId)
                .HasComment("Érintett task")
                .HasColumnName("task_id");
            entity.Property(e => e.UserId)
                .HasComment("Műveletet végző felhasználó")
                .HasColumnName("user_id");

            entity.HasOne(d => d.Task).WithMany(p => p.ProjectActivityLogs)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_act_task");

            entity.HasOne(d => d.User).WithMany(p => p.ProjectActivityLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_act_user");
        });

        modelBuilder.Entity<ProjectComment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("project_comment", tb => tb.HasComment("Task kommentek"));

            entity.HasIndex(e => e.TaskId, "fk_comment_task");

            entity.HasIndex(e => e.UserId, "fk_comment_user");

            entity.Property(e => e.Id)
                .HasComment("Komment azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.Content)
                .HasComment("Szöveg")
                .HasColumnType("text")
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Létrehozás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.TaskId)
                .HasComment("Task")
                .HasColumnName("task_id");
            entity.Property(e => e.UserId)
                .HasComment("Szerző")
                .HasColumnName("user_id");

            entity.HasOne(d => d.Task).WithMany(p => p.ProjectComments)
                .HasForeignKey(d => d.TaskId)
                .HasConstraintName("fk_comment_task");

            entity.HasOne(d => d.User).WithMany(p => p.ProjectComments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_comment_user");
        });

        modelBuilder.Entity<ProjectLabel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("project_label", tb => tb.HasComment("Projekt címkék"));

            entity.HasIndex(e => e.ProjectId, "fk_label_project");

            entity.Property(e => e.Id)
                .HasComment("Címke azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.Color)
                .HasMaxLength(7)
                .HasDefaultValueSql("'#888888'")
                .HasComment("Szín hex kódja")
                .HasColumnName("color");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasComment("Címke neve")
                .HasColumnName("name");
            entity.Property(e => e.ProjectId)
                .HasComment("Projekt")
                .HasColumnName("project_id");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectLabels)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("fk_label_project");
        });

        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.HasKey(e => new { e.ProjectId, e.UserId, e.RoleId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0, 0 });

            entity.ToTable("project_member", tb => tb.HasComment("Projekttagságok"));

            entity.HasIndex(e => e.RoleId, "fk_pm_role");

            entity.HasIndex(e => e.WorkspaceId, "fk_pm_workspace");

            entity.HasIndex(e => new { e.ProjectId, e.RoleId }, "idx_pm_role");

            entity.HasIndex(e => e.UserId, "idx_pm_user");

            entity.Property(e => e.ProjectId)
                .HasComment("Projekt azonosító")
                .HasColumnName("project_id");
            entity.Property(e => e.UserId)
                .HasComment("Felhasználó azonosító")
                .HasColumnName("user_id");
            entity.Property(e => e.RoleId)
                .HasComment("A tag szerepköre a projektben")
                .HasColumnName("role_id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Hozzárendelés ideje")
                .HasColumnType("timestamp")
                .HasColumnName("assigned_at");
            entity.Property(e => e.IsWorkspaceMember)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasComment("Jelzi, hogy a felhasználó a felettes workspace-nek is tagja-e")
                .HasColumnName("is_workspace_member");
            entity.Property(e => e.WorkspaceId)
                .HasComment("A projekt workspace-e")
                .HasColumnName("workspace_id");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectMembers)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("fk_pm_project");

            entity.HasOne(d => d.Role).WithMany(p => p.ProjectMembers)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pm_role");

            entity.HasOne(d => d.User).WithMany(p => p.ProjectMembers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_pm_user");

            entity.HasOne(d => d.Workspace).WithMany(p => p.ProjectMembers)
                .HasForeignKey(d => d.WorkspaceId)
                .HasConstraintName("fk_pm_workspace");
        });

        modelBuilder.Entity<ProjectTask>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("project_task", tb => tb.HasComment("Projekt taskok"));

            entity.HasIndex(e => e.AssigneeRoleId, "fk_task_assignee_role");

            entity.HasIndex(e => e.CreatedBy, "fk_task_creator");

            entity.HasIndex(e => e.AssigneeUserId, "idx_task_assignee_user");

            entity.HasIndex(e => new { e.ProjectId, e.DueDate }, "idx_task_project_due");

            entity.HasIndex(e => new { e.ProjectId, e.AssigneeRoleId }, "idx_task_project_role");

            entity.Property(e => e.Id)
                .HasComment("Task azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.AssigneeRoleId)
                .HasComment("Szerepkör, amire a task ki van hirdetve")
                .HasColumnName("assignee_role_id");
            entity.Property(e => e.AssigneeUserId)
                .HasComment("Konkrét felhasználó, aki lefoglalta a feladatot")
                .HasColumnName("assignee_user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Létrehozás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("Létrehozó felhasználó")
                .HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasComment("Task leírása")
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.DueDate)
                .HasComment("Határidő")
                .HasColumnName("due_date");
            entity.Property(e => e.Priority)
                .HasDefaultValueSql("'2'")
                .HasComment("1 = low, 2 = medium, 3 = high, 4 = urgent")
                .HasColumnName("priority");
            entity.Property(e => e.ProjectId)
                .HasComment("Projekt, amelyhez a task tartozik")
                .HasColumnName("project_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'1'")
                .HasComment("1 = planning, 2 = on_going, 3 = ready")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasComment("Task címe")
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Utolsó módosítás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.AssigneeRole).WithMany(p => p.ProjectTasks)
                .HasForeignKey(d => d.AssigneeRoleId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_task_assignee_role");

            entity.HasOne(d => d.AssigneeUser).WithMany(p => p.ProjectTaskAssigneeUsers)
                .HasForeignKey(d => d.AssigneeUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_task_assignee_user");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ProjectTaskCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_task_creator");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectTasks)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("fk_task_project");

            entity.HasMany(d => d.Labels).WithMany(p => p.Tasks)
                .UsingEntity<Dictionary<string, object>>(
                    "ProjectTaskLabel",
                    r => r.HasOne<ProjectLabel>().WithMany()
                        .HasForeignKey("LabelId")
                        .HasConstraintName("fk_ptl_label"),
                    l => l.HasOne<ProjectTask>().WithMany()
                        .HasForeignKey("TaskId")
                        .HasConstraintName("fk_ptl_task"),
                    j =>
                    {
                        j.HasKey("TaskId", "LabelId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("project_task_label", tb => tb.HasComment("Taskok és címkék kapcsolata"));
                        j.HasIndex(new[] { "LabelId" }, "fk_ptl_label");
                        j.IndexerProperty<Guid>("TaskId")
                            .HasComment("Task")
                            .HasColumnName("task_id");
                        j.IndexerProperty<Guid>("LabelId")
                            .HasComment("Címke")
                            .HasColumnName("label_id");
                    });
        });

        modelBuilder.Entity<StorageNode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("storage_node", tb => tb.HasComment("Fájlok és mappák fastruktúrája"));

            entity.HasIndex(e => e.CreatedBy, "fk_sn_creator");

            entity.HasIndex(e => e.UpdatedBy, "fk_sn_updater");

            entity.HasIndex(e => e.ParentId, "idx_sn_parent");

            entity.HasIndex(e => e.ProjectId, "idx_sn_project");

            entity.HasIndex(e => new { e.WorkspaceId, e.Type }, "idx_sn_workspace_type");

            entity.Property(e => e.Id)
                .HasComment("Node azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Létrehozás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("Létrehozó felhasználó")
                .HasColumnName("created_by");
            entity.Property(e => e.DeletedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Törlés ideje")
                .HasColumnType("timestamp")
                .HasColumnName("deleted_at");
            entity.Property(e => e.MimeType)
                .HasMaxLength(127)
                .HasComment("MIME típus")
                .HasColumnName("mime_type");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasComment("Fájl vagy mappa neve")
                .HasColumnName("name");
            entity.Property(e => e.ParentId)
                .HasComment("Szülő mappa (NULL = gyökér)")
                .HasColumnName("parent_id");
            entity.Property(e => e.ProjectId)
                .HasComment("Projekt")
                .HasColumnName("project_id");
            entity.Property(e => e.SizeBytes)
                .HasDefaultValueSql("'0'")
                .HasComment("Méret bájtban")
                .HasColumnName("size_bytes");
            entity.Property(e => e.StoragePath)
                .HasMaxLength(500)
                .HasComment("Fájl fizikai helye")
                .HasColumnName("storage_path");
            entity.Property(e => e.Type)
                .HasDefaultValueSql("'1'")
                .HasComment("1 = folder, 2 = file")
                .HasColumnName("type");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Utolsó módosítás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("Utolsó módosító")
                .HasColumnName("updated_by");
            entity.Property(e => e.WorkspaceId)
                .HasComment("Workspace")
                .HasColumnName("workspace_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.StorageNodeCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_sn_creator");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_sn_parent");

            entity.HasOne(d => d.Project).WithMany(p => p.StorageNodes)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_sn_project");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.StorageNodeUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_sn_updater");

            entity.HasOne(d => d.Workspace).WithMany(p => p.StorageNodes)
                .HasForeignKey(d => d.WorkspaceId)
                .HasConstraintName("fk_sn_workspace");

            entity.HasMany(d => d.Roles).WithMany(p => p.Nodes)
                .UsingEntity<Dictionary<string, object>>(
                    "StorageNodeRole",
                    r => r.HasOne<AppRole>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("fk_snr_role"),
                    l => l.HasOne<StorageNode>().WithMany()
                        .HasForeignKey("NodeId")
                        .HasConstraintName("fk_snr_node"),
                    j =>
                    {
                        j.HasKey("NodeId", "RoleId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("storage_node_role", tb => tb.HasComment("Fájlok és mappák szerepkör-alapú védelme"));
                        j.HasIndex(new[] { "RoleId" }, "fk_snr_role");
                        j.IndexerProperty<Guid>("NodeId")
                            .HasComment("Fájl vagy Mappa")
                            .HasColumnName("node_id");
                        j.IndexerProperty<Guid>("RoleId")
                            .HasComment("Szerepkör hozzáférés")
                            .HasColumnName("role_id");
                    });
        });

        modelBuilder.Entity<StorageNodeLabel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("storage_node_label", tb => tb.HasComment("Fájlok és mappák egyedi címkéi"));

            entity.HasIndex(e => e.Label, "idx_snl_label");

            entity.HasIndex(e => e.NodeId, "idx_snl_node");

            entity.Property(e => e.Id)
                .HasComment("Címke azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Létrehozás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Label)
                .HasMaxLength(100)
                .HasComment("Címke felirata")
                .HasColumnName("label");
            entity.Property(e => e.NodeId)
                .HasComment("Kapcsolódó fájl vagy mappa")
                .HasColumnName("node_id");

            entity.HasOne(d => d.Node).WithMany(p => p.StorageNodeLabels)
                .HasForeignKey(d => d.NodeId)
                .HasConstraintName("fk_snl_node");
        });

        modelBuilder.Entity<User2faCode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("user_2fa_code", tb => tb.HasComment("Egyszeri 2FA kódok"));

            entity.HasIndex(e => new { e.UserId, e.IsUsed, e.ExpiresAt }, "idx_u2c_user_active");

            entity.Property(e => e.Id)
                .HasComment("Kód azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.CodeHash)
                .HasMaxLength(255)
                .HasComment("Kód hash")
                .HasColumnName("code_hash");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Létrehozás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasComment("Lejárat ideje")
                .HasColumnType("timestamp")
                .HasColumnName("expires_at");
            entity.Property(e => e.FailedAttempts)
                .HasComment("Sikertelen próbálkozások")
                .HasColumnName("failed_attempts");
            entity.Property(e => e.IsUsed)
                .HasComment("Felhasználták-e")
                .HasColumnName("is_used");
            entity.Property(e => e.MaxAttempts)
                .HasDefaultValueSql("'5'")
                .HasComment("Max próbálkozás")
                .HasColumnName("max_attempts");
            entity.Property(e => e.UserId)
                .HasComment("Felhasználó")
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.User2faCodes)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_u2c_user");
        });

        modelBuilder.Entity<DataProtectionKey>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("data_protection_keys", tb => tb.HasComment("ASP.NET Core DataProtection főkulcsok perzisztens tárolása"));

            entity.Property(e => e.Id)
                .HasComment("Elsődleges kulcs")
                .HasColumnName("id");

            entity.Property(e => e.FriendlyName)
                .HasComment("Opcionális barátságos név")
                .HasColumnName("friendly_name");

            entity.Property(e => e.Xml)
                .HasComment("A titkosító főkulcs XML formátumban")
                .HasColumnName("xml");
        });

        OnModelCreatingPartial(modelBuilder);

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("user_notification", tb => tb.HasComment("Felhasználói értesítések"));

            entity.HasIndex(e => e.UserId, "fk_notif_user");

            entity.Property(e => e.Id)
                .HasComment("Értesítés azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.Content)
                .HasMaxLength(500)
                .HasComment("Szöveg")
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Létrehozás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead)
                .HasComment("Elolvasta-e")
                .HasColumnName("is_read");
            entity.Property(e => e.UserId)
                .HasComment("Címzett felhasználó")
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserNotifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_notif_user");
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("user_session", tb => tb.HasComment("Felhasználói munkamenetek"));

            entity.HasIndex(e => e.ReplacedBySessionId, "fk_session_replaced");

            entity.HasIndex(e => e.RefreshTokenHash, "idx_session_refresh").IsUnique();

            entity.HasIndex(e => e.UserId, "idx_session_user");

            entity.Property(e => e.Id)
                .HasComment("Munkamenet azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Létrehozás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceId)
                .HasMaxLength(255)
                .HasComment("Eszköz azonosító")
                .HasColumnName("device_id");
            entity.Property(e => e.DeviceInfo)
                .HasMaxLength(255)
                .HasComment("Böngésző / OS infó")
                .HasColumnName("device_info");
            entity.Property(e => e.ExpiresAt)
                .HasComment("Lejárat ideje")
                .HasColumnType("timestamp")
                .HasColumnName("expires_at");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasComment("IP cím")
                .HasColumnName("ip_address");
            entity.Property(e => e.IsRevoked)
                .HasComment("Visszavonták-e")
                .HasColumnName("is_revoked");
            entity.Property(e => e.RefreshTokenHash)
                .HasMaxLength(64)
                .HasComment("Refresh token hash")
                .HasColumnName("refresh_token_hash");
            entity.Property(e => e.ReplacedBySessionId)
                .HasComment("Rotációs felváltó session")
                .HasColumnName("replaced_by_session_id");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Utolsó módosítás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasComment("Felhasználó")
                .HasColumnName("user_id");

            entity.HasOne(d => d.ReplacedBySession).WithMany(p => p.InverseReplacedBySession)
                .HasForeignKey(d => d.ReplacedBySessionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_session_replaced");

            entity.HasOne(d => d.User).WithMany(p => p.UserSessions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_session_user");
        });

        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("workspace", tb => tb.HasComment("Workspace-ek (a legfelső szintű szervezeti egység)"));

            entity.HasIndex(e => e.OwnerId, "idx_workspace_owner");

            entity.Property(e => e.Id)
                .HasComment("Workspace azonosító (UUID)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Létrehozás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasComment("Workspace leírása")
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasComment("Workspace neve")
                .HasColumnName("name");
            entity.Property(e => e.OwnerId)
                .HasComment("Tulajdonos felhasználó")
                .HasColumnName("owner_id");
            entity.Property(e => e.StorageQuotaBytes)
                .HasDefaultValueSql("'26843545600'")
                .HasComment("Tárhelykvóta bájtban (alapértelmezett: 25 GB)")
                .HasColumnName("storage_quota_bytes");

            entity.HasOne(d => d.Owner).WithMany(p => p.Workspaces)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_workspace_owner");
        });

        modelBuilder.Entity<WorkspaceMember>(entity =>
        {
            entity.HasKey(e => new { e.WorkspaceId, e.UserId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("workspace_member", tb => tb.HasComment("Workspace tagságok és szerepkörük"));

            entity.HasIndex(e => e.RoleId, "fk_wm_role");

            entity.HasIndex(e => e.UserId, "idx_wm_user");

            entity.Property(e => e.WorkspaceId)
                .HasComment("Workspace")
                .HasColumnName("workspace_id");
            entity.Property(e => e.UserId)
                .HasComment("Tag")
                .HasColumnName("user_id");
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("Csatlakozás ideje")
                .HasColumnType("timestamp")
                .HasColumnName("joined_at");
            entity.Property(e => e.RoleId)
                .HasComment("A tag szerepköre a workspace-ben")
                .HasColumnName("role_id");

            entity.HasOne(d => d.Role).WithMany(p => p.WorkspaceMembers)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_wm_role");

            entity.HasOne(d => d.User).WithMany(p => p.WorkspaceMembers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_wm_user");

            entity.HasOne(d => d.Workspace).WithMany(p => p.WorkspaceMembers)
                .HasForeignKey(d => d.WorkspaceId)
                .HasConstraintName("fk_wm_workspace");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
