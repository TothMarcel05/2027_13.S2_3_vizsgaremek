/** Pieces every shell reuses: navigation data, workspace switcher, notifications, user menu. */
import { useState } from 'react'
import { useNavigate } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { AnimatePresence, motion } from 'framer-motion'
import {
  Bell, CalendarDays, Check, ChevronsUpDown, FolderKanban, HardDrive, Home, LogOut, MessagesSquare,
  Plus, Settings2, ShieldCheck, Trash2, UserRound, AtSign, CheckSquare, Mail, FileText, Info,
} from 'lucide-react'
import { USE_MOCK } from '../../api/client'
import { chatApi, notificationApi, workspaceApi } from '../../api/services'
import { CreateWorkspaceModal } from './CreateWorkspaceModal'
import type { Notification } from '../../api/types'
import { useAction, useMe, useWorkspace } from '../../lib/data'
import { cn, timeAgo } from '../../lib/utils'
import { MOTION } from '../../lib/motion'
import { Avatar, Badge, Button, EmptyState, Skeleton, hueStyle, initials } from '../ui/core'
import { Drawer, Modal, Popover } from '../ui/overlay'

export const NAV = [
  { to: '/', label: 'Overview', icon: Home },
  { to: '/projects', label: 'Projects', icon: FolderKanban },
  { to: '/chat', label: 'Chat', icon: MessagesSquare },
  { to: '/calendar', label: 'Calendar', icon: CalendarDays },
  { to: '/files', label: 'Files', icon: HardDrive },
  { to: '/trash', label: 'Trash', icon: Trash2 },
]
export const NAV_SECONDARY = [
  { to: '/workspace', label: 'Workspace', icon: Settings2 },
  { to: '/admin', label: 'Admin', icon: ShieldCheck },
]

export function useNavItems() {
  const me = useMe()
  return { main: NAV, secondary: NAV_SECONDARY.filter((n) => n.to !== '/admin' || me?.systemRole === 'super_admin') }
}
export const isActive = (pathname: string, to: string) => (to === '/' ? pathname === '/' : pathname === to || pathname.startsWith(`${to}/`))

export function useUnreadChat() {
  const { workspace } = useWorkspace()
  const { data = [] } = useQuery({ queryKey: ['channels', workspace.id], queryFn: () => chatApi.channels(workspace.id) })
  return data.reduce((n, c) => n + c.unread, 0)
}

export function Brand({ compact }: { compact?: boolean }) {
  return (
    <span className="flex items-center gap-2.5">
      <img src="/OkligonBlackIconTransparent.png" alt={compact ? 'Okligon' : ''} width={36} height={36} className="brand-mark" />
      {!compact && <span className="display text-xl">Okligon</span>}
    </span>
  )
}

const WsTile = ({ name, hue }: { name: string; hue: number }) => <span className="ws-tile" style={hueStyle(hue)}>{initials(name)}</span>

