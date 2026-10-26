DROP DATABASE IF EXISTS TMD_KM_ProjectManagerApp;
CREATE DATABASE TMD_KM_ProjectManagerApp
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE TMD_KM_ProjectManagerApp;

SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci;
SET FOREIGN_KEY_CHECKS = 0;

-- ============================================================
-- 0. ENUM / LOOKUP SZÓTÁR TÁBLA
-- ============================================================
CREATE TABLE enum_lookup (
    id          INT AUTO_INCREMENT PRIMARY KEY COMMENT 'Elsődleges kulcs',
    category    VARCHAR(100) NOT NULL COMMENT 'Kategória neve, pl. global_role, project_status',
    code        TINYINT      NOT NULL COMMENT 'Számkód (1, 2, 3...)',
    name        VARCHAR(100) NOT NULL COMMENT 'Megjelenített név / kód név',
    description VARCHAR(255) NULL     COMMENT 'Opcionális leírás',
    CONSTRAINT uq_category_code UNIQUE (category, code)
) ENGINE=InnoDB COMMENT='Rendszerszintű enum számszerű értékek szótára';

INSERT INTO enum_lookup (category, code, name, description) VALUES
-- Global Role (app_user)
('global_role', 1, 'User', 'Sima felhasználó'),
('global_role', 2, 'SystemAdmin', 'Rendszeradminisztrátor'),

-- 2FA Type (app_user)
('two_factor_type', 0, 'None', 'Nincs 2FA'),
('two_factor_type', 1, 'Authenticator', 'Hitelesítő alkalmazás'),
('two_factor_type', 2, 'Email', 'Email alapú 2FA'),

-- Permission Scope (permission)
('permission_scope', 1, 'workspace', 'Workspace hatókör'),
('permission_scope', 2, 'project', 'Projekt hatókör'),

-- Project Status (project)
('project_status', 1, 'planned', 'Tervezett'),
('project_status', 2, 'active', 'Aktív'),
('project_status', 3, 'on_hold', 'Felfüggesztve'),
('project_status', 4, 'completed', 'Befejezett'),
('project_status', 5, 'archived', 'Archivált'),

-- Invitation Type (app_invitation)
('invitation_type', 1, 'workspace', 'Workspace meghívó'),
('invitation_type', 2, 'project', 'Projekt meghívó'),

-- Invitation Status (app_invitation)
('invitation_status', 1, 'pending', 'Folyamatban'),
('invitation_status', 2, 'accepted', 'Elfogadva'),
('invitation_status', 3, 'refused', 'Elutasítva'),
('invitation_status', 4, 'expired', 'Lejárt'),

-- Task Status (project_task)
('task_status', 1, 'planning', 'Tervezés alatt'),
('task_status', 2, 'on_going', 'Folyamatban'),
('task_status', 3, 'ready', 'Elkészült'),

-- Task Priority (project_task)
('task_priority', 1, 'low', 'Alacsony'),
('task_priority', 2, 'medium', 'Közepes'),
('task_priority', 3, 'high', 'Magas'),
('task_priority', 4, 'urgent', 'Sürgős'),

-- Storage Type (storage_node)
('storage_type', 1, 'folder', 'Mappa'),
('storage_type', 2, 'file', 'Fájl'),

-- Chat Room Type (chat_room)
('chat_room_type', 1, 'direct', 'Közvetlen csevegés'),
('chat_room_type', 2, 'group', 'Csoportos csevegés'),
('chat_room_type', 3, 'project_channel', 'Projekt csatorna');

-- ============================================================
-- 1. APP USER & WORKSPACE
-- ============================================================
CREATE TABLE app_user (
    id                          CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Felhasználó azonosító (UUID)',
    name                        VARCHAR(255) NOT NULL COMMENT 'Megjelenített név',
    email                       VARCHAR(255) NOT NULL UNIQUE COMMENT 'Bejelentkezési e-mail cím (egyedi)',
    password_hash               VARCHAR(255) NOT NULL COMMENT 'Jelszó hash',
    avatar_url                  VARCHAR(500) COMMENT 'Profilkép URL',
    created_at                  TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Létrehozás ideje',
    updated_at                  TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Utolsó módosítás ideje',
    global_role                 TINYINT      NOT NULL DEFAULT 1 COMMENT '1 = User, 2 = SystemAdmin',
    deleted_at                  TIMESTAMP    NULL COMMENT 'Soft delete: a törlés ideje, NULL = aktív felhasználó',

    -- 2FA kiegészítések
    is_2fa_enabled              BOOLEAN      NOT NULL DEFAULT FALSE COMMENT 'Be van-e kapcsolva a kétfaktoros hitelesítés',
    two_factor_type             TINYINT      NOT NULL DEFAULT 0 COMMENT '0 = None, 1 = Authenticator, 2 = Email',
    two_factor_secret           VARCHAR(255) NULL COMMENT '2FA titkos kulcs (Authenticator)',
    two_factor_recovery_codes   TEXT         NULL COMMENT '2FA helyreállító kódok'
) ENGINE=InnoDB COMMENT='Alkalmazás felhasználói és 2FA beállításaik';

