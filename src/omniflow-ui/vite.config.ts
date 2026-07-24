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
  build: {
    // The only chunk over the default 500 kB warning is vendor-antd (~1.2 MB min /
    // ~384 kB gzip) — the Ant Design core, which can't be split further and is now
    // an isolated, long-cached vendor chunk. Raise the limit so the build stays
    // clean but still flags any *new* oversized chunk.
    chunkSizeWarningLimit: 1300,
    rollupOptions: {
      output: {
        // Rolldown (Vite 8) manual chunking: pull the heavy vendors into their
        // own long-cached chunks so the initial download shrinks and an app-code
        // change doesn't bust them. Order = priority; the specific groups win
        // over the generic react one.
        advancedChunks: {
          groups: [
            { name: 'vendor-icons', test: /node_modules[\\/]@ant-design[\\/]icons/ },
            { name: 'vendor-antd', test: /node_modules[\\/](antd|@ant-design|rc-[a-z]|@rc-component)[\\/]/ },
            { name: 'vendor-flow', test: /node_modules[\\/]@xyflow[\\/]/ },
            { name: 'vendor-signalr', test: /node_modules[\\/]@microsoft[\\/]signalr[\\/]/ },
            { name: 'vendor-react', test: /node_modules[\\/](react|react-dom|react-router|react-router-dom|scheduler)[\\/]/ },
          ],
        },
      },
    },
  },
})
