import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import QRCode from 'qrcode'
import { Check, Copy, Download, KeyRound, Mail, ShieldCheck, ShieldOff, Smartphone } from 'lucide-react'
import { authApi } from '../../api/auth'
import { TWO_FACTOR, type UserResponseDto } from '../../api/authTypes'
import { USE_MOCK } from '../../api/client'
import { profileQueryKey } from '../../api/users'
import { saveBlob } from '../../lib/utils'
import { Badge, Button, FormError, Input, SectionTitle, Skeleton } from '../../components/ui/core'
import { Modal, useToast } from '../../components/ui/overlay'
import { CodeInput } from '../auth/components'

function RecoveryCodesModal({ codes, onClose }: { codes: string[] | null; onClose: () => void }) {
  const toast = useToast()
  const [copied, setCopied] = useState(false)
  // Keeps the list in place while the modal animates out.
  const [shown, setShown] = useState<string[]>([])
  if (codes && codes !== shown) setShown(codes)
  const text = shown.join('\n')
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
      {shown.length ? (
        <ol className="grid grid-cols-2 gap-2">
          {shown.map((c, i) => <motion.li key={c} className="code-chip" initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.05 * i }}>{c}</motion.li>)}
        </ol>
      ) : <p className="text-sm text-muted">The server returned no recovery codes.</p>}
    </Modal>
  )
}

/** Mounted only while open, so each setup starts with a clean form. */
function TotpSetupModal({ onClose, onDone }: { onClose: () => void; onDone: (codes: string[]) => void }) {
  const [code, setCode] = useState('')
  const [qr, setQr] = useState<{ src?: string; failed?: boolean }>({})
  // The backend reuses a pending secret, so repeating this request is harmless.
  const setup = useQuery({ queryKey: ['totp-setup'], queryFn: authApi.beginTotpSetup, staleTime: Infinity, gcTime: 0, retry: false })
  const enable = useMutation({
    mutationFn: async () => {
      if (!/^\d{6}$/.test(code)) throw new Error('Enter the 6-digit code.')
      return authApi.enableTotp(code)
    },
    onSuccess: (result) => onDone(result.recoveryCodes),
  })
  const uri = setup.data?.otpAuthUri
  useEffect(() => {
    if (!uri) return
    let active = true
    QRCode.toDataURL(uri, { width: 360, margin: 1, color: { dark: '#1f1d1a', light: '#ffffff' } })
      .then((src) => { if (active) setQr({ src }) })
      .catch(() => { if (active) setQr({ failed: true }) })
    return () => { active = false }
  }, [uri])
  return (
    <Modal open onClose={onClose} onSubmit={() => { if (setup.data && !enable.isPending) enable.mutate() }} title="Set up authenticator app"
      footer={<><Button variant="ghost" onClick={onClose}>Cancel</Button><Button type="submit" variant="primary" disabled={!setup.data} loading={enable.isPending}>Turn on</Button></>}>
      {setup.isError ? (<>
        <FormError error={setup.error} />
        <Button variant="outline" loading={setup.isFetching} onClick={() => setup.refetch()}>Try again</Button>
      </>) : (<>
        <p className="text-sm text-muted">Scan the QR code with your authenticator app, then enter the code it shows.</p>
        {qr.failed ? <p className="text-center text-sm text-muted">The QR code couldn’t be drawn. Use the key below.</p> : (
          <div className="mx-auto rounded-xl border border-line bg-white p-2">{qr.src ? <img src={qr.src} alt="Two-factor setup QR code" width={180} height={180} /> : <Skeleton className="size-[180px]" />}</div>
        )}
        {setup.data && <p className="break-all text-center text-xs text-muted">Can’t scan? Enter this key: <code className="font-semibold tracking-wider text-ink">{setup.data.secret}</code></p>}
        <CodeInput value={code} onChange={setCode} />
        <FormError error={enable.error} />
        {USE_MOCK && <p className="demo-hint !mt-0">Demo mode: any 6 digits except 000000.</p>}
      </>)}
    </Modal>
  )
}

type PasswordAction = 'enable-email' | 'disable' | 'recovery-codes'

