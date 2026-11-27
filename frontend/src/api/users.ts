/** UserController (api/users): the signed-in user's own profile. */
import { request } from './client'
import type * as D from './authTypes'

export const userApi = {
  me: () => request<D.UserResponseDto>({ method: 'GET', path: '/users/me' }),
  updateMe: (dto: D.UserModifyRequestDto) =>
    request<D.UserModifyResponseDto>({ method: 'PUT', path: '/users/me', body: dto }),
  changePassword: (dto: D.ChangePasswordRequestDto) =>
    request<void>({ method: 'POST', path: '/users/me/change-password', body: dto }),
}

export const profileQueryKey = ['me'] as const
