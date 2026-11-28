/** In-browser stand-in for the REST API. The route table doubles as the backend contract. */
import { ApiError } from '../client'
import type * as T from '../types'
import { ME, seed, type Db } from './db'
import { registerAuthRoutes } from './auth'

// Persisted per tab so edits survive reloads.
const KEY = 'ws.mockdb.v1'
let db: Db = load()
function load(): Db {
  try {
    const raw = sessionStorage.getItem(KEY)
    if (raw) return JSON.parse(raw)
  } catch { /* storage unavailable: fall back to a fresh seed */ }
  return seed()
}
function save() {
  try { sessionStorage.setItem(KEY, JSON.stringify(db)) } catch { /* ignore quota errors */ }
}

const blobs = new Map<string, Blob>()
const uid = (p: string) => `${p}${Math.random().toString(36).slice(2, 9)}`
const now = () => new Date().toISOString()

function find<X extends { id: string }>(list: X[], id: string): X {
  const item = list.find((x) => x.id === id)
  if (!item) throw new ApiError(404, 'Not found')
  return item
}
const without = <X extends { id: string }>(list: X[], id: string) => list.filter((x) => x.id !== id)
const me = () => find(db.users, ME)
const fail = (message: string, status = 400): never => { throw new ApiError(status, message) }

const withCounts = (p: Db['projects'][number]): T.Project => {
  const mine = db.tasks.filter((t) => t.projectId === p.id)
  return { ...p, taskCount: mine.length, doneCount: mine.filter((t) => t.status === 'done').length }
}
const channelView = (c: Db['channels'][number]): T.Channel => {
  const last = db.messages.filter((m) => m.channelId === c.id).at(-1)
  return {
    ...c,
    name: c.kind === 'direct' ? find(db.users, c.name).name : c.name,
    lastMessage: last ? last.body || last.attachments[0]?.name || '' : 'No messages yet',
    lastAt: last?.createdAt ?? '',
  }
}
const toTrash = (workspaceId: string, type: T.TrashItem['type'], name: string, payload: unknown) =>
  db.trash.unshift({ id: uid('x'), workspaceId, type, name, deletedAt: now(), deletedBy: ME, payload })

/* ---------- live chat ---------- */
type LiveEvent = { type: 'typing' | 'message'; userId: string }
const listeners = new Map<string, Set<(e: LiveEvent) => void>>()
export function subscribeMock(channelId: string, fn: (e: LiveEvent) => void) {
  const set = listeners.get(channelId) ?? new Set()
  set.add(fn)
  listeners.set(channelId, set)
  return () => { set.delete(fn) }
}
const REPLIES = [
  'Sounds good to me.', 'Nice, I’ll take a look this afternoon.', 'Agreed. Let’s keep it simple.',
  'Can you drop that in the shared folder?', 'Great work, this is much calmer.', 'On it 👍',
]
function simulateReply(channelId: string) {
  const channel = find(db.channels, channelId)
  const others = channel.memberIds.filter((id) => id !== ME)
  const userId = others[Math.floor(Math.random() * others.length)]
  if (!userId) return
  const emit = (e: LiveEvent) => listeners.get(channelId)?.forEach((fn) => fn(e))
  setTimeout(() => emit({ type: 'typing', userId }), 900)
  setTimeout(() => {
    db.messages.push({ id: uid('m'), channelId, authorId: userId, body: REPLIES[Math.floor(Math.random() * REPLIES.length)], createdAt: now(), attachments: [] })
    save()
    emit({ type: 'message', userId })
  }, 2800)
}

/* ---------- routes ---------- */
type Ctx = { p: string[]; q: URLSearchParams; body: any }
type Handler = (ctx: Ctx) => unknown
const routes: [string, RegExp, Handler][] = []
const on = (method: string, pattern: string, handler: Handler) =>
  routes.push([method, new RegExp(`^${pattern.replace(/:\w+/g, '([^/]+)')}$`), handler])

