import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': { target: process.env.GEMORA_API_ORIGIN || 'http://127.0.0.1:5198', changeOrigin: true },
      '/uploads': { target: process.env.GEMORA_API_ORIGIN || 'http://127.0.0.1:5198', changeOrigin: true },
    },
  },
})
