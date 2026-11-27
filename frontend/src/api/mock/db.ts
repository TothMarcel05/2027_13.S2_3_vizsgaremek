/** Seed data. Dates are relative to "now" so the demo never goes stale. */
import type * as T from '../types'

const DAY = 86_400_000
const ago = (days: number, hours = 0) => new Date(Date.now() - days * DAY - hours * 3_600_000).toISOString()
const at = (dayOffset: number, hour = 9, minute = 0) => {
  const d = new Date()
  d.setDate(d.getDate() + dayOffset)
  d.setHours(hour, minute, 0, 0)
  return d.toISOString()
}

export const ME = 'u1'

function user(
  id: string, name: string, title: string, hue: number,
  systemRole: T.SystemRole = 'user', active = true, twoFactor: T.TwoFactorMethod = null,
): T.User {
  const email = `${name.toLowerCase().replace(/[^a-z ]/g, '').replace(/ /g, '.')}@example.com`
  const n = Number(id.slice(1))
  return { id, name, email, title, bio: '', hue, systemRole, active, twoFactor, createdAt: ago(400 - n * 11), lastSeen: ago(n % 6, n) }
}

const users: T.User[] = [
  { ...user('u1', 'Elena Marsh', 'Product Design Lead', 22, 'super_admin', true, 'totp'), bio: 'Designing calm tools for busy teams.', lastSeen: ago(0) },
  user('u2', 'Jonas Weber', 'Frontend Engineer', 150, 'support'),
  user('u3', 'Amara Okafor', 'Project Manager', 40),
  user('u4', 'Theo Lindqvist', 'Backend Engineer', 205),
  user('u5', 'Priya Raman', 'Brand Designer', 340),
  user('u6', 'Mateo Silva', 'QA Specialist', 95),
  user('u7', 'Hana Sato', 'Content Strategist', 265),
  user('u8', 'Oliver Grant', 'Marketing', 12, 'user', false),
  user('u9', 'Sofia Rossi', 'Data Analyst', 175),
  user('u10', 'Noah Becker', 'DevOps', 220),
  user('u11', 'Lina Haddad', 'UX Researcher', 30),
  user('u12', 'Victor Chen', 'Finance', 120, 'user', false),
  user('u13', 'Ines Duarte', 'Illustrator', 300),
  user('u14', 'Arjun Mehta', 'Mobile Engineer', 55),
  user('u15', 'Freya Nilsen', 'Customer Success', 190, 'support'),
  user('u16', 'Lucas Moreau', 'Sales', 8),
  user('u17', 'Zofia Nowak', 'People Ops', 280),
  user('u18', 'Kwame Mensah', 'Security', 140),
  user('u19', 'Clara Jensen', 'Copywriter', 45),
  user('u20', 'Dmitri Volkov', 'Architect', 230),
  user('u21', 'Maya Cohen', 'Motion Designer', 325),
  user('u22', 'Rafael Ortiz', 'Intern', 70, 'user', false),
  user('u23', 'Astrid Holm', 'Legal', 160),
]

const workspaces: T.Workspace[] = [
  { id: 'w1', name: 'Fieldnote Studio', plan: 'Team', hue: 22, ownerId: 'u1', storageUsed: 68.4e9, storageLimit: 100e9 },
  { id: 'w2', name: 'Northwind Labs', plan: 'Business', hue: 150, ownerId: 'u4', storageUsed: 212e9, storageLimit: 250e9 },
]

const member = (workspaceId: string, userId: string, role: T.WorkspaceRole, days: number) => ({ workspaceId, userId, role, joinedAt: ago(days) })
const members = [
  member('w1', 'u1', 'owner', 380), member('w1', 'u2', 'admin', 300), member('w1', 'u3', 'admin', 260),
  member('w1', 'u4', 'member', 210), member('w1', 'u5', 'member', 180), member('w1', 'u6', 'member', 90),
  member('w1', 'u7', 'viewer', 30),
  member('w2', 'u4', 'owner', 500), member('w2', 'u1', 'admin', 200), member('w2', 'u9', 'member', 120), member('w2', 'u10', 'member', 60),
]

