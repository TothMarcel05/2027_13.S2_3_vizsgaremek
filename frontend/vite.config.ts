import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  // Forwards API calls to the .NET backend (the `http` launch profile) when VITE_USE_MOCK=false.
  server: { port: 5173, proxy: { '/api': 'http://localhost:5243' } },
})