export function WorkspaceSwitcher({ compact, placement = 'bottom' }: { compact?: boolean; placement?: 'bottom' | 'side' }) {
  const { workspace, workspaces, setWorkspace } = useWorkspace()
  const navigate = useNavigate()
  const me = useMe()
  const [creating, setCreating] = useState(false)
  const [showInvites, setShowInvites] = useState(false)
  // Incoming invitations aren't connected yet, and sample ones would be mistaken for real ones.
  const invites = useQuery({ queryKey: ['my-invitations'], queryFn: workspaceApi.myInvitations, enabled: USE_MOCK })
  const respond = useAction((v: { id: string; accept: boolean }) => workspaceApi.respondInvitation(v.id, v.accept))
  const pending = invites.data?.length ?? 0
  return (
    <>
      <Popover
        placement={placement} className="w-72 p-1.5" wrapClass="min-w-0"
        trigger={({ open, toggle }) => (
          <button type="button" onClick={toggle} aria-expanded={open} aria-label={`Workspace: ${workspace.name}. Switch workspace`} className={cn('ws-trigger', compact && 'ws-trigger-compact')}>
            <span className="relative">
              <WsTile name={workspace.name} hue={workspace.hue} />
              {pending > 0 && <span className="absolute -right-1 -top-1 size-2.5 rounded-full border-2 border-surface bg-accent" />}
            </span>
            {!compact && (<>
              <span className="min-w-0 flex-1 text-left">
                <span className="block truncate text-sm font-semibold">{workspace.name}</span>
                <span className="block text-xs text-muted">{workspace.plan ? `${workspace.plan} plan` : workspace.ownerId === me?.id ? 'Owned by you' : 'Shared with you'}</span>
              </span>
              <ChevronsUpDown size={14} className="shrink-0 text-muted" />
            </>)}
          </button>
        )}
      >
        {(close) => (<>
          <p className="menu-label">Workspaces</p>
          {workspaces.map((w) => (
            <button key={w.id} type="button" className="menu-item" onClick={() => { setWorkspace(w.id); close(); navigate('/') }}>
              <WsTile name={w.name} hue={w.hue} /><span className="flex-1 truncate">{w.name}</span>
              {w.id === workspace.id && <Check size={15} className="text-accent" />}
            </button>
          ))}
          <div className="menu-sep" />
          <button type="button" className="menu-item" onClick={() => { setCreating(true); close() }}><Plus size={16} /> Create workspace</button>
          {USE_MOCK && (
            <button type="button" className="menu-item" onClick={() => { setShowInvites(true); close() }}>
              <Mail size={16} /><span className="flex-1">Pending invitations</span>{pending > 0 && <span className="count-badge">{pending}</span>}
            </button>
          )}
        </>)}
      </Popover>

      <CreateWorkspaceModal open={creating} onClose={() => setCreating(false)} onCreated={(id) => { setWorkspace(id); navigate('/') }} />

      <Modal open={showInvites} onClose={() => setShowInvites(false)} title="Pending invitations" description="Workspaces and projects you’ve been invited to.">
        {!pending && <EmptyState icon={<Mail size={18} />} title="You’re all caught up" />}
        <AnimatePresence initial={false}>
          {invites.data?.map((i) => (
            <motion.div key={i.id} layout exit={{ opacity: 0, height: 0 }} className="row-card flex flex-wrap items-center gap-3 p-3">
              <div className="min-w-0 flex-1">
                <p className="flex flex-wrap items-center gap-2 text-sm font-medium">{i.targetName} <Badge>{i.kind}</Badge></p>
                <p className="text-xs text-muted">{i.invitedBy} invited you as {i.role} · {timeAgo(i.sentAt)}</p>
              </div>
              <Button size="sm" variant="ghost" onClick={() => respond.mutate({ id: i.id, accept: false })}>Decline</Button>
              <Button size="sm" variant="primary" onClick={() => respond.mutate({ id: i.id, accept: true })}>Accept</Button>
            </motion.div>
          ))}
        </AnimatePresence>
      </Modal>
    </>
  )
}

const NOTE_ICON = { task: CheckSquare, mention: AtSign, invite: Mail, file: FileText, system: Info }

function NotificationRow({ n, onOpen, onRead }: { n: Notification; onOpen: () => void; onRead: () => void }) {
  const Icon = NOTE_ICON[n.type]
  return (
    <motion.li layout className={cn('note-row', !n.read && 'note-unread')}>
      <button type="button" onClick={onOpen} className="flex min-w-0 flex-1 gap-3 text-left">
        <span className="note-icon"><Icon size={14} /></span>
        <span className="min-w-0">
          <span className="block text-sm font-medium">{n.title}</span>
          <span className="block text-xs text-muted">{n.body}</span>
          <span className="mt-0.5 block text-[0.7rem] text-muted">{timeAgo(n.createdAt)}</span>
        </span>
      </button>
      {!n.read && <Button size="sm" variant="ghost" icon={<Check size={14} />} aria-label={`Mark “${n.title}” as read`} title="Mark as read" onClick={onRead} />}
    </motion.li>
  )
}

