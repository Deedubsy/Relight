import { defineConfig } from 'vite';
import { fileURLToPath } from 'node:url';

export default defineConfig({
  server: { fs: { allow: [fileURLToPath(new URL('..', import.meta.url))] }, port: 5173 },
  build: { target: 'es2022' },
});
