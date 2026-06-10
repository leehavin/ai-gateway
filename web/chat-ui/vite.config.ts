import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  // IPSpace CefSharp 静态服务从子目录加载，须用相对路径（勿用 /assets/ 绝对路径）
  base: './',
  plugins: [vue()],
  build: {
    // 与 IPSpace 内置 CefSharp 84（Chromium 84）对齐
    target: 'chrome84',
  },
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
