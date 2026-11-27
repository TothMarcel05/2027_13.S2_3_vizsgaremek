import { useState, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { DndContext, DragOverlay, KeyboardSensor, MouseSensor, TouchSensor, useDraggable, useDroppable, useSensor, useSensors, type DragEndEvent } from '@dnd-kit/core'
import { AnimatePresence, LayoutGroup, motion } from 'framer-motion'
import { ArrowLeft, Bookmark, CalendarDays, GripVertical, Columns3, List, Plus, Trash2, ListFilter } from 'lucide-react'
import { projectApi, taskApi } from '../../api/services'
import type { Priority, ProjectStatus, Task, TaskStatus } from '../../api/types'
import { useAction, useMembers, usePeople } from '../../lib/data'
import { cn, daysUntil, dueLabel, fmtDate, fromDateInput } from '../../lib/utils'
import { MOTION } from '../../lib/motion'
import { Avatar, AvatarStack, Badge, Button, EmptyState, ErrorState, FormError, Input, PageHeader, PRIORITY, PriorityBadge, PROJECT_STATUS, Progress, Segmented, Select, Skeleton, TASK_STATUS, Textarea } from '../../components/ui/core'
import { Modal, useConfirm, useToast } from '../../components/ui/overlay'
import { TaskDrawer } from './TaskDrawer'

const COLUMNS = Object.keys(TASK_STATUS) as TaskStatus[]

function CardFace({ task, onOpen, handle }: { task: Task; onOpen?: () => void; handle?: ReactNode }) {
  const people = usePeople()
  const owner = task.assigneeId ? people.get(task.assigneeId) : undefined
  return (
    <div className="kanban-card">
      <div className="flex items-start gap-1">
        <button type="button" className="min-w-0 flex-1 text-left text-sm font-medium after:absolute after:inset-0" onClick={onOpen}>{task.title}</button>
        {handle}
      </div>
      <div className="mt-2 flex flex-wrap items-center gap-1.5">
        <PriorityBadge value={task.priority} />
        {task.reservedById && <Badge tone="accent"><Bookmark size={10} /> Reserved</Badge>}
      </div>
      <div className="mt-2.5 flex items-center justify-between">
        <span className={cn('flex items-center gap-1 text-xs', task.dueDate && task.status !== 'done' && daysUntil(task.dueDate) <= 0 ? 'text-danger' : 'text-muted')}>
          <CalendarDays size={12} />{task.status === 'done' ? fmtDate(task.dueDate) : dueLabel(task.dueDate)}
        </span>
        <Avatar user={owner} size={22} className={owner ? '' : 'avatar-empty'} />
      </div>
    </div>
  )
}

function DraggableCard({ task, onOpen }: { task: Task; onOpen: () => void }) {
  const { setNodeRef, setActivatorNodeRef, attributes, listeners, isDragging } = useDraggable({ id: task.id })
  // The whole card drags with a mouse or long-press; the grip is the keyboard handle.
  const { onKeyDown, ...pointer } = listeners ?? {}
  return (
    <motion.div ref={setNodeRef} layout layoutId={task.id} transition={MOTION.spring}
      initial={{ opacity: 0, y: 12 }} animate={{ opacity: isDragging ? 0.35 : 1, y: 0 }} exit={{ opacity: 0, scale: 0.94 }}
      className="relative cursor-grab touch-manipulation" {...pointer}>
      <CardFace task={task} onOpen={onOpen} handle={
        <button ref={setActivatorNodeRef} type="button" {...attributes} onKeyDown={onKeyDown as never} className="grip relative z-10" aria-label={`Move “${task.title}”`}>
          <GripVertical size={14} />
        </button>
      } />
    </motion.div>
  )
}

function Column({ status, tasks, onOpen, onAdd }: { status: TaskStatus; tasks: Task[]; onOpen: (id: string) => void; onAdd: () => void }) {
  const { setNodeRef, isOver } = useDroppable({ id: status })
  return (
    <section ref={setNodeRef} className={cn('kanban-col', isOver && 'kanban-over')} aria-label={TASK_STATUS[status].label}>
      <header className="mb-2.5 flex items-center gap-2 px-1">
        <span className={`status-ring tone-${TASK_STATUS[status].tone}`} />
        <h2 className="column-title">{TASK_STATUS[status].label}</h2>
        <span className="text-xs text-muted">{tasks.length}</span>
        <Button size="sm" variant="ghost" className="ml-auto" icon={<Plus size={14} />} aria-label={`Add task to ${TASK_STATUS[status].label}`} onClick={onAdd} />
      </header>
      <div className="flex min-h-24 flex-1 flex-col gap-2.5">
        <AnimatePresence initial>
          {tasks.map((t) => <DraggableCard key={t.id} task={t} onOpen={() => onOpen(t.id)} />)}
        </AnimatePresence>
        {!tasks.length && <p className="drop-hint">Drop a task here</p>}
      </div>
    </section>
  )
}

export function ProjectDetail() {
  const { projectId = '' } = useParams()
  const navigate = useNavigate()
  const client = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const people = usePeople()
  const members = useMembers().data ?? []
  const [view, setView] = useState<'board' | 'list'>('board')
  const [status, setStatus] = useState<'all' | TaskStatus>('all')
  const [priority, setPriority] = useState<'all' | Priority>('all')
  const [openId, setOpenId] = useState<string | null>(null)
  const [dragId, setDragId] = useState<string | null>(null)
  const blank = { title: '', description: '', priority: 'medium' as Priority, due: '', assigneeId: '' }
  const [draft, setDraft] = useState<(typeof blank & { status: TaskStatus }) | null>(null)
  const lastDraft = draft ?? { ...blank, status: 'todo' as TaskStatus }

  const project = useQuery({ queryKey: ['project', projectId], queryFn: () => projectApi.get(projectId), retry: false })
  const tasks = useQuery({ queryKey: ['tasks', projectId], queryFn: () => taskApi.list(projectId) })

  // Optimistic: the card lands immediately and rolls back if the server refuses.
  const move = useMutation({
    mutationFn: (v: { id: string; status: TaskStatus }) => taskApi.update(v.id, { status: v.status }),
    onMutate: async (v) => {
      await client.cancelQueries({ queryKey: ['tasks', projectId] })
      const previous = client.getQueryData<Task[]>(['tasks', projectId])
      client.setQueryData<Task[]>(['tasks', projectId], (list) => list?.map((t) => (t.id === v.id ? { ...t, status: v.status } : t)))
      return { previous }
    },
    onError: (e: Error, _v, ctx) => { client.setQueryData(['tasks', projectId], ctx?.previous); toast(e.message, 'error') },
    onSettled: () => client.invalidateQueries(),
  })
  const create = useAction(
    () => taskApi.create(projectId, { title: draft!.title, description: draft!.description, priority: draft!.priority, status: draft!.status, assigneeId: draft!.assigneeId || null, dueDate: fromDateInput(draft!.due) }),
    { success: 'Task added', silentError: true, onSuccess: () => setDraft(null) },
  )
  const setProjectStatus = useAction((s: ProjectStatus) => projectApi.update(projectId, { status: s }), { success: 'Project status updated' })
  const remove = useAction(() => projectApi.remove(projectId), { success: 'Project moved to trash', onSuccess: () => navigate('/projects') })

  const sensors = useSensors(
    useSensor(MouseSensor, { activationConstraint: { distance: 6 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 220, tolerance: 6 } }),
    useSensor(KeyboardSensor, {
      // Left and right arrows jump a whole column.
      coordinateGetter: (e, { currentCoordinates }) => {
        const step = e.code === 'ArrowRight' ? 290 : e.code === 'ArrowLeft' ? -290 : 0
        return step ? { ...currentCoordinates, x: currentCoordinates.x + step } : undefined
      },
    }),
  )
  const dragEnd = ({ active, over }: DragEndEvent) => {
    setDragId(null)
    const task = tasks.data?.find((t) => t.id === active.id)
    if (task && over && task.status !== over.id) move.mutate({ id: task.id, status: over.id as TaskStatus })
  }

  if (project.isError) return <ErrorState error={new Error('This project no longer exists.')} retry={() => navigate('/projects')} />
  const p = project.data
  const all = tasks.data ?? []
  const visible = all.filter((t) => priority === 'all' || t.priority === priority)
  const listed = visible.filter((t) => status === 'all' || t.status === status)
  const dragging = all.find((t) => t.id === dragId)

  return (
    <>
      <Link to="/projects" className="link mb-3 inline-flex items-center gap-1.5 text-sm"><ArrowLeft size={14} /> All projects</Link>
      {!p ? <Skeleton className="mb-6 h-20" /> : (
        <PageHeader
          eyebrow={<span className="flex items-center gap-2"><Badge tone={PROJECT_STATUS[p.status].tone} dot>{PROJECT_STATUS[p.status].label}</Badge> Due {fmtDate(p.dueDate)}</span>}
          title={p.name} description={p.description}
          actions={<>
            <AvatarStack users={p.memberIds.map((id) => people.get(id))} />
            <Select aria-label="Project status" className="!w-auto" value={p.status} onChange={(e) => setProjectStatus.mutate(e.target.value as ProjectStatus)}>
              {Object.entries(PROJECT_STATUS).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
            </Select>
            <Button variant="ghost" icon={<Trash2 size={15} />} aria-label="Delete project" title="Delete project" onClick={async () => {
              if (await confirm({ title: 'Move project to trash?', message: `“${p.name}” and its tasks can be restored for 30 days.`, confirmLabel: 'Move to trash', danger: true })) remove.mutate()
            }} />
            <Button variant="primary" icon={<Plus size={15} />} onClick={() => setDraft({ ...blank, status: 'todo' })}>Add task</Button>
          </>}
        />
      )}
      {p && <Progress value={p.taskCount ? (p.doneCount / p.taskCount) * 100 : 0} className="mb-5" />}

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <Segmented label="View" value={view} onChange={setView} options={[
          { value: 'board', label: <><Columns3 size={14} /> Board</> }, { value: 'list', label: <><List size={14} /> List</> },
        ]} />
        <div className="ml-auto flex flex-wrap items-center gap-2">
          <ListFilter size={15} className="text-muted" aria-hidden />
          {view === 'list' && (
            <Select aria-label="Filter by status" className="!w-auto" value={status} onChange={(e) => setStatus(e.target.value as typeof status)}>
              <option value="all">All statuses</option>
              {COLUMNS.map((k) => <option key={k} value={k}>{TASK_STATUS[k].label}</option>)}
            </Select>
          )}
          <Select aria-label="Filter by priority" className="!w-auto" value={priority} onChange={(e) => setPriority(e.target.value as typeof priority)}>
            <option value="all">All priorities</option>
            {Object.entries(PRIORITY).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
          </Select>
        </div>
      </div>

      {tasks.isPending ? (
        <div className="grid grid-cols-2 gap-3 lg:grid-cols-4" role="status" aria-label="Loading tasks">{COLUMNS.map((c) => <div key={c} className="space-y-2.5"><Skeleton className="h-6" /><Skeleton className="h-28" /><Skeleton className="h-28" /></div>)}</div>
      ) : (
        <AnimatePresence mode="wait">
          <motion.div key={view} initial="hidden" animate="show" exit="exit" variants={MOTION.page}>
            {view === 'board' ? (
              <DndContext sensors={sensors} onDragStart={(e) => setDragId(String(e.active.id))} onDragEnd={dragEnd} onDragCancel={() => setDragId(null)}>
                <LayoutGroup>
                  <div className="kanban">
                    {COLUMNS.map((c) => <Column key={c} status={c} tasks={visible.filter((t) => t.status === c)} onOpen={setOpenId} onAdd={() => setDraft({ ...blank, status: c })} />)}
                  </div>
                </LayoutGroup>
                <DragOverlay dropAnimation={null}>{dragging && <div className="rotate-2 cursor-grabbing"><CardFace task={dragging} /></div>}</DragOverlay>
              </DndContext>
            ) : !listed.length ? (
              <div className="card"><EmptyState icon={<List size={18} />} title="No tasks match these filters" /></div>
            ) : (
              <div className="card divide-y divide-line overflow-hidden">
                <AnimatePresence initial={false}>
                  {listed.map((t, i) => (
                    <motion.div key={t.id} layout initial={{ opacity: 0, x: -8 }} animate={{ opacity: 1, x: 0, transition: { delay: Math.min(i, 10) * 0.03 } }} exit={{ opacity: 0 }} className="list-row px-4">
                      <input type="checkbox" className="check" checked={t.status === 'done'} aria-label={`Mark “${t.title}” ${t.status === 'done' ? 'not done' : 'done'}`}
                        onChange={() => move.mutate({ id: t.id, status: t.status === 'done' ? 'todo' : 'done' })} />
                      <button type="button" className={cn('min-w-0 flex-1 truncate text-left text-sm font-medium', t.status === 'done' && 'text-muted line-through')} onClick={() => setOpenId(t.id)}>{t.title}</button>
                      <span className="hidden w-28 md:block"><Badge tone={TASK_STATUS[t.status].tone}>{TASK_STATUS[t.status].label}</Badge></span>
                      <span className="w-20 max-sm:hidden"><PriorityBadge value={t.priority} /></span>
                      <span className="w-24 text-right text-xs text-muted">{dueLabel(t.dueDate)}</span>
                      <Avatar user={t.assigneeId ? people.get(t.assigneeId) : undefined} size={24} />
                    </motion.div>
                  ))}
                </AnimatePresence>
              </div>
            )}
          </motion.div>
        </AnimatePresence>
      )}

      <TaskDrawer taskId={openId} projectId={projectId} onClose={() => setOpenId(null)} />

      <Modal open={!!draft} onClose={() => { setDraft(null); create.reset() }} onSubmit={() => create.mutate()} title="Add a task"
        footer={<><Button variant="ghost" onClick={() => setDraft(null)}>Cancel</Button><Button type="submit" variant="primary" loading={create.isPending}>Add task</Button></>}>
        <Input label="Title" value={lastDraft.title} onChange={(e) => setDraft({ ...lastDraft, title: e.target.value })} data-autofocus />
        <Textarea label="Description" value={lastDraft.description} onChange={(e) => setDraft({ ...lastDraft, description: e.target.value })} />
        <div className="grid grid-cols-2 gap-3">
          <Select label="Status" value={lastDraft.status} onChange={(e) => setDraft({ ...lastDraft, status: e.target.value as TaskStatus })}>
            {COLUMNS.map((k) => <option key={k} value={k}>{TASK_STATUS[k].label}</option>)}
          </Select>
          <Select label="Priority" value={lastDraft.priority} onChange={(e) => setDraft({ ...lastDraft, priority: e.target.value as Priority })}>
            {Object.entries(PRIORITY).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
          </Select>
          <Input label="Due date" type="date" value={lastDraft.due} onChange={(e) => setDraft({ ...lastDraft, due: e.target.value })} />
          <Select label="Assignee" value={lastDraft.assigneeId} onChange={(e) => setDraft({ ...lastDraft, assigneeId: e.target.value })}>
            <option value="">Unassigned (open to claim)</option>
            {members.map((m) => <option key={m.userId} value={m.userId}>{m.user.name}</option>)}
          </Select>
        </div>
        <FormError error={create.error} />
      </Modal>
    </>
  )
}
