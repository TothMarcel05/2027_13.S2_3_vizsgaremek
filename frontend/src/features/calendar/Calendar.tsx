import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { AnimatePresence, motion } from 'framer-motion'
import { ChevronLeft, ChevronRight, Plus, Trash2 } from 'lucide-react'
import { calendarApi, projectApi } from '../../api/services'
import type { CalendarEvent, Project } from '../../api/types'
import { useAction, useWorkspace } from '../../lib/data'
import { addDays, cn, fmtDate, fmtTime, sameDay, startOfDay, toDateTimeInput, useMediaQuery } from '../../lib/utils'
import { Button, ErrorState, FormError, Input, PageHeader, Segmented, Select, Textarea, hueStyle } from '../../components/ui/core'
import { Modal, useConfirm } from '../../components/ui/overlay'

const HOUR_PX = 52
const WEEKDAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']
const monday = (d: Date) => addDays(startOfDay(d), -((d.getDay() + 6) % 7))

interface Draft { id?: string; title: string; description: string; location: string; projectId: string; start: string; end: string }
const draftFor = (day: Date, hour = 10): Draft => {
  const start = new Date(day.getFullYear(), day.getMonth(), day.getDate(), hour)
  return { title: '', description: '', location: '', projectId: '', start: toDateTimeInput(start), end: toDateTimeInput(new Date(start.getTime() + 3_600_000)) }
}
const draftOf = (e: CalendarEvent): Draft => ({ id: e.id, title: e.title, description: e.description, location: e.location, projectId: e.projectId ?? '', start: toDateTimeInput(new Date(e.start)), end: toDateTimeInput(new Date(e.end)) })

/** Events that overlap share the column width side by side. */
function layoutDay(events: CalendarEvent[]) {
  const lanes: number[] = []
  const placed = events.map((e) => {
    let lane = lanes.findIndex((end) => end <= Date.parse(e.start))
    if (lane === -1) lane = lanes.length
    lanes[lane] = Date.parse(e.end)
    return { e, lane }
  })
  return placed.map((p) => ({ ...p, lanes: lanes.length }))
}

