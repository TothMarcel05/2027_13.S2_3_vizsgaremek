import { useState, type ReactNode } from 'react'
import { Link, useNavigate } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { ArrowRight, Bookmark, CalendarDays, CheckCircle2, Cloud, FolderKanban, MapPin } from 'lucide-react'
import { workspaceApi } from '../../api/services'
import type { CalendarEvent, DashboardData, Project, Task } from '../../api/types'
import { useMe, usePeople, useWorkspace } from '../../lib/data'
import { daysUntil, dueLabel, fmtBytes, fmtDate, fmtTime } from '../../lib/utils'
import { AvatarStack, Button, Card, EmptyState, ErrorState, Item, PageHeader, PriorityBadge, Progress, SectionTitle, Skeleton, Stagger, TASK_STATUS, hueStyle } from '../../components/ui/core'
import { TaskDrawer } from '../projects/TaskDrawer'

const pct = (p: Project) => (p.taskCount ? (p.doneCount / p.taskCount) * 100 : 0)
const greeting = () => { const h = new Date().getHours(); return h < 12 ? 'Good morning' : h < 18 ? 'Good afternoon' : 'Good evening' }
const STORAGE_TONES = ['#c87d55', '#7f9a7b', '#d3a965', '#b9ac98']

const more = (to: string, label: string) => <Link to={to} className="link flex items-center gap-1 text-sm">{label} <ArrowRight size={13} /></Link>

function ProjectCard({ p, className }: { p: Project; className?: string }) {
  const people = usePeople()
  return (
    <Link to={`/projects/${p.id}`} className={`block h-full ${className ?? ''}`}>
      <Card hover className="flex h-full flex-col gap-3 p-4">
        <div className="flex items-start justify-between gap-2">
          <span className="project-dot" style={hueStyle(p.hue)}><FolderKanban size={15} /></span>
          <span className="text-xs text-muted">Due {fmtDate(p.dueDate)}</span>
        </div>
        <div>
          <h3 className="card-title">{p.name}</h3>
          <p className="mt-1 line-clamp-2 text-sm text-muted">{p.description}</p>
        </div>
        <div className="mt-auto space-y-2">
          <Progress value={pct(p)} hue={p.hue} />
          <div className="flex items-center justify-between">
            <AvatarStack users={p.memberIds.map((id) => people.get(id))} />
            <span className="text-xs text-muted">{p.doneCount} of {p.taskCount} done</span>
          </div>
        </div>
      </Card>
    </Link>
  )
}

function TaskRows({ tasks, projects, onOpen }: { tasks: Task[]; projects: Project[]; onOpen: (t: Task) => void }) {
  const me = useMe()
  if (!tasks.length) return <EmptyState icon={<CheckCircle2 size={18} />} title="Nothing on your plate">Reserve an open task from any project board.</EmptyState>
  return (
    <Stagger className="divide-y divide-line">
      {tasks.map((t) => (
        <Item key={t.id}>
          <button type="button" className="list-row w-full text-left" onClick={() => onOpen(t)}>
            <span className={`status-ring tone-${TASK_STATUS[t.status].tone}`} aria-label={TASK_STATUS[t.status].label} />
            <span className="min-w-0 flex-1">
              <span className="block truncate text-sm font-medium">{t.title}</span>
              <span className="flex items-center gap-1.5 text-xs text-muted">
                {t.reservedById === me?.id && <><Bookmark size={11} className="text-accent" /> Reserved ·</>}
                <span className="truncate">{projects.find((p) => p.id === t.projectId)?.name ?? TASK_STATUS[t.status].label}</span>
              </span>
            </span>
            <span className="hidden sm:block"><PriorityBadge value={t.priority} /></span>
            <span className={`w-24 shrink-0 text-right text-xs ${t.dueDate && daysUntil(t.dueDate) <= 0 ? 'font-semibold text-danger' : 'text-muted'}`}>{dueLabel(t.dueDate)}</span>
          </button>
        </Item>
      ))}
    </Stagger>
  )
}

function EventRows({ events }: { events: CalendarEvent[] }) {
  if (!events.length) return <EmptyState icon={<CalendarDays size={18} />} title="No upcoming events" />
  return (
    <Stagger className="flex flex-col">
      {events.map((e) => (
        <Item key={e.id}>
          <Link to="/calendar" className="list-row">
            <span className="date-chip"><span>{fmtDate(e.start, { month: 'short' })}</span><strong>{new Date(e.start).getDate()}</strong></span>
            <span className="min-w-0">
              <span className="block truncate text-sm font-medium">{e.title}</span>
              <span className="flex items-center gap-1 text-xs text-muted">{fmtTime(e.start)}–{fmtTime(e.end)}{e.location && <><MapPin size={11} className="ml-1" />{e.location}</>}</span>
            </span>
          </Link>
        </Item>
      ))}
    </Stagger>
  )
}

