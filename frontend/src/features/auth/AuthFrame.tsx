import type { ReactNode } from 'react'
import { motion } from 'framer-motion'
import { CalendarDays, CheckCircle2, MessagesSquare } from 'lucide-react'
import { MOTION } from '../../lib/motion'
import { Brand } from '../../components/layout/shared'

/** Split-screen frame shared by the auth screens. */
export function AuthFrame({ children }: { children: ReactNode }) {
  const content = (
    <motion.div initial="hidden" animate="show" variants={MOTION.page} className="w-full max-w-sm">{children}</motion.div>
  )

  return (
    <div className="grid min-h-dvh lg:grid-cols-[1.05fr_1fr]">
      <aside className="relative hidden flex-col justify-between overflow-hidden border-r border-line bg-sand p-12 lg:flex">
        <Brand />
        <div className="relative h-72">
          {[
            { icon: CheckCircle2, text: 'Design the editorial grid', meta: 'Reserved by you', pos: 'left-0 top-4 w-72 -rotate-2' },
            { icon: MessagesSquare, text: '“Love option B. Let’s test it.”', meta: '#website-redesign', pos: 'left-24 top-28 w-72 rotate-1' },
            { icon: CalendarDays, text: 'Design review at 14:00', meta: 'Studio room', pos: 'left-8 top-52 w-64 -rotate-1' },
          ].map((c, i) => (
            <motion.div key={c.text} className={`card absolute flex items-center gap-3 p-4 ${c.pos}`}
              initial={{ opacity: 0, y: 30 }} animate={{ opacity: 1, y: [0, -6, 0] }}
              transition={{ opacity: { delay: 0.2 + i * 0.15 }, y: { duration: 6, repeat: Infinity, delay: i * 0.9, ease: 'easeInOut' } }}>
              <c.icon size={18} className="shrink-0 text-accent" />
              <span><span className="block text-sm font-medium">{c.text}</span><span className="text-xs text-muted">{c.meta}</span></span>
            </motion.div>
          ))}
        </div>
        <div>
          <p className="display max-w-md text-4xl leading-tight">A quieter place for your team’s best work.</p>
          <p className="mt-3 max-w-sm text-sm text-muted">Projects, conversations, files and plans in one calm workspace.</p>
        </div>
      </aside>
      <main className="flex flex-col items-center justify-center gap-8 px-5 py-10">
        <div className="lg:hidden"><Brand /></div>
        {content}
      </main>
    </div>
  )
}