type ProjectRow = Omit<T.Project, 'taskCount' | 'doneCount'>
const project = (id: string, workspaceId: string, name: string, status: T.ProjectStatus, hue: number, due: number, memberIds: string[], description: string): ProjectRow =>
  ({ id, workspaceId, name, status, hue, dueDate: at(due), memberIds, description })

const projects: ProjectRow[] = [
  project('p1', 'w1', 'Website Redesign', 'active', 22, 24, ['u1', 'u2', 'u5', 'u7'], 'A quieter, faster marketing site with a new editorial system.'),
  project('p2', 'w1', 'Mobile Companion App', 'active', 150, 51, ['u1', 'u2', 'u4', 'u6'], 'Capture, triage and review tasks on the go.'),
  project('p3', 'w1', 'Brand Guidelines 2.0', 'planning', 40, 70, ['u1', 'u5', 'u7'], 'Typography, voice and illustration principles.'),
  project('p4', 'w1', 'Customer Research', 'on_hold', 265, 33, ['u3', 'u7'], 'Interview round with twelve long-term customers.'),
  project('p5', 'w1', 'Onboarding Revamp', 'completed', 95, -9, ['u1', 'u3', 'u4'], 'Shorter first-run flow with guided setup.'),
  project('p6', 'w2', 'Data Platform', 'active', 205, 40, ['u1', 'u4', 'u9', 'u10'], 'Unified event pipeline and warehouse.'),
  project('p7', 'w2', 'Infra Hardening', 'planning', 12, 62, ['u4', 'u10'], 'Least-privilege access and backup drills.'),
]

let taskSeq = 0
const task = (
  projectId: string, title: string, status: T.TaskStatus, priority: T.Priority,
  due: number | null, assigneeId: string | null, tags: string[] = [], reservedById: string | null = null,
): T.Task => ({
  id: `t${++taskSeq}`, projectId, workspaceId: projects.find((p) => p.id === projectId)!.workspaceId,
  title, status, priority, dueDate: due === null ? null : at(due, 17), assigneeId, reservedById, tags,
  description: `${title}. Outline the approach, share a first pass with the team, and note open questions before moving it to review.`,
})

const tasks: T.Task[] = [
  task('p1', 'Audit current page performance', 'done', 'medium', -6, 'u2', ['research']),
  task('p1', 'Draft new homepage narrative', 'review', 'high', 1, 'u7', ['content']),
  task('p1', 'Design the editorial grid', 'in_progress', 'high', 3, 'u1', ['design']),
  task('p1', 'Build pricing page components', 'in_progress', 'medium', 6, 'u2', ['frontend']),
  task('p1', 'Art-direct case study imagery', 'todo', 'medium', 9, null, ['design']),
  task('p1', 'Set up redirect map', 'todo', 'low', 15, null, ['seo']),
  task('p1', 'Fix contrast issues on buttons', 'todo', 'urgent', 0, null, ['a11y']),
  task('p1', 'Write migration checklist', 'todo', 'low', 18, 'u1', ['ops'], 'u1'),
  task('p2', 'Sketch quick-capture flow', 'done', 'medium', -3, 'u1', ['design']),
  task('p2', 'Offline sync prototype', 'in_progress', 'urgent', 2, 'u4', ['mobile']),
  task('p2', 'Push notification settings', 'todo', 'medium', 12, null, ['mobile']),
  task('p2', 'Test plan for beta', 'todo', 'high', 14, 'u6', ['qa']),
  task('p2', 'Review onboarding copy', 'review', 'low', 4, 'u1', ['content']),
  task('p3', 'Collect type specimens', 'todo', 'low', 20, null, ['design']),
  task('p3', 'Define voice principles', 'in_progress', 'medium', 11, 'u5', ['brand']),
  task('p4', 'Recruit interview participants', 'review', 'medium', 8, 'u3', ['research']),
  task('p4', 'Prepare discussion guide', 'todo', 'medium', 10, null, ['research']),
  task('p5', 'Ship guided setup', 'done', 'high', -12, 'u4', ['frontend']),
  task('p5', 'Measure activation lift', 'done', 'medium', -10, 'u3', ['analytics']),
  task('p6', 'Model core event schema', 'in_progress', 'high', 5, 'u9', ['data']),
  task('p6', 'Dashboard wireframes', 'todo', 'medium', 13, 'u1', ['design']),
  task('p6', 'Backfill historic events', 'todo', 'low', 25, null, ['data']),
  task('p7', 'Rotate service credentials', 'todo', 'urgent', 7, null, ['security']),
]

