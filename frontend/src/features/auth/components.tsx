import { useEffect, useRef, useState, type ClipboardEvent, type KeyboardEvent, type ReactNode } from 'react'
import { useMutation } from '@tanstack/react-query'
import { CheckCircle2 } from 'lucide-react'
import { ApiError } from '../../api/errors'
import { USE_MOCK } from '../../api/client'
import { Button } from '../../components/ui/core'
import { useToast } from '../../components/ui/overlay'

export function Head({ title, sub }: { title: string; sub: ReactNode }) {
  return (
    <div className="mb-6">
      <h1 className="page-title">{title}</h1>
      <p className="mt-2 text-sm text-muted">{sub}</p>
    </div>
  )
}

export const Hint = ({ children }: { children: ReactNode }) => (USE_MOCK ? <p className="demo-hint">{children}</p> : null)

export function Notice({ children }: { children: ReactNode }) {
  return (
    <p role="status" className="row-card mb-4 flex items-start gap-2 p-3 text-sm">
      <CheckCircle2 size={16} className="mt-0.5 shrink-0 text-accent" /> <span>{children}</span>
    </p>
  )
}

function strength(p: string) {
  return [p.length >= 8, /[A-Z]/.test(p) && /[a-z]/.test(p), /\d/.test(p), /[^A-Za-z0-9]/.test(p) || p.length >= 14].filter(Boolean).length
}

export function PasswordMeter({ value }: { value: string }) {
  const score = strength(value)
  return (
    <div className="-mt-2 flex items-center gap-2">
      <div className="grid flex-1 grid-cols-4 gap-1" aria-hidden>
        {[0, 1, 2, 3].map((i) => <span key={i} className={`h-1 rounded-full transition-colors duration-300 ${i < score ? (score < 3 ? 'bg-[#c8935a]' : 'bg-[#6f8f6c]') : 'bg-sand-2'}`} />)}
      </div>
      <span className="w-14 text-right text-xs text-muted">{value ? ['Weak', 'Weak', 'Fair', 'Good', 'Strong'][score] : ''}</span>
    </div>
  )
}

export function CodeInput({ value, onChange, onComplete }: { value: string; onChange: (v: string) => void; onComplete?: (v: string) => void }) {
  const refs = useRef<(HTMLInputElement | null)[]>([])
  const update = (next: string, focus: number) => {
    onChange(next)
    refs.current[Math.max(0, Math.min(5, focus))]?.focus()
    if (next.length === 6 && next !== value) onComplete?.(next)
  }
  const type = (i: number, raw: string) => {
    const digits = raw.replace(/\D/g, '')
    if (!digits) return
    const at = Math.min(i, value.length)
    // A multi-digit value means autofill or fast typing: spread it across the boxes.
    const current = value[at]
    const incoming = digits.length > 1 && current ? digits.replace(current, '') : digits
    update((value.slice(0, at) + incoming + value.slice(at + incoming.length)).slice(0, 6), at + incoming.length)
  }
  const paste = (e: ClipboardEvent) => {
    e.preventDefault()
    const digits = e.clipboardData.getData('text').replace(/\D/g, '').slice(0, 6)
    if (digits) update(digits, digits.length)
  }
  const key = (i: number, e: KeyboardEvent) => {
    if (e.key === 'Backspace') {
      e.preventDefault()
      const at = value[i] ? i : i - 1
      if (at >= 0) update(value.slice(0, at) + value.slice(at + 1), at)
    }
    if (e.key === 'ArrowLeft') refs.current[i - 1]?.focus()
    if (e.key === 'ArrowRight') refs.current[i + 1]?.focus()
  }
  return (
    <fieldset>
      <legend className="label mb-1.5">6-digit code</legend>
      <div className="grid grid-cols-6 gap-2">
        {[0, 1, 2, 3, 4, 5].map((i) => (
          <input
            key={i} ref={(el) => { refs.current[i] = el }} className="input code-box" inputMode="numeric"
            autoComplete={i === 0 ? 'one-time-code' : 'off'} aria-label={`Digit ${i + 1}`}
            value={value[i] ?? ''} onChange={(e) => type(i, e.target.value)} onKeyDown={(e) => key(i, e)} onPaste={paste} onFocus={(e) => e.target.select()}
          />
        ))}
      </div>
    </fieldset>
  )
}

function useCooldown(seconds: number) {
  // The first code was sent just before this mounted, so the cooldown starts running.
  const [until, setUntil] = useState(() => Date.now() + seconds * 1000)
  const [now, setNow] = useState(() => Date.now())
  const left = Math.max(0, Math.ceil((until - now) / 1000))
  useEffect(() => {
    if (!left) return
    const timer = setTimeout(() => setNow(Date.now()), 250)
    return () => clearTimeout(timer)
  }, [left, now])
  return { left, start: () => { const t = Date.now(); setNow(t); setUntil(t + seconds * 1000) } }
}

/** The backend refuses to send another email code within 30 seconds. */
export function ResendEmail({ send }: { send: () => Promise<unknown> }) {
  const cooldown = useCooldown(30)
  const toast = useToast()
  const resend = useMutation({
    mutationFn: send,
    onSuccess: () => { cooldown.start(); toast('A new code is on its way.') },
    onError: (e: Error) => {
      if (e instanceof ApiError && e.status === 429) cooldown.start()
      toast(e.message, 'error')
    },
  })
  return (
    <div className="flex items-center justify-between gap-3 text-sm text-muted">
      <span>Didn’t get it?</span>
      <Button variant="ghost" size="sm" disabled={cooldown.left > 0} loading={resend.isPending} onClick={() => resend.mutate()}>
        {cooldown.left > 0 ? <span className="tabular-nums">Resend in {cooldown.left}s</span> : 'Resend code'}
      </Button>
    </div>
  )
}
