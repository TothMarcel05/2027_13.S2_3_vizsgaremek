import { useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate, useSearchParams } from 'react-router'
import { useMutation } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { ArrowLeft, CheckCircle2, KeyRound, Mail, Smartphone } from 'lucide-react'
import { authApi } from '../../api/auth'
import { TWO_FACTOR, type LoginResultDto } from '../../api/authTypes'
import { USE_MOCK, sessionStore } from '../../api/client'
import { Button, FormError, Input } from '../../components/ui/core'
import { MOTION } from '../../lib/motion'
import { AuthFrame } from './AuthFrame'
import { CodeInput, Head, Hint, Notice, PasswordMeter, ResendEmail } from './components'
import * as check from './validation'

const submit = (fn: () => void) => (e: FormEvent) => { e.preventDefault(); fn() }

/** Router state handed from one auth screen to the next. */
interface LoginState { registeredEmail?: string }
interface TwoFactorState { twoFactorToken: string; twoFactorType: number }

function readTwoFactorState(state: unknown): TwoFactorState | null {
  if (typeof state !== 'object' || state === null) return null
  const { twoFactorToken, twoFactorType } = state as Partial<TwoFactorState>
  return typeof twoFactorToken === 'string' && typeof twoFactorType === 'number' ? { twoFactorToken, twoFactorType } : null
}

const BackToSignIn = () => (
  <Link to="/login" className="link mt-5 flex items-center justify-center gap-1.5 text-sm"><ArrowLeft size={14} /> Back to sign in</Link>
)

export function Login() {
  const navigate = useNavigate()
  const registered = (useLocation().state as LoginState | null)?.registeredEmail
  const [email, setEmail] = useState(registered ?? (USE_MOCK ? 'demo@example.com' : ''))
  const [password, setPassword] = useState(USE_MOCK && !registered ? 'demo-password' : '')
  const [expired] = useState(() => sessionStore.endReason() === 'expired')

  const next = (result: LoginResultDto) => {
    // Storing the session switches the app to its signed-in routes.
    if (result.auth) { sessionStore.set(result.auth); return }
    navigate('/two-factor', { state: { twoFactorToken: result.twoFactorToken, twoFactorType: result.twoFactorType } })
  }
  const login = useMutation({
    mutationFn: async () => {
      const address = email.trim()
      check.assertValid(check.email(address), check.required(password, 'Enter your password.'))
      const result = await authApi.login({ email: address, password })
      if (!result.auth && !(result.requiresTwoFactor && result.twoFactorToken)) {
        throw new Error('The server sent an unexpected sign-in response.')
      }
      return result
    },
    onSuccess: next,
  })

  return (
    <AuthFrame>
      <Head title="Welcome back" sub="Sign in to pick up where your team left off." />
      {registered && <Notice>Your account is ready. Sign in to continue.</Notice>}
      {expired && !registered && <p role="status" className="row-card mb-4 p-3 text-sm">Your session expired. Please sign in again.</p>}
      <form className="flex flex-col gap-4" onSubmit={submit(() => login.mutate())} noValidate>
        <Input label="Email" type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} />
        <Input label="Password" type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} />
        <Link to="/reset" className="link -mt-2 self-end text-sm">Forgot password?</Link>
        <FormError error={login.error} />
        <Button type="submit" variant="primary" size="lg" loading={login.isPending}>Sign in</Button>
      </form>
      <p className="mt-5 text-center text-sm text-muted">New here? <Link to="/register" className="link">Create an account</Link></p>
      <Hint>Demo mode: the fields are pre-filled. Any email and 5+ character password works.</Hint>
    </AuthFrame>
  )
}