/* auth + own profile */
registerAuthRoutes(
  (method, pattern, handler) => on(method, pattern, ({ p, q, body }) => handler({ params: p, query: q, body })),
  me,
  (email) => db.users.some((u) => u.email === email && u.id !== ME),
)

/* workspaces */
on('GET', '/workspaces', () => db.workspaces.filter((w) => db.members.some((m) => m.workspaceId === w.id && m.userId === ME)))
on('POST', '/workspaces', ({ body }) => {
  if (!body.name?.trim()) fail('Give the workspace a name.')
  const w: T.Workspace = { id: uid('w'), name: body.name.trim(), plan: 'Free', hue: Math.floor(Math.random() * 360), ownerId: ME, storageUsed: 0, storageLimit: 5e9 }
  db.workspaces.push(w)
  db.members.push({ workspaceId: w.id, userId: ME, role: 'owner', joinedAt: now() })
  return w
})
on('PATCH', '/workspaces/:ws', ({ p, body }) => Object.assign(find(db.workspaces, p[0]), body))
on('DELETE', '/workspaces/:ws', ({ p }) => {
  if (find(db.workspaces, p[0]).ownerId !== ME) fail('Only the owner can delete this workspace.', 403)
  // Its contents stay, as with the backend's soft delete.
  db.workspaces = without(db.workspaces, p[0])
  db.members = db.members.filter((m) => m.workspaceId !== p[0])
})
on('GET', '/workspaces/:ws/dashboard', ({ p }) => {
  const w = find(db.workspaces, p[0])
  const split = [['Design files', 0.46], ['Documents', 0.21], ['Media', 0.24], ['Other', 0.09]] as const
  return {
    projects: db.projects.filter((x) => x.workspaceId === w.id && x.status === 'active').map(withCounts),
    tasks: db.tasks
      .filter((t) => t.workspaceId === w.id && t.status !== 'done' && (t.assigneeId === ME || t.reservedById === ME))
      .sort((a, b) => (a.dueDate ?? '9').localeCompare(b.dueDate ?? '9')),
    events: db.events.filter((e) => e.workspaceId === w.id && e.end >= now()).sort((a, b) => a.start.localeCompare(b.start)).slice(0, 5),
    storage: { used: w.storageUsed, limit: w.storageLimit, breakdown: split.map(([label, f]) => ({ label, bytes: w.storageUsed * f })) },
  } satisfies T.DashboardData
})
on('GET', '/workspaces/:ws/members', ({ p }) =>
  db.members.filter((m) => m.workspaceId === p[0]).map((m) => ({ ...m, user: find(db.users, m.userId) })))
on('PATCH', '/workspaces/:ws/members/:user', ({ p, body }) => {
  const m = db.members.find((x) => x.workspaceId === p[0] && x.userId === p[1])
  if (!m) fail('Member not found', 404)
  else if (m.role === 'owner') fail('Transfer ownership to change the owner’s role.')
  else m.role = body.role
})
on('DELETE', '/workspaces/:ws/members/:user', ({ p }) => {
  db.members = db.members.filter((m) => !(m.workspaceId === p[0] && m.userId === p[1] && m.role !== 'owner'))
})
on('POST', '/workspaces/:ws/transfer-ownership', ({ p, body }) => {
  db.members.filter((m) => m.workspaceId === p[0]).forEach((m) => {
    if (m.role === 'owner') m.role = 'admin'
    if (m.userId === body.userId) m.role = 'owner'
  })
  find(db.workspaces, p[0]).ownerId = body.userId
})
on('GET', '/workspaces/:ws/invitations', ({ p }) => db.invitations.filter((i) => i.workspaceId === p[0] && i.direction === 'outgoing'))
on('POST', '/workspaces/:ws/invitations', ({ p, body }) => {
  if (!/^\S+@\S+\.\S+$/.test(body.email)) fail('Enter a valid email address.')
  if (db.invitations.some((i) => i.workspaceId === p[0] && i.email === body.email && i.direction === 'outgoing')) fail('That person already has a pending invitation.', 409)
  const inv: T.Invitation = { id: uid('i'), kind: 'workspace', targetName: find(db.workspaces, p[0]).name, workspaceId: p[0], email: body.email, role: body.role, invitedBy: me().name, sentAt: now(), direction: 'outgoing' }
  db.invitations.unshift(inv)
  return inv
})
on('GET', '/invitations/pending', () => db.invitations.filter((i) => i.direction === 'incoming'))
on('DELETE', '/invitations/:id', ({ p }) => { db.invitations = without(db.invitations, p[0]) })
on('POST', '/invitations/:id/accept', ({ p }) => {
  const inv = find(db.invitations, p[0])
  if (inv.kind === 'workspace') {
    if (!db.workspaces.some((w) => w.id === inv.workspaceId))
      db.workspaces.push({ id: inv.workspaceId, name: inv.targetName, plan: 'Team', hue: 190, ownerId: 'u11', storageUsed: 12e9, storageLimit: 50e9 })
    db.members.push({ workspaceId: inv.workspaceId, userId: ME, role: inv.role, joinedAt: now() })
  } else {
    db.projects.push({ id: uid('p'), workspaceId: inv.workspaceId, name: inv.targetName, description: 'Shared with you.', status: 'active', hue: 40, dueDate: new Date(Date.now() + 45 * 86_400_000).toISOString(), memberIds: [ME] })
  }
  db.invitations = without(db.invitations, p[0])
})
on('POST', '/invitations/:id/decline', ({ p }) => { db.invitations = without(db.invitations, p[0]) })

