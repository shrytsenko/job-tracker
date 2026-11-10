import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // The browser only ever talks to the Vite dev server; /api calls are forwarded to the
    // ASP.NET Core API. Same origin from the browser's point of view, so no CORS setup needed.
    // API_URL lets a second dev setup point elsewhere (e.g. API_URL=http://localhost:5199 npm run dev).
    proxy: {
      '/api': process.env.API_URL ?? 'http://localhost:5122',
    },
  },
})
