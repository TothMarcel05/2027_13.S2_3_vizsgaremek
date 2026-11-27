/**
 * Mirrors backend/PROTOTYPE-backend/DTOs/Auth/AuthDtos.cs and DTOs/Users/UserDtos.cs
 * as serialised by ASP.NET Core (camelCase).
 */

/** AppUser.TwoFactorType: 0 = none, 1 = authenticator app, 2 = email. */
export const TWO_FACTOR = { none: 0, authenticator: 1, email: 2 } as const
export type TwoFactorType = (typeof TWO_FACTOR)[keyof typeof TWO_FACTOR]

/** AppUser.GlobalRole: 1 = user, 2 = system admin. */
export const GLOBAL_ROLE = { user: 1, systemAdmin: 2 } as const

export interface UserDto {
  id: string
  username: string
  email: string
  createdAt: string
}

export interface RegisterDto {
  username: string
  password: string
  email: string
}

export interface LoginDto {
  email: string
  password: string
}

export interface AuthResponseDto {
  accessToken: string
  refreshToken: string
  user: UserDto
}

export interface LoginResultDto {
  requiresTwoFactor: boolean
  twoFactorType: number | null
  twoFactorToken: string | null
  auth: AuthResponseDto | null
}

export interface TwoFactorLoginDto {
  twoFactorToken: string
  /** A 6-digit code or a recovery code; the same endpoint accepts both. */
  code: string
}

export interface TwoFactorSetupDto {
  secret: string
  otpAuthUri: string
}

export interface RecoveryCodesDto {
  recoveryCodes: string[]
}

export interface DisabledTwoFactorResultDto {
  at: string
  active: boolean
}

export interface MessageDto {
  message: string
}

export interface UserResponseDto {
  userId: string
  name: string
  email: string
  avatarUrl: string | null
  createdAt: string
  updatedAt: string | null
  globalRole: number
  is2faEnabled: boolean
  twoFactorType: number
}

export interface UserModifyRequestDto {
  name: string
  avatarURL: string | null
}

export interface UserModifyResponseDto {
  userId: string
  name: string
  email: string
  avatarUrl: string | null
  updatedAt: string
}

export interface ChangePasswordRequestDto {
  currentPassword: string
  newPassword: string
}
