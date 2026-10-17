import { expect, test, type Locator } from "@playwright/test";
import { randomBytes } from "node:crypto";
import { readFile } from "node:fs/promises";
const email = process.env.GREENINK_E2E_EMAIL,
  password = process.env.GREENINK_E2E_PASSWORD;
test.skip(
  !email || !password,
  "Requires isolated development administrator credentials.",
);
test("guided preparation moves, weighs, delivers and issues a real kit with responsive layout", async ({
  page,
}) => {
  test.setTimeout(120000);
  page.setDefaultTimeout(10000);
  const suffix = randomBytes(4).toString("hex").toUpperCase();
  await page.goto("/");
  await page.getByLabel("Email address").fill(email!);
  await page.getByLabel("Password", { exact: true }).fill(password!);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Command Center", exact: true }),
  ).toBeVisible();
  async function post(path: string, data: object) {
    const r = await page.request.post(`/api/${path}`, {
      headers: { "X-GreenInk-Request": "1" },
      data,
    });
    expect(r.ok(), await r.text()).toBeTruthy();
    return r.json();
  }
  const tenant = await post("tenants", {
    name: `Preparation ${suffix}`,
    code: `PT-${suffix}`,
  });
  const plant = await post("plants", {
    tenantId: tenant.id,
    name: `Preparation plant ${suffix}`,
    code: `PP-${suffix}`,
  });
  const plantId = plant.id;
  const supplier = await post("operations/partners", {
    plantId,
    code: `SUP-${suffix}`,
    name: "Validation supplier",
    kind: "Supplier",
  });
  const wh = await post("operations/bins", {
    plantId,
    code: `WH-${suffix}`,
    name: "Raw material warehouse",
    warehouse: "Main",
  });
  const stage = await post("operations/bins", {
    plantId,
    code: `ST-${suffix}`,
    name: "Preparation room",
    warehouse: "Main",
  });
  const supply = await post("operations/bins", {
    plantId,
    code: `PS-${suffix}`,
    name: "Mixer 01 supply",
    warehouse: "Main",
  });
  const fg = await post("operations/materials", {
    plantId,
    code: `FG-${suffix}`,
    name: "Premium green coating",
    uom: "kg",
    kind: "Finished",
  });
  const raw = await post("operations/materials", {
    plantId,
    code: `RM-${suffix}`,
    name: "Acrylic resin",
    uom: "kg",
    kind: "Raw",
  });
  const po = await post("operations/purchase-orders", {
    plantId,
    number: `PO-${suffix}`,
    supplierId: supplier.id,
    materialId: raw.id,
    quantity: 100,
  });
  const received = await post("operations/receipts", {
    plantId,
    purchaseOrderId: po.id,
    quantity: 100,
    supplierBatch: `LOT-${suffix}`,
    batchNumber: `B-${suffix}`,
    commandId: crypto.randomUUID(),
  });
  await post("operations/quality-inspections", {
    plantId,
    batchId: received.batchId,
    decision: "Released",
    notes: "Validation release",
    commandId: crypto.randomUUID(),
  });
  await post("operations/transfers", {
    plantId,
    palletId: received.palletId,
    location: wh.code,
    commandId: crypto.randomUUID(),
  });
  const stock = await (
    await page.request.get(`/api/operations/inventory?plantId=${plantId}`)
  ).json();
  const source = stock[0].pallet;
  const order = await post("operations/production-orders", {
    plantId,
    number: `MO-${suffix}`,
    materialId: fg.id,
    plannedQuantity: 25,
    requirements: [{ materialId: raw.id, quantity: 10 }],
  });
  await post(`operations/production-orders/${order.id}/release`, {
    plantId,
    commandId: crypto.randomUUID(),
  });
  for (let n = 2; n <= 3; n++) {
    const next = await post("operations/production-orders", {
      plantId,
      number: `MO-${suffix}-${n}`,
      materialId: fg.id,
      plannedQuantity: 25 * n,
      requirements: [{ materialId: raw.id, quantity: 5 * n }],
    });
    await post(`operations/production-orders/${next.id}/release`, {
      plantId,
      commandId: crypto.randomUUID(),
    });
  }
  await page.reload();
  await page.locator("#tenant").selectOption(tenant.id);
  await page.locator("#plant").selectOption(plantId);
  await page
    .getByRole("link", { name: "Material Preparation", exact: true })
    .click();
  const nav = page.getByRole("navigation", {
    name: "Material Preparation sections",
  });
  async function tab(name: string) {
    await nav.getByRole("button", { name, exact: true }).click();
  }
  async function save(button: string, path: string, scope?: Locator) {
    const response = page.waitForResponse(
      (r) =>
        r.request().method() === "POST" &&
        new URL(r.url()).pathname === `/api/operations/${path}`,
    );
    await (scope || page)
      .getByRole("button", { name: button, exact: true })
      .click();
    const r = await response;
    expect(r.ok(), await r.text()).toBeTruthy();
    return r.json();
  }
  await tab("Setup");
  await page.getByLabel("Area code", { exact: true }).fill(`AREA-${suffix}`);
  await page
    .getByLabel("Area name", { exact: true })
    .fill("Coatings preparation");
  await page.getByLabel("Staging bin", { exact: true }).selectOption(stage.id);
  await page
    .getByLabel("Production supply bin", { exact: true })
    .selectOption(supply.id);
  const area = await save("Save staging area", "staging/areas");
  await page
    .getByRole("navigation", { name: "Preparation setup" })
    .getByRole("button", { name: "Material rules" })
    .click();
  await page.getByLabel("Rule material", { exact: true }).selectOption(raw.id);
  await page.getByLabel("Default pack size", { exact: true }).fill("20");
  await page.getByLabel("Weight tolerance (%)", { exact: true }).fill("1");
  await page.getByLabel("Require preweigh readings").check();
  await page
    .getByLabel("Safety & PPE instructions", { exact: true })
    .fill("Wear chemical-resistant gloves and eye protection.");
  await page
    .getByLabel("Handling notes", { exact: true })
    .fill("Keep the resin container closed between operations.");
  await save("Save material rule", "staging/rules");
  await tab("Orders & kits");
  await page
    .locator(".prep-order-card")
    .filter({ has: page.getByText(order.number, { exact: true }) })
    .first()
    .click();
  await page
    .getByRole("button", { name: "Schedule preparation", exact: true })
    .click();
  let dialog = page.getByRole("dialog");
  await dialog
    .getByLabel("Preparation area", { exact: true })
    .selectOption(area.id);
  await dialog.getByLabel("Prioritize this order").check();
  const plan = await save("Save preparation plan", "staging/plans", dialog);
  await expect(dialog).not.toBeVisible();
  await expect(
    page.getByRole("button", { name: "Prepare material", exact: true }),
  ).toBeVisible();
  await page.setViewportSize({ width: 1600, height: 1050 });
  await page.evaluate(() => window.scrollTo(0, 0));
  await page.screenshot({
    path: "/tmp/greenink-preparation-desktop.png",
    fullPage: true,
  });
  await page
    .getByRole("button", { name: "Prepare material", exact: true })
    .click();
  dialog = page.getByRole("dialog");
  await dialog.locator(".prep-source-list button").first().click();
  await dialog.getByRole("button", { name: "Continue", exact: true }).click();
  await dialog.getByLabel("Scan source bin", { exact: true }).fill(wh.code);
  await dialog
    .getByLabel("Scan material label", { exact: true })
    .fill(source.code);
  await expect(
    dialog.getByLabel("Physical quantity to pick", { exact: true }),
  ).toHaveValue("20");
  await dialog.getByLabel("I have read the safety instructions").check();
  const pick = await save(
    "Confirm material pick",
    `staging/plans/${plan.id}/pick`,
    dialog,
  );
  await page.getByRole("button", { name: /Kit contents/ }).click();
  await page.getByRole("button", { name: "Weigh & kit", exact: true }).click();
  dialog = page.getByRole("dialog");
  await dialog.getByLabel("Gross weight", { exact: true }).fill("12");
  await dialog.getByLabel("Tare weight", { exact: true }).fill("1");
  await expect(
    dialog.getByRole("button", { name: "Confirm weighed kit", exact: true }),
  ).toBeDisabled();
  await dialog.getByLabel("Gross weight", { exact: true }).fill("11");
  await expect(
    dialog.getByText("Within tolerance", { exact: true }),
  ).toBeVisible();
  await dialog
    .getByLabel("Scale / reading reference", { exact: true })
    .fill("Bench scale 02 / manual");
  await page.screenshot({
    path: "/tmp/greenink-preparation-weighing.png",
    fullPage: true,
  });
  await save(
    "Confirm weighed kit",
    `staging/picks/${pick.resultId}/weigh`,
    dialog,
  );
  await page.getByRole("button", { name: "Deliver kit", exact: true }).click();
  dialog = page.getByRole("dialog");
  await dialog.getByLabel("Scan kit code", { exact: true }).fill(plan.kitCode);
  await dialog.getByLabel("Scan supply bin", { exact: true }).fill(supply.code);
  await save(
    "Confirm kit delivery",
    `staging/plans/${plan.id}/putaway`,
    dialog,
  );
  const detail = await (
    await page.request.get(
      `/api/operations/staging/plans/${plan.id}?plantId=${plantId}`,
    )
  ).json();
  await save(
    "Issue to production",
    `reservations/${detail.reservations[0].id}/issue`,
  );
  await expect(
    page.locator(".prep-kit-row").getByText("Issued", { exact: true }),
  ).toBeVisible();
  await tab("Replenishment");
  await page
    .getByRole("button", { name: "Request material", exact: true })
    .click();
  dialog = page.getByRole("dialog");
  await dialog
    .getByLabel("Requested material", { exact: true })
    .selectOption(raw.id);
  await dialog
    .getByLabel("Replenishment target bin", { exact: true })
    .selectOption(stage.id);
  await dialog.getByLabel("Request quantity", { exact: true }).fill("5");
  await dialog.getByLabel("Linked preparation plan", { exact: true }).selectOption(plan.id);
  await dialog
    .getByLabel("Why is material needed?", { exact: true })
    .fill("=1+1");
  const req = await save(
    "Create replenishment request",
    "staging/requests",
    dialog,
  );
  await page
    .getByRole("button", { name: "Cancel request", exact: true })
    .click();
  dialog = page.getByRole("dialog");
  await dialog
    .getByLabel("Cancellation reason", { exact: true })
    .fill("Schedule moved");
  await save(
    "Confirm cancellation",
    `staging/requests/${req.resultId}/cancel`,
    dialog,
  );
  await expect(page.getByText("Cancelled", { exact: true })).toBeVisible();
  await tab("Orders & kits");
  await page.getByRole("button", {name:"Activity & weights",exact:true}).click();
  const downloadPromise=page.waitForEvent("download");
  await page.getByRole("button", {name:"Export shown events",exact:true}).click();
  const download=await downloadPromise;
  expect(download.suggestedFilename()).toBe(`${plan.kitCode}-events.csv`);
  await download.saveAs("/tmp/greenink-preparation-events.csv");
  const csv=await readFile("/tmp/greenink-preparation-events.csv","utf8");
  expect(csv).toContain("KitDelivered");expect(csv).toContain("'=1+1");
  await tab("Setup");
  await page
    .getByRole("navigation", { name: "Preparation setup" })
    .getByRole("button", { name: "Kanban policies" })
    .click();
  await page.getByLabel("Buffer bin", { exact: true }).selectOption(supply.id);
  await page
    .getByLabel("Buffer material", { exact: true })
    .selectOption(raw.id);
  await page.getByLabel("Urgent below", { exact: true }).fill("10");
  await page.getByLabel("Reorder below", { exact: true }).fill("20");
  await page.getByLabel("Maximum buffer stock", { exact: true }).fill("40");
  await save("Save Kanban policy", "staging/kanban");
  await tab("Kanban buffers");
  await expect(
    page.locator(".prep-kanban").getByText("Urgent", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Request 40", exact: true }),
  ).toBeEnabled();
  await tab("Orders & kits");
  await page.setViewportSize({ width: 390, height: 844 });
  await page
    .getByRole("button", { name: "Material requirements", exact: true })
    .click();
  await expect(page.locator(".prep-material")).toBeVisible();
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth,
    ),
  ).toBeTruthy();
  await page.screenshot({
    path: "/tmp/greenink-preparation-mobile.png",
    fullPage: true,
  });
  await page
    .getByRole("button", { name: "Use dark theme", exact: true })
    .click();
  await expect(page.locator(".app.dark")).toBeVisible();
  await page.screenshot({
    path: "/tmp/greenink-preparation-dark-mobile.png",
    fullPage: true,
  });
  const inv = await (
    await page.request.get(`/api/operations/inventory?plantId=${plantId}`)
  ).json();
  expect(inv.reduce((sum: number, r: any) => sum + r.pallet.quantity, 0)).toBe(
    90,
  );
  expect(
    inv.reduce((sum: number, r: any) => sum + r.pallet.reservedQuantity, 0),
  ).toBe(0);
});
