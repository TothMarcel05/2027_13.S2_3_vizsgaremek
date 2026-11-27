/**
 * Client-side checks that mirror the backend's rules, so most mistakes are caught before a request.
 * Each returns a message, or null when the value passes.
 */

const EMAIL = /^[^\s@]+@[^\s@]+$/

/** Throws the first failed check so a mutation can surface it like a server error. */
export function assertValid(...checks: (string | null)[]) {
  const message = checks.find((check) => check !== null)
  if (message) throw new Error(message)
}

const length = (value: string, label: string, min: number, max: number) =>
  value.length < min || value.length > max ? `${label} must be ${min}–${max} characters.` : null

export const required = (value: string, message: string) => (value.trim() ? null : message)

export const email = (value: string) => (EMAIL.test(value) ? null : 'Enter a valid email address.')

// RegisterDto
export const username = (value: string) => length(value, 'Username', 5, 30)
export const registerPassword = (value: string) =>
  required(value, 'Enter a password.') ?? length(value, 'Password', 5, 30)
export const registerEmail = (value: string) => email(value) ?? length(value, 'Email', 5, 100)

// ChangePasswordRequestDto. ResetPwdDto has no length rule; the same one is applied to it here.
export const newPassword = (value: string) =>
  required(value, 'Enter a new password.') ?? length(value, 'The new password', 8, 100)

export const matches = (a: string, b: string) => (a === b ? null : 'The passwords don’t match.')

// UserModifyRequestDto
export const displayName = (value: string) => length(value, 'Name', 2, 100)
export function avatarUrl(value: string) {
  if (!value) return null
  try {
    if (['http:', 'https:'].includes(new URL(value).protocol)) return null
  } catch { /* not a URL */ }
  return 'The picture address must be a full http or https URL.'
}

const RECOVERY = /^[A-Z0-9]{4}-?[A-Z0-9]{4}$/

/** Recovery codes look like ABCD-2345. Returns the canonical form, or null if it can't be one. */
export function normalizeRecoveryCode(value: string) {
  const compact = value.trim().toUpperCase()
  if (!RECOVERY.test(compact)) return null
  const plain = compact.replace('-', '')
  return `${plain.slice(0, 4)}-${plain.slice(4)}`
}
