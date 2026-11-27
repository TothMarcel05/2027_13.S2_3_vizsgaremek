import { useEffect, useRef, useState, type FormEvent } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { AnimatePresence, motion } from 'framer-motion'
import { ArrowLeft, Download, FileText, Hash, Paperclip, Search, SendHorizontal, Users, X, MessagesSquare } from 'lucide-react'
import { chatApi } from '../../api/services'
import type { Attachment, Channel, ChannelKind, Message } from '../../api/types'
import { useAction, useMe, usePeople, useWorkspace } from '../../lib/data'
import { cn, fmtBytes, fmtDate, fmtTime, sameDay, saveBlob, timeAgo } from '../../lib/utils'
import { MOTION } from '../../lib/motion'
import { Avatar, AvatarStack, Button, EmptyState, ErrorState, Skeleton } from '../../components/ui/core'
import { useToast } from '../../components/ui/overlay'

const GROUPS: { kind: ChannelKind; label: string }[] = [
  { kind: 'project_channel', label: 'Project channels' }, { kind: 'group', label: 'Group rooms' }, { kind: 'direct', label: 'Direct messages' },
]
const MAX_ATTACHMENT = 25e6

function ChannelIcon({ channel }: { channel: Channel }) {
  const people = usePeople()
  const me = useMe()
  if (channel.kind === 'direct') return <Avatar user={people.get(channel.memberIds.find((id) => id !== me?.id) ?? '')} size={26} />
  return <span className="channel-icon">{channel.kind === 'group' ? <Users size={13} /> : <Hash size={13} />}</span>
}

function AttachmentCard({ a, onRemove }: { a: Attachment; onRemove?: () => void }) {
  const toast = useToast()
  const image = a.type.startsWith('image/')
  const download = async () => {
    if (!a.url) return toast('Previews of seeded demo files aren’t available.', 'error')
    saveBlob(await (await fetch(a.url)).blob(), a.name)
  }
  return (
    <div className="attachment">
      {image && (a.url ? <img src={a.url} alt={a.name} className="h-28 w-full object-cover" /> : <div className="image-placeholder h-28" role="img" aria-label={`Preview of ${a.name}`} />)}
      <div className="flex items-center gap-2 px-2.5 py-2">
        <FileText size={16} className="shrink-0 text-accent" />
        <span className="min-w-0 flex-1"><span className="block truncate text-xs font-medium">{a.name}</span><span className="text-[0.7rem] text-muted">{fmtBytes(a.size)}</span></span>
        {onRemove
          ? <button type="button" className="icon-btn !size-6" onClick={onRemove} aria-label={`Remove ${a.name}`}><X size={13} /></button>
          : <button type="button" className="icon-btn !size-7" onClick={download} aria-label={`Download ${a.name}`}><Download size={14} /></button>}
      </div>
    </div>
  )
}

