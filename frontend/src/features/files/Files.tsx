import { useRef, useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { AnimatePresence, motion } from 'framer-motion'
import { ChevronRight, Download, File, FileArchive, FileImage, FileSpreadsheet, FileText, Folder, FolderPlus, LayoutGrid, List, MoreHorizontal, Search, Share2, Tag, Trash2, UploadCloud, UserPlus, X } from 'lucide-react'
import { fileApi } from '../../api/services'
import type { FileNode, Share, ShareRole } from '../../api/types'
import { useAction, useMe, useMembers, usePeople, useWorkspace } from '../../lib/data'
import { cn, fmtBytes, fmtDate, saveBlob } from '../../lib/utils'
import { MOTION } from '../../lib/motion'
import { Avatar, AvatarStack, Badge, Button, EmptyState, ErrorState, FormError, Input, PageHeader, Segmented, Select, SkeletonGrid } from '../../components/ui/core'
import { Modal, Popover, useConfirm, useToast } from '../../components/ui/overlay'

function kindOf(f: FileNode) {
  if (f.kind === 'folder') return { Icon: Folder, tone: 'amber' }
  const ext = f.name.split('.').pop()?.toLowerCase() ?? ''
  if (f.mime.startsWith('image/') || ['png', 'jpg', 'jpeg', 'svg', 'fig', 'gif', 'webp'].includes(ext)) return { Icon: FileImage, tone: 'accent' }
  if (['xls', 'xlsx', 'csv'].includes(ext)) return { Icon: FileSpreadsheet, tone: 'sage' }
  if (['zip', 'rar', 'gz'].includes(ext)) return { Icon: FileArchive, tone: 'neutral' }
  if (['pdf', 'doc', 'docx', 'txt', 'md'].includes(ext)) return { Icon: FileText, tone: 'blue' }
  return { Icon: File, tone: 'neutral' }
}

const ROLE_HELP: Record<ShareRole, string> = { viewer: 'Can view and download', commenter: 'Can view and comment', editor: 'Can edit and re-share' }

function ShareModal({ node, onClose }: { node: FileNode | null; onClose: () => void }) {
  const members = useMembers().data ?? []
  const people = usePeople()
  const me = useMe()
  const [userId, setUserId] = useState('')
  const [role, setRole] = useState<ShareRole>('viewer')
  const share = useAction((shares: Share[]) => fileApi.share(node!.id, shares), { success: 'Sharing updated' })
  const shares = node?.shares ?? []
  const candidates = members.filter((m) => m.userId !== node?.ownerId && !shares.some((s) => s.userId === m.userId))
  return (
    <Modal open={!!node} onClose={onClose} title={`Share “${node?.name ?? ''}”`} description="Access is granted by role."
      onSubmit={() => { if (userId) { share.mutate([...shares, { userId, role }]); setUserId('') } }}
      footer={<Button variant="primary" onClick={onClose}>Done</Button>}>
      <div className="flex flex-wrap items-end gap-2">
        <div className="min-w-40 flex-1">
          <Select label="Add a teammate" value={userId} onChange={(e) => setUserId(e.target.value)}>
            <option value="">Choose a person…</option>
            {candidates.map((m) => <option key={m.userId} value={m.userId}>{m.user.name}</option>)}
          </Select>
        </div>
        <Select aria-label="Access role" className="!w-auto" value={role} onChange={(e) => setRole(e.target.value as ShareRole)}>
          {Object.keys(ROLE_HELP).map((r) => <option key={r} value={r}>{r}</option>)}
        </Select>
        <Button type="submit" icon={<UserPlus size={15} />} disabled={!userId} loading={share.isPending}>Add</Button>
      </div>
      <p className="-mt-2 text-xs text-muted">{ROLE_HELP[role]}.</p>
      <ul className="divide-y divide-line">
        <li className="flex items-center gap-3 py-2.5">
          <Avatar user={people.get(node?.ownerId ?? '')} size={32} />
          <span className="flex-1 text-sm font-medium">{people.get(node?.ownerId ?? '')?.name}{node?.ownerId === me?.id && ' (you)'}</span><Badge>Owner</Badge>
        </li>
        <AnimatePresence initial={false}>
          {shares.map((s) => (
            <motion.li key={s.userId} layout initial={{ opacity: 0, height: 0 }} animate={{ opacity: 1, height: 'auto' }} exit={{ opacity: 0, height: 0 }} className="flex items-center gap-3 py-2.5">
              <Avatar user={people.get(s.userId)} size={32} />
              <span className="min-w-0 flex-1 truncate text-sm font-medium">{people.get(s.userId)?.name}</span>
              <Select aria-label={`Role for ${people.get(s.userId)?.name}`} className="!w-auto" value={s.role} onChange={(e) => share.mutate(shares.map((x) => (x.userId === s.userId ? { ...x, role: e.target.value as ShareRole } : x)))}>
                {Object.keys(ROLE_HELP).map((r) => <option key={r} value={r}>{r}</option>)}
              </Select>
              <Button size="sm" variant="ghost" icon={<X size={14} />} aria-label={`Remove ${people.get(s.userId)?.name}`} onClick={() => share.mutate(shares.filter((x) => x.userId !== s.userId))} />
            </motion.li>
          ))}
        </AnimatePresence>
      </ul>
    </Modal>
  )
}

export function Files() {
  const { workspace } = useWorkspace()
  const people = usePeople()
  const toast = useToast()
  const confirm = useConfirm()
  const client = useQueryClient()
  const [folderId, setFolderId] = useState<string | null>(null)
  const [view, setView] = useState<'grid' | 'list'>('grid')
  const [tag, setTag] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [over, setOver] = useState(false)
  const [uploads, setUploads] = useState<{ key: string; name: string }[]>([])
  const [folderName, setFolderName] = useState<string | null>(null)
  const [shareId, setShareId] = useState<string | null>(null)
  const [tagging, setTagging] = useState<{ id: string; value: string } | null>(null)
  const input = useRef<HTMLInputElement>(null)
  const query = useQuery({ queryKey: ['files', workspace.id], queryFn: () => fileApi.list(workspace.id) })
  const all = query.data ?? []

  const trail: FileNode[] = []
  for (let node = all.find((f) => f.id === folderId); node; node = all.find((f) => f.id === node!.parentId)) trail.unshift(node)
  const current = trail.at(-1)?.id ?? null
  // Tag and text filters search the whole drive; otherwise show the open folder.
  const filtering = !!tag || !!search
  const shown = all
    .filter((f) => (filtering ? (!tag || f.tags.includes(tag)) && f.name.toLowerCase().includes(search.toLowerCase()) : f.parentId === current))
    .sort((a, b) => (a.kind === b.kind ? a.name.localeCompare(b.name) : a.kind === 'folder' ? -1 : 1))
  const tags = [...new Set(all.flatMap((f) => f.tags))].sort()

  const createFolder = useAction(() => fileApi.createFolder(workspace.id, folderName ?? '', current), { success: 'Folder created', silentError: true, onSuccess: () => setFolderName(null) })
  const remove = useAction((id: string) => fileApi.remove(id), { success: 'Moved to trash' })
  const saveTags = useAction(() => fileApi.update(tagging!.id, { tags: [...new Set(tagging!.value.split(',').map((t) => t.trim().toLowerCase()).filter(Boolean))] }), { success: 'Tags saved', onSuccess: () => setTagging(null) })

  const upload = async (list: FileList | null) => {
    const files = [...(list ?? [])]
    if (!files.length) return
    const target = current
    const queued = files.map((f) => ({ key: `${f.name}-${Math.random()}`, name: f.name }))
    setUploads((u) => [...u, ...queued])
    let ok = 0
    for (const [i, file] of files.entries()) {
      try { await fileApi.upload(workspace.id, file, target); ok++ } catch (e) { toast((e as Error).message, 'error') }
      setUploads((u) => u.filter((x) => x.key !== queued[i].key))
      await client.invalidateQueries()
    }
    if (ok) toast(`${ok} ${ok === 1 ? 'file' : 'files'} uploaded`)
  }
  const download = async (f: FileNode) => {
    try { saveBlob(await fileApi.download(f.id), f.name) } catch (e) { toast((e as Error).message, 'error') }
  }
  const openNode = (f: FileNode) => {
    if (f.kind === 'folder') { setFolderId(f.id); setTag(null); setSearch('') } else download(f)
  }
  const trash = async (f: FileNode) => {
    if (await confirm({ title: `Move ${f.kind} to trash?`, message: `“${f.name}”${f.kind === 'folder' ? ' and everything inside it' : ''} can be restored for 30 days.`, confirmLabel: 'Move to trash', danger: true })) remove.mutate(f.id)
  }

  const menu = (f: FileNode) => (
    <Popover align="right" className="w-44 p-1.5"
      trigger={({ open, toggle }) => <button type="button" className="icon-btn !size-7" aria-expanded={open} aria-label={`Actions for ${f.name}`} onClick={toggle}><MoreHorizontal size={16} /></button>}>
      {(close) => (<>
        {f.kind === 'file' && <button type="button" className="menu-item" onClick={() => { close(); download(f) }}><Download size={15} /> Download</button>}
        <button type="button" className="menu-item" onClick={() => { close(); setShareId(f.id) }}><Share2 size={15} /> Share</button>
        <button type="button" className="menu-item" onClick={() => { close(); setTagging({ id: f.id, value: f.tags.join(', ') }) }}><Tag size={15} /> Edit tags</button>
        <button type="button" className="menu-item text-danger" onClick={() => { close(); trash(f) }}><Trash2 size={15} /> Move to trash</button>
      </>)}
    </Popover>
  )
  const meta = (f: FileNode) => {
    if (f.kind === 'file') return fmtBytes(f.size)
    const n = all.filter((x) => x.parentId === f.id).length
    return `${n} ${n === 1 ? 'item' : 'items'}`
  }

  return (
    <div onDragOver={(e) => { if (e.dataTransfer.types.includes('Files')) { e.preventDefault(); setOver(true) } }}
      onDragLeave={(e) => { if (!e.currentTarget.contains(e.relatedTarget as Node)) setOver(false) }}
      onDrop={(e) => { e.preventDefault(); setOver(false); upload(e.dataTransfer.files) }}>
      <PageHeader eyebrow={`${fmtBytes(workspace.storageUsed)} of ${fmtBytes(workspace.storageLimit)} used`} title="Files" description="Your team’s shared drive."
        actions={<>
          <Button variant="outline" icon={<FolderPlus size={15} />} onClick={() => { createFolder.reset(); setFolderName('') }}>New folder</Button>
          <Button variant="primary" icon={<UploadCloud size={15} />} onClick={() => input.current?.click()}>Upload</Button>
          <input ref={input} type="file" multiple hidden onChange={(e) => { upload(e.target.files); e.target.value = '' }} />
        </>} />

      <motion.button type="button" onClick={() => input.current?.click()} className={cn('dropzone', over && 'dropzone-over')} animate={{ scale: over ? 1.012 : 1 }}>
        <motion.span animate={{ y: over ? -4 : 0 }}><UploadCloud size={22} /></motion.span>
        <span><strong>{over ? 'Release to upload' : 'Drag files here'}</strong> <span className="text-muted">or click to browse · uploads go to {trail.at(-1)?.name ?? 'All files'}</span></span>
      </motion.button>
      <AnimatePresence>
        {uploads.map((u) => (
          <motion.div key={u.key} initial={{ opacity: 0, height: 0 }} animate={{ opacity: 1, height: 'auto' }} exit={{ opacity: 0, height: 0 }} className="mb-2">
            <div className="row-card overflow-hidden px-3 py-2 text-sm">Uploading {u.name}…<div className="loading-line !relative mt-1.5" /></div>
          </motion.div>
        ))}
      </AnimatePresence>

      <div className="mb-3 flex flex-wrap items-center gap-3">
        <nav aria-label="Folder path" className="flex min-w-0 flex-wrap items-center gap-1 text-sm">
          <button type="button" className={cn('crumb', !current && 'crumb-on')} onClick={() => setFolderId(null)}>All files</button>
          {trail.map((f) => <span key={f.id} className="flex items-center gap-1"><ChevronRight size={13} className="text-muted" /><button type="button" className={cn('crumb', f.id === current && 'crumb-on')} onClick={() => setFolderId(f.id)}>{f.name}</button></span>)}
        </nav>
        <div className="ml-auto flex flex-wrap items-center gap-2">
          <label className="search-box"><Search size={15} /><input aria-label="Search files" placeholder="Search files" value={search} onChange={(e) => setSearch(e.target.value)} /></label>
          <Segmented label="Layout" value={view} onChange={setView} options={[{ value: 'grid', label: <><LayoutGrid size={14} /><span className="sr-only">Grid</span></> }, { value: 'list', label: <><List size={14} /><span className="sr-only">List</span></> }]} />
        </div>
      </div>
      <div className="mb-4 flex flex-wrap gap-1.5" role="group" aria-label="Filter by tag">
        {tags.map((t) => <button key={t} type="button" aria-pressed={tag === t} className={cn('tag-chip', tag === t && 'tag-on')} onClick={() => setTag(tag === t ? null : t)}>#{t}</button>)}
        {filtering && <button type="button" className="link ml-1 text-xs" onClick={() => { setTag(null); setSearch('') }}>Clear filters</button>}
      </div>

      {query.isError ? <ErrorState error={query.error} retry={() => query.refetch()} />
        : query.isPending ? <SkeletonGrid count={8} className="grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-4" />
        : !shown.length ? <div className="card"><EmptyState icon={<Folder size={20} />} title={filtering ? 'No matching files' : 'This folder is empty'}>{filtering ? 'Try another tag or search.' : 'Drop files anywhere on this page.'}</EmptyState></div>
        : (
          <AnimatePresence mode="wait">
            <motion.div key={`${view}-${current}`} initial="hidden" animate="show" exit="exit" variants={MOTION.page}>
              {view === 'grid' ? (
                <div className="grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-4">
                  {shown.map((f, i) => {
                    const { Icon, tone } = kindOf(f)
                    return (
                      <motion.div key={f.id} className="card card-hover relative flex flex-col" initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: Math.min(i, 12) * 0.03 }}>
                        <button type="button" className="flex-1 p-3 text-left" onClick={() => openNode(f)} title={f.kind === 'folder' ? `Open ${f.name}` : `Download ${f.name}`}>
                          <span className={`file-art tone-${tone}`}><Icon size={30} strokeWidth={1.5} /></span>
                          <span className="mt-2.5 block truncate text-sm font-medium">{f.name}</span>
                          <span className="text-xs text-muted">{meta(f)} · {fmtDate(f.updatedAt)}</span>
                        </button>
                        <div className="flex items-center justify-between gap-2 px-3 pb-2.5">
                          {f.shares.length ? <AvatarStack users={f.shares.map((s) => people.get(s.userId))} max={3} /> : <span className="truncate text-[0.7rem] text-muted">{f.tags.map((t) => `#${t}`).join(' ')}</span>}
                          {menu(f)}
                        </div>
                      </motion.div>
                    )
                  })}
                </div>
              ) : (
                <div className="card divide-y divide-line">
                  {shown.map((f) => {
                    const { Icon, tone } = kindOf(f)
                    return (
                      <div key={f.id} className="list-row px-3">
                        <span className={`file-mini tone-${tone}`}><Icon size={16} /></span>
                        <button type="button" className="min-w-0 flex-1 truncate text-left text-sm font-medium" onClick={() => openNode(f)}>{f.name}</button>
                        <span className="hidden w-40 gap-1 overflow-hidden lg:flex">{f.tags.slice(0, 2).map((t) => <Badge key={t}>#{t}</Badge>)}</span>
                        <span className="hidden w-32 truncate text-xs text-muted md:block">{people.get(f.ownerId)?.name}</span>
                        <span className="w-16 text-right text-xs text-muted">{meta(f)}</span>
                        <span className="hidden w-16 text-right text-xs text-muted sm:block">{fmtDate(f.updatedAt)}</span>
                        {menu(f)}
                      </div>
                    )
                  })}
                </div>
              )}
            </motion.div>
          </AnimatePresence>
        )}

      <ShareModal node={all.find((f) => f.id === shareId) ?? null} onClose={() => setShareId(null)} />
      <Modal open={folderName !== null} onClose={() => setFolderName(null)} onSubmit={() => createFolder.mutate()} title="New folder"
        footer={<><Button variant="ghost" onClick={() => setFolderName(null)}>Cancel</Button><Button type="submit" variant="primary" loading={createFolder.isPending}>Create folder</Button></>}>
        <Input label="Folder name" value={folderName ?? ''} onChange={(e) => setFolderName(e.target.value)} data-autofocus />
        <FormError error={createFolder.error} />
      </Modal>
      <Modal open={!!tagging} onClose={() => setTagging(null)} onSubmit={() => saveTags.mutate()} title="Edit tags"
        footer={<><Button variant="ghost" onClick={() => setTagging(null)}>Cancel</Button><Button type="submit" variant="primary" loading={saveTags.isPending}>Save tags</Button></>}>
        <Input label="Tags" hint="Separate with commas." value={tagging?.value ?? ''} onChange={(e) => setTagging({ id: tagging!.id, value: e.target.value })} data-autofocus />
      </Modal>
    </div>
  )
}
