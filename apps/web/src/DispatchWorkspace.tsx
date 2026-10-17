import {
  useEffect,
  useRef,
  useState,
  type FormEvent,
  type ReactNode,
} from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowRight,
  Boxes,
  CalendarClock,
  Check,
  CheckCircle2,
  ClipboardList,
  Download,
  History,
  PackageCheck,
  Plus,
  Printer,
  RotateCcw,
  ScanLine,
  Search,
  ShieldCheck,
  Truck,
  Warehouse,
  X,
} from "lucide-react";
import { operationApi } from "./Operations";
import "./manufacturing.css";
import "./dispatch.css";
type Row = Record<string, any> & { id: string };
type Block = { items: Row[]; hasMore: boolean };
type Modal = { kind: string; row?: Row } | null;
const n = (v: unknown) =>
  Number(v || 0).toLocaleString(undefined, { maximumFractionDigits: 6 });
const date = (v: string) =>
  v
    ? new Date(v).toLocaleString(undefined, {
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      })
    : "—";
const json = (v: string) => {
  try {
    return JSON.parse(v);
  } catch {
    return {};
  }
};
const val = (f: FormData, k: string) => String(f.get(k) || "");
const localDate = (v?: string) => {
  const d = v ? new Date(v) : new Date();
  return new Date(d.getTime() - d.getTimezoneOffset() * 60000)
    .toISOString()
    .slice(0, 16);
};
function block(plantId: string, path: string) {
  return useQuery({
    queryKey: ["operations", plantId, "dispatch", path],
    queryFn: () => operationApi<Block>(`dispatch/${path}?plantId=${plantId}`),
    retry: false,
  });
}
function Badge({ children }: { children: ReactNode }) {
  return (
    <span
      className={`mfg-badge dispatch-badge ${String(children).toLowerCase()}`}
    >
      {children}
    </span>
  );
}
function Field({
  label,
  name,
  type = "text",
  value,
  required = true,
}: {
  label: string;
  name: string;
  type?: string;
  value?: string | number;
  required?: boolean;
}) {
  return (
    <label>
      {label}
      <input
        aria-label={label}
        name={name}
        type={type}
        defaultValue={value}
        required={required}
        step={type === "number" ? "0.000001" : undefined}
        maxLength={type === "text" ? 160 : undefined}
      />
    </label>
  );
}
function Select({
  label,
  name,
  rows,
  value,
  onChange,
  multiple = false,
}: {
  label: string;
  name: string;
  rows: Row[];
  value?: string;
  onChange?: (v: string) => void;
  multiple?: boolean;
}) {
  return (
    <label>
      {label}
      <select
        aria-label={label}
        name={name}
        required
        value={value}
        defaultValue={value === undefined ? (multiple ? [] : "") : undefined}
        multiple={multiple}
        onChange={onChange ? (e) => onChange(e.target.value) : undefined}
      >
        {!multiple && <option value="">Select {label.toLowerCase()}</option>}
        {rows.map((r) => (
          <option key={r.id} value={r.id}>
            {r.label ||
              r.number ||
              [r.code, r.name].filter(Boolean).join(" · ")}
          </option>
        ))}
      </select>
    </label>
  );
}
function Notes({
  label = "Notes",
  name = "notes",
  required = false,
  value = "",
}: {
  label?: string;
  name?: string;
  required?: boolean;
  value?: string;
}) {
  return (
    <label>
      {label}
      <textarea
        aria-label={label}
        name={name}
        rows={3}
        maxLength={2000}
        required={required}
        defaultValue={value}
      />
    </label>
  );
}
function Tick({
  label,
  name,
  required = false,
  checked = false,
}: {
  label: string;
  name: string;
  required?: boolean;
  checked?: boolean;
}) {
  return (
    <label className="mfg-check">
      <input
        name={name}
        type="checkbox"
        required={required}
        defaultChecked={checked}
      />
      <span>{label}</span>
    </label>
  );
}
function Button({
  children,
  onClick,
  disabled = false,
}: {
  children: ReactNode;
  onClick: () => void;
  disabled?: boolean;
}) {
  return (
    <button
      type="button"
      className="secondary"
      onClick={onClick}
      disabled={disabled}
    >
      {children}
    </button>
  );
}
function Empty({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="mfg-empty">
      <Truck size={32} />
      <h3>{title}</h3>
      <p>{children}</p>
    </div>
  );
}
function Dialog({
  title,
  wide = false,
  onClose,
  children,
}: {
  title: string;
  wide?: boolean;
  onClose: () => void;
  children: ReactNode;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const previous = document.activeElement as HTMLElement;
    ref.current?.showModal();
    return () => previous?.focus();
  }, []);
  return (
    <dialog
      className={`mfg-dialog dispatch-dialog ${wide ? "wide" : ""}`}
      ref={ref}
      aria-labelledby="dispatch-dialog-title"
      onCancel={onClose}
    >
      <header>
        <div>
          <span className="eyebrow">GREEN INK · OUTBOUND OPERATIONS</span>
          <h2 id="dispatch-dialog-title">{title}</h2>
        </div>
        <button
          type="button"
          aria-label="Close dialog"
          className="mfg-icon"
          onClick={onClose}
        >
          <X size={20} />
        </button>
      </header>
      {children}
    </dialog>
  );
}
function Action({
  title,
  path,
  plantId,
  build,
  disabled = false,
  onDone,
  children,
}: {
  title: string;
  path: string;
  plantId: string;
  build: (f: FormData) => object;
  disabled?: boolean;
  onDone?: (id: string) => void;
  children?: ReactNode;
}) {
  const cache = useQueryClient(),
    [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  const retry = useRef({ key: "", id: "" });
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setBusy(true);
    setError("");
    try {
      const input = { plantId, ...build(new FormData(e.currentTarget)) };
      const key = JSON.stringify({ path, input });
      if (retry.current.key !== key)
        retry.current = { key, id: crypto.randomUUID() };
      const result = await operationApi<{ resultId: string }>(
        `dispatch/${path}`,
        { ...input, commandId: retry.current.id },
      );
      retry.current = { key: "", id: "" };
      await cache.invalidateQueries({ queryKey: ["operations"] });
      onDone?.(result.resultId);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <form className="mfg-form" onSubmit={submit}>
      <fieldset disabled={busy || disabled}>{children}</fieldset>
      {error && (
        <p role="alert" className="mfg-error">
          {error}
        </p>
      )}
      <footer>
        <button className="primary" disabled={busy || disabled}>
          {busy ? "Saving…" : title}
        </button>
      </footer>
    </form>
  );
}
export function DispatchWorkspace({
  plantId,
  role,
  data,
  children,
}: {
  plantId: string;
  role: string;
  data: Record<string, any[]>;
  children: ReactNode;
}) {
  const manager = ["PlatformAdmin", "TenantAdmin", "PlantManager"].includes(
      role,
    ),
    writable = role !== "Viewer";
  const areas = block(plantId, "areas"),
    packages = block(plantId, "packages"),
    instructions = block(plantId, "instructions"),
    warehouses = block(plantId, "warehouses"),
    runs = block(plantId, "runs"),
    available = block(plantId, "available-deliveries");
  const operators = useQuery({
    queryKey: ["operations", plantId, "dispatch", "operators"],
    queryFn: () => operationApi<Row[]>(`dispatch/operators?plantId=${plantId}`),
    retry: false,
  });
  const [tab, setTab] = useState("Planning"),
    [modal, setModal] = useState<Modal>(null),
    [runId, setRunId] = useState(""),
    [search, setSearch] = useState(""),
    [day, setDay] = useState(""),
    [filter, setFilter] = useState("Active"),
    [label, setLabel] = useState<any>(null),
    [labelError, setLabelError] = useState("");
  const detail = useQuery({
    queryKey: ["operations", plantId, "dispatch-run", runId],
    queryFn: () =>
      operationApi<any>(`dispatch/runs/${runId}?plantId=${plantId}`),
    enabled: !!runId,
    retry: false,
  });
  const d = detail.data,
    run = d?.run,
    rows = (q: { data?: Block }) => q.data?.items || [],
    close = () => setModal(null),
    allRuns = rows(runs),
    customers = (data.partners || []).filter((p) => p.kind === "Customer"),
    materials = data.materials || [],
    terminal = run && ["Issued", "Reversed", "Cancelled"].includes(run.status),
    runName = (id: string) =>
      operators.data?.find((x) => x.id === id)?.name || "Assigned operator";
  const tabs = [
    "Planning",
    "Pick & pack",
    "Load & issue",
    "Handling units",
    "Setup",
    "Orders & shipments",
  ];
  const currentError = [
    areas,
    packages,
    instructions,
    warehouses,
    runs,
    operators,
  ].find((x) => x.error)?.error?.message;
  const metrics = [
    [
      "To pick",
      allRuns.filter((r) => ["Planned", "Picking"].includes(r.status)).length,
      ScanLine,
    ],
    [
      "To pack",
      allRuns.filter((r) => ["Picked", "Packing"].includes(r.status)).length,
      Boxes,
    ],
    [
      "To load",
      allRuns.filter((r) => ["Packed", "Loading", "Loaded"].includes(r.status))
        .length,
      Truck,
    ],
    [
      "Issued",
      allRuns.filter((r) => r.status === "Issued").length,
      CheckCircle2,
    ],
  ];
  function open(kind: string, row?: Row) {
    setModal({ kind, row });
  }
  function lineName(line: Row) {
    return (
      d?.materials.find((m: Row) => m.id === line.materialId)?.name ||
      materials.find((m) => m.id === line.materialId)?.name ||
      line.materialId
    );
  }
  function pallet(id: string) {
    return d?.pallets.find((p: Row) => p.id === id);
  }
  function quantity(line: Row) {
    return d.picks
      .filter((p: Row) => p.dispatchRunLineId === line.id)
      .reduce((a: number, p: Row) => a + p.quantity, 0);
  }
  const action = (
    title: string,
    path: string,
    build: (f: FormData) => object,
    disabled = false,
  ) => (
    <Action
      title={title}
      path={path}
      plantId={plantId}
      build={build}
      disabled={disabled}
    />
  );
  async function preview(unit: Row) {
    setLabel(null);
    setLabelError("");
    open("label", unit);
    try {
      setLabel(
        await operationApi(
          `dispatch/units/${unit.id}/label?plantId=${plantId}`,
        ),
      );
    } catch (e) {
      setLabelError((e as Error).message);
    }
  }
  function exportHistory() {
    const safe = (v: any) => {
      const s = String(v ?? "");
      return (
        '"' + (/^[\s=+\-@]/.test(s) ? "'" : "") + s.replaceAll('"', '""') + '"'
      );
    };
    const csv = [
      ["Time", "Event", "Operator", "Notes"],
      ...(d?.events || []).map((e: Row) => [
        e.createdAt,
        e.kind,
        d.actors.find((a: Row) => a.id === e.actorId)?.name || e.actorId,
        e.notes,
      ]),
    ]
      .map((r) => r.map(safe).join(","))
      .join("\r\n");
    const url = URL.createObjectURL(
      new Blob([csv], { type: "text/csv;charset=utf-8" }),
    );
    const a = document.createElement("a");
    a.href = url;
    a.download = "GreenInk-dispatch-history.csv";
    a.click();
    URL.revokeObjectURL(url);
  }
  const selectRun = (id: string, next = "Pick & pack") => {
    setRunId(id);
    setTab(next);
  };
  return (
    <section className="mfg-workspace dispatch-workspace">
      <div className="dispatch-hero">
        <div>
          <span className="eyebrow">OUTBOUND, UNDER CONTROL</span>
          <h2>
            From ready stock
            <br />
            to the right destination.
          </h2>
          <p>
            Plan the load. Verify every pick. Keep the whole journey connected.
          </p>
          <div className="dispatch-hero-flow">
            <span>
              <ScanLine size={15} />
              Pick
            </span>
            <ArrowRight size={14} />
            <span>
              <PackageCheck size={15} />
              Pack
            </span>
            <ArrowRight size={14} />
            <span>
              <Truck size={15} />
              Dispatch
            </span>
          </div>
        </div>
        <div className="dispatch-hero-icon">
          <Truck size={64} />
        </div>
      </div>
      {currentError && (
        <p className="mfg-error" role="alert">
          {currentError}
        </p>
      )}
      <div className="mfg-metrics">
        {metrics.map(([title, value, Icon]: any) => (
          <article key={title}>
            <Icon size={21} />
            <span>{title}</span>
            <strong>{value}</strong>
            <small>In loaded dispatch records</small>
          </article>
        ))}
      </div>
      <nav className="mfg-tabs" aria-label="Dispatch workspaces">
        {tabs.map((t) => (
          <button
            key={t}
            role="tab"
            aria-selected={tab === t}
            onClick={() => setTab(t)}
          >
            {t}
          </button>
        ))}
      </nav>
      {[areas, packages, instructions, warehouses, runs].some(
        (q) => q.data?.hasMore,
      ) && (
        <p className="mfg-note">
          Lists contain up to 200 records. Counts describe the loaded records.
        </p>
      )}
      {tab === "Orders & shipments" && (
        <div className="mfg-existing">{children}</div>
      )}
      {tab === "Planning" && (
        <>
          <div className="mfg-toolbar">
            <div>
              <h2>Dispatch planner</h2>
              <p>Schedule customer shipments and local warehouse transfers.</p>
            </div>
            <button
              className="primary"
              disabled={!manager}
              onClick={() => open("plan")}
            >
              <Plus size={17} />
              Plan dispatch
            </button>
          </div>
          <div className="dispatch-filters">
            <label className="mfg-search">
              <Search size={17} />
              <input
                aria-label="Search dispatch plans"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search dispatch, route or operator"
              />
            </label>
            <label className="dispatch-date">
              Dispatch date
              <input
                type="date"
                aria-label="Filter dispatch date"
                value={day}
                onChange={(e) => setDay(e.target.value)}
              />
            </label>
            <select
              aria-label="Dispatch status filter"
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
            >
              <option>Active</option>
              <option>All</option>
              {[
                "Planned",
                "Picking",
                "Picked",
                "Packing",
                "Packed",
                "Loading",
                "Loaded",
                "Issued",
                "Reversed",
                "Cancelled",
              ].map((x) => (
                <option key={x}>{x}</option>
              ))}
            </select>
          </div>
          <div className="dispatch-plan-grid">
            {allRuns
              .filter(
                (r) =>
                  (filter === "All" || filter === "Active"
                    ? !["Issued", "Reversed", "Cancelled"].includes(r.status) ||
                      filter === "All"
                    : r.status === filter) &&
                  (!day || localDate(r.scheduledAt).startsWith(day)) &&
                  JSON.stringify({ ...r, operator: runName(r.assignedToId) })
                    .toLowerCase()
                    .includes(search.toLowerCase()),
              )
              .map((r) => (
                <button
                  className="dispatch-plan"
                  key={r.id}
                  onClick={() => selectRun(r.id)}
                >
                  <div className="mfg-row">
                    <span className="dispatch-kind">
                      {r.kind === "Shuttle" ? (
                        <Warehouse size={16} />
                      ) : (
                        <Truck size={16} />
                      )}{" "}
                      {r.kind === "Shuttle"
                        ? "Warehouse transfer"
                        : "Customer shipment"}
                    </span>
                    <Badge>{r.status}</Badge>
                  </div>
                  <h3>{r.number}</h3>
                  <p>
                    {date(r.scheduledAt)}{" "}
                    {r.priority && (
                      <b className="dispatch-priority">Priority</b>
                    )}
                  </p>
                  <p>
                    {runName(r.assignedToId)} ·{" "}
                    {r.route || "Route not specified"}
                  </p>
                  <div className="dispatch-plan-bottom">
                    <span>
                      {rows(areas).find((a) => a.id === r.dispatchAreaId)?.name}
                    </span>
                    <ArrowRight size={18} />
                  </div>
                </button>
              ))}
          </div>
          {!allRuns.length && (
            <Empty title="The next delivery starts here">
              Create delivery orders in Orders & shipments, configure the
              dispatch area and package types, then plan the shipment.
            </Empty>
          )}
        </>
      )}
      {["Pick & pack", "Load & issue", "Handling units"].includes(tab) && (
        <>
          <div className="mfg-toolbar">
            <div>
              <h2>
                {tab === "Pick & pack"
                  ? "Dispatch execution"
                  : tab === "Load & issue"
                    ? "Loading & goods issue"
                    : "Handling units"}
              </h2>
              <p>
                Selected dispatch:{" "}
                {run?.number || "Choose a dispatch to continue"}
              </p>
            </div>
            <Select
              label="Select dispatch"
              name="selectRun"
              value={runId}
              onChange={setRunId}
              rows={allRuns}
            />
          </div>
          {detail.error ? (
            <p role="alert" className="mfg-error">
              {detail.error.message}
            </p>
          ) : !d ? (
            <Empty title="One dispatch, one clear flow">
              Select a dispatch plan above or choose one from Planning.
            </Empty>
          ) : (
            <>
              <div className="dispatch-summary">
                <div>
                  <span className="eyebrow">
                    {d.area.shippingPoint} ·{" "}
                    {run.kind === "Shuttle"
                      ? "LOCAL WAREHOUSE TRANSFER"
                      : "CUSTOMER DISPATCH"}
                  </span>
                  <h2>{run.number}</h2>
                  <p>
                    {runName(run.assignedToId)} · {date(run.scheduledAt)} ·{" "}
                    {run.route || "No route specified"}
                  </p>
                </div>
                <div className="dispatch-summary-actions">
                  <Badge>{run.status}</Badge>
                  <Button onClick={() => open("history")}>
                    <History size={16} />
                    History
                  </Button>
                  <Button onClick={() => open("manifest")}>
                    <ClipboardList size={16} />
                    Pick list / manifest
                  </Button>
                  {manager && !terminal && (
                    <Button onClick={() => open("assignment")}>
                      Reassign / schedule
                    </Button>
                  )}
                </div>
              </div>
              <div className="dispatch-steps">
                {[
                  "Planned",
                  "Picking",
                  "Picked",
                  "Packing",
                  "Packed",
                  "Loading",
                  "Loaded",
                  "Issued",
                ].map((s, i) => (
                  <div
                    className={
                      s === run.status
                        ? "current"
                        : [
                              "Planned",
                              "Picking",
                              "Picked",
                              "Packing",
                              "Packed",
                              "Loading",
                              "Loaded",
                              "Issued",
                            ].indexOf(run.status) > i
                          ? "done"
                          : ""
                    }
                    key={s}
                  >
                    <span>
                      {s === run.status ? <Truck size={13} /> : i + 1}
                    </span>
                    {s}
                  </div>
                ))}
              </div>
              {run.kind === "Shuttle" && (
                <div className="dispatch-external">
                  <Warehouse size={20} />
                  <div>
                    <b>{d.destination?.name}</b>
                    <p>
                      Receiving plant {d.destination?.receivingPlant} · storage{" "}
                      {d.destination?.storageLocation} · gate{" "}
                      {d.destination?.gate}. Local stock posting only; external
                      receipt and SAP confirmation are not connected.
                    </p>
                  </div>
                </div>
              )}
              {tab === "Pick & pack" && (
                <>
                  <div className="mfg-toolbar">
                    <h3>Delivery requirements</h3>
                    <div className="mfg-row">
                      {run.status === "Planned" && (
                        <Button
                          disabled={!writable}
                          onClick={() => open("start-pick")}
                        >
                          Start picking
                        </Button>
                      )}
                      {run.status === "Picking" && (
                        <>
                          <Button
                            disabled={!writable}
                            onClick={() => open("pick", d.lines[0])}
                          >
                            <ScanLine size={16} />
                            Confirm pick
                          </Button>
                          <Button
                            disabled={!writable}
                            onClick={() => open("finish-pick")}
                          >
                            Complete picking
                          </Button>
                        </>
                      )}
                      {run.status === "Picked" && (
                        <Button
                          disabled={!writable}
                          onClick={() => open("start-pack")}
                        >
                          Start packing
                        </Button>
                      )}
                      {run.status === "Packing" && (
                        <>
                          <Button
                            disabled={!writable}
                            onClick={() => open("unit")}
                          >
                            <Plus size={16} />
                            Open handling unit
                          </Button>
                          <Button
                            disabled={!writable}
                            onClick={() => open("finish-pack")}
                          >
                            Complete packing
                          </Button>
                        </>
                      )}
                    </div>
                  </div>
                  <div className="dispatch-lines">
                    {d.lines.map((line: Row) => {
                      const instruction = json(line.instructionsJson),
                        material = d.materials.find(
                          (m: Row) => m.id === line.materialId,
                        );
                      return (
                        <article key={line.id}>
                          <div className="mfg-row">
                            <div>
                              <span className="eyebrow">
                                {d.deliveries.find(
                                  (o: Row) => o.id === line.deliveryOrderId,
                                )?.number || "TRANSFER LINE"}
                              </span>
                              <h3>{lineName(line)}</h3>
                              <p>
                                {d.customers.find(
                                  (c: Row) => c.id === line.customerId,
                                )?.name || d.destination?.name}
                              </p>
                            </div>
                            <div className="dispatch-line-quantity">
                              <b>
                                {n(quantity(line))} / {n(line.quantity)}
                              </b>
                              <small>{material?.uom} picked</small>
                            </div>
                          </div>
                          <div className="mfg-progress">
                            <span
                              style={{
                                width: `${Math.min(100, (100 * quantity(line)) / line.quantity)}%`,
                              }}
                            />
                          </div>
                          <details>
                            <summary>
                              Picking, packing & shipping instructions
                            </summary>
                            {["picking", "packing", "shipping", "safety"].map(
                              (k) => (
                                <p key={k}>
                                  <b>{k[0].toUpperCase() + k.slice(1)}:</b>{" "}
                                  {instruction[k] ||
                                    "No additional instructions"}
                                </p>
                              ),
                            )}
                            <p>
                              Minimum shelf life:{" "}
                              {instruction.minimumShelfDays || 0} days{" "}
                              {instruction.productPacking
                                ? " · Product-by-product validation"
                                : ""}
                            </p>
                          </details>
                        </article>
                      );
                    })}
                  </div>
                  <h3>Picked allocations</h3>
                  <div className="dispatch-picks">
                    {d.picks.map((p: Row) => (
                      <article key={p.id}>
                        <div>
                          <b>{pallet(p.pickedPalletId)?.code}</b>
                          <p>
                            {n(p.quantity)}{" "}
                            {
                              d.materials.find(
                                (m: Row) =>
                                  m.id ===
                                  d.lines.find(
                                    (l: Row) => l.id === p.dispatchRunLineId,
                                  )?.materialId,
                              )?.uom
                            }{" "}
                            ·{" "}
                            {
                              d.batches.find(
                                (b: Row) =>
                                  b.id === pallet(p.pickedPalletId)?.batchId,
                              )?.number
                            }
                          </p>
                          <small>
                            {pallet(p.pickedPalletId)?.location} · Returned{" "}
                            {n(p.returnedQuantity)}
                          </small>
                        </div>
                        <div>
                          <Badge>{p.status}</Badge>
                          {manager &&
                            !terminal &&
                            p.status === "Picked" &&
                            p.quantity > 0 && (
                              <Button onClick={() => open("return", p)}>
                                <RotateCcw size={15} />
                                Return pick
                              </Button>
                            )}
                        </div>
                      </article>
                    ))}
                  </div>
                  {run.status === "Packing" && (
                    <Units
                      d={d}
                      writable={writable}
                      manager={manager}
                      open={open}
                      preview={preview}
                    />
                  )}
                  {manager && !terminal && (
                    <div className="dispatch-cancel">
                      <p>
                        To cancel: unload, unpack and return all allocated stock
                        first.
                      </p>
                      <Button onClick={() => open("cancel")}>
                        Cancel dispatch
                      </Button>
                    </div>
                  )}
                </>
              )}
              {tab === "Load & issue" && (
                <>
                  <div className="dispatch-posting-note">
                    <ShieldCheck size={20} />
                    <p>
                      Goods issue updates Green Ink stock and local delivery
                      history. SAP posting and destination receipt are not
                      connected.
                    </p>
                  </div>
                  <div className="mfg-row">
                    {run.status === "Packed" && (
                      <Button
                        disabled={!writable}
                        onClick={() => open("truck")}
                      >
                        <Truck size={16} />
                        Start truck loading
                      </Button>
                    )}
                    {run.status === "Loading" &&
                      action(
                        "Complete truck loading",
                        `runs/${run.id}/loading-complete`,
                        () => ({}),
                        !writable,
                      )}
                    {run.status === "Loaded" && (
                      <button
                        className="primary"
                        disabled={!manager}
                        onClick={() => open("issue")}
                      >
                        <CheckCircle2 size={16} />
                        Post local goods issue
                      </button>
                    )}
                    {run.status === "Issued" && (
                      <Button
                        disabled={!manager}
                        onClick={() => open("reverse")}
                      >
                        <RotateCcw size={16} />
                        Reverse local goods issue
                      </Button>
                    )}
                  </div>
                  {run.truck && (
                    <div className="dispatch-truck">
                      <Truck />
                      <div>
                        <b>{run.truck}</b>
                        <p>{run.carrier}</p>
                      </div>
                      {run.goodsIssueReference && (
                        <code>{run.goodsIssueReference}</code>
                      )}
                    </div>
                  )}
                  <Units
                    d={d}
                    writable={writable}
                    manager={manager}
                    open={open}
                    preview={preview}
                  />
                  {run.status === "Reversed" && (
                    <div className="mfg-warning">
                      Goods issue was reversed. Returned stock is on quality
                      hold in the packing bin. Review Quality before reuse.
                    </div>
                  )}
                </>
              )}
              {tab === "Handling units" && (
                <Units
                  d={d}
                  writable={writable}
                  manager={manager}
                  open={open}
                  preview={preview}
                />
              )}
            </>
          )}
        </>
      )}
      {tab === "Setup" && (
        <div className="dispatch-setup">
          {[
            [
              "areas",
              "Dispatch areas",
              "Configure shipping point, bins and pick/pack policies.",
              areas,
            ],
            [
              "instructions",
              "Customer instructions",
              "Keep picking, packing, shipping and safety requirements together.",
              instructions,
            ],
            [
              "packages",
              "Package types",
              "Record package dimensions, tare, gross limits and device references.",
              packages,
            ],
            [
              "warehouses",
              "External warehouses",
              "Define the destination for a local shuttle transfer.",
              warehouses,
            ],
          ].map(([kind, title, desc, q]: any) => (
            <section className="mfg-tile" key={kind}>
              <div className="mfg-row">
                <h3>{title}</h3>
                <Button disabled={!manager} onClick={() => open(kind)}>
                  <Plus size={16} />
                  Add
                </Button>
              </div>
              <p>{desc}</p>
              {rows(q).map((r: Row) => (
                <div className="dispatch-master" key={r.id}>
                  <div>
                    <b>
                      {r.name ||
                        customers.find((c) => c.id === r.customerId)?.name}
                    </b>
                    <small>
                      {r.code || r.picking || "Customer instructions"}
                    </small>
                  </div>
                  {kind === "instructions" ? (
                    <Button disabled={!manager} onClick={() => open(kind, r)}>
                      Edit
                    </Button>
                  ) : (
                    <Badge>
                      {kind === "areas"
                        ? `${r.pickMode} / ${r.packMode}`
                        : kind === "packages"
                          ? `${n(r.maximumGrossKg)} kg max`
                          : r.receivingPlant}
                    </Badge>
                  )}
                </div>
              ))}
            </section>
          ))}
        </div>
      )}
      {modal && (
        <Dialog
          title={
            (
              {
                plan: "Plan a dispatch",
                assignment: "Reassign or reschedule",
                areas: "Create dispatch area",
                instructions: "Customer dispatch instructions",
                packages: "Create package type",
                warehouses: "External warehouse destination",
                pick: "Scan & confirm picks",
                unit: "Open handling unit",
                pack: "Check and pack allocations",
                return: "Return picked stock",
                truck: "Start truck loading",
                issue: "Confirm local goods issue",
                reverse: "Reverse local goods issue",
                history: "Dispatch activity",
                manifest: "Pick list & shipment manifest",
                label: "Handling-unit label",
                "start-pick": "Start picking",
                "finish-pick": "Complete picking",
                "start-pack": "Start packing",
                "finish-pack": "Complete packing",
                cancel: "Cancel dispatch",
                Close: "Close handling unit",
                Load: "Load handling unit",
                Unload: "Unload handling unit",
                Unpack: "Unpack handling unit",
              } as Record<string, string>
            )[modal.kind] || "Dispatch action"
          }
          onClose={close}
          wide={["plan", "pick", "pack", "history", "manifest"].includes(
            modal.kind,
          )}
        >
          {modal.kind === "plan" && (
            <PlanEditor
              plantId={plantId}
              areas={rows(areas)}
              warehouses={rows(warehouses)}
              operators={operators.data || []}
              orders={rows(available)}
              materials={materials.filter((m) => m.kind === "Finished")}
              onDone={(id) => {
                setRunId(id);
                setTab("Pick & pack");
                close();
              }}
            />
          )}
          {modal.kind === "areas" && (
            <Action
              title="Save dispatch area"
              path="areas"
              plantId={plantId}
              onDone={close}
              build={(f) => ({
                code: val(f, "code"),
                name: val(f, "name"),
                shippingPoint: val(f, "point"),
                packBinId: val(f, "pack"),
                loadingBinId: val(f, "load"),
                pickMode: val(f, "picking"),
                packMode: val(f, "packing"),
                allowOverpick: f.has("overpick"),
              })}
            >
              <p className="mfg-note">
                Area policies remain fixed for planned dispatches. Use a new
                area code to revise them.
              </p>
              <div className="mfg-fields">
                <Field label="Dispatch area code" name="code" />
                <Field label="Dispatch area name" name="name" />
                <Field label="Shipping point" name="point" />
                <Select
                  label="Packing bin"
                  name="pack"
                  rows={data.bins || []}
                />
                <Select
                  label="Loading bin"
                  name="load"
                  rows={data.bins || []}
                />
                <label>
                  Picking mode
                  <select aria-label="Picking mode" name="picking">
                    <option value="Single">Single delivery line</option>
                    <option value="Group">Group picking</option>
                  </select>
                </label>
                <label>
                  Packing mode
                  <select aria-label="Packing mode" name="packing">
                    <option value="Product">Product by product</option>
                    <option value="Collective">Collective validation</option>
                  </select>
                </label>
              </div>
              <Tick
                label="Allow overpicking; excess must be returned before completion"
                name="overpick"
              />
            </Action>
          )}
          {modal.kind === "instructions" && (
            <Action
              title="Save customer instructions"
              path="instructions"
              plantId={plantId}
              onDone={close}
              build={(f) => ({
                customerId: val(f, "customer"),
                picking: val(f, "picking"),
                packing: val(f, "packing"),
                shipping: val(f, "shipping"),
                safety: val(f, "safety"),
                minimumShelfDays: Number(f.get("shelf")),
                productPacking: f.has("product"),
                version: modal.row?.version || 0,
              })}
            >
              <Select
                label="Instruction customer"
                name="customer"
                rows={customers}
                value={modal.row?.customerId}
              />
              {["Picking", "Packing", "Shipping", "Safety"].map((k) => (
                <Notes
                  key={k}
                  label={`${k} instructions`}
                  name={k.toLowerCase()}
                  value={modal.row?.[k.toLowerCase()] || ""}
                />
              ))}
              <Field
                label="Minimum remaining shelf life (days)"
                name="shelf"
                type="number"
                value={modal.row?.minimumShelfDays || 0}
              />
              <Tick
                label="Always validate product by product for this customer"
                name="product"
                checked={modal.row?.productPacking}
              />
            </Action>
          )}
          {modal.kind === "packages" && (
            <Action
              title="Save package type"
              path="packages"
              plantId={plantId}
              onDone={close}
              build={(f) => ({
                code: val(f, "code"),
                name: val(f, "name"),
                lengthCm: Number(f.get("length")),
                widthCm: Number(f.get("width")),
                heightCm: Number(f.get("height")),
                tareKg: Number(f.get("tare")),
                maximumGrossKg: Number(f.get("max")),
                temperatureDeviceRequired: f.has("device"),
              })}
            >
              <div className="mfg-fields">
                <Field label="Package code" name="code" />
                <Field label="Package name" name="name" />
                <Field label="Length (cm)" name="length" type="number" />
                <Field label="Width (cm)" name="width" type="number" />
                <Field label="Height (cm)" name="height" type="number" />
                <Field label="Tare (kg)" name="tare" type="number" value="0" />
                <Field
                  label="Maximum gross weight (kg)"
                  name="max"
                  type="number"
                />
              </div>
              <Tick
                label="Require temperature-device serial reference"
                name="device"
              />
              <p className="mfg-note">
                The serial is a recorded reference. Live temperature telemetry
                is not connected.
              </p>
            </Action>
          )}
          {modal.kind === "warehouses" && (
            <Action
              title="Save external destination"
              path="warehouses"
              plantId={plantId}
              onDone={close}
              build={(f) => ({
                code: val(f, "code"),
                name: val(f, "name"),
                receivingPlant: val(f, "plant"),
                storageLocation: val(f, "storage"),
                gate: val(f, "gate"),
              })}
            >
              <Field label="External warehouse code" name="code" />
              <Field label="External warehouse name" name="name" />
              <Field label="Receiving plant code" name="plant" />
              <Field label="Destination storage location" name="storage" />
              <Field label="External dispatch gate" name="gate" />
            </Action>
          )}
          {modal.kind === "assignment" && (
            <Action
              title="Save dispatch assignment"
              path={`runs/${run.id}/assignment`}
              plantId={plantId}
              onDone={close}
              build={(f) => ({
                assignedToId: val(f, "operator"),
                scheduledAt: new Date(val(f, "scheduled")).toISOString(),
                priority: f.has("priority"),
                reason: val(f, "reason"),
                version: run.version,
              })}
            >
              <Select
                label="New assigned operator"
                name="operator"
                rows={operators.data || []}
              />
              <Field
                label="Revised dispatch date"
                name="scheduled"
                type="datetime-local"
                value={localDate(run.scheduledAt)}
              />
              <Tick
                label="High priority"
                name="priority"
                checked={run.priority}
              />
              <Notes label="Reassignment reason" name="reason" required />
            </Action>
          )}
          {[
            "start-pick",
            "finish-pick",
            "start-pack",
            "finish-pack",
            "cancel",
          ].includes(modal.kind) && (
            <Action
              title={
                (
                  {
                    "start-pick": "Confirm picking start",
                    "finish-pick": "Confirm picking complete",
                    "start-pack": "Confirm packing start",
                    "finish-pack": "Confirm packing complete",
                    cancel: "Confirm dispatch cancellation",
                  } as Record<string, string>
                )[modal.kind]
              }
              path={`runs/${run.id}/state`}
              plantId={plantId}
              onDone={close}
              build={(f) => ({
                action: (
                  {
                    "start-pick": "StartPicking",
                    "finish-pick": "CompletePicking",
                    "start-pack": "StartPacking",
                    "finish-pack": "CompletePacking",
                    cancel: "Cancel",
                  } as Record<string, string>
                )[modal.kind],
                confirmed: f.has("confirm"),
                notes: val(f, "notes"),
              })}
            >
              <Instructions
                lines={d.lines}
                stage={modal.kind.includes("pack") ? "packing" : "picking"}
              />
              <Tick
                label="I confirm the action and applicable instructions"
                name="confirm"
                required
              />
              <Notes
                label={
                  modal.kind === "cancel"
                    ? "Cancellation reason"
                    : "Operator notes"
                }
                required={modal.kind === "cancel"}
              />
            </Action>
          )}
          {modal.kind === "pick" && (
            <PickEditor
              plantId={plantId}
              d={d}
              initialLine={modal.row?.id}
              onDone={close}
            />
          )}
          {modal.kind === "return" && (
            <Action
              title="Confirm pick return"
              path={`picks/${modal.row!.id}/return`}
              plantId={plantId}
              onDone={close}
              build={(f) => ({
                quantity: Number(f.get("quantity")),
                palletCode: val(f, "pallet"),
                destinationBin: val(f, "bin"),
                reason: val(f, "reason"),
              })}
            >
              <p className="mfg-note">
                Picked label {pallet(modal.row!.pickedPalletId)?.code}
                <br />
                Return to original source{" "}
                {pallet(modal.row!.sourcePalletId)?.code} in{" "}
                {pallet(modal.row!.sourcePalletId)?.location}.
              </p>
              <Field
                label="Returned quantity"
                name="quantity"
                type="number"
                value={modal.row!.quantity}
              />
              <Field label="Scan returned picked pallet" name="pallet" />
              <Field label="Scan original-source bin" name="bin" />
              <Notes label="Return reason" name="reason" required />
            </Action>
          )}
          {modal.kind === "unit" && (
            <Action
              title="Create handling unit"
              path={`runs/${run.id}/units`}
              plantId={plantId}
              onDone={close}
              build={(f) => ({
                dispatchPackageId: val(f, "package"),
                temperatureDevice: val(f, "device"),
              })}
            >
              <Select
                label="Handling-unit package type"
                name="package"
                rows={rows(packages)}
              />
              <Field
                label="Temperature-device serial"
                name="device"
                required={false}
              />
              <p className="mfg-note">
                Serial reference is mandatory for package types configured to
                require one. IDs are local handling-unit codes, not GS1 SSCC
                numbers.
              </p>
            </Action>
          )}
          {modal.kind === "pack" && (
            <PackEditor
              plantId={plantId}
              d={d}
              unit={modal.row!}
              onDone={close}
            />
          )}
          {["Close", "Load", "Unload", "Unpack"].includes(modal.kind) && (
            <Action
              title={`Confirm ${modal.kind.toLowerCase()}`}
              path={`units/${modal.row!.id}/state`}
              plantId={plantId}
              onDone={close}
              build={(f) => ({
                action: modal.kind,
                unitCode: val(f, "scan"),
                grossKg: modal.kind === "Close" ? Number(f.get("gross")) : null,
                notes: val(f, "notes"),
              })}
            >
              <p className="mfg-note">Handling unit {modal.row!.code}</p>
              <Field label="Scan handling-unit code" name="scan" />
              {modal.kind === "Close" && (
                <Field
                  label="Actual gross weight (kg)"
                  name="gross"
                  type="number"
                />
              )}
              <Notes
                label={
                  ["Unload", "Unpack"].includes(modal.kind)
                    ? "Reversal reason"
                    : "Handling notes"
                }
                required={["Unload", "Unpack"].includes(modal.kind)}
              />
            </Action>
          )}
          {modal.kind === "truck" && (
            <Action
              title="Confirm truck loading"
              path={`runs/${run.id}/truck`}
              plantId={plantId}
              onDone={close}
              build={(f) => ({
                truck: val(f, "truck"),
                carrier: val(f, "carrier"),
                instructionsConfirmed: f.has("instructions"),
              })}
            >
              <Instructions lines={d.lines} stage="shipping" />
              <Field label="Loading truck registration" name="truck" />
              <Field label="Carrier name" name="carrier" />
              <Tick
                label="Shipping instructions checked before loading"
                name="instructions"
                required
              />
            </Action>
          )}
          {["issue", "reverse"].includes(modal.kind) && (
            <Action
              title={
                modal.kind === "issue"
                  ? "Confirm local goods issue"
                  : "Confirm local reversal"
              }
              path={`runs/${run.id}/${modal.kind === "issue" ? "goods-issue" : "reverse"}`}
              plantId={plantId}
              onDone={close}
              build={(f) => ({
                action: modal.kind,
                confirmed: f.has("confirmed"),
                notes: val(f, "notes"),
              })}
            >
              <div className="mfg-warning">
                {modal.kind === "issue"
                  ? "This posts the loaded quantities out of Green Ink stock. No SAP posting or external receipt confirmation will be sent."
                  : "Confirm that all issued stock has physically returned. Quantities will be restored to the packing bin on quality hold. Original issue history remains intact; no external ERP reversal is sent."}
              </div>
              <Tick
                label={
                  modal.kind === "issue"
                    ? "I confirm the loaded stock is being dispatched"
                    : "I confirm all stock has physically returned"
                }
                name="confirmed"
                required
              />
              <Notes
                label={
                  modal.kind === "reverse"
                    ? "Goods-issue reversal reason"
                    : "Goods-issue notes"
                }
                required={modal.kind === "reverse"}
              />
            </Action>
          )}
          {modal.kind === "history" && (
            <div className="mfg-modal-content">
              <div className="mfg-row">
                <p>Activity for {run.number}</p>
                <Button onClick={exportHistory}>
                  <Download size={16} />
                  Export shown activity
                </Button>
              </div>
              {d.events.map((e: Row) => (
                <article key={e.id} className="mfg-event">
                  <span />
                  <div>
                    <h3>{e.kind.replace(/([a-z])([A-Z])/g, "$1 $2")}</h3>
                    <p>{e.notes}</p>
                    <small>
                      {date(e.createdAt)} ·{" "}
                      {d.actors.find((a: Row) => a.id === e.actorId)?.name ||
                        e.actorId}
                    </small>
                  </div>
                </article>
              ))}
              {d.eventsHasMore && (
                <p>Only the latest 200 events are shown and exported.</p>
              )}
            </div>
          )}
          {modal.kind === "manifest" && (
            <div className="mfg-modal-content">
              <div className="dispatch-manifest">
                <h2>GREEN INK · DISPATCH</h2>
                <h3>{run.number}</h3>
                <p>
                  {run.kind} · {run.route} · {date(run.scheduledAt)}
                </p>
                <p>
                  {run.truck} · {run.carrier}
                </p>
                {d.lines.map((l: Row) => (
                  <div key={l.id}>
                    <b>{lineName(l)}</b>
                    <p>
                      Required {n(l.quantity)}{" "}
                      {d.materials.find((m: Row) => m.id === l.materialId)?.uom}{" "}
                      · Picked {n(quantity(l))}
                    </p>
                    {d.picks
                      .filter((p: Row) => p.dispatchRunLineId === l.id)
                      .map((p: Row) => (
                        <small key={p.id}>
                          {pallet(p.pickedPalletId)?.code} · {n(p.quantity)} ·{" "}
                          {p.status}
                          <br />
                        </small>
                      ))}
                  </div>
                ))}
                <p>
                  Local operational document. Not an invoice, carrier booking or
                  ERP confirmation.
                </p>
              </div>
              <Button onClick={() => window.print()}>
                <Printer size={16} />
                Print pick list / manifest
              </Button>
            </div>
          )}
          {modal.kind === "label" && (
            <div className="mfg-modal-content">
              {labelError ? (
                <p role="alert">{labelError}</p>
              ) : !label ? (
                <p>Loading local label…</p>
              ) : (
                <>
                  <div className="mfg-label">
                    <h3>GREEN INK · HANDLING UNIT</h3>
                    <div dangerouslySetInnerHTML={{ __html: label.qrSvg }} />
                    <strong>{label.unit.code}</strong>
                    <p>{label.runNumber}</p>
                    <p>{json(label.unit.packageJson).name}</p>
                    <p>
                      {label.unit.grossKg
                        ? n(label.unit.grossKg) + " kg gross"
                        : "Weight not yet recorded"}
                    </p>
                    {label.unit.temperatureDevice && (
                      <p>Device reference {label.unit.temperatureDevice}</p>
                    )}
                    <p>{label.labelType}</p>
                  </div>
                  <p className="mfg-note">
                    Local browser print preview. No physical printer delivery or
                    ERP acknowledgment is claimed.
                  </p>
                  <Button onClick={() => window.print()}>
                    <Printer size={16} />
                    Print handling-unit preview
                  </Button>
                </>
              )}
            </div>
          )}
        </Dialog>
      )}
    </section>
  );
}
function Instructions({ lines, stage }: { lines: Row[]; stage: string }) {
  const notes = [
    ...new Set(
      lines.map((l) => json(l.instructionsJson)[stage]).filter(Boolean),
    ),
  ] as string[];
  return (
    <div className="dispatch-instructions">
      <b>{stage[0].toUpperCase() + stage.slice(1)} instructions</b>
      {notes.length ? (
        notes.map((t) => <p key={t}>{t}</p>)
      ) : (
        <p>No additional instructions recorded for these lines.</p>
      )}
    </div>
  );
}
function Units({
  d,
  writable,
  manager,
  open,
  preview,
}: {
  d: any;
  writable: boolean;
  manager: boolean;
  open: (kind: string, row?: Row) => void;
  preview: (row: Row) => void;
}) {
  return (
    <div className="dispatch-units">
      {d.units.map((u: Row) => (
        <article key={u.id}>
          <div className="mfg-row">
            <PackageCheck size={23} />
            <Badge>{u.status}</Badge>
          </div>
          <h3>{u.code}</h3>
          <p>{json(u.packageJson).name}</p>
          <small>
            {
              d.picks.filter(
                (p: Row) => p.dispatchHandlingUnitId === u.id && p.quantity > 0,
              ).length
            }{" "}
            allocation(s) ·{" "}
            {u.grossKg ? n(u.grossKg) + " kg gross" : "Weight pending"}
          </small>
          <div className="mfg-row">
            {u.status === "Open" && d.run.status === "Packing" && (
              <>
                <Button disabled={!writable} onClick={() => open("pack", u)}>
                  Pack allocations
                </Button>
                <Button disabled={!writable} onClick={() => open("Close", u)}>
                  Close unit
                </Button>
              </>
            )}
            {u.status === "Closed" && d.run.status === "Loading" && (
              <Button disabled={!writable} onClick={() => open("Load", u)}>
                Scan and load
              </Button>
            )}
            {manager &&
              u.status === "Loaded" &&
              !["Issued", "Reversed"].includes(d.run.status) && (
                <Button onClick={() => open("Unload", u)}>Unload unit</Button>
              )}
            {manager &&
              ["Open", "Closed"].includes(u.status) &&
              !["Issued", "Cancelled", "Reversed"].includes(d.run.status) && (
                <Button onClick={() => open("Unpack", u)}>Unpack unit</Button>
              )}
            <Button onClick={() => preview(u)}>
              <Printer size={15} />
              Label preview
            </Button>
          </div>
        </article>
      ))}
      {!d.units.length && (
        <Empty title="Ready to pack">
          Complete picking, start packing and open a handling unit.
        </Empty>
      )}
    </div>
  );
}
function PlanEditor({
  plantId,
  areas,
  warehouses,
  operators,
  orders,
  materials,
  onDone,
}: {
  plantId: string;
  areas: Row[];
  warehouses: Row[];
  operators: Row[];
  orders: Row[];
  materials: Row[];
  onDone: (id: string) => void;
}) {
  const [kind, setKind] = useState("Customer"),
    [shuttle, setShuttle] = useState([{ materialId: "", quantity: "" }]);
  return (
    <Action
      title="Save dispatch plan"
      path="runs"
      plantId={plantId}
      onDone={onDone}
      build={(f) => ({
        number: val(f, "number"),
        dispatchAreaId: val(f, "area"),
        kind,
        dispatchWarehouseId: kind === "Shuttle" ? val(f, "warehouse") : null,
        assignedToId: val(f, "operator"),
        scheduledAt: new Date(val(f, "scheduled")).toISOString(),
        priority: f.has("priority"),
        route: val(f, "route"),
        deliveryOrderIds:
          kind === "Customer" ? f.getAll("deliveries").map(String) : [],
        shuttleLines:
          kind === "Shuttle"
            ? shuttle.map((l) => ({
                materialId: l.materialId,
                quantity: Number(l.quantity),
              }))
            : [],
      })}
    >
      <div className="mfg-fields">
        <Field label="Dispatch reference" name="number" />
        <label>
          Dispatch type
          <select
            aria-label="Dispatch type"
            value={kind}
            onChange={(e) => setKind(e.target.value)}
          >
            <option value="Customer">Customer shipment</option>
            <option value="Shuttle">Local warehouse transfer</option>
          </select>
        </label>
        <Select label="Dispatch area" name="area" rows={areas} />
        <Select label="Assigned operator" name="operator" rows={operators} />
        <Field
          label="Scheduled dispatch date"
          name="scheduled"
          type="datetime-local"
          value={localDate()}
        />
        <Field label="Dispatch route" name="route" required={false} />
      </div>
      <Tick label="High priority" name="priority" />
      {kind === "Customer" ? (
        <>
          <Select
            label="Delivery orders to dispatch"
            name="deliveries"
            rows={orders.map((o) => ({
              ...o,
              label: `${o.number} · ${n(o.quantity)} · ${materials.find((m) => m.id === o.materialId)?.name || o.materialId}`,
            }))}
            multiple
          />
          <p className="mfg-note">
            Select one or more delivery orders. On desktop use Ctrl / Command to
            select multiple rows. Each existing delivery order contains one
            material; a dispatch can group many orders.
          </p>
        </>
      ) : (
        <>
          <Select
            label="External warehouse destination"
            name="warehouse"
            rows={warehouses}
          />
          <div className="mfg-warning">
            This workflow records local stock movements and a local goods issue.
            SAP/STO creation and external receiving confirmation are not
            connected.
          </div>
          {shuttle.map((l, i) => (
            <div className="mfg-fields" key={i}>
              <Select
                label={`Shuttle material ${i + 1}`}
                name={`material-${i}`}
                rows={materials}
                value={l.materialId}
                onChange={(v) =>
                  setShuttle(
                    shuttle.map((x, j) =>
                      i === j ? { ...x, materialId: v } : x,
                    ),
                  )
                }
              />
              <label>
                Shuttle quantity {i + 1}
                <input
                  type="number"
                  required
                  step="0.000001"
                  value={l.quantity}
                  onChange={(e) =>
                    setShuttle(
                      shuttle.map((x, j) =>
                        i === j ? { ...x, quantity: e.target.value } : x,
                      ),
                    )
                  }
                />
              </label>
              {shuttle.length > 1 && (
                <Button
                  onClick={() => setShuttle(shuttle.filter((_, j) => j !== i))}
                >
                  Remove line {i + 1}
                </Button>
              )}
            </div>
          ))}
          <Button
            disabled={shuttle.length >= 50}
            onClick={() =>
              setShuttle([...shuttle, { materialId: "", quantity: "" }])
            }
          >
            Add shuttle material
          </Button>
        </>
      )}
    </Action>
  );
}
function PickEditor({
  plantId,
  d,
  initialLine,
  onDone,
}: {
  plantId: string;
  d: any;
  initialLine?: string;
  onDone: () => void;
}) {
  const [lines, setLines] = useState([initialLine || d.lines[0]?.id || ""]);
  return (
    <Action
      title="Confirm scanned picks"
      path={`runs/${d.run.id}/picks`}
      plantId={plantId}
      onDone={onDone}
      build={(f) => ({
        lines: lines.map((id, i) => ({
          dispatchRunLineId: id,
          palletId: val(f, `pallet-${i}`),
          palletCode: val(f, `scan-${i}`),
          sourceBin: val(f, `bin-${i}`),
          quantity: Number(f.get(`quantity-${i}`)),
        })),
        instructionsConfirmed: f.has("instructions"),
        safetyConfirmed: f.has("safety"),
      })}
    >
      <Instructions lines={d.lines} stage="picking" />
      {d.lines.some((l: Row) => json(l.instructionsJson).safety) && (
        <div className="mfg-warning">
          {[
            ...new Set(
              d.lines
                .map((l: Row) => json(l.instructionsJson).safety)
                .filter(Boolean),
            ),
          ].join("\n")}
        </div>
      )}
      {lines.map((lineId, i) => (
        <PickRow
          key={i}
          index={i}
          lineId={lineId}
          plantId={plantId}
          d={d}
          setLine={(id) => setLines(lines.map((l, j) => (j === i ? id : l)))}
        />
      ))}
      {d.area.pickMode === "Group" && (
        <Button
          disabled={lines.length >= 50}
          onClick={() => setLines([...lines, d.lines[0]?.id || ""])}
        >
          <Plus size={16} />
          Add grouped pick
        </Button>
      )}
      <Tick
        label="I have checked the picking instructions"
        name="instructions"
        required
      />
      <Tick
        label="PPE and material safety requirements confirmed"
        name="safety"
        required
      />
      <p className="mfg-note">
        Picking moves stock into the packing bin and reserves it for this
        dispatch. Extra picked quantity must be returned before picking can
        finish.
      </p>
    </Action>
  );
}
function PickRow({
  index,
  lineId,
  plantId,
  d,
  setLine,
}: {
  index: number;
  lineId: string;
  plantId: string;
  d: any;
  setLine: (id: string) => void;
}) {
  const stock = useQuery({
    queryKey: ["operations", plantId, "dispatch-stock", d.run.id, lineId],
    queryFn: () =>
      operationApi<Block>(
        `dispatch/runs/${d.run.id}/stock?plantId=${plantId}&lineId=${lineId}`,
      ),
    enabled: !!lineId,
    retry: false,
  });
  const [selected, setSelected] = useState("");
  const sources = stock.data?.items || [],
    source = sources.find((s: Row) => s.pallet.id === selected);
  useEffect(() => setSelected(""), [lineId]);
  return (
    <div className="mfg-editor-row">
      <h3>Pick {index + 1}</h3>
      <Select
        label={`Delivery line ${index + 1}`}
        name={`line-${index}`}
        rows={d.lines.map((l: Row) => ({
          ...l,
          label: `${d.materials.find((m: Row) => m.id === l.materialId)?.name} · required ${n(l.quantity)} · ${d.deliveries.find((o: Row) => o.id === l.deliveryOrderId)?.number || "Shuttle"}`,
        }))}
        value={lineId}
        onChange={setLine}
      />
      <Select
        label={`Source pallet ${index + 1}`}
        name={`pallet-${index}`}
        rows={sources.map((s: Row) => ({
          id: s.pallet.id,
          label: `${s.pallet.code} · ${s.pallet.location} · free ${n(s.pallet.quantity - s.pallet.reservedQuantity)} · batch ${s.batch.number}`,
        }))}
        value={selected}
        onChange={setSelected}
      />
      {stock.error && <p role="alert">{stock.error.message}</p>}
      {source && (
        <p className="mfg-note">
          Source bin <b>{source.pallet.location}</b> · label{" "}
          <b>{source.pallet.code}</b>
          <br />
          Batch {source.batch.number} · expires{" "}
          {source.batch.expiryDate
            ? date(source.batch.expiryDate)
            : "not specified"}
          . Suggestions are ordered by earliest expiry.
        </p>
      )}
      <div className="mfg-fields">
        <Field label={`Scan source bin ${index + 1}`} name={`bin-${index}`} />
        <Field
          label={`Scan source pallet ${index + 1}`}
          name={`scan-${index}`}
        />
        <Field
          label={`Picked quantity ${index + 1}`}
          name={`quantity-${index}`}
          type="number"
        />
      </div>
    </div>
  );
}
function PackEditor({
  plantId,
  d,
  unit,
  onDone,
}: {
  plantId: string;
  d: any;
  unit: Row;
  onDone: () => void;
}) {
  const [selected, setSelected] = useState<string[]>([]),
    picks = d.picks.filter(
      (p: Row) =>
        p.status === "Picked" && p.quantity > 0 && !p.dispatchHandlingUnitId,
    );
  return (
    <Action
      title="Confirm checked packing"
      path={`units/${unit.id}/pack`}
      plantId={plantId}
      onDone={onDone}
      build={(f) => ({
        pickIds: selected,
        palletCodes: selected.map((id) => val(f, id)),
        instructionsConfirmed: f.has("instructions"),
      })}
    >
      <p className="mfg-note">
        Handling unit <b>{unit.code}</b>. Scan every selected picked label.
        Product mode validates one allocation per confirmation.
      </p>
      <Instructions lines={d.lines} stage="packing" />
      {picks.map((p: Row) => {
        const pallet = d.pallets.find((x: Row) => x.id === p.pickedPalletId),
          line = d.lines.find((x: Row) => x.id === p.dispatchRunLineId);
        return (
          <div className="mfg-editor-row" key={p.id}>
            <label className="mfg-check">
              <input
                type="checkbox"
                checked={selected.includes(p.id)}
                onChange={(e) =>
                  setSelected(
                    e.target.checked
                      ? [...selected, p.id]
                      : selected.filter((id) => id !== p.id),
                  )
                }
              />
              <span>
                {pallet.code} · {n(p.quantity)} ·{" "}
                {d.materials.find((m: Row) => m.id === line.materialId)?.name}
              </span>
            </label>
            {selected.includes(p.id) && (
              <Field label={`Scan picked label ${pallet.code}`} name={p.id} />
            )}
          </div>
        );
      })}
      <Tick
        label="Packing instructions checked and selected products verified"
        name="instructions"
        required
      />
    </Action>
  );
}
