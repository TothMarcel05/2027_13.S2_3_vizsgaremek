import { useState, type FormEvent } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { AnimatePresence, motion } from 'framer-motion'
import { ShieldCheck, UserRound } from 'lucide-react'
import { GLOBAL_ROLE, type UserResponseDto } from '../../api/authTypes'
import { profileQueryKey, userApi } from '../../api/users'
import { useMe, useProfile } from '../../lib/data'
import { fmtDate } from '../../lib/utils'
import { MOTION } from '../../lib/motion'
import { Avatar, Badge, Button, ErrorState, FormError, Input, PageHeader, SectionTitle, Segmented, Skeleton } from '../../components/ui/core'
import { useToast } from '../../components/ui/overlay'
import { PasswordMeter } from '../auth/components'
import * as check from '../auth/validation'
import { TwoFactorSettings } from './TwoFactorSettings'

/** Shows the profile picture, falling back to initials when there is none or it fails to load. */
function Picture({ url, name, hue }: { url: string | null; name: string; hue: number }) {
  const [failed, setFailed] = useState<string | null>(null)
  if (!url || failed === url) return <Avatar user={{ name: name || '?', hue }} size={60} />
  return <img src={url} alt="" width={60} height={60} className="size-[60px] shrink-0 rounded-full object-cover" referrerPolicy="no-referrer" onError={() => setFailed(url)} />
}

function Details({ profile }: { profile: UserResponseDto }) {
  const client = useQueryClient()
  const toast = useToast()
  const hue = useMe()?.hue ?? 30
  const [form, setForm] = useState({ name: profile.name, avatarUrl: profile.avatarUrl ?? '' })
  const dirty = form.name.trim() !== profile.name || form.avatarUrl.trim() !== (profile.avatarUrl ?? '')
  const save = useMutation({
    mutationFn: async () => {
      const name = form.name.trim()
      const avatarUrl = form.avatarUrl.trim()
      check.assertValid(check.displayName(name), check.avatarUrl(avatarUrl))
      // The backend rejects an empty string as a URL; null removes the picture.
      return userApi.updateMe({ name, avatarURL: avatarUrl || null })
    },
    onSuccess: ({ name, avatarUrl, updatedAt }) => {
      client.setQueryData<UserResponseDto>(profileQueryKey, (old) => old && { ...old, name, avatarUrl, updatedAt })
      setForm({ name, avatarUrl: avatarUrl ?? '' })
      toast('Profile saved')
    },
  })
  const submit = (e: FormEvent) => { e.preventDefault(); save.mutate() }
  return (
    <form className="card p-5" onSubmit={submit} noValidate>
      <SectionTitle title="Personal information" action={profile.globalRole === GLOBAL_ROLE.systemAdmin ? <Badge tone="accent">System admin</Badge> : undefined} />
      <div className="mb-4 flex items-center gap-4">
        <Picture url={profile.avatarUrl} name={profile.name} hue={hue} />
        <div className="min-w-0"><p className="truncate font-semibold">{profile.name}</p><p className="text-sm text-muted">Member since {fmtDate(profile.createdAt, { day: 'numeric', month: 'long', year: 'numeric' })}</p></div>
      </div>
      <div className="grid gap-4 sm:grid-cols-2">
        <Input label="Name" autoComplete="name" maxLength={100} value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
        <Input label="Email" type="email" hint="Your sign-in email can’t be changed here." value={profile.email} readOnly />
      </div>
      <div className="mt-4"><Input label="Picture address (optional)" type="url" placeholder="https://…" value={form.avatarUrl} onChange={(e) => setForm({ ...form, avatarUrl: e.target.value })} /></div>
      <div className="mt-4"><FormError error={save.error} /></div>
      <div className="mt-4 flex justify-end"><Button type="submit" variant="primary" disabled={!dirty} loading={save.isPending}>Save changes</Button></div>
    </form>
  )
}

function Password() {
  const toast = useToast()
  const empty = { current: '', next: '', confirm: '' }
  const [form, setForm] = useState(empty)
  const change = useMutation({
    mutationFn: async () => {
      check.assertValid(
        check.required(form.current, 'Enter your current password.'),
        check.newPassword(form.next),
        form.next === form.current ? 'The new password must differ from the current one.' : null,
        check.matches(form.next, form.confirm),
      )
      await userApi.changePassword({ currentPassword: form.current, newPassword: form.next })
    },
    onSuccess: () => { setForm(empty); toast('Password changed') },
  })
  const field = (k: keyof typeof form) => ({ value: form[k], onChange: (e: { target: { value: string } }) => setForm({ ...form, [k]: e.target.value }) })
  return (
    <form className="card flex flex-col gap-4 p-5" onSubmit={(e) => { e.preventDefault(); change.mutate() }} noValidate>
      <SectionTitle title="Change password" />
      <Input label="Current password" type="password" autoComplete="current-password" {...field('current')} />
      <Input label="New password" hint="8–100 characters." type="password" autoComplete="new-password" maxLength={100} {...field('next')} />
      <PasswordMeter value={form.next} />
      <Input label="Confirm new password" type="password" autoComplete="new-password" maxLength={100} {...field('confirm')} />
      <FormError error={change.error} />
      <Button type="submit" variant="primary" className="self-end" loading={change.isPending}>Update password</Button>
    </form>
  )
}

export function Profile() {
  const query = useProfile()
  const [tab, setTab] = useState<'profile' | 'security'>('profile')
  return (
    <div className="max-w-3xl">
      <PageHeader title="Profile settings" description="Your details and sign-in security." />
      <div className="mb-5">
        <Segmented label="Settings section" value={tab} onChange={setTab} options={[
          { value: 'profile', label: <><UserRound size={14} /> Profile</> }, { value: 'security', label: <><ShieldCheck size={14} /> Security</> },
        ]} />
      </div>
      {query.data ? (
        <AnimatePresence mode="wait">
          <motion.div key={tab} initial="hidden" animate="show" exit="exit" variants={MOTION.page} className="space-y-4">
            {tab === 'profile' && <Details profile={query.data} />}
            {tab === 'security' && <><TwoFactorSettings profile={query.data} /><Password /></>}
          </motion.div>
        </AnimatePresence>
      ) : query.isError ? <ErrorState error={query.error} retry={() => void query.refetch()} />
        : <div role="status" aria-label="Loading profile"><Skeleton className="h-80" /></div>}
    </div>
  )
}