const channel = (id: string, workspaceId: string, kind: T.ChannelKind, name: string, topic: string, memberIds: string[], unread = 0) =>
  ({ id, workspaceId, kind, name, topic, memberIds, unread })
const channels = [
  channel('c1', 'w1', 'project_channel', 'website-redesign', 'Everything for the new site', ['u1', 'u2', 'u5', 'u7']),
  channel('c2', 'w1', 'project_channel', 'mobile-app', 'Companion app build', ['u1', 'u2', 'u4', 'u6'], 2),
  channel('c3', 'w1', 'project_channel', 'brand-2-0', 'Guidelines refresh', ['u1', 'u5', 'u7']),
  channel('c4', 'w1', 'group', 'Design Critique', 'Thursdays, bring work in progress', ['u1', 'u5', 'u7'], 1),
  channel('c5', 'w1', 'group', 'Coffee Corner', 'Off-topic and good links', ['u1', 'u2', 'u3', 'u4', 'u5']),
  channel('c6', 'w1', 'direct', 'u2', '', ['u1', 'u2']),
  channel('c7', 'w1', 'direct', 'u3', '', ['u1', 'u3'], 3),
  channel('c8', 'w1', 'direct', 'u5', '', ['u1', 'u5']),
  channel('c9', 'w2', 'project_channel', 'data-platform', 'Pipeline and warehouse', ['u1', 'u4', 'u9', 'u10'], 1),
  channel('c10', 'w2', 'direct', 'u4', '', ['u1', 'u4']),
]

let msgSeq = 0
const msg = (channelId: string, authorId: string, body: string, hoursAgo: number, attachments: T.Attachment[] = []): T.Message =>
  ({ id: `m${++msgSeq}`, channelId, authorId, body, createdAt: ago(0, hoursAgo), attachments })
const messages: T.Message[] = [
  msg('c1', 'u7', 'First pass of the homepage story is up. I kept it to three beats.', 27),
  msg('c1', 'u2', 'Reads really well. The second section could lose a paragraph.', 26),
  msg('c1', 'u5', 'Here are the grid explorations for tomorrow.', 5, [
    { name: 'grid-explorations.fig', size: 4.2e6, type: 'application/octet-stream' },
    { name: 'moodboard.png', size: 1.8e6, type: 'image/png' },
  ]),
  msg('c1', 'u1', 'Love option B. Let’s test it with real copy.', 4),
  msg('c1', 'u2', 'On it. I’ll have a preview link this afternoon.', 3),
  msg('c2', 'u4', 'Offline sync works for create and edit. Deletes are next.', 8),
  msg('c2', 'u6', 'I’ll start writing conflict cases today.', 2),
  msg('c3', 'u5', 'Collecting specimens in the shared folder.', 50),
  msg('c4', 'u5', 'Anyone have work to show this week?', 20),
  msg('c5', 'u3', 'The new roaster downstairs is excellent.', 30),
  msg('c6', 'u2', 'Can you look at the pricing table spacing?', 6),
  msg('c7', 'u3', 'Timeline review moved to Friday.', 7),
  msg('c7', 'u3', 'Could you send the updated estimate?', 6),
  msg('c7', 'u3', 'No rush, end of day is fine.', 6),
  msg('c8', 'u5', 'Thanks for the feedback on the illustrations!', 48),
  msg('c9', 'u9', 'Event schema draft is ready for comments.', 9, [{ name: 'event-schema.json', size: 18_400, type: 'application/json' }]),
  msg('c10', 'u4', 'Welcome to Northwind!', 200),
]

const event = (id: string, workspaceId: string, projectId: string | null, title: string, day: number, hour: number, length: number, location = ''): T.CalendarEvent =>
  ({ id, workspaceId, projectId, title, description: '', location, start: at(day, hour), end: at(day, hour + length) })
