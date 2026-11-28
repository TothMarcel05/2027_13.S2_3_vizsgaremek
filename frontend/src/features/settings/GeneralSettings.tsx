/** Workspace settings, General tab: rename, delete and (demo mode only) ownership transfer. */
import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Crown, Trash2 } from 'lucide-react'
import { USE_MOCK } from '../../api/client'
import { workspaceApi } from '../../api/services'
import { workspaceKeys, workspaceService, type MemberRow, type WorkspaceAccess } from '../../api/workspaces'
import type { ID, Workspace } from '../../api/types'
import { useAction, useWorkspace } from '../../lib/data'
import { Button, FormError, Input, SectionTitle, Select, Textarea } from '../../components/ui/core'
import { Modal, useConfirm, useToast } from '../../components/ui/overlay'

function RenameForm({ allowed }: { allowed: boolean }) {
  const { workspace } = useWorkspace()
  const confirm = useConfirm()
  const [name, setName] = useState<string | null>(null)
  const [description, setDescription] = useState('')
  const rename = useAction(
    () => workspaceService.update(workspace.id, { name: name?.trim() ?? workspace.name, description: description.trim() }),
    { success: 'Workspace saved', silentError: true, onSuccess: () => { setName(null); setDescription('') } },
  )
  const current = name ?? workspace.name
  // The demo has no descriptions. The real server stores one but never sends it back.
  const dirty = current.trim() !== workspace.name || (!USE_MOCK && !!description.trim())
  const submit = async (e: FormEvent) => {
    e.preventDefault()
    if (!allowed || !dirty || !current.trim() || rename.isPending) return
    if (!USE_MOCK && !description.trim() && !await confirm({
      title: 'Save without a description?', confirmLabel: 'Save anyway',
      message: 'Saving replaces the stored description. With the field empty, any existing description is removed.',
    })) return
    rename.mutate()
  }
  return (
    <form className="card p-5" onSubmit={submit} noValidate>
      <SectionTitle title="Workspace details" />
      <div className="flex flex-col gap-4">
        <Input label="Workspace name" maxLength={255} value={current} onChange={(e) => setName(e.target.value)} disabled={!allowed} />
        {!USE_MOCK && (
          <div>
            <Textarea label="New description" value={description} onChange={(e) => setDescription(e.target.value)} disabled={!allowed} />
            <p className="mt-1.5 text-xs text-muted">The current description can’t be shown here. Saving replaces it, and leaving this empty removes it.</p>
          </div>
        )}
        <FormError error={rename.error} />
        <div className="flex justify-end"><Button type="submit" variant="primary" loading={rename.isPending} disabled={!allowed || !current.trim() || !dirty}>Save</Button></div>
      </div>
      {!allowed && <p className="mt-2 text-xs text-muted">You don’t have permission to change these.</p>}
    </form>
  )
}

function DeleteWorkspace({ allowed }: { allowed: boolean }) {
  const { workspace } = useWorkspace()
  const client = useQueryClient()
  const navigate = useNavigate()
  const toast = useToast()
  const [open, setOpen] = useState(false)
  const [typed, setTyped] = useState('')
  const close = () => { setOpen(false); setTyped(''); remove.reset() }
  const remove = useMutation({
    mutationFn: (id: ID) => workspaceService.remove(id),
    // Not useAction: refetching everything would request the workspace that was just deleted.
    onSuccess: (_, id) => {
      setOpen(false)
      toast('Workspace deleted')
      navigate('/')
      // Dropping it from the list moves the app to another workspace.
      client.setQueryData<Workspace[]>(workspaceKeys.list, (list) => list?.filter((w) => w.id !== id))
      void client.invalidateQueries({ queryKey: workspaceKeys.list })
    },
  })
  const confirmed = typed === workspace.name
  return (
    <section className="card danger-zone p-5">
      <SectionTitle title="Delete workspace" />
      <p className="mb-3 text-sm text-muted">Everyone loses access to this workspace.</p>
      <Button variant="outline" icon={<Trash2 size={15} />} disabled={!allowed} onClick={() => setOpen(true)}>Delete workspace</Button>
      {!allowed && <p className="mt-2 text-xs text-muted">You don’t have permission to delete this workspace.</p>}
      <Modal open={open} onClose={close} onSubmit={() => confirmed && !remove.isPending && remove.mutate(workspace.id)} title="Delete workspace?" description="It disappears for every member."
        footer={<><Button variant="ghost" onClick={close}>Cancel</Button><Button type="submit" variant="danger" disabled={!confirmed} loading={remove.isPending}>Delete workspace</Button></>}>
        <Input label={`Type “${workspace.name}” to confirm`} value={typed} onChange={(e) => setTyped(e.target.value)} data-autofocus />
        <FormError error={remove.error} />
      </Modal>
    </section>
  )
}

/** The backend's transfer endpoint isn't connected yet, so this renders in demo mode only. */
function TransferOwnership({ members, myId }: { members: MemberRow[]; myId: ID | undefined }) {
  const { workspace } = useWorkspace()
  const [open, setOpen] = useState(false)
  const [heir, setHeir] = useState('')
  const [typed, setTyped] = useState('')
  const isOwner = members.some((m) => m.userId === myId && m.roleId === 'owner')
  const transfer = useAction(() => workspaceApi.transfer(workspace.id, heir), { success: 'Ownership transferred', onSuccess: () => { setOpen(false); setHeir(''); setTyped('') } })
  return (
    <section className="card danger-zone p-5">
      <SectionTitle title="Transfer ownership" />
      <p className="mb-3 text-sm text-muted">Hand this workspace to another member. You’ll become an admin.</p>
      <Button variant="outline" icon={<Crown size={15} />} disabled={!isOwner || members.length < 2} onClick={() => setOpen(true)}>Transfer ownership</Button>
      {!isOwner && <p className="mt-2 text-xs text-muted">Only the current owner can do this.</p>}
      <Modal open={open} onClose={() => setOpen(false)} onSubmit={() => heir && typed === workspace.name && transfer.mutate()} title="Transfer ownership" description="Only the new owner will be able to reverse this."
        footer={<><Button variant="ghost" onClick={() => setOpen(false)}>Cancel</Button><Button type="submit" variant="danger" disabled={!heir || typed !== workspace.name} loading={transfer.isPending}>Transfer ownership</Button></>}>
        <Select label="New owner" value={heir} onChange={(e) => setHeir(e.target.value)}>
          <option value="">Choose a member…</option>
          {members.filter((m) => m.userId !== myId).map((m) => <option key={m.userId} value={m.userId}>{m.name}</option>)}
        </Select>
        <Input label={`Type “${workspace.name}” to confirm`} value={typed} onChange={(e) => setTyped(e.target.value)} />
      </Modal>
    </section>
  )
}

export function GeneralSettings({ access, members, myId }: { access: WorkspaceAccess | undefined; members: MemberRow[]; myId: ID | undefined }) {
  return (<>
    <RenameForm allowed={access?.rename === true} />
    {USE_MOCK && <TransferOwnership members={members} myId={myId} />}
    <DeleteWorkspace allowed={access?.remove === true} />
  </>)
}
