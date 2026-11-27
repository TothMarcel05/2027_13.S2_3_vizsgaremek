import { Fragment, useDeferredValue, useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { AnimatePresence, motion } from 'framer-motion'
import { Check, ChevronLeft, ChevronRight, Lock, Plus, Search, ShieldCheck, Users } from 'lucide-react'
import { adminApi } from '../../api/services'
import type { Role, SystemRole, User } from '../../api/types'
import { useAction, useMe } from '../../lib/data'
import { cn, timeAgo } from '../../lib/utils'
import { MOTION } from '../../lib/motion'
import { Avatar, Badge, Button, EmptyState, ErrorState, FormError, Input, PageHeader, Segmented, Select, SkeletonGrid, Switch } from '../../components/ui/core'
import { Modal, useConfirm } from '../../components/ui/overlay'

const PAGE_SIZE = 8

function UserTable({ roles }: { roles: Role[] }) {
  const me = useMe()
  const confirm = useConfirm()
  const [search, setSearch] = useState('')
  const [role, setRole] = useState('')
  const [page, setPage] = useState(1)
  const term = useDeferredValue(search)
  const query = useQuery({
    queryKey: ['admin-users', term, role, page],
    queryFn: () => adminApi.users({ search: term, role, page, pageSize: PAGE_SIZE }),
    placeholderData: (prev) => prev,
  })
  const update = useAction((v: { id: string; patch: Partial<User> }) => adminApi.updateUser(v.id, v.patch), { success: 'User updated' })
  const total = query.data?.total ?? 0
  const pages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  useEffect(() => { if (page > pages) setPage(pages) }, [page, pages])

  const toggle = async (u: User) => {
    if (u.active && !(await confirm({ title: `Suspend ${u.name}?`, message: 'They won’t be able to sign in until reactivated.', confirmLabel: 'Suspend user', danger: true }))) return
    update.mutate({ id: u.id, patch: { active: !u.active } })
  }

  if (query.isError) return <ErrorState error={query.error} retry={() => query.refetch()} />
  return (
    <section className="card relative overflow-hidden">
      {query.isFetching && <div className="loading-line" />}
      <div className="flex flex-wrap items-center gap-2 border-b border-line p-3">
        <label className="search-box flex-1 sm:flex-none"><Search size={15} /><input aria-label="Search users" placeholder="Search name or email" value={search} onChange={(e) => { setSearch(e.target.value); setPage(1) }} /></label>
        <Select aria-label="Filter by role" className="!w-auto" value={role} onChange={(e) => { setRole(e.target.value); setPage(1) }}>
          <option value="">All roles</option>
          {roles.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}
        </Select>
        <span className="ml-auto text-sm text-muted">{total} users</span>
      </div>
      {query.isPending ? <SkeletonGrid count={PAGE_SIZE} className="space-y-2 p-3" item="h-11" /> : (
        <div className="overflow-x-auto">
          <table className="data-table">
            <thead><tr><th>User</th><th>System role</th><th>2FA</th><th>Last seen</th><th>Status</th></tr></thead>
            <tbody>
              {query.data?.items.map((u, i) => (
                <motion.tr key={u.id} initial={{ opacity: 0, y: 6 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: i * 0.025 }}>
                  <td><div className="flex items-center gap-2.5"><Avatar user={u} size={30} /><div><p className="font-medium">{u.name}{u.id === me?.id && <span className="text-muted"> (you)</span>}</p><p className="text-xs text-muted">{u.email}</p></div></div></td>
                  <td>
                    <Select aria-label={`System role for ${u.name}`} className="!w-36" value={u.systemRole} disabled={u.id === me?.id} onChange={(e) => update.mutate({ id: u.id, patch: { systemRole: e.target.value as SystemRole } })}>
                      {roles.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}
                    </Select>
                  </td>
                  <td>{u.twoFactor ? <Badge tone="sage">{u.twoFactor === 'totp' ? 'App' : 'Email'}</Badge> : <span className="text-muted">—</span>}</td>
                  <td className="text-muted">{timeAgo(u.lastSeen)}</td>
                  <td><div className="flex items-center gap-2"><Switch label={`${u.name} active`} checked={u.active} disabled={u.id === me?.id} onChange={() => toggle(u)} /><span className={cn('text-xs', !u.active && 'text-danger')}>{u.active ? 'Active' : 'Suspended'}</span></div></td>
                </motion.tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {!query.isPending && !total && <EmptyState icon={<Users size={18} />} title="No users match your search" />}
      <div className="flex items-center justify-between gap-2 border-t border-line p-3 text-sm">
        <span className="text-muted">{total ? `${(page - 1) * PAGE_SIZE + 1}–${Math.min(page * PAGE_SIZE, total)} of ${total}` : '0 results'}</span>
        <nav className="flex items-center gap-1" aria-label="Pagination">
          <Button size="sm" variant="ghost" icon={<ChevronLeft size={15} />} aria-label="Previous page" disabled={page <= 1} onClick={() => setPage(page - 1)} />
          {Array.from({ length: pages }, (_, i) => i + 1).map((n) => <Button key={n} size="sm" variant={n === page ? 'soft' : 'ghost'} aria-current={n === page ? 'page' : undefined} onClick={() => setPage(n)}>{n}</Button>)}
          <Button size="sm" variant="ghost" icon={<ChevronRight size={15} />} aria-label="Next page" disabled={page >= pages} onClick={() => setPage(page + 1)} />
        </nav>
      </div>
    </section>
  )
}

function Matrix({ roles, permissions }: { roles: Role[]; permissions: { key: string; label: string; group: string }[] }) {
  const [draft, setDraft] = useState(roles)
  const [adding, setAdding] = useState(false)
  const [form, setForm] = useState({ name: '', description: '' })
  useEffect(() => {
    // Keep unsaved toggles when the server list changes (for example after adding a role).
    setDraft((current) => roles.map((r) => current.find((c) => c.id === r.id) ?? r))
  }, [roles])
  const key = (r: Role) => [...r.permissions].sort().join('|')
  const changes = draft.reduce((n, r) => {
    const saved = new Set(roles.find((x) => x.id === r.id)?.permissions)
    return n + permissions.filter((p) => saved.has(p.key) !== r.permissions.includes(p.key)).length
  }, 0)
  const save = useAction(() => adminApi.saveRoles(draft), { success: 'Permissions saved' })
  const add = useAction(() => adminApi.createRole(form.name, form.description), { success: 'Role created', silentError: true, onSuccess: () => { setAdding(false); setForm({ name: '', description: '' }) } })
  const set = (roleId: string, keys: string[], on: boolean) =>
    setDraft((d) => d.map((r) => (r.id !== roleId ? r : { ...r, permissions: on ? [...new Set([...r.permissions, ...keys])] : r.permissions.filter((p) => !keys.includes(p)) })))
  const groups = [...new Set(permissions.map((p) => p.group))]
  return (
    <>
      <div className="mb-3 flex justify-end"><Button variant="outline" icon={<Plus size={15} />} onClick={() => setAdding(true)}>New role</Button></div>
      <section className="card overflow-x-auto">
        <table className="data-table matrix">
          <thead>
            <tr>
              <th>Permission</th>
              {draft.map((r) => (
                <th key={r.id} className="!text-center"><span className="flex items-center justify-center gap-1">{r.name}{r.locked && <Lock size={11} />}</span><span className="block text-[0.68rem] font-normal normal-case tracking-normal text-muted">{r.permissions.length}/{permissions.length} granted</span></th>
              ))}
            </tr>
          </thead>
          <tbody>
            {groups.map((g) => {
              const inGroup = permissions.filter((p) => p.group === g)
              return (
                <Fragment key={g}>
                  <tr className="matrix-group">
                    <td>{g}</td>
                    {draft.map((r) => {
                      const all = inGroup.every((p) => r.permissions.includes(p.key))
                      return <td key={r.id} className="text-center">{!r.locked && <button type="button" className="link text-[0.7rem]" onClick={() => set(r.id, inGroup.map((p) => p.key), !all)} aria-label={`${all ? 'Clear' : 'Grant'} all ${g} permissions for ${r.name}`}>{all ? 'Clear all' : 'Grant all'}</button>}</td>
                    })}
                  </tr>
                  {inGroup.map((p) => (
                    <tr key={p.key}>
                      <td><p className="font-medium">{p.label}</p><code className="text-[0.7rem] text-muted">{p.key}</code></td>
                      {draft.map((r) => {
                        const on = r.permissions.includes(p.key)
                        const changed = on !== !!roles.find((x) => x.id === r.id)?.permissions.includes(p.key)
                        return (
                          <td key={r.id} className="text-center">
                            <motion.button type="button" role="checkbox" aria-checked={on} aria-label={`${r.name}: ${p.label}`} disabled={r.locked} whileTap={{ scale: 0.8 }} onClick={() => set(r.id, [p.key], !on)} className={cn('matrix-cell', on && 'matrix-on', changed && 'matrix-changed')}>
                              <AnimatePresence initial={false}>{on && <motion.span initial={{ scale: 0 }} animate={{ scale: 1 }} exit={{ scale: 0 }}><Check size={14} strokeWidth={3} /></motion.span>}</AnimatePresence>
                            </motion.button>
                          </td>
                        )
                      })}
                    </tr>
                  ))}
                </Fragment>
              )
            })}
          </tbody>
        </table>
      </section>
      <AnimatePresence>
        {draft.some((r) => roles.find((x) => x.id === r.id) && key(r) !== key(roles.find((x) => x.id === r.id)!)) && (
          <motion.div className="save-bar" initial={{ y: 30, opacity: 0 }} animate={{ y: 0, opacity: 1 }} exit={{ y: 30, opacity: 0 }}>
            <span className="text-sm">{changes} unsaved {changes === 1 ? 'change' : 'changes'}</span>
            <Button size="sm" variant="ghost" className="!text-inherit" onClick={() => setDraft(roles)}>Discard</Button>
            <Button size="sm" variant="primary" loading={save.isPending} onClick={() => save.mutate()}>Save permissions</Button>
          </motion.div>
        )}
      </AnimatePresence>
      <Modal open={adding} onClose={() => setAdding(false)} onSubmit={() => add.mutate()} title="New role"
        footer={<><Button variant="ghost" onClick={() => setAdding(false)}>Cancel</Button><Button type="submit" variant="primary" loading={add.isPending}>Create role</Button></>}>
        <Input label="Role name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} data-autofocus />
        <Input label="Description" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
        <FormError error={add.error} />
      </Modal>
    </>
  )
}

export function Admin() {
  const [tab, setTab] = useState<'users' | 'roles'>('users')
  const roles = useQuery({ queryKey: ['admin-roles'], queryFn: adminApi.roles })
  return (
    <>
      <PageHeader eyebrow="System administration" title="Admin" description="Manage every account and what each system role is allowed to do." />
      <div className="mb-4">
        <Segmented label="Admin section" value={tab} onChange={setTab} options={[{ value: 'users', label: <><Users size={14} /> Users</> }, { value: 'roles', label: <><ShieldCheck size={14} /> Roles & permissions</> }]} />
      </div>
      {roles.isError ? <ErrorState error={roles.error} retry={() => roles.refetch()} />
        : !roles.data ? <SkeletonGrid count={6} className="space-y-2" item="h-12" />
        : (
          <AnimatePresence mode="wait">
            <motion.div key={tab} initial="hidden" animate="show" exit="exit" variants={MOTION.page}>
              {tab === 'users' ? <UserTable roles={roles.data.roles} /> : <Matrix roles={roles.data.roles} permissions={roles.data.permissions} />}
            </motion.div>
          </AnimatePresence>
        )}
    </>
  )
}
