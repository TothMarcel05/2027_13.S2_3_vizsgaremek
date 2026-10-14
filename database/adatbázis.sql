DROP DATABASE IF EXISTS TMD_KM_ProjectManagerApp;
CREATE DATABASE TMD_KM_ProjectManagerApp
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE TMD_KM_ProjectManagerApp;

SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci;
SET FOREIGN_KEY_CHECKS = 0;

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
    global_role                 ENUM('User', 'SystemAdmin') NOT NULL DEFAULT 'User' COMMENT 'Rendszerszintű szerep (rendszeradmin jelölés)',
    deleted_at                  TIMESTAMP    NULL COMMENT 'Soft delete: a törlés ideje, NULL = aktív felhasználó',

    -- 2FA kiegészítések
    is_2fa_enabled              BOOLEAN      NOT NULL DEFAULT FALSE COMMENT 'Be van-e kapcsolva a kétfaktoros hitelesítés',
    two_factor_type             ENUM('None', 'Authenticator', 'Email') NOT NULL DEFAULT 'None' COMMENT '2FA módszer',
    two_factor_secret           VARCHAR(255) NULL COMMENT '2FA titkos kulcs (Authenticator)',
    two_factor_recovery_codes   TEXT         NULL COMMENT '2FA helyreállító kódok'
) ENGINE=InnoDB COMMENT='Alkalmazás felhasználói és 2FA beállításaik';

CREATE TABLE workspace (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Workspace azonosító (UUID)',
    name            VARCHAR(255) NOT NULL COMMENT 'Workspace neve',
    owner_id        CHAR(36)     NOT NULL COMMENT 'Tulajdonos felhasználó',
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Létrehozás ideje',

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
    code        VARCHAR(100) NOT NULL UNIQUE COMMENT 'Gépi kód, pl. workspace.manage, project.task.create, storage.file.upload',
    scope       ENUM('workspace', 'project') NOT NULL COMMENT 'Hatókör: melyik szinten értelmezhető a jog',
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
) ENGINE=InnoDB COMMENT='Globális szerepkörök, workspace és projekt szinten is ugyanezek használatosak';

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
-- 3. WORKSPACE, PROJECT, TEAM TAGSÁGOK & MEGHÍVÓK
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
    status          ENUM('planned', 'active', 'on_hold', 'completed', 'archived') NOT NULL DEFAULT 'planned' COMMENT 'Projekt állapota',
    deadline        DATE COMMENT 'Határidő',
    owner_id        CHAR(36)     NULL COMMENT 'Projekt felelőse (általában a létrehozó)',
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

CREATE TABLE team (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Csapat azonosító (UUID)',
    owner_id        CHAR(36)     NOT NULL COMMENT 'Csapat tulajdonosa (ő kezeli a tagságot)',
    name            VARCHAR(100) NOT NULL COMMENT 'Csapat neve',
    description     TEXT COMMENT 'Csapat leírása',
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Létrehozás ideje',

    CONSTRAINT uq_team_name UNIQUE (owner_id, name),

    CONSTRAINT fk_team_owner
        FOREIGN KEY (owner_id) REFERENCES app_user(id)
        ON DELETE RESTRICT
) ENGINE=InnoDB COMMENT='Workspace-ektől független csapattag-csoportok';

CREATE TABLE project_member (
    project_id   CHAR(36)  NOT NULL COMMENT 'Projekt azonosító',
    workspace_id CHAR(36)  NOT NULL COMMENT 'A projekt workspace-e',
    user_id      CHAR(36)  NOT NULL COMMENT 'Felhasználó azonosító',
    role_id      CHAR(36)  NOT NULL COMMENT 'A tag szerepköre a projektben (pl. Developer, Viewer)',
    team_id      CHAR(36)  NULL     COMMENT 'Opcionális: melyik csapathoz tartozik ezen a projekten belül (NULL = egyéni projekttag)',
    assigned_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP COMMENT 'Hozzárendelés ideje',

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
        ON DELETE RESTRICT,
    CONSTRAINT fk_pm_team
        FOREIGN KEY (team_id) REFERENCES team(id)
        ON DELETE SET NULL
) ENGINE=InnoDB COMMENT='Projekttagságok és csapatszintű hozzárendelések';