export function CalendarView() {
  const { workspace } = useWorkspace()
  const narrow = useMediaQuery('(max-width: 640px)')
  const confirm = useConfirm()
  const [mode, setMode] = useState<'month' | 'week'>('month')
  const [cursor, setCursor] = useState(() => startOfDay(new Date()))
  const [direction, setDirection] = useState(1)
  const [projectId, setProjectId] = useState('')
  const [draft, setDraft] = useState<Draft | null>(null)
  const [last, setLast] = useState<Draft>(() => draftFor(new Date()))
  const open = (d: Draft) => { setDraft(d); setLast(d); save.reset() }
  const form = draft ?? last
  const patch = (p: Partial<Draft>) => { setDraft({ ...form, ...p }); setLast({ ...form, ...p }) }

  const first = mode === 'month' ? monday(new Date(cursor.getFullYear(), cursor.getMonth(), 1)) : monday(cursor)
  const monthDays = new Date(cursor.getFullYear(), cursor.getMonth() + 1, 0).getDate()
  const offset = (new Date(cursor.getFullYear(), cursor.getMonth(), 1).getDay() + 6) % 7
  const count = mode === 'month' ? Math.ceil((offset + monthDays) / 7) * 7 : 7
  const days = Array.from({ length: count }, (_, i) => addDays(first, i))
  const from = first.toISOString(), to = addDays(first, count).toISOString()

  const projects = useQuery({ queryKey: ['projects', workspace.id], queryFn: () => projectApi.list(workspace.id) }).data ?? []
  const query = useQuery({
    queryKey: ['events', workspace.id, from, to, projectId],
    queryFn: () => calendarApi.list(workspace.id, from, to, projectId || undefined),
    placeholderData: (prev) => prev,
  })
  const events = query.data ?? []
  const on = (day: Date) => events.filter((e) => sameDay(new Date(e.start), day))
  const hue = (e: CalendarEvent, list: Project[] = projects) => list.find((p) => p.id === e.projectId)?.hue ?? 38

  const save = useAction(() => {
    const data = { title: form.title, description: form.description, location: form.location, projectId: form.projectId || null,
      start: form.start ? new Date(form.start).toISOString() : '', end: form.end ? new Date(form.end).toISOString() : '' }
    return form.id ? calendarApi.update(form.id, data) : calendarApi.create(workspace.id, data)
  }, { success: 'Event saved', silentError: true, onSuccess: () => setDraft(null) })
  const remove = useAction((id: string) => calendarApi.remove(id), { success: 'Event moved to trash', onSuccess: () => setDraft(null) })

  const step = (n: number) => {
    setDirection(n)
    setCursor(mode === 'month' ? new Date(cursor.getFullYear(), cursor.getMonth() + n, 1) : addDays(cursor, n * 7))
  }
  const heading = mode === 'month'
    ? cursor.toLocaleDateString('en-GB', { month: 'long', year: 'numeric' })
    : `${fmtDate(days[0].toISOString())} – ${fmtDate(days[6].toISOString(), { day: 'numeric', month: 'short', year: 'numeric' })}`

  // The week grid stretches to fit anything outside office hours.
  const startHour = Math.min(8, ...events.map((e) => new Date(e.start).getHours()))
  const endHour = Math.max(19, ...events.map((e) => new Date(e.end).getHours() + 1))
  const hours = Array.from({ length: Math.min(24, endHour) - startHour }, (_, i) => startHour + i)

  const chip = (e: CalendarEvent) => (
    <motion.button layout key={e.id} type="button" className="event-chip" style={hueStyle(hue(e))} onClick={(ev) => { ev.stopPropagation(); open(draftOf(e)) }} initial={{ opacity: 0, scale: 0.9 }} animate={{ opacity: 1, scale: 1 }}>
      <span className="opacity-70 max-md:hidden">{fmtTime(e.start)}</span> <span className="truncate">{e.title}</span>
    </motion.button>
  )

  return (
    <>
      <PageHeader eyebrow={workspace.name} title="Calendar" description="Deadlines, reviews and everything in between."
        actions={<Button variant="primary" icon={<Plus size={15} />} onClick={() => open(draftFor(new Date()))}>New event</Button>} />
      <div className="mb-4 flex flex-wrap items-center gap-3">
        <div className="flex items-center gap-1">
          <Button variant="ghost" size="sm" icon={<ChevronLeft size={17} />} aria-label={`Previous ${mode}`} onClick={() => step(-1)} />
          <Button variant="outline" size="sm" onClick={() => setCursor(startOfDay(new Date()))}>Today</Button>
          <Button variant="ghost" size="sm" icon={<ChevronRight size={17} />} aria-label={`Next ${mode}`} onClick={() => step(1)} />
        </div>
        <h2 className="display text-2xl" aria-live="polite">{heading}</h2>
        <div className="ml-auto flex flex-wrap items-center gap-2">
          <Select aria-label="Filter by project" className="!w-auto max-w-48" value={projectId} onChange={(e) => setProjectId(e.target.value)}>
            <option value="">All projects</option>
            {projects.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
          </Select>
          <Segmented label="Calendar view" value={mode} onChange={setMode} options={[{ value: 'month', label: 'Month' }, { value: 'week', label: 'Week' }]} />
        </div>
      </div>

      {query.isError ? <ErrorState error={query.error} retry={() => query.refetch()} /> : (
        <div className="card relative overflow-hidden">
          {query.isFetching && <div className="loading-line" />}
          <AnimatePresence mode="wait" initial={false}>
            <motion.div key={`${mode}-${from}`} initial={{ opacity: 0, x: direction * 24 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, x: direction * -24 }} transition={{ duration: 0.2 }}>
              {mode === 'month' ? (
                <div className="grid grid-cols-7">
                  {WEEKDAYS.map((d) => <div key={d} className="eyebrow border-b border-line px-2 py-2 text-center">{d}</div>)}
                  {days.map((day) => {
                    const list = on(day)
                    const limit = narrow ? 2 : 3
                    return (
                      <div key={day.toISOString()} className={cn('day-cell', day.getMonth() !== cursor.getMonth() && 'day-out')} onClick={() => open(draftFor(day))}>
                        <button type="button" className={cn('day-num', sameDay(day, new Date()) && 'day-today')} aria-label={`Add event on ${fmtDate(day.toISOString(), { day: 'numeric', month: 'long' })}`}>{day.getDate()}</button>
                        {list.slice(0, limit).map(chip)}
                        {list.length > limit && <button type="button" className="link text-left text-[0.68rem]" onClick={(e) => { e.stopPropagation(); setCursor(day); setMode('week') }}>+{list.length - limit} more</button>}
                      </div>
                    )
                  })}
                </div>
              ) : narrow ? (
                <ul className="divide-y divide-line">
                  {days.map((day) => (
                    <li key={day.toISOString()} className="flex gap-3 p-3">
                      <button type="button" className={cn('day-num !size-9 shrink-0', sameDay(day, new Date()) && 'day-today')} onClick={() => open(draftFor(day))} aria-label={`Add event on ${fmtDate(day.toISOString())}`}>{day.getDate()}</button>
                      <div className="flex min-w-0 flex-1 flex-col gap-1"><span className="eyebrow">{fmtDate(day.toISOString(), { weekday: 'long' })}</span>{on(day).map(chip)}{!on(day).length && <span className="text-xs text-muted">No events</span>}</div>
                    </li>
                  ))}
                </ul>
              ) : (
                <div className="overflow-x-auto">
                  <div className="min-w-[44rem]">
                    <div className="grid grid-cols-[3.5rem_repeat(7,1fr)] border-b border-line">
                      <span />
                      {days.map((d, i) => <div key={i} className="border-l border-line py-2 text-center"><span className="eyebrow">{WEEKDAYS[i]}</span><span className={cn('day-num mx-auto mt-1 !flex', sameDay(d, new Date()) && 'day-today')}>{d.getDate()}</span></div>)}
                    </div>
                    <div className="grid grid-cols-[3.5rem_repeat(7,1fr)]">
                      <div>{hours.map((h) => <div key={h} style={{ height: HOUR_PX }} className="pr-2 pt-1 text-right text-[0.68rem] text-muted">{String(h).padStart(2, '0')}:00</div>)}</div>
                      {days.map((day) => (
                        <div key={day.toISOString()} className="relative border-l border-line">
                          {hours.map((h) => <button key={h} type="button" tabIndex={-1} aria-label={`Add event ${fmtDate(day.toISOString())} at ${h}:00`} style={{ height: HOUR_PX }} className="hour-slot" onClick={() => open(draftFor(day, h))} />)}
                          {layoutDay(on(day)).map(({ e, lane, lanes }) => {
                            const s = new Date(e.start)
                            const top = (s.getHours() - startHour + s.getMinutes() / 60) * HOUR_PX
                            const height = Math.max(24, Math.min((Date.parse(e.end) - s.getTime()) / 3_600_000 * HOUR_PX, hours.length * HOUR_PX - top) - 3)
                            return (
                              <motion.button key={e.id} type="button" className="week-event" style={{ ...hueStyle(hue(e)), top, height, left: `calc(${(lane / lanes) * 100}% + 2px)`, width: `calc(${100 / lanes}% - 4px)` }} onClick={() => open(draftOf(e))} initial={{ opacity: 0, y: 6 }} animate={{ opacity: 1, y: 0 }} whileHover={{ scale: 1.02 }}>
                                <strong className="block truncate">{e.title}</strong><span className="opacity-70">{fmtTime(e.start)}</span>
                              </motion.button>
                            )
                          })}
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              )}
            </motion.div>
          </AnimatePresence>
        </div>
      )}

      <Modal open={!!draft} onClose={() => setDraft(null)} onSubmit={() => save.mutate()} title={form.id ? 'Edit event' : 'New event'}
        footer={<>
          {form.id && <Button variant="ghost" className="mr-auto text-danger" icon={<Trash2 size={15} />} onClick={async () => {
            if (await confirm({ title: 'Delete this event?', message: `“${form.title}” can be restored from the trash for 30 days.`, confirmLabel: 'Delete event', danger: true })) remove.mutate(form.id!)
          }}>Delete</Button>}
          <Button variant="ghost" onClick={() => setDraft(null)}>Cancel</Button>
          <Button type="submit" variant="primary" loading={save.isPending}>{form.id ? 'Save changes' : 'Create event'}</Button>
        </>}>
        <Input label="Title" value={form.title} onChange={(e) => patch({ title: e.target.value })} data-autofocus />
        <div className="grid gap-3 sm:grid-cols-2">
          <Input label="Starts" type="datetime-local" value={form.start} onChange={(e) => {
            const length = Date.parse(form.end) - Date.parse(form.start)
            const next = Date.parse(e.target.value)
            patch({ start: e.target.value, ...(next && length > 0 ? { end: toDateTimeInput(new Date(next + length)) } : {}) })
          }} />
          <Input label="Ends" type="datetime-local" value={form.end} onChange={(e) => patch({ end: e.target.value })} />
        </div>
        <Select label="Project" value={form.projectId} onChange={(e) => patch({ projectId: e.target.value })}>
          <option value="">No project</option>
          {projects.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
        </Select>
        <Input label="Location" value={form.location} onChange={(e) => patch({ location: e.target.value })} placeholder="Room or call link" />
        <Textarea label="Notes" value={form.description} onChange={(e) => patch({ description: e.target.value })} />
        <FormError error={save.error} />
      </Modal>
    </>
  )
}
