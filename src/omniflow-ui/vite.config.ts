import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Dev proxy: the SPA talks to same-origin relative paths (/api, /connect, /hubs)
// and Vite forwards them to the API. This removes every hardcoded localhost:5226
// from the source and lets a prod build point elsewhere via VITE_API_URL.
const API_TARGET = process.env.VITE_API_TARGET ?? 'http://localhost:5226'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': { target: API_TARGET, changeOrigin: true },
      '/connect': { target: API_TARGET, changeOrigin: true },
      '/hubs': { target: API_TARGET, changeOrigin: true, ws: true },
    },
  },
})