CREATE INDEX idx_pm_user ON project_member(user_id);
CREATE INDEX idx_pm_team ON project_member(project_id, team_id);
CREATE INDEX idx_pm_role ON project_member(project_id, role_id);

CREATE TABLE app_invitation (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Meghívó azonosító (UUID)',
    type            ENUM('workspace', 'project', 'team') NOT NULL COMMENT 'Milyen szintű meghívóról van szó',
    target_id       CHAR(36)     NOT NULL COMMENT 'A workspace_id, project_id vagy team_id azonosítója',
    invited_email   VARCHAR(255) NOT NULL COMMENT 'A meghívott e-mail címe (regisztrálatlanoknak is)',
    invited_user_id CHAR(36)     NULL COMMENT 'A meghívott felhasználó azonosítója (ha már regisztrált)',
    role_id         CHAR(36)     NULL COMMENT 'A felajánlott szerepkör azonosítója',
    invited_by      CHAR(36)     NOT NULL COMMENT 'A meghívást kiküldő felhasználó azonosítója',
    status          ENUM('pending', 'accepted', 'refused', 'expired') NOT NULL DEFAULT 'pending' COMMENT 'Meghívó állapota',
    token           VARCHAR(255) NOT NULL UNIQUE COMMENT 'Egyedi biztonsági token e-mailes hivatkozáshoz',
    expires_at      TIMESTAMP    NOT NULL COMMENT 'A meghívó lejárati ideje',
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Kiküldés ideje',
    responded_at    TIMESTAMP    NULL COMMENT 'A válaszadás (elfogadás/elutasítás) ideje',

    CONSTRAINT fk_inv_role FOREIGN KEY (role_id) REFERENCES app_role(id) ON DELETE SET NULL,
    CONSTRAINT fk_inv_invited_user FOREIGN KEY (invited_user_id) REFERENCES app_user(id) ON DELETE CASCADE,
    CONSTRAINT fk_inv_invited_by FOREIGN KEY (invited_by) REFERENCES app_user(id) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Meghívások kezelése státuszkövetéssel (pending, accepted, refused, expired)';

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
    status              ENUM('planning', 'on_going', 'ready') NOT NULL DEFAULT 'planning' COMMENT 'Task állapota',
    priority            ENUM('low', 'medium', 'high', 'urgent') NOT NULL DEFAULT 'medium' COMMENT 'Prioritás',
    assignee_role_id    CHAR(36)     NULL COMMENT 'Szerepkör, amire a task ki van hirdetve (akiknek ez a szerepkörük, az megkapja/látja)',
    assignee_user_id    CHAR(36)     NULL COMMENT 'Konkrét felhasználó, aki lefoglalta/megkapta a feladatot (NULL = szabadon felvehető)',
    created_by          CHAR(36)     NULL COMMENT 'Létrehozó felhasználó',
    due_date            DATE COMMENT 'Határidő',
    created_at          TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Létrehozás ideje',
    updated_at          TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Utolsó módosítás ideje',

    CONSTRAINT fk_task_project FOREIGN KEY (project_id) REFERENCES project(id) ON DELETE CASCADE,
    CONSTRAINT fk_task_assignee_role FOREIGN KEY (assignee_role_id) REFERENCES app_role(id) ON DELETE SET NULL,
    CONSTRAINT fk_task_assignee_user FOREIGN KEY (assignee_user_id) REFERENCES app_user(id) ON DELETE SET NULL,
    CONSTRAINT fk_task_creator FOREIGN KEY (created_by) REFERENCES app_user(id) ON DELETE SET NULL
) ENGINE=InnoDB COMMENT='Projekt taskok (szerepkörre kiírva és/vagy személyre lefoglalva)';

