DROP DATABASE IF EXISTS TDM_KM_ProjectManagerApp;
CREATE DATABASE TDM_KM_ProjectManagerApp
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE TDM_KM_ProjectManagerApp;

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ============================================================
-- 1. APP USERS & CORE IAM
-- ============================================================
CREATE TABLE app_users (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    name            VARCHAR(255) NOT NULL,
    email           VARCHAR(255) NOT NULL UNIQUE,
    password_hash   VARCHAR(255) NOT NULL,
    avatar_url      VARCHAR(500),
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB;

CREATE TABLE user_workspaces (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    name            VARCHAR(255) NOT NULL,
    owner_id        CHAR(36)     NOT NULL,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_user_workspaces_owner
        FOREIGN KEY (owner_id) REFERENCES app_users(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE INDEX idx_user_workspaces_owner ON user_workspaces(owner_id);

-- ============================================================
-- 2. DINAMIKUS RBAC (ROLES & PERMISSIONS)
-- ============================================================
CREATE TABLE permissions (
    id          CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    code        VARCHAR(100) NOT NULL UNIQUE,
    name        VARCHAR(255) NOT NULL,
    description TEXT,
    created_at  TIMESTAMP    DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

CREATE TABLE roles (
    id           CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    workspace_id CHAR(36)     NULL,
    name         VARCHAR(100) NOT NULL,
    description  TEXT,
    is_system    BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at   TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_roles_workspace
        FOREIGN KEY (workspace_id) REFERENCES user_workspaces(id)
        ON DELETE CASCADE,
    CONSTRAINT uq_role_per_workspace UNIQUE (workspace_id, name)
) ENGINE=InnoDB;

CREATE INDEX idx_roles_workspace ON roles(workspace_id);

CREATE TABLE role_permissions (
    role_id       CHAR(36) NOT NULL,
    permission_id CHAR(36) NOT NULL,

    PRIMARY KEY (role_id, permission_id),

    CONSTRAINT fk_rp_role
        FOREIGN KEY (role_id) REFERENCES roles(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_rp_permission
        FOREIGN KEY (permission_id) REFERENCES permissions(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

-- ============================================================
-- 3. WORKSPACE & PROJECT MEMBERSHIPS
-- ============================================================
CREATE TABLE workspace_members (
    workspace_id CHAR(36)  NOT NULL,
    user_id      CHAR(36)  NOT NULL,
    joined_at    TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (workspace_id, user_id),

    CONSTRAINT fk_wm_workspace
        FOREIGN KEY (workspace_id) REFERENCES user_workspaces(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_wm_user
        FOREIGN KEY (user_id) REFERENCES app_users(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE user_workspace_roles (
    workspace_id CHAR(36) NOT NULL,
    user_id      CHAR(36) NOT NULL,
    role_id      CHAR(36) NOT NULL,

    PRIMARY KEY (workspace_id, user_id, role_id),

    CONSTRAINT fk_uwr_member
        FOREIGN KEY (workspace_id, user_id) REFERENCES workspace_members(workspace_id, user_id)
        ON DELETE CASCADE,
    CONSTRAINT fk_uwr_role
        FOREIGN KEY (role_id) REFERENCES roles(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE app_projects (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    workspace_id    CHAR(36)     NOT NULL,
    name            VARCHAR(255) NOT NULL,
    description     TEXT,
    status          ENUM('planned', 'active', 'on_hold', 'completed', 'archived') NOT NULL DEFAULT 'planned',
    deadline        DATE,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_app_projects_workspace
        FOREIGN KEY (workspace_id) REFERENCES user_workspaces(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE INDEX idx_app_projects_workspace ON app_projects(workspace_id);

CREATE TABLE project_members (
    project_id CHAR(36)  NOT NULL,
    user_id    CHAR(36)  NOT NULL,
    joined_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (project_id, user_id),

    CONSTRAINT fk_pm_project
        FOREIGN KEY (project_id) REFERENCES app_projects(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_pm_user
        FOREIGN KEY (user_id) REFERENCES app_users(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE user_project_roles (
    project_id CHAR(36) NOT NULL,
    user_id    CHAR(36) NOT NULL,
    role_id    CHAR(36) NOT NULL,

    PRIMARY KEY (project_id, user_id, role_id),

    CONSTRAINT fk_upr_member
        FOREIGN KEY (project_id, user_id) REFERENCES project_members(project_id, user_id)
        ON DELETE CASCADE,
    CONSTRAINT fk_upr_role
        FOREIGN KEY (role_id) REFERENCES roles(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

-- ============================================================
-- 4. PROJECT TASKS & SUB-MODULES
-- ============================================================
CREATE TABLE project_tasks (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    project_id      CHAR(36)     NOT NULL,
    title           VARCHAR(255) NOT NULL,
    description     TEXT,
    
    -- Magyar nyelvű feladatÁllapot ENUM:
    status          ENUM('Tervezés', 'Folyamatban', 'Kész') NOT NULL DEFAULT 'Tervezés',
    
    priority        ENUM('low', 'medium', 'high', 'urgent') NOT NULL DEFAULT 'medium',
    assignee_id     CHAR(36),
    reporter_id     CHAR(36),
    due_date        DATE,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_tasks_project FOREIGN KEY (project_id) REFERENCES app_projects(id) ON DELETE CASCADE,
    CONSTRAINT fk_tasks_assignee FOREIGN KEY (assignee_id) REFERENCES app_users(id) ON DELETE SET NULL,
    CONSTRAINT fk_tasks_reporter FOREIGN KEY (reporter_id) REFERENCES app_users(id) ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE INDEX idx_tasks_project ON project_tasks(project_id);
CREATE INDEX idx_tasks_assignee ON project_tasks(assignee_id);
CREATE INDEX idx_tasks_status_assignee ON project_tasks(project_id, status, assignee_id);

CREATE TABLE project_comments (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    task_id         CHAR(36)     NOT NULL,
    user_id         CHAR(36)     NOT NULL,
    content         TEXT         NOT NULL,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_comments_task FOREIGN KEY (task_id) REFERENCES project_tasks(id) ON DELETE CASCADE,
    CONSTRAINT fk_comments_user FOREIGN KEY (user_id) REFERENCES app_users(id) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE project_labels (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    project_id      CHAR(36)     NOT NULL,
    name            VARCHAR(100) NOT NULL,
    color           VARCHAR(7)   NOT NULL DEFAULT '#888888',

    CONSTRAINT fk_labels_project FOREIGN KEY (project_id) REFERENCES app_projects(id) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE project_task_labels (
    task_id         CHAR(36) NOT NULL,
    label_id        CHAR(36) NOT NULL,

    PRIMARY KEY (task_id, label_id),

    CONSTRAINT fk_ptl_task FOREIGN KEY (task_id) REFERENCES project_tasks(id) ON DELETE CASCADE,
    CONSTRAINT fk_ptl_label FOREIGN KEY (label_id) REFERENCES project_labels(id) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE project_activity_log (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    task_id         CHAR(36)     NULL,
    user_id         CHAR(36)     NULL,
    action          VARCHAR(100) NOT NULL,
    details         JSON,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_act_task FOREIGN KEY (task_id) REFERENCES project_tasks(id) ON DELETE CASCADE,
    CONSTRAINT fk_act_user FOREIGN KEY (user_id) REFERENCES app_users(id) ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE TABLE user_notifications (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    user_id         CHAR(36)     NOT NULL,
    content         VARCHAR(500) NOT NULL,
    is_read         BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_notif_user FOREIGN KEY (user_id) REFERENCES app_users(id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- ============================================================
-- 5. STORAGE & DRIVE MODULE (NEM E2E)
-- ============================================================
CREATE TABLE storage_nodes (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    workspace_id    CHAR(36)     NOT NULL,
    project_id      CHAR(36)     NULL,
    parent_id       CHAR(36)     NULL,
    name            VARCHAR(255) NOT NULL,
    type            ENUM('folder', 'file') NOT NULL,
    mime_type       VARCHAR(127) NULL,
    size_bytes      BIGINT       UNSIGNED DEFAULT 0,
    created_by      CHAR(36)     NULL,
    updated_by      CHAR(36)     NULL,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_sn_workspace FOREIGN KEY (workspace_id) REFERENCES user_workspaces(id) ON DELETE CASCADE,
    CONSTRAINT fk_sn_project FOREIGN KEY (project_id) REFERENCES app_projects(id) ON DELETE CASCADE,
    CONSTRAINT fk_sn_parent FOREIGN KEY (parent_id) REFERENCES storage_nodes(id) ON DELETE CASCADE,
    CONSTRAINT fk_sn_creator FOREIGN KEY (created_by) REFERENCES app_users(id) ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE INDEX idx_sn_parent ON storage_nodes(parent_id);

CREATE TABLE storage_tags (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    workspace_id    CHAR(36)     NOT NULL,
    name            VARCHAR(100) NOT NULL,
    color           VARCHAR(7)   NOT NULL DEFAULT '#3B82F6',

    CONSTRAINT fk_st_workspace FOREIGN KEY (workspace_id) REFERENCES user_workspaces(id) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE storage_tag_rbac (
    tag_id          CHAR(36)     NOT NULL,
    role_id         CHAR(36)     NOT NULL,
    can_read        BOOLEAN      NOT NULL DEFAULT TRUE,
    can_write       BOOLEAN      NOT NULL DEFAULT FALSE,
    can_delete      BOOLEAN      NOT NULL DEFAULT FALSE,

    PRIMARY KEY (tag_id, role_id),

    CONSTRAINT fk_str_tag FOREIGN KEY (tag_id) REFERENCES storage_tags(id) ON DELETE CASCADE,
    CONSTRAINT fk_str_role FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE storage_node_tags (
    node_id         CHAR(36)     NOT NULL,
    tag_id          CHAR(36)     NOT NULL,

    PRIMARY KEY (node_id, tag_id),

    CONSTRAINT fk_snt_node FOREIGN KEY (node_id) REFERENCES storage_nodes(id) ON DELETE CASCADE,
    CONSTRAINT fk_snt_tag FOREIGN KEY (tag_id) REFERENCES storage_tags(id) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE storage_file_versions (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    node_id         CHAR(36)     NOT NULL,
    file_path       VARCHAR(500) NOT NULL,
    version_number  INT          NOT NULL DEFAULT 1,
    size_bytes      BIGINT       UNSIGNED NOT NULL,
    uploaded_by     CHAR(36)     NULL,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_sfv_node FOREIGN KEY (node_id) REFERENCES storage_nodes(id) ON DELETE CASCADE,
    CONSTRAINT fk_sfv_uploader FOREIGN KEY (uploaded_by) REFERENCES app_users(id) ON DELETE SET NULL
) ENGINE=InnoDB;

-- ============================================================
-- 6. END-TO-END (E2E) ENCRYPTED CHAT MODULE
-- ============================================================
CREATE TABLE chat_rooms (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    workspace_id    CHAR(36)     NOT NULL,
    project_id      CHAR(36)     NULL,
    name            VARCHAR(255) NULL,
    type            ENUM('direct', 'group', 'project_channel') NOT NULL DEFAULT 'group',
    is_private      BOOLEAN      NOT NULL DEFAULT FALSE,
    created_by      CHAR(36)     NULL,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_cr_workspace FOREIGN KEY (workspace_id) REFERENCES user_workspaces(id) ON DELETE CASCADE,
    CONSTRAINT fk_cr_project FOREIGN KEY (project_id) REFERENCES app_projects(id) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE e2e_user_keys (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    user_id         CHAR(36)     NOT NULL,
    device_id       VARCHAR(255) NOT NULL,
    identity_key    TEXT         NOT NULL,
    signed_prekey   TEXT         NOT NULL,
    prekey_signature TEXT        NOT NULL,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_e2e_uk_user FOREIGN KEY (user_id) REFERENCES app_users(id) ON DELETE CASCADE,
    CONSTRAINT uq_user_device UNIQUE (user_id, device_id)
) ENGINE=InnoDB;

CREATE TABLE e2e_room_keys (
    room_id         CHAR(36)     NOT NULL,
    user_id         CHAR(36)     NOT NULL,
    key_version     INT          NOT NULL DEFAULT 1,
    encrypted_key   TEXT         NOT NULL,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (room_id, user_id, key_version),
    CONSTRAINT fk_e2e_rk_room FOREIGN KEY (room_id) REFERENCES chat_rooms(id) ON DELETE CASCADE,
    CONSTRAINT fk_e2e_rk_user FOREIGN KEY (user_id) REFERENCES app_users(id) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE e2e_chat_messages (
    id               CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    room_id          CHAR(36)     NOT NULL,
    sender_id        CHAR(36)     NULL,
    sender_device_id VARCHAR(255) NOT NULL,
    parent_id        CHAR(36)     NULL,
    ciphertext       TEXT         NOT NULL,
    nonce_or_iv      VARCHAR(255) NOT NULL,
    key_version      INT          NOT NULL DEFAULT 1,
    created_at       TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_e2e_cm_room FOREIGN KEY (room_id) REFERENCES chat_rooms(id) ON DELETE CASCADE,
    CONSTRAINT fk_e2e_cm_sender FOREIGN KEY (sender_id) REFERENCES app_users(id) ON DELETE SET NULL,
    CONSTRAINT fk_e2e_cm_parent FOREIGN KEY (parent_id) REFERENCES e2e_chat_messages(id) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE INDEX idx_e2e_cm_room_created ON e2e_chat_messages(room_id, created_at);

CREATE TABLE chat_room_members (
    room_id              CHAR(36)  NOT NULL,
    user_id              CHAR(36)  NOT NULL,
    last_read_message_id CHAR(36)  NULL,
    joined_at            TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (room_id, user_id),

    CONSTRAINT fk_crm_room FOREIGN KEY (room_id) REFERENCES chat_rooms(id) ON DELETE CASCADE,
    CONSTRAINT fk_crm_user FOREIGN KEY (user_id) REFERENCES app_users(id) ON DELETE CASCADE,
    CONSTRAINT fk_crm_last_msg FOREIGN KEY (last_read_message_id) REFERENCES e2e_chat_messages(id) ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE TABLE chat_message_attachments (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    message_id      CHAR(36)     NOT NULL,
    storage_node_id CHAR(36)     NULL,
    file_url        VARCHAR(500) NOT NULL,
    file_name       VARCHAR(255) NOT NULL,

    CONSTRAINT fk_cma_message FOREIGN KEY (message_id) REFERENCES e2e_chat_messages(id) ON DELETE CASCADE,
    CONSTRAINT fk_cma_storage FOREIGN KEY (storage_node_id) REFERENCES storage_nodes(id) ON DELETE SET NULL
) ENGINE=InnoDB;

-- ============================================================
-- 7. CALENDAR MODULE
-- ============================================================
CREATE TABLE calendars (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    workspace_id    CHAR(36)     NULL,
    project_id      CHAR(36)     NULL,
    user_id         CHAR(36)     NULL,
    name            VARCHAR(255) NOT NULL,
    color           VARCHAR(7)   NOT NULL DEFAULT '#3B82F6',
    type            ENUM('personal', 'workspace_user', 'workspace_shared', 'project') NOT NULL DEFAULT 'personal',
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_cal_workspace FOREIGN KEY (workspace_id) REFERENCES user_workspaces(id) ON DELETE CASCADE,
    CONSTRAINT fk_cal_project FOREIGN KEY (project_id) REFERENCES app_projects(id) ON DELETE CASCADE,
    CONSTRAINT fk_cal_user FOREIGN KEY (user_id) REFERENCES app_users(id) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE calendar_events (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    calendar_id     CHAR(36)     NOT NULL,
    task_id         CHAR(36)     NULL,
    title           VARCHAR(255) NOT NULL,
    description     TEXT,
    location        VARCHAR(255) NULL,
    start_time      DATETIME     NOT NULL,
    end_time        DATETIME     NOT NULL,
    is_all_day      BOOLEAN      NOT NULL DEFAULT FALSE,
    created_by      CHAR(36)     NULL,

    CONSTRAINT fk_ce_calendar FOREIGN KEY (calendar_id) REFERENCES calendars(id) ON DELETE CASCADE,
    CONSTRAINT fk_ce_task FOREIGN KEY (task_id) REFERENCES project_tasks(id) ON DELETE SET NULL,
    CONSTRAINT fk_ce_creator FOREIGN KEY (created_by) REFERENCES app_users(id) ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE INDEX idx_ce_dates ON calendar_events(start_time, end_time);

-- ============================================================
-- 8. AUTH & JWT SESSION MANAGEMENT
-- ============================================================
CREATE TABLE user_sessions (
    id                    CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    user_id               CHAR(36)     NOT NULL,
    refresh_token_hash    VARCHAR(64)  NOT NULL UNIQUE,
    replaced_by_session_id CHAR(36)    NULL,
    device_info           VARCHAR(255) NULL,
    ip_address            VARCHAR(45)  NULL,
    is_revoked            BOOLEAN      NOT NULL DEFAULT FALSE,
    expires_at            TIMESTAMP    NOT NULL,
    created_at            TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    updated_at            TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_sessions_user FOREIGN KEY (user_id) REFERENCES app_users(id) ON DELETE CASCADE,
    CONSTRAINT fk_sessions_replaced FOREIGN KEY (replaced_by_session_id) REFERENCES user_sessions(id) ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE INDEX idx_sessions_user ON user_sessions(user_id);
CREATE INDEX idx_sessions_refresh ON user_sessions(refresh_token_hash);

CREATE TABLE jwt_blacklisted_tokens (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    jti             VARCHAR(255) NOT NULL UNIQUE,
    user_id         CHAR(36)     NOT NULL,
    expires_at      TIMESTAMP    NOT NULL,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_blacklist_user FOREIGN KEY (user_id) REFERENCES app_users(id) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE INDEX idx_blacklist_jti ON jwt_blacklisted_tokens(jti);
CREATE INDEX idx_blacklist_expires ON jwt_blacklisted_tokens(expires_at);

SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- 9. MYSQL EVENT SCHEDULER (AUTOMATIKUS TISZTÍTÁS)
-- ============================================================

-- Az eseményütemező szál bekapcsolása az adatbázis-szerveren
SET GLOBAL event_scheduler = ON;

DROP EVENT IF EXISTS purge_expired_auth_data;

DELIMITER $$

CREATE EVENT purge_expired_auth_data
ON SCHEDULE EVERY 1 HOUR
STARTS CURRENT_TIMESTAMP
DO
BEGIN
    -- 1. Lejárt JWT feketelista tokenek azonnali törlése
    DELETE FROM jwt_blacklisted_tokens
    WHERE expires_at < NOW();

    -- 2. Lejárt és visszavont munkamenetek törlése (7 nap türelmi idő után)
    DELETE FROM user_sessions
    WHERE expires_at < DATE_SUB(NOW(), INTERVAL 7 DAY)
      AND is_revoked = TRUE;
END$$

DELIMITER ;