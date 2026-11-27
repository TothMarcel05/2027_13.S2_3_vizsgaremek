export type ID = string

export type WorkspaceRole = 'owner' | 'admin' | 'member' | 'viewer'
export type SystemRole = 'super_admin' | 'support' | 'user'
export type TwoFactorMethod = 'totp' | 'email' | null

export interface User {
  id: ID
  name: string
  email: string
  title: string
  bio: string
  hue: number
  systemRole: SystemRole
  active: boolean
  twoFactor: TwoFactorMethod
  createdAt: string
  lastSeen: string
}

export interface Session {
  id: ID
  device: string
  location: string
  lastActive: string
  current: boolean
}

export interface Workspace {
  id: ID
  name: string
  plan: string
  hue: number
  ownerId: ID
  storageUsed: number
  storageLimit: number
}

export interface Member {
  userId: ID
  role: WorkspaceRole
  joinedAt: string
  user: User
}

export interface Invitation {
  id: ID
  kind: 'workspace' | 'project'
  targetName: string
  workspaceId: ID
  email: string
  role: WorkspaceRole
  invitedBy: string
  sentAt: string
  direction: 'incoming' | 'outgoing'
}

export type ProjectStatus = 'planning' | 'active' | 'on_hold' | 'completed'

export interface Project {
  id: ID
  workspaceId: ID
  name: string
  description: string
  status: ProjectStatus
  hue: number
  dueDate: string
  memberIds: ID[]
  taskCount: number
  doneCount: number
}

export type TaskStatus = 'todo' | 'in_progress' | 'review' | 'done'
export type Priority = 'low' | 'medium' | 'high' | 'urgent'

export interface Task {
  id: ID
  workspaceId: ID
  projectId: ID
  title: string
  description: string
  status: TaskStatus
  priority: Priority
  dueDate: string | null
  assigneeId: ID | null
  reservedById: ID | null
  tags: string[]
}

export type ChannelKind = 'project_channel' | 'group' | 'direct'

export interface Channel {
  id: ID
  workspaceId: ID
  kind: ChannelKind
  name: string
  topic: string
  memberIds: ID[]
  unread: number
  lastMessage: string
  lastAt: string
}

export interface Attachment {
  name: string
  size: number
  type: string
  url?: string
}

export interface Message {
  id: ID
  channelId: ID
  authorId: ID
  body: string
  createdAt: string
  attachments: Attachment[]
}

export interface CalendarEvent {
  id: ID
  workspaceId: ID
  projectId: ID | null
  title: string
  description: string
  start: string
  end: string
  location: string
}

export type ShareRole = 'viewer' | 'commenter' | 'editor'

export interface Share {
  userId: ID
  role: ShareRole
}

export interface FileNode {
  id: ID
  workspaceId: ID
  parentId: ID | null
  kind: 'folder' | 'file'
  name: string
  size: number
  mime: string
  tags: string[]
  ownerId: ID
  updatedAt: string
  shares: Share[]
}

export interface Notification {
  id: ID
  title: string
  body: string
  type: 'task' | 'mention' | 'invite' | 'file' | 'system'
  createdAt: string
  read: boolean
  link: string
}

export interface TrashItem {
  id: ID
  workspaceId: ID
  type: 'project' | 'task' | 'file' | 'folder' | 'event'
  name: string
  deletedAt: string
  deletedBy: ID
}

export interface Permission {
  key: string
  label: string
  group: string
}

export interface Role {
  id: ID
  name: string
  description: string
  locked: boolean
  permissions: string[]
}

export interface Paged<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface DashboardData {
  projects: Project[]
  tasks: Task[]
  events: CalendarEvent[]
  storage: { used: number; limit: number; breakdown: { label: string; bytes: number }[] }
}
