import { forwardRef, useId, type ButtonHTMLAttributes, type CSSProperties, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes, type TextareaHTMLAttributes, type PointerEvent } from 'react'
import { motion, useMotionValue, useReducedMotion, useSpring, type HTMLMotionProps } from 'framer-motion'
import { Loader2, AlertCircle } from 'lucide-react'
import { cn } from '../../lib/utils'
import { useEdition } from '../../editions'
import type { Priority, ProjectStatus, TaskStatus, User } from '../../api/types'

type ButtonProps = Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'onDrag' | 'onDragStart' | 'onDragEnd' | 'onAnimationStart'> & {
  variant?: 'primary' | 'soft' | 'ghost' | 'danger' | 'outline'
  size?: 'sm' | 'md' | 'lg'
  icon?: ReactNode
  loading?: boolean
}

/** Spring press everywhere; primary buttons also lean toward the pointer. */
export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  { variant = 'soft', size = 'md', icon, loading, className, children, disabled, type = 'button', ...rest }, ref,
) {
  const edition = useEdition()
  const reduce = useReducedMotion()
  const x = useMotionValue(0), y = useMotionValue(0)
  const sx = useSpring(x, { stiffness: 260, damping: 18 }), sy = useSpring(y, { stiffness: 260, damping: 18 })
  const magnetic = variant === 'primary' && !reduce && edition.magnet > 0
  const move = (e: PointerEvent<HTMLButtonElement>) => {
    if (!magnetic || e.pointerType !== 'mouse') return
    const r = e.currentTarget.getBoundingClientRect()
    x.set((e.clientX - r.left - r.width / 2) * edition.magnet)
    y.set((e.clientY - r.top - r.height / 2) * edition.magnet)
  }
  return (
    <motion.button
      ref={ref}
      type={type}
      style={{ x: sx, y: sy }}
      whileTap={disabled || reduce ? undefined : { scale: edition.press }}
      onPointerMove={move}
      onPointerLeave={() => { x.set(0); y.set(0) }}
      disabled={disabled || loading}
      className={cn('btn', `btn-${variant}`, `btn-${size}`, !children && 'btn-icon', className)}
      {...rest}
    >
      {loading ? <Loader2 size={15} className="animate-spin" /> : icon}
      {children}
    </motion.button>
  )
})

export function Field({ label, hint, children, className }: { label: string; hint?: string; children: (id: string) => ReactNode; className?: string }) {
  const id = useId()
  return (
    <div className={cn('flex flex-col gap-1.5', className)}>
      <label htmlFor={id} className="label">{label}</label>
      {children(id)}
      {hint && <p className="text-xs text-muted">{hint}</p>}
    </div>
  )
}

type InputProps = InputHTMLAttributes<HTMLInputElement> & { label?: string; hint?: string }
export function Input({ label, hint, className, ...rest }: InputProps) {
  if (!label) return <input className={cn('input', className)} {...rest} />
  return <Field label={label} hint={hint}>{(id) => <input id={id} className={cn('input', className)} {...rest} />}</Field>
}

export function Textarea({ label, className, ...rest }: TextareaHTMLAttributes<HTMLTextAreaElement> & { label: string }) {
  return <Field label={label}>{(id) => <textarea id={id} rows={3} className={cn('input resize-none', className)} {...rest} />}</Field>
}

export function Select({ label, className, children, ...rest }: SelectHTMLAttributes<HTMLSelectElement> & { label?: string }) {
  if (!label) return <select className={cn('input select', className)} {...rest}>{children}</select>
  return <Field label={label}>{(id) => <select id={id} className={cn('input select', className)} {...rest}>{children}</select>}</Field>
}

export function Card({ hover, className, children, ...rest }: HTMLMotionProps<'div'> & { hover?: boolean }) {
  return <motion.div className={cn('card', hover && 'card-hover', className)} {...rest}>{children}</motion.div>
}

export function Stagger({ className, children }: { className?: string; children: ReactNode }) {
  const edition = useEdition()
  return (
    <motion.div className={className} initial="hidden" animate="show" variants={{ hidden: {}, show: { transition: { staggerChildren: edition.stagger } } }}>
      {children}
    </motion.div>
  )
}
export function Item({ className, children, ...rest }: HTMLMotionProps<'div'>) {
  const edition = useEdition()
  return <motion.div variants={edition.item} className={className} {...rest}>{children}</motion.div>
}

export const hueStyle = (hue: number) => ({ '--h': hue }) as CSSProperties

export const initials = (name: string) => name.split(/\s+/).map((w) => w[0]).slice(0, 2).join('').toUpperCase()

export function Avatar({ user, size = 28, className }: { user?: Pick<User, 'name' | 'hue'>; size?: number; className?: string }) {
  return (
    <span
      className={cn('avatar', className)}
      style={{ ...hueStyle(user?.hue ?? 30), width: size, height: size, fontSize: size * 0.37 }}
      title={user?.name ?? 'Unassigned'}
      aria-label={user?.name ?? 'Unassigned'}
    >
      {user ? initials(user.name) : '?'}
    </span>
  )
}

export function AvatarStack({ users, max = 4 }: { users: (User | undefined)[]; max?: number }) {
  const list = users.filter(Boolean) as User[]
  return (
    <span className="flex items-center">
      {list.slice(0, max).map((u) => <Avatar key={u.id} user={u} size={26} className="-ml-1.5 first:ml-0 ring-2 ring-surface" />)}
      {list.length > max && <span className="ml-1.5 text-xs text-muted">+{list.length - max}</span>}
    </span>
  )
}