CREATE TABLE workspace (
    id                  CHAR(36)        PRIMARY KEY DEFAULT (UUID()) COMMENT 'Workspace azonosító (UUID)',
    name                VARCHAR(255)    NOT NULL COMMENT 'Workspace neve',
    description         TEXT            COMMENT 'Workspace leírása', 
    owner_id            CHAR(36)        NOT NULL COMMENT 'Tulajdonos felhasználó',
    storage_quota_bytes BIGINT UNSIGNED NOT NULL DEFAULT 26843545600 COMMENT 'Tárhelykvóta bájtban (alapértelmezett: 25 GB)',
    created_at          TIMESTAMP       DEFAULT CURRENT_TIMESTAMP COMMENT 'Létrehozás ideje',

    CONSTRAINT fk_workspace_owner
        FOREIGN KEY (owner_id) REFERENCES app_user(id)
        ON DELETE RESTRICT
) ENGINE=InnoDB COMMENT='Workspace-ek (a legfelső szintű szervezeti egység)';

CREATE INDEX idx_workspace_owner ON workspace(owner_id);

-- ============================================================
-- 2. RBAC (GLOBÁLIS ROLE + PERMISSION)
-- ============================================================
CREATE TABLE permission (
    id          CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Jogosultság azonosító (UUID)',
    code        VARCHAR(100) NOT NULL UNIQUE COMMENT 'Gépi kód, pl. workspace.manage, project.task.create',
    scope       TINYINT      NOT NULL DEFAULT 1 COMMENT '1 = workspace, 2 = project',
    name        VARCHAR(255) NOT NULL COMMENT 'Olvasható név',
    description TEXT COMMENT 'Leírás',
    created_at  TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Létrehozás ideje'
) ENGINE=InnoDB COMMENT='Jogosultságok katalógusa';

CREATE TABLE app_role (
    id           CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Szerepkör azonosító (UUID)',
    name         VARCHAR(100) NOT NULL UNIQUE COMMENT 'Szerepkör neve (globálisan egyedi)',
    description  TEXT COMMENT 'Leírás',
    is_system    BOOLEAN      NOT NULL DEFAULT FALSE COMMENT 'Beépített szerepkör (nem törölhető)',
    created_at   TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Létrehozás ideje'
) ENGINE=InnoDB COMMENT='Globális szerepkörök';

