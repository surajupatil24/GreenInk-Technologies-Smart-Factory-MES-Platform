import { expect, test, type Locator } from "@playwright/test";
import { randomBytes } from "node:crypto";
const email = process.env.GREENINK_E2E_EMAIL;
const password = process.env.GREENINK_E2E_PASSWORD;
test.skip(
  !email || !password,
  "Set isolated development platform administrator credentials.",
);
test("plant lifecycle from raw receipt through production, quality, labels and dispatch", async ({
  page,
}) => {
  test.setTimeout(90000);
  page.setDefaultTimeout(10000);
  const suffix = randomBytes(4).toString("hex").toUpperCase();
  await page.goto("/");
  await page.getByLabel("Email address").fill(email!);
  await page.getByLabel("Password", { exact: true }).fill(password!);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Command Center", exact: true }),
  ).toBeVisible();
  const headers = { "X-GreenInk-Request": "1" };
  const tenantResponse = await page.request.post("/api/tenants", {
    headers,
    data: { name: `Operations ${suffix}`, code: `OT-${suffix}` },
  });
  expect(tenantResponse.ok()).toBeTruthy();
  const tenant = await tenantResponse.json();
  const plantResponse = await page.request.post("/api/plants", {
    headers,
    data: {
      name: `Operations Plant ${suffix}`,
      code: `OP-${suffix}`,
      tenantId: tenant.id,
    },
  });
  expect(plantResponse.ok()).toBeTruthy();
  const plant = await plantResponse.json();
  await page.reload();
  await page.locator("#tenant").selectOption(tenant.id);
  await page.locator("#plant").selectOption(plant.id);
  const section = (title: string) =>
    page
      .locator("section.operation-form")
      .filter({ has: page.getByRole("heading", { name: title, exact: true }) });
  async function save(title: string, path: string) {
    const responsePromise = page.waitForResponse(
      (r) =>
        r.request().method() === "POST" &&
        new URL(r.url()).pathname === `/api/operations/${path}`,
    );
    await section(title)
      .getByRole("button", { name: title, exact: true })
      .click();
    const response = await responsePromise;
    expect(response.ok(), await response.text()).toBeTruthy();
    return response.json();
  }
  async function fill(form: Locator, values: Record<string, string>) {
    for (const [label, value] of Object.entries(values))
      await form.getByLabel(label, { exact: true }).fill(value);
  }
  async function navigate(name: string) {
    await page.getByRole("link", { name, exact: true }).click();
    await expect(
      page.getByRole("heading", { name, exact: true }),
    ).toBeVisible();
    if (name === "Production")
      await page
        .getByRole("tab", { name: "Orders & output", exact: true })
        .click();
    if (name === "Quality")
      await page
        .getByRole("tab", { name: "Batch decisions", exact: true })
        .click();
    if (name === "Dispatch")
      await page
        .getByRole("tab", { name: "Orders & shipments", exact: true })
        .click();
    if (name === "Finished Goods")
      await page
        .getByRole("tab", { name: "Inventory records", exact: true })
        .click();
  }
  await navigate("Inbound");
  await page.getByRole("tab", { name: "Master data" }).click();
  let form = section("Create material");
  await fill(form, {
    "Material code": `RAW-${suffix}`,
    "Material name": "Operational raw material",
    "Unit of measure": "kg",
  });
  await form.getByLabel("Material kind", { exact: true }).selectOption("Raw");
  const raw = await save("Create material", "materials");
  await fill(form, {
    "Material code": `FG-${suffix}`,
    "Material name": "Operational finished material",
  });
  await form
    .getByLabel("Material kind", { exact: true })
    .selectOption("Finished");
  const finished = await save("Create material", "materials");
  form = section("Create partner");
  await fill(form, {
    "Partner code": `SUP-${suffix}`,
    "Partner name": "Lifecycle supplier",
  });
  await form
    .getByLabel("Partner kind", { exact: true })
    .selectOption("Supplier");
  const supplier = await save("Create partner", "partners");
  await fill(form, {
    "Partner code": `CUS-${suffix}`,
    "Partner name": "Lifecycle customer",
  });
  await form
    .getByLabel("Partner kind", { exact: true })
    .selectOption("Customer");
  const customer = await save("Create partner", "partners");
  form = section("Create bin");
  await fill(form, {
    "Bin code": `BIN-${suffix}`,
    "Bin name": "Operations storage",
    Warehouse: "Main warehouse",
  });
  const bin = await save("Create bin", "bins");
  await page.getByRole("tab", { name: "Receiving", exact: true }).click();
  form = section("Create purchase order");
  await fill(form, {
    "Purchase order number": `PO-${suffix}`,
    "Ordered quantity": "10",
  });
  await form.getByLabel("Supplier", { exact: true }).selectOption(supplier.id);
  await form.getByLabel("Raw material", { exact: true }).selectOption(raw.id);
  const po = await save("Create purchase order", "purchase-orders");
  form = section("Post goods receipt");
  await form.getByLabel("Purchase order", { exact: true }).selectOption(po.id);
  await fill(form, {
    "Received quantity": "10",
    "Supplier batch": `SUPB-${suffix}`,
    "New batch number": `RAWB-${suffix}`,
    "Expiry date": "2030-12-31",
  });
  const receipt = await save("Post goods receipt", "receipts");
  await page.evaluate(() => window.scrollTo(0, 0));
  await page.screenshot({
    path: "/tmp/greenink-operations-inbound.png",
    fullPage: true,
  });
  await navigate("Quality");
  form = section("Record quality decision");
  await form.getByLabel("Batch", { exact: true }).selectOption(receipt.batchId);
  await form.getByLabel("Decision", { exact: true }).selectOption("Released");
  await fill(form, { "Inspection notes": "Accepted after inspection" });
  await save("Record quality decision", "quality-inspections");
  await navigate("Inventory");
  form = section("Transfer pallet");
  await form
    .getByLabel("Pallet", { exact: true })
    .selectOption(receipt.palletId);
  await form
    .getByLabel("Destination bin", { exact: true })
    .selectOption(bin.code);
  await save("Transfer pallet", "transfers");
  await page
    .getByLabel("Material for FEFO", { exact: true })
    .selectOption(raw.id);
  await page
    .getByRole("button", { name: "Find FEFO stock", exact: true })
    .click();
  await expect(page.locator(".operation-result")).toContainText(raw.code);
  await navigate("Production");
  form = section("Create production order");
  await fill(form, {
    "Production order number": `PROD-${suffix}`,
    "Planned output quantity": "8",
    "Requirement 1 quantity": "10",
  });
  await form
    .getByLabel("Finished material", { exact: true })
    .selectOption(finished.id);
  await form
    .getByLabel("Requirement 1 material", { exact: true })
    .selectOption(raw.id);
  const order = await save("Create production order", "production-orders");
  form = section("Release order");
  await form
    .getByLabel("Draft production order", { exact: true })
    .selectOption(order.id);
  await save("Release order", `production-orders/${order.id}/release`);
  form = section("Complete production");
  await form
    .getByLabel("Production order to complete", { exact: true })
    .selectOption(order.id);
  await fill(form, { "Good output quantity": "8", "Scrap quantity": "0" });
  await expect(form.locator(".saved-bom")).toContainText(
    "Planned output: 8 kg",
  );
  await expect(form.locator(".saved-bom")).toContainText("Still to issue10 kg");
  await expect(
    form.getByRole("button", { name: "Complete production", exact: true }),
  ).toBeDisabled();
  await navigate("Material Preparation");
  await page
    .getByRole("button", { name: "Quick reservations", exact: true })
    .click();
  form = section("Reserve material");
  await form
    .getByLabel("Released production order", { exact: true })
    .selectOption(order.id);
  await form
    .getByLabel("Source pallet", { exact: true })
    .selectOption(receipt.palletId);
  await fill(form, { "Reservation quantity": "10" });
  const reservation = await save("Reserve material", "reservations");
  await expect(form.locator(".saved-bom")).toContainText(
    "Issue the existing reservations",
  );
  await expect(form.locator(".saved-bom")).toContainText(
    "Reserved, awaiting issue10 kg",
  );
  form = section("Issue reservation");
  await form
    .getByLabel("Reservation to issue", { exact: true })
    .selectOption(reservation.id);
  await save("Issue reservation", `reservations/${reservation.id}/issue`);
  await navigate("Production");
  form = section("Record downtime");
  await form
    .getByLabel("Production order", { exact: true })
    .selectOption(order.id);
  await fill(form, {
    "Downtime minutes": "5",
    "Downtime reason": "Planned cleaning",
  });
  await save("Record downtime", "downtime");
  form = section("Complete production");
  await form
    .getByLabel("Production order to complete", { exact: true })
    .selectOption(order.id);
  await expect(form.locator(".saved-bom")).toContainText(
    "Material requirement fully issued",
  );
  await fill(form, { "Good output quantity": "5", "Scrap quantity": "0" });
  await expect(
    form.getByRole("button", { name: "Complete production", exact: true }),
  ).toBeDisabled();
  await expect(form).toContainText("Partial completion is not supported");
  await fill(form, { "Good output quantity": "8", "Scrap quantity": "0" });
  await form
    .getByLabel("Finished goods bin", { exact: true })
    .selectOption(bin.code);
  const confirmation = await save(
    "Complete production",
    `production-orders/${order.id}/complete`,
  );
  await page.evaluate(() => window.scrollTo(0, 0));
  await page.screenshot({
    path: "/tmp/greenink-operations-production.png",
    fullPage: true,
  });
  await navigate("Quality");
  form = section("Record quality decision");
  await form
    .getByLabel("Batch", { exact: true })
    .selectOption(confirmation.batchId);
  await form.getByLabel("Decision", { exact: true }).selectOption("Released");
  await fill(form, { "Inspection notes": "Finished output accepted" });
  await save("Record quality decision", "quality-inspections");
  await navigate("Finished Goods");
  await expect(page.locator("tbody")).toContainText(finished.code);
  await navigate("Label Studio");
  form = section("Create label template");
  await fill(form, { "Template name": `LABEL-${suffix}` });
  const template = await save("Create label template", "label-templates");
  form = section("Queue local label");
  await form
    .getByLabel("Pallet to label", { exact: true })
    .selectOption(confirmation.palletId);
  await form
    .getByLabel("Label template", { exact: true })
    .selectOption(template.id);
  const job = await save("Queue local label", "print-jobs");
  await page
    .getByLabel("Queued print job", { exact: true })
    .selectOption(job.id);
  await expect(page.locator(".label-preview")).toContainText(finished.name);
  const previewResponse = await page.request.get(
    `/api/operations/print-jobs/${job.id}/preview?plantId=${plant.id}`,
  );
  expect(previewResponse.ok()).toBeTruthy();
  expect(await previewResponse.json()).toMatchObject({
    snapshotAvailable: true,
    physicalPrint: false,
  });
  const qr = page.getByRole("img", { name: "Pallet QR code" });
  await expect(qr).toBeVisible();
  await expect
    .poll(() =>
      qr.evaluate(
        (image: HTMLImageElement) => image.complete && image.naturalWidth > 0,
      ),
    )
    .toBeTruthy();
  await navigate("Dispatch");
  form = section("Create delivery order");
  await fill(form, {
    "Delivery order number": `DO-${suffix}`,
    "Delivery quantity": "8",
  });
  await form.getByLabel("Customer", { exact: true }).selectOption(customer.id);
  await form
    .getByLabel("Finished material", { exact: true })
    .selectOption(finished.id);
  const delivery = await save("Create delivery order", "delivery-orders");
  form = section("Confirm shipment");
  await form
    .getByLabel("Open delivery order", { exact: true })
    .selectOption(delivery.id);
  await form
    .getByLabel("Finished pallet", { exact: true })
    .selectOption(confirmation.palletId);
  await fill(form, {
    "Shipment quantity": "8",
    "Truck registration": `TRUCK-${suffix}`,
  });
  await save("Confirm shipment", "shipments");
  await navigate("Traceability");
  await page
    .getByLabel("Trace batch", { exact: true })
    .selectOption(confirmation.batchId);
  await expect(
    page.getByRole("heading", { name: "Source Batches", exact: true }),
  ).toBeVisible();
  await expect(page.locator("#main")).toContainText(`RAWB-${suffix}`);
  await expect(page.locator("#main")).toContainText(`TRUCK-${suffix}`);
  const traceResponse = await page.request.get(
    `/api/operations/traceability?plantId=${plant.id}&batchId=${confirmation.batchId}`,
  );
  expect(traceResponse.ok()).toBeTruthy();
  expect(await traceResponse.json()).toMatchObject({
    isComplete: true,
    collectionsTruncated: false,
    graphDepthLimitReached: false,
  });
  await navigate("Integration Hub");
  form = section("Register local message");
  await fill(form, {
    "External message identifier": `MSG-${suffix}`,
    "Message kind": "MaterialMaster",
    "JSON payload": '{"material":"test"}',
  });
  const message = await save("Register local message", "integration-messages");
  form = section("Validate local message");
  await form
    .getByLabel("Integration message", { exact: true })
    .selectOption(message.id);
  await save(
    "Validate local message",
    `integration-messages/${message.id}/process`,
  );
  await expect(page.locator("tbody")).toContainText("Validated");
  await navigate("Reports & Analytics");
  await expect(
    page.getByRole("heading", { name: "Operational aggregates", exact: true }),
  ).toBeVisible();
  const report = await page.request.get(
    `/api/operations/reports?plantId=${plant.id}`,
  );
  expect(report.ok()).toBeTruthy();
  const result = await report.json();
  expect(
    result.byUom.find((row: { uom: string }) => row.uom === "kg"),
  ).toMatchObject({ receipts: 10, consumed: 10, produced: 8, shipped: 8 });
  expect(result.downtimeMinutes).toBe(5);
  await page.screenshot({
    path: "/tmp/greenink-operations-reports.png",
    fullPage: true,
  });
  await navigate("Command Center");
  const summary = page.locator("section.operational-summary");
  await expect(
    summary.getByRole("heading", { name: "Plant operations", exact: true }),
  ).toBeVisible();
  await expect(summary).toContainText("Downtime minutes");
  await expect(
    summary
      .locator(".summary-metrics>div")
      .filter({ hasText: "Downtime minutes" })
      .locator("strong"),
  ).toHaveText("5");
  const viewerEmail = `viewer-${suffix}@example.invalid`;
  const viewerPassword = `V!${randomBytes(18).toString("base64url")}x2`;
  const viewer = await page.request.post("/api/users", {
    headers,
    data: {
      email: viewerEmail,
      displayName: "Operations viewer",
      password: viewerPassword,
      role: "Viewer",
      tenantId: tenant.id,
      plantIds: [plant.id],
    },
  });
  expect(viewer.ok()).toBeTruthy();
  await page.getByRole("button", { name: "Sign out", exact: true }).click();
  await page.getByLabel("Email address").fill(viewerEmail);
  await page.getByLabel("Password", { exact: true }).fill(viewerPassword);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Command Center", exact: true }),
  ).toBeVisible();
  await page.locator("#plant").selectOption(plant.id);
  await navigate("Inbound");
  await expect(
    section("Create purchase order").getByRole("button", {
      name: "Create purchase order",
      exact: true,
    }),
  ).toBeDisabled();
  await navigate("Quality");
  await expect(
    section("Record quality decision").getByRole("button", {
      name: "Record quality decision",
      exact: true,
    }),
  ).toBeDisabled();
  await page.getByRole("button", { name: "Sign out", exact: true }).click();
});
