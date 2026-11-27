/** The three application frames. Same destinations, three different ways to get around. */
import { useEffect, useState, type ReactNode } from 'react'
import { NavLink, useLocation } from 'react-router'
import { motion } from 'framer-motion'
import { Menu, MoreHorizontal, PanelLeftClose, PanelLeftOpen } from 'lucide-react'
import { useEdition, type EditionId } from '../../editions'
import { cn, useMediaQuery } from '../../lib/utils'
import { Button } from '../ui/core'
import { Drawer } from '../ui/overlay'
import { Brand, EditionSwitcher, isActive, NotificationBell, useNavItems, useUnreadChat, UserMenu, WorkspaceSwitcher } from './shared'

interface ShellProps { children: ReactNode; signOut: () => void }

function useNavDrawer() {
  const [open, setOpen] = useState(false)
  const { pathname } = useLocation()
  useEffect(() => setOpen(false), [pathname])
  return { open, setOpen }
}

function SideLinks({ collapsed, group }: { collapsed?: boolean; group: string }) {
  const { main, secondary } = useNavItems()
  const { pathname } = useLocation()
  const edition = useEdition()
  const unread = useUnreadChat()
  const link = ({ to, label, icon: Icon }: (typeof main)[number]) => {
    const active = isActive(pathname, to)
    return (
      <NavLink key={to} to={to} end={to === '/'} className={cn('side-link', active && 'side-on', collapsed && 'justify-center')} aria-label={collapsed ? label : undefined} title={collapsed ? label : undefined}>
        {active && <motion.span layoutId={`side-${group}`} className="side-pill" transition={edition.spring} />}
        <Icon size={18} className="relative shrink-0" />
        {!collapsed && <span className="relative flex-1">{label}</span>}
        {to === '/chat' && unread > 0 && (collapsed ? <span className="absolute right-2 top-1.5 size-2 rounded-full bg-accent" /> : <span className="count-badge relative">{unread}</span>)}
      </NavLink>
    )
  }
  return (
    <nav aria-label="Main" className="flex flex-col gap-0.5">
      {main.map(link)}
      <div className="menu-sep !my-3" />
      {secondary.map(link)}
    </nav>
  )
}

function AtelierShell({ children, signOut }: ShellProps) {
  const [preference, setPreference] = useState(() => localStorage.getItem('ws.sidebar') === 'collapsed')
  const small = useMediaQuery('(max-width: 1100px)')
  const collapsed = small || preference
  const nav = useNavDrawer()
  const edition = useEdition()
  const toggle = () => { localStorage.setItem('ws.sidebar', preference ? 'open' : 'collapsed'); setPreference(!preference) }
  return (
    <div className="flex min-h-dvh">
      <motion.aside className="sidebar max-md:!hidden" initial={false} animate={{ width: collapsed ? 76 : 252 }} transition={edition.spring}>
        <div className={cn('mb-4 flex items-center', collapsed ? 'justify-center' : 'justify-between pl-1')}>
          <Brand compact={collapsed} />
          {!collapsed && <Button variant="ghost" size="sm" icon={<PanelLeftClose size={16} />} aria-label="Collapse sidebar" onClick={toggle} />}
        </div>
        {collapsed && !small && <Button variant="ghost" size="sm" className="mx-auto mb-2" icon={<PanelLeftOpen size={16} />} aria-label="Expand sidebar" onClick={toggle} />}
        <div className="mb-4"><WorkspaceSwitcher compact={collapsed} placement={collapsed ? 'side' : 'bottom'} /></div>
        <SideLinks collapsed={collapsed} group="atelier" />
        <div className={cn('mt-auto flex border-t border-line pt-3', collapsed && 'justify-center')}>
          <UserMenu signOut={signOut} placement={collapsed ? 'side' : 'top'} align="left" detailed={!collapsed} />
        </div>
      </motion.aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="topbar">
          <Button variant="ghost" className="md:!hidden" icon={<Menu size={19} />} aria-label="Open navigation" onClick={() => nav.setOpen(true)} />
          <span className="md:hidden"><Brand compact /></span>
          <p className="text-sm text-muted max-md:hidden">{new Date().toLocaleDateString('en-GB', { weekday: 'long', day: 'numeric', month: 'long' })}</p>
          <div className="ml-auto flex items-center gap-2"><EditionSwitcher /><NotificationBell /><span className="md:hidden"><UserMenu signOut={signOut} /></span></div>
        </header>
        <main className="app-main">{children}</main>
      </div>
      <Drawer side="left" open={nav.open} onClose={() => nav.setOpen(false)} title={<Brand />}>
        <div className="mb-4"><WorkspaceSwitcher /></div><SideLinks group="atelier-mobile" />
      </Drawer>
    </div>
  )
}