function Thread({ channel, onBack }: { channel: Channel; onBack: () => void }) {
  const client = useQueryClient()
  const people = usePeople()
  const me = useMe()
  const [text, setText] = useState('')
  const [pending, setPending] = useState<Attachment[]>([])
  const [typing, setTyping] = useState<string | null>(null)
  const [over, setOver] = useState(false)
  const scroller = useRef<HTMLDivElement>(null)
  const fileInput = useRef<HTMLInputElement>(null)
  const toast = useToast()
  const query = useQuery({ queryKey: ['messages', channel.id], queryFn: () => chatApi.messages(channel.id) })
  const refresh = () => Promise.all([client.invalidateQueries({ queryKey: ['messages', channel.id] }), client.invalidateQueries({ queryKey: ['channels'] })])

  useEffect(() => chatApi.subscribe(channel.id, (e) => {
    if (e.type === 'typing') setTyping(e.userId)
    else { setTyping(null); refresh() }
  }), [channel.id])

  const markRead = useAction(() => chatApi.markRead(channel.id))
  useEffect(() => { if (channel.unread) markRead.mutate() }, [channel.id, channel.unread])

  useEffect(() => { scroller.current?.scrollTo({ top: scroller.current.scrollHeight, behavior: 'smooth' }) }, [query.data?.length, typing])

  const send = useAction((v: { body: string; files: Attachment[] }) => chatApi.send(channel.id, v.body, v.files))
  const submit = (e?: FormEvent) => {
    e?.preventDefault()
    if (!text.trim() && !pending.length) return
    send.mutate({ body: text.trim(), files: pending }, { onError: () => { setText(text); setPending(pending) } })
    setText(''); setPending([])
  }
  const attach = (list: FileList | null) => {
    const files = [...(list ?? [])]
    if (files.some((f) => f.size > MAX_ATTACHMENT)) toast('Attachments are limited to 25 MB each.', 'error')
    setPending((p) => [...p, ...files.filter((f) => f.size <= MAX_ATTACHMENT).map((f) => ({ name: f.name, size: f.size, type: f.type, url: URL.createObjectURL(f) }))])
  }

  const messages: Message[] = query.data ?? []
  const title = channel.kind === 'project_channel' ? `# ${channel.name}` : channel.name
  return (
    <section className="relative flex min-w-0 flex-1 flex-col" aria-label={title}
      onDragOver={(e) => { if (e.dataTransfer.types.includes('Files')) { e.preventDefault(); setOver(true) } }}
      onDragLeave={(e) => { if (!e.currentTarget.contains(e.relatedTarget as Node)) setOver(false) }}
      onDrop={(e) => { e.preventDefault(); setOver(false); attach(e.dataTransfer.files) }}>
      {over && <div className="drop-overlay">Drop to attach</div>}
      <header className="flex items-center gap-3 border-b border-line px-4 py-3">
        <button type="button" className="icon-btn md:hidden" onClick={onBack} aria-label="Back to conversations"><ArrowLeft size={18} /></button>
        <ChannelIcon channel={channel} />
        <div className="min-w-0 flex-1"><h2 className="section-title truncate">{title}</h2>{channel.topic && <p className="truncate text-xs text-muted">{channel.topic}</p>}</div>
        <span className="max-sm:hidden"><AvatarStack users={channel.memberIds.map((id) => people.get(id))} /></span>
      </header>

      <div ref={scroller} className="flex-1 overflow-y-auto px-4 pb-32 pt-4" role="log">
        {query.isPending ? <div className="space-y-4">{[60, 45, 70, 40].map((w) => <div key={w} className="flex gap-3"><Skeleton className="size-8 !rounded-full" /><div style={{ width: `${w}%` }}><Skeleton className="h-12" /></div></div>)}</div>
          : query.isError ? <ErrorState error={query.error} retry={() => query.refetch()} />
          : !messages.length ? <EmptyState icon={<MessagesSquare size={20} />} title="Say hello">This is the start of the conversation.</EmptyState>
          : messages.map((m, i) => {
            const previous = messages[i - 1]
            const newDay = !previous || !sameDay(new Date(previous.createdAt), new Date(m.createdAt))
            const grouped = !newDay && previous.authorId === m.authorId && Date.parse(m.createdAt) - Date.parse(previous.createdAt) < 3_600_000
            const author = people.get(m.authorId)
            const mine = m.authorId === me?.id
            return (
              <div key={m.id}>
                {newDay && <div className="day-sep"><span>{sameDay(new Date(m.createdAt), new Date()) ? 'Today' : fmtDate(m.createdAt, { weekday: 'long', day: 'numeric', month: 'short' })}</span></div>}
                <motion.div className={cn('msg', mine && 'msg-mine', grouped ? 'mt-1' : 'mt-4')} initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} transition={MOTION.spring}>
                  <span className="msg-avatar">{!grouped && <Avatar user={author} size={32} />}</span>
                  <div className="msg-body">
                    {!grouped && <p className="msg-meta"><strong>{mine ? 'You' : author?.name ?? 'Former member'}</strong><span>{fmtTime(m.createdAt)}</span></p>}
                    {m.body && <p className="bubble" title={fmtTime(m.createdAt)}>{m.body}</p>}
                    {m.attachments.length > 0 && <div className="mt-1.5 flex flex-wrap items-start gap-2">{m.attachments.map((a, n) => <AttachmentCard key={n} a={a} />)}</div>}
                  </div>
                </motion.div>
              </div>
            )
          })}
        <AnimatePresence>
          {typing && (
            <motion.div className="mt-3 flex items-center gap-2 text-xs text-muted" initial={{ opacity: 0, y: 6 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }}>
              <span className="typing"><i /><i /><i /></span>{people.get(typing)?.name ?? 'Someone'} is typing
            </motion.div>
          )}
        </AnimatePresence>
      </div>

      <motion.form className="composer" onSubmit={submit} layout="position">
        {pending.length > 0 && <div className="flex flex-wrap gap-2 px-2 pt-2">{pending.map((a, i) => <AttachmentCard key={a.url} a={a} onRemove={() => setPending(pending.filter((_, n) => n !== i))} />)}</div>}
        <div className="flex items-end gap-1.5 p-1.5">
          <input ref={fileInput} type="file" multiple hidden onChange={(e) => { attach(e.target.files); e.target.value = '' }} />
          <Button variant="ghost" icon={<Paperclip size={17} />} aria-label="Attach files" onClick={() => fileInput.current?.click()} />
          <textarea rows={1} aria-label={`Message ${title}`} placeholder={`Message ${title}`} className="composer-input" value={text}
            onChange={(e) => setText(e.target.value)} onKeyDown={(e) => { if (e.key === 'Enter' && !e.shiftKey && !e.nativeEvent.isComposing) { e.preventDefault(); submit() } }} />
          <Button type="submit" variant="primary" icon={<SendHorizontal size={16} />} aria-label="Send message" disabled={!text.trim() && !pending.length} />
        </div>
      </motion.form>
    </section>
  )
}

