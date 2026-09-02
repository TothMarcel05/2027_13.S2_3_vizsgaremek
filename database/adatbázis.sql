DROP DATABASE IF EXISTS TDM_KM_ProjectManagerApp;
CREATE DATABASE TDM_KM_ProjectManagerApp
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE TDM_KM_ProjectManagerApp;

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ------------------------------------------------------------
-- AppUsers (régi: users)
-- ------------------------------------------------------------
CREATE TABLE app_users (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    name            VARCHAR(255) NOT NULL,
    email           VARCHAR(255) NOT NULL UNIQUE,
    password_hash   VARCHAR(255) NOT NULL,
    avatar_url      VARCHAR(500),
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- ------------------------------------------------------------
-- UserWorkspaces (régi: workspaces)
-- ------------------------------------------------------------
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

-- ------------------------------------------------------------
-- WorkspaceMembers
-- ------------------------------------------------------------
CREATE TABLE workspace_members (
    workspace_id    CHAR(36)     NOT NULL,
    user_id         CHAR(36)     NOT NULL,
    role            ENUM('owner', 'admin', 'member') NOT NULL DEFAULT 'member',
    joined_at       TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (workspace_id, user_id),

    CONSTRAINT fk_wm_user_workspace
        FOREIGN KEY (workspace_id) REFERENCES user_workspaces(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_wm_user
        FOREIGN KEY (user_id) REFERENCES app_users(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

-- ------------------------------------------------------------
-- AppProjects (régi: projects)
-- ------------------------------------------------------------
CREATE TABLE app_projects (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    workspace_id    CHAR(36)     NOT NULL,
    name            VARCHAR(255) NOT NULL,
    description     TEXT,
    status          ENUM('planned', 'active', 'on_hold', 'completed', 'archived') NOT NULL DEFAULT 'planned',
    deadline        DATE,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_app_projects_user_workspace
        FOREIGN KEY (workspace_id) REFERENCES user_workspaces(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE INDEX idx_app_projects_workspace ON app_projects(workspace_id);
CREATE INDEX idx_app_projects_status ON app_projects(status);

-- ------------------------------------------------------------
-- ProjectMembers
-- ------------------------------------------------------------
CREATE TABLE project_members (
    project_id      CHAR(36)     NOT NULL,
    user_id         CHAR(36)     NOT NULL,
    role            ENUM('manager', 'member', 'viewer') NOT NULL DEFAULT 'member',
    joined_at       TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (project_id, user_id),

    CONSTRAINT fk_pm_project
        FOREIGN KEY (project_id) REFERENCES app_projects(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_pm_user
        FOREIGN KEY (user_id) REFERENCES app_users(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

-- ------------------------------------------------------------
-- ProjectTasks (régi: tasks)
-- ------------------------------------------------------------
CREATE TABLE project_tasks (
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

    CONSTRAINT fk_project_tasks_project
        FOREIGN KEY (project_id) REFERENCES app_projects(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_project_tasks_assignee
        FOREIGN KEY (assignee_id) REFERENCES app_users(id)
        ON DELETE SET NULL,
    CONSTRAINT fk_project_tasks_reporter
        FOREIGN KEY (reporter_id) REFERENCES app_users(id)
        ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE INDEX idx_project_tasks_project ON project_tasks(project_id);
CREATE INDEX idx_project_tasks_assignee ON project_tasks(assignee_id);
CREATE INDEX idx_project_tasks_status ON project_tasks(status);
CREATE INDEX idx_project_tasks_due_date ON project_tasks(due_date);

-- ------------------------------------------------------------
-- ProjectTaskDependencies (régi: task_dependencies)
-- ------------------------------------------------------------
CREATE TABLE project_task_dependencies (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    task_id         CHAR(36)     NOT NULL,
    depends_on_id   CHAR(36)     NOT NULL,
    type            ENUM('blocks', 'related_to') NOT NULL DEFAULT 'blocks',

    CONSTRAINT fk_ptd_task
        FOREIGN KEY (task_id) REFERENCES project_tasks(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_ptd_depends_on
        FOREIGN KEY (depends_on_id) REFERENCES project_tasks(id)
        ON DELETE CASCADE,
    CONSTRAINT uq_project_task_dependency UNIQUE (task_id, depends_on_id)
) ENGINE=InnoDB;

-- ------------------------------------------------------------
-- ProjectComments (régi: comments)
-- ------------------------------------------------------------
CREATE TABLE project_comments (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    task_id         CHAR(36)     NOT NULL,
    user_id         CHAR(36)     NOT NULL,
    content         TEXT         NOT NULL,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_project_comments_task
        FOREIGN KEY (task_id) REFERENCES project_tasks(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_project_comments_user
        FOREIGN KEY (user_id) REFERENCES app_users(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE INDEX idx_project_comments_task ON project_comments(task_id);

-- ------------------------------------------------------------
-- ProjectAttachments (régi: attachments)
-- ------------------------------------------------------------
CREATE TABLE project_attachments (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    task_id         CHAR(36)     NOT NULL,
    file_url        VARCHAR(500) NOT NULL,
    file_name       VARCHAR(255) NOT NULL,
    file_size       INT,
    uploaded_by     CHAR(36),
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_project_attachments_task
        FOREIGN KEY (task_id) REFERENCES project_tasks(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_project_attachments_uploader
        FOREIGN KEY (uploaded_by) REFERENCES app_users(id)
        ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE INDEX idx_project_attachments_task ON project_attachments(task_id);

-- ------------------------------------------------------------
-- ProjectLabels (régi: labels)
-- ------------------------------------------------------------
CREATE TABLE project_labels (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    project_id      CHAR(36)     NOT NULL,
    name            VARCHAR(100) NOT NULL,
    color           VARCHAR(7)   NOT NULL DEFAULT '#888888',

    CONSTRAINT fk_project_labels_project
        FOREIGN KEY (project_id) REFERENCES app_projects(id)
        ON DELETE CASCADE,
    CONSTRAINT uq_project_label_per_project UNIQUE (project_id, name)
) ENGINE=InnoDB;

-- ------------------------------------------------------------
-- ProjectTaskLabels (régi: task_labels)
-- ------------------------------------------------------------
CREATE TABLE project_task_labels (
    task_id         CHAR(36)     NOT NULL,
    label_id        CHAR(36)     NOT NULL,

    PRIMARY KEY (task_id, label_id),

    CONSTRAINT fk_ptl_task
        FOREIGN KEY (task_id) REFERENCES project_tasks(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_ptl_label
        FOREIGN KEY (label_id) REFERENCES project_labels(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

-- ------------------------------------------------------------
-- ProjectActivityLog (régi: activity_log)
-- ------------------------------------------------------------
CREATE TABLE project_activity_log (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    task_id         CHAR(36)     NULL,
    user_id         CHAR(36)     NULL,
    action          VARCHAR(100) NOT NULL,
    details         JSON,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_project_activity_task
        FOREIGN KEY (task_id) REFERENCES project_tasks(id)
        ON DELETE CASCADE,
    CONSTRAINT fk_project_activity_user
        FOREIGN KEY (user_id) REFERENCES app_users(id)
        ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE INDEX idx_project_activity_task ON project_activity_log(task_id);
CREATE INDEX idx_project_activity_created ON project_activity_log(created_at);

-- ------------------------------------------------------------
-- UserNotifications (régi: notifications)
-- ------------------------------------------------------------
CREATE TABLE user_notifications (
    id              CHAR(36)     PRIMARY KEY DEFAULT (UUID()),
    user_id         CHAR(36)     NOT NULL,
    content         VARCHAR(500) NOT NULL,
    is_read         BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_user_notifications_user
        FOREIGN KEY (user_id) REFERENCES app_users(id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE INDEX idx_user_notifications_user ON user_notifications(user_id, is_read);

SET FOREIGN_KEY_CHECKS = 1;
