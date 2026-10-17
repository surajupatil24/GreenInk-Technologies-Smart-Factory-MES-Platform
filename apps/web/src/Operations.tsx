import { PreparationWorkspace } from "./PreparationWorkspace";
import { InboundDesk } from "./InboundDesk";
import { useRef, useState, type FormEvent, type ReactNode } from "react";
import { useQueries, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link } from "react-router-dom";
type Row = Record<string, unknown> & { id: string };
type Option = { value: string; label: string };
type Field = {
  key: string;
  label: string;
  type?: "number" | "date" | "textarea";
  options?: Option[];
  required?: boolean;
  value?: string;
  signed?: boolean;
  integer?: boolean;
  hint?: string;
};
type FormProps = {
  title: string;
  endpoint: string | ((data: Record<string, unknown>) => string);
  plantId: string;
  fields: Field[];
  disabled?: boolean;
  children?: ReactNode;
  extra?: () => Record<string, unknown>;
  note?: string;
  onValuesChange?: (values: Record<string, string>) => void;
  blockedReason?: string;
};
export async function operationApi<T>(
  endpoint: string,
  data?: unknown,
): Promise<T> {
  const response = await fetch(`/api/operations/${endpoint}`, {
    credentials: "include",
    method: data ? "POST" : "GET",
    headers: data
      ? { "Content-Type": "application/json", "X-GreenInk-Request": "1" }
      : undefined,
    body: data ? JSON.stringify(data) : undefined,
  });
  if (!response.ok) {
    let message = `Operation failed (${response.status}).`;
    try {
      const body = await response.json();
      message = body.detail || body.message || body.title || message;
      if (body.errors) message = Object.values(body.errors).flat().join(" ");
    } catch {}
    throw new Error(message);
  }
  return response.status === 204 ? (undefined as T) : response.json();
}
function CommandForm({
  title,
  endpoint,
  plantId,
  fields,
  disabled,
  children,
  extra,
  note,
  onValuesChange,
  blockedReason,
}: FormProps) {
  const client = useQueryClient();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const retry = useRef<{ signature: string; id: string } | null>(null);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setSuccess("");
    const form = new FormData(event.currentTarget);
    const data: Record<string, unknown> = { plantId };
    for (const field of fields) {
      const value = String(form.get(field.key) || "");
      data[field.key] =
        field.type === "number"
          ? Number(value)
          : field.type === "date"
            ? value
              ? new Date(`${value}T00:00:00Z`).toISOString()
              : null
            : value;
    }
    Object.assign(data, extra?.());
    const signature = JSON.stringify(data);
    if (!retry.current || retry.current.signature !== signature)
      retry.current = { signature, id: crypto.randomUUID() };
    data.commandId = retry.current.id;
    setBusy(true);
    try {
      await operationApi(
        typeof endpoint === "function" ? endpoint(data) : endpoint,
        data,
      );
      retry.current = null;
      setSuccess("Saved successfully. Lists refreshed.");
      await client.invalidateQueries({ queryKey: ["operations"] });
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <section className="card operation-form">
      <h2>{title}</h2>
      {note && <p className="form-note">{note}</p>}
      <form
        onSubmit={submit}
        onChange={(event) => {
          const values = new FormData(event.currentTarget);
          onValuesChange?.(
            Object.fromEntries(
              fields.map((f) => [f.key, String(values.get(f.key) ?? "")]),
            ),
          );
        }}
      >
        <fieldset className="operation-fields" disabled={disabled || busy}>
          {fields.map((f) => (
            <label key={f.key}>
              {f.label}
              {f.options ? (
                <select
                  name={f.key}
                  aria-label={f.label}
                  required={f.required !== false}
                  defaultValue={f.value || ""}
                >
                  <option value="">Select {f.label.toLowerCase()}</option>
                  {f.options.map((o) => (
                    <option key={o.value} value={o.value}>
                      {o.label}
                    </option>
                  ))}
                </select>
              ) : f.type === "textarea" ? (
                <textarea
                  name={f.key}
                  aria-label={f.label}
                  required={f.required !== false}
                  defaultValue={f.value}
                  rows={4}
                  maxLength={
                    f.key === "content"
                      ? 4000
                      : f.key === "notes"
                        ? 2000
                        : 32768
                  }
                />
              ) : (
                <input
                  name={f.key}
                  aria-label={f.label}
                  type={
                    f.type === "number"
                      ? "number"
                      : f.type === "date"
                        ? "date"
                        : "text"
                  }
                  step={
                    f.type === "number"
                      ? f.integer
                        ? "1"
                        : "0.000001"
                      : undefined
                  }
                  min={
                    f.type === "number"
                      ? f.signed
                        ? undefined
                        : f.required === false
                          ? 0
                          : f.integer
                            ? 1
                            : "0.000001"
                      : undefined
                  }
                  max={f.type === "number" ? 1000000000 : undefined}
                  maxLength={
                    ["code", "number", "batchNumber"].includes(f.key)
                      ? 32
                      : f.key === "uom"
                        ? 16
                        : 160
                  }
                  required={f.required !== false}
                  defaultValue={f.value}
                />
              )}
              {f.hint && <small>{f.hint}</small>}
            </label>
          ))}
          {children}
        </fieldset>
        {disabled && (
          <p className="form-note">
            Your role cannot perform this action. Contact an authorized
            administrator or manager.
          </p>
        )}
        {error && (
          <p role="alert" className="error">
            {error}
          </p>
        )}
        {success && (
          <p role="status" className="success">
            {success}
          </p>
        )}
        {blockedReason && (
          <p className="form-note" role="status">
            {blockedReason}
          </p>
        )}
        <button
          className="primary"
          disabled={busy || disabled || !!blockedReason}
        >
          {busy ? "Saving…" : title}
        </button>
      </form>
    </section>
  );
}
const human = (key: string) =>
  key.replace(/([a-z])([A-Z])/g, "$1 $2").replace(/^./, (c) => c.toUpperCase());
