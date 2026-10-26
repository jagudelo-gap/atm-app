/// <reference types="vitest/config" />
import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

const api = process.env.API_URL ?? 'http://localhost:5080'

export default defineConfig({
  plugins: [vue()],
  server: { proxy: { '/api': api, '/health': api } },
  test: { environment: 'jsdom' },
})
