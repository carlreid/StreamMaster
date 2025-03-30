import { defineConfig } from 'vite';
import type { UserConfig } from 'vite';
import baseConfig from './vite.config';

const baseViteConfig = baseConfig as UserConfig;

export default defineConfig({
  ...baseViteConfig,
  server: {
    ...baseViteConfig.server,
    watch: {
      usePolling: true,
      interval: 1000
    }
  }
});
