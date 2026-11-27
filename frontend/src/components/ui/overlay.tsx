import { createContext, useCallback, useContext, useEffect, useId, useRef, useState, type FormEvent, type ReactNode, type RefObject } from 'react'
import { createPortal } from 'react-dom'
import { AnimatePresence, motion } from 'framer-motion'
import { X, CheckCircle2, AlertCircle } from 'lucide-react'
import { cn } from '../../lib/utils'
import { MOTION } from '../../lib/motion'
import { Button } from './core'

/* Only the topmost layer reacts to Escape and traps focus. */
const stack: symbol[] = []
const FOCUSABLE = 'a[href],button:not([disabled]),input:not([disabled]),select:not([disabled]),textarea:not([disabled]),[tabindex]:not([tabindex="-1"])'

function useLayer(open: boolean, onClose: () => void, ref: RefObject<HTMLElement | null>) {
  const close = useRef(onClose)
  close.current = onClose
  useEffect(() => {
    if (!open) return
    const token = Symbol()
    stack.push(token)
    const previous = document.activeElement as HTMLElement | null
    const frame = requestAnimationFrame(() => {
      const el = ref.current
      if (el && !el.contains(document.activeElement)) (el.querySelector<HTMLElement>('[data-autofocus]') ?? el).focus()
    })
    const onKey = (e: KeyboardEvent) => {
      if (stack.at(-1) !== token) return
      if (e.key === 'Escape') { e.stopPropagation(); close.current() }
      if (e.key === 'Tab' && ref.current) {
        const items = [...ref.current.querySelectorAll<HTMLElement>(FOCUSABLE)]
        const first = items[0], last = items.at(-1)
        if (!first || !last) return
        if (e.shiftKey && (document.activeElement === first || document.activeElement === ref.current)) { e.preventDefault(); last.focus() }
        else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus() }
      }
    }
    document.addEventListener('keydown', onKey)
    document.body.style.overflow = 'hidden'
    return () => {
      cancelAnimationFrame(frame)
      document.removeEventListener('keydown', onKey)
      stack.splice(stack.indexOf(token), 1)
      if (!stack.length) document.body.style.overflow = ''
      previous?.focus?.()
    }
  }, [open, ref])
}

interface PanelProps {
  open: boolean
  onClose: () => void
  title: ReactNode
  description?: ReactNode
  children: ReactNode
  footer?: ReactNode
}

export function Modal({ open, onClose, title, description, children, footer, wide, onSubmit }: PanelProps & { wide?: boolean; onSubmit?: () => void }) {
  const ref = useRef<HTMLFormElement>(null)
  const titleId = useId()
  useLayer(open, onClose, ref)
  const submit = (e: FormEvent) => { e.preventDefault(); onSubmit?.() }
  return createPortal(
    <AnimatePresence>
      {open && (
        <div className="fixed inset-0 z-50 grid place-items-end sm:place-items-center sm:p-4">
          <motion.div className="scrim" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} onClick={onClose} />
          <motion.form
            ref={ref} tabIndex={-1} role="dialog" aria-modal="true" aria-labelledby={titleId} onSubmit={submit} noValidate
            className={cn('modal', wide ? 'sm:max-w-2xl' : 'sm:max-w-md')}
            initial={{ opacity: 0, y: 24, scale: 0.97 }} animate={{ opacity: 1, y: 0, scale: 1 }} exit={{ opacity: 0, y: 16, scale: 0.98, transition: { duration: 0.15 } }}
            transition={MOTION.spring}
          >
            <div className="flex items-start justify-between gap-4 px-5 pt-5">
              <div>
                <h2 id={titleId} className="section-title text-lg">{title}</h2>
                {description && <p className="mt-1 text-sm text-muted">{description}</p>}
              </div>
              <Button variant="ghost" size="sm" icon={<X size={16} />} aria-label="Close" onClick={onClose} />
            </div>
            <div className="flex flex-col gap-4 overflow-y-auto px-5 py-4">{children}</div>
            {footer && <div className="flex flex-wrap items-center justify-end gap-2 border-t border-line px-5 py-3.5">{footer}</div>}
          </motion.form>
        </div>
      )}
    </AnimatePresence>,
    document.body,
  )
}

export function Drawer({ open, onClose, title, children, footer, side = 'right' }: PanelProps & { side?: 'right' | 'left' }) {
  const ref = useRef<HTMLDivElement>(null)
  const titleId = useId()
  useLayer(open, onClose, ref)
  const off = side === 'right' ? '105%' : '-105%'
  return createPortal(
    <AnimatePresence>
      {open && (
        <div className="fixed inset-0 z-40">
          <motion.div className="scrim" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} onClick={onClose} />
          <motion.div
            ref={ref} tabIndex={-1} role="dialog" aria-modal="true" aria-labelledby={titleId}
            className={cn('drawer', side === 'right' ? 'drawer-right' : 'drawer-left')}
            initial={{ x: off }} animate={{ x: 0 }} exit={{ x: off }} transition={MOTION.spring}
          >
            <div className="flex items-center justify-between gap-3 border-b border-line px-5 py-3.5">
              <div id={titleId} className="section-title min-w-0 truncate">{title}</div>
              <Button variant="ghost" size="sm" icon={<X size={16} />} aria-label="Close panel" onClick={onClose} />
            </div>
            <div className="flex-1 overflow-y-auto px-5 py-5">{children}</div>
            {footer && <div className="flex items-center gap-2 border-t border-line px-5 py-3.5">{footer}</div>}
          </motion.div>
        </div>
      )}
    </AnimatePresence>,
    document.body,
  )
}