function StorageBar({ storage }: { storage: DashboardData['storage'] }) {
  return (
    <div>
      <div className="flex items-baseline justify-between gap-2">
        <p><span className="display text-3xl">{storage.used ? fmtBytes(storage.used) : '0 GB'}</span> <span className="text-sm text-muted">of {fmtBytes(storage.limit)}</span></p>
        <span className="text-sm font-medium">{Math.round((storage.used / storage.limit) * 100)}%</span>
      </div>
      <div className="progress mt-3 flex !h-2.5 gap-px" role="img" aria-label="Storage used by file type">
        {storage.breakdown.map((b, i) => (
          <motion.span key={b.label} style={{ background: STORAGE_TONES[i] }} initial={{ width: 0 }} animate={{ width: `${(b.bytes / storage.limit) * 100}%` }} transition={{ duration: 0.8, delay: 0.15 + i * 0.1, ease: 'easeOut' }} />
        ))}
      </div>
      <ul className="mt-3 grid grid-cols-2 gap-x-4 gap-y-1.5 text-xs">
        {storage.breakdown.map((b, i) => (
          <li key={b.label} className="flex items-center gap-1.5"><span className="size-2 rounded-full" style={{ background: STORAGE_TONES[i] }} /><span className="flex-1 truncate text-muted">{b.label}</span>{fmtBytes(b.bytes)}</li>
        ))}
      </ul>
    </div>
  )
}

function Panel({ title, action, children, className }: { title: string; action?: ReactNode; children: ReactNode; className?: string }) {
  return (
    <Item className={className}>
      <section className="card h-full p-4 sm:p-5">
        <SectionTitle title={title} action={action} />
        {children}
      </section>
    </Item>
  )
}

function stats(d: DashboardData) {
  return [
    { label: 'Active projects', value: d.projects.length, icon: FolderKanban },
    { label: 'My open tasks', value: d.tasks.length, icon: CheckCircle2 },
    { label: 'Due this week', value: d.tasks.filter((t) => t.dueDate && daysUntil(t.dueDate) <= 7).length, icon: CalendarDays },
    { label: 'Storage used', value: `${Math.round((d.storage.used / d.storage.limit) * 100)}%`, icon: Cloud },
  ]
}

interface ViewProps { d: DashboardData; first: string; open: (t: Task) => void }

/** Greeting, stat cards and a two-column bento grid. */
function Overview({ d, first, open }: ViewProps) {
  const { workspace } = useWorkspace()
  const navigate = useNavigate()
  return (
    <>
      <PageHeader eyebrow={workspace.name} title={`${greeting()}, ${first}`} description="Here’s what needs your attention today."
        actions={<Button variant="primary" icon={<FolderKanban size={15} />} onClick={() => navigate('/projects?new=1')}>New project</Button>} />
      <Stagger className="grid gap-4 lg:grid-cols-3">
        <div className="grid grid-cols-2 gap-4 lg:col-span-3 lg:grid-cols-4">
          {stats(d).map((s) => (
            <Item key={s.label}>
              <Card hover className="flex items-center justify-between p-4">
                <div><p className="text-xs text-muted">{s.label}</p><p className="display mt-1 text-3xl">{s.value}</p></div>
                <span className="empty-icon"><s.icon size={17} /></span>
              </Card>
            </Item>
          ))}
        </div>
        <Item className="lg:col-span-3">
          <SectionTitle title="Active projects" action={more('/projects', 'All projects')} />
          {d.projects.length ? <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">{d.projects.map((p) => <ProjectCard key={p.id} p={p} />)}</div>
            : <div className="card"><EmptyState icon={<FolderKanban size={18} />} title="No active projects yet" /></div>}
        </Item>
        <Panel title="My tasks" className="lg:col-span-2 lg:row-span-2"><TaskRows tasks={d.tasks} projects={d.projects} onOpen={open} /></Panel>
        <Panel title="Upcoming" action={more('/calendar', 'Calendar')}><EventRows events={d.events} /></Panel>
        <Panel title="Storage" action={more('/files', 'Files')}><StorageBar storage={d.storage} /></Panel>
      </Stagger>
    </>
  )
}

export function Dashboard() {
  const { workspace } = useWorkspace()
  const me = useMe()
  const [openTask, setOpenTask] = useState<{ id: string; projectId: string } | null>(null)
  const query = useQuery({ queryKey: ['dashboard', workspace.id], queryFn: () => workspaceApi.dashboard(workspace.id) })
  if (query.isError) return <ErrorState error={query.error} retry={() => query.refetch()} />
  if (!query.data) return (
    <div className="space-y-5" role="status" aria-label="Loading overview">
      <Skeleton className="h-24 w-full" />
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">{[0, 1, 2, 3].map((i) => <Skeleton key={i} className="h-24" />)}</div>
      <div className="grid gap-4 lg:grid-cols-3"><Skeleton className="h-72 lg:col-span-2" /><Skeleton className="h-72" /></div>
    </div>
  )
  return (
    <>
      <Overview d={query.data} first={me?.name.split(' ')[0] ?? ''} open={(t) => setOpenTask(t)} />
      <TaskDrawer taskId={openTask?.id ?? null} projectId={openTask?.projectId ?? null} onClose={() => setOpenTask(null)} />
    </>
  )
}
