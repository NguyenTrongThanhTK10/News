import { defineConfig, devices } from '@playwright/test';
import { fileURLToPath } from 'node:url';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  workers: 1,
  reporter: 'list',
  outputDir: '../artifacts/playwright',
  use: {
    baseURL: 'http://127.0.0.1:5174',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    launchOptions: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH
      ? { executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH }
      : {},
  },
  projects: [
    { name: 'desktop', use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 1000 } } },
    { name: 'mobile', use: { ...devices['Pixel 7'] } },
  ],
  // Luôn khởi động server riêng để dữ liệu test không ghi vào database đang phát triển.
  webServer: [
    {
      command: 'dotnet run --project News.Api --no-launch-profile --urls http://127.0.0.1:5081',
      cwd: fileURLToPath(new URL('../backend', import.meta.url)),
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development', Database__Provider: 'Sqlite',
        ConnectionStrings__Sqlite: 'Data Source=../news-e2e.db',
        RateLimits__LoginPermitLimit: '100', RateLimits__FeedbackPermitLimit: '100',
        Cors__AllowedOrigins__0: 'http://127.0.0.1:5174', Cors__AllowedOrigins__1: 'http://localhost:5174',
      },
      url: 'http://127.0.0.1:5081/health',
      reuseExistingServer: false,
      timeout: 60_000,
    },
    {
      command: 'npm run dev -- --port 5174',
      env: { VITE_PROXY_TARGET: 'http://127.0.0.1:5081', VITE_API_URL: '/api' },
      url: 'http://127.0.0.1:5174',
      reuseExistingServer: false,
      timeout: 30_000,
    },
  ],
});
