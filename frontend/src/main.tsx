import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MotionConfig } from 'framer-motion'
import '@fontsource-variable/inter'
import '@fontsource-variable/fraunces'
import '@fontsource-variable/fraunces/wght-italic.css'
import './styles/base.css'
import './styles/editions.css'
import { App } from './App'
import { Chooser } from './Chooser'
import { EDITIONS, EditionContext, type EditionId } from './editions'
import { OverlayProvider } from './components/ui/overlay'

const client = new QueryClient({ defaultOptions: { queries: { staleTime: 15_000, retry: 1, refetchOnWindowFocus: false } } })
const segment = window.location.pathname.split('/')[1] ?? ''
const edition = Object.hasOwn(EDITIONS, segment) ? EDITIONS[segment as EditionId] : null

// The edition lives on <html> so portals (modals, drawers, toasts) are themed too.
document.documentElement.dataset.edition = edition?.id ?? 'atelier'
if (edition) document.title = `${edition.name} — Workspace`

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {edition ? (
      <QueryClientProvider client={client}>
        <EditionContext.Provider value={edition}>
          <MotionConfig reducedMotion="user">
            <BrowserRouter basename={`/${edition.id}`}>
              <OverlayProvider><App /></OverlayProvider>
            </BrowserRouter>
          </MotionConfig>
        </EditionContext.Provider>
      </QueryClientProvider>
    ) : <Chooser />}
  </StrictMode>,
)
