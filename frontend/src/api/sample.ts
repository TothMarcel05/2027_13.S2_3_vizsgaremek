/**
 * Sample data for the modules that aren't connected yet (projects, tasks, files, chat, calendar,
 * notifications, trash, admin). They are answered by the in-browser mock in both modes.
 *
 * With the real API, workspace and user ids are UUIDs the mock has never seen. This bridge shows the
 * same demo workspace under every real one, and presents the demo user as the signed-in user, so
 * "my tasks" and "my messages" still work. Delete it once the last module is connected.
 */
import type { Session } from './session'

const SAMPLE_WORKSPACE = 'w1'
const SAMPLE_USER = 'u1'

export const SAMPLE_STORAGE = { storageUsed: 68.4e9, storageLimit: 100e9 }

/** Copies a value, passing every string and every `{ id | userId, user }`-style object through. */
function rewrite(value: unknown, swap: (text: string) => string, person: (record: Record<string, unknown>) => void): unknown {
  if (typeof value === 'string') return swap(value)
  if (Array.isArray(value)) return value.map((item) => rewrite(item, swap, person))
  // Files and blobs are passed on untouched.
  if (typeof value !== 'object' || value === null || Object.getPrototypeOf(value) !== Object.prototype) return value
  const copy: Record<string, unknown> = Object.fromEntries(
    Object.entries(value).map(([key, item]) => [key, rewrite(item, swap, person)]))
  person(copy)
  return copy
}

async function mock(method: string, url: string, body: unknown): Promise<unknown> {
  const { handleMock } = await import('./mock/server')
  return handleMock(method, url, body)
}

export async function sampleRequest(method: string, url: string, body: unknown, session: Session | null): Promise<unknown> {
  const user = session?.user
  if (!user) return mock(method, url, body)

  const toMock = (text: string) => (text === user.id ? SAMPLE_USER : text)
  const fromMock = (text: string) => (text === SAMPLE_USER ? user.id : text)
  const asSignedIn = (record: Record<string, unknown>) => {
    if (record.id === user.id && 'email' in record) Object.assign(record, { name: user.username, email: user.email })
  }

  const path = url
    .replace(/^\/workspaces\/[^/?]+/, `/workspaces/${SAMPLE_WORKSPACE}`)
    .replaceAll(user.id, SAMPLE_USER)
  const result = await mock(method, path, rewrite(body, toMock, () => {}))
  return rewrite(result, fromMock, asSignedIn)
}
