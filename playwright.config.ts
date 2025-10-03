import { defineConfig, devices } from '@playwright/test';

// Use environment variables for flexible testing
// Default to localhost for local dev, or use deployed URLs for CI
const FRONTEND_URL = process.env.FRONTEND_URL || 'http://localhost:5173';
const BACKEND_URL = process.env.BACKEND_URL || 'http://localhost:5280';

// In CI, skip webServer if using deployed backend
const shouldStartServer = !process.env.CI || BACKEND_URL.includes('localhost');

export default defineConfig({
  testDir: './tests',
  timeout: 60_000,
  expect: { timeout: 10_000 },
  fullyParallel: true,
  reporter: [['list']],
  use: {
    baseURL: FRONTEND_URL,
    trace: 'on-first-retry',
  },
  webServer: shouldStartServer ? [
    {
      command: 'IOS_SIM=0 bash scripts/start.sh',
      url: FRONTEND_URL,
      reuseExistingServer: true,
      timeout: 120_000,
    },
  ] : undefined,
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
