import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { AnimatePresence, motion } from 'framer-motion'
import { Crown, Mail, Send, UserMinus, Users } from 'lucide-react'
import { workspaceApi } from '../../api/services'
import type { WorkspaceRole } from '../../api/types'
import { useAction, useMe, useMembers, useWorkspace } from '../../lib/data'
import { fmtDate, timeAgo } from '../../lib/utils'
import { MOTION } from '../../lib/motion'
import { Avatar, Badge, Button, EmptyState, FormError, Input, PageHeader, SectionTitle, Segmented, Select, SkeletonGrid } from '../../components/ui/core'
import { Modal, useConfirm } from '../../components/ui/overlay'

const ASSIGNABLE: WorkspaceRole[] = ['admin', 'member', 'viewer']

export function WorkspaceSettings() {
  const { workspace } = useWorkspace()
  const me = useMe()
  const confirm = useConfirm()
  const members = useMembers()
  const invites = useQuery({ queryKey: ['invitations', workspace.id], queryFn: () => workspaceApi.sentInvitations(workspace.id) })
  const [tab, setTab] = useState<'members' | 'invitations' | 'general'>('members')
  const [email, setEmail] = useState('')
  const [role, setRole] = useState<WorkspaceRole>('member')
  const [name, setName] = useState<string | null>(null)
  const [transferring, setTransferring] = useState(false)
  const [heir, setHeir] = useState('')
  const [typed, setTyped] = useState('')

  const myRole = members.data?.find((m) => m.userId === me?.id)?.role
  const isOwner = myRole === 'owner'
  const canManage = isOwner || myRole === 'admin'

  const setMemberRole = useAction((v: { id: string; role: WorkspaceRole }) => workspaceApi.setRole(workspace.id, v.id, v.role), { success: 'Role updated' })
  const removeMember = useAction((id: string) => workspaceApi.removeMember(workspace.id, id), { success: 'Member removed' })
  const invite = useAction(() => workspaceApi.invite(workspace.id, email.trim(), role), { success: 'Invitation sent', silentError: true, onSuccess: () => setEmail('') })
  const cancel = useAction((id: string) => workspaceApi.cancelInvitation(id), { success: 'Invitation cancelled' })
  const rename = useAction(() => workspaceApi.update(workspace.id, { name: name!.trim() }), { success: 'Workspace renamed', onSuccess: () => setName(null) })
  const transfer = useAction(() => workspaceApi.transfer(workspace.id, heir), { success: 'Ownership transferred', onSuccess: () => { setTransferring(false); setHeir(''); setTyped('') } })

  return (
    <div className="max-w-4xl">
      <PageHeader eyebrow={workspace.name} title="Workspace settings" description="People, roles and invitations." />
      {!canManage && members.data && <p className="demo-hint mb-4 !mt-0">You have {myRole} access. Only owners and admins can make changes here.</p>}
      <div className="mb-5">
        <Segmented label="Settings section" value={tab} onChange={setTab} options={[
          { value: 'members', label: 'Members', count: members.data?.length }, { value: 'invitations', label: 'Invitations', count: invites.data?.length }, { value: 'general', label: 'General' },
        ]} />
      </div>
      <AnimatePresence mode="wait">
        <motion.div key={tab} initial="hidden" animate="show" exit="exit" variants={MOTION.page} className="space-y-4">
          {tab === 'members' && (members.isPending ? <SkeletonGrid count={5} className="space-y-2" item="h-14" /> : (
            <section className="card p-5">
              <SectionTitle title="Members" action={<Button size="sm" variant="outline" icon={<Mail size={14} />} onClick={() => setTab('invitations')}>Invite people</Button>} />
              <ul className="divide-y divide-line">
                <AnimatePresence initial={false}>
                  {members.data?.map((m) => (
                    <motion.li key={m.userId} layout exit={{ opacity: 0, x: 40 }} className="flex flex-wrap items-center gap-3 py-3">
                      <Avatar user={m.user} size={36} />
                      <div className="min-w-0 flex-1 basis-40"><p className="truncate text-sm font-medium">{m.user.name}{m.userId === me?.id && <span className="text-muted"> (you)</span>}</p><p className="truncate text-xs text-muted">{m.user.email} · joined {fmtDate(m.joinedAt, { month: 'short', year: 'numeric' })}</p></div>
                      {m.role === 'owner' ? <Badge tone="accent"><Crown size={11} /> Owner</Badge> : (
                        <Select aria-label={`Role of ${m.user.name}`} className="!w-auto capitalize" value={m.role} disabled={!canManage || m.userId === me?.id} onChange={(e) => setMemberRole.mutate({ id: m.userId, role: e.target.value as WorkspaceRole })}>
                          {ASSIGNABLE.map((r) => <option key={r} value={r}>{r[0].toUpperCase() + r.slice(1)}</option>)}
                        </Select>
                      )}
                      <Button size="sm" variant="ghost" icon={<UserMinus size={15} />} aria-label={`Remove ${m.user.name}`} disabled={!canManage || m.role === 'owner' || m.userId === me?.id} onClick={async () => {
                        if (await confirm({ title: `Remove ${m.user.name}?`, message: 'They lose access to this workspace right away.', confirmLabel: 'Remove member', danger: true })) removeMember.mutate(m.userId)
                      }} />
                    </motion.li>
                  ))}
                </AnimatePresence>
              </ul>
              {members.data?.length === 1 && <EmptyState icon={<Users size={18} />} title="It’s just you so far" action={<Button onClick={() => setTab('invitations')}>Invite your team</Button>} />}
            </section>
          ))}

          {tab === 'invitations' && (<>
            <form className="card p-5" onSubmit={(e) => { e.preventDefault(); invite.mutate() }} noValidate>
              <SectionTitle title="Send an invitation" />
              <div className="flex flex-wrap items-end gap-2">
                <div className="min-w-52 flex-1"><Input label="Email address" type="email" placeholder="teammate@company.com" value={email} onChange={(e) => setEmail(e.target.value)} disabled={!canManage} /></div>
                <Select aria-label="Invite role" className="!w-auto" value={role} onChange={(e) => setRole(e.target.value as WorkspaceRole)} disabled={!canManage}>
                  {ASSIGNABLE.map((r) => <option key={r} value={r}>{r[0].toUpperCase() + r.slice(1)}</option>)}
                </Select>
                <Button type="submit" variant="primary" icon={<Send size={14} />} loading={invite.isPending} disabled={!canManage}>Send invite</Button>
              </div>
              <div className="mt-3"><FormError error={invite.error} /></div>
            </form>
            <section className="card p-5">
              <SectionTitle title="Pending invitations" />
              {!invites.data?.length && !invites.isPending && <EmptyState icon={<Mail size={18} />} title="No pending invitations" />}
              <ul className="divide-y divide-line">
                <AnimatePresence initial={false}>
                  {invites.data?.map((i) => (
                    <motion.li key={i.id} layout initial={{ opacity: 0, y: -8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, x: 40 }} className="flex flex-wrap items-center gap-3 py-3">
                      <span className="empty-icon !size-9"><Mail size={15} /></span>
                      <div className="min-w-0 flex-1 basis-40"><p className="truncate text-sm font-medium">{i.email}</p><p className="text-xs text-muted">Invited by {i.invitedBy} · {timeAgo(i.sentAt)}</p></div>
                      <Badge>{i.role}</Badge>
                      <Button size="sm" variant="ghost" disabled={!canManage} onClick={() => cancel.mutate(i.id)}>Cancel invite</Button>
                    </motion.li>
                  ))}
                </AnimatePresence>
              </ul>
            </section>
          </>)}

          {tab === 'general' && (<>
            <form className="card p-5" onSubmit={(e) => { e.preventDefault(); if (name?.trim()) rename.mutate() }}>
              <SectionTitle title="Workspace name" />
              <div className="flex flex-wrap items-end gap-2">
                <div className="min-w-52 flex-1"><Input aria-label="Workspace name" value={name ?? workspace.name} onChange={(e) => setName(e.target.value)} disabled={!canManage} /></div>
                <Button type="submit" variant="primary" loading={rename.isPending} disabled={!name?.trim() || name.trim() === workspace.name}>Save</Button>
              </div>
            </form>
            <section className="card danger-zone p-5">
              <SectionTitle title="Transfer ownership" />
              <p className="mb-3 text-sm text-muted">Hand this workspace to another member. You’ll become an admin.</p>
              <Button variant="outline" icon={<Crown size={15} />} disabled={!isOwner || (members.data?.length ?? 0) < 2} onClick={() => setTransferring(true)}>Transfer ownership</Button>
              {!isOwner && <p className="mt-2 text-xs text-muted">Only the current owner can do this.</p>}
            </section>
          </>)}
        </motion.div>
      </AnimatePresence>

      <Modal open={transferring} onClose={() => setTransferring(false)} onSubmit={() => heir && typed === workspace.name && transfer.mutate()} title="Transfer ownership" description="Only the new owner will be able to reverse this."
        footer={<><Button variant="ghost" onClick={() => setTransferring(false)}>Cancel</Button><Button type="submit" variant="danger" disabled={!heir || typed !== workspace.name} loading={transfer.isPending}>Transfer ownership</Button></>}>
        <Select label="New owner" value={heir} onChange={(e) => setHeir(e.target.value)}>
          <option value="">Choose a member…</option>
          {members.data?.filter((m) => m.userId !== me?.id).map((m) => <option key={m.userId} value={m.userId}>{m.user.name} ({m.role})</option>)}
        </Select>
        <Input label={`Type “${workspace.name}” to confirm`} value={typed} onChange={(e) => setTyped(e.target.value)} />
      </Modal>
    </div>
  )
}
