/** AuthController (api/auth): the account and session endpoints. */
import { logoutSession, request } from './client'
import type * as D from './authTypes'

export const authApi = {
  register: (dto: D.RegisterDto) =>
    request<D.UserDto>({ method: 'POST', path: '/auth/register', body: dto, anonymous: true }),
  login: (dto: D.LoginDto) =>
    request<D.LoginResultDto>({ method: 'POST', path: '/auth/login', body: dto, anonymous: true }),
  verifyTwoFactor: (dto: D.TwoFactorLoginDto) =>
    request<D.AuthResponseDto>({ method: 'POST', path: '/auth/2fa/verify', body: dto, anonymous: true }),
  resendTwoFactorEmail: (twoFactorToken: string) =>
    request<D.MessageDto>({ method: 'POST', path: '/auth/2fa/resend-email', body: twoFactorToken, anonymous: true, jsonString: true }),
  requestPasswordReset: (email: string) =>
    request<D.MessageDto>({ method: 'POST', path: '/auth/password-reset', query: { email }, anonymous: true }),
  resetPassword: (email: string, token: string, newPwd: string) =>
    request<D.MessageDto>({ method: 'PUT', path: '/auth/password-reset', query: { email, token }, body: { newPwd }, anonymous: true }),

  beginTotpSetup: () => request<D.TwoFactorSetupDto>({ method: 'POST', path: '/auth/2fa/setup' }),
  enableTotp: (code: string) =>
    request<D.RecoveryCodesDto>({ method: 'POST', path: '/auth/2fa/enable', body: { code } }),
  enableEmailTwoFactor: (password: string) =>
    request<D.RecoveryCodesDto>({ method: 'POST', path: '/auth/me/2fa/enable-email', body: { password } }),
  disableTwoFactor: (password: string) =>
    request<D.DisabledTwoFactorResultDto>({ method: 'DELETE', path: '/auth/2fa/disable', body: password, jsonString: true }),
  regenerateRecoveryCodes: (password: string) =>
    request<D.RecoveryCodesDto>({ method: 'POST', path: '/auth/me/2fa/recovery-codes', body: { password } }),

  /** Clears the local session immediately; the returned promise tracks server-side revocation. */
  logout: logoutSession,
}
