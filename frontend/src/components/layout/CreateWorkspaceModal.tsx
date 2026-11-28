import { useState } from 'react'
import { workspaceService } from '../../api/workspaces'
import type { ID } from '../../api/types'
import { useAction } from '../../lib/data'
import { Button, FormError, Input } from '../ui/core'
import { Modal } from '../ui/overlay'

export function CreateWorkspaceModal({ open, onClose, onCreated }: { open: boolean; onClose: () => void; onCreated: (id: ID) => void }) {
  const [name, setName] = useState('')
  const create = useAction(async () => {
    if (!name.trim()) throw new Error('Give the workspace a name.')
    return workspaceService.create(name.trim())
  }, {
    success: 'Workspace created', silentError: true,
    onSuccess: ({ id }) => { close(); onCreated(id) },
  })
  const close = () => { setName(''); create.reset(); onClose() }

  return (
    <Modal open={open} onClose={close} onSubmit={() => { if (!create.isPending) create.mutate() }} title="Create a workspace" description="A separate home for another team or client."
      footer={<><Button variant="ghost" onClick={close}>Cancel</Button><Button type="submit" variant="primary" loading={create.isPending}>Create workspace</Button></>}>
      <Input label="Workspace name" placeholder="e.g. Harbor Collective" maxLength={255} value={name} onChange={(e) => setName(e.target.value)} data-autofocus />
      <FormError error={create.error} />
    </Modal>
  )
}