export function Chat() {
  const { workspace } = useWorkspace()
  const [selected, setSelected] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [mobileThread, setMobileThread] = useState(false)
  const query = useQuery({ queryKey: ['channels', workspace.id], queryFn: () => chatApi.channels(workspace.id) })
  const channels = query.data ?? []
  const active = channels.find((c) => c.id === selected) ?? channels[0]
  return (
    <div className="chat-frame">
      <h1 className="sr-only">Team chat</h1>
      <aside className={cn('chat-list', mobileThread && 'max-md:hidden')}>
        <div className="border-b border-line p-3">
          <label className="search-box !w-full"><Search size={14} /><input aria-label="Find a conversation" placeholder="Find a conversation" value={search} onChange={(e) => setSearch(e.target.value)} /></label>
        </div>
        <div className="flex-1 overflow-y-auto p-2">
          {query.isPending && <div className="space-y-2">{Array.from({ length: 7 }, (_, i) => <Skeleton key={i} className="h-11" />)}</div>}
          {GROUPS.map((g) => {
            const rows = channels.filter((c) => c.kind === g.kind && c.name.toLowerCase().includes(search.toLowerCase()))
            return rows.length > 0 && (
              <div key={g.kind} className="mb-3">
                <p className="menu-label">{g.label}</p>
                {rows.map((c) => (
                  <button key={c.id} type="button" aria-current={c.id === active?.id} className={cn('channel-row', c.id === active?.id && 'channel-on')} onClick={() => { setSelected(c.id); setMobileThread(true) }}>
                    <ChannelIcon channel={c} />
                    <span className="min-w-0 flex-1">
                      <span className={cn('block truncate text-sm', c.unread ? 'font-semibold' : 'font-medium')}>{c.name}</span>
                      <span className="block truncate text-xs text-muted">{c.lastMessage}</span>
                    </span>
                    {c.unread > 0 ? <span className="count-badge">{c.unread}</span> : <span className="shrink-0 text-[0.68rem] text-muted">{c.lastAt && timeAgo(c.lastAt).replace(' ago', '')}</span>}
                  </button>
                ))}
              </div>
            )
          })}
        </div>
      </aside>
      <div className={cn('flex min-w-0 flex-1', !mobileThread && 'max-md:hidden')}>
        {query.isError ? <ErrorState error={query.error} retry={() => query.refetch()} />
          : active ? <Thread key={active.id} channel={active} onBack={() => setMobileThread(false)} />
          : !query.isPending && <div className="m-auto"><EmptyState icon={<MessagesSquare size={20} />} title="No conversations yet">Channels appear as your team creates projects.</EmptyState></div>}
      </div>
    </div>
  )
}
