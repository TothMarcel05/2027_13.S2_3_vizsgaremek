export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
    public code?: string,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

const isRecord = (value: unknown): value is Record<string, unknown> => typeof value === 'object' && value !== null

/** The backend answers every failure with `{ "error": { "message", "code" } }`. */
export function parseErrorBody(data: unknown): { message: string; code?: string } | null {
  if (!isRecord(data) || !isRecord(data.error)) return null
  const { message, code } = data.error
  if (typeof message !== 'string' || !message) return null
  return { message, code: typeof code === 'string' ? code : undefined }
}

/** Used when a response carries no readable error body (proxy errors, crashes, network loss). */
export function fallbackMessage(status: number) {
  if (status === 0) return 'Can’t reach the server. Check your connection and try again.'
  if (status === 429) return 'Too many requests. Wait a moment and try again.'
  if (status >= 500) return 'The server ran into a problem. Try again in a moment.'
  return `The request failed (status ${status}).`
}
