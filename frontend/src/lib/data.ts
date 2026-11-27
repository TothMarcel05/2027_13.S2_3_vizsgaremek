import { createContext, useContext } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { meApi, workspaceApi } from '../api/services'
import type { Workspace, User, ID } from '../api/types'
import { useToast } from '../components/ui/overlay'

/** Mutation with the app's standard behaviour: refresh data, toast success, toast failure. */
export function useAction<V = void, R = unknown>(
  fn: (vars: V) => Promise<R>,
  opts: { success?: string; onSuccess?: (result: R, vars: V) => void; silentError?: boolean } = {},
) {
  const client = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: fn,
    onSuccess: async (result, vars) => {
      await client.invalidateQueries()
      if (opts.success) toast(opts.success)
      opts.onSuccess?.(result, vars)
    },
    onError: (e: Error) => { if (!opts.silentError) toast(e.message, 'error') },
  })
}

export const WorkspaceContext = createContext<{ workspace: Workspace; workspaces: Workspace[]; setWorkspace: (id: ID) => void } | null>(null)
export const useWorkspace = () => useContext(WorkspaceContext)!

export const useMe = () => useQuery({ queryKey: ['me'], queryFn: meApi.get }).data as User | undefined

export const useMembers = () => {
  const { workspace } = useWorkspace()
  return useQuery({ queryKey: ['members', workspace.id], queryFn: () => workspaceApi.members(workspace.id) })
}

/** Lookup of people in the current workspace, for avatars and names. */
export function usePeople() {
  const { data = [] } = useMembers()
  return new Map(data.map((m) => [m.userId, m.user]))
}