CREATE INDEX idx_task_project_due ON project_task(project_id, due_date);
CREATE INDEX idx_task_project_role ON project_task(project_id, assignee_role_id);
CREATE INDEX idx_task_assignee_user ON project_task(assignee_user_id);
CREATE INDEX idx_task_status_role ON project_task(project_id, status, assignee_role_id);

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
) ENGINE=InnoDB COMMENT='Projekt címkék (taskok vizuális jelölésére)';

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
-- 5. STORAGE & DRIVE MODULE (KÖZVETLEN SZEREPKÖR VÉDELEMMEL És CÍMKÉKKEL)
-- ============================================================
CREATE TABLE storage_node (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Node azonosító (UUID)',
    workspace_id    CHAR(36)     NOT NULL COMMENT 'Workspace',
    project_id      CHAR(36)     NULL COMMENT 'Projekt (Projekt létrehozásakor automatikusan létrejön a nevén a gyökérmappa)',
    parent_id       CHAR(36)     NULL COMMENT 'Szülő mappa (NULL = gyökér)',
    name            VARCHAR(255) NOT NULL COMMENT 'Fájl vagy mappa neve',
    type            ENUM('folder', 'file') NOT NULL COMMENT 'Node típusa',
    mime_type       VARCHAR(127) NULL COMMENT 'MIME típus (csak fájlnál)',
    storage_path    VARCHAR(500) NULL COMMENT 'Fájl fizikai helye a tárolón',
    size_bytes      BIGINT       UNSIGNED DEFAULT 0 COMMENT 'Méret bájtban',
    created_by      CHAR(36)     NULL COMMENT 'Létrehozó felhasználó',
    updated_by      CHAR(36)     NULL COMMENT 'Utolsó módosító',
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP COMMENT 'Létrehozás ideje',
    updated_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Utolsó módosítás ideje',

    CONSTRAINT fk_sn_workspace FOREIGN KEY (workspace_id) REFERENCES workspace(id) ON DELETE CASCADE,
    CONSTRAINT fk_sn_project FOREIGN KEY (project_id) REFERENCES project(id) ON DELETE CASCADE,
    CONSTRAINT fk_sn_parent FOREIGN KEY (parent_id) REFERENCES storage_node(id) ON DELETE CASCADE,
    CONSTRAINT fk_sn_creator FOREIGN KEY (created_by) REFERENCES app_user(id) ON DELETE SET NULL,
    CONSTRAINT fk_sn_updater FOREIGN KEY (updated_by) REFERENCES app_user(id) ON DELETE SET NULL
) ENGINE=InnoDB COMMENT='Fájlok és mappák fastruktúrája';

CREATE INDEX idx_sn_parent ON storage_node(parent_id);
CREATE INDEX idx_sn_project ON storage_node(project_id);

CREATE TABLE storage_node_role (
    node_id         CHAR(36)     NOT NULL COMMENT 'Fájl vagy Mappa',
    role_id         CHAR(36)     NOT NULL COMMENT 'Szerepkör, amivel rendelkező tagok láthatják/hozzáférhetnek ehhez a mappához vagy fájlhoz',

    PRIMARY KEY (node_id, role_id),

    CONSTRAINT fk_snr_node FOREIGN KEY (node_id) REFERENCES storage_node(id) ON DELETE CASCADE,
    CONSTRAINT fk_snr_role FOREIGN KEY (role_id) REFERENCES app_role(id) ON DELETE CASCADE
) ENGINE=InnoDB COMMENT='Fájlok és mappák közvetlen szerepkör-alapú védelme (ha a tábla üres az adott node-ra, minden projekttag eléri)';

CREATE TABLE storage_node_label (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()) COMMENT 'Címke azonosító (UUID)',
    node_id         CHAR(36)     NOT NULL COMMENT 'Kapcsolódó fájl vagy mappa azonosítója',
    label           VARCHAR(100) NOT NULL COMMENT 'Címke felirata/neve (pl. Bizalmas, Pénzügy, Végleges)',
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
    project_id      CHAR(36)     NULL COMMENT 'Projekt (ha projekt csatorna)',
    name            VARCHAR(255) NULL COMMENT 'Szoba neve',
    type            ENUM('direct', 'group', 'project_channel') NOT NULL DEFAULT 'group' COMMENT 'Típus',
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
    parent_id        CHAR(36)     NULL COMMENT 'Szülő üzenet (válasz esetén)',
    message_text     TEXT         NOT NULL COMMENT 'Üzenet olvasható szövege',
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
    failed_attempts TINYINT      UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Sikertelen próbálkozások száma',
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

SET FOREIGN_KEY_CHECKS = 1;