function DataTable({
  title,
  rows,
  columns,
  resolve,
}: {
  title: string;
  rows: Row[];
  columns: string[];
  resolve?: (key: string, value: unknown) => string;
}) {
  return (
    <section className="card operations-table">
      <div className="card-heading">
        <h2>{title}</h2>
        <span>{rows.length} records</span>
      </div>
      {!rows.length ? (
        <p className="empty compact">
          No {title.toLowerCase()} for this plant yet.
        </p>
      ) : (
        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                {columns.map((c) => (
                  <th key={c}>{human(c)}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map((row, i) => (
                <tr key={row.id || i}>
                  {columns.map((c) => (
                    <td key={c}>
                      {resolve?.(c, row[c]) ??
                        (row[c] === null || row[c] === undefined
                          ? "—"
                          : typeof row[c] === "object"
                            ? JSON.stringify(row[c])
                            : String(row[c]))}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
const text = (key: string, label: string, value?: string): Field => ({
  key,
  label,
  value,
});
const num = (key: string, label: string, value?: string): Field => ({
  key,
  label,
  type: "number",
  value,
});
const choice = (key: string, label: string, options: Option[]): Field => ({
  key,
  label,
  options,
});
const fixed = (values: string[]) =>
  values.map((value) => ({ value, label: value }));
const routeResources: Record<string, string[]> = {
  inbound: ["purchase-orders", "receipts"],
  inventory: ["movements"],
  preparation: ["production-orders", "reservations", "consumptions"],
  production: ["production-orders", "confirmations", "downtimes"],
  quality: ["quality-inspections"],
  "finished-goods": ["finished-goods"],
  dispatch: ["delivery-orders", "shipments"],
  traceability: [],
  labels: ["label-templates", "print-jobs"],
  integrations: ["integration-messages"],
  reports: [],
};
export function Operations({
  module,
  name,
  plant,
  plants,
  role,
}: {
  module: string;
  name: string;
  plant: string;
  plants: { id: string; name: string }[];
  role: string;
}) {
  const [fallback, setFallback] = useState("");
  const selected = plant || fallback;
  return (
    <>
      <span className="eyebrow">
        {module === "preparation"
          ? "SHOP FLOOR · MATERIAL READINESS"
          : "PLANT OPERATIONS · FUNCTIONAL CORE"}
      </span>
      <h1>{name}</h1>
      <p className="intro">
        {module === "preparation"
          ? "Plan the shift, prepare every material and deliver with confidence."
          : "Persisted plant records and controlled transactions. Advanced workflows remain on the product roadmap."}
      </p>
      {!plant && (
        <label className="operation-context">
          Operating plant
          <select
            aria-label="Operating plant"
            value={fallback}
            onChange={(e) => setFallback(e.target.value)}
          >
            <option value="">Select a plant to continue</option>
            {plants.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
        </label>
      )}
      {selected ? (
        <PlantOperations
          key={`${module}:${selected}`}
          module={module}
          plantId={selected}
          role={role}
        />
      ) : (
        <div className="empty card">
          <h2>Select an operating plant</h2>
          <p>
            Choose an accessible plant above. Every transaction stays within
            that plant.
          </p>
        </div>
      )}
    </>
  );
}
type RequirementStatus = {
  materialId: string;
  code: string;
  name: string;
  uom: string;
  requiredQuantity: number;
  reservedQuantity: number;
  issuedQuantity: number;
  toReserve: number;
  toIssue: number;
  availableQuantity: number;
};
type OrderStatus = {
  orderId: string;
  number: string;
  status: string;
  plannedQuantity: number;
  completedQuantity: number;
  outputUom: string;
  canComplete: boolean;
  requirements: RequirementStatus[];
};
function ReadinessSummary({
  order,
  pending,
  error,
}: {
  order?: OrderStatus;
  pending: boolean;
  error?: string;
}) {
  if (pending) return <p role="status">Loading saved BOM…</p>;
  if (error)
    return (
      <p role="alert" className="error">
        {error}
      </p>
    );
  if (!order)
    return (
      <p className="form-note">
        Select a production order to view its saved requirements.
      </p>
    );
  return (
    <div className="saved-bom" aria-label={`Saved BOM for ${order.number}`}>
      <h3>Saved BOM · {order.number}</h3>
      <p>
        Planned output:{" "}
        <strong>
          {order.plannedQuantity} {order.outputUom}
        </strong>
        . Raw-material quantities below are for the entire order.
      </p>
      {order.requirements.map((line) => (
        <article className="saved-bom-material" key={line.materialId}>
          <h4>
            {line.code} · {line.name}
          </h4>
          <dl>
            {[
              ["Required", line.requiredQuantity],
              ["Reserved, awaiting issue", line.reservedQuantity],
              ["BOM requirement issued", line.issuedQuantity],
              ["Still to reserve", line.toReserve],
              ["Still to issue", line.toIssue],
              ["Released stock available", line.availableQuantity],
            ].map(([label, value]) => (
              <div key={label}>
                <dt>{label}</dt>
                <dd>
                  {value} {line.uom}
                </dd>
              </div>
            ))}
          </dl>
          {line.toIssue === 0 ? (
            <p className="success">Material requirement fully issued.</p>
          ) : line.toReserve === 0 ? (
            <p className="form-note">
              Issue the existing reservations; no additional reservation is
              needed.
            </p>
          ) : (
            <p className="form-note">
              Reserve {line.toReserve} {line.uom} more, then issue the
              reservations.
            </p>
          )}
        </article>
      ))}
    </div>
  );
}
function PlantOperations({
  module,
  plantId,
  role,
}: {
  module: string;
  plantId: string;
  role: string;
}) {
  const resources = [
    ...new Set([
      "materials",
      "partners",
      "bins",
      "batches",
      "pallets",
      ...(routeResources[module] || []),
    ]),
  ];
  const queries = useQueries({
    queries: resources.map((resource) => ({
      queryKey: ["operations", plantId, resource],
      queryFn: () => operationApi<Row[]>(`${resource}?plantId=${plantId}`),
      retry: false,
    })),
  });
  const data: Record<string, Row[]> = {};
  resources.forEach((resource, i) => (data[resource] = queries[i].data || []));
  const writable = role !== "Viewer";
  const manager = ["PlatformAdmin", "TenantAdmin", "PlantManager"].includes(
    role,
  );
  const [tab, setTab] = useState("workflow");
  const [prepOrderId, setPrepOrderId] = useState("");
  const [completion, setCompletion] = useState({
    targetId: "",
    quantity: "",
    scrapQuantity: "0",
  });
  const preparation = useQuery({
    queryKey: ["operations", plantId, "requirements", prepOrderId],
    queryFn: () =>
      operationApi<OrderStatus>(
        `production-orders/${prepOrderId}/requirements?plantId=${plantId}`,
      ),
    enabled: !!prepOrderId,
    retry: false,
  });
  const readiness = useQuery({
    queryKey: ["operations", plantId, "requirements", completion.targetId],
    queryFn: () =>
      operationApi<OrderStatus>(
        `production-orders/${completion.targetId}/requirements?plantId=${plantId}`,
      ),
    enabled: !!completion.targetId,
    retry: false,
  });
  const outputTotal =
    Math.round(
      (Number(completion.quantity || 0) +
        Number(completion.scrapQuantity || 0)) *
        1e6,
    ) / 1e6;
  const completionBlock = !completion.targetId
    ? "Select an order to see its planned output and saved BOM."
    : readiness.isPending
      ? "Checking saved material requirements…"
      : readiness.isError
        ? "Material requirements could not be checked. Refresh before completing."
        : readiness.data?.status !== "Released"
          ? `Order is ${readiness.data?.status}. Select a released order.`
          : !readiness.data?.canComplete
            ? "Issue all remaining raw materials in Material Preparation before completing. See the saved BOM below."
            : outputTotal !== readiness.data.plannedQuantity
              ? `Entered output plus scrap: ${outputTotal}. Planned: ${readiness.data.plannedQuantity} ${readiness.data.outputUom}. Partial completion is not supported. Enter actual quantities; do not add fictitious scrap.`
              : "";

  const [bom, setBom] = useState([{ materialId: "", quantity: "1" }]);
  const [lookup, setLookup] = useState("");
  const [lookupResult, setLookupResult] = useState<unknown>(null);
  const [lookupError, setLookupError] = useState("");
  const [lookupBusy, setLookupBusy] = useState(false);
  const [preview, setPreview] = useState<unknown>(null);
  const [previewId, setPreviewId] = useState("");
  const previewSequence = useRef(0);
  const [previewBusy, setPreviewBusy] = useState(false);
  const options = (
    resource: string,
    predicate?: (r: Row) => boolean,
    valueKey = "id",
  ): Option[] =>
    (data[resource] || []).filter(predicate || (() => true)).map((row) => ({
      value: String(row[valueKey]),
      label: [
        row.number || row.code || row.name || String(row.id).slice(0, 8),
        row.name && row.name !== row.code ? row.name : "",
        row.status || row.qualityStatus || "",
        row.quantity !== undefined ? `qty ${row.quantity}` : "",
      ]
        .filter(Boolean)
        .join(" · "),
    }));
  const raw = options("materials", (r) => r.kind === "Raw");
  const finished = options("materials", (r) => r.kind === "Finished");
  const suppliers = options("partners", (r) => r.kind === "Supplier");
  const customers = options("partners", (r) => r.kind === "Customer");
  const pallets = options("pallets");
  const eligiblePallets = (data.pallets || []).flatMap((pallet) => {
    const batch = data.batches?.find((b) => b.id === pallet.batchId);
    const line = preparation.data?.requirements.find(
      (r) => r.materialId === batch?.materialId && r.toReserve > 0,
    );
    const free = Number(
      (Number(pallet.quantity) - Number(pallet.reservedQuantity)).toFixed(6),
    );
    if (
      !line ||
      !batch ||
      batch.kind !== "Raw" ||
      batch.qualityStatus !== "Released" ||
      (batch.expiryDate &&
        Date.parse(String(batch.expiryDate)) <= Date.now()) ||
      free <= 0
    )
      return [];
    return [
      {
        value: pallet.id,
        label: `${line.code} · ${pallet.code} · available ${free} ${line.uom}`,
      },
    ];
  });
  const batches = options("batches");
  const bins = options("bins", undefined, "code");
  const production = options("production-orders");
  const released = options("production-orders", (r) => r.status === "Released");
  const reservations = options(
    "reservations",
    (r) =>
      r.status === "Reserved" &&
      (!prepOrderId || r.productionOrderId === prepOrderId),
  );
  const references: Record<string, string> = {
    materialId: "materials",
    supplierId: "partners",
    customerId: "partners",
    batchId: "batches",
    palletId: "pallets",
    productionOrderId: "production-orders",
    deliveryOrderId: "delivery-orders",
    purchaseOrderId: "purchase-orders",
    labelTemplateId: "label-templates",
  };
  const resolve = (key: string, value: unknown) => {
    const row = data[references[key]]?.find((r) => r.id === value);
    if (row) return String(row.number || row.code || row.name || value);
    if (key === "createdAt" && value)
      return new Date(String(value)).toLocaleString();
    return value == null
      ? "—"
      : typeof value === "object"
        ? JSON.stringify(value)
        : String(value);
  };
  const table = (resource: string, title: string, columns: string[]) => (
    <DataTable
      key={resource}
      title={title}
      rows={data[resource] || []}
      columns={columns}
      resolve={resolve}
    />
  );
  const form = (
    title: string,
    endpoint: FormProps["endpoint"],
    fields: Field[],
    isManager = false,
    extra?: FormProps["extra"],
    children?: ReactNode,
    note?: string,
    onValuesChange?: FormProps["onValuesChange"],
    blockedReason?: string,
  ) => (
    <CommandForm
      key={title}
      title={title}
      endpoint={endpoint}
      plantId={plantId}
      fields={fields}
      disabled={isManager ? !manager : !writable}
      extra={extra}
      note={note}
      onValuesChange={onValuesChange}
      blockedReason={blockedReason}
    >
      {children}
    </CommandForm>
  );
  const byId =
    (resource: string, action: string) => (input: Record<string, unknown>) =>
      `${resource}/${input.targetId}/${action}`;
  async function inventoryLookup(kind: "barcode" | "allocation") {
    setLookupError("");
    setLookupBusy(true);
    try {
      setLookupResult(
        await operationApi(
          `${kind === "barcode" ? "inventory" : "allocation"}?plantId=${plantId}&${kind === "barcode" ? "barcode" : "materialId"}=${encodeURIComponent(lookup)}`,
        ),
      );
    } catch (e) {
      setLookupError((e as Error).message);
    } finally {
      setLookupBusy(false);
    }
  }
  return (
    <>
      {queries.some((q) => q.isPending) && (
        <p role="status" className="form-note">
          Loading plant records…
        </p>
      )}
      {queries
        .filter((q) => q.isError)
        .map((q, i) => (
          <p key={i} role="alert" className="error">
            {q.error?.message}
          </p>
        ))}
      {module === "inbound" && (
        <div className="tabs" role="tablist" aria-label="Inbound sections">
          {[
            ["workflow", "Receiving"],
            ["masters", "Master data"],
            ["yard", "Truck & dock"],
            ["packed", "Packed receipt"],
            ["setup", "Receiving setup"],
          ].map(([value, label]) => (
            <button
              key={value}
              role="tab"
              aria-selected={tab === value}
              className={tab === value ? "selected" : ""}
              onClick={() => setTab(value)}
            >
              {label}
            </button>
          ))}
        </div>
      )}
      {module === "inbound" && ["yard", "packed", "setup"].includes(tab) && (
        <InboundDesk
          key={plantId}
          plantId={plantId}
          role={role}
          section={tab}
        />
      )}
      {module === "inbound" && tab === "masters" && (
        <>
          <div className="operations-forms">
            {form("Create material", "materials", [
              text("code", "Material code"),
              text("name", "Material name"),
              text("uom", "Unit of measure", "kg"),
              choice("kind", "Material kind", fixed(["Raw", "Finished"])),
            ])}
            {form("Create partner", "partners", [
              text("code", "Partner code"),
              text("name", "Partner name"),
              choice("kind", "Partner kind", fixed(["Supplier", "Customer"])),
            ])}
            {form("Create bin", "bins", [
              text("code", "Bin code"),
              text("name", "Bin name"),
              text("warehouse", "Warehouse"),
            ])}
          </div>
          {table("materials", "Materials", ["code", "name", "kind", "uom"])}
          {table("partners", "Suppliers and customers", [
            "code",
            "name",
            "kind",
          ])}
          {table("bins", "Warehouse bins", ["code", "name", "warehouse"])}
        </>
      )}
      {module === "inbound" && tab === "workflow" && (
        <>
          <div className="operations-forms">
            {form("Create purchase order", "purchase-orders", [
              text("number", "Purchase order number"),
              choice("supplierId", "Supplier", suppliers),
              choice("materialId", "Raw material", raw),
              num("quantity", "Ordered quantity"),
            ])}
            {form(
              "Post goods receipt",
              "receipts",
              [
                choice(
                  "purchaseOrderId",
                  "Purchase order",
                  options(
                    "purchase-orders",
                    (r) => r.status === "Open" || r.status === "Partial",
                  ),
                ),
                num("quantity", "Received quantity"),
                text("supplierBatch", "Supplier batch"),
                text("batchNumber", "New batch number"),
                {
                  key: "expiryDate",
                  label: "Expiry date",
                  type: "date",
                  required: false,
                },
              ],
              false,
              undefined,
              undefined,
              "Creates a quality-held raw batch and pallet at RECEIVING. Release through Quality before consumption. If truck or delivery-note rules are enabled, use Packed receipt.",
            )}
          </div>
          {table("purchase-orders", "Purchase orders", [
            "number",
            "supplierId",
            "materialId",
            "quantity",
            "receivedQuantity",
            "status",
          ])}
          {table("receipts", "Goods receipts", [
            "createdAt",
            "purchaseOrderId",
            "batchId",
            "palletId",
            "quantity",
            "supplierBatch",
          ])}
        </>
      )}
      {module === "inventory" && (
        <>
          <div className="operations-forms">
            {form("Transfer pallet", "transfers", [
              choice("palletId", "Pallet", pallets),
              choice("location", "Destination bin", bins),
            ])}
            {form(
              "Adjust stock",
              "adjustments",
              [
                choice("palletId", "Pallet", pallets),
                { ...num("quantity", "Quantity change"), signed: true },
                { key: "reason", label: "Adjustment reason", type: "textarea" },
              ],
              true,
              undefined,
              undefined,
              "Signed quantity: positive adds stock; negative removes stock. Corrections append an auditable movement.",
            )}
          </div>
          <section className="card operation-form">
            <h2>Barcode lookup and FEFO allocation</h2>
            <p className="form-note">
              Scan a pallet code for lookup, or choose a material for FEFO stock
              suggestions.
            </p>
            <label>
              Barcode or material identifier
              <input
                aria-label="Barcode or material identifier"
                value={lookup}
                onChange={(e) => setLookup(e.target.value)}
                autoComplete="off"
                onKeyDown={(e) => {
                  if (e.key === "Enter" && lookup) {
                    e.preventDefault();
                    void inventoryLookup("barcode");
                  }
                }}
              />
            </label>
            <label>
              Material for FEFO
              <select
                aria-label="Material for FEFO"
                value={raw.some((o) => o.value === lookup) ? lookup : ""}
                onChange={(e) => setLookup(e.target.value)}
              >
                <option value="">Select material</option>
                {raw.map((o) => (
                  <option key={o.value} value={o.value}>
                    {o.label}
                  </option>
                ))}
              </select>
            </label>
            <div className="button-row">
              <button
                className="primary"
                disabled={!lookup || lookupBusy}
                onClick={() => inventoryLookup("barcode")}
              >
                Look up barcode
              </button>
              <button
                className="secondary"
                disabled={!lookup || lookupBusy}
                onClick={() => inventoryLookup("allocation")}
              >
                Find FEFO stock
              </button>
            </div>
            {lookupBusy && <p role="status">Looking up stock…</p>}
            {lookupError && (
              <p className="error" role="alert">
                {lookupError}
              </p>
            )}
            {lookupResult !== null && (
              <pre className="operation-result">
                {JSON.stringify(lookupResult, null, 2)}
              </pre>
            )}
          </section>
          {table("pallets", "Stock balances", [
            "code",
            "batchId",
            "location",
            "quantity",
            "reservedQuantity",
          ])}
          {table("batches", "Batch quality", [
            "number",
            "materialId",
            "qualityStatus",
            "expiryDate",
          ])}
          {table("movements", "Immutable stock ledger", [
            "createdAt",
            "kind",
            "palletId",
            "quantity",
            "fromLocation",
            "toLocation",
            "reference",
          ])}
        </>
      )}
      {module === "preparation" && (
        <PreparationWorkspace plantId={plantId} role={role}>
          <div className="operations-forms">
            {form(
              "Reserve material",
              "reservations",
              [
                choice(
                  "productionOrderId",
                  "Released production order",
                  released,
                ),
                choice("palletId", "Source pallet", eligiblePallets),
                num("quantity", "Reservation quantity"),
              ],
              false,
              undefined,
              <ReadinessSummary
                order={preparation.data}
                pending={!!prepOrderId && preparation.isPending}
                error={preparation.error?.message}
              />,
              "Select an order first. Only released, unexpired raw pallets still needed by its saved BOM are offered. Enter quantities in the material’s unit.",
              (values) => setPrepOrderId(values.productionOrderId),
            )}
            {form("Issue reservation", byId("reservations", "issue"), [
              choice("targetId", "Reservation to issue", reservations),
            ])}
            {form("Cancel reservation", byId("reservations", "cancel"), [
              choice("targetId", "Reservation to cancel", reservations),
            ])}
          </div>
          {table("reservations", "Reservations", [
            "productionOrderId",
            "palletId",
            "quantity",
            "status",
          ])}
          {table("consumptions", "Issued material", [
            "createdAt",
            "productionOrderId",
            "palletId",
            "quantity",
          ])}
        </PreparationWorkspace>
      )}
      {module === "production" && (
        <>
          <div className="operations-forms">
            {form(
              "Create production order",
              "production-orders",
              [
                text("number", "Production order number"),
                choice("materialId", "Finished material", finished),
                num("plannedQuantity", "Planned output quantity"),
              ],
              false,
              () => ({
                requirements: bom.map((line) => ({
                  materialId: line.materialId,
                  quantity: Number(line.quantity),
                })),
              }),
              <fieldset className="bom-lines">
                <legend>Raw material requirements</legend>
                {bom.map((line, i) => (
                  <div className="bom-line" key={i}>
                    <label>
                      Requirement {i + 1} material
                      <select
                        aria-label={`Requirement ${i + 1} material`}
                        required
                        value={line.materialId}
                        onChange={(e) =>
                          setBom(
                            bom.map((v, j) =>
                              j === i
                                ? { ...v, materialId: e.target.value }
                                : v,
                            ),
                          )
                        }
                      >
                        <option value="">Select raw material</option>
                        {raw.map((o) => (
                          <option key={o.value} value={o.value}>
                            {o.label}
                          </option>
                        ))}
                      </select>
                    </label>
                    <label>
                      Requirement {i + 1} quantity
                      <input
                        aria-label={`Requirement ${i + 1} quantity`}
                        type="number"
                        step="0.000001"
                        min="0.000001"
                        required
                        value={line.quantity}
                        onChange={(e) =>
                          setBom(
                            bom.map((v, j) =>
                              j === i ? { ...v, quantity: e.target.value } : v,
                            ),
                          )
                        }
                      />
                    </label>
                    {bom.length > 1 && (
                      <button
                        type="button"
                        className="secondary"
                        onClick={() => setBom(bom.filter((_, j) => j !== i))}
                      >
                        Remove requirement {i + 1}
                      </button>
                    )}
                  </div>
                ))}
                <button
                  type="button"
                  className="secondary"
                  onClick={() =>
                    setBom([...bom, { materialId: "", quantity: "1" }])
                  }
                >
                  Add requirement
                </button>
              </fieldset>,
            )}
            {form(
              "Release order",
              byId("production-orders", "release"),
              [
                choice(
                  "targetId",
                  "Draft production order",
                  options("production-orders", (r) => r.status === "Draft"),
                ),
              ],
              true,
            )}
            {form(
              "Complete production",
              byId("production-orders", "complete"),
              [
                choice("targetId", "Production order to complete", released),
                num("quantity", "Good output quantity"),
                {
                  ...num("scrapQuantity", "Scrap quantity", "0"),
                  required: false,
                },
                choice("location", "Finished goods bin", bins),
              ],
              false,
              undefined,
              <ReadinessSummary
                order={readiness.data}
                pending={!!completion.targetId && readiness.isPending}
                error={readiness.error?.message}
              />,
              "This closes the whole order. Good output + actual scrap must equal its planned output. All saved raw-material requirements must be issued first.",
              (values) =>
                setCompletion({
                  targetId: values.targetId,
                  quantity: values.quantity,
                  scrapQuantity: values.scrapQuantity,
                }),
              completionBlock,
            )}
            {form("Record downtime", "downtime", [
              choice("productionOrderId", "Production order", production),
              { ...num("minutes", "Downtime minutes"), integer: true },
              text("reason", "Downtime reason"),
            ])}
          </div>
          {table("production-orders", "Production orders", [
            "number",
            "materialId",
            "plannedQuantity",
            "completedQuantity",
            "status",
          ])}
          {table("confirmations", "Production confirmations", [
            "createdAt",
            "productionOrderId",
            "batchId",
            "quantity",
            "scrapQuantity",
          ])}
          {table("downtimes", "Downtime events", [
            "createdAt",
            "productionOrderId",
            "minutes",
            "reason",
          ])}
        </>
      )}
      {module === "quality" && (
        <>
          <div className="operations-forms">
            {form(
              "Record quality decision",
              "quality-inspections",
              [
                choice("batchId", "Batch", batches),
                choice(
                  "decision",
                  "Decision",
                  fixed(["Released", "Rejected", "Hold"]),
                ),
                { key: "notes", label: "Inspection notes", type: "textarea" },
              ],
              true,
              undefined,
              undefined,
              "Manager authorization required. Decisions control batch consumption and dispatch eligibility; every decision is audited.",
            )}
          </div>
          {table("batches", "Batch dispositions", [
            "number",
            "kind",
            "materialId",
            "qualityStatus",
            "expiryDate",
          ])}
          {table("quality-inspections", "Inspection history", [
            "createdAt",
            "batchId",
            "decision",
            "notes",
            "actorId",
          ])}
        </>
      )}
      {module === "finished-goods" && (
        <>
          {table("finished-goods", "Finished goods inventory", [
            "code",
            "batchNumber",
            "materialId",
            "location",
            "quantity",
            "reservedQuantity",
            "qualityStatus",
            "expiryDate",
          ])}
          <p className="info-note">
            Finished goods are created by Production completion. Release their
            batch through Quality before Dispatch.
          </p>
        </>
      )}
      {module === "dispatch" && (
        <>
          <div className="operations-forms">
            {form("Create delivery order", "delivery-orders", [
              text("number", "Delivery order number"),
              choice("customerId", "Customer", customers),
              choice("materialId", "Finished material", finished),
              num("quantity", "Delivery quantity"),
            ])}
            {form(
              "Confirm shipment",
              "shipments",
              [
                choice(
                  "deliveryOrderId",
                  "Open delivery order",
                  options("delivery-orders", (r) => r.status === "Open"),
                ),
                choice(
                  "palletId",
                  "Finished pallet",
                  options("pallets", (p) =>
                    data.batches.some(
                      (b) => b.id === p.batchId && b.kind === "Finished",
                    ),
                  ),
                ),
                num("quantity", "Shipment quantity"),
                text("truck", "Truck registration"),
              ],
              false,
              undefined,
              undefined,
              "Released, unexpired finished stock only. The core workflow ships the entire delivery quantity from one pallet.",
            )}
          </div>
          {table("delivery-orders", "Delivery orders", [
            "number",
            "customerId",
            "materialId",
            "quantity",
            "status",
          ])}
          {table("shipments", "Confirmed shipments", [
            "createdAt",
            "deliveryOrderId",
            "palletId",
            "quantity",
            "truck",
          ])}
        </>
      )}
      {module === "traceability" && (
        <TraceView plantId={plantId} batches={batches} />
      )}
      {module === "labels" && (
        <>
          <p className="info-note">
            Local label rendering and queue only. No physical printer is
            connected; queued jobs do not confirm physical printing.
          </p>
          <div className="operations-forms">
            {form(
              "Create label template",
              "label-templates",
              [
                text("name", "Template name"),
                {
                  key: "content",
                  label: "Template content",
                  type: "textarea",
                  value:
                    "Pallet {{palletCode}} | Batch {{batchNumber}}\n{{materialName}} · {{quantity}} {{uom}}\nLocation {{location}}",
                  hint: "Variables: {{palletCode}}, {{batchNumber}}, {{materialName}}, {{quantity}}, {{uom}}, {{location}}.",
                },
              ],
              true,
            )}
            {form(
              "Queue local label",
              "print-jobs",
              [
                choice("palletId", "Pallet to label", pallets),
                choice(
                  "labelTemplateId",
                  "Label template",
                  options("label-templates"),
                ),
                { ...num("copies", "Copies", "1"), integer: true },
              ],
              false,
              () => ({ printer: "Local preview" }),
            )}
            {form(
              "Authorize reprint",
              byId("print-jobs", "reprint"),
              [
                choice("targetId", "Original print job", options("print-jobs")),
                { key: "reason", label: "Reprint reason", type: "textarea" },
              ],
              true,
            )}
          </div>
          <section className="card operation-form">
            <h2>Preview label</h2>
            <label>
              Queued print job
              <select
                aria-label="Queued print job"
                defaultValue=""
                onChange={async (e) => {
                  const id = e.target.value;
                  const sequence = ++previewSequence.current;
                  setPreview(null);
                  setPreviewId(id);
                  setLookupError("");
                  setPreviewBusy(!!id);
                  if (id)
                    try {
                      const result = await operationApi(
                        `print-jobs/${id}/preview?plantId=${plantId}`,
                      );
                      if (sequence === previewSequence.current)
                        setPreview(result);
                    } catch (error) {
                      if (sequence === previewSequence.current)
                        setLookupError((error as Error).message);
                    } finally {
                      if (sequence === previewSequence.current)
                        setPreviewBusy(false);
                    }
                }}
              >
                <option value="">Select queued job</option>
                {options("print-jobs").map((o) => (
                  <option key={o.value} value={o.value}>
                    {o.label}
                  </option>
                ))}
              </select>
            </label>
            {lookupError && (
              <p className="error" role="alert">
                {lookupError}
              </p>
            )}
            {previewBusy && <p role="status">Loading label snapshot…</p>}
            {preview &&
            typeof preview === "object" &&
            "snapshotAvailable" in preview &&
            preview.snapshotAvailable === false ? (
              <p className="warning" role="alert">
                Historical snapshot unavailable. This legacy job has no saved
                label content; do not treat an empty preview as an original
                label.
              </p>
            ) : null}
            {preview !== null && (
              <>
                <img
                  className="label-qr"
                  alt="Pallet QR code"
                  src={`/api/operations/print-jobs/${previewId}/barcode.svg?plantId=${plantId}`}
                />
                <pre className="label-preview">
                  {typeof preview === "object" &&
                  preview &&
                  "renderedContent" in preview
                    ? String(preview.renderedContent)
                    : JSON.stringify(preview, null, 2)}
                </pre>
              </>
            )}
          </section>
          {table("label-templates", "Label templates", [
            "name",
            "revision",
            "content",
          ])}
          {table("print-jobs", "Local print queue", [
            "createdAt",
            "palletId",
            "labelTemplateId",
            "copies",
            "status",
            "printer",
            "reprintOfId",
            "reason",
          ])}
        </>
      )}
      {module === "integrations" && (
        <>
          <p className="info-note">
            Local test adapter only. JSON validation records readiness; no
            message is transmitted to SAP or any external ERP.
          </p>
          <div className="operations-forms">
            {form(
              "Register local message",
              "integration-messages",
              [
                text("externalId", "External message identifier"),
                text("kind", "Message kind", "MaterialMaster"),
                {
                  key: "payload",
                  label: "JSON payload",
                  type: "textarea",
                  value: "{}",
                },
              ],
              true,
            )}
            {form(
              "Validate local message",
              byId("integration-messages", "process"),
              [
                choice(
                  "targetId",
                  "Integration message",
                  options("integration-messages"),
                ),
              ],
              true,
            )}
          </div>
          {table("integration-messages", "Integration messages", [
            "createdAt",
            "externalId",
            "kind",
            "status",
            "attempts",
            "error",
            "correlationId",
          ])}
        </>
      )}
      {module === "reports" && <ReportsView plantId={plantId} />}
    </>
  );
}
function TraceView({
  plantId,
  batches,
}: {
  plantId: string;
  batches: Option[];
}) {
  const [batchId, setBatchId] = useState("");
  const query = useQuery({
    queryKey: ["operations", plantId, "trace", batchId],
    queryFn: () =>
      operationApi<Record<string, unknown>>(
        `traceability?plantId=${plantId}&batchId=${batchId}`,
      ),
    enabled: !!batchId,
    retry: false,
  });
  return (
    <>
      <section className="card operation-form">
        <h2>Forward and backward batch genealogy</h2>
        <label>
          Trace batch
          <select
            aria-label="Trace batch"
            value={batchId}
            onChange={(e) => setBatchId(e.target.value)}
          >
            <option value="">Select batch</option>
            {batches.map((o) => (
              <option key={o.value} value={o.value}>
                {o.label}
              </option>
            ))}
          </select>
        </label>
        <p className="form-note">
          Follow source consumption, output batches, quality decisions, stock
          movements, and customer shipments.
        </p>
      </section>
      {query.isFetching && <p role="status">Loading genealogy…</p>}
      {query.error && (
        <p role="alert" className="error">
          {query.error.message}
        </p>
      )}
      {query.data?.isComplete === false && (
        <p className="warning" role="alert">
          Incomplete genealogy: a linked-record limit or graph depth limit was
          reached. Do not treat this view as a complete traceability record.
          Review truncation metadata below and narrow the investigation.
        </p>
      )}
      {query.data &&
        Object.entries(query.data).map(([key, value]) => (
          <section className="card operation-form" key={key}>
            <h2>{human(key)}</h2>
            <StructuredValue value={value} />
          </section>
        ))}
    </>
  );
}
function StructuredValue({ value }: { value: unknown }) {
  if (Array.isArray(value))
    return value.length ? (
      <div className="trace-records">
        {value.map((row, i) => (
          <StructuredValue key={i} value={row} />
        ))}
      </div>
    ) : (
      <p className="form-note">No linked records.</p>
    );
  if (value && typeof value === "object")
    return (
      <dl>
        {Object.entries(value)
          .filter(([k]) => !["tenantId", "plantId", "version"].includes(k))
          .map(([k, v]) => (
            <div key={k}>
              <dt>{human(k)}</dt>
              <dd>
                {v === null ? (
                  "—"
                ) : typeof v === "object" ? (
                  <StructuredValue value={v} />
                ) : (
                  String(v)
                )}
              </dd>
            </div>
          ))}
      </dl>
    );
  return <p>{String(value ?? "No linked record")}</p>;
}
function ReportsView({ plantId }: { plantId: string }) {
  const query = useQuery({
    queryKey: ["operations", plantId, "reports"],
    queryFn: () =>
      operationApi<Record<string, unknown>>(`reports?plantId=${plantId}`),
    retry: false,
  });
  return (
    <section className="card operation-form">
      <h2>Operational aggregates</h2>
      <p className="form-note">
        Computed from persisted plant transactions, grouped by unit of measure.
        OEE requires telemetry and is not estimated.
      </p>
      {query.isPending ? (
        <p role="status">Loading report…</p>
      ) : query.error ? (
        <p className="error" role="alert">
          {query.error.message}
        </p>
      ) : (
        <StructuredValue value={query.data} />
      )}
    </section>
  );
}

export function OperationalSummary({ plantId }: { plantId: string }) {
  const query = useQuery({
    queryKey: ["operations", plantId, "reports"],
    queryFn: () =>
      operationApi<Record<string, unknown>>(`reports?plantId=${plantId}`),
    enabled: !!plantId,
    retry: false,
  });
  if (!plantId) return null;
  return (
    <section className="card operational-summary">
      <div className="card-heading">
        <div>
          <h2>Plant operations</h2>
          <p>Current transaction totals for the selected plant.</p>
        </div>
        <Link to="/reports" className="text-link">
          View reports →
        </Link>
      </div>
      {query.isPending ? (
        <p className="empty compact" role="status">
          Loading plant summary…
        </p>
      ) : query.error ? (
        <p role="alert" className="error">
          {query.error.message}
        </p>
      ) : (
        <>
          <div className="summary-metrics">
            {[
              ["openOrders", "Open production orders"],
              ["completedOrders", "Completed orders"],
              ["qualityHoldCount", "Batches on quality hold"],
              ["downtimeMinutes", "Downtime minutes"],
            ].map(([key, label]) => (
              <div key={key}>
                <strong>{String(query.data?.[key] ?? "—")}</strong>
                <span>{label}</span>
              </div>
            ))}
          </div>
          <p className="form-note summary-time">
            Snapshot:{" "}
            {query.data?.asOf
              ? new Date(String(query.data.asOf)).toLocaleString()
              : "Unavailable"}
            . OEE requires equipment telemetry.
          </p>
        </>
      )}
    </section>
  );
}
