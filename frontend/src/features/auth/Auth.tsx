import { useEffect, useRef, useState, type FormEvent, type ReactNode, type ClipboardEvent, type KeyboardEvent } from 'react'
import { Link, useLocation, useNavigate, Navigate } from 'react-router'
import { useMutation } from '@tanstack/react-query'
import { AnimatePresence, motion } from 'framer-motion'
import { ArrowLeft, CheckCircle2, KeyRound, Mail, ShieldCheck, Smartphone } from 'lucide-react'
import { authApi } from '../../api/services'
import { USE_MOCK } from '../../api/client'
import type { AuthResult } from '../../api/types'
import { Button, FormError, Input, Segmented } from '../../components/ui/core'
import { useToast } from '../../components/ui/overlay'
import { useEdition } from '../../editions'
import { AuthFrame } from './AuthFrame'

type SignIn = (token: string) => void

function Head({ title, sub }: { title: string; sub: ReactNode }) {
  return (
    <div className="mb-6">
      <h1 className="page-title">{title}</h1>
      <p className="mt-2 text-sm text-muted">{sub}</p>
    </div>
  )
}
const Hint = ({ children }: { children: ReactNode }) => (USE_MOCK ? <p className="demo-hint">{children}</p> : null)
const submit = (fn: () => void) => (e: FormEvent) => { e.preventDefault(); fn() }

function useAuthStep(signIn: SignIn) {
  const navigate = useNavigate()
  return (r: AuthResult) => {
    if (r.requires2fa) navigate('/two-factor', { state: { challengeId: r.challengeId, method: r.method } })
    else if (r.token) { signIn(r.token); navigate('/') }
  }
}

export function Login({ signIn }: { signIn: SignIn }) {
  const next = useAuthStep(signIn)
  const [email, setEmail] = useState(USE_MOCK ? 'demo@example.com' : '')
  const [password, setPassword] = useState(USE_MOCK ? 'demo-password' : '')
  const login = useMutation({ mutationFn: () => authApi.login(email, password), onSuccess: next })
  return (
    <AuthFrame>
      <Head title="Welcome back" sub="Sign in to pick up where your team left off." />
      <form className="flex flex-col gap-4" onSubmit={submit(() => login.mutate())} noValidate>
        <Input label="Email" type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} />
        <Input label="Password" type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} />
        <Link to="/reset" className="link -mt-2 self-end text-sm">Forgot password?</Link>
        <FormError error={login.error} />
        <Button type="submit" variant="primary" size="lg" loading={login.isPending}>Sign in</Button>
      </form>
      <p className="mt-5 text-center text-sm text-muted">New here? <Link to="/register" className="link">Create an account</Link></p>
      <Hint>Demo mode: the fields are pre-filled. Any email and 6+ character password works.</Hint>
    </AuthFrame>
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

export function Register({ signIn }: { signIn: SignIn }) {
  const next = useAuthStep(signIn)
  const [form, setForm] = useState({ name: '', email: '', password: '', confirm: '' })
  const set = (k: keyof typeof form) => (e: { target: { value: string } }) => setForm({ ...form, [k]: e.target.value })
  const register = useMutation({
    mutationFn: async () => {
      if (form.password.length < 8) throw new Error('Use at least 8 characters for your password.')
      if (form.password !== form.confirm) throw new Error('The passwords don’t match.')
      return authApi.register(form.name, form.email, form.password)
    },
    onSuccess: next,
  })
  return (
    <AuthFrame>
      <Head title="Create your account" sub="Free for small teams. No card needed." />
      <form className="flex flex-col gap-4" onSubmit={submit(() => register.mutate())} noValidate>
        <Input label="Full name" autoComplete="name" value={form.name} onChange={set('name')} />
        <Input label="Work email" type="email" autoComplete="email" value={form.email} onChange={set('email')} />
        <Input label="Password" type="password" autoComplete="new-password" value={form.password} onChange={set('password')} />
        <PasswordMeter value={form.password} />
        <Input label="Confirm password" type="password" autoComplete="new-password" value={form.confirm} onChange={set('confirm')} />
        <FormError error={register.error} />
        <Button type="submit" variant="primary" size="lg" loading={register.isPending}>Create account</Button>
      </form>
      <p className="mt-5 text-center text-sm text-muted">Already have an account? <Link to="/login" className="link">Sign in</Link></p>
    </AuthFrame>
  )
}

