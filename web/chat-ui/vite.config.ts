import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  server: {
    host: true,
    port: 5173,
    strictPort: true,
    proxy: {
      '/v1': {
        target: 'http://127.0.0.1:5080',
        changeOrigin: true,
      },
    },
  },
  preview: {
    host: true,
    port: 5173,
  },
})