/* projects & tasks */
on('GET', '/workspaces/:ws/projects', ({ p }) => db.projects.filter((x) => x.workspaceId === p[0]).map(withCounts))
on('POST', '/workspaces/:ws/projects', ({ p, body }) => {
  if (!body.name?.trim()) fail('Give the project a name.')
  const row = { id: uid('p'), workspaceId: p[0], name: body.name.trim(), description: body.description ?? '', status: body.status ?? 'planning', hue: body.hue ?? 22, dueDate: body.dueDate || new Date(Date.now() + 30 * 86_400_000).toISOString(), memberIds: [ME] }
  db.projects.push(row)
  return withCounts(row)
})
on('GET', '/projects/:id', ({ p }) => withCounts(find(db.projects, p[0])))
on('PATCH', '/projects/:id', ({ p, body }) => withCounts(Object.assign(find(db.projects, p[0]), body)))
on('DELETE', '/projects/:id', ({ p }) => {
  const project = find(db.projects, p[0])
  toTrash(project.workspaceId, 'project', project.name, { project, tasks: db.tasks.filter((t) => t.projectId === p[0]) })
  db.projects = without(db.projects, p[0])
  db.tasks = db.tasks.filter((t) => t.projectId !== p[0])
})
on('GET', '/projects/:id/tasks', ({ p }) => db.tasks.filter((t) => t.projectId === p[0]))
on('POST', '/projects/:id/tasks', ({ p, body }) => {
  if (!body.title?.trim()) fail('Give the task a title.')
  const t: T.Task = { id: uid('t'), projectId: p[0], workspaceId: find(db.projects, p[0]).workspaceId, title: body.title.trim(), description: body.description ?? '', status: body.status ?? 'todo', priority: body.priority ?? 'medium', dueDate: body.dueDate || null, assigneeId: body.assigneeId || null, reservedById: null, tags: body.tags ?? [] }
  db.tasks.push(t)
  return t
})
on('PATCH', '/tasks/:id', ({ p, body }) => Object.assign(find(db.tasks, p[0]), body))
on('POST', '/tasks/:id/reserve', ({ p }) => {
  const t = find(db.tasks, p[0])
  if (t.reservedById && t.reservedById !== ME) fail('Someone else already reserved this task.', 409)
  if (t.assigneeId && t.assigneeId !== ME) fail('This task is assigned to a teammate.', 409)
  return Object.assign(t, { reservedById: ME, assigneeId: ME })
})
on('POST', '/tasks/:id/unreserve', ({ p }) => Object.assign(find(db.tasks, p[0]), { reservedById: null, assigneeId: null }))
on('DELETE', '/tasks/:id', ({ p }) => {
  const t = find(db.tasks, p[0])
  toTrash(t.workspaceId, 'task', t.title, { tasks: [t] })
  db.tasks = without(db.tasks, p[0])
})