const events: T.CalendarEvent[] = [
  event('e1', 'w1', 'p1', 'Design review', 0, 14, 1, 'Studio room'),
  event('e2', 'w1', null, 'Team standup', 1, 9, 1, 'Video call'),
  event('e3', 'w1', 'p2', 'Beta planning', 2, 11, 2),
  event('e4', 'w1', 'p1', 'Content freeze', 5, 17, 1),
  event('e5', 'w1', 'p3', 'Brand workshop', 7, 10, 3, 'Library'),
  event('e6', 'w1', 'p4', 'Interview: Atlas Co.', 9, 15, 1, 'Video call'),
  event('e7', 'w1', null, 'Monthly retro', 12, 16, 1),
  event('e8', 'w1', 'p1', 'Launch rehearsal', 18, 13, 2),
  event('e9', 'w1', 'p5', 'Onboarding wrap-up', -8, 11, 1),
  event('e10', 'w1', 'p2', 'Sprint demo', -2, 15, 1),
  event('e11', 'w1', null, 'Portfolio hour', 1, 13, 1),
  event('e12', 'w2', 'p6', 'Schema sign-off', 3, 10, 1),
  event('e13', 'w2', 'p7', 'Backup drill', 8, 9, 3),
]

const node = (
  id: string, workspaceId: string, parentId: string | null, kind: 'file' | 'folder', name: string,
  size = 0, mime = '', tags: string[] = [], days = 3, ownerId = ME, shares: T.Share[] = [],
): T.FileNode => ({ id, workspaceId, parentId, kind, name, size, mime, tags, ownerId, updatedAt: ago(days), shares })
const files: T.FileNode[] = [
  node('f1', 'w1', null, 'folder', 'Design', 0, '', ['design'], 1),
  node('f2', 'w1', null, 'folder', 'Research', 0, '', ['research'], 6, 'u3'),
  node('f3', 'w1', null, 'folder', 'Contracts', 0, '', ['legal'], 20),
  node('f4', 'w1', null, 'file', 'Q4-roadmap.pdf', 2.4e6, 'application/pdf', ['planning'], 2, 'u3', [{ userId: 'u2', role: 'viewer' }]),
  node('f5', 'w1', null, 'file', 'team-offsite.jpg', 5.8e6, 'image/jpeg', ['culture'], 12, 'u5'),
  node('f6', 'w1', null, 'file', 'budget-forecast.xlsx', 640_000, 'application/vnd.ms-excel', ['planning', 'finance'], 4),
  node('f7', 'w1', 'f1', 'file', 'homepage-v3.fig', 18.2e6, 'application/octet-stream', ['design'], 1, 'u5', [{ userId: 'u1', role: 'editor' }]),
  node('f8', 'w1', 'f1', 'file', 'moodboard.png', 1.8e6, 'image/png', ['design', 'brand'], 2, 'u5'),
  node('f9', 'w1', 'f1', 'folder', 'Icons', 0, '', ['design'], 15),
  node('f10', 'w1', 'f9', 'file', 'icon-set.zip', 3.1e6, 'application/zip', ['design'], 15),
  node('f11', 'w1', 'f2', 'file', 'interview-notes.docx', 88_000, 'application/msword', ['research'], 6, 'u7'),
  node('f12', 'w1', 'f3', 'file', 'vendor-agreement.pdf', 410_000, 'application/pdf', ['legal'], 20),
  node('f13', 'w2', null, 'folder', 'Schemas', 0, '', ['data'], 2, 'u9'),
  node('f14', 'w2', null, 'file', 'architecture.pdf', 3.3e6, 'application/pdf', ['data', 'planning'], 5, 'u4'),
]

const note = (id: string, type: T.Notification['type'], title: string, body: string, hours: number, read: boolean, link: string): T.Notification =>
  ({ id, type, title, body, createdAt: ago(0, hours), read, link })
const notifications: T.Notification[] = [
  note('n1', 'mention', 'Jonas mentioned you', '“Can you look at the pricing table spacing?”', 1, false, '/chat'),
  note('n2', 'task', 'Task due today', 'Fix contrast issues on buttons is due today and unclaimed.', 3, false, '/projects/p1'),
  note('n3', 'invite', 'Workspace invitation', 'Harbor & Pine invited you to join as a member.', 10, false, '/'),
  note('n4', 'file', 'File shared with you', 'Priya shared homepage-v3.fig with edit access.', 26, true, '/files'),
  note('n5', 'system', 'New sign-in', 'A new session started from Chrome on Windows.', 50, true, '/profile'),
]

