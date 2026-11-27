import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { AnimatePresence, motion } from 'framer-motion'
import QRCode from 'qrcode'
import { Check, Copy, Download, KeyRound, Laptop, Mail, Monitor, ShieldCheck, ShieldOff, Smartphone, UserRound } from 'lucide-react'
import { meApi, authApi } from '../../api/services'
import { USE_MOCK } from '../../api/client'
import type { User } from '../../api/types'
import { useAction, useMe } from '../../lib/data'
import { saveBlob, timeAgo } from '../../lib/utils'
import { useEdition } from '../../editions'
import { Avatar, Badge, Button, FormError, Input, PageHeader, SectionTitle, Segmented, Skeleton, Textarea } from '../../components/ui/core'
import { Modal, useConfirm, useToast } from '../../components/ui/overlay'
import { CodeInput, PasswordMeter, ResendEmail } from '../auth/Auth'

export function RecoveryCodesModal({ codes, onClose }: { codes: string[] | null; onClose: () => void }) {
  const toast = useToast()
  const [copied, setCopied] = useState(false)
  const text = codes?.join('\n') ?? ''
  const copy = async () => {
    try { await navigator.clipboard.writeText(text); setCopied(true); setTimeout(() => setCopied(false), 1800) }
    catch { toast('Couldn’t reach the clipboard. Download the codes instead.', 'error') }
  }
  return (
    <Modal open={!!codes} onClose={onClose} title="Recovery codes" description="Each code works once if you lose your second factor. Store them somewhere safe; they won’t be shown again."
      footer={<>
        <Button variant="outline" icon={copied ? <Check size={15} /> : <Copy size={15} />} onClick={copy}>{copied ? 'Copied' : 'Copy all'}</Button>
        <Button variant="outline" icon={<Download size={15} />} onClick={() => saveBlob(new Blob([text], { type: 'text/plain' }), 'recovery-codes.txt')}>Download</Button>
        <Button variant="primary" onClick={onClose}>I’ve saved them</Button>
      </>}>
      <ol className="grid grid-cols-2 gap-2">
        {codes?.map((c, i) => <motion.li key={c} className="code-chip" initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.05 * i }}>{c}</motion.li>)}
      </ol>
    </Modal>
  )
}

function TwoFactorSetup({ method, onClose, onDone }: { method: 'totp' | 'email' | null; onClose: () => void; onDone: (codes: string[]) => void }) {
  const [code, setCode] = useState('')
  const [qr, setQr] = useState('')
  const [shown, setShown] = useState<'totp' | 'email'>('totp')
  const setup = useQuery({ queryKey: ['totp-setup'], queryFn: meApi.setupTotp, enabled: method === 'totp', staleTime: Infinity, gcTime: 0 })
  const enable = useAction((value: string) => meApi.enable2fa(shown, value), { success: 'Two-factor authentication is on', silentError: true, onSuccess: (r) => onDone(r.recoveryCodes) })
  useEffect(() => { if (method) { setShown(method); setCode(''); enable.reset() } }, [method])
  useEffect(() => {
    if (setup.data) QRCode.toDataURL(setup.data.otpauthUrl, { width: 360, margin: 1, color: { dark: '#1f1d1a', light: '#ffffff' } }).then(setQr)
  }, [setup.data])
  return (
    <Modal open={!!method} onClose={onClose} onSubmit={() => enable.mutate(code)} title={shown === 'totp' ? 'Set up authenticator app' : 'Set up email codes'}
      footer={<><Button variant="ghost" onClick={onClose}>Cancel</Button><Button type="submit" variant="primary" loading={enable.isPending}>Turn on</Button></>}>
      {shown === 'totp' ? (<>
        <p className="text-sm text-muted">Scan the QR code with your authenticator app, then enter the code it shows.</p>
        <div className="mx-auto rounded-xl border border-line bg-white p-2">{qr ? <img src={qr} alt="Two-factor setup QR code" width={180} height={180} /> : <Skeleton className="size-[180px]" />}</div>
        {setup.data && <p className="text-center text-xs text-muted">Can’t scan? Enter this key: <code className="font-semibold tracking-wider text-ink">{setup.data.secret}</code></p>}
      </>) : <p className="text-sm text-muted">We sent a code to your email. Enter it to confirm.</p>}
      <CodeInput value={code} onChange={setCode} />
      {shown === 'email' && method && <ResendEmail send={() => authApi.resendEmailCode('setup')} />}
      <FormError error={enable.error} />
      {USE_MOCK && <p className="demo-hint !mt-0">Demo mode: any 6 digits except 000000.</p>}
    </Modal>
  )
}

