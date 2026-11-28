import { useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { Crown, Lock, Mail, Send, UserMinus, Users } from 'lucide-react'
import { workspaceService, type MemberRow, type RoleOption, type SentInvitation } from '../../api/workspaces'
import type { ID } from '../../api/types'
import { useAction, useMe, useWorkspace } from '../../lib/data'
import { fmtDate, timeAgo } from '../../lib/utils'
import { MOTION } from '../../lib/motion'
import { Avatar, Badge, Button, EmptyState, ErrorState, FormError, Input, PageHeader, SectionTitle, Segmented, Select, SkeletonGrid } from '../../components/ui/core'
import { useConfirm } from '../../components/ui/overlay'
import * as check from '../auth/validation'
import { GeneralSettings } from './GeneralSettings'
import { useWorkspaceAdmin } from './useWorkspaceAdmin'

type Admin = ReturnType<typeof useWorkspaceAdmin>
type Tab = 'members' | 'invitations' | 'general'

const roleName = (roles: RoleOption[] | undefined, id: ID | null) => roles?.find((r) => r.id === id)?.name

/** The assignable roles, plus the member's current one so the list never misstates it. */
function RoleOptions({ roles, current }: { roles: RoleOption[]; current?: ID }) {
  return roles.filter((r) => r.assignable || r.id === current).map((r) => <option key={r.id} value={r.id} disabled={!r.assignable}>{r.name}</option>)
}

function Members({ admin, myId, onInvite }: { admin: Admin; myId: ID | undefined; onInvite: () => void }) {
  const { workspace } = useWorkspace()
  const confirm = useConfirm()
  const { members, roles, access } = admin
  const canManage = access.data?.manageMembers === true
  const setRole = useAction((v: { userId: ID; roleId: ID }) => workspaceService.setRole(workspace.id, v.userId, v.roleId), { success: 'Role updated' })
  const remove = useAction((userId: ID) => workspaceService.removeMember(workspace.id, userId), { success: 'Member removed' })

  if (members.isPending) return <SkeletonGrid count={5} className="space-y-2" item="h-14" />
  if (members.isError) return <ErrorState error={members.error} retry={() => members.refetch()} />

  const roleControl = (m: MemberRow) => {
    // The server doesn't stop the owner being demoted or removed, so the screen must.
    if (m.userId === workspace.ownerId) return <Badge tone="accent"><Crown size={11} /> Owner</Badge>
    if (access.isPending || (roles.isFetching && !roles.data)) return <span className="text-xs text-muted">Loading role…</span>
    if (!roles.data) return <Badge>Role hidden</Badge>
    return (
      <Select aria-label={`Role of ${m.name}`} className="!w-auto" value={m.roleId} disabled={!canManage || m.userId === myId || setRole.isPending}
        onChange={(e) => setRole.mutate({ userId: m.userId, roleId: e.target.value })}>
        <RoleOptions roles={roles.data} current={m.roleId} />
      </Select>
    )
  }

  return (
    <section className="card p-5">
      <SectionTitle title="Members" action={access.data?.manageInvitations ? <Button size="sm" variant="outline" icon={<Mail size={14} />} onClick={onInvite}>Invite people</Button> : undefined} />
      {roles.isError && <div className="mb-3"><FormError error={roles.error} /></div>}
      <ul className="divide-y divide-line">
        <AnimatePresence initial={false}>
          {members.data.map((m) => (
            <motion.li key={m.userId} layout exit={{ opacity: 0, x: 40 }} className="flex flex-wrap items-center gap-3 py-3">
              <Avatar user={m} size={36} />
              <div className="min-w-0 flex-1 basis-40">
                <p className="truncate text-sm font-medium">{m.name}{m.userId === myId && <span className="text-muted"> (you)</span>}</p>
                <p className="truncate text-xs text-muted">{m.email && `${m.email} · `}joined {fmtDate(m.joinedAt, { month: 'short', year: 'numeric' })}</p>
              </div>
              {roleControl(m)}
              <Button size="sm" variant="ghost" icon={<UserMinus size={15} />} aria-label={`Remove ${m.name}`}
                disabled={!canManage || m.userId === workspace.ownerId || m.userId === myId || remove.isPending}
                onClick={async () => {
                  if (await confirm({ title: `Remove ${m.name}?`, message: 'They lose access to this workspace.', confirmLabel: 'Remove member', danger: true })) remove.mutate(m.userId)
                }} />
            </motion.li>
          ))}
        </AnimatePresence>
      </ul>
      {members.data.length === 1 && access.data?.manageInvitations && <EmptyState icon={<Users size={18} />} title="It’s just you so far" action={<Button onClick={onInvite}>Invite your team</Button>} />}
    </section>
  )
}

function Invitations({ admin, myId }: { admin: Admin; myId: ID | undefined }) {
  const { workspace } = useWorkspace()
  const confirm = useConfirm()
  const { invitations, members, roles, access } = admin
  const [email, setEmail] = useState('')
  const [picked, setPicked] = useState('')
  const choices = roles.data?.filter((r) => r.assignable) ?? []
  // A role is required: accepting an invitation without one adds no membership.
  const roleId = choices.find((r) => r.id === picked)?.id ?? (choices.find((r) => r.name === 'Member') ?? choices[0])?.id

  const invite = useAction(async () => {
    const address = email.trim()
    check.assertValid(check.email(address))
    if (invitations.data?.some((i) => i.email.toLowerCase() === address.toLowerCase())) throw new Error('That address already has a pending invitation.')
    if (!roleId) throw new Error('Choose a role for the invitation.')
    await workspaceService.invite(workspace.id, address, roleId)
  }, { success: 'Invitation created', silentError: true, onSuccess: () => setEmail('') })
  const cancel = useAction((id: ID) => workspaceService.cancelInvitation(workspace.id, id), { success: 'Invitation cancelled' })

  if (access.isPending) return <SkeletonGrid count={3} className="space-y-2" item="h-14" />
  if (!access.data?.manageInvitations) return <section className="card p-5"><EmptyState icon={<Lock size={18} />} title="You can’t manage invitations">Your role doesn’t allow seeing or sending them.</EmptyState></section>

  const inviter = (i: SentInvitation) =>
    i.invitedByName ?? (i.invitedById === myId ? 'you' : members.data?.find((m) => m.userId === i.invitedById)?.name ?? 'a former member')

  return (<>
    <form className="card p-5" onSubmit={(e) => { e.preventDefault(); if (!invite.isPending) invite.mutate() }} noValidate>
      <SectionTitle title="Send an invitation" />
      <div className="flex flex-wrap items-end gap-2">
        <div className="min-w-52 flex-1"><Input label="Email address" type="email" placeholder="teammate@company.com" value={email} onChange={(e) => setEmail(e.target.value)} /></div>
        <Select aria-label="Invite role" className="!w-auto" value={roleId ?? ''} onChange={(e) => setPicked(e.target.value)} disabled={!choices.length}>
          {!choices.length && <option value="">No roles available</option>}
          <RoleOptions roles={choices} />
        </Select>
        <Button type="submit" variant="primary" icon={<Send size={14} />} loading={invite.isPending} disabled={!roleId}>Send invite</Button>
      </div>
      <div className="mt-3 space-y-2"><FormError error={invite.error} /><FormError error={roles.error} /></div>
    </form>
    <section className="card p-5">
      <SectionTitle title="Pending invitations" />
      {invitations.isPending ? <SkeletonGrid count={2} className="space-y-2" item="h-14" />
        : invitations.isError ? <ErrorState error={invitations.error} retry={() => invitations.refetch()} />
        : !invitations.data.length && <EmptyState icon={<Mail size={18} />} title="No pending invitations" />}
      <ul className="divide-y divide-line">
        <AnimatePresence initial={false}>
          {invitations.data?.map((i) => (
            <motion.li key={i.id} layout initial={{ opacity: 0, y: -8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, x: 40 }} className="flex flex-wrap items-center gap-3 py-3">
              <span className="empty-icon !size-9"><Mail size={15} /></span>
              <div className="min-w-0 flex-1 basis-40"><p className="truncate text-sm font-medium">{i.email}</p><p className="text-xs text-muted">Invited by {inviter(i)} · {timeAgo(i.sentAt)}</p></div>
              {roleName(roles.data, i.roleId) && <Badge>{roleName(roles.data, i.roleId)}</Badge>}
              <Button size="sm" variant="ghost" disabled={cancel.isPending} onClick={async () => {
                if (await confirm({ title: 'Cancel this invitation?', message: `${i.email} will no longer be able to accept it.`, confirmLabel: 'Cancel invitation', danger: true })) cancel.mutate(i.id)
              }}>Cancel invite</Button>
            </motion.li>
          ))}
        </AnimatePresence>
      </ul>
    </section>
  </>)
}

export function WorkspaceSettings() {
  const { workspace } = useWorkspace()
  const myId = useMe()?.id
  const admin = useWorkspaceAdmin(workspace.id)
  const { access, members, invitations } = admin
  const [tab, setTab] = useState<Tab>('members')
  const readOnly = access.data && !access.data.rename && !access.data.remove && !access.data.manageMembers && !access.data.manageInvitations

  return (
    <div className="max-w-4xl">
      <PageHeader eyebrow={workspace.name} title="Workspace settings" description="People, roles and invitations." />
      {readOnly && <p className="row-card mb-4 p-3 text-sm">You have view-only access to these settings.</p>}
      {access.isError && (
        <div className="mb-4 flex flex-wrap items-center gap-2">
          <FormError error={access.error} /><Button size="sm" variant="outline" onClick={() => access.refetch()}>Check permissions again</Button>
        </div>
      )}
      <div className="mb-5">
        <Segmented label="Settings section" value={tab} onChange={setTab} options={[
          { value: 'members', label: 'Members', count: members.data?.length }, { value: 'invitations', label: 'Invitations', count: invitations.data?.length }, { value: 'general', label: 'General' },
        ]} />
      </div>
      <AnimatePresence mode="wait">
        <motion.div key={tab} initial="hidden" animate="show" exit="exit" variants={MOTION.page} className="space-y-4">
          {tab === 'members' && <Members admin={admin} myId={myId} onInvite={() => setTab('invitations')} />}
          {tab === 'invitations' && <Invitations admin={admin} myId={myId} />}
          {tab === 'general' && <GeneralSettings access={access.data} members={members.data ?? []} myId={myId} />}
        </motion.div>
      </AnimatePresence>
    </div>
  )
}
