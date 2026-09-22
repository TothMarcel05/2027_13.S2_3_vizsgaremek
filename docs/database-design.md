```mermaid

erDiagram

    app_users {
        string id PK
        string name
        string email UK
        string password_hash
        string avatar_url
        datetime created_at
        datetime updated_at
    }

    user_workspaces {
        string id PK
        string name
        string owner_id FK
        datetime created_at
    }

    app_users ||--o{ user_workspaces : "owners"

    permissions {
        string id PK
        string code UK
        string name
        text description
        datetime created_at
    }

    roles {
        string id PK
        string workspace_id FK
        string name
        text description
        boolean is_system
        datetime created_at
    }

    role_permissions {
        string role_id PK
        string permission_id PK
    }

    user_workspaces ||--o{ roles : "contains"
    roles ||--o{ role_permissions : "has"
    permissions ||--o{ role_permissions : "granted_to"

    workspace_members {
        string workspace_id PK
        string user_id PK
        datetime joined_at
    }

    user_workspace_roles {
        string workspace_id PK
        string user_id PK
        string role_id PK
    }

    app_projects {
        string id PK
        string workspace_id FK
        string name
        text description
        enum status
        date deadline
        datetime created_at
        datetime updated_at
    }

    project_members {
        string project_id PK
        string user_id PK
        datetime joined_at
    }

    user_project_roles {
        string project_id PK
        string user_id PK
        string role_id PK
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

    project_tasks {
        string id PK
        string project_id FK
        string title
        text description
        enum status
        enum priority
        string assignee_id FK
        string reporter_id FK
        date due_date
        datetime created_at
        datetime updated_at
    }

    project_comments {
        string id PK
        string task_id FK
        string user_id FK
        text content
        datetime created_at
    }

    project_labels {
        string id PK
        string project_id FK
        string name
        string color
    }

    project_task_labels {
        string task_id PK
        string label_id PK
    }

    project_activity_log {
        string id PK
        string task_id FK
        string user_id FK
        string action
        json details
        datetime created_at
    }

    user_notifications {
        string id PK
        string user_id FK
        string content
        boolean is_read
        datetime created_at
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

    storage_nodes {
        string id PK
        string workspace_id FK
        string project_id FK
        string parent_id FK
        string name
        enum type
        string mime_type
        bigint size_bytes
        string created_by FK
        string updated_by FK
        datetime created_at
        datetime updated_at
    }

    storage_tags {
        string id PK
        string workspace_id FK
        string name
        string color
    }

    storage_tag_rbac {
        string tag_id PK
        string role_id PK
        boolean can_read
        boolean can_write
        boolean can_delete
    }

    storage_node_tags {
        string node_id PK
        string tag_id PK
    }

    storage_file_versions {
        string id PK
        string node_id FK
        string file_path
        int version_number
        bigint size_bytes
        string uploaded_by FK
        datetime created_at
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

    chat_rooms {
        string id PK
        string workspace_id FK
        string project_id FK
        string name
        enum type
        boolean is_private
        string created_by FK
        datetime created_at
    }

    e2e_user_keys {
        string id PK
        string user_id FK
        string device_id
        text identity_key
        text signed_prekey
        text prekey_signature
    }

    e2e_room_keys {
        string room_id PK
        string user_id PK
        int key_version PK
        text encrypted_key
    }

    e2e_chat_messages {
        string id PK
        string room_id FK
        string sender_id FK
        string sender_device_id
        string parent_id FK
        text ciphertext
        string nonce_or_iv
        int key_version
        datetime created_at
    }

    chat_room_members {
        string room_id PK
        string user_id PK
        string last_read_message_id FK
    }

    chat_message_attachments {
        string id PK
        string message_id FK
        string storage_node_id FK
        string file_url
        string file_name
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

    calendars {
        string id PK
        string workspace_id FK
        string project_id FK
        string user_id FK
        string name
        string color
        enum type
    }

    calendar_events {
        string id PK
        string calendar_id FK
        string task_id FK
        string title
        text description
        datetime start_time
        datetime end_time
        boolean is_all_day
        string created_by FK
    }

    user_workspaces ||--o{ calendars : "owns_cal"
    app_projects ||--o{ calendars : "owns_cal"
    app_users ||--o{ calendars : "owns_cal"

    calendars ||--o{ calendar_events : "contains"
    project_tasks ||--o{ calendar_events : "linked_to_task"
    app_users ||--o{ calendar_events : "created_event"

    user_sessions {
        string id PK
        string user_id FK
        string refresh_token_hash UK
        string replaced_by_session_id FK
        string device_info
        string ip_address
        boolean is_revoked
        datetime expires_at
    }

    jwt_blacklisted_tokens {
        string id PK
        string jti UK
        string user_id FK
        datetime expires_at
    }

    app_users ||--o{ user_sessions : "has_sessions"
    user_sessions ||--o{ user_sessions : "rotated_to"
    app_users ||--o{ jwt_blacklisted_tokens : "blacklisted"