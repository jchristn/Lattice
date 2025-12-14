import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      '/v1.0': {
        target: 'http://localhost:5099',
        changeOrigin: true,
      },
    },
  },
})