CREATE TABLE role_permission (
    role_id       CHAR(36) NOT NULL COMMENT 'Szerepkör',
    permission_id CHAR(36) NOT NULL COMMENT 'Jogosultság',

    PRIMARY KEY (role_id, permission_id),

    CONSTRAINT fk_rp_role
        FOREIGN KEY (role_id) REFERENCES app_role(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_rp_permission
        FOREIGN KEY (permission_id) REFERENCES permission(id)
        ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Szerepkörökhöz rendelt jogosultságok';

-- ============================================================
-- 3. WORKSPACE & PROJECT TAGSÁGOK & MEGHÍVÓK
-- ============================================================
CREATE TABLE workspace_member (
    workspace_id CHAR(36)  NOT NULL COMMENT 'Workspace',
    user_id      CHAR(36)  NOT NULL COMMENT 'Tag',
    role_id      CHAR(36)  NOT NULL COMMENT 'A tag szerepköre a workspace-ben',
    joined_at    TIMESTAMP DEFAULT CURRENT_TIMESTAMP COMMENT 'Csatlakozás ideje',

    PRIMARY KEY (workspace_id, user_id),

    CONSTRAINT fk_wm_workspace
        FOREIGN KEY (workspace_id) REFERENCES workspace(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_wm_user
        FOREIGN KEY (user_id) REFERENCES app_user(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_wm_role
        FOREIGN KEY (role_id) REFERENCES app_role(id)
        ON DELETE RESTRICT
) ENGINE=InnoDB COMMENT='Workspace tagságok és szerepkörük';

CREATE INDEX idx_wm_user ON workspace_member(user_id);

CREATE TABLE project (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Projekt azonosító (UUID)',
    workspace_id    CHAR(36)     NOT NULL COMMENT 'Workspace, amelyhez a projekt tartozik',
    name            VARCHAR(255) NOT NULL COMMENT 'Projekt neve',
    description     TEXT COMMENT 'Projekt leírása',
    status          TINYINT      NOT NULL DEFAULT 1 COMMENT '1 = planned, 2 = active, 3 = on_hold, 4 = completed, 5 = archived',
    deadline        DATE COMMENT 'Határidő',
    owner_id        CHAR(36)     NULL COMMENT 'Projekt felelőse',
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Létrehozás ideje',
    updated_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Utolsó módosítás ideje',

    CONSTRAINT fk_project_workspace
        FOREIGN KEY (workspace_id) REFERENCES workspace(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_project_owner
        FOREIGN KEY (owner_id) REFERENCES app_user(id)
        ON DELETE SET NULL
) ENGINE=InnoDB COMMENT='Projektek';

CREATE INDEX idx_project_workspace ON project(workspace_id);

CREATE TABLE project_member (
    project_id          CHAR(36)  NOT NULL COMMENT 'Projekt azonosító',
    workspace_id        CHAR(36)  NOT NULL COMMENT 'A projekt workspace-e',
    user_id             CHAR(36)  NOT NULL COMMENT 'Felhasználó azonosító',
    role_id             CHAR(36)  NOT NULL COMMENT 'A tag szerepköre a projektben',
    is_workspace_member BOOLEAN   NOT NULL DEFAULT TRUE COMMENT 'Jelzi, hogy a felhasználó a felettes workspace-nek is tagja-e',
    assigned_at         TIMESTAMP DEFAULT CURRENT_TIMESTAMP COMMENT 'Hozzárendelés ideje',

    PRIMARY KEY (project_id, user_id, role_id),

    CONSTRAINT fk_pm_project
        FOREIGN KEY (project_id) REFERENCES project(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_pm_workspace
        FOREIGN KEY (workspace_id) REFERENCES workspace(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_pm_user
        FOREIGN KEY (user_id) REFERENCES app_user(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_pm_role
        FOREIGN KEY (role_id) REFERENCES app_role(id)
        ON DELETE RESTRICT
) ENGINE=InnoDB COMMENT='Projekttagságok';

CREATE INDEX idx_pm_user ON project_member(user_id);
CREATE INDEX idx_pm_role ON project_member(project_id, role_id);

CREATE TABLE app_invitation (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Meghívó azonosító (UUID)',
    type            TINYINT      NOT NULL COMMENT '1 = workspace, 2 = project',
    target_id       CHAR(36)     NOT NULL COMMENT 'A workspace_id vagy project_id azonosítója',
    invited_email   VARCHAR(255) NOT NULL COMMENT 'A meghívott e-mail címe',
    invited_user_id CHAR(36)     NULL COMMENT 'A meghívott felhasználó azonosítója (ha regisztrált)',
    role_id         CHAR(36)     NULL COMMENT 'A felajánlott szerepkör azonosítója',
    invited_by      CHAR(36)     NOT NULL COMMENT 'A meghívást kiküldő felhasználó',
    status          TINYINT      NOT NULL DEFAULT 1 COMMENT '1 = pending, 2 = accepted, 3 = refused, 4 = expired',
    token           VARCHAR(255) NOT NULL UNIQUE COMMENT 'Egyedi biztonsági token',
    expires_at      TIMESTAMP    NOT NULL COMMENT 'A meghívó lejárati ideje',
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Kiküldés ideje',
    responded_at    TIMESTAMP    NULL COMMENT 'A válaszadás ideje',

    CONSTRAINT fk_inv_role FOREIGN KEY (role_id) REFERENCES app_role(id) ON DELETE SET NULL,
    CONSTRAINT fk_inv_invited_user FOREIGN KEY (invited_user_id) REFERENCES app_user(id) ON DELETE CASCADE,
    CONSTRAINT fk_inv_invited_by FOREIGN KEY (invited_by) REFERENCES app_user(id) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Meghívások kezelése státuszkövetéssel';

CREATE INDEX idx_inv_email_status ON app_invitation(invited_email, status);
CREATE INDEX idx_inv_user_status ON app_invitation(invited_user_id, status);

-- ============================================================
-- 4. PROJECT TASK & SUB-MODULES
-- ============================================================
CREATE TABLE project_task (
    id                  CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Task azonosító (UUID)',
    project_id          CHAR(36)     NOT NULL COMMENT 'Projekt, amelyhez a task tartozik',
    title               VARCHAR(255) NOT NULL COMMENT 'Task címe',
    description         TEXT COMMENT 'Task leírása',
    status              TINYINT      NOT NULL DEFAULT 1 COMMENT '1 = planning, 2 = on_going, 3 = ready',
    priority            TINYINT      NOT NULL DEFAULT 2 COMMENT '1 = low, 2 = medium, 3 = high, 4 = urgent',
    assignee_role_id    CHAR(36)     NULL COMMENT 'Szerepkör, amire a task ki van hirdetve',
    assignee_user_id    CHAR(36)     NULL COMMENT 'Konkrét felhasználó, aki lefoglalta a feladatot',
    created_by          CHAR(36)     NULL COMMENT 'Létrehozó felhasználó',
    due_date            DATE COMMENT 'Határidő',
    created_at          TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Létrehozás ideje',
    updated_at          TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Utolsó módosítás ideje',

    CONSTRAINT fk_task_project FOREIGN KEY (project_id) REFERENCES project(id) ON DELETE CASCADE,
    CONSTRAINT fk_task_assignee_role FOREIGN KEY (assignee_role_id) REFERENCES app_role(id) ON DELETE SET NULL,
    CONSTRAINT fk_task_assignee_user FOREIGN KEY (assignee_user_id) REFERENCES app_user(id) ON DELETE SET NULL,
    CONSTRAINT fk_task_creator FOREIGN KEY (created_by) REFERENCES app_user(id) ON DELETE SET NULL
) ENGINE=InnoDB COMMENT='Projekt taskok';

CREATE INDEX idx_task_project_due ON project_task(project_id, due_date);
CREATE INDEX idx_task_project_role ON project_task(project_id, assignee_role_id);
CREATE INDEX idx_task_assignee_user ON project_task(assignee_user_id);

CREATE TABLE project_comment (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Komment azonosító (UUID)',
    task_id         CHAR(36)     NOT NULL COMMENT 'Task',
    user_id         CHAR(36)     NOT NULL COMMENT 'Szerző',
    content         TEXT         NOT NULL COMMENT 'Szöveg',
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Létrehozás ideje',

    CONSTRAINT fk_comment_task FOREIGN KEY (task_id) REFERENCES project_task(id) ON DELETE CASCADE,
    CONSTRAINT fk_comment_user FOREIGN KEY (user_id) REFERENCES app_user(id) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Task kommentek';

CREATE TABLE project_label (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Címke azonosító (UUID)',
    project_id      CHAR(36)     NOT NULL COMMENT 'Projekt',
    name            VARCHAR(100) NOT NULL COMMENT 'Címke neve',
    color           VARCHAR(7)   NOT NULL DEFAULT '#888888' COMMENT 'Szín hex kódja',

    CONSTRAINT fk_label_project FOREIGN KEY (project_id) REFERENCES project(id) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Projekt címkék';

CREATE TABLE project_task_label (
    task_id         CHAR(36) NOT NULL COMMENT 'Task',
    label_id        CHAR(36) NOT NULL COMMENT 'Címke',

    PRIMARY KEY (task_id, label_id),

    CONSTRAINT fk_ptl_task FOREIGN KEY (task_id) REFERENCES project_task(id) ON DELETE CASCADE,
    CONSTRAINT fk_ptl_label FOREIGN KEY (label_id) REFERENCES project_label(id) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Taskok és címkék kapcsolata';

CREATE TABLE project_activity_log (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Naplóbejegyzés azonosító (UUID)',
    task_id         CHAR(36)     NULL COMMENT 'Érintett task',
    user_id         CHAR(36)     NULL COMMENT 'Műveletet végző felhasználó',
    action          VARCHAR(100) NOT NULL COMMENT 'Művelet kódja',
    details         JSON COMMENT 'Részletek',
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Művelet ideje',

    CONSTRAINT fk_act_task FOREIGN KEY (task_id) REFERENCES project_task(id) ON DELETE CASCADE,
    CONSTRAINT fk_act_user FOREIGN KEY (user_id) REFERENCES app_user(id) ON DELETE SET NULL
) ENGINE=InnoDB COMMENT='Projekt tevékenységnapló';

CREATE TABLE user_notification (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Értesítés azonosító (UUID)',
    user_id         CHAR(36)     NOT NULL COMMENT 'Címzett felhasználó',
    content         VARCHAR(500) NOT NULL COMMENT 'Szöveg',
    is_read         BOOLEAN      NOT NULL DEFAULT FALSE COMMENT 'Elolvasta-e',
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Létrehozás ideje',

    CONSTRAINT fk_notif_user FOREIGN KEY (user_id) REFERENCES app_user(id) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Felhasználói értesítések';

-- ============================================================
-- 5. STORAGE & DRIVE MODULE
-- ============================================================
CREATE TABLE storage_node (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Node azonosító (UUID)',
    workspace_id    CHAR(36)     NOT NULL COMMENT 'Workspace',
    project_id      CHAR(36)     NULL COMMENT 'Projekt',
    parent_id       CHAR(36)     NULL COMMENT 'Szülő mappa (NULL = gyökér)',
    name            VARCHAR(255) NOT NULL COMMENT 'Fájl vagy mappa neve',
    type            TINYINT      NOT NULL DEFAULT 1 COMMENT '1 = folder, 2 = file',
    mime_type       VARCHAR(127) NULL COMMENT 'MIME típus',
    storage_path    VARCHAR(500) NULL COMMENT 'Fájl fizikai helye',
    size_bytes      BIGINT       UNSIGNED DEFAULT 0 COMMENT 'Méret bájtban',
    created_by      CHAR(36)     NULL COMMENT 'Létrehozó felhasználó',
    updated_by      CHAR(36)     NULL COMMENT 'Utolsó módosító',
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Létrehozás ideje',
    deleted_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Törlés ideje',
    updated_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Utolsó módosítás ideje',

    CONSTRAINT fk_sn_workspace FOREIGN KEY (workspace_id) REFERENCES workspace(id) ON DELETE CASCADE,
    CONSTRAINT fk_sn_project FOREIGN KEY (project_id) REFERENCES project(id) ON DELETE CASCADE,
    CONSTRAINT fk_sn_parent FOREIGN KEY (parent_id) REFERENCES storage_node(id) ON DELETE CASCADE,
    CONSTRAINT fk_sn_creator FOREIGN KEY (created_by) REFERENCES app_user(id) ON DELETE SET NULL,
    CONSTRAINT fk_sn_updater FOREIGN KEY (updated_by) REFERENCES app_user(id) ON DELETE SET NULL
) ENGINE=InnoDB COMMENT='Fájlok és mappák fastruktúrája';

CREATE INDEX idx_sn_parent ON storage_node(parent_id);
CREATE INDEX idx_sn_project ON storage_node(project_id);
CREATE INDEX idx_sn_workspace_type ON storage_node(workspace_id, type);

CREATE TABLE storage_node_role (
    node_id         CHAR(36)     NOT NULL COMMENT 'Fájl vagy Mappa',
    role_id         CHAR(36)     NOT NULL COMMENT 'Szerepkör hozzáférés',

    PRIMARY KEY (node_id, role_id),

    CONSTRAINT fk_snr_node FOREIGN KEY (node_id) REFERENCES storage_node(id) ON DELETE CASCADE,
    CONSTRAINT fk_snr_role FOREIGN KEY (role_id) REFERENCES app_role(id) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Fájlok és mappák szerepkör-alapú védelme';

CREATE TABLE storage_node_label (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Címke azonosító (UUID)',
    node_id         CHAR(36)     NOT NULL COMMENT 'Kapcsolódó fájl vagy mappa',
    label           VARCHAR(100) NOT NULL COMMENT 'Címke felirata',
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Létrehozás ideje',

    CONSTRAINT fk_snl_node FOREIGN KEY (node_id) REFERENCES storage_node(id) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Fájlok és mappák egyedi címkéi';

CREATE INDEX idx_snl_node ON storage_node_label(node_id);
CREATE INDEX idx_snl_label ON storage_node_label(label);

-- ============================================================
-- 6. REAL-TIME CHAT MODULE 
-- ============================================================
CREATE TABLE chat_room (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Szoba azonosító (UUID)',
    workspace_id    CHAR(36)     NOT NULL COMMENT 'Workspace',
    project_id      CHAR(36)     NULL COMMENT 'Projekt',
    name            VARCHAR(255) NULL COMMENT 'Szoba neve',
    type            TINYINT      NOT NULL DEFAULT 2 COMMENT '1 = direct, 2 = group, 3 = project_channel',
    is_private      BOOLEAN      NOT NULL DEFAULT FALSE COMMENT 'Privát-e',
    created_by      CHAR(36)     NULL COMMENT 'Létrehozó',
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Létrehozás ideje',

    CONSTRAINT fk_cr_workspace FOREIGN KEY (workspace_id) REFERENCES workspace(id) ON DELETE CASCADE,
    CONSTRAINT fk_cr_project FOREIGN KEY (project_id) REFERENCES project(id) ON DELETE CASCADE,
    CONSTRAINT fk_cr_creator FOREIGN KEY (created_by) REFERENCES app_user(id) ON DELETE SET NULL
) ENGINE=InnoDB COMMENT='Chat szobák';

CREATE TABLE chat_room_member (
    room_id              CHAR(36)  NOT NULL COMMENT 'Szoba',
    user_id              CHAR(36)  NOT NULL COMMENT 'Tag',
    joined_at            TIMESTAMP DEFAULT CURRENT_TIMESTAMP COMMENT 'Csatlakozás ideje',

    PRIMARY KEY (room_id, user_id),

    CONSTRAINT fk_crm_room FOREIGN KEY (room_id) REFERENCES chat_room(id) ON DELETE CASCADE,
    CONSTRAINT fk_crm_user FOREIGN KEY (user_id) REFERENCES app_user(id) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Chat szoba tagok';

CREATE TABLE chat_message (
    id               CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Üzenet azonosító (UUID)',
    room_id          CHAR(36)     NOT NULL COMMENT 'Szoba',
    sender_id        CHAR(36)     NULL COMMENT 'Feladó',
    parent_id        CHAR(36)     NULL COMMENT 'Szülő üzenet',
    message_text     TEXT         NOT NULL COMMENT 'Üzenet szövege',
    created_at       TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Küldés ideje',

    CONSTRAINT fk_cm_room FOREIGN KEY (room_id) REFERENCES chat_room(id) ON DELETE CASCADE,
    CONSTRAINT fk_cm_sender FOREIGN KEY (sender_id) REFERENCES app_user(id) ON DELETE SET NULL,
    CONSTRAINT fk_cm_parent FOREIGN KEY (parent_id) REFERENCES chat_message(id) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Chat üzenetek';

CREATE INDEX idx_cm_room_created ON chat_message(room_id, created_at);

CREATE TABLE chat_message_attachment (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Melléklet azonosító (UUID)',
    message_id      CHAR(36)     NOT NULL COMMENT 'Üzenet',
    storage_node_id CHAR(36)     NULL COMMENT 'Kapcsolódó storage node',
    file_url        VARCHAR(500) NOT NULL COMMENT 'Fájl URL',
    file_name       VARCHAR(255) NOT NULL COMMENT 'Fájl neve',

    CONSTRAINT fk_cma_message FOREIGN KEY (message_id) REFERENCES chat_message(id) ON DELETE CASCADE,
    CONSTRAINT fk_cma_storage FOREIGN KEY (storage_node_id) REFERENCES storage_node(id) ON DELETE SET NULL
) ENGINE=InnoDB COMMENT='Chat üzenet mellékletek';

-- ============================================================
-- 7. CALENDAR MODULE
-- ============================================================
CREATE TABLE calendar_event (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Esemény azonosító (UUID)',
    project_id      CHAR(36)     NULL COMMENT 'Projekt',
    title           VARCHAR(255) NOT NULL COMMENT 'Esemény címe',
    description     TEXT COMMENT 'Leírás',
    location        VARCHAR(255) NULL COMMENT 'Helyszín',
    start_time      DATETIME     NOT NULL COMMENT 'Kezdés',
    end_time        DATETIME     NOT NULL COMMENT 'Befejezés',
    is_all_day      BOOLEAN      NOT NULL DEFAULT FALSE COMMENT 'Egész napos-e',
    created_by      CHAR(36)     NULL COMMENT 'Létrehozó',

    CONSTRAINT fk_ce_project FOREIGN KEY (project_id) REFERENCES project(id) ON DELETE CASCADE,
    CONSTRAINT fk_ce_creator FOREIGN KEY (created_by) REFERENCES app_user(id) ON DELETE SET NULL
) ENGINE=InnoDB COMMENT='Naptár események';

CREATE INDEX idx_ce_project_start ON calendar_event(project_id, start_time);
CREATE INDEX idx_ce_dates ON calendar_event(start_time, end_time);

-- ============================================================
-- 8. AUTH, 2FA & JWT SESSION MANAGEMENT
-- ============================================================
CREATE TABLE user_session (
    id                      CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Munkamenet azonosító (UUID)',
    user_id                 CHAR(36)     NOT NULL COMMENT 'Felhasználó',
    refresh_token_hash      VARCHAR(64)  NOT NULL UNIQUE COMMENT 'Refresh token hash',
    replaced_by_session_id  CHAR(36)     NULL COMMENT 'Rotációs felváltó session',
    device_id               VARCHAR(255) NULL COMMENT 'Eszköz azonosító',
    device_info             VARCHAR(255) NULL COMMENT 'Böngésző / OS infó',
    ip_address              VARCHAR(45)  NULL COMMENT 'IP cím',
    is_revoked              BOOLEAN      NOT NULL DEFAULT FALSE COMMENT 'Visszavonták-e',
    expires_at              TIMESTAMP    NOT NULL COMMENT 'Lejárat ideje',
    created_at              TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Létrehozás ideje',
    updated_at              TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Utolsó módosítás ideje',

    CONSTRAINT fk_session_user FOREIGN KEY (user_id) REFERENCES app_user(id) ON DELETE CASCADE,
    CONSTRAINT fk_session_replaced FOREIGN KEY (replaced_by_session_id) REFERENCES user_session(id) ON DELETE SET NULL
) ENGINE=InnoDB COMMENT='Felhasználói munkamenetek';

CREATE INDEX idx_session_user ON user_session(user_id);
CREATE INDEX idx_session_refresh ON user_session(refresh_token_hash);

CREATE TABLE user_2fa_code (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Kód azonosító (UUID)',
    user_id         CHAR(36)     NOT NULL COMMENT 'Felhasználó',
    code_hash       VARCHAR(255) NOT NULL COMMENT 'Kód hash',
    failed_attempts TINYINT      UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Sikertelen próbálkozások',
    max_attempts    TINYINT      UNSIGNED NOT NULL DEFAULT 5 COMMENT 'Max próbálkozás',
    expires_at      TIMESTAMP    NOT NULL COMMENT 'Lejárat ideje',
    is_used         BOOLEAN      NOT NULL DEFAULT FALSE COMMENT 'Felhasználták-e',
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Létrehozás ideje',

    CONSTRAINT fk_u2c_user FOREIGN KEY (user_id) REFERENCES app_user(id) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Egyszeri 2FA kódok';

CREATE INDEX idx_u2c_user_active ON user_2fa_code(user_id, is_used, expires_at);

CREATE TABLE jwt_blacklisted_token (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Bejegyzés azonosító (UUID)',
    jti             VARCHAR(255) NOT NULL UNIQUE COMMENT 'JWT jti claim',
    user_id         CHAR(36)     NOT NULL COMMENT 'Felhasználó',
    expires_at      TIMESTAMP    NOT NULL COMMENT 'A token lejárata',
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Feketelistára kerülés ideje',

    CONSTRAINT fk_blacklist_user FOREIGN KEY (user_id) REFERENCES app_user(id) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Visszavont JWT tokenek feketelistája';

CREATE INDEX idx_blacklist_jti ON jwt_blacklisted_token(jti);
CREATE INDEX idx_blacklist_expires ON jwt_blacklisted_token(expires_at);

CREATE TABLE data_protection_keys (
    id          INT AUTO_INCREMENT PRIMARY KEY COMMENT 'Elsődleges kulcs',
    friendly_name LONGTEXT NULL               COMMENT 'Opcionális barátságos név',
    xml         LONGTEXT NULL               COMMENT 'A titkosító főkulcs XML formátumban'
) ENGINE=InnoDB COMMENT='ASP.NET Core DataProtection főkulcsok perzisztens tárolása';

SET FOREIGN_KEY_CHECKS = 1;