import { createContext, useContext } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { workspaceApi } from '../api/services'
import { profileQueryKey, userApi } from '../api/users'
import { GLOBAL_ROLE, TWO_FACTOR, type UserResponseDto } from '../api/authTypes'
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

/** The signed-in user's profile, as the backend returns it. */
export const useProfile = () => useQuery({ queryKey: profileQueryKey, queryFn: userApi.me })

const hueFrom = (id: string) => [...id].reduce((hash, char) => (hash * 31 + char.charCodeAt(0)) % 360, 7)

/** Adapts the profile to the shape the other modules still use. The backend has no title, bio or hue. */
function toUser(p: UserResponseDto): User {
  return {
    id: p.userId, name: p.name, email: p.email, title: '', bio: '', hue: hueFrom(p.userId),
    systemRole: p.globalRole === GLOBAL_ROLE.systemAdmin ? 'super_admin' : 'user',
    active: true,
    twoFactor: p.twoFactorType === TWO_FACTOR.authenticator ? 'totp' : p.twoFactorType === TWO_FACTOR.email ? 'email' : null,
    createdAt: p.createdAt, lastSeen: p.updatedAt ?? p.createdAt,
  }
}

export const useMeQuery = () => useQuery({ queryKey: profileQueryKey, queryFn: userApi.me, select: toUser })
export const useMe = (): User | undefined => useMeQuery().data

export const useMembers = () => {
  const { workspace } = useWorkspace()
  return useQuery({ queryKey: ['members', workspace.id], queryFn: () => workspaceApi.members(workspace.id) })
}

/** Lookup of people in the current workspace, for avatars and names. */
export function usePeople() {
  const { data = [] } = useMembers()
  return new Map(data.map((m) => [m.userId, m.user]))
}
