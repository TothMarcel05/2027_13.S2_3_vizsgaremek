import { useEffect, useState } from 'react'

export const cn = (...parts: (string | false | null | undefined)[]) => parts.filter(Boolean).join(' ')

const DAY = 86_400_000
export const fmtDate = (iso: string | null | undefined, opts: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric' }) =>
  iso ? new Date(iso).toLocaleDateString('en-GB', opts) : '—'
export const fmtTime = (iso: string) => new Date(iso).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })
export const startOfDay = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate())
export const addDays = (d: Date, n: number) => new Date(d.getFullYear(), d.getMonth(), d.getDate() + n, d.getHours(), d.getMinutes())
export const sameDay = (a: Date, b: Date) => startOfDay(a).getTime() === startOfDay(b).getTime()
export const daysUntil = (iso: string) => Math.round((startOfDay(new Date(iso)).getTime() - startOfDay(new Date()).getTime()) / DAY)

export function dueLabel(iso: string | null) {
  if (!iso) return 'No due date'
  const d = daysUntil(iso)
  if (d === 0) return 'Today'
  if (d === 1) return 'Tomorrow'
  if (d === -1) return 'Yesterday'
  if (d < 0) return `${-d} days overdue`
  return d < 7 ? `In ${d} days` : fmtDate(iso)
}

export function timeAgo(iso: string) {
  const mins = Math.floor((Date.now() - Date.parse(iso)) / 60_000)
  if (mins < 1) return 'just now'
  if (mins < 60) return `${mins}m ago`
  if (mins < 1440) return `${Math.floor(mins / 60)}h ago`
  const days = Math.floor(mins / 1440)
  return days < 30 ? `${days}d ago` : fmtDate(iso)
}

export function fmtBytes(n: number) {
  if (!n) return '—'
  const units = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.min(units.length - 1, Math.floor(Math.log(n) / Math.log(1000)))
  const v = n / 1000 ** i
  return `${v >= 100 || i === 0 ? Math.round(v) : v.toFixed(1)} ${units[i]}`
}

/** yyyy-mm-dd and yyyy-mm-ddThh:mm in local time, for native inputs. */
export const toDateInput = (iso: string | null | undefined) => (iso ? toDateTimeInput(new Date(iso)).slice(0, 10) : '')
export const toDateTimeInput = (d: Date) => new Date(d.getTime() - d.getTimezoneOffset() * 60_000).toISOString().slice(0, 16)
export const fromDateInput = (v: string) => (v ? new Date(`${v}T17:00`).toISOString() : null)

export function useMediaQuery(query: string) {
  const [match, setMatch] = useState(() => window.matchMedia(query).matches)
  useEffect(() => {
    const mq = window.matchMedia(query)
    const onChange = () => setMatch(mq.matches)
    onChange()
    mq.addEventListener('change', onChange)
    return () => mq.removeEventListener('change', onChange)
  }, [query])
  return match
}

export function saveBlob(blob: Blob, name: string) {
  const url = URL.createObjectURL(blob)
  const a = Object.assign(document.createElement('a'), { href: url, download: name })
  a.click()
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}