export function Register() {
  const navigate = useNavigate()
  const [form, setForm] = useState({ username: '', email: '', password: '', confirm: '' })
  const set = (k: keyof typeof form) => (e: { target: { value: string } }) => setForm({ ...form, [k]: e.target.value })
  const register = useMutation({
    mutationFn: async () => {
      const dto = { username: form.username.trim(), email: form.email.trim(), password: form.password }
      check.assertValid(
        check.username(dto.username), check.registerEmail(dto.email),
        check.registerPassword(dto.password), check.matches(dto.password, form.confirm),
      )
      return authApi.register(dto)
    },
    // Registration returns the new user without tokens, so the next step is signing in.
    onSuccess: (user) => navigate('/login', { replace: true, state: { registeredEmail: user.email } satisfies LoginState }),
  })
  return (
    <AuthFrame>
      <Head title="Create your account" sub="Free for small teams. No card needed." />
      <form className="flex flex-col gap-4" onSubmit={submit(() => register.mutate())} noValidate>
        <Input label="Username" hint="5–30 characters." autoComplete="username" maxLength={30} value={form.username} onChange={set('username')} />
        <Input label="Work email" type="email" autoComplete="email" maxLength={100} value={form.email} onChange={set('email')} />
        <Input label="Password" hint="5–30 characters." type="password" autoComplete="new-password" maxLength={30} value={form.password} onChange={set('password')} />
        <PasswordMeter value={form.password} />
        <Input label="Confirm password" type="password" autoComplete="new-password" maxLength={30} value={form.confirm} onChange={set('confirm')} />
        <FormError error={register.error} />
        <Button type="submit" variant="primary" size="lg" loading={register.isPending}>Create account</Button>
      </form>
      <p className="mt-5 text-center text-sm text-muted">Already have an account? <Link to="/login" className="link">Sign in</Link></p>
    </AuthFrame>
  )
}

export function ForgotPassword() {
  const [email, setEmail] = useState('')
  const request = useMutation({
    mutationFn: async (address: string) => {
      check.assertValid(check.email(address))
      await authApi.requestPasswordReset(address)
      return address
    },
  })
  return (
    <AuthFrame>
      {request.isSuccess ? (<>
        <Head title="Check your inbox" sub={<>If an account exists for <strong className="text-ink">{request.data}</strong>, we’ve sent it a reset link. The link works for 10 minutes.</>} />
        <Button variant="ghost" className="w-full" onClick={() => request.reset()}>Use a different email</Button>
        <Hint>Demo mode sends no email. <Link className="link" to={`/reset-password?token=demo&email=${encodeURIComponent(request.data)}`}>Open the reset link</Link></Hint>
      </>) : (<>
        <Head title="Reset your password" sub="We’ll email you a link to choose a new one." />
        <form className="flex flex-col gap-4" onSubmit={submit(() => request.mutate(email.trim()))} noValidate>
          <Input label="Email" type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} />
          <FormError error={request.error} />
          <Button type="submit" variant="primary" size="lg" loading={request.isPending}>Send reset link</Button>
        </form>
      </>)}
      <BackToSignIn />
    </AuthFrame>
  )
}

/** Opened from the emailed link, which carries `token` and `email` in its query string. */
export function ResetPassword() {
  const [params] = useSearchParams()
  const navigate = useNavigate()
  const email = params.get('email') ?? ''
  const token = params.get('token') ?? ''
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const reset = useMutation({
    mutationFn: async () => {
      check.assertValid(check.newPassword(password), check.matches(password, confirm))
      await authApi.resetPassword(email, token, password)
    },
    onSuccess: () => {
      // If this browser is signed in to the same account, make it use the new password.
      if (sessionStore.get()?.user.email.toLowerCase() === email.toLowerCase()) {
        authApi.logout().catch(() => { /* the local session is already cleared */ })
      }
    },
  })

  if (!email || !token) return (
    <AuthFrame>
      <Head title="This link is incomplete" sub="Open the link from your email again, or ask for a new one." />
      <Button variant="primary" size="lg" className="w-full" onClick={() => navigate('/reset')}>Request a new link</Button>
      <BackToSignIn />
    </AuthFrame>
  )

  return (
    <AuthFrame>
      {reset.isSuccess ? (
        <div className="flex flex-col items-center gap-3 py-4 text-center" role="status">
          <motion.span initial={{ scale: 0 }} animate={{ scale: 1 }} transition={{ type: 'spring', stiffness: 260, damping: 14 }} className="empty-icon"><CheckCircle2 size={22} /></motion.span>
          <Head title="Password updated" sub="You can sign in with your new password now." />
        </div>
      ) : (<>
        <Head title="Choose a new password" sub={<>For <strong className="text-ink">{email}</strong></>} />
        <form className="flex flex-col gap-4" onSubmit={submit(() => reset.mutate())} noValidate>
          <Input label="New password" hint="8–100 characters." type="password" autoComplete="new-password" maxLength={100} value={password} onChange={(e) => setPassword(e.target.value)} />
          <PasswordMeter value={password} />
          <Input label="Confirm new password" type="password" autoComplete="new-password" maxLength={100} value={confirm} onChange={(e) => setConfirm(e.target.value)} />
          <FormError error={reset.error} />
          <Button type="submit" variant="primary" size="lg" loading={reset.isPending}>Set new password</Button>
          <Button variant="ghost" onClick={() => navigate('/reset')}>Request a new link</Button>
        </form>
      </>)}
      <BackToSignIn />
    </AuthFrame>
  )
}

