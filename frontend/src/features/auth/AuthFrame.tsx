import type { ReactNode } from 'react'
import { motion } from 'framer-motion'
import { CalendarDays, CheckCircle2, FolderKanban, MessagesSquare } from 'lucide-react'
import { useEdition } from '../../editions'
import { Brand, EditionSwitcher } from '../../components/layout/shared'

/** Each edition frames the same auth forms differently: split-screen, broadsheet, or floating card. */
export function AuthFrame({ children }: { children: ReactNode }) {
  const edition = useEdition()
  const content = (
    <motion.div initial="hidden" animate="show" variants={edition.page} className="w-full max-w-sm">{children}</motion.div>
  )

  if (edition.id === 'atelier') return (
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
        <EditionSwitcher inline />
      </main>
    </div>
  )

  if (edition.id === 'almanac') return (
    <div className="flex min-h-dvh flex-col px-5">
      <header className="mx-auto w-full max-w-5xl border-b-[3px] border-double border-ink py-5 text-center">
        <p className="display text-4xl italic sm:text-6xl">The Almanac</p>
        <p className="eyebrow mt-2">{new Date().toLocaleDateString('en-GB', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}</p>
      </header>
      <main className="mx-auto grid w-full max-w-5xl flex-1 items-center gap-10 py-10 md:grid-cols-[1fr_auto_24rem]">
        <ul className="hidden divide-y divide-line border-y border-line md:block">
          {[
            { icon: FolderKanban, head: 'Projects', text: 'Boards and lists that stay out of your way.' },
            { icon: MessagesSquare, head: 'Correspondence', text: 'Channels, group rooms and direct notes.' },
            { icon: CalendarDays, head: 'The diary', text: 'Every date your team is working toward.' },
          ].map((f, i) => (
            <motion.li key={f.head} className="flex gap-4 py-5" initial={{ opacity: 0, x: -14 }} animate={{ opacity: 1, x: 0 }} transition={{ delay: 0.1 + i * 0.08 }}>
              <span className="display text-2xl italic text-accent">{['i', 'ii', 'iii'][i]}.</span>
              <span><span className="display block text-xl">{f.head}</span><span className="text-sm text-muted">{f.text}</span></span>
            </motion.li>
          ))}
        </ul>
        <span className="hidden h-full w-px bg-line md:block" />
        <div className="flex justify-center">{content}</div>
      </main>
      <footer className="mx-auto flex w-full max-w-5xl justify-center border-t border-line py-4"><EditionSwitcher inline /></footer>
    </div>
  )

  return (
    <div className="relative flex min-h-dvh flex-col items-center justify-center gap-6 overflow-hidden bg-sand px-4 py-10">
      {['-left-24 -top-24 size-96 bg-[#f0cfa5]', '-bottom-32 -right-20 size-[28rem] bg-[#d9cdb9]', 'right-[18%] top-[12%] size-40 bg-[#fbe6cb]'].map((pos, i) => (
        <motion.span key={pos} aria-hidden className={`absolute rounded-full opacity-70 blur-2xl ${pos}`}
          animate={{ x: [0, 24, 0], y: [0, -18, 0] }} transition={{ duration: 12 + i * 3, repeat: Infinity, ease: 'easeInOut' }} />
      ))}
      <div className="relative"><Brand /></div>
      <main className="card relative flex w-full max-w-md justify-center p-6 sm:p-9">{content}</main>
      <div className="relative"><EditionSwitcher inline /></div>
    </div>
  )
}