/* chat */
on('GET', '/workspaces/:ws/channels', ({ p }) => db.channels.filter((c) => c.workspaceId === p[0]).map(channelView))
on('GET', '/channels/:id/messages', ({ p }) => db.messages.filter((m) => m.channelId === p[0]))
on('POST', '/channels/:id/messages', ({ p, body }) => {
  const m: T.Message = { id: uid('m'), channelId: p[0], authorId: ME, body: body.body, attachments: body.attachments ?? [], createdAt: now() }
  db.messages.push(m)
  simulateReply(p[0])
  return m
})
on('POST', '/channels/:id/read', ({ p }) => { find(db.channels, p[0]).unread = 0 })

/* calendar */
on('GET', '/workspaces/:ws/events', ({ p, q }) => {
  const from = q.get('from') ?? '', to = q.get('to') ?? '9999', project = q.get('projectId')
  return db.events.filter((e) => e.workspaceId === p[0] && e.end >= from && e.start <= to && (!project || e.projectId === project))
    .sort((a, b) => a.start.localeCompare(b.start))
})
const checkEvent = (e: Partial<T.CalendarEvent>) => {
  if (!e.title?.trim()) fail('Give the event a title.')
  if (!e.start || !e.end || e.end <= e.start) fail('The event needs to end after it starts.')
}
on('POST', '/workspaces/:ws/events', ({ p, body }) => {
  checkEvent(body)
  const e = { id: uid('e'), workspaceId: p[0], projectId: null, description: '', location: '', ...body } as T.CalendarEvent
  db.events.push(e)
  return e
})
on('PATCH', '/events/:id', ({ p, body }) => { const e = find(db.events, p[0]); checkEvent({ ...e, ...body }); return Object.assign(e, body) })
on('DELETE', '/events/:id', ({ p }) => {
  const e = find(db.events, p[0])
  toTrash(e.workspaceId, 'event', e.title, { event: e })
  db.events = without(db.events, p[0])
})

/* files */
on('GET', '/workspaces/:ws/files', ({ p }) => db.files.filter((f) => f.workspaceId === p[0]))
on('POST', '/workspaces/:ws/folders', ({ p, body }) => {
  const name = body.name?.trim() || fail('Name the folder.')
  if (db.files.some((f) => f.workspaceId === p[0] && f.parentId === body.parentId && f.name === name)) fail('A folder with that name already exists here.', 409)
  const f: T.FileNode = { id: uid('f'), workspaceId: p[0], parentId: body.parentId ?? null, kind: 'folder', name, size: 0, mime: '', tags: [], ownerId: ME, updatedAt: now(), shares: [] }
  db.files.push(f)
  return f
})
on('POST', '/workspaces/:ws/files', ({ p, body }) => {
  const file = body.file as File
  const w = find(db.workspaces, p[0])
  if (w.storageUsed + file.size > w.storageLimit) fail('This upload would exceed the storage limit.', 413)
  const f: T.FileNode = { id: uid('f'), workspaceId: p[0], parentId: body.parentId ?? null, kind: 'file', name: file.name, size: file.size, mime: file.type, tags: ['upload'], ownerId: ME, updatedAt: now(), shares: [] }
  blobs.set(f.id, file)
  db.files.push(f)
  w.storageUsed += file.size
  return f
})
on('PATCH', '/files/:id', ({ p, body }) => Object.assign(find(db.files, p[0]), body, { updatedAt: now() }))
on('PUT', '/files/:id/shares', ({ p, body }) => Object.assign(find(db.files, p[0]), { shares: body.shares }))
on('DELETE', '/files/:id', ({ p }) => {
  const root = find(db.files, p[0])
  const ids = new Set([root.id])
  for (let grew = true; grew; ) {
    grew = false
    db.files.forEach((f) => { if (f.parentId && ids.has(f.parentId) && !ids.has(f.id)) { ids.add(f.id); grew = true } })
  }
  toTrash(root.workspaceId, root.kind, root.name, { files: db.files.filter((f) => ids.has(f.id)) })
  db.files = db.files.filter((f) => !ids.has(f.id))
})
on('GET', '/files/:id/download', ({ p }) => {
  const f = find(db.files, p[0])
  return blobs.get(f.id) ?? new Blob([`Demo placeholder for "${f.name}".\nThe real file will come from the backend.\n`], { type: 'text/plain' })
})