const PASSWORD_ACTIONS: Record<PasswordAction, {
  title: string
  description: (email: string) => string
  confirmLabel: string
  danger?: boolean
  success: string
  run: (password: string) => Promise<string[] | null>
}> = {
  'enable-email': {
    title: 'Turn on email codes',
    description: (email) => `Each sign-in will need a code sent to ${email}. It turns on without a test message, so make sure you can receive mail there.`,
    confirmLabel: 'Turn on', success: 'Two-factor authentication is on',
    run: async (password) => (await authApi.enableEmailTwoFactor(password)).recoveryCodes,
  },
  disable: {
    title: 'Turn off two-factor authentication?',
    description: () => 'Your account will be protected by your password only, and your recovery codes will stop working.',
    confirmLabel: 'Turn off', danger: true, success: 'Two-factor authentication turned off',
    run: async (password) => { await authApi.disableTwoFactor(password); return null },
  },
  'recovery-codes': {
    title: 'Generate new recovery codes?',
    description: () => 'Your previous codes will stop working.',
    confirmLabel: 'Generate codes', success: 'New recovery codes generated',
    run: async (password) => (await authApi.regenerateRecoveryCodes(password)).recoveryCodes,
  },
}

/** The backend asks for the current password before changing how the account is protected. */
function PasswordConfirmModal({ action, email, onClose, onDone }: {
  action: PasswordAction; email: string; onClose: () => void; onDone: (codes: string[] | null, message: string) => void
}) {
  const [password, setPassword] = useState('')
  const config = PASSWORD_ACTIONS[action]
  const confirm = useMutation({
    mutationFn: async () => {
      if (!password) throw new Error('Enter your password.')
      return config.run(password)
    },
    onSuccess: (codes) => onDone(codes, config.success),
  })
  return (
    <Modal open onClose={onClose} onSubmit={() => { if (!confirm.isPending) confirm.mutate() }} title={config.title} description={config.description(email)}
      footer={<><Button variant="ghost" onClick={onClose}>Cancel</Button><Button type="submit" variant={config.danger ? 'danger' : 'primary'} loading={confirm.isPending}>{config.confirmLabel}</Button></>}>
      <Input label="Current password" type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} data-autofocus />
      <FormError error={confirm.error} />
    </Modal>
  )
}

export function TwoFactorSettings({ profile }: { profile: UserResponseDto }) {
  const client = useQueryClient()
  const toast = useToast()
  const [dialog, setDialog] = useState<'totp' | PasswordAction | null>(null)
  const [codes, setCodes] = useState<string[] | null>(null)
  const close = () => {
    setDialog(null)
    // A failed attempt may mean the status changed elsewhere.
    void client.invalidateQueries({ queryKey: profileQueryKey })
  }
  const done = (newCodes: string[] | null, message: string) => {
    close()
    setCodes(newCodes)
    toast(message)
  }
  const active = profile.is2faEnabled ? profile.twoFactorType : TWO_FACTOR.none
  const options = [
    { type: TWO_FACTOR.authenticator, dialog: 'totp' as const, icon: Smartphone, title: 'Authenticator app', text: 'Time-based codes (TOTP) from an app on your phone.' },
    { type: TWO_FACTOR.email, dialog: 'enable-email' as const, icon: Mail, title: 'Email codes', text: `A one-time code sent to ${profile.email}.` },
  ]
  return (
    <section className="card p-5">
      <SectionTitle title="Two-factor authentication" action={profile.is2faEnabled ? <Badge tone="sage" dot>On</Badge> : <Badge tone="amber" dot>Off</Badge>} />
      <div className="grid gap-3 sm:grid-cols-2">
        {options.map((o) => (
          <div key={o.type} className={`row-card flex flex-col gap-2 p-4 ${active === o.type ? 'selected-card' : ''}`}>
            <span className="empty-icon !size-10"><o.icon size={17} /></span>
            <p className="text-sm font-semibold">{o.title}</p><p className="flex-1 text-xs text-muted">{o.text}</p>
            {active === o.type
              ? <span className="flex items-center gap-1.5 text-sm font-medium"><ShieldCheck size={15} className="text-accent" /> Active method</span>
              : <Button variant="outline" size="sm" disabled={profile.is2faEnabled} onClick={() => setDialog(o.dialog)}>Set up</Button>}
          </div>
        ))}
      </div>
      {profile.is2faEnabled && (<>
        <p className="mt-3 text-xs text-muted">To change method, turn two-factor authentication off first.</p>
        <div className="mt-3 flex flex-wrap gap-2">
          <Button icon={<KeyRound size={15} />} onClick={() => setDialog('recovery-codes')}>New recovery codes</Button>
          <Button variant="ghost" className="text-danger" icon={<ShieldOff size={15} />} onClick={() => setDialog('disable')}>Turn off</Button>
        </div>
      </>)}
      {dialog === 'totp' && <TotpSetupModal onClose={close} onDone={(c) => done(c, 'Two-factor authentication is on')} />}
      {dialog && dialog !== 'totp' && <PasswordConfirmModal action={dialog} email={profile.email} onClose={close} onDone={done} />}
      <RecoveryCodesModal codes={codes} onClose={() => setCodes(null)} />
    </section>
  )
}