function Details({ me }: { me: User }) {
  const [form, setForm] = useState({ name: me.name, email: me.email, title: me.title, bio: me.bio })
  const save = useAction(() => meApi.update(form), { success: 'Profile saved' })
  const dirty = (Object.keys(form) as (keyof typeof form)[]).some((k) => form[k] !== me[k])
  const field = (k: keyof typeof form) => ({ value: form[k], onChange: (e: { target: { value: string } }) => setForm({ ...form, [k]: e.target.value }) })
  return (
    <form className="card p-5" onSubmit={(e) => { e.preventDefault(); save.mutate() }}>
      <SectionTitle title="Personal information" />
      <div className="mb-4 flex items-center gap-4"><Avatar user={{ name: form.name || '?', hue: me.hue }} size={60} /><div><p className="font-semibold">{form.name}</p><p className="text-sm text-muted">{form.title}</p></div></div>
      <div className="grid gap-4 sm:grid-cols-2"><Input label="Full name" required {...field('name')} /><Input label="Email" type="email" required {...field('email')} /></div>
      <div className="mt-4 grid gap-4"><Input label="Job title" {...field('title')} /><Textarea label="Bio" {...field('bio')} /></div>
      <div className="mt-4 flex justify-end"><Button type="submit" variant="primary" disabled={!dirty || !form.name.trim()} loading={save.isPending}>Save changes</Button></div>
    </form>
  )
}

function Password() {
  const empty = { current: '', next: '', confirm: '' }
  const [form, setForm] = useState(empty)
  const change = useAction(async () => {
    if (form.next !== form.confirm) throw new Error('The new passwords don’t match.')
    await meApi.changePassword(form.current, form.next)
  }, { success: 'Password changed', silentError: true, onSuccess: () => setForm(empty) })
  return (
    <form className="card flex flex-col gap-4 p-5" onSubmit={(e) => { e.preventDefault(); change.mutate() }} noValidate>
      <SectionTitle title="Change password" />
      <Input label="Current password" type="password" autoComplete="current-password" value={form.current} onChange={(e) => setForm({ ...form, current: e.target.value })} />
      <Input label="New password" type="password" autoComplete="new-password" value={form.next} onChange={(e) => setForm({ ...form, next: e.target.value })} />
      <PasswordMeter value={form.next} />
      <Input label="Confirm new password" type="password" autoComplete="new-password" value={form.confirm} onChange={(e) => setForm({ ...form, confirm: e.target.value })} />
      <FormError error={change.error} />
      <Button type="submit" variant="primary" className="self-end" loading={change.isPending}>Update password</Button>
    </form>
  )
}

