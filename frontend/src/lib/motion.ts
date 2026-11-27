/** The app's shared motion settings. */
import type { Transition, Variants } from 'framer-motion'

interface MotionSettings {
  spring: Transition
  item: Variants
  page: Variants
  /** Delay between list items, in seconds. */
  stagger: number
  /** How far primary buttons lean toward the pointer. */
  magnet: number
  /** Button scale while pressed. */
  press: number
}

export const MOTION: MotionSettings = {
  spring: { type: 'spring', stiffness: 380, damping: 32 },
  item: { hidden: { opacity: 0, y: 14 }, show: { opacity: 1, y: 0, transition: { duration: 0.45, ease: [0.22, 1, 0.36, 1] } } },
  page: { hidden: { opacity: 0, y: 10 }, show: { opacity: 1, y: 0, transition: { duration: 0.35 } }, exit: { opacity: 0, y: -6, transition: { duration: 0.16 } } },
  stagger: 0.06,
  magnet: 0.18,
  press: 0.97,
}
