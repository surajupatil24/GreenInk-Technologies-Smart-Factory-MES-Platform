import { expect, test } from "@playwright/test";
import { randomBytes } from "node:crypto";
const email = process.env.GREENINK_E2E_EMAIL,
  password = process.env.GREENINK_E2E_PASSWORD;
test.skip(!email || !password, "Requires isolated development administrator.");
test("dispatch planner drives grouped scan picks, handling units, loading, local issue and audited return", async ({
  page,
}) => {
  test.setTimeout(150000);
  page.setDefaultTimeout(10000);
  await page.setViewportSize({ width: 1600, height: 1000 });
  await page.goto("/");
  await page.getByLabel("Email address").fill(email!);
  await page.getByLabel("Password", { exact: true }).fill(password!);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Command Center", exact: true }),
  ).toBeVisible();
  const headers = { "X-GreenInk-Request": "1" },
    suffix = randomBytes(4).toString("hex").toUpperCase();
  async function post(path: string, data: object) {
    const r = await page.request.post("/api/" + path, { headers, data });
    expect(r.ok(), await r.text()).toBeTruthy();
    return r.json();
  }
  const tenant = await post("tenants", {
    name: `Dispatch Validation ${suffix}`,
    code: `DT-${suffix}`,
  });
  const plant = await post("plants", {
      name: `Dispatch plant ${suffix}`,
      code: `DP-${suffix}`,
      tenantId: tenant.id,
    }),
    plantId = plant.id;
  const raw = await post("operations/materials", {
      plantId,
      code: `RM-${suffix}`,
      name: "Validation raw",
      uom: "kg",
      kind: "Raw",
    }),
    finished = await post("operations/materials", {
      plantId,
      code: `FG-${suffix}`,
      name: "Finished food-grade blend",
      uom: "kg",
      kind: "Finished",
    }),
    supplier = await post("operations/partners", {
      plantId,
      code: `SUP-${suffix}`,
      name: "Validation supplier",
      kind: "Supplier",
    }),
    customer = await post("operations/partners", {
      plantId,
      code: `C-${suffix}`,
      name: "Horizon Foods (Validation)",
      kind: "Customer",
    });
  const sourceBin = await post("operations/bins", {
      plantId,
      code: "SRC",
      name: "Released finished rack",
      warehouse: "MAIN",
    }),
    packBin = await post("operations/bins", {
      plantId,
      code: "PACK",
      name: "Outbound packing",
      warehouse: "MAIN",
    }),
    loadBin = await post("operations/bins", {
      plantId,
      code: "LOAD",
      name: "Dispatch gate",
      warehouse: "MAIN",
    });
  const po = await post("operations/purchase-orders", {
      plantId,
      number: `PO-${suffix}`,
      supplierId: supplier.id,
      materialId: raw.id,
      quantity: 100,
    }),
    receipt = await post("operations/receipts", {
      plantId,
      purchaseOrderId: po.id,
      quantity: 100,
      supplierBatch: "RAW-LOT",
      batchNumber: `RAW-${suffix}`,
      commandId: crypto.randomUUID(),
    });
  await post("operations/quality-inspections", {
    plantId,
    batchId: receipt.batchId,
    decision: "Released",
    notes: "Validated incoming stock",
    commandId: crypto.randomUUID(),
  });
  const order = await post("operations/production-orders", {
    plantId,
    number: `PR-${suffix}`,
    materialId: finished.id,
    plannedQuantity: 100,
    requirements: [{ materialId: raw.id, quantity: 100 }],
  });
  await post(`operations/production-orders/${order.id}/release`, {
    plantId,
    commandId: crypto.randomUUID(),
  });
  const reserve = await post("operations/reservations", {
    plantId,
    productionOrderId: order.id,
    palletId: receipt.palletId,
    quantity: 100,
    commandId: crypto.randomUUID(),
  });
  await post(`operations/reservations/${reserve.id}/issue`, {
    plantId,
    commandId: crypto.randomUUID(),
  });
  const confirmation = await post(
    `operations/production-orders/${order.id}/complete`,
    {
      plantId,
      quantity: 100,
      scrapQuantity: 0,
      location: "SRC",
      batchNumber: `FG-${suffix}`,
      expiryDate: "2028-12-31T00:00:00Z",
      commandId: crypto.randomUUID(),
    },
  );
  await post("operations/quality-inspections", {
    plantId,
    batchId: confirmation.batchId,
    decision: "Released",
    notes: "Finished inspection accepted",
    commandId: crypto.randomUUID(),
  });
  const orderA = await post("operations/delivery-orders", {
      plantId,
      number: `DA-${suffix}`,
      customerId: customer.id,
      materialId: finished.id,
      quantity: 10,
    }),
    orderB = await post("operations/delivery-orders", {
      plantId,
      number: `DB-${suffix}`,
      customerId: customer.id,
      materialId: finished.id,
      quantity: 5,
    });
  await page.reload();
  await page.locator("#tenant").selectOption(tenant.id);
  await page.locator("#plant").selectOption(plantId);
  await page.getByRole("link", { name: "Dispatch", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Dispatch", exact: true }),
  ).toBeVisible();
  const dialog = () => page.getByRole("dialog");
  const setup = (name: string) =>
    page
      .locator(".dispatch-setup section")
      .filter({ has: page.getByRole("heading", { name, exact: true }) });
  async function tab(name: string) {
    await page.getByRole("tab", { name, exact: true }).click();
  }
  async function save(button: string, path: string) {
    const done = page.waitForResponse(
      (r) =>
        r.request().method() === "POST" &&
        new URL(r.url()).pathname === "/api/operations/dispatch/" + path,
    );
    await page.getByRole("button", { name: button, exact: true }).click();
    const r = await done;
    expect(r.ok(), await r.text()).toBeTruthy();
    return r.json();
  }
  await tab("Setup");
  await setup("Dispatch areas")
    .getByRole("button", { name: "Add", exact: true })
    .click();
  await dialog()
    .getByLabel("Dispatch area code", { exact: true })
    .fill("OUTBOUND");
  await dialog()
    .getByLabel("Dispatch area name", { exact: true })
    .fill("Outbound control");
  await dialog().getByLabel("Shipping point", { exact: true }).fill("SHIP-01");
  await dialog()
    .getByLabel("Packing bin", { exact: true })
    .selectOption(packBin.id);
  await dialog()
    .getByLabel("Loading bin", { exact: true })
    .selectOption(loadBin.id);
  await dialog()
    .getByLabel("Picking mode", { exact: true })
    .selectOption("Group");
  await dialog()
    .getByLabel("Packing mode", { exact: true })
    .selectOption("Collective");
  const area = await save("Save dispatch area", "areas");
  await setup("Customer instructions")
    .getByRole("button", { name: "Add", exact: true })
    .click();
  await dialog()
    .getByLabel("Instruction customer", { exact: true })
    .selectOption(customer.id);
  await dialog()
    .getByLabel("Picking instructions", { exact: true })
    .fill("Verify the batch and seals before picking.");
  await dialog()
    .getByLabel("Packing instructions", { exact: true })
    .fill("Protect the packages and keep labels visible.");
  await dialog()
    .getByLabel("Shipping instructions", { exact: true })
    .fill("Load upright and secure before departure.");
  await dialog()
    .getByLabel("Safety instructions", { exact: true })
    .fill("Wear safety shoes and gloves.");
  await dialog()
    .getByLabel("Minimum remaining shelf life (days)", { exact: true })
    .fill("30");
  await save("Save customer instructions", "instructions");
  await setup("Package types")
    .getByRole("button", { name: "Add", exact: true })
    .click();
  await dialog().getByLabel("Package code", { exact: true }).fill("BOX-01");
  await dialog()
    .getByLabel("Package name", { exact: true })
    .fill("Secured outbound container");
  await dialog().getByLabel("Length (cm)", { exact: true }).fill("60");
  await dialog().getByLabel("Width (cm)", { exact: true }).fill("50");
  await dialog().getByLabel("Height (cm)", { exact: true }).fill("40");
  await dialog().getByLabel("Tare (kg)", { exact: true }).fill("1");
  await dialog()
    .getByLabel("Maximum gross weight (kg)", { exact: true })
    .fill("100");
  await dialog()
    .getByLabel("Require temperature-device serial reference", { exact: true })
    .check();
  const pack = await save("Save package type", "packages");
  const operators = await (
    await page.request.get(
      `/api/operations/dispatch/operators?plantId=${plantId}`,
    )
  ).json();
  await tab("Planning");
  await page
    .getByRole("button", { name: "Plan dispatch", exact: true })
    .click();
  await dialog()
    .getByLabel("Dispatch reference", { exact: true })
    .fill(`SHIP-${suffix}`);
  await dialog()
    .getByLabel("Dispatch area", { exact: true })
    .selectOption(area.resultId);
  await dialog()
    .getByLabel("Assigned operator", { exact: true })
    .selectOption(operators[0].id);
  await dialog()
    .getByLabel("Dispatch route", { exact: true })
    .fill("Pune distribution route");
  await dialog().getByLabel("High priority", { exact: true }).check();
  await dialog()
    .getByLabel("Delivery orders to dispatch", { exact: true })
    .selectOption([orderA.id, orderB.id]);
  const plan = await save("Save dispatch plan", "runs");
  const runId = plan.resultId;
  await page
    .getByRole("button", { name: "Start picking", exact: true })
    .click();
  await dialog()
    .getByLabel("I confirm the action and applicable instructions", {
      exact: true,
    })
    .check();
  await save("Confirm picking start", `runs/${runId}/state`);
  let d = await (
    await page.request.get(
      `/api/operations/dispatch/runs/${runId}?plantId=${plantId}`,
    )
  ).json();
  const lineA = d.lines.find((l: any) => l.deliveryOrderId === orderA.id),
    lineB = d.lines.find((l: any) => l.deliveryOrderId === orderB.id);
  const pallets = await (
    await page.request.get(`/api/operations/pallets?plantId=${plantId}`)
  ).json();
  const source = pallets.find((p: any) => p.id === confirmation.palletId);
  await page.getByRole("button", { name: "Confirm pick", exact: true }).click();
  await dialog()
    .getByLabel("Delivery line 1", { exact: true })
    .selectOption(lineA.id);
  await dialog()
    .getByLabel("Source pallet 1", { exact: true })
    .selectOption(source.id);
  await dialog().getByLabel("Scan source bin 1", { exact: true }).fill("SRC");
  await dialog()
    .getByLabel("Scan source pallet 1", { exact: true })
    .fill(source.code);
  await dialog().getByLabel("Picked quantity 1", { exact: true }).fill("10");
  await dialog()
    .getByRole("button", { name: "Add grouped pick", exact: true })
    .click();
  await dialog()
    .getByLabel("Delivery line 2", { exact: true })
    .selectOption(lineB.id);
  await dialog()
    .getByLabel("Source pallet 2", { exact: true })
    .selectOption(source.id);
  await dialog().getByLabel("Scan source bin 2", { exact: true }).fill("SRC");
  await dialog()
    .getByLabel("Scan source pallet 2", { exact: true })
    .fill(source.code);
  await dialog().getByLabel("Picked quantity 2", { exact: true }).fill("5");
  await dialog()
    .getByLabel("I have checked the picking instructions", { exact: true })
    .check();
  await dialog()
    .getByLabel("PPE and material safety requirements confirmed", {
      exact: true,
    })
    .check();
  await save("Confirm scanned picks", `runs/${runId}/picks`);
  for (const [button, confirm] of [
    ["Complete picking", "Confirm picking complete"],
    ["Start packing", "Confirm packing start"],
  ]) {
    await page.getByRole("button", { name: button, exact: true }).click();
    await dialog()
      .getByLabel("I confirm the action and applicable instructions", {
        exact: true,
      })
      .check();
    await save(confirm, `runs/${runId}/state`);
  }
  await page
    .getByRole("button", { name: "Open handling unit", exact: true })
    .click();
  await dialog()
    .getByLabel("Handling-unit package type", { exact: true })
    .selectOption(pack.resultId);
  await dialog()
    .getByLabel("Temperature-device serial", { exact: true })
    .fill("TEMP-VALIDATION-001");
  const unit = await save("Create handling unit", `runs/${runId}/units`);
  d = await (
    await page.request.get(
      `/api/operations/dispatch/runs/${runId}?plantId=${plantId}`,
    )
  ).json();
  const hu = d.units.find((u: any) => u.id === unit.resultId);
  await page
    .getByRole("button", { name: "Pack allocations", exact: true })
    .click();
  for (const pick of d.picks) {
    const code = d.pallets.find((p: any) => p.id === pick.pickedPalletId).code;
    await dialog()
      .locator(".mfg-editor-row")
      .filter({ hasText: code })
      .getByRole("checkbox")
      .check();
    await dialog()
      .getByLabel(`Scan picked label ${code}`, { exact: true })
      .fill(code);
  }
  await dialog()
    .getByLabel("Packing instructions checked and selected products verified", {
      exact: true,
    })
    .check();
  await save("Confirm checked packing", `units/${hu.id}/pack`);
  await page.getByRole("button", { name: "Close unit", exact: true }).click();
  await dialog()
    .getByLabel("Scan handling-unit code", { exact: true })
    .fill(hu.code);
  await dialog()
    .getByLabel("Actual gross weight (kg)", { exact: true })
    .fill("17");
  await save("Confirm close", `units/${hu.id}/state`);
  await page
    .getByRole("button", { name: "Complete packing", exact: true })
    .click();
  await dialog()
    .getByLabel("I confirm the action and applicable instructions", {
      exact: true,
    })
    .check();
  await save("Confirm packing complete", `runs/${runId}/state`);
  await tab("Load & issue");
  await page
    .getByRole("button", { name: "Start truck loading", exact: true })
    .click();
  await dialog()
    .getByLabel("Loading truck registration", { exact: true })
    .fill("MH12-DE-2048");
  await dialog()
    .getByLabel("Carrier name", { exact: true })
    .fill("Green Ink Demonstration Transport");
  await dialog()
    .getByLabel("Shipping instructions checked before loading", { exact: true })
    .check();
  await save("Confirm truck loading", `runs/${runId}/truck`);
  await page
    .getByRole("button", { name: "Scan and load", exact: true })
    .click();
  await dialog()
    .getByLabel("Scan handling-unit code", { exact: true })
    .fill(hu.code);
  await save("Confirm load", `units/${hu.id}/state`);
  await save("Complete truck loading", `runs/${runId}/loading-complete`);
  await page.evaluate(() => window.scrollTo(0, 0));
  await page.waitForTimeout(150);
  await page.screenshot({
    path: "/workspace/GreenInk-Dispatch-Preview.png",
    fullPage: true,
  });
  await page
    .getByRole("button", { name: "Post local goods issue", exact: true })
    .click();
  await dialog()
    .getByLabel("I confirm the loaded stock is being dispatched", {
      exact: true,
    })
    .check();
  await save("Confirm local goods issue", `runs/${runId}/goods-issue`);
  await expect(page.locator(".dispatch-summary")).toContainText("Issued");
  let report = await (
    await page.request.get(`/api/operations/reports?plantId=${plantId}`)
  ).json();
  expect(report.byUom.find((r: any) => r.uom === "kg").shipped).toBe(15);
  await tab("Handling units");
  await page
    .getByRole("button", { name: "Label preview", exact: true })
    .click();
  await expect(dialog().locator(".mfg-label svg")).toBeVisible();
  await expect(dialog()).toContainText(hu.code);
  await dialog()
    .getByRole("button", { name: "Close dialog", exact: true })
    .click();
  await tab("Load & issue");
  await page
    .getByRole("button", { name: "Reverse local goods issue", exact: true })
    .click();
  await dialog()
    .getByLabel("I confirm all stock has physically returned", { exact: true })
    .check();
  await dialog()
    .getByLabel("Goods-issue reversal reason", { exact: true })
    .fill("Validation: customer cancelled and all stock returned.");
  await save("Confirm local reversal", `runs/${runId}/reverse`);
  await expect(page.locator(".dispatch-summary")).toContainText("Reversed");
  report = await (
    await page.request.get(`/api/operations/reports?plantId=${plantId}`)
  ).json();
  expect(report.byUom.find((r: any) => r.uom === "kg").shipped).toBe(0);
  await page.getByRole("button", { name: "History", exact: true }).click();
  await expect(dialog()).toContainText("Goods Issue Reversed");
  const download = page.waitForEvent("download");
  await dialog()
    .getByRole("button", { name: "Export shown activity", exact: true })
    .click();
  expect((await download).suggestedFilename()).toBe(
    "GreenInk-dispatch-history.csv",
  );
  await dialog()
    .getByRole("button", { name: "Close dialog", exact: true })
    .click();
  await page.setViewportSize({ width: 390, height: 844 });
  await tab("Handling units");
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= innerWidth,
    ),
  ).toBeTruthy();
  await page
    .getByRole("button", { name: "Use dark theme", exact: true })
    .click();
  await page.screenshot({
    path: "/tmp/greenink-dispatch-mobile.png",
    fullPage: true,
  });
  await page
    .getByRole("button", { name: "Label preview", exact: true })
    .click();
  expect(
    await dialog().evaluate(
      (el) => el.getBoundingClientRect().width <= innerWidth,
    ),
  ).toBeTruthy();
  await dialog()
    .getByRole("button", { name: "Close dialog", exact: true })
    .click();
  const stock = await (
    await page.request.get(`/api/operations/finished-goods?plantId=${plantId}`)
  ).json();
  expect(stock.reduce((s: number, p: any) => s + p.quantity, 0)).toBe(100);
  expect(stock.every((p: any) => p.qualityStatus === "Hold")).toBeTruthy();
  expect(stock.reduce((s: number, p: any) => s + p.reservedQuantity, 0)).toBe(
    0,
  );
});
