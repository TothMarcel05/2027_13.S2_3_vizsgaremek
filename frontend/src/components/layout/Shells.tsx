/** The application frame: collapsible sidebar and top bar, with a drawer on small screens. */
import { useEffect, useState, type ReactNode } from 'react'
import { NavLink, useLocation } from 'react-router'
import { motion } from 'framer-motion'
import { Menu, PanelLeftClose, PanelLeftOpen } from 'lucide-react'
import { MOTION } from '../../lib/motion'
import { cn, useMediaQuery } from '../../lib/utils'
import { Button } from '../ui/core'
import { Drawer } from '../ui/overlay'
import { Brand, isActive, NotificationBell, useNavItems, useUnreadChat, UserMenu, WorkspaceSwitcher } from './shared'

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
  const unread = useUnreadChat()
  const link = ({ to, label, icon: Icon }: (typeof main)[number]) => {
    const active = isActive(pathname, to)
    return (
      <NavLink key={to} to={to} end={to === '/'} className={cn('side-link', active && 'side-on', collapsed && 'justify-center')} aria-label={collapsed ? label : undefined} title={collapsed ? label : undefined}>
        {active && <motion.span layoutId={`side-${group}`} className="side-pill" transition={MOTION.spring} />}
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

export function Shell({ children, signOut }: ShellProps) {
  const [preference, setPreference] = useState(() => localStorage.getItem('ws.sidebar') === 'collapsed')
  const small = useMediaQuery('(max-width: 1100px)')
  const collapsed = small || preference
  const nav = useNavDrawer()
  const toggle = () => { localStorage.setItem('ws.sidebar', preference ? 'open' : 'collapsed'); setPreference(!preference) }
  return (
    <div className="flex min-h-dvh">
      <motion.aside className="sidebar max-md:!hidden" initial={false} animate={{ width: collapsed ? 76 : 252 }} transition={MOTION.spring}>
        <div className={cn('mb-4 flex items-center', collapsed ? 'justify-center' : 'justify-between pl-1')}>
          <Brand compact={collapsed} />
          {!collapsed && <Button variant="ghost" size="sm" icon={<PanelLeftClose size={16} />} aria-label="Collapse sidebar" onClick={toggle} />}
        </div>
        {collapsed && !small && <Button variant="ghost" size="sm" className="mx-auto mb-2" icon={<PanelLeftOpen size={16} />} aria-label="Expand sidebar" onClick={toggle} />}
        <div className="mb-4"><WorkspaceSwitcher compact={collapsed} placement={collapsed ? 'side-start' : 'bottom'} /></div>
        <SideLinks collapsed={collapsed} group="desktop" />
        <div className={cn('mt-auto flex border-t border-line pt-3', collapsed && 'justify-center')}>
          <UserMenu signOut={signOut} placement={collapsed ? 'side' : 'top'} align="left" detailed={!collapsed} />
        </div>
      </motion.aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="topbar">
          <Button variant="ghost" className="md:!hidden" icon={<Menu size={19} />} aria-label="Open navigation" onClick={() => nav.setOpen(true)} />
          <span className="md:hidden"><Brand compact /></span>
          <p className="text-sm text-muted max-md:hidden">{new Date().toLocaleDateString('en-GB', { weekday: 'long', day: 'numeric', month: 'long' })}</p>
          <div className="ml-auto flex items-center gap-2"><NotificationBell /><span className="md:hidden"><UserMenu signOut={signOut} /></span></div>
        </header>
        <main className="app-main">{children}</main>
      </div>
      <Drawer side="left" open={nav.open} onClose={() => nav.setOpen(false)} title={<Brand />}>
        <div className="mb-4"><WorkspaceSwitcher /></div><SideLinks group="mobile" />
      </Drawer>
    </div>
  )
}