/* notifications */
on('GET', '/notifications', () => db.notifications)
on('POST', '/notifications/read-all', () => { db.notifications.forEach((n) => { n.read = true }) })
on('POST', '/notifications/:id/read', ({ p }) => { find(db.notifications, p[0]).read = true })

/* trash */
on('GET', '/workspaces/:ws/trash', ({ p }) => {
  db.trash = db.trash.filter((t) => Date.now() - Date.parse(t.deletedAt) < 30 * 86_400_000)
  return db.trash.filter((t) => t.workspaceId === p[0]).map(({ payload: _payload, ...item }) => item)
})
on('POST', '/trash/:id/restore', ({ p }) => {
  const item = find(db.trash, p[0])
  const data = item.payload as { project?: Db['projects'][number]; tasks?: T.Task[]; files?: T.FileNode[]; event?: T.CalendarEvent }
  if (item.type === 'task' && data.tasks && !db.projects.some((x) => x.id === data.tasks![0].projectId)) fail('Restore the parent project first.', 409)
  if (data.project) db.projects.push(data.project)
  if (data.tasks) db.tasks.push(...data.tasks)
  if (data.event) db.events.push(data.event)
  if (data.files) {
    const restoring = new Set(data.files.map((f) => f.id))
    db.files.push(...data.files.map((f) =>
      f.parentId && !restoring.has(f.parentId) && !db.files.some((x) => x.id === f.parentId) ? { ...f, parentId: null } : f))
  }
  db.trash = without(db.trash, p[0])
})
on('DELETE', '/trash/:id', ({ p }) => { db.trash = without(db.trash, p[0]) })

/* admin */
on('GET', '/admin/users', ({ q }) => {
  const search = (q.get('search') ?? '').toLowerCase(), role = q.get('role')
  const page = Number(q.get('page') ?? 1), pageSize = Number(q.get('pageSize') ?? 8)
  const all = db.users.filter((u) => (!search || `${u.name} ${u.email}`.toLowerCase().includes(search)) && (!role || u.systemRole === role))
  return { items: all.slice((page - 1) * pageSize, page * pageSize), total: all.length, page, pageSize }
})
on('PATCH', '/admin/users/:id', ({ p, body }) => {
  if (p[0] === ME) fail('You can’t change your own role or status.')
  return Object.assign(find(db.users, p[0]), body)
})
on('GET', '/admin/roles', () => ({ roles: db.roles, permissions: db.permissions }))
on('PUT', '/admin/roles', ({ body }) => { db.roles = body.roles })
on('POST', '/admin/roles', ({ body }) => {
  if (!body.name?.trim()) fail('Name the role.')
  const role: T.Role = { id: uid('r'), name: body.name.trim(), description: body.description ?? '', locked: false, permissions: [] }
  db.roles.push(role)
  return role
})

export async function handleMock(method: string, url: string, body?: unknown): Promise<unknown> {
  const [path, query] = url.split('?')
  await new Promise((r) => setTimeout(r, method === 'GET' ? 280 + Math.random() * 320 : 220))
  for (const [m, pattern, handler] of routes) {
    const match = m === method && path.match(pattern)
    if (!match) continue
    const result = handler({ p: match.slice(1), q: new URLSearchParams(query), body })
    if (method !== 'GET') save()
    // Clone so callers can never mutate the "server" state by reference.
    return result instanceof Blob || result === undefined ? result : structuredClone(result)
  }
  throw new ApiError(404, `No mock route for ${method} ${path}`)
}
