import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { AnimatePresence, motion } from 'framer-motion'
import { FolderKanban, Plus, Search } from 'lucide-react'
import { projectApi } from '../../api/services'
import type { ProjectStatus } from '../../api/types'
import { useAction, usePeople, useWorkspace } from '../../lib/data'
import { fmtDate, fromDateInput } from '../../lib/utils'
import { useEdition } from '../../editions'
import { AvatarStack, Badge, Button, Card, EmptyState, ErrorState, Field, FormError, Input, PageHeader, PROJECT_STATUS, Progress, Segmented, Select, SkeletonGrid, Textarea, hueStyle } from '../../components/ui/core'
import { Modal } from '../../components/ui/overlay'

const HUES = [22, 40, 95, 150, 205, 265, 340]

export function Projects() {
  const { workspace } = useWorkspace()
  const edition = useEdition()
  const people = usePeople()
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()
  const [filter, setFilter] = useState<'all' | ProjectStatus>('all')
  const [search, setSearch] = useState('')
  const empty = { name: '', description: '', status: 'planning' as ProjectStatus, due: '', hue: 22 }
  const [form, setForm] = useState(empty)
  const creating = params.get('new') === '1'
  const setCreating = (v: boolean) => setParams(v ? { new: '1' } : {}, { replace: true })
  const query = useQuery({ queryKey: ['projects', workspace.id], queryFn: () => projectApi.list(workspace.id) })
  const create = useAction(
    () => projectApi.create(workspace.id, { name: form.name, description: form.description, status: form.status, hue: form.hue, dueDate: fromDateInput(form.due) ?? undefined }),
    { success: 'Project created', silentError: true, onSuccess: (p) => { setForm(empty); navigate(`/projects/${p.id}`) } },
  )
  const all = query.data ?? []
  const shown = all.filter((p) => (filter === 'all' || p.status === filter) && p.name.toLowerCase().includes(search.toLowerCase()))
  const table = edition.id === 'almanac'

  return (
    <>
      <PageHeader eyebrow={workspace.name} title="Projects" description="Every initiative your team is working on."
        actions={<Button variant="primary" icon={<Plus size={15} />} onClick={() => setCreating(true)}>New project</Button>} />
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <Segmented label="Filter by status" value={filter} onChange={setFilter} options={[
          { value: 'all', label: 'All', count: all.length },
          ...Object.entries(PROJECT_STATUS).map(([k, v]) => ({ value: k as ProjectStatus, label: v.label, count: all.filter((p) => p.status === k).length })),
        ]} />
        <label className="search-box"><Search size={15} /><input aria-label="Search projects" placeholder="Search projects" value={search} onChange={(e) => setSearch(e.target.value)} /></label>
      </div>

      {query.isError ? <ErrorState error={query.error} retry={() => query.refetch()} />
        : query.isPending ? <SkeletonGrid item="h-48" />
        : !shown.length ? <div className="card"><EmptyState icon={<FolderKanban size={20} />} title="No projects here" action={<Button onClick={() => setCreating(true)}>Create a project</Button>}>Try another filter, or start something new.</EmptyState></div>
        : (
          <motion.div layout className={table ? 'border-t-2 border-ink' : 'grid gap-4 sm:grid-cols-2 xl:grid-cols-3'}>
            <AnimatePresence mode="popLayout">
              {shown.map((p, i) => {
                const value = p.taskCount ? (p.doneCount / p.taskCount) * 100 : 0
                const status = <Badge tone={PROJECT_STATUS[p.status].tone} dot>{PROJECT_STATUS[p.status].label}</Badge>
                const team = <AvatarStack users={p.memberIds.map((id) => people.get(id))} />
                return (
                  <motion.div key={p.id} layout initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0, transition: { delay: i * edition.stagger } }} exit={{ opacity: 0, scale: 0.96 }} transition={edition.spring}>
                    <Link to={`/projects/${p.id}`} className="block h-full">
                      {table ? (
                        <div className="headline-row grid grid-cols-1 items-center gap-x-6 gap-y-2 border-b border-line md:grid-cols-[minmax(0,1fr)_7rem_9rem_6rem_4rem]">
                          <span><span className="display block text-xl">{p.name}</span><span className="text-sm text-muted">{p.description}</span></span>
                          <span>{status}</span>
                          <span><Progress value={value} className="!h-[3px]" /><span className="text-xs text-muted">{p.doneCount}/{p.taskCount} tasks</span></span>
                          {team}
                          <span className="text-sm text-muted md:text-right">{fmtDate(p.dueDate)}</span>
                        </div>
                      ) : (
                        <Card hover className="flex h-full flex-col gap-3 p-5">
                          <div className="flex items-start justify-between gap-2"><span className="project-dot" style={hueStyle(p.hue)}><FolderKanban size={16} /></span>{status}</div>
                          <div><h2 className="card-title">{p.name}</h2><p className="mt-1 line-clamp-2 text-sm text-muted">{p.description}</p></div>
                          <div className="mt-auto space-y-2.5 pt-1">
                            <div className="flex justify-between text-xs text-muted"><span>{p.doneCount} of {p.taskCount} tasks</span><span>Due {fmtDate(p.dueDate)}</span></div>
                            <Progress value={value} hue={p.hue} />
                            {team}
                          </div>
                        </Card>
                      )}
                    </Link>
                  </motion.div>
                )
              })}
            </AnimatePresence>
          </motion.div>
        )}

      <Modal open={creating} onClose={() => { setCreating(false); create.reset() }} onSubmit={() => create.mutate()} title="New project"
        footer={<><Button variant="ghost" onClick={() => setCreating(false)}>Cancel</Button><Button type="submit" variant="primary" loading={create.isPending}>Create project</Button></>}>
        <Input label="Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} placeholder="e.g. Spring launch" data-autofocus />
        <Textarea label="Description" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
        <div className="grid grid-cols-2 gap-3">
          <Select label="Status" value={form.status} onChange={(e) => setForm({ ...form, status: e.target.value as ProjectStatus })}>
            {Object.entries(PROJECT_STATUS).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
          </Select>
          <Input label="Due date" type="date" value={form.due} onChange={(e) => setForm({ ...form, due: e.target.value })} />
        </div>
        <Field label="Colour">{() => (
          <div className="flex gap-2">{HUES.map((h) => (
            <button key={h} type="button" aria-label={`Colour ${h}`} aria-pressed={form.hue === h} onClick={() => setForm({ ...form, hue: h })} className={`swatch ${form.hue === h ? 'swatch-on' : ''}`} style={hueStyle(h)} />
          ))}</div>
        )}</Field>
        <FormError error={create.error} />
      </Modal>
    </>
  )
}