export function ResetPassword() {
  const [step, setStep] = useState<'email' | 'code' | 'done'>('email')
  const [email, setEmail] = useState('')
  const [code, setCode] = useState('')
  const [password, setPassword] = useState('')
  const request = useMutation({ mutationFn: () => authApi.requestReset(email), onSuccess: () => setStep('code') })
  const reset = useMutation({ mutationFn: () => authApi.resetPassword(code, password), onSuccess: () => setStep('done') })
  return (
    <AuthFrame>
      <AnimatePresence mode="wait">
        <motion.div key={step} initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, x: -20 }} transition={{ duration: 0.2 }}>
          {step === 'email' && (<>
            <Head title="Reset your password" sub="We’ll email you a 6-digit code." />
            <form className="flex flex-col gap-4" onSubmit={submit(() => request.mutate())} noValidate>
              <Input label="Email" type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} />
              <FormError error={request.error} />
              <Button type="submit" variant="primary" size="lg" loading={request.isPending}>Send code</Button>
            </form>
          </>)}
          {step === 'code' && (<>
            <Head title="Check your inbox" sub={<>If an account exists for <strong className="text-ink">{email}</strong>, a code is on its way.</>} />
            <form className="flex flex-col gap-4" onSubmit={submit(() => reset.mutate())} noValidate>
              <CodeInput value={code} onChange={setCode} />
              <Input label="New password" type="password" autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} />
              <PasswordMeter value={password} />
              <FormError error={reset.error} />
              <Button type="submit" variant="primary" size="lg" loading={reset.isPending}>Set new password</Button>
              <Button variant="ghost" onClick={() => setStep('email')}>Use a different email</Button>
            </form>
            <Hint>Demo mode: any 6 digits except 000000.</Hint>
          </>)}
          {step === 'done' && (
            <div className="flex flex-col items-center gap-3 py-4 text-center">
              <motion.span initial={{ scale: 0 }} animate={{ scale: 1 }} transition={{ type: 'spring', stiffness: 260, damping: 14 }} className="empty-icon"><CheckCircle2 size={22} /></motion.span>
              <Head title="Password updated" sub="You can sign in with your new password now." />
            </div>
          )}
        </motion.div>
      </AnimatePresence>
      <Link to="/login" className="link mt-5 flex items-center justify-center gap-1.5 text-sm"><ArrowLeft size={14} /> Back to sign in</Link>
    </AuthFrame>
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
    const incoming = digits.length > 1 && value[at] ? digits.replace(value[at], '') : digits
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
  const [until, setUntil] = useState(0)
  const [, tick] = useState(0)
  const left = Math.max(0, Math.ceil((until - Date.now()) / 1000))
  useEffect(() => {
    if (!left) return
    const t = setTimeout(() => tick((n) => n + 1), 250)
    return () => clearTimeout(t)
  })
  return { left, start: () => setUntil(Date.now() + seconds * 1000) }
}

export function ResendEmail({ send }: { send: () => Promise<unknown> }) {
  const cooldown = useCooldown(30)
  const toast = useToast()
  const resend = useMutation({
    mutationFn: send,
    onSuccess: () => { cooldown.start(); toast('A new code is on its way.') },
    onError: (e: Error) => toast(e.message, 'error'),
  })
  const started = useRef(false)
  // The first code was just sent, so the cooldown starts as soon as the screen opens.
  useEffect(() => { if (!started.current) { started.current = true; cooldown.start() } })
  return (
    <div className="flex items-center justify-between gap-3 text-sm text-muted">
      <span>Didn’t get it?</span>
      <Button variant="ghost" size="sm" disabled={cooldown.left > 0} loading={resend.isPending} onClick={() => resend.mutate()}>
        {cooldown.left > 0 ? <span className="tabular-nums">Resend in {cooldown.left}s</span> : 'Resend code'}
      </Button>
    </div>
  )
}

export function TwoFactor({ signIn }: { signIn: SignIn }) {
  const state = useLocation().state as { challengeId?: string; method?: 'totp' | 'email' } | null
  const edition = useEdition()
  const next = useAuthStep(signIn)
  const [method, setMethod] = useState<'totp' | 'email'>(state?.method ?? 'totp')
  const [recovery, setRecovery] = useState(false)
  const [code, setCode] = useState('')
  const verify = useMutation({
    mutationFn: (value: string) => authApi.verify2fa(state!.challengeId!, value, recovery ? 'recovery' : method),
    onSuccess: next,
  })
  if (!state?.challengeId) return <Navigate to="/login" replace />
  const reset = () => { setCode(''); verify.reset() }
  const Icon = recovery ? KeyRound : method === 'totp' ? Smartphone : Mail
  return (
    <AuthFrame>
      <motion.span key={Icon.displayName} initial={{ scale: 0.6, opacity: 0 }} animate={{ scale: 1, opacity: 1 }} transition={edition.spring} className="empty-icon mb-4"><Icon size={20} /></motion.span>
      <Head
        title="Two-factor check"
        sub={recovery ? 'Enter one of the recovery codes you saved.' : method === 'totp' ? 'Enter the code from your authenticator app.' : 'Enter the code we sent to your email.'}
      />
      {!recovery && (
        <div className="mb-5">
          <Segmented label="Verification method" value={method} onChange={(m) => { setMethod(m); reset() }}
            options={[{ value: 'totp', label: <><ShieldCheck size={14} /> Authenticator</> }, { value: 'email', label: <><Mail size={14} /> Email</> }]} />
        </div>
      )}
      <form className="flex flex-col gap-4" onSubmit={submit(() => verify.mutate(code))} noValidate>
        {recovery
          ? <Input label="Recovery code" placeholder="abcde-12345" value={code} onChange={(e) => setCode(e.target.value.trim())} autoFocus />
          : <CodeInput value={code} onChange={setCode} onComplete={(v) => verify.mutate(v)} />}
        {method === 'email' && !recovery && <ResendEmail send={() => authApi.resendEmailCode(state.challengeId!)} />}
        <FormError error={verify.error} />
        <Button type="submit" variant="primary" size="lg" loading={verify.isPending}>Verify and continue</Button>
      </form>
      <div className="mt-4 flex flex-wrap items-center justify-between gap-2">
        <Link to="/login" className="link flex items-center gap-1.5 text-sm"><ArrowLeft size={14} /> Back</Link>
        <button type="button" className="link text-sm" onClick={() => { setRecovery(!recovery); reset() }}>
          {recovery ? 'Use a 6-digit code instead' : 'Use a recovery code'}
        </button>
      </div>
      <Hint>{recovery ? 'Demo mode: try abcde-12345.' : 'Demo mode: any 6 digits except 000000.'}</Hint>
    </AuthFrame>
  )
}