const invitations: T.Invitation[] = [
  { id: 'i1', kind: 'workspace', targetName: 'Harbor & Pine', workspaceId: 'w3', email: users[0].email, role: 'member', invitedBy: 'Lina Haddad', sentAt: ago(0, 10), direction: 'incoming' },
  { id: 'i2', kind: 'project', targetName: 'Annual Report', workspaceId: 'w2', email: users[0].email, role: 'viewer', invitedBy: 'Theo Lindqvist', sentAt: ago(2), direction: 'incoming' },
  { id: 'i3', kind: 'workspace', targetName: 'Fieldnote Studio', workspaceId: 'w1', email: 'new.hire@example.com', role: 'member', invitedBy: 'Elena Marsh', sentAt: ago(3), direction: 'outgoing' },
  { id: 'i4', kind: 'workspace', targetName: 'Fieldnote Studio', workspaceId: 'w1', email: 'freelancer@example.com', role: 'viewer', invitedBy: 'Amara Okafor', sentAt: ago(9), direction: 'outgoing' },
]

type Trashed = T.TrashItem & { payload: unknown }
const trashed = (id: string, type: T.TrashItem['type'], name: string, days: number, payload: unknown): Trashed =>
  ({ id, workspaceId: 'w1', type, name, deletedAt: ago(days), deletedBy: ME, payload })
const trash: Trashed[] = [
  trashed('x1', 'file', 'old-logo.svg', 2, { files: [node('f90', 'w1', null, 'file', 'old-logo.svg', 24_000, 'image/svg+xml', ['brand'])] }),
  trashed('x2', 'task', 'Explore dark theme', 14, { tasks: [task('p1', 'Explore dark theme', 'todo', 'low', 30, null)] }),
  trashed('x3', 'folder', 'Archive 2024', 27, { files: [node('f91', 'w1', null, 'folder', 'Archive 2024')] }),
  trashed('x4', 'event', 'Cancelled vendor call', 21, { event: event('e90', 'w1', null, 'Cancelled vendor call', 4, 10, 1) }),
]

const perm = (key: string, label: string, group: string): T.Permission => ({ key, label, group })
const permissions: T.Permission[] = [
  perm('users.read', 'View users', 'Users'), perm('users.write', 'Edit users', 'Users'), perm('users.suspend', 'Suspend users', 'Users'),
  perm('workspaces.read', 'View workspaces', 'Workspaces'), perm('workspaces.manage', 'Manage workspaces', 'Workspaces'), perm('workspaces.delete', 'Delete workspaces', 'Workspaces'),
  perm('billing.read', 'View billing', 'Billing'), perm('billing.manage', 'Manage billing', 'Billing'),
  perm('roles.manage', 'Edit roles & permissions', 'System'), perm('audit.read', 'Read audit log', 'System'), perm('system.settings', 'Change system settings', 'System'),
]
const roles: T.Role[] = [
  { id: 'super_admin', name: 'Super admin', description: 'Full control of the platform', locked: true, permissions: permissions.map((p) => p.key) },
  { id: 'support', name: 'Support', description: 'Helps customers, limited writes', locked: false, permissions: ['users.read', 'users.write', 'workspaces.read', 'audit.read'] },
  { id: 'user', name: 'User', description: 'Standard account', locked: false, permissions: ['workspaces.read'] },
]

const sessions: T.Session[] = [
  { id: 's1', device: 'Chrome on Windows', location: 'Budapest, HU', lastActive: ago(0), current: true },
  { id: 's2', device: 'Safari on iPhone', location: 'Budapest, HU', lastActive: ago(0, 9), current: false },
  { id: 's3', device: 'Firefox on macOS', location: 'Vienna, AT', lastActive: ago(6), current: false },
]

export function seed() {
  return structuredClone({
    users, workspaces, members, projects, tasks, channels, messages, events, files, notifications, invitations, trash, permissions, roles, sessions,
  })
}
export type Db = ReturnType<typeof seed>
