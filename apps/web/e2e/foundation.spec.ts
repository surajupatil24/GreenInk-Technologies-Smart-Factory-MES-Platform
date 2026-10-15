import { expect, test } from "@playwright/test";
import { randomBytes } from "node:crypto";
const email = process.env.GREENINK_E2E_EMAIL;
const password = process.env.GREENINK_E2E_PASSWORD;
test.skip(
  !email || !password,
  "Set GREENINK_E2E_EMAIL and GREENINK_E2E_PASSWORD for an isolated development platform administrator.",
);
test("administrator manages organizations, plants and user access through real APIs", async ({
  page,
}) => {
  const suffix = randomBytes(5).toString("hex");
  const tenantName = `E2E organization ${suffix}`;
  const plantName = `E2E plant ${suffix}`;
  const userName = `E2E operator ${suffix}`;
  const userEmail = `operator-${suffix}@example.invalid`;
  const userPassword = `G!${randomBytes(18).toString("base64url")}a2`;
  await page.goto("/");
  await page.getByLabel("Email address").fill(email!);
  await page.getByLabel("Password", { exact: true }).fill(password!);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Command Center", exact: true }),
  ).toBeVisible();
  await page.getByRole("link", { name: "Administration", exact: true }).click();
  await page.getByRole("tab", { name: "Organizations", exact: true }).click();
  await page.getByLabel("Name", { exact: true }).fill(tenantName);
  await page.getByLabel("Code", { exact: true }).fill(`T-${suffix}`);
  await page
    .getByRole("button", { name: "Create organization", exact: true })
    .click();
  await expect(
    page.getByRole("cell", { name: tenantName, exact: true }),
  ).toBeVisible();
  await page.getByRole("tab", { name: "Plants", exact: true }).click();
  await page.getByLabel("Name", { exact: true }).fill(plantName);
  await page.getByLabel("Code", { exact: true }).fill(`P-${suffix}`);
  await page
    .locator("form")
    .getByLabel("Organization", { exact: true })
    .selectOption({ label: tenantName });
  await page.getByRole("button", { name: "Create plant", exact: true }).click();
  await expect(
    page.getByRole("cell", { name: plantName, exact: true }),
  ).toBeVisible();
  await page.getByRole("tab", { name: "Users", exact: true }).click();
  await page.getByLabel("Full name", { exact: true }).fill(userName);
  await page.getByLabel("Email", { exact: true }).fill(userEmail);
  await page.getByLabel("Initial password", { exact: true }).fill(userPassword);
  await page.getByLabel("Role", { exact: true }).selectOption("Operator");
  await page
    .locator("form")
    .getByLabel("Organization", { exact: true })
    .selectOption({ label: tenantName });
  await page.getByRole("checkbox", { name: plantName, exact: true }).check();
  await page.getByRole("button", { name: "Create user", exact: true }).click();
  const userRow = page.getByRole("row").filter({ hasText: userName });
  await expect(userRow).toContainText("Operator");
  await userRow
    .getByRole("button", { name: "Edit access", exact: true })
    .click();
  const editor = page.getByRole("dialog");
  await expect(
    editor.getByRole("checkbox", { name: plantName, exact: true }),
  ).toBeChecked();
  await editor.getByLabel("Role", { exact: true }).selectOption("Viewer");
  await editor
    .getByRole("button", { name: "Save access", exact: true })
    .click();
  await expect(editor).not.toBeVisible();
  await expect(userRow).toContainText("Viewer");
  await page.getByRole("tab", { name: "Audit", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Audit history", exact: true }),
  ).toBeVisible();
  await expect(page.locator("tbody tr").first()).toBeVisible();
  await page.getByRole("link", { name: "Command Center", exact: true }).click();
  await expect(
    page.locator("#main").getByText(plantName, { exact: true }),
  ).toBeVisible();
  await page.locator("#tenant").selectOption({ label: tenantName });
  await expect(
    page
      .locator(".stat")
      .filter({ hasText: "Organizations" })
      .locator("strong"),
  ).toHaveText("1");
  await expect(
    page.locator(".stat").filter({ hasText: "Plants" }).locator("strong"),
  ).toHaveText("1");
  await page.locator("#plant").selectOption({ label: plantName });
  const selectedTenant = await page.locator("#tenant").inputValue();
  const selectedPlant = await page.locator("#plant").inputValue();
  const scoped = await page.request.get(
    `/api/dashboard?tenantId=${selectedTenant}&plantId=${selectedPlant}`,
  );
  expect(scoped.ok()).toBeTruthy();
  const scopedCounts = await scoped.json();
  expect(scopedCounts.tenantCount).toBe(1);
  expect(scopedCounts.plantCount).toBe(1);
  expect(scopedCounts.userCount).toBe(1);
  const totals = await page.request.get("/api/dashboard");
  expect(totals.ok()).toBeTruthy();
  const counts = await totals.json();
  expect(counts.tenantCount).toBeGreaterThan(0);
  expect(counts.plantCount).toBeGreaterThan(0);
  expect(counts.userCount).toBeGreaterThan(1);
  await page.evaluate(() => window.scrollTo(0, 0));
  await page.screenshot({
    path: "/tmp/greenink-dashboard.png",
    fullPage: true,
  });
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(
    page.getByRole("button", { name: "Open navigation", exact: true }),
  ).toBeVisible();
  await expect(page.locator(".sidebar")).toHaveCSS("transform", "matrix(1, 0, 0, 1, -247, 0)");
  await page.screenshot({
    path: "/tmp/greenink-dashboard-mobile.png",
    fullPage: true,
  });
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.getByRole("button", { name: "Sign out", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Welcome to SmartFactory", exact: true }),
  ).toBeVisible();
  expect((await page.request.get("/api/auth/me")).status()).toBe(401);
  await page.getByLabel("Email address").fill(userEmail);
  await page.getByLabel("Password", { exact: true }).fill(userPassword);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Command Center", exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("link", { name: "Administration", exact: true }),
  ).toHaveCount(0);
  await page
    .getByRole("button", { name: "Change password", exact: true })
    .click();
  const passwordEditor = page.getByRole("dialog");
  const nextPassword = `N!${randomBytes(18).toString("base64url")}b3`;
  await passwordEditor
    .getByLabel("Current password", { exact: true })
    .fill(userPassword);
  await passwordEditor
    .getByLabel("New password", { exact: true })
    .fill(nextPassword);
  await passwordEditor
    .getByRole("button", { name: "Change password and sign out", exact: true })
    .click();
  await expect(
    page.getByRole("heading", { name: "Welcome to SmartFactory", exact: true }),
  ).toBeVisible();
  expect((await page.request.get("/api/auth/me")).status()).toBe(401);
  await page.getByLabel("Email address").fill(userEmail);
  await page.getByLabel("Password", { exact: true }).fill(nextPassword);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Command Center", exact: true }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Sign out", exact: true }).click();
});
