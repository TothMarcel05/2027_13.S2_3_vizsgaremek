import { motion, MotionConfig } from 'framer-motion'
import { ArrowRight } from 'lucide-react'
import { EDITIONS, type Edition } from './editions'

/** Tiny wireframe of each edition's layout. */
function Preview({ edition }: { edition: Edition }) {
  const [, soft, sand, accent, ink] = edition.swatches
  const bar = (className: string, color = sand) => <span className={`block rounded-sm ${className}`} style={{ background: color }} />
  if (edition.id === 'atelier') return (
    <div className="flex h-40 gap-2 rounded-xl border border-[#eae4dc] bg-white p-2">
      <div className="flex w-12 flex-col gap-1.5 rounded-lg p-1.5" style={{ background: sand }}>{bar('h-3', accent)}{bar('h-2', '#fff')}{bar('h-2', '#ddd4c7')}{bar('h-2', '#ddd4c7')}</div>
      <div className="grid flex-1 grid-cols-3 grid-rows-[1rem_1fr_1fr] gap-1.5">{bar('col-span-2', ink)}{bar('', accent)}{bar('')}{bar('')}{bar('')}{bar('col-span-2', soft)}{bar('')}</div>
    </div>
  )
  if (edition.id === 'almanac') return (
    <div className="flex h-40 flex-col gap-2 border border-[#eae4dc] bg-white p-2.5">
      <div className="flex justify-between">{bar('h-2 w-14', ink)}{bar('h-2 w-6', accent)}</div>
      <div className="border-y-[3px] border-double py-2" style={{ borderColor: ink }}>{bar('mx-auto h-3 w-28', ink)}</div>
      <div className="grid flex-1 grid-cols-3 divide-x divide-[#e6dfd5]">{[0, 1, 2].map((c) => <div key={c} className="space-y-1.5 px-2">{bar('h-2 w-10', accent)}{bar('h-1.5')}{bar('h-1.5')}{bar('h-1.5 w-2/3')}{bar('h-1.5')}</div>)}</div>
    </div>
  )
  return (
    <div className="flex h-40 gap-2 rounded-3xl border border-[#eae4dc] p-2" style={{ background: sand }}>
      <div className="my-2 flex w-6 flex-col items-center gap-1.5 rounded-full py-2" style={{ background: ink }}>{[accent, '#6b665e', '#6b665e', '#6b665e'].map((c, i) => <span key={i} className="size-3 rounded-full" style={{ background: c }} />)}</div>
      <div className="flex flex-1 flex-col gap-2"><div className="h-14 rounded-2xl p-2" style={{ background: ink }}>{bar('h-2.5 w-20 !rounded-full', accent)}</div><div className="flex gap-2">{[0, 1, 2].map((i) => <span key={i} className="h-9 flex-1 rounded-2xl bg-white" />)}</div><div className="flex-1 rounded-2xl bg-white" /></div>
    </div>
  )
}

export function Chooser() {
  return (
    <MotionConfig reducedMotion="user">
      <main className="mx-auto flex min-h-dvh max-w-6xl flex-col justify-center px-5 py-12">
        <p className="eyebrow">Workspace &amp; project management</p>
        <h1 className="display mt-2 text-4xl sm:text-5xl">One workspace, three editions</h1>
        <p className="mt-3 max-w-2xl text-muted">All three have the same features and share the same demo data. They differ in layout, navigation, typography, accent colour and motion. You can switch between them from inside the app and stay on the same page.</p>
        <motion.ul className="mt-9 grid gap-5 lg:grid-cols-3" initial="hidden" animate="show" variants={{ show: { transition: { staggerChildren: 0.09 } } }}>
          {Object.values(EDITIONS).map((e) => (
            <motion.li key={e.id} variants={{ hidden: { opacity: 0, y: 24 }, show: { opacity: 1, y: 0 } }} whileHover={{ y: -6 }} transition={{ type: 'spring', stiffness: 260, damping: 22 }}>
              <a href={`/${e.id}`} className="chooser-card group">
                <Preview edition={e} />
                <h2 className="display mt-4 text-2xl">{e.name}</h2>
                <p className="mt-0.5 text-sm font-medium">{e.tagline}</p>
                <p className="mt-2 flex-1 text-sm text-muted">{e.blurb}</p>
                <div className="mt-4 flex items-center justify-between">
                  <span className="flex gap-1.5" aria-hidden>{e.swatches.map((c) => <span key={c} className="size-4 rounded-full border border-[#d8cfc1]" style={{ background: c }} />)}</span>
                  <span className="flex items-center gap-1.5 text-sm font-semibold">Open <ArrowRight size={15} className="transition-transform group-hover:translate-x-1" /></span>
                </div>
              </a>
            </motion.li>
          ))}
        </motion.ul>
      </main>
    </MotionConfig>
  )
}
