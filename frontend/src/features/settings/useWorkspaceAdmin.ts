/** Data for Workspace settings. Queries the user isn't allowed to make are never sent. */
import { useQuery } from '@tanstack/react-query'
import { workspaceKeys, workspaceService } from '../../api/workspaces'
import type { ID } from '../../api/types'

export function useWorkspaceAdmin(workspaceId: ID) {
  const access = useQuery({ queryKey: workspaceKeys.access(workspaceId), queryFn: () => workspaceService.access(workspaceId) })
  const members = useQuery({ queryKey: workspaceKeys.members(workspaceId), queryFn: () => workspaceService.members(workspaceId) })
  const roles = useQuery({
    queryKey: workspaceKeys.roles(workspaceId), queryFn: () => workspaceService.roles(workspaceId),
    enabled: access.data?.viewRoles === true,
  })
  const invitations = useQuery({
    queryKey: workspaceKeys.invitations(workspaceId), queryFn: () => workspaceService.invitations(workspaceId),
    enabled: access.data?.manageInvitations === true,
  })
  return { access, members, roles, invitations }
}
