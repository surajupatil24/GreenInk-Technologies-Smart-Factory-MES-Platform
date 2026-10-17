import { expect, test } from "@playwright/test";
import { randomBytes } from "node:crypto";
const email = process.env.GREENINK_E2E_EMAIL,
  password = process.env.GREENINK_E2E_PASSWORD;
test.skip(!email || !password, "Requires isolated development administrator.");
test("guided production, laboratory review and finished packing form a persisted lifecycle", async ({
  page,
}) => {
  test.setTimeout(150000);
  page.setDefaultTimeout(10000);
  await page.setViewportSize({ width: 1600, height: 1000 });
  const suffix = randomBytes(4).toString("hex").toUpperCase(),
    headers = { "X-GreenInk-Request": "1" };
  await page.goto("/");
  await page.getByLabel("Email address").fill(email!);
  await page.getByLabel("Password", { exact: true }).fill(password!);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Command Center", exact: true }),
  ).toBeVisible();
  async function post(path: string, data: object) {
    const r = await page.request.post("/api/" + path, { headers, data });
    expect(r.ok(), await r.text()).toBeTruthy();
    return r.json();
  }
  const tenant = await post("tenants", {
    name: `Manufacturing ${suffix}`,
    code: `MT-${suffix}`,
  });
  const plant = await post("plants", {
    name: `Manufacturing Plant ${suffix}`,
    code: `MP-${suffix}`,
    tenantId: tenant.id,
  });
  const plantId = plant.id;
  const raw = await post("operations/materials", {
      plantId,
      code: `RM-${suffix}`,
      name: "Validation raw material",
      kind: "Raw",
      uom: "kg",
    }),
    fg = await post("operations/materials", {
      plantId,
      code: `FG-${suffix}`,
      name: "Finished blend",
      kind: "Finished",
      uom: "kg",
    });
  const supplier = await post("operations/partners", {
    plantId,
    code: `SUP-${suffix}`,
    name: "Validation supplier",
    kind: "Supplier",
  });
  await post("operations/bins", {
    plantId,
    code: "FG-HOLD",
    name: "Finished inspection bay",
    warehouse: "MAIN",
  });
  await post("operations/bins", {
    plantId,
    code: "FG-RACK",
    name: "Released storage rack",
    warehouse: "MAIN",
  });
  const po = await post("operations/purchase-orders", {
    plantId,
    number: `PO-${suffix}`,
    supplierId: supplier.id,
    materialId: raw.id,
    quantity: 100,
  });
  const receipt = await post("operations/receipts", {
    plantId,
    purchaseOrderId: po.id,
    quantity: 100,
    supplierBatch: "SUP-LOT",
    batchNumber: `RAW-${suffix}`,
    commandId: crypto.randomUUID(),
  });
  await post("operations/quality-inspections", {
    plantId,
    batchId: receipt.batchId,
    decision: "Released",
    notes: "Incoming inspection accepted",
    commandId: crypto.randomUUID(),
  });
  const order = await post("operations/production-orders", {
    plantId,
    number: `PR-${suffix}`,
    materialId: fg.id,
    plannedQuantity: 10,
    requirements: [{ materialId: raw.id, quantity: 10 }],
  });
  await post(`operations/production-orders/${order.id}/release`, {
    plantId,
    commandId: crypto.randomUUID(),
  });
  const reserve = await post("operations/reservations", {
    plantId,
    productionOrderId: order.id,
    palletId: receipt.palletId,
    quantity: 10,
    commandId: crypto.randomUUID(),
  });
  await post(`operations/reservations/${reserve.id}/issue`, {
    plantId,
    commandId: crypto.randomUUID(),
  });
  await page.reload();
  await page.locator("#tenant").selectOption(tenant.id);
  await page.locator("#plant").selectOption(plantId);
  async function nav(name: string) {
    if (page.viewportSize()!.width < 760)
      await page
        .getByRole("button", { name: "Open navigation", exact: true })
        .click();
    await page.getByRole("link", { name, exact: true }).click();
    await expect(
      page.getByRole("heading", { name, exact: true }),
    ).toBeVisible();
  }
  async function tab(name: string) {
    await page.getByRole("tab", { name, exact: true }).click();
  }
  const dialog = () => page.getByRole("dialog");
  async function save(button: string, path: string) {
    const done = page.waitForResponse(
      (r) =>
        r.request().method() === "POST" &&
        new URL(r.url()).pathname === "/api/operations/" + path,
    );
    await page.getByRole("button", { name: button, exact: true }).click();
    const r = await done;
    expect(r.ok(), await r.text()).toBeTruthy();
    return r.json();
  }
  await nav("Quality");
  await tab("Specifications");
  await page
    .getByRole("button", { name: "Create specification revision", exact: true })
    .click();
  await dialog()
    .getByLabel("Specification material", { exact: true })
    .selectOption(fg.id);
  await dialog()
    .getByLabel("Specification name", { exact: true })
    .fill("Finished blend laboratory limits");
  await dialog().getByLabel("Test code 1", { exact: true }).fill("PH");
  await dialog().getByLabel("Test name 1", { exact: true }).fill("Acidity");
  await dialog().getByLabel("Unit 1", { exact: true }).fill("pH");
  await dialog().getByLabel("Lower limit 1", { exact: true }).fill("6");
  await dialog().getByLabel("Upper limit 1", { exact: true }).fill("8");
  const spec = await save(
    "Save specification draft",
    "manufacturing/specifications",
  );
  await save(
    "Publish specification",
    `manufacturing/specifications/${spec.resultId}/publish`,
  );
  await nav("Production");
  await tab("Resources");
  await page.getByRole("button", { name: "Add resource", exact: true }).click();
  await dialog().getByLabel("Resource code", { exact: true }).fill("MIX-01");
  await dialog()
    .getByLabel("Resource name", { exact: true })
    .fill("Blending vessel 01");
  const resource = await save(
    "Save production resource",
    "manufacturing/resources",
  );
  await tab("SOP library");
  await page
    .getByRole("button", { name: "Create SOP revision", exact: true })
    .click();
  await dialog().getByLabel("SOP code", { exact: true }).fill("BLEND");
  await dialog()
    .getByLabel("SOP name", { exact: true })
    .fill("Controlled blend and finish");
  await dialog()
    .getByLabel("Finished material for SOP", { exact: true })
    .selectOption(fg.id);
  await dialog()
    .getByLabel("Safety precautions and production notes", { exact: true })
    .fill("Wear gloves and goggles. Verify guards before starting.");
  await dialog().getByLabel("Phase name 1", { exact: true }).fill("Blending");
  await dialog()
    .getByLabel("Instructions 1", { exact: true })
    .fill("Mix with the vessel sealed and record temperature.");
  await dialog()
    .getByLabel("Checklist prompts 1", { exact: true })
    .fill("Line clearance completed\nPPE confirmed");
  await dialog()
    .getByLabel("Require passed laboratory sample for phase 1", { exact: true })
    .check();
  await dialog()
    .getByRole("button", { name: "Add parameter to phase 1", exact: true })
    .click();
  await dialog().getByLabel("Parameter code 1.1", { exact: true }).fill("TEMP");
  await dialog()
    .getByLabel("Parameter name 1.1", { exact: true })
    .fill("Blend temperature");
  await dialog().getByLabel("Parameter unit 1.1", { exact: true }).fill("C");
  await dialog()
    .getByLabel("Parameter minimum 1.1", { exact: true })
    .fill("20");
  await dialog()
    .getByLabel("Parameter maximum 1.1", { exact: true })
    .fill("30");
  await dialog()
    .getByRole("button", { name: "Add phase", exact: true })
    .click();
  await dialog().getByLabel("Phase name 2", { exact: true }).fill("Discharge");
  await dialog()
    .getByLabel("Instructions 2", { exact: true })
    .fill("Verify packaging and discharge the blend.");
  await dialog()
    .getByLabel("Checklist prompts 2", { exact: true })
    .fill("Packaging verified");
  const recipe = await save("Save SOP draft", "manufacturing/recipes");
  await save("Publish SOP", `manufacturing/recipes/${recipe.resultId}/publish`);
  await tab("Execution");
  await page
    .getByRole("button", { name: "Schedule execution", exact: true })
    .click();
  await dialog()
    .getByLabel("Released order for execution", { exact: true })
    .selectOption(order.id);
  await dialog()
    .getByLabel("Published SOP revision", { exact: true })
    .selectOption(recipe.resultId);
  await dialog()
    .getByLabel("Execution resource", { exact: true })
    .selectOption(resource.resultId);
  const run = await save("Schedule run", "manufacturing/runs");
  await expect(page.locator(".mfg-detail")).toContainText(order.number);
  await page
    .getByRole("button", { name: "Start production", exact: true })
    .click();
  await dialog()
    .getByLabel(
      "I confirm the production notes, safety precautions and PPE requirements",
      { exact: true },
    )
    .check();
  await save("Confirm and start", `manufacturing/runs/${run.resultId}/state`);
  let details = await (
    await page.request.get(
      `/api/operations/manufacturing/runs/${run.resultId}?plantId=${plantId}`,
    )
  ).json();
  const [phase1, phase2] = details.phases;
  await save("Start phase 1", `manufacturing/phases/${phase1.id}/start`);
  await page
    .getByRole("button", { name: "Record phase evidence", exact: true })
    .click();
  await dialog()
    .getByLabel("Line clearance completed", { exact: true })
    .check();
  await dialog().getByLabel("PPE confirmed", { exact: true }).check();
  await dialog()
    .getByLabel("Blend temperature (C)", { exact: true })
    .fill("25");
  await save(
    "Save phase evidence",
    `manufacturing/phases/${phase1.id}/evidence`,
  );
  await page
    .getByRole("button", { name: "Collect phase sample", exact: true })
    .click();
  await dialog()
    .getByLabel("Collection notes", { exact: true })
    .fill("In-process blend sample");
  const inProcess = await save("Collect sample", "manufacturing/samples");
  async function lab(sampleId: string) {
    await nav("Quality");
    const list = await (
      await page.request.get(
        `/api/operations/manufacturing/samples?plantId=${plantId}`,
      )
    ).json();
    const item = list.items.find((s: any) => s.id === sampleId);
    await page
      .locator(".mfg-list-item")
      .filter({ hasText: item.number })
      .click();
    await save("Hand over sample", `manufacturing/samples/${sampleId}/state`);
    await save(
      "Receive at laboratory",
      `manufacturing/samples/${sampleId}/state`,
    );
    await save(
      "Start laboratory testing",
      `manufacturing/samples/${sampleId}/state`,
    );
    await page
      .getByRole("button", { name: "Record test result", exact: true })
      .click();
    await dialog().getByLabel("Measured result", { exact: true }).fill("7");
    await dialog()
      .getByLabel("Result notes", { exact: true })
      .fill("Measured within the published range");
    await save(
      "Save test result",
      `manufacturing/samples/${sampleId}/readings`,
    );
    await page
      .getByRole("button", { name: "Review sample", exact: true })
      .click();
    await dialog()
      .getByLabel("Laboratory decision", { exact: true })
      .selectOption("Passed");
    await dialog()
      .getByLabel("Review justification", { exact: true })
      .fill("All required tests accepted");
    await save(
      "Save laboratory review",
      `manufacturing/samples/${sampleId}/review`,
    );
    await expect(page.locator(".mfg-test-list")).toContainText("In spec");
  }
  await lab(inProcess.resultId);
  await page.screenshot({
    path: "/workspace/GreenInk-Quality-Preview.png",
    fullPage: true,
  });
  await nav("Production");
  await page
    .locator(".mfg-list-item")
    .filter({ hasText: order.number })
    .click();
  await save("Complete phase 1", `manufacturing/phases/${phase1.id}/complete`);
  await save("Start phase 2", `manufacturing/phases/${phase2.id}/start`);
  await page
    .getByRole("button", { name: "Record phase evidence", exact: true })
    .click();
  await dialog().getByLabel("Packaging verified", { exact: true }).check();
  await save(
    "Save phase evidence",
    `manufacturing/phases/${phase2.id}/evidence`,
  );
  await save("Complete phase 2", `manufacturing/phases/${phase2.id}/complete`);
  await page.screenshot({
    path: "/workspace/GreenInk-Production-Preview.png",
    fullPage: true,
  });
  await tab("Orders & output");
  const completion = page.locator("section.operation-form").filter({
    has: page.getByRole("heading", {
      name: "Complete production",
      exact: true,
    }),
  });
  await completion
    .getByLabel("Production order to complete", { exact: true })
    .selectOption(order.id);
  await completion
    .getByLabel("Good output quantity", { exact: true })
    .fill("10");
  await completion
    .getByLabel("Finished goods bin", { exact: true })
    .selectOption("FG-HOLD");
  await completion
    .getByLabel("Finished batch number", { exact: true })
    .fill(`FG-${suffix}`);
  await completion
    .getByLabel("Finished expiry date", { exact: true })
    .fill("2027-12-31");
  const confirmation = await save(
    "Complete production",
    `production-orders/${order.id}/complete`,
  );
  await nav("Quality");
  await page
    .getByRole("button", { name: "Collect batch sample", exact: true })
    .click();
  await dialog()
    .getByLabel("Batch to sample", { exact: true })
    .selectOption(confirmation.batchId);
  await dialog()
    .getByLabel("Published quality specification", { exact: true })
    .selectOption(spec.resultId);
  const finalSample = await save("Collect sample", "manufacturing/samples");
  await lab(finalSample.resultId);
  await tab("Batch decisions");
  const decision = page.locator("section.operation-form").filter({
    has: page.getByRole("heading", {
      name: "Record quality decision",
      exact: true,
    }),
  });
  await decision
    .getByLabel("Batch", { exact: true })
    .selectOption(confirmation.batchId);
  await decision
    .getByLabel("Decision", { exact: true })
    .selectOption("Released");
  await decision
    .getByLabel("Inspection notes", { exact: true })
    .fill("Released after reviewed batch tests");
  await save("Record quality decision", "quality-inspections");
  await nav("Finished Goods");
  const inventory = await (
    await page.request.get(`/api/operations/pallets?plantId=${plantId}`)
  ).json();
  const source = inventory.find((p: any) => p.id === confirmation.palletId);
  await page.getByRole("button", { name: "Pack pallet", exact: true }).click();
  await dialog()
    .getByLabel("Scan packing source pallet", { exact: true })
    .fill(source.code);
  await dialog().getByLabel("Pack size", { exact: true }).fill("2");
  await dialog().getByLabel("Whole pack count", { exact: true }).fill("3");
  await dialog().getByLabel("Loose quantity", { exact: true }).fill("1");
  await dialog()
    .getByLabel("Packaging reference", { exact: true })
    .fill("Seven kilograms in sealed food-grade bags");
  const packed = await save("Create packed pallet", "manufacturing/packs");
  const label = await (
    await page.request.get(
      `/api/operations/manufacturing/packs/${packed.resultId}/label?plantId=${plantId}`,
    )
  ).json();
  const card = page
    .locator(".mfg-stock-card")
    .filter({ hasText: label.palletCode });
  await card.getByRole("button", { name: "Put away", exact: true }).click();
  await dialog()
    .getByLabel("Scan finished pallet label", { exact: true })
    .fill(label.palletCode);
  await dialog()
    .getByLabel("Scan destination bin", { exact: true })
    .fill("FG-RACK");
  await save("Confirm finished putaway", "manufacturing/putaway");
  await expect(card).toContainText("FG-RACK");
  await page.screenshot({
    path: "/workspace/GreenInk-Finished-Goods-Preview.png",
    fullPage: true,
  });
  await tab("Packing history");
  await page
    .getByRole("button", { name: "Preview packed label", exact: true })
    .click();
  await expect(dialog().locator(".mfg-label svg")).toBeVisible();
  await expect(dialog()).toContainText(label.palletCode);
  await dialog()
    .getByRole("button", { name: "Close dialog", exact: true })
    .click();
  await page.setViewportSize({ width: 390, height: 844 });
  await tab("Stock");
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= innerWidth,
    ),
  ).toBeTruthy();
  await page
    .getByRole("button", { name: "Use dark theme", exact: true })
    .click();
  await page.screenshot({
    path: "/tmp/greenink-finished-mobile.png",
    fullPage: true,
  });
  await nav("Production");
  await page.locator(".mfg-list select").selectOption("All");
  await page
    .locator(".mfg-list-item")
    .filter({ hasText: order.number })
    .click();
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= innerWidth,
    ),
  ).toBeTruthy();
  await page.screenshot({
    path: "/tmp/greenink-production-mobile.png",
    fullPage: true,
  });
  const finalStock = await (
    await page.request.get(`/api/operations/finished-goods?plantId=${plantId}`)
  ).json();
  expect(finalStock.reduce((sum: number, p: any) => sum + p.quantity, 0)).toBe(
    10,
  );
  expect(
    finalStock.find((p: any) => p.code === label.palletCode).quantity,
  ).toBe(7);
  expect(
    finalStock.find((p: any) => p.code === label.palletCode).qualityStatus,
  ).toBe("Released");
});
