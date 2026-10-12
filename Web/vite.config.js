import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test/setup.js',
    // These suites use node:test and run through the separate test:node script.
    exclude: ['**/node_modules/**', '**/dist/**', 'src/services/authSession.test.js', 'src/utils/orderMessages.test.js'],
  },
  server: {
    proxy: {
      '/api': { target: process.env.GEMORA_API_ORIGIN || 'http://127.0.0.1:5198', changeOrigin: true },
      '/uploads': { target: process.env.GEMORA_API_ORIGIN || 'http://127.0.0.1:5198', changeOrigin: true },
    },
  },
})
