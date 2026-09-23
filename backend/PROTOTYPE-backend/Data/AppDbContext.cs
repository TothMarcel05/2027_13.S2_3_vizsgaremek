using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Models;

namespace PROTOTYPE_backend.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AppProject> AppProjects { get; set; }

    public virtual DbSet<AppUser> AppUsers { get; set; }

    public virtual DbSet<Calendar> Calendars { get; set; }

    public virtual DbSet<CalendarEvent> CalendarEvents { get; set; }

    public virtual DbSet<ChatMessageAttachment> ChatMessageAttachments { get; set; }

    public virtual DbSet<ChatRoom> ChatRooms { get; set; }

    public virtual DbSet<ChatRoomMember> ChatRoomMembers { get; set; }

    public virtual DbSet<E2eChatMessage> E2eChatMessages { get; set; }

    public virtual DbSet<E2eRoomKey> E2eRoomKeys { get; set; }

    public virtual DbSet<E2eUserKey> E2eUserKeys { get; set; }

    public virtual DbSet<JwtBlacklistedToken> JwtBlacklistedTokens { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<ProjectActivityLog> ProjectActivityLogs { get; set; }

    public virtual DbSet<ProjectComment> ProjectComments { get; set; }

    public virtual DbSet<ProjectLabel> ProjectLabels { get; set; }

    public virtual DbSet<ProjectMember> ProjectMembers { get; set; }

    public virtual DbSet<ProjectTask> ProjectTasks { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<StorageFileVersion> StorageFileVersions { get; set; }

    public virtual DbSet<StorageNode> StorageNodes { get; set; }

    public virtual DbSet<StorageTag> StorageTags { get; set; }

    public virtual DbSet<StorageTagRbac> StorageTagRbacs { get; set; }

    public virtual DbSet<UserNotification> UserNotifications { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    public virtual DbSet<UserWorkspace> UserWorkspaces { get; set; }

    public virtual DbSet<WorkspaceMember> WorkspaceMembers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<AppProject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("app_projects");

            entity.HasIndex(e => e.WorkspaceId, "idx_app_projects_workspace");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Deadline).HasColumnName("deadline");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'planned'")
                .HasColumnType("enum('planned','active','on_hold','completed','archived')")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.Workspace).WithMany(p => p.AppProjects)
                .HasForeignKey(d => d.WorkspaceId)
                .HasConstraintName("fk_app_projects_workspace");
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("app_users");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Calendar>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("calendars");

            entity.HasIndex(e => e.ProjectId, "fk_cal_project");

            entity.HasIndex(e => e.UserId, "fk_cal_user");

            entity.HasIndex(e => e.WorkspaceId, "fk_cal_workspace");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Color)
                .HasMaxLength(7)
                .HasDefaultValueSql("'#3B82F6'")
                .HasColumnName("color");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Type)
                .HasDefaultValueSql("'personal'")
                .HasColumnType("enum('personal','workspace_user','workspace_shared','project')")
                .HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.Project).WithMany(p => p.Calendars)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_cal_project");

            entity.HasOne(d => d.User).WithMany(p => p.Calendars)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_cal_user");

            entity.HasOne(d => d.Workspace).WithMany(p => p.Calendars)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_cal_workspace");
        });

        modelBuilder.Entity<CalendarEvent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("calendar_events");

            entity.HasIndex(e => e.CalendarId, "fk_ce_calendar");

            entity.HasIndex(e => e.CreatedBy, "fk_ce_creator");

            entity.HasIndex(e => e.TaskId, "fk_ce_task");

            entity.HasIndex(e => new { e.StartTime, e.EndTime }, "idx_ce_dates");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CalendarId).HasColumnName("calendar_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.EndTime)
                .HasColumnType("datetime")
                .HasColumnName("end_time");
            entity.Property(e => e.IsAllDay).HasColumnName("is_all_day");
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .HasColumnName("location");
            entity.Property(e => e.StartTime)
                .HasColumnType("datetime")
                .HasColumnName("start_time");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");

            entity.HasOne(d => d.Calendar).WithMany(p => p.CalendarEvents)
                .HasForeignKey(d => d.CalendarId)
                .HasConstraintName("fk_ce_calendar");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CalendarEvents)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_ce_creator");

            entity.HasOne(d => d.Task).WithMany(p => p.CalendarEvents)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_ce_task");
        });

        modelBuilder.Entity<ChatMessageAttachment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("chat_message_attachments");

            entity.HasIndex(e => e.MessageId, "fk_cma_message");

            entity.HasIndex(e => e.StorageNodeId, "fk_cma_storage");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .HasColumnName("file_name");
            entity.Property(e => e.FileUrl)
                .HasMaxLength(500)
                .HasColumnName("file_url");
            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.StorageNodeId).HasColumnName("storage_node_id");

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

            entity.ToTable("chat_rooms");

            entity.HasIndex(e => e.ProjectId, "fk_cr_project");

            entity.HasIndex(e => e.WorkspaceId, "fk_cr_workspace");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsPrivate).HasColumnName("is_private");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Type)
                .HasDefaultValueSql("'group'")
                .HasColumnType("enum('direct','group','project_channel')")
                .HasColumnName("type");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

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

            entity.ToTable("chat_room_members");

            entity.HasIndex(e => e.LastReadMessageId, "fk_crm_last_msg");

            entity.HasIndex(e => e.UserId, "fk_crm_user");

            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("joined_at");
            entity.Property(e => e.LastReadMessageId).HasColumnName("last_read_message_id");

            entity.HasOne(d => d.LastReadMessage).WithMany(p => p.ChatRoomMembers)
                .HasForeignKey(d => d.LastReadMessageId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_crm_last_msg");

            entity.HasOne(d => d.Room).WithMany(p => p.ChatRoomMembers)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("fk_crm_room");

            entity.HasOne(d => d.User).WithMany(p => p.ChatRoomMembers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_crm_user");
        });

        modelBuilder.Entity<E2eChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("e2e_chat_messages");

            entity.HasIndex(e => e.ParentId, "fk_e2e_cm_parent");

            entity.HasIndex(e => e.SenderId, "fk_e2e_cm_sender");

            entity.HasIndex(e => new { e.RoomId, e.CreatedAt }, "idx_e2e_cm_room_created");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Ciphertext)
                .HasColumnType("text")
                .HasColumnName("ciphertext");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.KeyVersion)
                .HasDefaultValueSql("'1'")
                .HasColumnName("key_version");
            entity.Property(e => e.NonceOrIv)
                .HasMaxLength(255)
                .HasColumnName("nonce_or_iv");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.SenderDeviceId)
                .HasMaxLength(255)
                .HasColumnName("sender_device_id");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_e2e_cm_parent");

            entity.HasOne(d => d.Room).WithMany(p => p.E2eChatMessages)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("fk_e2e_cm_room");

            entity.HasOne(d => d.Sender).WithMany(p => p.E2eChatMessages)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_e2e_cm_sender");
        });

        modelBuilder.Entity<E2eRoomKey>(entity =>
        {
            entity.HasKey(e => new { e.RoomId, e.UserId, e.KeyVersion })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0, 0 });

            entity.ToTable("e2e_room_keys");

            entity.HasIndex(e => e.UserId, "fk_e2e_rk_user");

            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.KeyVersion)
                .HasDefaultValueSql("'1'")
                .HasColumnName("key_version");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.EncryptedKey)
                .HasColumnType("text")
                .HasColumnName("encrypted_key");

            entity.HasOne(d => d.Room).WithMany(p => p.E2eRoomKeys)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("fk_e2e_rk_room");

            entity.HasOne(d => d.User).WithMany(p => p.E2eRoomKeys)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_e2e_rk_user");
        });

        modelBuilder.Entity<E2eUserKey>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("e2e_user_keys");

            entity.HasIndex(e => new { e.UserId, e.DeviceId }, "uq_user_device").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.IdentityKey)
                .HasColumnType("text")
                .HasColumnName("identity_key");
            entity.Property(e => e.PrekeySignature)
                .HasColumnType("text")
                .HasColumnName("prekey_signature");
            entity.Property(e => e.SignedPrekey)
                .HasColumnType("text")
                .HasColumnName("signed_prekey");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.E2eUserKeys)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_e2e_uk_user");
        });

        modelBuilder.Entity<JwtBlacklistedToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("jwt_blacklisted_tokens");

            entity.HasIndex(e => e.UserId, "fk_blacklist_user");

            entity.HasIndex(e => e.ExpiresAt, "idx_blacklist_expires");

            entity.HasIndex(e => e.Jti, "idx_blacklist_jti").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("timestamp")
                .HasColumnName("expires_at");
            entity.Property(e => e.Jti).HasColumnName("jti");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.JwtBlacklistedTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_blacklist_user");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("permissions");

            entity.HasIndex(e => e.Code, "code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<ProjectActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("project_activity_log");

            entity.HasIndex(e => e.TaskId, "fk_act_task");

            entity.HasIndex(e => e.UserId, "fk_act_user");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .HasColumnName("action");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Details)
                .HasColumnType("json")
                .HasColumnName("details");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

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

            entity.ToTable("project_comments");

            entity.HasIndex(e => e.TaskId, "fk_comments_task");

            entity.HasIndex(e => e.UserId, "fk_comments_user");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Content)
                .HasColumnType("text")
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Task).WithMany(p => p.ProjectComments)
                .HasForeignKey(d => d.TaskId)
                .HasConstraintName("fk_comments_task");

            entity.HasOne(d => d.User).WithMany(p => p.ProjectComments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_comments_user");
        });

        modelBuilder.Entity<ProjectLabel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("project_labels");

            entity.HasIndex(e => e.ProjectId, "fk_labels_project");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Color)
                .HasMaxLength(7)
                .HasDefaultValueSql("'#888888'")
                .HasColumnName("color");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectLabels)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("fk_labels_project");
        });

        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.HasKey(e => new { e.ProjectId, e.UserId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("project_members");

            entity.HasIndex(e => e.UserId, "fk_pm_user");

            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("joined_at");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectMembers)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("fk_pm_project");

            entity.HasOne(d => d.User).WithMany(p => p.ProjectMembers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_pm_user");

            entity.HasMany(d => d.Roles).WithMany(p => p.ProjectMembers)
                .UsingEntity<Dictionary<string, object>>(
                    "UserProjectRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("fk_upr_role"),
                    l => l.HasOne<ProjectMember>().WithMany()
                        .HasForeignKey("ProjectId", "UserId")
                        .HasConstraintName("fk_upr_member"),
                    j =>
                    {
                        j.HasKey("ProjectId", "UserId", "RoleId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0, 0 });
                        j.ToTable("user_project_roles");
                        j.HasIndex(new[] { "RoleId" }, "fk_upr_role");
                        j.IndexerProperty<Guid>("ProjectId").HasColumnName("project_id");
                        j.IndexerProperty<Guid>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<Guid>("RoleId").HasColumnName("role_id");
                    });
        });

        modelBuilder.Entity<ProjectTask>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("project_tasks");

            entity.HasIndex(e => e.ReporterId, "fk_tasks_reporter");

            entity.HasIndex(e => e.AssigneeId, "idx_tasks_assignee");

            entity.HasIndex(e => e.ProjectId, "idx_tasks_project");

            entity.HasIndex(e => new { e.ProjectId, e.Status, e.AssigneeId }, "idx_tasks_status_assignee");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssigneeId).HasColumnName("assignee_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.Priority)
                .HasDefaultValueSql("'medium'")
                .HasColumnType("enum('low','medium','high','urgent')")
                .HasColumnName("priority");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ReporterId).HasColumnName("reporter_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Tervezés'")
                .HasColumnType("enum('Tervezés','Folyamatban','Kész')")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Assignee).WithMany(p => p.ProjectTaskAssignees)
                .HasForeignKey(d => d.AssigneeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_tasks_assignee");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectTasks)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("fk_tasks_project");

            entity.HasOne(d => d.Reporter).WithMany(p => p.ProjectTaskReporters)
                .HasForeignKey(d => d.ReporterId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_tasks_reporter");

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
                        j.ToTable("project_task_labels");
                        j.HasIndex(new[] { "LabelId" }, "fk_ptl_label");
                        j.IndexerProperty<Guid>("TaskId").HasColumnName("task_id");
                        j.IndexerProperty<Guid>("LabelId").HasColumnName("label_id");
                    });
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("roles");

            entity.HasIndex(e => e.WorkspaceId, "idx_roles_workspace");

            entity.HasIndex(e => new { e.WorkspaceId, e.Name }, "uq_role_per_workspace").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.IsSystem).HasColumnName("is_system");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.Workspace).WithMany(p => p.Roles)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_roles_workspace");

            entity.HasMany(d => d.Permissions).WithMany(p => p.Roles)
                .UsingEntity<Dictionary<string, object>>(
                    "RolePermission",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .HasConstraintName("fk_rp_permission"),
                    l => l.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("fk_rp_role"),
                    j =>
                    {
                        j.HasKey("RoleId", "PermissionId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("role_permissions");
                        j.HasIndex(new[] { "PermissionId" }, "fk_rp_permission");
                        j.IndexerProperty<Guid>("RoleId").HasColumnName("role_id");
                        j.IndexerProperty<Guid>("PermissionId").HasColumnName("permission_id");
                    });
        });

        modelBuilder.Entity<StorageFileVersion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("storage_file_versions");

            entity.HasIndex(e => e.NodeId, "fk_sfv_node");

            entity.HasIndex(e => e.UploadedBy, "fk_sfv_uploader");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.FilePath)
                .HasMaxLength(500)
                .HasColumnName("file_path");
            entity.Property(e => e.NodeId).HasColumnName("node_id");
            entity.Property(e => e.SizeBytes).HasColumnName("size_bytes");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");
            entity.Property(e => e.VersionNumber)
                .HasDefaultValueSql("'1'")
                .HasColumnName("version_number");

            entity.HasOne(d => d.Node).WithMany(p => p.StorageFileVersions)
                .HasForeignKey(d => d.NodeId)
                .HasConstraintName("fk_sfv_node");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.StorageFileVersions)
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_sfv_uploader");
        });

        modelBuilder.Entity<StorageNode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("storage_nodes");

            entity.HasIndex(e => e.CreatedBy, "fk_sn_creator");

            entity.HasIndex(e => e.ProjectId, "fk_sn_project");

            entity.HasIndex(e => e.WorkspaceId, "fk_sn_workspace");

            entity.HasIndex(e => e.ParentId, "idx_sn_parent");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.MimeType)
                .HasMaxLength(127)
                .HasColumnName("mime_type");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.SizeBytes)
                .HasDefaultValueSql("'0'")
                .HasColumnName("size_bytes");
            entity.Property(e => e.Type)
                .HasColumnType("enum('folder','file')")
                .HasColumnName("type");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.StorageNodes)
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

            entity.HasOne(d => d.Workspace).WithMany(p => p.StorageNodes)
                .HasForeignKey(d => d.WorkspaceId)
                .HasConstraintName("fk_sn_workspace");

            entity.HasMany(d => d.Tags).WithMany(p => p.Nodes)
                .UsingEntity<Dictionary<string, object>>(
                    "StorageNodeTag",
                    r => r.HasOne<StorageTag>().WithMany()
                        .HasForeignKey("TagId")
                        .HasConstraintName("fk_snt_tag"),
                    l => l.HasOne<StorageNode>().WithMany()
                        .HasForeignKey("NodeId")
                        .HasConstraintName("fk_snt_node"),
                    j =>
                    {
                        j.HasKey("NodeId", "TagId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("storage_node_tags");
                        j.HasIndex(new[] { "TagId" }, "fk_snt_tag");
                        j.IndexerProperty<Guid>("NodeId").HasColumnName("node_id");
                        j.IndexerProperty<Guid>("TagId").HasColumnName("tag_id");
                    });
        });

        modelBuilder.Entity<StorageTag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("storage_tags");

            entity.HasIndex(e => e.WorkspaceId, "fk_st_workspace");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Color)
                .HasMaxLength(7)
                .HasDefaultValueSql("'#3B82F6'")
                .HasColumnName("color");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.Workspace).WithMany(p => p.StorageTags)
                .HasForeignKey(d => d.WorkspaceId)
                .HasConstraintName("fk_st_workspace");
        });

        modelBuilder.Entity<StorageTagRbac>(entity =>
        {
            entity.HasKey(e => new { e.TagId, e.RoleId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("storage_tag_rbac");

            entity.HasIndex(e => e.RoleId, "fk_str_role");

            entity.Property(e => e.TagId).HasColumnName("tag_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.CanDelete).HasColumnName("can_delete");
            entity.Property(e => e.CanRead)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("can_read");
            entity.Property(e => e.CanWrite).HasColumnName("can_write");

            entity.HasOne(d => d.Role).WithMany(p => p.StorageTagRbacs)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("fk_str_role");

            entity.HasOne(d => d.Tag).WithMany(p => p.StorageTagRbacs)
                .HasForeignKey(d => d.TagId)
                .HasConstraintName("fk_str_tag");
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("user_notifications");

            entity.HasIndex(e => e.UserId, "fk_notif_user");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Content)
                .HasMaxLength(500)
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserNotifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_notif_user");
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("user_sessions");

            entity.HasIndex(e => e.ReplacedBySessionId, "fk_sessions_replaced");

            entity.HasIndex(e => e.RefreshTokenHash, "idx_sessions_refresh").IsUnique();

            entity.HasIndex(e => e.UserId, "idx_sessions_user");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceInfo)
                .HasMaxLength(255)
                .HasColumnName("device_info");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("timestamp")
                .HasColumnName("expires_at");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasColumnName("ip_address");
            entity.Property(e => e.IsRevoked).HasColumnName("is_revoked");
            entity.Property(e => e.RefreshTokenHash)
                .HasMaxLength(64)
                .HasColumnName("refresh_token_hash");
            entity.Property(e => e.ReplacedBySessionId).HasColumnName("replaced_by_session_id");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ReplacedBySession).WithMany(p => p.InverseReplacedBySession)
                .HasForeignKey(d => d.ReplacedBySessionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_sessions_replaced");

            entity.HasOne(d => d.User).WithMany(p => p.UserSessions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_sessions_user");
        });

        modelBuilder.Entity<UserWorkspace>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("user_workspaces");

            entity.HasIndex(e => e.OwnerId, "idx_user_workspaces_owner");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");

            entity.HasOne(d => d.Owner).WithMany(p => p.UserWorkspaces)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("fk_user_workspaces_owner");
        });

        modelBuilder.Entity<WorkspaceMember>(entity =>
        {
            entity.HasKey(e => new { e.WorkspaceId, e.UserId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("workspace_members");

            entity.HasIndex(e => e.UserId, "fk_wm_user");

            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("joined_at");

            entity.HasOne(d => d.User).WithMany(p => p.WorkspaceMembers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_wm_user");

            entity.HasOne(d => d.Workspace).WithMany(p => p.WorkspaceMembers)
                .HasForeignKey(d => d.WorkspaceId)
                .HasConstraintName("fk_wm_workspace");

            entity.HasMany(d => d.Roles).WithMany(p => p.WorkspaceMembers)
                .UsingEntity<Dictionary<string, object>>(
                    "UserWorkspaceRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("fk_uwr_role"),
                    l => l.HasOne<WorkspaceMember>().WithMany()
                        .HasForeignKey("WorkspaceId", "UserId")
                        .HasConstraintName("fk_uwr_member"),
                    j =>
                    {
                        j.HasKey("WorkspaceId", "UserId", "RoleId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0, 0 });
                        j.ToTable("user_workspace_roles");
                        j.HasIndex(new[] { "RoleId" }, "fk_uwr_role");
                        j.IndexerProperty<Guid>("WorkspaceId").HasColumnName("workspace_id");
                        j.IndexerProperty<Guid>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<Guid>("RoleId").HasColumnName("role_id");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