export function TwoFactor() {
  const state = readTwoFactorState(useLocation().state)
  return state ? <TwoFactorForm {...state} /> : <Navigate to="/login" replace />
}

/** The account's own method decides which code is asked for; it can't be switched here. */
function TwoFactorForm({ twoFactorToken, twoFactorType }: TwoFactorState) {
  const byEmail = twoFactorType === TWO_FACTOR.email
  const [recovery, setRecovery] = useState(false)
  const [code, setCode] = useState('')
  const verify = useMutation({
    mutationFn: async (value: string) => {
      const normalized = recovery ? check.normalizeRecoveryCode(value) : /^\d{6}$/.test(value) ? value : null
      if (!normalized) throw new Error(recovery ? 'Recovery codes look like ABCD-2345.' : 'Enter the 6-digit code.')
      return authApi.verifyTwoFactor({ twoFactorToken, code: normalized })
    },
    onSuccess: (auth) => sessionStore.set(auth),
  })
  const send = (value: string) => { if (!verify.isPending) verify.mutate(value) }
  const toggle = () => { setRecovery(!recovery); setCode(''); verify.reset() }
  const Icon = recovery ? KeyRound : byEmail ? Mail : Smartphone
  return (
    <AuthFrame>
      <motion.span key={String(recovery)} initial={{ scale: 0.6, opacity: 0 }} animate={{ scale: 1, opacity: 1 }} transition={MOTION.spring} className="empty-icon mb-4"><Icon size={20} /></motion.span>
      <Head
        title="Two-factor check"
        sub={recovery ? 'Enter one of the recovery codes you saved.' : byEmail ? 'Enter the code we sent to your email.' : 'Enter the code from your authenticator app.'}
      />
      <form className="flex flex-col gap-4" onSubmit={submit(() => send(code))} noValidate>
        {recovery
          ? <Input label="Recovery code" placeholder="ABCD-2345" autoCapitalize="characters" value={code} onChange={(e) => setCode(e.target.value)} autoFocus />
          : <CodeInput value={code} onChange={setCode} onComplete={send} />}
        {byEmail && !recovery && <ResendEmail send={() => authApi.resendTwoFactorEmail(twoFactorToken)} />}
        <FormError error={verify.error} />
        <Button type="submit" variant="primary" size="lg" loading={verify.isPending}>Verify and continue</Button>
        <p className="text-center text-xs text-muted">This step expires after 5 minutes. After that, go back and sign in again.</p>
      </form>
      <div className="mt-4 flex flex-wrap items-center justify-between gap-2">
        <Link to="/login" className="link flex items-center gap-1.5 text-sm"><ArrowLeft size={14} /> Back</Link>
        <button type="button" className="link text-sm" onClick={toggle}>
          {recovery ? 'Use a 6-digit code instead' : 'Use a recovery code'}
        </button>
      </div>
      <Hint>{recovery ? 'Demo mode: try ABCD-2345.' : 'Demo mode: any 6 digits except 000000.'}</Hint>
    </AuthFrame>
  )
}
