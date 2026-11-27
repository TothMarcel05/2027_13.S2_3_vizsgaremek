import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { AnimatePresence, motion } from 'framer-motion'
import { CalendarDays, CheckSquare, File, Folder, FolderKanban, RotateCcw, Trash2, X } from 'lucide-react'
import { trashApi } from '../../api/services'
import type { TrashItem } from '../../api/types'
import { useAction, usePeople, useWorkspace } from '../../lib/data'
import { fmtDate, timeAgo } from '../../lib/utils'
import { Button, EmptyState, ErrorState, PageHeader, Segmented, SkeletonGrid } from '../../components/ui/core'
import { useConfirm } from '../../components/ui/overlay'

const ICONS = { project: FolderKanban, task: CheckSquare, file: File, folder: Folder, event: CalendarDays }
export const daysLeft = (item: TrashItem) => Math.max(0, Math.ceil(30 - (Date.now() - Date.parse(item.deletedAt)) / 86_400_000))

function Countdown({ days }: { days: number }) {
  const urgent = days <= 5
  return (
    <span className="relative grid size-11 shrink-0 place-items-center" title={`${days} days until permanent deletion`}>
      <svg viewBox="0 0 36 36" className="absolute inset-0 -rotate-90" aria-hidden>
        <circle cx="18" cy="18" r="15.5" fill="none" stroke="var(--line)" strokeWidth="3" />
        <motion.circle cx="18" cy="18" r="15.5" fill="none" stroke={urgent ? 'var(--danger)' : 'var(--accent)'} strokeWidth="3" strokeLinecap="round" initial={{ pathLength: 0 }} animate={{ pathLength: days / 30 }} transition={{ duration: 0.9, ease: 'easeOut' }} />
      </svg>
      <span className={`text-xs font-semibold ${urgent ? 'text-danger' : ''}`}>{days}d</span>
    </span>
  )
}

export function Trash() {
  const { workspace } = useWorkspace()
  const people = usePeople()
  const confirm = useConfirm()
  const [filter, setFilter] = useState<'all' | TrashItem['type']>('all')
  const query = useQuery({ queryKey: ['trash', workspace.id], queryFn: () => trashApi.list(workspace.id) })
  const restore = useAction((item: TrashItem) => trashApi.restore(item.id), { success: 'Restored' })
  const purge = useAction((id: string) => trashApi.purge(id), { success: 'Deleted permanently' })
  const all = query.data ?? []
  const shown = all.filter((i) => filter === 'all' || i.type === filter).sort((a, b) => daysLeft(a) - daysLeft(b))
  const types = [...new Set(all.map((i) => i.type))]
  return (
    <>
      <PageHeader eyebrow={workspace.name} title="Trash" description="Deleted items stay here for 30 days, then they’re removed for good." />
      <div className="mb-4">
        <Segmented label="Filter by type" value={filter} onChange={setFilter} options={[
          { value: 'all', label: 'All', count: all.length },
          ...types.map((t) => ({ value: t, label: <span className="capitalize">{t}s</span>, count: all.filter((i) => i.type === t).length })),
        ]} />
      </div>
      {query.isError ? <ErrorState error={query.error} retry={() => query.refetch()} />
        : query.isPending ? <SkeletonGrid count={4} className="space-y-2.5" item="h-16" />
        : !shown.length ? <div className="card"><EmptyState icon={<Trash2 size={20} />} title="Trash is empty">Deleted projects, tasks, files and events show up here.</EmptyState></div>
        : (
          <ul className="flex flex-col gap-2.5">
            <AnimatePresence>
              {shown.map((item, i) => {
                const Icon = ICONS[item.type]
                return (
                  <motion.li key={item.id} layout initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0, transition: { delay: i * 0.04 } }} exit={{ opacity: 0, x: 60 }} className="row-card flex flex-wrap items-center gap-3 p-3">
                    <span className="empty-icon !size-10"><Icon size={17} /></span>
                    <div className="min-w-0 flex-1 basis-40">
                      <p className="truncate text-sm font-semibold">{item.name}</p>
                      <p className="text-xs text-muted"><span className="capitalize">{item.type}</span> · deleted {timeAgo(item.deletedAt)} by {people.get(item.deletedBy)?.name ?? 'a former member'} · removed on {fmtDate(new Date(Date.parse(item.deletedAt) + 30 * 86_400_000).toISOString())}</p>
                    </div>
                    <Countdown days={daysLeft(item)} />
                    <Button variant="outline" size="sm" icon={<RotateCcw size={14} />} loading={restore.isPending && restore.variables?.id === item.id} onClick={() => restore.mutate(item)}>Restore</Button>
                    <Button variant="ghost" size="sm" icon={<X size={15} />} aria-label={`Delete ${item.name} permanently`} title="Delete permanently" onClick={async () => {
                      if (await confirm({ title: 'Delete permanently?', message: `“${item.name}” will be gone for good. This can’t be undone.`, confirmLabel: 'Delete forever', danger: true })) purge.mutate(item.id)
                    }} />
                  </motion.li>
                )
              })}
            </AnimatePresence>
          </ul>
        )}
    </>
  )
}
