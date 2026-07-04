-- ============================================================
-- Projektmenedzsment alkalmazás adatbázis-sémája
-- MySQL 8.0+
-- ============================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ------------------------------------------------------------
-- users
-- ------------------------------------------------------------
CREATE TABLE users (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    name            VARCHAR(255) NOT NULL,
    email           VARCHAR(255) NOT NULL UNIQUE,
    password_hash   VARCHAR(255) NOT NULL,
    avatar_url      VARCHAR(500),
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- ------------------------------------------------------------
-- workspaces
-- ------------------------------------------------------------
CREATE TABLE workspaces (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    name            VARCHAR(255) NOT NULL,
    owner_id        CHAR(36)     NOT NULL,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_workspaces_owner
        FOREIGN KEY (owner_id) REFERENCES users(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE INDEX idx_workspaces_owner ON workspaces(owner_id);

-- ------------------------------------------------------------
-- workspace_members (N:M users <-> workspaces)
-- ------------------------------------------------------------
CREATE TABLE workspace_members (
    workspace_id    CHAR(36)     NOT NULL,
    user_id         CHAR(36)     NOT NULL,
    role            ENUM('owner', 'admin', 'member') NOT NULL DEFAULT 'member',
    joined_at       TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (workspace_id, user_id),

    CONSTRAINT fk_wm_workspace
        FOREIGN KEY (workspace_id) REFERENCES workspaces(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_wm_user
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

-- ------------------------------------------------------------
-- projects
-- ------------------------------------------------------------
CREATE TABLE projects (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    workspace_id    CHAR(36)     NOT NULL,
    name            VARCHAR(255) NOT NULL,
    description     TEXT,
    status          ENUM('planned', 'active', 'on_hold', 'completed', 'archived') NOT NULL DEFAULT 'planned',
    deadline        DATE,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_projects_workspace
        FOREIGN KEY (workspace_id) REFERENCES workspaces(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE INDEX idx_projects_workspace ON projects(workspace_id);
CREATE INDEX idx_projects_status ON projects(status);

-- ------------------------------------------------------------
-- project_members (N:M users <-> projects)
-- ------------------------------------------------------------
CREATE TABLE project_members (
    project_id      CHAR(36)     NOT NULL,
    user_id         CHAR(36)     NOT NULL,
    role            ENUM('manager', 'member', 'viewer') NOT NULL DEFAULT 'member',
    joined_at       TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (project_id, user_id),

    CONSTRAINT fk_pm_project
        FOREIGN KEY (project_id) REFERENCES projects(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_pm_user
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

-- ------------------------------------------------------------
-- tasks
-- ------------------------------------------------------------
CREATE TABLE tasks (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    project_id      CHAR(36)     NOT NULL,
    title           VARCHAR(255) NOT NULL,
    description     TEXT,
    status          ENUM('todo', 'in_progress', 'in_review', 'done') NOT NULL DEFAULT 'todo',
    priority        ENUM('low', 'medium', 'high', 'urgent') NOT NULL DEFAULT 'medium',
    assignee_id     CHAR(36),
    reporter_id     CHAR(36),
    due_date        DATE,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_tasks_project
        FOREIGN KEY (project_id) REFERENCES projects(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_tasks_assignee
        FOREIGN KEY (assignee_id) REFERENCES users(id)
        ON DELETE SET NULL,
    CONSTRAINT fk_tasks_reporter
        FOREIGN KEY (reporter_id) REFERENCES users(id)
        ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE INDEX idx_tasks_project ON tasks(project_id);
CREATE INDEX idx_tasks_assignee ON tasks(assignee_id);
CREATE INDEX idx_tasks_status ON tasks(status);
CREATE INDEX idx_tasks_due_date ON tasks(due_date);

-- ------------------------------------------------------------
-- task_dependencies (pl. "blocks" / "depends_on")
-- ------------------------------------------------------------
CREATE TABLE task_dependencies (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    task_id         CHAR(36)     NOT NULL,
    depends_on_id   CHAR(36)     NOT NULL,
    type            ENUM('blocks', 'related_to') NOT NULL DEFAULT 'blocks',

    CONSTRAINT fk_td_task
        FOREIGN KEY (task_id) REFERENCES tasks(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_td_depends_on
        FOREIGN KEY (depends_on_id) REFERENCES tasks(id)
        ON DELETE CASCADE,
    CONSTRAINT uq_task_dependency UNIQUE (task_id, depends_on_id)
) ENGINE=InnoDB;

-- ------------------------------------------------------------
-- comments
-- ------------------------------------------------------------
CREATE TABLE comments (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    task_id         CHAR(36)     NOT NULL,
    user_id         CHAR(36)     NOT NULL,
    content         TEXT         NOT NULL,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_comments_task
        FOREIGN KEY (task_id) REFERENCES tasks(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_comments_user
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE INDEX idx_comments_task ON comments(task_id);

-- ------------------------------------------------------------
-- attachments
-- ------------------------------------------------------------
CREATE TABLE attachments (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    task_id         CHAR(36)     NOT NULL,
    file_url        VARCHAR(500) NOT NULL,
    file_name       VARCHAR(255) NOT NULL,
    file_size       INT,
    uploaded_by     CHAR(36),
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_attachments_task
        FOREIGN KEY (task_id) REFERENCES tasks(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_attachments_uploader
        FOREIGN KEY (uploaded_by) REFERENCES users(id)
        ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE INDEX idx_attachments_task ON attachments(task_id);

-- ------------------------------------------------------------
-- labels
-- ------------------------------------------------------------
CREATE TABLE labels (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    project_id      CHAR(36)     NOT NULL,
    name            VARCHAR(100) NOT NULL,
    color           VARCHAR(7)   NOT NULL DEFAULT '#888888',

    CONSTRAINT fk_labels_project
        FOREIGN KEY (project_id) REFERENCES projects(id)
        ON DELETE CASCADE,
    CONSTRAINT uq_label_per_project UNIQUE (project_id, name)
) ENGINE=InnoDB;

-- ------------------------------------------------------------
-- task_labels (N:M tasks <-> labels)
-- ------------------------------------------------------------
CREATE TABLE task_labels (
    task_id         CHAR(36)     NOT NULL,
    label_id        CHAR(36)     NOT NULL,

    PRIMARY KEY (task_id, label_id),

    CONSTRAINT fk_tl_task
        FOREIGN KEY (task_id) REFERENCES tasks(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_tl_label
        FOREIGN KEY (label_id) REFERENCES labels(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

-- ------------------------------------------------------------
-- activity_log
-- ------------------------------------------------------------
CREATE TABLE activity_log (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    task_id         CHAR(36),
    user_id         CHAR(36),
    action          VARCHAR(100) NOT NULL,
    details         JSON,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_activity_task
        FOREIGN KEY (task_id) REFERENCES tasks(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_activity_user
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE INDEX idx_activity_task ON activity_log(task_id);
CREATE INDEX idx_activity_created ON activity_log(created_at);

-- ------------------------------------------------------------
-- notifications
-- ------------------------------------------------------------
CREATE TABLE notifications (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    user_id         CHAR(36)     NOT NULL,
    content         VARCHAR(500) NOT NULL,
    is_read         BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_notifications_user
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE INDEX idx_notifications_user ON notifications(user_id, is_read);

SET FOREIGN_KEY_CHECKS = 1;