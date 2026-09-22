erDiagram

    %% ----------------------------------------------------
    %% 1. APP USERS & CORE IAM
    %% ----------------------------------------------------
    app_users {
        CHAR_36 id PK
        VARCHAR_255 name
        VARCHAR_255 email UK
        VARCHAR_255 password_hash
        VARCHAR_500 avatar_url
        TIMESTAMP created_at
        TIMESTAMP updated_at
    }

    user_workspaces {
        CHAR_36 id PK
        VARCHAR_255 name
        CHAR_36 owner_id FK
        TIMESTAMP created_at
    }

    app_users ||--o{ user_workspaces : "owners"

    %% ----------------------------------------------------
    %% 2. DINAMIKUS RBAC
    %% ----------------------------------------------------
    permissions {
        CHAR_36 id PK
        VARCHAR_100 code UK
        VARCHAR_255 name
        TEXT description
        TIMESTAMP created_at
    }

    roles {
        CHAR_36 id PK
        CHAR_36 workspace_id FK
        VARCHAR_100 name
        TEXT description
        BOOLEAN is_system
        TIMESTAMP created_at
    }

    role_permissions {
        CHAR_36 role_id PK, FK
        CHAR_36 permission_id PK, FK
    }

    user_workspaces ||--o{ roles : "contains"
    roles ||--o{ role_permissions : "has"
    permissions ||--o{ role_permissions : "granted_to"

    %% ----------------------------------------------------
    %% 3. WORKSPACE & PROJECT MEMBERSHIPS
    %% ----------------------------------------------------
    workspace_members {
        CHAR_36 workspace_id PK, FK
        CHAR_36 user_id PK, FK
        TIMESTAMP joined_at
    }

    user_workspace_roles {
        CHAR_36 workspace_id PK, FK
        CHAR_36 user_id PK, FK
        CHAR_36 role_id PK, FK
    }

    app_projects {
        CHAR_36 id PK
        CHAR_36 workspace_id FK
        VARCHAR_255 name
        TEXT description
        ENUM status
        DATE deadline
        TIMESTAMP created_at
        TIMESTAMP updated_at
    }

    project_members {
        CHAR_36 project_id PK, FK
        CHAR_36 user_id PK, FK
        TIMESTAMP joined_at
    }

    user_project_roles {
        CHAR_36 project_id PK, FK
        CHAR_36 user_id PK, FK
        CHAR_36 role_id PK, FK
    }

    user_workspaces ||--o{ workspace_members : "has"
    app_users ||--o{ workspace_members : "belongs_to"
    workspace_members ||--o{ user_workspace_roles : "assigned"
    roles ||--o{ user_workspace_roles : "role_in_ws"

    user_workspaces ||--o{ app_projects : "contains"
    app_projects ||--o{ project_members : "has"
    app_users ||--o{ project_members : "belongs_to"
    project_members ||--o{ user_project_roles : "assigned"
    roles ||--o{ user_project_roles : "role_in_project"

    %% ----------------------------------------------------
    %% 4. PROJECT TASKS & SUB-MODULES
    %% ----------------------------------------------------
    project_tasks {
        CHAR_36 id PK
        CHAR_36 project_id FK
        VARCHAR_255 title
        TEXT description
        ENUM_hu status
        ENUM priority
        CHAR_36 assignee_id FK
        CHAR_36 reporter_id FK
        DATE due_date
        TIMESTAMP created_at
        TIMESTAMP updated_at
    }

    project_comments {
        CHAR_36 id PK
        CHAR_36 task_id FK
        CHAR_36 user_id FK
        TEXT content
        TIMESTAMP created_at
    }

    project_labels {
        CHAR_36 id PK
        CHAR_36 project_id FK
        VARCHAR_100 name
        VARCHAR_7 color
    }

    project_task_labels {
        CHAR_36 task_id PK, FK
        CHAR_36 label_id PK, FK
    }

    project_activity_log {
        CHAR_36 id PK
        CHAR_36 task_id FK
        CHAR_36 user_id FK
        VARCHAR_100 action
        JSON details
        TIMESTAMP created_at
    }

    user_notifications {
        CHAR_36 id PK
        CHAR_36 user_id FK
        VARCHAR_500 content
        BOOLEAN is_read
        TIMESTAMP created_at
    }

    app_projects ||--o{ project_tasks : "contains"
    app_users ||--o{ project_tasks : "assigned_to"
    app_users ||--o{ project_tasks : "reported_by"
    
    project_tasks ||--o{ project_comments : "has"
    app_users ||--o{ project_comments : "authored"

    app_projects ||--o{ project_labels : "defines"
    project_tasks ||--o{ project_task_labels : "tagged_with"
    project_labels ||--o{ project_task_labels : "applied_to"

    project_tasks ||--o{ project_activity_log : "logged_for"
    app_users ||--o{ project_activity_log : "performed_by"
    app_users ||--o{ user_notifications : "notified"

    %% ----------------------------------------------------
    %% 5. STORAGE & DRIVE MODULE
    %% ----------------------------------------------------
    storage_nodes {
        CHAR_36 id PK
        CHAR_36 workspace_id FK
        CHAR_36 project_id FK
        CHAR_36 parent_id FK
        VARCHAR_255 name
        ENUM type
        VARCHAR_127 mime_type
        BIGINT size_bytes
        CHAR_36 created_by FK
        CHAR_36 updated_by FK
        TIMESTAMP created_at
        TIMESTAMP updated_at
    }

    storage_tags {
        CHAR_36 id PK
        CHAR_36 workspace_id FK
        VARCHAR_100 name
        VARCHAR_7 color
    }

    storage_tag_rbac {
        CHAR_36 tag_id PK, FK
        CHAR_36 role_id PK, FK
        BOOLEAN can_read
        BOOLEAN can_write
        BOOLEAN can_delete
    }

    storage_node_tags {
        CHAR_36 node_id PK, FK
        CHAR_36 tag_id PK, FK
    }

    storage_file_versions {
        CHAR_36 id PK
        CHAR_36 node_id FK
        VARCHAR_500 file_path
        INT version_number
        BIGINT size_bytes
        CHAR_36 uploaded_by FK
        TIMESTAMP created_at
    }

    user_workspaces ||--o{ storage_nodes : "owns_nodes"
    app_projects ||--o{ storage_nodes : "owns_nodes"
    storage_nodes ||--o{ storage_nodes : "parent_of"
    app_users ||--o{ storage_nodes : "created_node"

    user_workspaces ||--o{ storage_tags : "owns_tags"
    storage_tags ||--o{ storage_tag_rbac : "protected_by"
    roles ||--o{ storage_tag_rbac : "has_access"

    storage_nodes ||--o{ storage_node_tags : "tagged_with"
    storage_tags ||--o{ storage_node_tags : "applied_to"

    storage_nodes ||--o{ storage_file_versions : "has_versions"
    app_users ||--o{ storage_file_versions : "uploaded_by"

    %% ----------------------------------------------------
    %% 6. END-TO-END CHAT MODULE
    %% ----------------------------------------------------
    chat_rooms {
        CHAR_36 id PK
        CHAR_36 workspace_id FK
        CHAR_36 project_id FK
        VARCHAR_255 name
        ENUM type
        BOOLEAN is_private
        CHAR_36 created_by FK
        TIMESTAMP created_at
    }

    e2e_user_keys {
        CHAR_36 id PK
        CHAR_36 user_id FK
        VARCHAR_255 device_id
        TEXT identity_key
        TEXT signed_prekey
        TEXT prekey_signature
    }

    e2e_room_keys {
        CHAR_36 room_id PK, FK
        CHAR_36 user_id PK, FK
        INT key_version PK
        TEXT encrypted_key
    }

    e2e_chat_messages {
        CHAR_36 id PK
        CHAR_36 room_id FK
        CHAR_36 sender_id FK
        VARCHAR_255 sender_device_id
        CHAR_36 parent_id FK
        TEXT ciphertext
        VARCHAR_255 nonce_or_iv
        INT key_version
        TIMESTAMP created_at
    }

    chat_room_members {
        CHAR_36 room_id PK, FK
        CHAR_36 user_id PK, FK
        CHAR_36 last_read_message_id FK
    }

    chat_message_attachments {
        CHAR_36 id PK
        CHAR_36 message_id FK
        CHAR_36 storage_node_id FK
        VARCHAR_500 file_url
        VARCHAR_255 file_name
    }

    user_workspaces ||--o{ chat_rooms : "contains"
    app_projects ||--o{ chat_rooms : "channel_for"

    app_users ||--o{ e2e_user_keys : "owns_device_keys"
    chat_rooms ||--o{ e2e_room_keys : "has_keys"
    app_users ||--o{ e2e_room_keys : "holds_key"

    chat_rooms ||--o{ e2e_chat_messages : "contains"
    app_users ||--o{ e2e_chat_messages : "sent_by"
    e2e_chat_messages ||--o{ e2e_chat_messages : "reply_to"

    chat_rooms ||--o{ chat_room_members : "members"
    app_users ||--o{ chat_room_members : "joined"
    e2e_chat_messages ||--o{ chat_room_members : "last_read"

    e2e_chat_messages ||--o{ chat_message_attachments : "has_attachment"
    storage_nodes ||--o{ chat_message_attachments : "referenced_in"

    %% ----------------------------------------------------
    %% 7. CALENDAR MODULE
    %% ----------------------------------------------------
    calendars {
        CHAR_36 id PK
        CHAR_36 workspace_id FK
        CHAR_36 project_id FK
        CHAR_36 user_id FK
        VARCHAR_255 name
        VARCHAR_7 color
        ENUM type
    }

    calendar_events {
        CHAR_36 id PK
        CHAR_36 calendar_id FK
        CHAR_36 task_id FK
        VARCHAR_255 title
        TEXT description
        DATETIME start_time
        DATETIME end_time
        BOOLEAN is_all_day
        CHAR_36 created_by FK
    }

    user_workspaces ||--o{ calendars : "owns_cal"
    app_projects ||--o{ calendars : "owns_cal"
    app_users ||--o{ calendars : "owns_cal"

    calendars ||--o{ calendar_events : "contains"
    project_tasks ||--o{ calendar_events : "linked_to_task"
    app_users ||--o{ calendar_events : "created_event"

    %% ----------------------------------------------------
    %% 8. AUTH & JWT SESSION MANAGEMENT
    %% ----------------------------------------------------
    user_sessions {
        CHAR_36 id PK
        CHAR_36 user_id FK
        VARCHAR_64 refresh_token_hash UK
        CHAR_36 replaced_by_session_id FK
        VARCHAR_255 device_info
        VARCHAR_45 ip_address
        BOOLEAN is_revoked
        TIMESTAMP expires_at
    }

    jwt_blacklisted_tokens {
        CHAR_36 id PK
        VARCHAR_255 jti UK
        CHAR_36 user_id FK
        TIMESTAMP expires_at
    }

    app_users ||--o{ user_sessions : "has_sessions"
    user_sessions ||--o{ user_sessions : "rotated_to"
    app_users ||--o{ jwt_blacklisted_tokens : "blacklisted"