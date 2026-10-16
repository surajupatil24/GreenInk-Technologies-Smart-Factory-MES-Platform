import { useRef, useState, type FormEvent, type ReactNode } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { CalendarClock, ClipboardCheck, Truck, Warehouse } from "lucide-react";
import { operationApi } from "./Operations";
import "./inbound.css";

type Row = Record<string, any> & { id: string };
function Action({
  title,
  endpoint,
  plantId,
  disabled,
  build,
  children,
  onDone,
}: {
  title: string;
  endpoint: string;
  plantId: string;
  disabled: boolean;
  build: (f: FormData) => object;
  children: ReactNode;
  onDone?: () => void;
}) {
  const cache = useQueryClient();
  const [busy, setBusy] = useState(false),
    [error, setError] = useState(""),
    [saved, setSaved] = useState(false);
  const retry = useRef({ signature: "", id: "" });
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError("");
    setSaved(false);
    setBusy(true);
    try {
      const data = { plantId, ...build(new FormData(e.currentTarget)) };
      const signature = JSON.stringify(data);
      if (retry.current.signature !== signature)
        retry.current = { signature, id: crypto.randomUUID() };
      await operationApi(endpoint, { ...data, commandId: retry.current.id });
      retry.current = { signature: "", id: "" };
      setSaved(true);
      await cache.invalidateQueries({ queryKey: ["operations"] });
      onDone?.();
    } catch (error) {
      setError((error as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <section className="card inbound-action">
      <h2>{title}</h2>
      <form onSubmit={submit}>
        <fieldset disabled={disabled || busy}>{children}</fieldset>
        {error && (
          <p className="error" role="alert">
            {error}
          </p>
        )}
        {saved && (
          <p className="success" role="status">
            Saved. Records refreshed.
          </p>
        )}
        {disabled && <p className="form-note">Your role has read access.</p>}
        <button className="primary" disabled={disabled || busy}>
          {busy ? "Saving…" : title}
        </button>
      </form>
    </section>
  );
}
const value = (f: FormData, key: string) => String(f.get(key) || "");
const label = (r: Row) => String(r.number || r.code || r.name || r.truckNumber);
function Select({
  name,
  title,
  rows,
  required = true,
}: {
  name: string;
  title: string;
  rows: Row[];
  required?: boolean;
}) {
  return (
    <label>
      {title}
      <select name={name} required={required} defaultValue="">
        <option value="">Select {title.toLowerCase()}</option>
        {rows.map((r) => (
          <option value={r.id} key={r.id}>
            {label(r)}
            {r.status ? ` · ${r.status}` : ""}
          </option>
        ))}
      </select>
    </label>
  );
}
export function InboundDesk({
  plantId,
  role,
  section,
}: {
  plantId: string;
  role: string;
  section: string;
}) {
  const manager = ["PlatformAdmin", "TenantAdmin", "PlantManager"].includes(
      role,
    ),
    writable = role !== "Viewer";
  function useRows(resource: string) {
    return useQuery({
      queryKey: ["operations", plantId, resource],
      queryFn: () => operationApi<Row[]>(`${resource}?plantId=${plantId}`),
      retry: false,
    });
  }
  const visits = useRows("receiving/visits"),
    docks = useRows("receiving/docks"),
    checks = useRows("receiving/checks"),
    orders = useRows("purchase-orders"),
    materials = useRows("materials");
  const policy = useQuery({
    queryKey: ["operations", plantId, "receiving/policy"],
    queryFn: () => operationApi<Row>(`receiving/policy?plantId=${plantId}`),
    retry: false,
  });
  const [visitId, setVisitId] = useState(""),
    [search, setSearch] = useState(""),
    [status, setStatus] = useState("Active");
  const detail = useQuery({
    queryKey: ["operations", plantId, "receiving/visit", visitId],
    queryFn: () =>
      operationApi<{ visit: Row; orders: Row[]; checks: Row[] }>(
        `receiving/visits/${visitId}?plantId=${plantId}`,
      ),
    enabled: !!visitId,
    retry: false,
  });
  const [pack, setPack] = useState({ size: "", count: "", loose: "0" }),
    [receiptVisit, setReceiptVisit] = useState("");
  const quantity =
    Math.round(
      (Number(pack.size) * Number(pack.count) + Number(pack.loose)) * 1e6,
    ) / 1e6;
  const allVisits = visits.data || [],
    allOrders = orders.data || [],
    allDocks = docks.data || [];
  const openOrders = allOrders.filter((r) =>
    ["Open", "Partial"].includes(r.status),
  );
  const active = allVisits.filter((r) => r.status !== "Out"),
    selected = detail.data?.visit;
  const visible = allVisits.filter(
    (r) =>
      (status === "All" ||
        (status === "Active" ? r.status !== "Out" : r.status === status)) &&
      `${r.truckNumber} ${r.driverName} ${r.token} ${r.orderSignature} ${allOrders
        .filter((o) => r.orderSignature.includes(o.id))
        .map((o) => o.number)
        .join(" ")}`
        .toLowerCase()
        .includes(search.toLowerCase()),
  );
  const errors = [
    visits,
    docks,
    checks,
    orders,
    materials,
    policy,
    detail,
  ].filter((q) => q.isError);
  const next: Record<string, string> = {
    Expected: "Arrived",
    Arrived: "Docked",
    Docked: "Unloading",
    Unloading: "Unloaded",
    Unloaded: "Out",
  };
  return (
    <div className="inbound-desk">
      <div className="inbound-intro">
        <div>
          <span className="eyebrow">RECEIVING CONTROL</span>
          <h2>From gate to goods receipt</h2>
          <p>
            Coordinate arrivals, clear checks and receive material into quality
            hold.
          </p>
        </div>
        <Truck size={34} />
      </div>
      {errors.map((q, i) => (
        <p className="error" role="alert" key={i}>
          {q.error?.message}
        </p>
      ))}
      {[visits, docks, checks, orders, materials, policy].some(
        (q) => q.isPending,
      ) && <p role="status">Loading receiving records…</p>}
      {section === "yard" && (
        <>
          <div className="inbound-metrics">
            {[
              [
                CalendarClock,
                "Expected",
                allVisits.filter((r) => r.status === "Expected").length,
              ],
              [
                Truck,
                "On site",
                active.filter((r) => r.status !== "Expected").length,
              ],
              [
                Warehouse,
                "Unloading",
                active.filter((r) => r.status === "Unloading").length,
              ],
              [
                ClipboardCheck,
                "Available docks",
                allDocks.filter(
                  (r) =>
                    !r.maintenance &&
                    !active.some(
                      (v) =>
                        v.dockStationId === r.id && v.status === "Unloading",
                    ),
                ).length,
              ],
            ].map(([Icon, title, count]) => {
              const I = Icon as typeof Truck;
              return (
                <article className="card" key={String(title)}>
                  <I size={20} />
                  <span>{String(title)}</span>
                  <strong>{String(count)}</strong>
                </article>
              );
            })}
          </div>
          <div className="inbound-layout">
            <section className="card inbound-queue">
              <h2>Arrival board</h2>
              <div className="inbound-filter">
                <input
                  aria-label="Search truck visits"
                  placeholder="Truck, token, driver or order…"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
                <select
                  aria-label="Visit status filter"
                  value={status}
                  onChange={(e) => setStatus(e.target.value)}
                >
                  {[
                    "Active",
                    "All",
                    "Expected",
                    "Arrived",
                    "Docked",
                    "Unloading",
                    "Unloaded",
                    "Out",
                  ].map((s) => (
                    <option key={s}>{s}</option>
                  ))}
                </select>
              </div>
              <p className="form-note">
                Latest 200 scheduled visits. Docked trucks may queue; only one
                truck can unload at a dock.
              </p>
              <div className="inbound-visit-list">
                {visible.map((v) => (
                  <button
                    key={v.id}
                    className={`inbound-visit ${visitId === v.id ? "chosen" : ""}`}
                    onClick={() => setVisitId(v.id)}
                  >
                    <span>
                      <strong>{v.truckNumber}</strong>
                      <small>
                        {new Date(v.scheduledAt).toLocaleString()} ·{" "}
                        {v.driverName}
                      </small>
                      <small>
                        {allOrders
                          .filter((o) => v.orderSignature.includes(o.id))
                          .map((o) => o.number)
                          .join(", ")}
                      </small>
                    </span>
                    <span className="inbound-status">{v.status}</span>
                  </button>
                ))}
                {!visible.length && (
                  <p className="empty">
                    No matching arrivals. Register a truck below.
                  </p>
                )}
              </div>
            </section>
            <div className="inbound-detail">
              {!selected ? (
                <section className="card inbound-action">
                  <h2>Visit workspace</h2>
                  <p>
                    Select a truck to view its orders, checks and next gate.
                  </p>
                </section>
              ) : (
                <>
                  <section className="card inbound-action">
                    <h2>{selected.truckNumber}</h2>
                    <p>
                      {selected.driverName} · {selected.status}
                    </p>
                    <p className="form-note">
                      {selected.exitedAt
                        ? "Exited · token inactive"
                        : "Gate token"}
                      : <code>{selected.token}</code>
                    </p>
                    <ol className="inbound-timeline">
                      {[
                        "Expected",
                        "Arrived",
                        "Docked",
                        "Unloading",
                        "Unloaded",
                        "Out",
                      ].map((s) => (
                        <li
                          key={s}
                          aria-current={
                            selected.status === s ? "step" : undefined
                          }
                        >
                          {s}
                        </li>
                      ))}
                    </ol>
                    <dl className="inbound-dates">
                      {[
                        ["Scheduled", selected.scheduledAt],
                        ["Arrived", selected.arrivedAt],
                        ["Unloading started", selected.unloadingStartedAt],
                        ["Unloading finished", selected.unloadingCompletedAt],
                        ["Exited", selected.exitedAt],
                      ].map(([name, date]) => (
                        <div key={name}>
                          <dt>{name}</dt>
                          <dd>
                            {date ? new Date(date).toLocaleString() : "Pending"}
                          </dd>
                        </div>
                      ))}
                    </dl>
                  </section>
                  {detail.data?.checks.map((c) => (
                    <Action
                      key={`${c.id}:${c.answeredAt}`}
                      title={`Save ${c.stage.toLowerCase()} check`}
                      endpoint={`receiving/checks/${c.id}/answer`}
                      plantId={plantId}
                      disabled={
                        !writable ||
                        (c.stage === "Arrival"
                          ? !["Expected", "Arrived"].includes(selected.status)
                          : !["Expected", "Arrived", "Docked"].includes(
                              selected.status,
                            ))
                      }
                      build={(f) => ({
                        passed: value(f, "passed") === "true",
                        notes: value(f, "notes"),
                      })}
                    >
                      <p>
                        <strong>{c.prompt}</strong>
                        <small className="form-note">
                          {c.required ? "Answer required" : "Optional answer"} ·{" "}
                          {c.requirePass ? "Must pass" : "Recorded result"}
                          {c.answeredAt
                            ? ` · saved ${new Date(c.answeredAt).toLocaleString()}`
                            : ""}
                        </small>
                      </p>
                      <label>
                        Result
                        <select
                          name="passed"
                      aria-label="Result"
                          required
                          defaultValue={
                            c.passed === null ? "" : String(c.passed)
                          }
                        >
                          <option value="">Select result</option>
                          <option value="true">Pass</option>
                          <option value="false">Fail</option>
                        </select>
                      </label>
                      <label>
                        Check notes
                        <textarea
                          name="notes"
                          maxLength={2000}
                          defaultValue={c.notes}
                        />
                      </label>
                    </Action>
                  ))}
                  {next[selected.status] && (
                    <Action
                      key={selected.id + selected.status}
                      title={`Mark ${next[selected.status]}`}
                      endpoint={`receiving/visits/${selected.id}/status`}
                      plantId={plantId}
                      disabled={!writable}
                      build={(f) => ({
                        status: next[selected.status],
                        dockStationId: value(f, "dockStationId") || null,
                      })}
                    >
                      {selected.status === "Arrived" && (
                        <Select
                          name="dockStationId"
                          title="Receiving dock"
                          rows={allDocks.filter((r) => !r.maintenance)}
                        />
                      )}
                      <p className="form-note">
                        Required checklist answers are checked before the next
                        gate. Receipt posting is available while unloading.
                      </p>
                    </Action>
                  )}
                </>
              )}
            </div>
          </div>
          <Action
            title="Register truck"
            endpoint="receiving/visits"
            plantId={plantId}
            disabled={!writable}
            build={(f) => ({
              truckNumber: value(f, "truckNumber"),
              driverName: value(f, "driverName"),
              scheduledAt: new Date(value(f, "scheduledAt")).toISOString(),
              purchaseOrderIds: f.getAll("purchaseOrderIds"),
            })}
          >
            <div className="inbound-input-grid">
              <label>
                Truck registration number
                <input name="truckNumber" required maxLength={32} />
              </label>
              <label>
                Driver name
                <input name="driverName" required maxLength={160} />
              </label>
              <label>
                Scheduled arrival
                <input name="scheduledAt" type="datetime-local" required />
              </label>
              <label>
                Registered purchase orders
                <select name="purchaseOrderIds" multiple required size={4}>
                  {openOrders.map((o) => (
                    <option key={o.id} value={o.id}>
                      {o.number} · remaining {o.quantity - o.receivedQuantity}
                    </option>
                  ))}
                </select>
                <small>Use Ctrl or Command to select multiple orders.</small>
              </label>
            </div>
          </Action>
        </>
      )}
      {section === "packed" && (
        <Action
          title="Post packed receipt"
          endpoint="receipts"
          plantId={plantId}
          disabled={!writable || !policy.data}
          build={(f) => ({
            purchaseOrderId: value(f, "purchaseOrderId"),
            truckVisitId: value(f, "truckVisitId") || null,
            deliveryNote: value(f, "deliveryNote"),
            supplierBatch: value(f, "supplierBatch"),
            batchNumber: value(f, "batchNumber"),
            expiryDate: value(f, "expiryDate")
              ? new Date(`${value(f, "expiryDate")}T00:00:00Z`).toISOString()
              : null,
            packSize: Number(pack.size),
            packCount: Number(pack.count),
            looseQuantity: Number(pack.loose),
            quantity,
          })}
        >
          <div className="inbound-input-grid">
            <label>
              Unloading truck
              <select
                name="truckVisitId"
                required={policy.data?.requireTruck}
                value={receiptVisit}
                onChange={(e) => setReceiptVisit(e.target.value)}
              >
                <option value="">No truck selected</option>
                {allVisits
                  .filter((v) => v.status === "Unloading")
                  .map((v) => (
                    <option value={v.id} key={v.id}>
                      {v.truckNumber}
                    </option>
                  ))}
              </select>
            </label>
            <Select
              name="purchaseOrderId"
              title="Packed purchase order"
              rows={openOrders
                .filter(
                  (o) =>
                    !receiptVisit ||
                    allVisits
                      .find((v) => v.id === receiptVisit)
                      ?.orderSignature.includes(o.id),
                )
                .map((o) => ({
                  ...o,
                  number: `${o.number} · ${materials.data?.find((m) => m.id === o.materialId)?.uom || ""} · remaining ${o.quantity - o.receivedQuantity}`,
                }))}
            />
            <label>
              Delivery note
              <input
                name="deliveryNote"
                maxLength={160}
                required={policy.data?.requireDeliveryNote}
              />
            </label>
            <label>
              Supplier batch
              <input name="supplierBatch" required maxLength={160} />
            </label>
            <label>
              Internal batch number
              <input name="batchNumber" required maxLength={32} />
            </label>
            <label>
              Pack size
              <input
                type="number"
                min="0.000001"
                step="0.000001"
                required
                value={pack.size}
                onChange={(e) => setPack({ ...pack, size: e.target.value })}
              />
              <small>In the material's unit of measure.</small>
            </label>
            <label>
              Whole pack count
              <input
                type="number"
                min={1}
                max={1000000}
                step={1}
                required
                value={pack.count}
                onChange={(e) => setPack({ ...pack, count: e.target.value })}
              />
            </label>
            <label>
              Loose quantity
              <input
                type="number"
                min={0}
                step="0.000001"
                required
                value={pack.loose}
                onChange={(e) => setPack({ ...pack, loose: e.target.value })}
              />
            </label>
            <label>
              Packed expiry date
              <input name="expiryDate" type="date" />
            </label>
          </div>
          <div className="inbound-total">
            <span>Calculated receipt</span>
            <strong>{Number.isFinite(quantity) ? quantity : 0}</strong>
            <small>
              Pack size × whole packs + loose quantity. Must fit the order
              balance.
            </small>
          </div>
          <p className="form-note">
            Creates one receiving pallet on quality hold. Use separate receipts
            for different supplier batches. Mixed pack rows, invoice blind
            counting and weighbridge receiving are planned.
          </p>
        </Action>
      )}
      {section === "setup" && (
        <>
          <p className="form-note">
            Managers maintain receiving rules. Checklist changes apply to newly
            registered visits; existing visits retain their saved requirements.
          </p>
          <div className="inbound-input-grid">
            <Action
              key={String(policy.data?.version)}
              title="Save receiving rules"
              endpoint="receiving/policy"
              plantId={plantId}
              disabled={!manager || !policy.data}
              build={(f) => ({
                requireTruck: f.has("requireTruck"),
                requireDeliveryNote: f.has("requireDeliveryNote"),
              })}
            >
              <label className="inbound-checkbox">
                <input
                  name="requireTruck"
                  type="checkbox"
                  defaultChecked={policy.data?.requireTruck}
                />
                Require an unloading truck for every receipt
              </label>
              <label className="inbound-checkbox">
                <input
                  name="requireDeliveryNote"
                  type="checkbox"
                  defaultChecked={policy.data?.requireDeliveryNote}
                />
                Require a delivery note
              </label>
              <p className="form-note">
                These rules also protect the quick receipt API.
              </p>
            </Action>
            <Action
              title="Save dock"
              endpoint="receiving/docks"
              plantId={plantId}
              disabled={!manager}
              build={(f) => ({
                code: value(f, "code"),
                name: value(f, "name"),
                maintenance: f.has("maintenance"),
              })}
            >
              <label>
                Dock code
                <input name="code" required maxLength={32} />
              </label>
              <label>
                Dock name
                <input name="name" required maxLength={160} />
              </label>
              <label className="inbound-checkbox">
                <input name="maintenance" type="checkbox" />
                Under maintenance
              </label>
              <p className="form-note">
                Enter an existing code to update its name or maintenance state.
              </p>
            </Action>
            <Action
              title="Save checklist rule"
              endpoint="receiving/checks"
              plantId={plantId}
              disabled={!manager}
              build={(f) => ({
                prompt: value(f, "prompt"),
                stage: value(f, "stage"),
                required: f.has("required"),
                requirePass: f.has("requirePass"),
                active: f.has("active"),
              })}
            >
              <label>
                Check prompt
                <input name="prompt" required maxLength={160} />
              </label>
              <label>
                Gate
                <select name="stage" aria-label="Gate">
                  <option value="Arrival">Before dock assignment</option>
                  <option value="Unloading">Before unloading starts</option>
                </select>
              </label>
              <label className="inbound-checkbox">
                <input type="checkbox" name="required" defaultChecked />
                Answer required
              </label>
              <label className="inbound-checkbox">
                <input type="checkbox" name="requirePass" defaultChecked />
                Pass required
              </label>
              <label className="inbound-checkbox">
                <input type="checkbox" name="active" defaultChecked />
                Active for new visits
              </label>
              <p className="form-note">
                Same prompt and gate updates the rule.
              </p>
            </Action>
          </div>
          <section className="card inbound-action">
            <h2>Dock availability</h2>
            {allDocks.map((d) => (
              <p key={d.id}>
                <strong>
                  {d.code} · {d.name}
                </strong>{" "}
                —{" "}
                {d.maintenance
                  ? "Maintenance"
                  : active.some(
                        (v) =>
                          v.dockStationId === d.id && v.status === "Unloading",
                      )
                    ? "Unloading"
                    : "Available"}
              </p>
            ))}
            {!allDocks.length && <p>No docks configured.</p>}
            <h2>Checklist library</h2>
            {checks.data?.map((c) => (
              <p key={c.id}>
                {c.prompt} · {c.stage} · {c.active ? "Active" : "Inactive"} ·{" "}
                {c.required ? "Required" : "Optional"} ·{" "}
                {c.requirePass ? "Must pass" : "Result recorded"}
              </p>
            ))}
          </section>
        </>
      )}
    </div>
  );
}
