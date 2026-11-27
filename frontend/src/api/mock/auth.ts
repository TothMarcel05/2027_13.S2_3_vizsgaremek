/** Demo-mode stand-in for AuthController and UserController, using the same paths and payloads. */
import { ApiError } from '../errors'
import type * as D from '../authTypes'
import { GLOBAL_ROLE, TWO_FACTOR } from '../authTypes'
import type { User } from '../types'

export interface MockContext {
  params: string[]
  query: URLSearchParams
  body: unknown
}
type On = (method: string, pattern: string, handler: (ctx: MockContext) => unknown) => void

const field = (body: unknown, key: string): string => {
  const value = typeof body === 'object' && body !== null ? (body as Record<string, unknown>)[key] : undefined
  return typeof value === 'string' ? value : ''
}

const fail = (message: string, status = 400): never => { throw new ApiError(status, message) }

const recoveryCodes = (): D.RecoveryCodesDto => {
  const part = () => Math.random().toString(36).slice(2, 6).toUpperCase().padEnd(4, 'A')
  return { recoveryCodes: Array.from({ length: 10 }, () => `${part()}-${part()}`) }
}

function checkCode(code: string, status: number) {
  if (/^\d{6}$/.test(code) && code !== '000000') return
  fail('That code is incorrect or has expired.', status)
}

const requirePassword = (password: string) => { if (password.length < 5) fail('Wrong password.', 401) }

export function registerAuthRoutes(on: On, me: () => User, emailTaken: (email: string) => boolean) {
  let avatarUrl: string | null = null

  const userDto = (): D.UserDto => ({ id: me().id, username: me().name, email: me().email, createdAt: me().createdAt })
  const authResponse = (): D.AuthResponseDto => ({ accessToken: 'mock-access', refreshToken: 'mock-refresh', user: userDto() })
  const twoFactorType = () =>
    me().twoFactor === 'totp' ? TWO_FACTOR.authenticator : me().twoFactor === 'email' ? TWO_FACTOR.email : TWO_FACTOR.none
  const profile = (): D.UserResponseDto => ({
    userId: me().id, name: me().name, email: me().email, avatarUrl, createdAt: me().createdAt, updatedAt: null,
    globalRole: me().systemRole === 'super_admin' ? GLOBAL_ROLE.systemAdmin : GLOBAL_ROLE.user,
    is2faEnabled: me().twoFactor !== null, twoFactorType: twoFactorType(),
  })

  on('POST', '/auth/register', ({ body }): D.UserDto => {
    if (emailTaken(field(body, 'email'))) fail('An account with this email already exists.', 409)
    Object.assign(me(), { name: field(body, 'username'), email: field(body, 'email'), twoFactor: null })
    return userDto()
  })
  on('POST', '/auth/login', ({ body }): D.LoginResultDto => {
    if (field(body, 'password').length < 5) fail('Email or password is incorrect.', 401)
    return me().twoFactor
      ? { requiresTwoFactor: true, twoFactorType: twoFactorType(), twoFactorToken: 'mock-2fa', auth: null }
      : { requiresTwoFactor: false, twoFactorType: null, twoFactorToken: null, auth: authResponse() }
  })
  on('POST', '/auth/2fa/verify', ({ body }) => {
    const code = field(body, 'code')
    if (!/^[A-Z0-9]{4}-[A-Z0-9]{4}$/i.test(code)) checkCode(code, 401)
    return authResponse()
  })
  on('POST', '/auth/2fa/resend-email', () => ({ message: 'A new code is on its way.' }))
  on('POST', '/auth/password-reset', () => ({ message: 'If the account exists, a reset link is on its way.' }))
  on('PUT', '/auth/password-reset', ({ query }) => {
    if (query.get('token') === 'expired') fail('The reset link is invalid or has expired.')
    return { message: 'Password changed.' }
  })
  on('POST', '/auth/2fa/setup', (): D.TwoFactorSetupDto => {
    const secret = 'JBSWY3DPEHPK3PXP'
    return { secret, otpAuthUri: `otpauth://totp/PROTOTYPE:${encodeURIComponent(me().email)}?secret=${secret}&issuer=PROTOTYPE` }
  })
  on('POST', '/auth/2fa/enable', ({ body }) => {
    checkCode(field(body, 'code'), 400)
    me().twoFactor = 'totp'
    return recoveryCodes()
  })
  on('POST', '/auth/me/2fa/enable-email', ({ body }) => {
    requirePassword(field(body, 'password'))
    me().twoFactor = 'email'
    return recoveryCodes()
  })
  on('DELETE', '/auth/2fa/disable', ({ body }): D.DisabledTwoFactorResultDto => {
    requirePassword(typeof body === 'string' ? body : '')
    me().twoFactor = null
    return { at: new Date().toISOString(), active: false }
  })
  on('POST', '/auth/me/2fa/recovery-codes', ({ body }) => {
    requirePassword(field(body, 'password'))
    return recoveryCodes()
  })

  on('GET', '/users/me', profile)
  on('PUT', '/users/me', ({ body }): D.UserModifyResponseDto => {
    me().name = field(body, 'name')
    avatarUrl = field(body, 'avatarURL') || null
    const { userId, name, email } = profile()
    return { userId, name, email, avatarUrl, updatedAt: new Date().toISOString() }
  })
  on('POST', '/users/me/change-password', ({ body }) => {
    if (field(body, 'currentPassword').length < 5) fail('The current password is wrong.')
  })
}