function Security({ me }: { me: User }) {
  const confirm = useConfirm()
  const [setting, setSetting] = useState<'totp' | 'email' | null>(null)
  const [codes, setCodes] = useState<string[] | null>(null)
  const disable = useAction(() => meApi.disable2fa(), { success: 'Two-factor authentication turned off' })
  const regenerate = useAction(() => meApi.recoveryCodes(), { onSuccess: (r) => setCodes(r.recoveryCodes) })
  const options = [
    { id: 'totp' as const, icon: Smartphone, title: 'Authenticator app', text: 'Time-based codes (TOTP) from an app on your phone.' },
    { id: 'email' as const, icon: Mail, title: 'Email codes', text: `A one-time code sent to ${me.email}.` },
  ]
  return (
    <section className="card p-5">
      <SectionTitle title="Two-factor authentication" action={me.twoFactor ? <Badge tone="sage" dot>On</Badge> : <Badge tone="amber" dot>Off</Badge>} />
      <div className="grid gap-3 sm:grid-cols-2">
        {options.map((o) => (
          <div key={o.id} className={`row-card flex flex-col gap-2 p-4 ${me.twoFactor === o.id ? 'selected-card' : ''}`}>
            <span className="empty-icon !size-10"><o.icon size={17} /></span>
            <p className="text-sm font-semibold">{o.title}</p><p className="flex-1 text-xs text-muted">{o.text}</p>
            {me.twoFactor === o.id ? <span className="flex items-center gap-1.5 text-sm font-medium"><ShieldCheck size={15} className="text-accent" /> Active method</span>
              : <Button variant="outline" size="sm" onClick={() => setSetting(o.id)}>{me.twoFactor ? 'Switch to this' : 'Set up'}</Button>}
          </div>
        ))}
      </div>
      {me.twoFactor && (
        <div className="mt-4 flex flex-wrap gap-2">
          <Button icon={<KeyRound size={15} />} loading={regenerate.isPending} onClick={async () => {
            if (await confirm({ title: 'Generate new recovery codes?', message: 'Your previous codes will stop working.', confirmLabel: 'Generate codes' })) regenerate.mutate()
          }}>Recovery codes</Button>
          <Button variant="ghost" className="text-danger" icon={<ShieldOff size={15} />} onClick={async () => {
            if (await confirm({ title: 'Turn off two-factor authentication?', message: 'Your account will be protected by your password only.', confirmLabel: 'Turn off', danger: true })) disable.mutate()
          }}>Turn off</Button>
        </div>
      )}
      <TwoFactorSetup method={setting} onClose={() => setSetting(null)} onDone={(c) => { setSetting(null); setCodes(c) }} />
      <RecoveryCodesModal codes={codes} onClose={() => setCodes(null)} />
    </section>
  )
}

function Sessions() {
  const query = useQuery({ queryKey: ['sessions'], queryFn: meApi.sessions })
  const revoke = useAction((id: string) => meApi.revokeSession(id), { success: 'Session signed out' })
  const revokeAll = useAction(() => meApi.revokeOtherSessions(), { success: 'Other sessions signed out' })
  const list = query.data ?? []
  return (
    <section className="card p-5">
      <SectionTitle title="Active sessions" action={<Button size="sm" variant="outline" disabled={list.length < 2} loading={revokeAll.isPending} onClick={() => revokeAll.mutate()}>Sign out others</Button>} />
      {query.isPending && <div className="space-y-2"><Skeleton className="h-14" /><Skeleton className="h-14" /></div>}
      <ul className="divide-y divide-line">
        <AnimatePresence initial={false}>
          {list.map((s) => (
            <motion.li key={s.id} layout exit={{ opacity: 0, height: 0 }} className="flex items-center gap-3 py-3">
              {/iPhone|Android/.test(s.device) ? <Smartphone size={18} className="text-muted" /> : /macOS/.test(s.device) ? <Laptop size={18} className="text-muted" /> : <Monitor size={18} className="text-muted" />}
              <div className="min-w-0 flex-1"><p className="text-sm font-medium">{s.device}</p><p className="text-xs text-muted">{s.location} · {s.current ? 'active now' : timeAgo(s.lastActive)}</p></div>
              {s.current ? <Badge tone="sage">This device</Badge> : <Button size="sm" variant="ghost" onClick={() => revoke.mutate(s.id)}>Sign out</Button>}
            </motion.li>
          ))}
        </AnimatePresence>
      </ul>
    </section>
  )
}

export function Profile() {
  const me = useMe()
  const edition = useEdition()
  const [tab, setTab] = useState<'profile' | 'security' | 'sessions'>('profile')
  return (
    <div className="max-w-3xl">
      <PageHeader title="Profile settings" description="Your details, sign-in security and devices." />
      <div className="mb-5">
        <Segmented label="Settings section" value={tab} onChange={setTab} options={[
          { value: 'profile', label: <><UserRound size={14} /> Profile</> }, { value: 'security', label: <><ShieldCheck size={14} /> Security</> }, { value: 'sessions', label: <><Monitor size={14} /> Sessions</> },
        ]} />
      </div>
      {!me ? <Skeleton className="h-80" /> : (
        <AnimatePresence mode="wait">
          <motion.div key={tab} initial="hidden" animate="show" exit="exit" variants={edition.page} className="space-y-4">
            {tab === 'profile' && <Details me={me} />}
            {tab === 'security' && <><Security me={me} /><Password /></>}
            {tab === 'sessions' && <Sessions />}
          </motion.div>
        </AnimatePresence>
      )}
    </div>
  )
}
