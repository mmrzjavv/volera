import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'
import basicSsl from '@vitejs/plugin-basic-ssl'
import { readFileSync } from 'node:fs'

const pkg = JSON.parse(readFileSync(new URL('./package.json', import.meta.url), 'utf-8'))

// HTTPS is required for getUserMedia on phones (especially iOS) when opening via LAN IP.
// Open https://<your-lan-ip>:5173 and accept the self-signed cert once.
// https://vite.dev/config/
export default defineConfig({
  define: {
    __APP_VERSION__: JSON.stringify(pkg.version),
  },
  plugins: [
    react(),
    basicSsl(),
    VitePWA({
      strategies: 'injectManifest',
      srcDir: 'src',
      filename: 'sw.ts',
      registerType: 'autoUpdate',
      devOptions: {
        enabled: true,
        type: 'module',
      },
      includeAssets: ['icon.svg'],
      manifest: {
        name: 'Volera',
        short_name: 'Volera',
        description: 'Secure messaging and calls',
        theme_color: '#0d9488',
        icons: [
          {
            src: 'icon.svg',
            sizes: 'any',
            type: 'image/svg+xml',
            purpose: 'any maskable'
          }
        ]
      }
    })
  ],
  server: {
    host: true,
    port: 5173,
    // Keep client Host / X-Forwarded-Host so /Call/ice-servers can advertise the LAN IP for Coturn.
    proxy: {
      '/api': {
        target: 'http://localhost:5002',
        changeOrigin: true,
        configure: (proxy) => {
          proxy.on('proxyReq', (proxyReq, req) => {
            const host = req.headers.host;
            if (host) proxyReq.setHeader('X-Forwarded-Host', host);
            proxyReq.setHeader('X-Forwarded-Proto', 'https');
          });
        },
      },
      '/callHub': {
        target: 'http://localhost:5002',
        ws: true,
        changeOrigin: true,
        configure: (proxy) => {
          proxy.on('proxyReq', (proxyReq, req) => {
            const host = req.headers.host;
            if (host) proxyReq.setHeader('X-Forwarded-Host', host);
          });
        },
      },
      '/chatHub': {
        target: 'http://localhost:5002',
        ws: true,
        changeOrigin: true,
      },
      '/health': 'http://localhost:5002',
      '/version': 'http://localhost:5002',
    },
  },
})
