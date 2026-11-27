import { createContext, useContext } from 'react'
import type { Transition, Variants } from 'framer-motion'

export type EditionId = 'atelier' | 'almanac' | 'studio'

export interface Edition {
  id: EditionId
  name: string
  tagline: string
  blurb: string
  swatches: string[]
  /** Motion personality: each edition moves differently, not just looks different. */
  spring: Transition
  item: Variants
  page: Variants
  stagger: number
  magnet: number
  press: number
}

export const EDITIONS: Record<EditionId, Edition> = {
  atelier: {
    id: 'atelier', name: 'Atelier', tagline: 'The classic editorial workspace',
    blurb: 'Labelled sand sidebar, soft cards, serif headlines and a terracotta accent. Calm and familiar.',
    swatches: ['#FFFFFF', '#FAF8F5', '#F3EFEA', '#C87D55', '#1F1D1A'],
    spring: { type: 'spring', stiffness: 380, damping: 32 },
    item: { hidden: { opacity: 0, y: 14 }, show: { opacity: 1, y: 0, transition: { duration: 0.45, ease: [0.22, 1, 0.36, 1] } } },
    page: { hidden: { opacity: 0, y: 10 }, show: { opacity: 1, y: 0, transition: { duration: 0.35 } }, exit: { opacity: 0, y: -6, transition: { duration: 0.16 } } },
    stagger: 0.06, magnet: 0.18, press: 0.97,
  },
  almanac: {
    id: 'almanac', name: 'Almanac', tagline: 'A workspace set like a broadsheet',
    blurb: 'Top navigation, hairline rules in place of boxes, a large serif masthead and sage green. Dense and quiet.',
    swatches: ['#FFFFFF', '#FAF8F5', '#E6DFD5', '#5F7A61', '#1F1D1A'],
    spring: { type: 'spring', stiffness: 520, damping: 46 },
    item: { hidden: { opacity: 0, x: -10 }, show: { opacity: 1, x: 0, transition: { duration: 0.38, ease: 'easeOut' } } },
    page: { hidden: { opacity: 0, x: 16 }, show: { opacity: 1, x: 0, transition: { duration: 0.3, ease: 'easeOut' } }, exit: { opacity: 0, x: -12, transition: { duration: 0.14 } } },
    stagger: 0.045, magnet: 0, press: 0.985,
  },
  studio: {
    id: 'studio', name: 'Studio', tagline: 'Soft, tactile and floating',
    blurb: 'A floating icon dock, pill controls, layered sand surfaces, amber accent and springy motion.',
    swatches: ['#FFFFFF', '#F3EFEA', '#E6DFD5', '#B8691F', '#1F1D1A'],
    spring: { type: 'spring', stiffness: 300, damping: 22 },
    item: { hidden: { opacity: 0, y: 22, scale: 0.96 }, show: { opacity: 1, y: 0, scale: 1, transition: { type: 'spring', stiffness: 260, damping: 22 } } },
    page: { hidden: { opacity: 0, scale: 0.985, y: 8 }, show: { opacity: 1, scale: 1, y: 0, transition: { type: 'spring', stiffness: 280, damping: 26 } }, exit: { opacity: 0, scale: 0.99, transition: { duration: 0.14 } } },
    stagger: 0.075, magnet: 0.3, press: 0.93,
  },
}

export const EditionContext = createContext<Edition>(EDITIONS.atelier)
export const useEdition = () => useContext(EditionContext)