export type Tone = 'neutral' | 'accent' | 'sage' | 'amber' | 'red' | 'blue'
export function Badge({ tone = 'neutral', dot, children }: { tone?: Tone; dot?: boolean; children: ReactNode }) {
  return <span className={cn('badge', `tone-${tone}`)}>{dot && <span className="size-1.5 rounded-full bg-current" />}{children}</span>
}

export const PRIORITY: Record<Priority, { label: string; tone: Tone }> = {
  low: { label: 'Low', tone: 'neutral' }, medium: { label: 'Medium', tone: 'blue' },
  high: { label: 'High', tone: 'amber' }, urgent: { label: 'Urgent', tone: 'red' },
}
export const TASK_STATUS: Record<TaskStatus, { label: string; tone: Tone }> = {
  todo: { label: 'To do', tone: 'neutral' }, in_progress: { label: 'In progress', tone: 'accent' },
  review: { label: 'In review', tone: 'amber' }, done: { label: 'Done', tone: 'sage' },
}
export const PROJECT_STATUS: Record<ProjectStatus, { label: string; tone: Tone }> = {
  planning: { label: 'Planning', tone: 'blue' }, active: { label: 'Active', tone: 'accent' },
  on_hold: { label: 'On hold', tone: 'amber' }, completed: { label: 'Completed', tone: 'sage' },
}
export const PriorityBadge = ({ value }: { value: Priority }) => <Badge tone={PRIORITY[value].tone} dot>{PRIORITY[value].label}</Badge>

export function Segmented<V extends string>({ value, onChange, options, label }: {
  value: V; onChange: (v: V) => void; label: string
  options: { value: V; label: ReactNode; count?: number }[]
}) {
  const id = useId()
  const edition = useEdition()
  return (
    <div className="tabs" role="group" aria-label={label}>
      {options.map((o) => (
        <button key={o.value} type="button" aria-pressed={value === o.value} onClick={() => onChange(o.value)} className={cn('tab', value === o.value && 'tab-active')}>
          {value === o.value && <motion.span layoutId={id} className="tab-pill" transition={edition.spring} />}
          <span className="relative z-10 flex items-center gap-1.5">
            {o.label}
            {o.count !== undefined && <span className="text-[0.7rem] opacity-60">{o.count}</span>}
          </span>
        </button>
      ))}
    </div>
  )
}

export function Switch({ checked, onChange, label, disabled }: { checked: boolean; onChange: (v: boolean) => void; label: string; disabled?: boolean }) {
  const edition = useEdition()
  return (
    <button type="button" role="switch" aria-checked={checked} aria-label={label} disabled={disabled} onClick={() => onChange(!checked)} className={cn('switch', checked && 'switch-on')}>
      <motion.span layout transition={edition.spring} className="switch-knob" />
    </button>
  )
}

export function Progress({ value, className, hue }: { value: number; className?: string; hue?: number }) {
  return (
    <div className={cn('progress', className)} role="progressbar" aria-valuenow={Math.round(value)} aria-valuemin={0} aria-valuemax={100}>
      <motion.div
        className={cn('progress-bar', hue !== undefined && 'progress-hue')}
        style={hue !== undefined ? hueStyle(hue) : undefined}
        initial={{ width: 0 }}
        animate={{ width: `${Math.min(100, value)}%` }}
        transition={{ duration: 0.9, ease: [0.22, 1, 0.36, 1] }}
      />
    </div>
  )
}

export const Skeleton = ({ className }: { className?: string }) => <div className={cn('skeleton', className)} aria-hidden />

export function SkeletonGrid({ count = 6, className = 'grid gap-4 sm:grid-cols-2 xl:grid-cols-3', item = 'h-36' }) {
  return (
    <div className={className} role="status" aria-label="Loading">
      {Array.from({ length: count }, (_, i) => <Skeleton key={i} className={item} />)}
    </div>
  )
}

export function EmptyState({ icon, title, children, action }: { icon: ReactNode; title: string; children?: ReactNode; action?: ReactNode }) {
  return (
    <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col items-center gap-2 px-6 py-12 text-center">
      <span className="empty-icon">{icon}</span>
      <p className="font-medium">{title}</p>
      {children && <p className="max-w-sm text-sm text-muted">{children}</p>}
      {action && <div className="mt-2">{action}</div>}
    </motion.div>
  )
}

export function ErrorState({ error, retry }: { error: Error; retry: () => void }) {
  return (
    <EmptyState icon={<AlertCircle size={20} />} title="Something didn’t load" action={<Button onClick={retry}>Try again</Button>}>
      {error.message}
    </EmptyState>
  )
}

export function FormError({ error }: { error: Error | null }) {
  if (!error) return null
  return (
    <motion.p role="alert" initial={{ opacity: 0, x: 0 }} animate={{ opacity: 1, x: [0, -6, 6, -3, 3, 0] }} className="form-error">
      <AlertCircle size={15} className="shrink-0" /> {error.message}
    </motion.p>
  )
}

export function PageHeader({ eyebrow, title, description, actions }: { eyebrow?: ReactNode; title: ReactNode; description?: ReactNode; actions?: ReactNode }) {
  return (
    <header className="page-header">
      <div className="min-w-0">
        {eyebrow && <div className="eyebrow">{eyebrow}</div>}
        <h1 className="page-title">{title}</h1>
        {description && <p className="page-desc">{description}</p>}
      </div>
      {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
    </header>
  )
}

export function SectionTitle({ title, action }: { title: ReactNode; action?: ReactNode }) {
  return (
    <div className="section-head">
      <h2 className="section-title">{title}</h2>
      {action}
    </div>
  )
}
