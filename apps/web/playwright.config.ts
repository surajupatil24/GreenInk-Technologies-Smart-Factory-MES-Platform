import { defineConfig } from "@playwright/test";
const port = process.env.GREENINK_E2E_PORT || "5173";
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: "list",
  use: {
    baseURL: `http://127.0.0.1:${port}`,
    browserName: "chromium",
    launchOptions: { executablePath: process.env.GREENINK_BROWSER_PATH },
    trace: "off",
    screenshot: "off",
    video: "off",
  },
  webServer: {
    command: `npm run dev -- --port ${port} --strictPort`,
    url: `http://127.0.0.1:${port}`,
    reuseExistingServer: !process.env.CI,
  },
});