function AlmanacShell({ children, signOut }: ShellProps) {
  const { main, secondary } = useNavItems()
  const { pathname } = useLocation()
  const edition = useEdition()
  const unread = useUnreadChat()
  const nav = useNavDrawer()
  return (
    <div className="flex min-h-dvh flex-col">
      <header className="masthead">
        <div className="mx-auto flex h-16 w-full max-w-7xl items-center gap-3 px-4 sm:px-6">
          <Button variant="ghost" className="lg:!hidden" icon={<Menu size={19} />} aria-label="Open navigation" onClick={() => nav.setOpen(true)} />
          <span className="max-sm:hidden"><Brand /></span>
          <span className="mx-1 h-6 w-px bg-line max-sm:hidden" />
          <WorkspaceSwitcher />
          <div className="ml-auto flex items-center gap-2"><EditionSwitcher /><NotificationBell /><UserMenu signOut={signOut} /></div>
        </div>
        <nav aria-label="Main" className="mx-auto hidden w-full max-w-7xl items-center border-t border-line px-6 lg:flex">
          {[...main, ...secondary].map(({ to, label }, i) => {
            const active = isActive(pathname, to)
            return (
              <NavLink key={to} to={to} end={to === '/'} className={cn('top-link', active && 'top-on', i === main.length && 'ml-auto')}>
                {label}{to === '/chat' && unread > 0 && <sup className="ml-0.5 text-accent">{unread}</sup>}
                {active && <motion.span layoutId="top-rule" className="top-rule" transition={edition.spring} />}
              </NavLink>
            )
          })}
        </nav>
      </header>
      <main className="app-main mx-auto w-full max-w-7xl">{children}</main>
      <Drawer side="left" open={nav.open} onClose={() => nav.setOpen(false)} title={<Brand />}><SideLinks group="almanac-mobile" /></Drawer>
    </div>
  )
}

function StudioShell({ children, signOut }: ShellProps) {
  const { main, secondary } = useNavItems()
  const { pathname } = useLocation()
  const edition = useEdition()
  const unread = useUnreadChat()
  const nav = useNavDrawer()
  const dockLink = ({ to, label, icon: Icon }: (typeof main)[number], group: string) => {
    const active = isActive(pathname, to)
    return (
      <motion.div key={to} whileHover={{ scale: 1.12 }} whileTap={{ scale: 0.9 }} transition={edition.spring}>
        <NavLink to={to} end={to === '/'} aria-label={label} className={cn('dock-link', active && 'dock-on')}>
          {active && <motion.span layoutId={group} className="dock-pill" transition={edition.spring} />}
          <Icon size={19} className="relative" />
          {to === '/chat' && unread > 0 && <span className="absolute right-1.5 top-1.5 size-2 rounded-full bg-[#f0b878] ring-2 ring-[#1f1d1a]" />}
          <span className="dock-tip">{label}</span>
        </NavLink>
      </motion.div>
    )
  }
  return (
    <div className="min-h-dvh md:pl-[5.75rem]">
      <aside className="dock max-md:!hidden">
        <Brand compact />
        <nav aria-label="Main" className="flex flex-col gap-1.5">
          {main.map((n) => dockLink(n, 'dock'))}
          <span className="mx-auto my-1 h-px w-6 bg-white/15" />
          {secondary.map((n) => dockLink(n, 'dock'))}
        </nav>
        <UserMenu signOut={signOut} placement="side" />
      </aside>
      <header className="studio-bar">
        <span className="md:hidden"><Brand compact /></span>
        <div className="floating-chip min-w-0"><WorkspaceSwitcher /></div>
        <div className="floating-chip ml-auto flex items-center gap-1 !p-1.5"><EditionSwitcher /><NotificationBell /><span className="md:hidden"><UserMenu signOut={signOut} /></span></div>
      </header>
      <main className="app-main mx-auto w-full max-w-6xl max-md:!pb-28">{children}</main>
      <nav aria-label="Main" className="bottom-dock md:!hidden">
        {main.slice(0, 4).map((n) => dockLink(n, 'bottom'))}
        <button type="button" className="dock-link" aria-label="More pages" onClick={() => nav.setOpen(true)}><MoreHorizontal size={20} /></button>
      </nav>
      <Drawer side="left" open={nav.open} onClose={() => nav.setOpen(false)} title={<Brand />}><SideLinks group="studio-mobile" /></Drawer>
    </div>
  )
}

export const SHELLS: Record<EditionId, (p: ShellProps) => ReactNode> = { atelier: AtelierShell, almanac: AlmanacShell, studio: StudioShell }
