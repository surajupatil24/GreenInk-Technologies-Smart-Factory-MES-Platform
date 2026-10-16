import { expect, test } from "@playwright/test";
import { randomBytes } from "node:crypto";
const email = process.env.GREENINK_E2E_EMAIL,
  password = process.env.GREENINK_E2E_PASSWORD;
test.skip(!email || !password, "Requires isolated development administrator.");
test("receiving desk drives checklist gates, packed receipt and token exit", async ({
  page,
}) => {
  test.setTimeout(90000);
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
    name: `Receiving ${suffix}`,
    code: `RT-${suffix}`,
  });
  const plant = await post("plants", {
    name: `Receiving plant ${suffix}`,
    code: `RP-${suffix}`,
    tenantId: tenant.id,
  });
  const plantId = plant.id;
  const raw = await post("operations/materials", {
    plantId,
    code: `RAW-${suffix}`,
    name: "Packed validation material",
    uom: "kg",
    kind: "Raw",
  });
  const supplier = await post("operations/partners", {
    plantId,
    code: `SUP-${suffix}`,
    name: "Receiving validation supplier",
    kind: "Supplier",
  });
  const po = await post("operations/purchase-orders", {
    plantId,
    number: `PO-${suffix}`,
    supplierId: supplier.id,
    materialId: raw.id,
    quantity: 100,
  });
  await page.reload();
  await page.locator("#tenant").selectOption(tenant.id);
  await page.locator("#plant").selectOption(plantId);
  await page.getByRole("link", { name: "Inbound", exact: true }).click();
  const form = (title: string) =>
    page
      .locator("section.inbound-action")
      .filter({ has: page.getByRole("heading", { name: title, exact: true }) });
  async function save(title: string, path: string) {
    const done = page.waitForResponse(
      (r) =>
        r.request().method() === "POST" &&
        new URL(r.url()).pathname === `/api/operations/${path}`,
    );
    await form(title).getByRole("button", { name: title, exact: true }).click();
    const r = await done;
    expect(r.ok(), await r.text()).toBeTruthy();
    return r.json();
  }
  await page.getByRole("tab", { name: "Receiving setup", exact: true }).click();
  await expect(form("Save receiving rules").getByRole("button")).toBeEnabled();
  await form("Save receiving rules")
    .getByLabel("Require an unloading truck for every receipt")
    .check();
  await form("Save receiving rules")
    .getByLabel("Require a delivery note")
    .check();
  await save("Save receiving rules", "receiving/policy");
  await form("Save dock").getByLabel("Dock code").fill(`D-${suffix}`);
  await form("Save dock").getByLabel("Dock name").fill("Packing bay");
  await save("Save dock", "receiving/docks");
  const check = form("Save checklist rule");
  await check.getByLabel("Check prompt").fill("Safety briefing completed");
  await save("Save checklist rule", "receiving/checks");
  await check.getByLabel("Check prompt").fill("Unloading zone safe");
  await check.getByLabel("Gate", { exact: true }).selectOption("Unloading");
  await save("Save checklist rule", "receiving/checks");
  await page.getByRole("tab", { name: "Truck & dock", exact: true }).click();
  const register = form("Register truck");
  await register.getByLabel("Truck registration number").fill(`TR-${suffix}`);
  await register.getByLabel("Driver name").fill("Inbound validation driver");
  await register
    .getByLabel("Scheduled arrival")
    .fill(new Date().toISOString().slice(0, 16));
  await register.getByLabel("Registered purchase orders").selectOption(po.id);
  const visit = await save("Register truck", "receiving/visits");
  await page
    .getByRole("button")
    .filter({ hasText: `TR-${suffix}` })
    .click();
  await save("Mark Arrived", `receiving/visits/${visit.id}/status`);
  const dockForm = form("Mark Docked");
  await dockForm
    .getByLabel("Receiving dock")
    .selectOption({ label: `D-${suffix}` });
  await dockForm.getByRole("button").click();
  await expect(dockForm.getByRole("alert")).toContainText("Safety briefing");
  let detail = await (
    await page.request.get(
      `/api/operations/receiving/visits/${visit.id}?plantId=${plantId}`,
    )
  ).json();
  await form("Save arrival check")
    .getByLabel("Result", { exact: true })
    .selectOption("true");
  await save(
    "Save arrival check",
    `receiving/checks/${detail.checks.find((c: any) => c.stage === "Arrival").id}/answer`,
  );
  await save("Mark Docked", `receiving/visits/${visit.id}/status`);
  await form("Save unloading check")
    .getByLabel("Result", { exact: true })
    .selectOption("true");
  await save(
    "Save unloading check",
    `receiving/checks/${detail.checks.find((c: any) => c.stage === "Unloading").id}/answer`,
  );
  await save("Mark Unloading", `receiving/visits/${visit.id}/status`);
  await page.getByRole("tab", { name: "Packed receipt", exact: true }).click();
  const packed = form("Post packed receipt");
  await packed.getByLabel("Unloading truck").selectOption(visit.id);
  await packed.getByLabel("Packed purchase order").selectOption(po.id);
  await packed
    .getByLabel("Delivery note", { exact: true })
    .fill(`DN-${suffix}`);
  await packed.getByLabel("Supplier batch", { exact: true }).fill("SUP-LOT");
  await packed.getByLabel("Internal batch number").fill(`B-${suffix}`);
  await packed.getByLabel("Pack size").fill("10");
  await packed.getByLabel("Whole pack count").fill("2");
  await packed.getByLabel("Loose quantity", { exact: true }).fill("5");
  await expect(page.locator(".inbound-total strong")).toHaveText("25");
  const receipt = await save("Post packed receipt", "receipts");
  expect(receipt.quantity).toBe(25);
  await page.getByRole("tab", { name: "Truck & dock", exact: true }).click();
  await save("Mark Unloaded", `receiving/visits/${visit.id}/status`);
  await save("Mark Out", `receiving/visits/${visit.id}/status`);
  await expect(page.getByText(/Exited · token inactive/)).toBeVisible();
  const stock = await (
    await page.request.get(`/api/operations/inventory?plantId=${plantId}`)
  ).json();
  expect(stock).toHaveLength(1);
  expect(stock[0].pallet.quantity).toBe(25);
  expect(stock[0].batch.qualityStatus).toBe("Hold");
  await page.setViewportSize({ width: 390, height: 844 });
  await page.getByRole("tab", { name: "Receiving setup", exact: true }).click();
  await expect(form("Save receiving rules")).toBeVisible();
  await expect(page.locator(".inbound-desk")).toBeVisible();
  await page.screenshot({
    path: "/tmp/greenink-inbound-mobile.png",
    fullPage: true,
  });
});