export function NotificationBell({ placement = 'bottom', align = 'right' }: { placement?: 'bottom' | 'side'; align?: 'left' | 'right' }) {
  const navigate = useNavigate()
  const [drawer, setDrawer] = useState(false)
  const [filter, setFilter] = useState<'all' | 'unread'>('all')
  const query = useQuery({ queryKey: ['notifications'], queryFn: notificationApi.list })
  const list = query.data ?? []
  const unread = list.filter((n) => !n.read).length
  const markRead = useAction(notificationApi.markRead)
  const markAll = useAction(notificationApi.markAllRead, { success: 'All caught up' })
  const rows = (items: Notification[], close: () => void) => (
    <ul className="flex flex-col">
      {items.map((n) => (
        <NotificationRow key={n.id} n={n} onRead={() => markRead.mutate(n.id)}
          onOpen={() => { if (!n.read) markRead.mutate(n.id); close(); navigate(n.link) }} />
      ))}
    </ul>
  )
  const header = (
    <div className="flex items-center justify-between gap-2 px-2.5 py-1.5">
      <p className="section-title">Notifications</p>
      <Button size="sm" variant="ghost" disabled={!unread} onClick={() => markAll.mutate()}>Mark all as read</Button>
    </div>
  )
  return (
    <>
      <Popover
        align={align} placement={placement} className="w-[min(23rem,calc(100vw-2rem))] p-1.5"
        trigger={({ open, toggle }) => (
          <button type="button" className="icon-btn" onClick={toggle} aria-expanded={open} aria-label={`Notifications, ${unread} unread`}>
            <motion.span key={unread} animate={unread ? { rotate: [0, -14, 12, -7, 0] } : {}} transition={{ duration: 0.55 }}><Bell size={18} /></motion.span>
            <AnimatePresence>
              {unread > 0 && <motion.span className="bell-count" initial={{ scale: 0 }} animate={{ scale: 1 }} exit={{ scale: 0 }} transition={MOTION.spring}>{unread}</motion.span>}
            </AnimatePresence>
          </button>
        )}
      >
        {(close) => (<>
          {header}
          <div className="max-h-80 overflow-y-auto">
            {query.isPending ? <div className="space-y-2 p-2"><Skeleton className="h-14" /><Skeleton className="h-14" /></div>
              : list.length ? rows(list.slice(0, 4), close) : <EmptyState icon={<Bell size={18} />} title="Nothing yet" />}
          </div>
          <Button variant="soft" className="mt-1 w-full" onClick={() => { close(); setDrawer(true) }}>View all notifications</Button>
        </>)}
      </Popover>
      <Drawer open={drawer} onClose={() => setDrawer(false)} title="Notifications">
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <div className="flex gap-1">
            {(['all', 'unread'] as const).map((f) => <Button key={f} size="sm" variant={filter === f ? 'soft' : 'ghost'} aria-pressed={filter === f} onClick={() => setFilter(f)}>{f === 'all' ? 'All' : `Unread (${unread})`}</Button>)}
          </div>
          <Button size="sm" variant="ghost" disabled={!unread} onClick={() => markAll.mutate()}>Mark all as read</Button>
        </div>
        {rows(list.filter((n) => filter === 'all' || !n.read), () => setDrawer(false))}
        {filter === 'unread' && !unread && <EmptyState icon={<Check size={18} />} title="No unread notifications" />}
      </Drawer>
    </>
  )
}

export function UserMenu({ signOut, placement = 'bottom', align = 'right', detailed }: { signOut: () => void; placement?: 'bottom' | 'top' | 'side'; align?: 'left' | 'right'; detailed?: boolean }) {
  const me = useMe()
  const navigate = useNavigate()
  return (
    <Popover
      placement={placement} align={align} className="w-60 p-1.5" wrapClass={detailed ? 'min-w-0 flex-1' : ''}
      trigger={({ open, toggle }) => (
        <button type="button" onClick={toggle} aria-expanded={open} aria-label="Account menu" className={cn('flex items-center gap-2.5 rounded-full', detailed && 'w-full min-w-0')}>
          <Avatar user={me} size={32} />
          {detailed && <span className="min-w-0 text-left"><span className="block truncate text-sm font-medium">{me?.name}</span><span className="block truncate text-xs text-muted">{me?.title || me?.email}</span></span>}
        </button>
      )}
    >
      {(close) => (<>
        <div className="px-2.5 py-2"><p className="text-sm font-semibold">{me?.name}</p><p className="truncate text-xs text-muted">{me?.email}</p></div>
        <div className="menu-sep" />
        <button type="button" className="menu-item" onClick={() => { close(); navigate('/profile') }}><UserRound size={16} /> Profile settings</button>
        <button type="button" className="menu-item" onClick={() => { close(); navigate('/workspace') }}><Settings2 size={16} /> Workspace settings</button>
        <button type="button" className="menu-item" onClick={signOut}><LogOut size={16} /> Sign out</button>
      </>)}
    </Popover>
  )
}
