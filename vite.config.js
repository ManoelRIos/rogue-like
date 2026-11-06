import { defineConfig } from 'vite';

// base relativa para funcionar no GitHub Pages (/rogue-like/) e localmente
export default defineConfig({
  base: './',
  build: { outDir: 'dist', chunkSizeWarningLimit: 1200 },
});
