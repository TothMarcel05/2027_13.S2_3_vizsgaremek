import { useRef } from 'react'
import { Link, useLocation } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { AnimatePresence, motion } from 'framer-motion'
import { ArrowUpRight, Bookmark, BookmarkCheck, Lock, Trash2 } from 'lucide-react'
import { projectApi, taskApi } from '../../api/services'
import type { Priority, Task, TaskStatus } from '../../api/types'
import { useAction, useMe, useMembers } from '../../lib/data'
import { dueLabel, fromDateInput, toDateInput } from '../../lib/utils'
import { Avatar, Badge, Button, Field, Input, PRIORITY, PriorityBadge, Select, Skeleton, TASK_STATUS, Textarea } from '../../components/ui/core'
import { Drawer, useConfirm } from '../../components/ui/overlay'

function Body({ task }: { task: Task }) {
  const me = useMe()
  const members = useMembers().data ?? []
  const project = useQuery({ queryKey: ['project', task.projectId], queryFn: () => projectApi.get(task.projectId) })
  const { pathname } = useLocation()
  const update = useAction((patch: Partial<Task>) => taskApi.update(task.id, patch))
  const reserve = useAction(() => taskApi.reserve(task.id), { success: 'Task reserved. It’s yours.' })
  const unreserve = useAction(() => taskApi.unreserve(task.id), { success: 'Task released back to the team.' })
  const nameOf = (id: string | null) => members.find((m) => m.userId === id)?.user
  const holder = nameOf(task.reservedById ?? task.assigneeId)
  const mine = task.reservedById === me?.id
  const takenByOther = !!(task.reservedById && !mine) || !!(task.assigneeId && task.assigneeId !== me?.id)

  return (
    <div className="flex flex-col gap-5">
      <div>
        {project.data && !pathname.startsWith('/projects/') && (
          <Link to={`/projects/${task.projectId}`} className="link mb-1 inline-flex items-center gap-1 text-xs">{project.data.name} <ArrowUpRight size={12} /></Link>
        )}
        <input key={task.title} aria-label="Task title" className="title-input" defaultValue={task.title}
          onBlur={(e) => { const v = e.target.value.trim(); if (!v) e.target.value = task.title; else if (v !== task.title) update.mutate({ title: v }) }}
          onKeyDown={(e) => e.key === 'Enter' && e.currentTarget.blur()} />
        <div className="mt-2 flex flex-wrap gap-1.5">
          <Badge tone={TASK_STATUS[task.status].tone}>{TASK_STATUS[task.status].label}</Badge>
          <PriorityBadge value={task.priority} />
          {task.tags.map((t) => <Badge key={t}>#{t}</Badge>)}
        </div>
      </div>

      <motion.section layout className={`reserve-box ${mine ? 'reserve-mine' : ''}`}>
        <div className="flex items-center gap-3">
          <Avatar user={holder} size={36} />
          <AnimatePresence mode="wait" initial={false}>
            <motion.p key={`${task.reservedById}-${task.assigneeId}`} initial={{ opacity: 0, y: 6 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -6 }} className="min-w-0 text-sm">
              <span className="block font-semibold">{mine ? 'Reserved by you' : task.reservedById ? `Reserved by ${holder?.name}` : holder ? `Assigned to ${holder.name}` : 'Open task'}</span>
              <span className="text-xs text-muted">{mine ? 'Teammates can see you’re on it.' : takenByOther ? 'Ask them to release it if you want to help.' : task.assigneeId ? 'Reserve it to show you’re working on it now.' : 'Claim it so nobody doubles up.'}</span>
            </motion.p>
          </AnimatePresence>
        </div>
        {mine ? (
          <Button size="lg" variant="outline" className="w-full" icon={<BookmarkCheck size={17} />} loading={unreserve.isPending} onClick={() => unreserve.mutate()}>Unreserve task</Button>
        ) : (
          <Button size="lg" variant="primary" className="w-full" icon={takenByOther ? <Lock size={16} /> : <Bookmark size={17} />}
            disabled={takenByOther || task.status === 'done'} loading={reserve.isPending} onClick={() => reserve.mutate()}>
            {task.status === 'done' ? 'Task completed' : takenByOther ? 'Not available' : 'Reserve task'}
          </Button>
        )}
      </motion.section>

      <Textarea key={task.description} label="Description" rows={5} defaultValue={task.description}
        onBlur={(e) => e.target.value !== task.description && update.mutate({ description: e.target.value })} />

      <div className="grid grid-cols-2 gap-3">
        <Select label="Status" value={task.status} onChange={(e) => update.mutate({ status: e.target.value as TaskStatus })}>
          {Object.entries(TASK_STATUS).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
        </Select>
        <Select label="Priority" value={task.priority} onChange={(e) => update.mutate({ priority: e.target.value as Priority })}>
          {Object.entries(PRIORITY).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
        </Select>
        <Input label="Due date" type="date" value={toDateInput(task.dueDate)} hint={dueLabel(task.dueDate)} onChange={(e) => update.mutate({ dueDate: fromDateInput(e.target.value) })} />
        <Field label="Assignee">{(id) => (
          <Select id={id} value={task.assigneeId ?? ''} onChange={(e) => update.mutate({ assigneeId: e.target.value || null, reservedById: null })}>
            <option value="">Unassigned</option>
            {members.map((m) => <option key={m.userId} value={m.userId}>{m.user.name}</option>)}
          </Select>
        )}</Field>
      </div>
    </div>
  )
}

export function TaskDrawer({ taskId, projectId, onClose }: { taskId: string | null; projectId: string | null; onClose: () => void }) {
  const confirm = useConfirm()
  const query = useQuery({ queryKey: ['tasks', projectId], queryFn: () => taskApi.list(projectId!), enabled: !!projectId })
  const found = query.data?.find((t) => t.id === taskId)
  // Keep the last task rendered while the drawer slides away.
  const last = useRef<Task | undefined>(undefined)
  if (found) last.current = found
  const task = found ?? last.current
  const remove = useAction((id: string) => taskApi.remove(id), { success: 'Task moved to trash', onSuccess: onClose })
  return (
    <Drawer open={!!taskId} onClose={onClose} title="Task details"
      footer={task && <>
        <Button variant="ghost" className="text-danger" icon={<Trash2 size={15} />} onClick={async () => {
          if (await confirm({ title: 'Move task to trash?', message: `“${task.title}” can be restored for 30 days.`, confirmLabel: 'Move to trash', danger: true })) remove.mutate(task.id)
        }}>Delete</Button>
        <span className="ml-auto text-xs text-muted">Changes save automatically</span>
      </>}>
      {task && task.id === taskId ? <Body key={task.id} task={task} />
        : <div className="space-y-3" role="status" aria-label="Loading task"><Skeleton className="h-8 w-3/4" /><Skeleton className="h-28" /><Skeleton className="h-32" /></div>}
    </Drawer>
  )
}
