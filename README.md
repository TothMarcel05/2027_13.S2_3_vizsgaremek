erDiagram
    USERS {
        int id PK
        string name
        string email
        string password_hash
        string avatar_url
        datetime created_at
    }

    WORKSPACES {
        int id PK
        string name
        int owner_id FK
        datetime created_at
    }

    WORKSPACE_MEMBERS {
        int workspace_id PK, FK
        int user_id PK, FK
        string role
    }

    PROJECTS {
        int id PK
        int workspace_id FK
        string name
        string description
        string status
        date deadline
        datetime created_at
    }

    PROJECT_MEMBERS {
        int project_id PK, FK
        int user_id PK, FK
        string role
    }

    TASKS {
        int id PK
        int project_id FK
        string title
        string description
        string status
        string priority
        int assignee_id FK
        int reporter_id FK
        date due_date
        datetime created_at
        datetime updated_at
    }

    SUBTASKS {
        int id PK
        int parent_task_id FK
        int child_task_id FK
        string type
    }

    COMMENTS {
        int id PK
        int task_id FK
        int user_id FK
        string content
        datetime created_at
    }

    ATTACHMENTS {
        int id PK
        int task_id FK
        string file_url
        int uploaded_by FK
        datetime created_at
    }

    LABELS {
        int id PK
        int project_id FK
        string name
        string color
    }

    TASK_LABELS {
        int task_id PK, FK
        int label_id PK, FK
    }

    ACTIVITY_LOG {
        int id PK
        int task_id FK
        int user_id FK
        string action
        datetime created_at
    }

    NOTIFICATIONS {
        int id PK
        int user_id FK
        string content
        boolean is_read
        datetime created_at
    }

    %% Kapcsolatok definíciója
    USERS ||--o{ WORKSPACES : "owns"
    USERS ||--o{ WORKSPACE_MEMBERS : "is member of"
    WORKSPACES ||--o{ WORKSPACE_MEMBERS : "has members"
    
    WORKSPACES ||--o{ PROJECTS : "contains"
    PROJECTS ||--o{ PROJECT_MEMBERS : "has team"
    USERS ||--o{ PROJECT_MEMBERS : "works on"
    
    PROJECTS ||--o{ TASKS : "has"
    USERS ||--o{ TASKS : "assigned to"
    USERS ||--o{ TASKS : "reported by"
    
    TASKS ||--o{ SUBTASKS : "parent"
    TASKS ||--o{ SUBTASKS : "child"
    
    TASKS ||--o{ COMMENTS : "has"
    USERS ||--o{ COMMENTS : "wrote"
    
    TASKS ||--o{ ATTACHMENTS : "contains"
    USERS ||--o{ ATTACHMENTS : "uploaded"
    
    PROJECTS ||--o{ LABELS : "defines"
    TASKS ||--o{ TASK_LABELS : "has label"
    LABELS ||--o{ TASK_LABELS : "applied to"
    
    TASKS ||--o{ ACTIVITY_LOG : "tracked"
    USERS ||--o{ ACTIVITY_LOG : "performed"
    
    USERS ||--o{ NOTIFICATIONS : "receives"
