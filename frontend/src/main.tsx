import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MotionConfig } from 'framer-motion'
import '@fontsource-variable/inter'
import '@fontsource-variable/fraunces'
import '@fontsource-variable/fraunces/wght-italic.css'
import './styles/base.css'
import { App } from './App'
import { OverlayProvider } from './components/ui/overlay'
import { sessionStore } from './api/client'

const client = new QueryClient({ defaultOptions: { queries: { staleTime: 15_000, retry: 1, refetchOnWindowFocus: false } } })
// Subscribed before React renders, so one account's cached data is never shown to the next.
let accountId = sessionStore.get()?.user.id
sessionStore.subscribe(() => {
  const next = sessionStore.get()?.user.id
  if (next === accountId) return
  accountId = next
  client.clear()
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={client}>
      <MotionConfig reducedMotion="user">
        <BrowserRouter>
          <OverlayProvider><App /></OverlayProvider>
        </BrowserRouter>
      </MotionConfig>
    </QueryClientProvider>
  </StrictMode>,
)