export function Popover({ trigger, children, align = 'left', placement = 'bottom', className, wrapClass }: {
  trigger: (p: { open: boolean; toggle: () => void }) => ReactNode
  children: (close: () => void) => ReactNode
  align?: 'left' | 'right'; placement?: 'bottom' | 'top' | 'side'; className?: string; wrapClass?: string
}) {
  const [open, setOpen] = useState(false)
  const root = useRef<HTMLDivElement>(null)
  useEffect(() => {
    if (!open) return
    const down = (e: PointerEvent) => { if (!root.current?.contains(e.target as Node)) setOpen(false) }
    const key = (e: KeyboardEvent) => { if (e.key === 'Escape' && !stack.length) setOpen(false) }
    document.addEventListener('pointerdown', down)
    document.addEventListener('keydown', key)
    return () => { document.removeEventListener('pointerdown', down); document.removeEventListener('keydown', key) }
  }, [open])
  const position = placement === 'side'
    ? 'left-full bottom-0 ml-3'
    : cn(placement === 'top' ? 'bottom-full mb-2' : 'top-full mt-2', align === 'right' ? 'right-0' : 'left-0')
  return (
    <div ref={root} className={cn('relative', wrapClass)}>
      {trigger({ open, toggle: () => setOpen((o) => !o) })}
      <AnimatePresence>
        {open && (
          <motion.div
            className={cn('popover', position, className)}
            initial={{ opacity: 0, y: placement === 'top' ? 6 : -6, scale: 0.97 }} animate={{ opacity: 1, y: 0, scale: 1 }} exit={{ opacity: 0, scale: 0.97, transition: { duration: 0.1 } }}
            transition={{ duration: 0.18, ease: 'easeOut' }}
          >
            {children(() => setOpen(false))}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}

/* ---------- toasts & confirm ---------- */
type ToastKind = 'success' | 'error'
interface ConfirmOptions { title: string; message: ReactNode; confirmLabel: string; danger?: boolean }
const ToastContext = createContext<(message: string, kind?: ToastKind) => void>(() => {})
const ConfirmContext = createContext<(o: ConfirmOptions) => Promise<boolean>>(async () => false)
export const useToast = () => useContext(ToastContext)
export const useConfirm = () => useContext(ConfirmContext)

export function OverlayProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<{ id: number; message: string; kind: ToastKind }[]>([])
  const [ask, setAsk] = useState<(ConfirmOptions & { resolve: (v: boolean) => void }) | null>(null)
  const last = useRef<ConfirmOptions | null>(null)
  if (ask) last.current = ask
  const toast = useCallback((message: string, kind: ToastKind = 'success') => {
    const id = Date.now() + Math.random()
    setToasts((t) => [...t.slice(-2), { id, message, kind }])
    setTimeout(() => setToasts((t) => t.filter((x) => x.id !== id)), 3600)
  }, [])
  const confirm = useCallback((o: ConfirmOptions) => new Promise<boolean>((resolve) => setAsk({ ...o, resolve })), [])
  const answer = (v: boolean) => { ask?.resolve(v); setAsk(null) }
  const shown = ask ?? last.current
  return (
    <ToastContext.Provider value={toast}>
      <ConfirmContext.Provider value={confirm}>
        {children}
        <Modal
          open={!!ask} onClose={() => answer(false)} title={shown?.title ?? ''}
          footer={<>
            <Button variant="ghost" onClick={() => answer(false)}>Cancel</Button>
            <Button variant={shown?.danger ? 'danger' : 'primary'} data-autofocus onClick={() => answer(true)}>{shown?.confirmLabel}</Button>
          </>}
        >
          <p className="text-sm text-muted">{shown?.message}</p>
        </Modal>
        {createPortal(
          <div className="pointer-events-none fixed inset-x-4 bottom-20 z-[60] flex flex-col items-center gap-2 sm:inset-x-auto sm:right-5 sm:items-end" aria-live="polite">
            <AnimatePresence>
              {toasts.map((t) => (
                <motion.div key={t.id} layout className="toast" initial={{ opacity: 0, y: 16, scale: 0.95 }} animate={{ opacity: 1, y: 0, scale: 1 }} exit={{ opacity: 0, x: 30 }}>
                  {t.kind === 'success' ? <CheckCircle2 size={16} className="text-[#a9c4a4]" /> : <AlertCircle size={16} className="text-[#f0a48c]" />}
                  {t.message}
                </motion.div>
              ))}
            </AnimatePresence>
          </div>,
          document.body,
        )}
      </ConfirmContext.Provider>
    </ToastContext.Provider>
  )
}
