import {
  useEffect,
  useRef,
  useState,
  type ReactNode,
  type FormEvent,
} from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowRight,
  ArrowUpRight,
  Boxes,
  CalendarClock,
  Check,
  CheckCircle2,
  ChevronRight,
  ClipboardList,
  Download,
  History,
  PackageCheck,
  Plus,
  Printer,
  RefreshCw,
  RotateCcw,
  Scale,
  Search,
  ShieldCheck,
  SlidersHorizontal,
  Truck,
  Warehouse,
  X,
  AlertTriangle,
} from "lucide-react";
import { operationApi } from "./Operations";
import "./preparation.css";

type Row = Record<string, any> & { id: string };
type Detail = {
  plan: Row;
  area: Row;
  order: Row;
  bins: Row[];
  lines: Row[];
  progress: Row[];
  picks: Row[];
  reservations: Row[];
  measurements: Row[];
  preparedPallets: Row[];
  sources: Row[];
  history: Row[];
  sourcesTruncated: boolean;
  historyTruncated: boolean;
};
type ModalState = { kind: string; row?: Row; line?: Row } | null;
const num = (n: unknown) =>
  Number(n || 0).toLocaleString(undefined, { maximumFractionDigits: 6 });
const when = (s: unknown) =>
  s
    ? new Date(String(s)).toLocaleString(undefined, {
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      })
    : "Unscheduled";
const str = (f: FormData, key: string) => String(f.get(key) || "");
const localDate = (date?: string) => {
  const d = date ? new Date(date) : new Date();
  return new Date(d.getTime() - d.getTimezoneOffset() * 60000)
    .toISOString()
    .slice(0, 16);
};
function useResource<T>(plantId: string, resource: string) {
  return useQuery({
    queryKey: ["operations", plantId, resource],
    queryFn: () => operationApi<T>(`${resource}?plantId=${plantId}`),
    retry: false,
  });
}
function Status({
  children,
  tone = "neutral",
}: {
  children: ReactNode;
  tone?: string;
}) {
  return <span className={`prep-badge ${tone}`}>{children}</span>;
}
function Empty({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="prep-empty">
      <Boxes size={28} strokeWidth={1.4} />
      <h3>{title}</h3>
      <p>{children}</p>
    </div>
  );
}
function Select({
  name,
  title,
  rows,
  optional = false,
  defaultValue = "",
}: {
  name: string;
  title: string;
  rows: Row[];
  optional?: boolean;
  defaultValue?: string;
}) {
  return (
    <label>
      {title}
      <select
        name={name}
        aria-label={title}
        defaultValue={defaultValue}
        required={!optional}
      >
        <option value="">{optional ? "Not linked" : "Choose an option"}</option>
        {rows.map((r) => (
          <option value={r.id} key={r.id}>
            {r.name || r.number || r.code}
          </option>
        ))}
      </select>
    </label>
  );
}
function Field({
  title,
  name,
  type = "text",
  value,
  required = true,
  min,
  step,
  maxLength = 160,
}: {
  title: string;
  name: string;
  type?: string;
  value?: string | number;
  required?: boolean;
  min?: number;
  step?: string;
  maxLength?: number;
}) {
  return (
    <label>
      {title}
      <input
        name={name}
        aria-label={title}
        type={type}
        defaultValue={value}
        required={required}
        min={min}
        step={step || (type === "number" ? "0.000001" : undefined)}
        maxLength={maxLength}
      />
    </label>
  );
}
function Notes({
  name,
  title,
  value,
  required = false,
}: {
  name: string;
  title: string;
  value?: string;
  required?: boolean;
}) {
  return (
    <label>
      {title}
      <textarea
        name={name}
        aria-label={title}
        rows={3}
        maxLength={2000}
        defaultValue={value}
        required={required}
      />
    </label>
  );
}
function Tick({
  name,
  children,
  checked = false,
}: {
  name: string;
  children: ReactNode;
  checked?: boolean;
}) {
  return (
    <label className="prep-tick">
      <input type="checkbox" name={name} defaultChecked={checked} />
      <span>{children}</span>
    </label>
  );
}
function Action({
  title,
  endpoint,
  build,
  plantId,
  children,
  disabled = false,
  blocked = false,
  onDone,
}: {
  title: string;
  endpoint: string;
  plantId: string;
  build: (f: FormData) => object;
  children: ReactNode;
  disabled?: boolean;
  blocked?: boolean;
  onDone?: () => void;
}) {
  const cache = useQueryClient(),
    retry = useRef({ signature: "", id: "" });
  const [busy, setBusy] = useState(false),
    [error, setError] = useState(""),
    [done, setDone] = useState(false);
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setBusy(true);
    setError("");
    setDone(false);
    try {
      const data = { plantId, ...build(new FormData(e.currentTarget)) };
      const signature = JSON.stringify(data);
      if (retry.current.signature !== signature)
        retry.current = { signature, id: crypto.randomUUID() };
      await operationApi(endpoint, { ...data, commandId: retry.current.id });
      retry.current = { signature: "", id: "" };
      await cache.invalidateQueries({ queryKey: ["operations"] });
      setDone(true);
      onDone?.();
    } catch (error) {
      setError((error as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <form className="prep-form" onSubmit={submit}>
      <fieldset disabled={busy || disabled}>{children}</fieldset>
      {error && (
        <p role="alert" className="prep-message bad">
          <AlertTriangle size={17} />
          {error}
        </p>
      )}
      {done && (
        <p role="status" className="prep-message good">
          <CheckCircle2 size={17} />
          Saved. Workspace refreshed.
        </p>
      )}
      <footer>
        <button className="prep-primary" disabled={busy || disabled || blocked}>
          {busy ? "Saving…" : title}
          <ArrowRight size={16} />
        </button>
      </footer>
    </form>
  );
}
function Dialog({
  title,
  subtitle,
  children,
  close,
}: {
  title: string;
  subtitle: string;
  children: ReactNode;
  close: () => void;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    ref.current?.showModal();
    const dialog = ref.current;
    return () => dialog?.close();
  }, []);
  return (
    <dialog
      ref={ref}
      className="prep-dialog"
      onCancel={(e) => {
        e.preventDefault();
        close();
      }}
      onClick={(e) => {
        if (e.target === e.currentTarget) close();
      }}
      aria-labelledby="prep-dialog-title"
    >
      <header>
        <div>
          <span className="prep-overline">MATERIAL PREPARATION</span>
          <h2 id="prep-dialog-title">{title}</h2>
          <p>{subtitle}</p>
        </div>
        <button
          type="button"
          className="prep-icon-button"
          aria-label="Close dialog"
          onClick={close}
        >
          <X size={20} />
        </button>
      </header>
      <div className="prep-dialog-body">{children}</div>
    </dialog>
  );
}
export function PreparationWorkspace({
  plantId,
  role,
  children,
}: {
  plantId: string;
  role: string;
  children: ReactNode;
}) {
  const writable = role !== "Viewer",
    manager = ["PlatformAdmin", "TenantAdmin", "PlantManager"].includes(role),
    cache = useQueryClient();
  const [tab, setTab] = useState("orders"),
    [search, setSearch] = useState(""),
    [filter, setFilter] = useState("Open"),
    [orderId, setOrderId] = useState(""),
    [modal, setModal] = useState<ModalState>(null),
    [detailTab, setDetailTab] = useState("materials"),
    [setupTab, setSetupTab] = useState("areas");
  const orders = useResource<{ items: Row[]; truncated: boolean }>(
      plantId,
      "staging/orders",
    ),
    areas = useResource<Row[]>(plantId, "staging/areas"),
    bins = useResource<Row[]>(plantId, "bins"),
    materials = useResource<Row[]>(plantId, "materials"),
    rules = useResource<Row[]>(plantId, "staging/rules"),
    factors = useResource<Row[]>(plantId, "staging/factors"),
    batches = useResource<Row[]>(plantId, "batches"),
    stock = useResource<Row[]>(plantId, "inventory"),
    requests = useResource<Row[]>(plantId, "staging/requests"),
    issues = useResource<Row[]>(plantId, "staging/stock-issues"),
    kanban = useResource<Row[]>(plantId, "staging/kanban");
  const list = orders.data?.items || [],
    activeOrder = list.find((o) => o.id === orderId),
    selectedPlan = activeOrder?.planId;
  const detail = useQuery({
    queryKey: ["operations", plantId, "staging/plan", selectedPlan],
    queryFn: () =>
      operationApi<Detail>(`staging/plans/${selectedPlan}?plantId=${plantId}`),
    enabled: !!selectedPlan,
    retry: false,
  });
  const d = detail.data,
    close = () => setModal(null);
  const raw = (materials.data || [])
    .filter((m) => m.kind === "Raw")
    .map((m) => ({ ...m, name: `${m.code} · ${m.name}` }));
  const allBins: Row[] = (bins.data || []).map((b) => ({
    ...b,
    name: `${b.code} · ${b.name}`,
  }));
  const inv = (stock.data || []).filter((s) => s.batch.kind === "Raw");
  const eligible = inv.filter(
    (s) =>
      s.batch.qualityStatus === "Released" &&
      (!s.batch.expiryDate || Date.parse(s.batch.expiryDate) > Date.now()) &&
      s.pallet.quantity > s.pallet.reservedQuantity &&
      !(issues.data || []).some(
        (i) => i.palletId === s.pallet.id && i.status === "Open",
      ),
  );
  const queryErrors = [
    orders,
    areas,
    bins,
    materials,
    rules,
    factors,
    batches,
    stock,
    requests,
    issues,
    kanban,
    detail,
  ].filter((q) => q.isError);
  function state(o: Row) {
    if (o.status === "Completed") return "Completed";
    if (o.approvalPending) return "Approval needed";
    if (!o.planId) return "Unscheduled";
    if (o.waiting) return "Weighing pending";
    if (o.supplied) return "At supply";
    if (o.issued > 0 && o.issued === o.picked) return "Kit issued";
    return o.picked ? "In progress" : "Scheduled";
  }
  const filtered = list.filter(
    (o) =>
      (filter === "All" ||
        (filter === "Open" && o.status !== "Completed") ||
        filter === state(o)) &&
      `${o.number} ${o.product} ${o.productCode}`
        .toLowerCase()
        .includes(search.toLowerCase()),
  );
  const livePicks = d?.picks.filter((p) => p.status !== "Returned") || [];
  const completeLines =
    d?.lines.filter((l) => {
      const p = d.progress.find((p) => p.materialId === l.materialId);
      return p && p.ready + p.issued >= l.required;
    }).length || 0;
  const progress = d?.lines.length
    ? Math.round((100 * completeLines) / d.lines.length)
    : 0;
  const openRequests = (requests.data || []).filter((r) =>
    ["Requested", "Started"].includes(r.status),
  );
  const currentStock = (id: string) => inv.find((s) => s.pallet.id === id);
  function exportHistory() {
    if (!d) return;
    const cell = (v: unknown) => {
      let s = String(v ?? "");
      if (/^\s*[=+\-@\t\r]/.test(s)) s = "'" + s;
      return `"${s.replaceAll('"', '""')}"`;
    };
    const csv = [
      ["Time", "Action", "Notes", "Result"],
      ...d.history.map((e) => [e.createdAt, e.kind, e.notes, e.resultId]),
    ]
      .map((row) => row.map(cell).join(","))
      .join("\r\n");
    const url = URL.createObjectURL(
      new Blob(["\uFEFF" + csv], { type: "text/csv;charset=utf-8" }),
    );
    const a = document.createElement("a");
    a.href = url;
    a.download = `${d.plan.kitCode}-events.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }
  return (
    <div className="prep-workspace">
      <div className="prep-topline">
        <div className="prep-live">
          <span />
          CONNECTED WORKSPACE
          <span className="prep-top-divider" />
          PICK · WEIGH · DELIVER
        </div>
        <button
          className="prep-text-button"
          onClick={() => cache.invalidateQueries({ queryKey: ["operations"] })}
        >
          <RefreshCw size={15} />
          Refresh
        </button>
      </div>
      <div className="prep-overview">
        <div className="prep-overview-copy">
          <span className="prep-overline">PREPARED FOR A BETTER SHIFT</span>
          <h2>
            The right material.
            <br />
            Ready for the next batch.
          </h2>
          <p>
            Bring order requirements, warehouse stock and precise preparation
            together in one workspace.
          </p>
        </div>
        <div className="prep-overview-steps">
          {[
            [
              ClipboardList,
              "01",
              "Plan the work",
              "Priorities & material demand",
            ],
            [
              Scale,
              "02",
              "Prepare with confidence",
              "Verified batches & weights",
            ],
            [
              PackageCheck,
              "03",
              "Deliver to production",
              "Traceable kits & supply",
            ],
          ].map(([Icon, n, title, desc]) => {
            const I = Icon as typeof Scale;
            return (
              <div key={String(n)}>
                <span className="prep-step-icon">
                  <I size={20} />
                </span>
                <div>
                  <small>{String(n)}</small>
                  <strong>{String(title)}</strong>
                  <p>{String(desc)}</p>
                </div>
              </div>
            );
          })}
        </div>
      </div>
      <div className="prep-stats">
        {[
          [
            ClipboardList,
            "Open orders",
            list.filter((o) => o.status !== "Completed").length,
            "In the current order window",
          ],
          [
            Scale,
            "Awaiting weighing",
            list.reduce((s, o) => s + o.waiting, 0),
            "Picked allocations to prepare",
          ],
          [
            PackageCheck,
            "Ready at supply",
            list.reduce((s, o) => s + o.supplied, 0),
            "Allocations ready for issue",
          ],
          [Truck, "Replenishments", openRequests.length, "Open requests"],
        ].map(([Icon, title, count, caption]) => {
          const I = Icon as typeof Scale;
          return (
            <article key={String(title)}>
              <div>
                <span>{String(title)}</span>
                <I size={18} />
              </div>
              <strong>{String(count)}</strong>
              <small>{String(caption)}</small>
            </article>
          );
        })}
      </div>
      <nav className="prep-nav" aria-label="Material Preparation sections">
        {[
          ["orders", "Orders & kits"],
          ["requests", "Replenishment"],
          ["kanban", "Kanban buffers"],
          ["stock", "Stock & issues"],
          ["setup", "Setup"],
          ["quick", "Quick reservations"],
        ].map(([key, title]) => (
          <button
            type="button"
            key={key}
            aria-current={tab === key ? "page" : undefined}
            onClick={() => setTab(key)}
          >
            {title}
            {key === "requests" && openRequests.length > 0 && (
              <span>{openRequests.length}</span>
            )}
          </button>
        ))}
      </nav>
      {queryErrors.map((q, i) => (
        <p role="alert" className="prep-message bad" key={i}>
          {q.error?.message}
        </p>
      ))}
      {orders.isPending && (
        <div className="prep-loading" role="status">
          Loading your preparation workspace…
        </div>
      )}
      {tab === "orders" && (
        <div className="prep-order-layout">
          <aside className="prep-order-rail">
            <header>
              <div>
                <h3>Production queue</h3>
                <p>{filtered.length} orders in view</p>
              </div>
              <CalendarClock size={20} />
            </header>
            <label className="prep-search">
              <Search size={16} />
              <input
                aria-label="Search preparation orders"
                placeholder="Order or product…"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </label>
            <select
              aria-label="Preparation order status"
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
            >
              {[
                "Open",
                "All",
                "Unscheduled",
                "Scheduled",
                "Approval needed",
                "In progress",
                "Weighing pending",
                "At supply",
                "Kit issued",
                "Completed",
              ].map((f) => (
                <option key={f}>{f}</option>
              ))}
            </select>
            <div className="prep-orders">
              {filtered.map((o) => (
                <button
                  className={`prep-order-card ${o.id === orderId ? "selected" : ""}`}
                  key={o.id}
                  onClick={() => {
                    setOrderId(o.id);
                    setDetailTab("materials");
                  }}
                >
                  <div>
                    <strong>{o.number}</strong>
                    {o.priority && (
                      <span className="prep-priority">PRIORITY</span>
                    )}
                  </div>
                  <p>{o.product}</p>
                  <small>
                    {num(o.plannedQuantity)} {o.uom} · {o.productCode}
                  </small>
                  <footer>
                    <Status
                      tone={
                        o.approvalPending
                          ? "amber"
                          : o.supplied || o.status === "Completed"
                            ? "green"
                            : "neutral"
                      }
                    >
                      {state(o)}
                    </Status>
                    <ChevronRight size={15} />
                  </footer>
                  <time>{when(o.scheduledAt)}</time>
                </button>
              ))}
              {!filtered.length && (
                <Empty title="No orders here">
                  Create and release a production order in Production to begin.
                </Empty>
              )}
            </div>
            {orders.data?.truncated && (
              <p className="prep-footnote">Showing the first 200 orders.</p>
            )}
          </aside>
          <section className="prep-order-main">
            {!activeOrder ? (
              <div className="prep-start">
                <div className="prep-start-icon">
                  <ClipboardList size={36} strokeWidth={1.3} />
                </div>
                <span className="prep-overline">
                  YOUR NEXT BATCH STARTS HERE
                </span>
                <h3>Choose an order to prepare</h3>
                <p>
                  Select a production order from the queue to review its
                  materials, plan the work and build a traceable kit.
                </p>
                <div>
                  <span>
                    <ShieldCheck size={16} />
                    Quality-released stock
                  </span>
                  <span>
                    <Scale size={16} />
                    Recorded weights
                  </span>
                  <span>
                    <History size={16} />
                    Every step traceable
                  </span>
                </div>
              </div>
            ) : (
              <>
                <header className="prep-order-heading">
                  <div>
                    <span className="prep-overline">PRODUCTION ORDER</span>
                    <h2>
                      {activeOrder.number}
                      <Status
                        tone={activeOrder.approvalPending ? "amber" : "green"}
                      >
                        {state(activeOrder)}
                      </Status>
                    </h2>
                    <p>
                      {activeOrder.product} <span>·</span>{" "}
                      {num(activeOrder.plannedQuantity)} {activeOrder.uom}{" "}
                      planned
                    </p>
                  </div>
                  {manager && activeOrder.status === "Released" && (
                    <button
                      className="prep-secondary"
                      onClick={() =>
                        setModal({ kind: "plan", row: activeOrder })
                      }
                    >
                      <CalendarClock size={16} />
                      {selectedPlan ? "Edit plan" : "Schedule preparation"}
                    </button>
                  )}
                </header>
                {!selectedPlan ? (
                  <div className="prep-unscheduled">
                    <CalendarClock size={28} />
                    <h3>Give this order a preparation plan</h3>
                    <p>
                      Choose a staging area and start time. Its workflow will
                      guide the team from the first pick to production supply.
                    </p>
                    {!areas.data?.length ? (
                      <button
                        className="prep-secondary"
                        onClick={() => {
                          setTab("setup");
                          setSetupTab("areas");
                        }}
                      >
                        Configure your first staging area
                        <ArrowRight size={16} />
                      </button>
                    ) : (
                      manager && (
                        <button
                          className="prep-primary"
                          onClick={() =>
                            setModal({ kind: "plan", row: activeOrder })
                          }
                        >
                          Create preparation plan
                          <Plus size={16} />
                        </button>
                      )
                    )}
                  </div>
                ) : detail.isPending ? (
                  <p role="status" className="prep-loading">
                    Reading the saved BOM and stock…
                  </p>
                ) : (
                  d && (
                    <>
                      <div className="prep-plan-strip">
                        <div>
                          <Warehouse size={17} />
                          <strong>{d.area.name}</strong>
                          <span>
                            {d.plan.workflow === "Separate"
                              ? "Pick, then kit"
                              : d.plan.workflow === "Together"
                                ? "Pick & kit together"
                                : "Pick only"}
                          </span>
                        </div>
                        <div>
                          <CalendarClock size={16} />
                          {when(d.plan.scheduledAt)}
                        </div>
                        <button
                          className="prep-text-button"
                          onClick={() => setModal({ kind: "label" })}
                        >
                          <Printer size={16} />
                          {d.plan.kitCode}
                        </button>
                      </div>
                      {d.plan.approvalRequired && (
                        <p
                          className={`prep-message ${d.plan.approved ? "good" : "warn"}`}
                        >
                          <ShieldCheck size={18} />
                          {d.plan.approved
                            ? `Engineering approval recorded. ${d.plan.approvalNotes}`
                            : "Engineering approval is pending. Picking is blocked until a manager approves this plan."}
                        </p>
                      )}
                      <div className="prep-readiness">
                        <div>
                          <strong>
                            {completeLines}
                            <span> / {d.lines.length}</span>
                          </strong>
                          <p>materials prepared or issued</p>
                        </div>
                        <div className="prep-progress-track">
                          <span style={{ width: `${progress}%` }} />
                        </div>
                        <strong>{progress}%</strong>
                      </div>
                      <div className="prep-detail-nav">
                        <div>
                          {[
                            ["materials", "Material requirements"],
                            ["kit", `Kit contents (${livePicks.length})`],
                            ["history", "Activity & weights"],
                          ].map(([key, title]) => (
                            <button
                              key={key}
                              aria-pressed={detailTab === key}
                              onClick={() => setDetailTab(key)}
                            >
                              {title}
                            </button>
                          ))}
                        </div>
                        {detailTab === "history" && (
                          <button
                            className="prep-text-button"
                            onClick={exportHistory}
                          >
                            <Download size={15} />
                            Export shown events
                          </button>
                        )}
                      </div>
                      {detailTab === "materials" && (
                        <div className="prep-requirements">
                          {d.lines.map((line) => {
                            const p = d.progress.find(
                              (p) => p.materialId === line.materialId,
                            )!;
                            const full =
                                line.packSize > 0
                                  ? Math.floor(line.required / line.packSize)
                                  : 0,
                              part =
                                line.packSize > 0
                                  ? Math.round(
                                      (line.required - full * line.packSize) *
                                        1e6,
                                    ) / 1e6
                                  : 0;
                            return (
                              <article
                                className="prep-material"
                                key={line.materialId}
                              >
                                <div
                                  className={`prep-material-marker ${p.remaining === 0 ? "done" : ""}`}
                                >
                                  {p.remaining === 0 ? (
                                    <Check size={18} />
                                  ) : (
                                    <Boxes size={18} />
                                  )}
                                </div>
                                <div className="prep-material-body">
                                  <header>
                                    <div>
                                      <span>{line.code}</span>
                                      <h3>{line.name}</h3>
                                    </div>
                                    <div className="prep-material-tags">
                                      {line.preweighRequired && (
                                        <Status tone="blue">
                                          <Scale size={12} />
                                          Preweigh
                                        </Status>
                                      )}
                                      {line.correctionRequired && (
                                        <Status tone="amber">
                                          Batch factor
                                        </Status>
                                      )}
                                    </div>
                                  </header>
                                  <dl>
                                    {[
                                      ["BOM required", line.required],
                                      ["Allocated", p.allocated],
                                      ["Issued credit", p.issued],
                                      ["Still to prepare", p.remaining],
                                    ].map(([label, qty]) => (
                                      <div key={String(label)}>
                                        <dt>{label}</dt>
                                        <dd>
                                          {num(qty)} <small>{line.uom}</small>
                                        </dd>
                                      </div>
                                    ))}
                                  </dl>
                                  <footer>
                                    <span>
                                      {line.packSize > 0
                                        ? `${full} full × ${num(line.packSize)}${part > 0 ? ` + ${num(part)} part` : ""} ${line.uom} in the BOM`
                                        : "Use the material's unit of measure"}
                                    </span>
                                    <button
                                      className="prep-small-action"
                                      disabled={
                                        !writable ||
                                        p.remaining <= 0 ||
                                        activeOrder.approvalPending ||
                                        activeOrder.status !== "Released"
                                      }
                                      onClick={() =>
                                        setModal({ kind: "pick", line })
                                      }
                                    >
                                      Prepare material
                                      <ArrowUpRight size={15} />
                                    </button>
                                  </footer>
                                </div>
                              </article>
                            );
                          })}
                          <p className="prep-footnote">
                            BOM quantities are requirement credits. Physical
                            weights may differ under an approved tolerance or
                            batch correction factor. Picking moves stock;
                            production issue consumes it.
                          </p>
                        </div>
                      )}
                      {detailTab === "kit" && (
                        <div className="prep-kit-contents">
                          {!livePicks.length ? (
                            <Empty title="Your kit is waiting">
                              Prepare the first material to create a labelled
                              allocation.
                            </Empty>
                          ) : (
                            livePicks.map((p) => {
                              const pallet = d.preparedPallets.find(
                                  (a) => a.id === p.currentPalletId,
                                ),
                                mat = d.lines.find(
                                  (l) => l.materialId === p.materialId,
                                ),
                                res = d.reservations.find(
                                  (r) => r.stagingPickId === p.id,
                                );
                              return (
                                <article className="prep-kit-row" key={p.id}>
                                  <div className="prep-kit-symbol">
                                    <PackageCheck size={20} />
                                  </div>
                                  <div>
                                    <strong>{mat?.name}</strong>
                                    <p>
                                      {pallet?.batchNumber} <span>·</span>{" "}
                                      {pallet?.location}
                                    </p>
                                    <small className="prep-code">
                                      {pallet?.code}
                                    </small>
                                    <p>
                                      {num(p.quantity)} {mat?.uom} physical ·{" "}
                                      {num(p.requirementQuantity)} BOM credit ·
                                      factor {num(p.correctionFactor)}
                                    </p>
                                  </div>
                                  <div className="prep-kit-actions">
                                    <Status
                                      tone={
                                        p.status === "Picked"
                                          ? "amber"
                                          : "green"
                                      }
                                    >
                                      {p.status === "Supply"
                                        ? "At supply"
                                        : p.status}
                                    </Status>
                                    {p.status === "Picked" && (
                                      <button
                                        className="prep-small-action"
                                        disabled={!writable}
                                        onClick={() =>
                                          setModal({
                                            kind: "weigh",
                                            row: p,
                                            line: mat,
                                          })
                                        }
                                      >
                                        <Scale size={15} />
                                        Weigh & kit
                                      </button>
                                    )}
                                    {p.status === "Supply" && res && (
                                      <IssueButton
                                        plantId={plantId}
                                        reservationId={res.id}
                                        disabled={!writable}
                                      />
                                    )}
                                    {p.status !== "Issued" && (
                                      <button
                                        className="prep-text-button"
                                        disabled={!writable}
                                        onClick={() =>
                                          setModal({ kind: "return", row: p })
                                        }
                                      >
                                        <RotateCcw size={14} />
                                        Return allocation
                                      </button>
                                    )}
                                  </div>
                                </article>
                              );
                            })
                          )}
                          {d.picks.some((p) => p.status === "Kitted") && (
                            <div className="prep-delivery-callout">
                              <div>
                                <PackageCheck size={22} />
                                <div>
                                  <strong>
                                    Bring the complete kit to production
                                  </strong>
                                  <p>
                                    Deliver once all materials are kitted. The
                                    scanned kit and supply bin must match this
                                    plan.
                                  </p>
                                </div>
                              </div>
                              <button
                                className="prep-primary"
                                disabled={!writable}
                                onClick={() => setModal({ kind: "deliver" })}
                              >
                                Deliver kit
                                <ArrowRight size={16} />
                              </button>
                            </div>
                          )}
                          <p className="prep-footnote">
                            Loose remainder from a full-bag pick stays
                            unreserved in staging. Find it in Stock & issues for
                            reuse or warehouse transfer.
                          </p>
                        </div>
                      )}
                      {detailTab === "history" && (
                        <div className="prep-history">
                          <h3>Recorded weights</h3>
                          {d.measurements.length ? (
                            <div className="prep-table-wrap">
                              <table>
                                <thead>
                                  <tr>
                                    {[
                                      "Time",
                                      "Gross",
                                      "Tare",
                                      "Net",
                                      "Target",
                                      "Source",
                                    ].map((t) => (
                                      <th key={t}>{t}</th>
                                    ))}
                                  </tr>
                                </thead>
                                <tbody>
                                  {d.measurements.map((m) => (
                                    <tr key={m.id}>
                                      <td>{when(m.createdAt)}</td>
                                      <td>{num(m.gross)}</td>
                                      <td>{num(m.tare)}</td>
                                      <td>
                                        <strong>{num(m.net)}</strong>
                                      </td>
                                      <td>{num(m.expectedQuantity)}</td>
                                      <td>Manual · {m.scaleReference}</td>
                                    </tr>
                                  ))}
                                </tbody>
                              </table>
                            </div>
                          ) : (
                            <p>No weights recorded yet.</p>
                          )}
                          <h3>Kit activity</h3>
                          {d.history.map((event) => (
                            <div className="prep-history-event" key={event.id}>
                              <span>
                                <CheckCircle2 size={16} />
                              </span>
                              <div>
                                <strong>
                                  {event.kind.replace(/([A-Z])/g, " $1").trim()}
                                </strong>
                                <p>
                                  {event.notes ||
                                    "Recorded against this preparation plan."}
                                </p>
                              </div>
                              <time>{when(event.createdAt)}</time>
                            </div>
                          ))}
                          {!d.history.length && (
                            <p>Activity will appear when preparation starts.</p>
                          )}
                          {d.historyTruncated && (
                            <p>Showing the latest 200 events.</p>
                          )}
                        </div>
                      )}
                    </>
                  )
                )}
              </>
            )}
          </section>
        </div>
      )}
      {tab === "requests" && (
        <section className="prep-panel">
          <PanelHeading
            title="Keep the floor supplied"
            subtitle="Track shortage, immediate and Kanban requests from request to delivery."
            action={
              writable && (
                <button
                  className="prep-primary"
                  onClick={() => setModal({ kind: "request" })}
                >
                  <Plus size={16} />
                  Request material
                </button>
              )
            }
          />
          {requests.data?.length ? (
            <div className="prep-request-list">
              {requests.data.map((r) => (
                <article key={r.id}>
                  <span className="prep-request-icon">
                    <Truck size={22} />
                  </span>
                  <div>
                    <small>
                      {r.kind} · {when(r.createdAt)}
                    </small>
                    <h3>
                      {materials.data?.find((m) => m.id === r.materialId)?.name}
                    </h3>
                    <p>
                      {allBins.find((b) => b.id === r.destinationBinId)?.name} ·{" "}
                      {r.reason}
                    </p>
                    <div className="prep-request-progress">
                      <span
                        style={{
                          width: `${(100 * r.fulfilledQuantity) / r.quantity}%`,
                        }}
                      />
                    </div>
                    <small>
                      {num(r.fulfilledQuantity)} / {num(r.quantity)}{" "}
                      {materials.data?.find((m) => m.id === r.materialId)?.uom}{" "}
                      delivered
                    </small>
                  </div>
                  <div className="prep-request-actions">
                    <Status
                      tone={
                        r.status === "Completed"
                          ? "green"
                          : r.status === "Cancelled"
                            ? "neutral"
                            : "amber"
                      }
                    >
                      {r.status}
                    </Status>
                    {["Requested", "Started"].includes(r.status) && (
                      <button
                        className="prep-small-action"
                        disabled={!writable}
                        onClick={() => setModal({ kind: "fulfil", row: r })}
                      >
                        Deliver stock
                        <ArrowRight size={15} />
                      </button>
                    )}
                    {r.status === "Requested" && (
                      <button
                        className="prep-text-button"
                        disabled={!writable}
                        onClick={() => setModal({ kind: "cancel", row: r })}
                      >
                        Cancel request
                      </button>
                    )}
                  </div>
                </article>
              ))}
            </div>
          ) : (
            <Empty title="No replenishment requests">
              Create a request when more material is needed at the staging or
              supply bin.
            </Empty>
          )}
        </section>
      )}
      {tab === "kanban" && (
        <section className="prep-panel">
          <PanelHeading
            title="A clear view of every buffer"
            subtitle="Prioritize low stock and avoid duplicate requests. Values use the material's own unit."
            action={
              manager && (
                <button
                  className="prep-secondary"
                  onClick={() => {
                    setTab("setup");
                    setSetupTab("kanban");
                  }}
                >
                  <SlidersHorizontal size={16} />
                  Configure buffers
                </button>
              )
            }
          />
          <div className="prep-kanban-grid">
            {kanban.data?.map((k) => (
              <article
                className={`prep-kanban ${k.status.toLowerCase()}`}
                key={k.policy.id}
              >
                <header>
                  <span className="prep-overline">{k.binCode}</span>
                  <Status
                    tone={
                      k.status === "Urgent"
                        ? "red"
                        : k.status === "Replenish" || k.status === "Capacity"
                          ? "amber"
                          : "green"
                    }
                  >
                    {k.status}
                  </Status>
                </header>
                <small>{k.materialCode}</small>
                <h3>{k.materialName}</h3>
                <div className="prep-kanban-qty">
                  {num(k.available)} <span>{k.uom} available</span>
                </div>
                <div className="prep-buffer-track">
                  <span
                    style={{
                      width: `${Math.min(100, (100 * k.available) / k.policy.maxStock)}%`,
                    }}
                  />
                  <i
                    style={{
                      left: `${(100 * k.policy.reorderStock) / k.policy.maxStock}%`,
                    }}
                  />
                </div>
                <dl>
                  <div>
                    <dt>Reorder level</dt>
                    <dd>{num(k.policy.reorderStock)}</dd>
                  </div>
                  <div>
                    <dt>Maximum</dt>
                    <dd>{num(k.policy.maxStock)}</dd>
                  </div>
                  <div>
                    <dt>Open deliveries</dt>
                    <dd>{num(k.openQuantity)}</dd>
                  </div>
                  {k.policy.mode === "Demand" && (
                    <div>
                      <dt>Unallocated order demand</dt>
                      <dd>{num(k.demand)}</dd>
                    </div>
                  )}
                </dl>
                <footer>
                  <span>
                    {k.policy.mode === "Demand"
                      ? "Open-plan demand"
                      : "Static buffer"}
                  </span>
                  <button
                    className="prep-small-action"
                    disabled={!writable || k.requestQuantity <= 0}
                    onClick={() =>
                      setModal({
                        kind: "request",
                        row: {
                          id: k.policy.id,
                          materialId: k.policy.materialId,
                          destinationBinId: k.policy.binId,
                          quantity: k.requestQuantity,
                          kind: "Kanban",
                          reason: "Kanban buffer replenishment",
                        },
                      })
                    }
                  >
                    Request {num(k.requestQuantity)}
                    <Plus size={15} />
                  </button>
                </footer>
              </article>
            ))}
          </div>
          {!kanban.data?.length && (
            <Empty title="Set up your first Kanban buffer">
              Define a material, bin and stock thresholds in Setup. The
              dashboard will use actual released stock.
            </Empty>
          )}
        </section>
      )}
      {tab === "stock" && (
        <section className="prep-panel">
          <PanelHeading
            title="Stock with a clear next step"
            subtitle="Raw inventory, reservations and discrepancies. Reporting an issue does not silently change stock."
          />
          <div className="prep-stock-summary">
            <span>
              <Warehouse size={17} />
              {inv.length} loaded stock labels
            </span>
            <span>
              <AlertTriangle size={17} />
              {issues.data?.filter((i) => i.status === "Open").length || 0} open
              issues
            </span>
          </div>
          <div className="prep-table-wrap">
            <table>
              <thead>
                <tr>
                  {[
                    "Material / batch",
                    "Bin",
                    "On hand",
                    "Reserved",
                    "Quality",
                    "Actions",
                  ].map((t) => (
                    <th key={t}>{t}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {inv
                  .filter((s) => s.pallet.quantity > 0)
                  .map((s) => (
                    <tr key={s.pallet.id}>
                      <td>
                        <strong>{s.material.name}</strong>
                        <small>
                          {s.batch.number} · {s.material.code}
                        </small>
                        <small className="prep-code">{s.pallet.code}</small>
                      </td>
                      <td>{s.pallet.location}</td>
                      <td>
                        {num(s.pallet.quantity)} {s.material.uom}
                      </td>
                      <td>{num(s.pallet.reservedQuantity)}</td>
                      <td>
                        <Status
                          tone={
                            s.batch.qualityStatus === "Released"
                              ? "green"
                              : "amber"
                          }
                        >
                          {s.batch.qualityStatus}
                        </Status>
                      </td>
                      <td>
                        <div className="prep-inline-actions">
                          <button
                            disabled={
                              !writable ||
                              !eligible.some((e) => e.pallet.id === s.pallet.id)
                            }
                            className="prep-text-button"
                            onClick={() =>
                              setModal({ kind: "transfer", row: s })
                            }
                          >
                            Transfer
                          </button>
                          <button
                            disabled={!writable}
                            className="prep-text-button"
                            onClick={() => setModal({ kind: "issue", row: s })}
                          >
                            Report issue
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
              </tbody>
            </table>
          </div>
          {!inv.length && (
            <Empty title="No raw stock yet">
              Receive material in Inbound, then release it in Quality before
              preparation.
            </Empty>
          )}
          <h3 className="prep-subtitle">Stock issue log</h3>
          {issues.data?.map((i) => {
            const s = currentStock(i.palletId);
            return (
              <article className="prep-stock-issue" key={i.id}>
                <AlertTriangle size={19} />
                <div>
                  <strong>
                    {i.kind} · {s?.material.name || "Stock label"}
                  </strong>
                  <p>{i.notes}</p>
                  <small>
                    System {num(i.systemQuantity)}
                    {i.countedQuantity !== null
                      ? ` · Counted ${num(i.countedQuantity)}`
                      : ""}{" "}
                    · {when(i.createdAt)}
                  </small>
                  {i.resolution && <p>{i.resolution}</p>}
                </div>
                <Status tone={i.status === "Open" ? "amber" : "green"}>
                  {i.status}
                </Status>
                {manager && i.status === "Open" && (
                  <button
                    className="prep-secondary"
                    onClick={() => setModal({ kind: "resolve", row: i })}
                  >
                    Review
                  </button>
                )}
              </article>
            );
          })}
          <p className="prep-footnote">
            Stock and support lists show up to 200 records. No mixed-unit stock
            total is calculated.
          </p>
        </section>
      )}
      {tab === "setup" && (
        <section className="prep-panel">
          <PanelHeading
            title="Preparation, configured for your floor"
            subtitle="Manager settings. Existing picks keep their saved weight rules and correction factors."
          />
          <div className="prep-setup-layout">
            <nav aria-label="Preparation setup">
              {[
                ["areas", "Staging areas", Warehouse],
                ["rules", "Material rules", SlidersHorizontal],
                ["factors", "Batch factors", Scale],
                ["kanban", "Kanban policies", Boxes],
              ].map(([key, title, Icon]) => {
                const I = Icon as typeof Scale;
                return (
                  <button
                    key={String(key)}
                    aria-current={setupTab === key ? "page" : undefined}
                    onClick={() => setSetupTab(String(key))}
                  >
                    <I size={18} />
                    {String(title)}
                    <ChevronRight size={15} />
                  </button>
                );
              })}
            </nav>
            <div className="prep-setup-content">
              {setupTab === "areas" && (
                <>
                  <h3>Staging areas</h3>
                  <p>
                    Connect a staging bin, a production supply bin and the
                    team's workflow.
                  </p>
                  <Action
                    title="Save staging area"
                    endpoint="staging/areas"
                    plantId={plantId}
                    disabled={!manager}
                    build={(f) => ({
                      code: str(f, "code"),
                      name: str(f, "name"),
                      stagingBinId: str(f, "stagingBinId"),
                      supplyBinId: str(f, "supplyBinId"),
                      workflow: str(f, "workflow"),
                    })}
                  >
                    <div className="prep-field-grid">
                      <Field title="Area code" name="code" />
                      <Field title="Area name" name="name" />
                      <Select
                        title="Staging bin"
                        name="stagingBinId"
                        rows={allBins}
                      />
                      <Select
                        title="Production supply bin"
                        name="supplyBinId"
                        rows={allBins}
                      />
                    </div>
                    <label>
                      Workflow
                      <select name="workflow" aria-label="Preparation workflow">
                        <option value="Separate">Pick, then weigh & kit</option>
                        <option value="Together">Pick & kit together</option>
                        <option value="PickOnly">Pick only</option>
                      </select>
                    </label>
                    <p className="prep-hint">
                      Create bins under Inbound → Master data. Used areas keep
                      their workflow and bins; create a new area for changes.
                    </p>
                  </Action>
                  <div className="prep-config-list">
                    {areas.data?.map((a) => (
                      <div key={a.id}>
                        <Warehouse size={18} />
                        <span>
                          <strong>{a.name}</strong>
                          <small>
                            {a.code} · {a.workflow}
                          </small>
                        </span>
                      </div>
                    ))}
                  </div>
                </>
              )}
              {setupTab === "rules" && (
                <>
                  <h3>Material preparation rules</h3>
                  <p>
                    Define pack size, weighing limits and the safety information
                    shown before picking.
                  </p>
                  <Action
                    title="Save material rule"
                    endpoint="staging/rules"
                    plantId={plantId}
                    disabled={!manager}
                    build={(f) => ({
                      materialId: str(f, "materialId"),
                      packSize: Number(str(f, "packSize")),
                      preweighRequired: f.has("preweighRequired"),
                      tolerancePercent: Number(str(f, "tolerancePercent")),
                      correctionRequired: f.has("correctionRequired"),
                      safetyNotes: str(f, "safetyNotes"),
                      handlingNotes: str(f, "handlingNotes"),
                    })}
                  >
                    <Select
                      name="materialId"
                      title="Rule material"
                      rows={raw}
                    />
                    <div className="prep-field-grid">
                      <Field
                        title="Default pack size"
                        name="packSize"
                        type="number"
                        value={0}
                        min={0}
                      />
                      <Field
                        title="Weight tolerance (%)"
                        name="tolerancePercent"
                        type="number"
                        value={0}
                        min={0}
                      />
                    </div>
                    <Tick name="preweighRequired">
                      Require preweigh readings
                    </Tick>
                    <Tick name="correctionRequired">
                      Require an approved batch correction factor
                    </Tick>
                    <Notes
                      title="Safety & PPE instructions"
                      name="safetyNotes"
                    />
                    <Notes title="Handling notes" name="handlingNotes" />
                  </Action>
                  <div className="prep-config-list">
                    {rules.data?.map((r) => (
                      <div key={r.id}>
                        <SlidersHorizontal size={18} />
                        <span>
                          <strong>
                            {
                              materials.data?.find((m) => m.id === r.materialId)
                                ?.name
                            }
                          </strong>
                          <small>
                            Pack {num(r.packSize)} · ±{num(r.tolerancePercent)}%
                            {r.preweighRequired ? " · Preweigh" : ""}
                            {r.correctionRequired ? " · Batch factor" : ""}
                          </small>
                        </span>
                      </div>
                    ))}
                  </div>
                </>
              )}
              {setupTab === "factors" && (
                <>
                  <h3>Batch correction factors</h3>
                  <p>
                    Physical target = remaining BOM quantity × batch factor.
                    Picks retain the approved factor used at preparation.
                  </p>
                  <Action
                    title="Save batch factor"
                    endpoint="staging/factors"
                    plantId={plantId}
                    disabled={!manager}
                    build={(f) => ({
                      batchId: str(f, "batchId"),
                      factor: Number(str(f, "factor")),
                      evidence: str(f, "evidence"),
                    })}
                  >
                    <Select
                      title="Factor batch"
                      name="batchId"
                      rows={(batches.data || [])
                        .filter((b) => b.kind === "Raw")
                        .map((b) => ({
                          ...b,
                          name: `${b.number} · ${materials.data?.find((m) => m.id === b.materialId)?.code || ""}`,
                        }))}
                    />
                    <Field
                      title="Correction factor"
                      name="factor"
                      type="number"
                      min={0.000001}
                    />
                    <Notes
                      title="Quality evidence / approval reference"
                      name="evidence"
                      required
                    />
                  </Action>
                  <div className="prep-config-list">
                    {factors.data?.map((f) => (
                      <div key={f.id}>
                        <Scale size={18} />
                        <span>
                          <strong>
                            {
                              batches.data?.find((b) => b.id === f.batchId)
                                ?.number
                            }
                          </strong>
                          <small>
                            Factor {num(f.factor)} · {f.evidence}
                          </small>
                        </span>
                      </div>
                    ))}
                  </div>
                  <p className="prep-hint">
                    Values are manager-entered. No live SAP quality-method
                    lookup is connected.
                  </p>
                </>
              )}
              {setupTab === "kanban" && (
                <>
                  <h3>Kanban buffer policies</h3>
                  <p>
                    Static policies maintain stock thresholds. Demand policies
                    use outstanding requirements of open plans mapped to the
                    bin.
                  </p>
                  <Action
                    title="Save Kanban policy"
                    endpoint="staging/kanban"
                    plantId={plantId}
                    disabled={!manager}
                    build={(f) => ({
                      binId: str(f, "binId"),
                      materialId: str(f, "materialId"),
                      mode: str(f, "mode"),
                      lowStock: Number(str(f, "lowStock")),
                      reorderStock: Number(str(f, "reorderStock")),
                      maxStock: Number(str(f, "maxStock")),
                    })}
                  >
                    <Select title="Buffer bin" name="binId" rows={allBins} />
                    <Select
                      title="Buffer material"
                      name="materialId"
                      rows={raw}
                    />
                    <label>
                      Replenishment mode
                      <select name="mode" aria-label="Replenishment mode">
                        <option value="Static">Static stock thresholds</option>
                        <option value="Demand">Open staging plan demand</option>
                      </select>
                    </label>
                    <div className="prep-field-grid">
                      <Field
                        title="Urgent below"
                        name="lowStock"
                        type="number"
                        min={0}
                      />
                      <Field
                        title="Reorder below"
                        name="reorderStock"
                        type="number"
                        min={0}
                      />
                      <Field
                        title="Maximum buffer stock"
                        name="maxStock"
                        type="number"
                        min={0.000001}
                      />
                    </div>
                    <p className="prep-hint">
                      Use static buffers for correction-factor materials. No
                      automatic replenishment or physical capacity override is
                      enabled.
                    </p>
                  </Action>
                </>
              )}
            </div>
          </div>
          {!manager && (
            <p className="prep-message warn">
              Your role can view settings. A manager must save configuration
              changes.
            </p>
          )}
        </section>
      )}
      {tab === "quick" && (
        <section className="prep-quick">
          <div className="prep-message">
            <ClipboardList size={18} />
            Quick reservations support orders without a guided staging plan. Use
            Orders & kits for planned picking and weighing.
          </div>
          {children}
        </section>
      )}
      {modal && (
        <Dialog
          close={close}
          title={
            (
              {
                plan: selectedPlan
                  ? "Update preparation plan"
                  : "Schedule preparation",
                pick: "Prepare material",
                weigh: "Weigh & build the kit",
                deliver: "Deliver kit to production",
                return: "Return the allocation",
                label: "Kit label",
                request: "Request more material",
                fulfil: "Deliver replenishment",
                cancel: "Cancel replenishment",
                transfer: "Transfer available stock",
                issue: "Report a stock issue",
                resolve: "Review the stock issue",
              } as Record<string, string>
            )[modal.kind]
          }
          subtitle={
            modal.line?.name || activeOrder?.number || "Plant material control"
          }
        >
          {modal.kind === "plan" && (
            <Action
              title="Save preparation plan"
              endpoint="staging/plans"
              plantId={plantId}
              disabled={!manager}
              onDone={close}
              build={(f) => ({
                productionOrderId: modal.row!.id,
                stagingAreaId: str(f, "stagingAreaId"),
                scheduledAt: new Date(str(f, "scheduledAt")).toISOString(),
                priority: f.has("priority"),
                approvalRequired: f.has("approvalRequired"),
                approved: f.has("approved"),
                approvalNotes: str(f, "approvalNotes"),
              })}
            >
              <Select
                name="stagingAreaId"
                title="Preparation area"
                rows={areas.data || []}
                defaultValue={d?.plan.stagingAreaId || ""}
              />
              <Field
                title="Preparation start"
                name="scheduledAt"
                type="datetime-local"
                value={localDate(d?.plan.scheduledAt)}
              />
              <Tick name="priority" checked={d?.plan.priority}>
                Prioritize this order
              </Tick>
              <Tick name="approvalRequired" checked={d?.plan.approvalRequired}>
                Engineering approval is required
              </Tick>
              <Tick name="approved" checked={d?.plan.approved}>
                Engineering approval is recorded
              </Tick>
              <Notes
                name="approvalNotes"
                title="Approval reference / planning notes"
                value={d?.plan.approvalNotes}
              />
            </Action>
          )}
          {modal.kind === "pick" && d && modal.line && (
            <PickWizard
              key={modal.line.materialId}
              plantId={plantId}
              detail={d}
              line={modal.line}
              writable={writable}
              close={close}
            />
          )}
          {modal.kind === "weigh" && d && modal.row && (
            <WeightForm
              plantId={plantId}
              pick={modal.row}
              line={modal.line!}
              writable={writable}
              close={close}
            />
          )}
          {modal.kind === "deliver" && d && (
            <Action
              title="Confirm kit delivery"
              endpoint={`staging/plans/${d.plan.id}/putaway`}
              plantId={plantId}
              disabled={!writable}
              onDone={close}
              build={(f) => ({
                kitCode: str(f, "kitCode"),
                destinationBin: str(f, "destinationBin"),
              })}
            >
              <div className="prep-modal-summary">
                <PackageCheck size={26} />
                <div>
                  <strong>{d.plan.kitCode}</strong>
                  <p>
                    Supply bin:{" "}
                    {d.bins.find((b) => b.id === d.area.supplyBinId)?.code}
                  </p>
                </div>
              </div>
              <Field title="Scan kit code" name="kitCode" />
              <Field title="Scan supply bin" name="destinationBin" />
              <p className="prep-hint">
                All BOM materials must be allocated and prepared. Delivery moves
                the kit; issue remains a separate production action.
              </p>
            </Action>
          )}
          {modal.kind === "return" && d && modal.row && (
            <Action
              title="Return to warehouse"
              endpoint={`staging/picks/${modal.row.id}/return`}
              plantId={plantId}
              disabled={!writable}
              onDone={close}
              build={(f) => ({
                destinationBin: str(f, "destinationBin"),
                reason: str(f, "reason"),
              })}
            >
              <p>
                Return {num(modal.row.quantity)} physical units and release{" "}
                {num(modal.row.requirementQuantity)} BOM credit. Issued material
                cannot be returned with this action.
              </p>
              <Select
                title="Return warehouse bin"
                name="destinationBin"
                rows={allBins
                  .filter(
                    (b) =>
                      b.id !== d.area.stagingBinId &&
                      b.id !== d.area.supplyBinId,
                  )
                  .map((b) => ({ ...b, id: b.code }))}
              />
              <Notes title="Return reason" name="reason" required />
            </Action>
          )}
          {modal.kind === "label" && d && (
            <div className="prep-label">
              <span className="prep-overline">
                GREEN INK · MATERIAL PREPARATION
              </span>
              <h3>{d.order.number}</h3>
              <img
                src={`/api/operations/staging/plans/${d.plan.id}/label.svg?plantId=${plantId}`}
                alt={`QR code for kit ${d.plan.kitCode}`}
              />
              <strong>{d.plan.kitCode}</strong>
              <p>
                {d.area.name} · {d.plan.workflow}
              </p>
              <small>Kit identity · Local browser print</small>
              <button className="prep-primary" onClick={() => window.print()}>
                <Printer size={16} />
                Print kit label
              </button>
            </div>
          )}
          {modal.kind === "request" && (
            <Action
              title="Create replenishment request"
              endpoint="staging/requests"
              plantId={plantId}
              disabled={!writable}
              onDone={close}
              build={(f) => ({
                materialId: str(f, "materialId"),
                destinationBinId: str(f, "destinationBinId"),
                stagingPlanId: str(f, "stagingPlanId") || null,
                quantity: Number(str(f, "quantity")),
                kind: str(f, "kind"),
                reason: str(f, "reason"),
              })}
            >
              <Select
                title="Requested material"
                name="materialId"
                rows={raw}
                defaultValue={modal.row?.materialId}
              />
              <Select
                title="Replenishment target bin"
                name="destinationBinId"
                rows={allBins}
                defaultValue={modal.row?.destinationBinId}
              />
              <Select
                title="Linked preparation plan"
                name="stagingPlanId"
                optional
                rows={list
                  .filter((o) => o.planId && o.status === "Released")
                  .map((o) => ({ id: o.planId, name: o.number }))}
              />
              <div className="prep-field-grid">
                <Field
                  title="Request quantity"
                  name="quantity"
                  type="number"
                  min={0.000001}
                  value={modal.row?.quantity}
                />
                <label>
                  Request type
                  <select
                    name="kind"
                    aria-label="Request type"
                    defaultValue={modal.row?.kind || "RMShortage"}
                  >
                    {["RMShortage", "Immediate", "Kanban"].map((k) => (
                      <option key={k}>{k}</option>
                    ))}
                  </select>
                </label>
              </div>
              <Notes
                title="Why is material needed?"
                name="reason"
                required
                value={modal.row?.reason}
              />
            </Action>
          )}
          {modal.kind === "fulfil" && modal.row && (
            <Action
              title="Confirm replenishment delivery"
              endpoint={`staging/requests/${modal.row.id}/fulfil`}
              plantId={plantId}
              disabled={!writable}
              onDone={close}
              build={(f) => {
                const s = eligible.find(
                  (s) => s.pallet.id === str(f, "palletId"),
                );
                return {
                  palletId: str(f, "palletId"),
                  sourceBin: s?.pallet.location,
                  quantity: Number(str(f, "quantity")),
                };
              }}
            >
              <p>
                Outstanding:{" "}
                {num(modal.row.quantity - modal.row.fulfilledQuantity)}{" "}
                {
                  materials.data?.find((m) => m.id === modal.row?.materialId)
                    ?.uom
                }
              </p>
              <Select
                title="Replenishment source label"
                name="palletId"
                rows={eligible
                  .filter((s) => s.material.id === modal.row?.materialId)
                  .map((s) => ({
                    id: s.pallet.id,
                    name: `${s.batch.number} · ${s.pallet.location} · ${num(s.pallet.quantity - s.pallet.reservedQuantity)} available · ${s.pallet.code}`,
                  }))}
              />
              <Field
                title="Delivery quantity"
                name="quantity"
                type="number"
                min={0.000001}
              />
              <p className="prep-hint">
                Partial deliveries are supported. Cancellation locks after the
                first delivery.
              </p>
            </Action>
          )}
          {modal.kind === "cancel" && modal.row && (
            <Action
              title="Confirm cancellation"
              endpoint={`staging/requests/${modal.row.id}/cancel`}
              plantId={plantId}
              disabled={!writable}
              onDone={close}
              build={(f) => ({ reason: str(f, "reason") })}
            >
              <Notes title="Cancellation reason" name="reason" required />
              <p className="prep-hint">
                Only requests with no picking or delivery started can be
                cancelled.
              </p>
            </Action>
          )}
          {modal.kind === "transfer" && modal.row && (
            <Action
              title="Confirm stock transfer"
              endpoint="staging/transfers"
              plantId={plantId}
              disabled={!writable}
              onDone={close}
              build={(f) => ({
                palletId: modal.row!.pallet.id,
                sourceBin: str(f, "sourceBin"),
                destinationBin: str(f, "destinationBin"),
                quantity: Number(str(f, "quantity")),
                reason: str(f, "reason"),
              })}
            >
              <p>
                {modal.row.material.name} · {modal.row.batch.number}
                <br />
                {num(
                  modal.row.pallet.quantity - modal.row.pallet.reservedQuantity,
                )}{" "}
                {modal.row.material.uom} unreserved at{" "}
                {modal.row.pallet.location}
              </p>
              <Field title="Scan transfer source bin" name="sourceBin" />
              <Select
                title="Transfer destination"
                name="destinationBin"
                rows={allBins
                  .filter((b) => b.code !== modal.row?.pallet.location)
                  .map((b) => ({ ...b, id: b.code }))}
              />
              <Field
                title="Transfer quantity"
                name="quantity"
                type="number"
                min={0.000001}
              />
              <Notes title="Transfer reason" name="reason" required />
            </Action>
          )}
          {modal.kind === "issue" && modal.row && (
            <Action
              title="Record stock issue"
              endpoint="staging/stock-issues"
              plantId={plantId}
              disabled={!writable}
              onDone={close}
              build={(f) => ({
                palletId: modal.row!.pallet.id,
                kind: str(f, "kind"),
                notes: str(f, "notes"),
                countedQuantity:
                  str(f, "countedQuantity") === ""
                    ? null
                    : Number(str(f, "countedQuantity")),
              })}
            >
              <p>
                {modal.row.material.name} · System quantity{" "}
                {num(modal.row.pallet.quantity)} {modal.row.material.uom}
              </p>
              <label>
                Issue type
                <select name="kind" aria-label="Issue type">
                  {[
                    "StockNotFound",
                    "BatchMismatch",
                    "BinMismatch",
                    "Loss",
                    "Gain",
                  ].map((k) => (
                    <option key={k}>{k}</option>
                  ))}
                </select>
              </label>
              <Field
                title="Physical counted quantity"
                name="countedQuantity"
                type="number"
                min={0}
                required={false}
              />
              <Notes title="What did you find?" name="notes" required />
              <p className="prep-hint">
                This label is removed from picking suggestions until reviewed. A
                manager can apply a verified loss/gain count; the original stock
                ledger stays intact.
              </p>
            </Action>
          )}
          {modal.kind === "resolve" && modal.row && (
            <Action
              title="Resolve stock issue"
              endpoint={`staging/stock-issues/${modal.row.id}/resolve`}
              plantId={plantId}
              disabled={!manager}
              onDone={close}
              build={(f) => ({
                applyCount: f.has("applyCount"),
                resolution: str(f, "resolution"),
              })}
            >
              <p>{modal.row.notes}</p>
              <p>
                System at report: {num(modal.row.systemQuantity)} · Physical
                count:{" "}
                {modal.row.countedQuantity === null
                  ? "Not recorded"
                  : num(modal.row.countedQuantity)}
              </p>
              {["Loss", "Gain"].includes(modal.row.kind) && (
                <Tick name="applyCount">
                  Apply the verified count as an auditable stock adjustment
                </Tick>
              )}
              <Notes
                title="Review findings / resolution"
                name="resolution"
                required
              />
              <p className="prep-hint">
                Adjustment is blocked if stock changed after counting or would
                fall below reserved quantity.
              </p>
            </Action>
          )}
        </Dialog>
      )}
    </div>
  );
}
function PanelHeading({
  title,
  subtitle,
  action,
}: {
  title: string;
  subtitle: string;
  action?: ReactNode;
}) {
  return (
    <header className="prep-panel-heading">
      <div>
        <h2>{title}</h2>
        <p>{subtitle}</p>
      </div>
      {action}
    </header>
  );
}
function IssueButton({
  plantId,
  reservationId,
  disabled,
}: {
  plantId: string;
  reservationId: string;
  disabled: boolean;
}) {
  return (
    <Action
      title="Issue to production"
      endpoint={`reservations/${reservationId}/issue`}
      plantId={plantId}
      disabled={disabled}
      build={() => ({})}
    >
      <></>
    </Action>
  );
}
function PickWizard({
  plantId,
  detail,
  line,
  writable,
  close,
}: {
  plantId: string;
  detail: Detail;
  line: Row;
  writable: boolean;
  close: () => void;
}) {
  const [step, setStep] = useState(1),
    [sourceId, setSourceId] = useState(""),
    [barcode, setBarcode] = useState(""),
    [credit, setCredit] = useState(""),
    [quantity, setQuantity] = useState(""),
    [gross, setGross] = useState(""),
    [tare, setTare] = useState("0");
  const remaining =
    detail.progress.find((p) => p.materialId === line.materialId)?.remaining ||
    0;
  const sources = detail.sources.filter(
    (s) =>
      s.materialId === line.materialId &&
      (!barcode ||
        `${s.code} ${s.location} ${s.batchNumber}`
          .toLowerCase()
          .includes(barcode.toLowerCase())),
  );
  const source = detail.sources.find((s) => s.id === sourceId),
    factor = line.correctionRequired ? source?.factor : 1;
  const expected = Math.round(Number(credit) * Number(factor || 0) * 1e6) / 1e6,
    net = Math.round((Number(gross) - Number(tare)) * 1e6) / 1e6;
  const separate = detail.plan.workflow === "Separate",
    actual = separate ? Number(quantity) : net;
  const tolerance = (expected * line.tolerancePercent) / 100,
    validWeight =
      actual > 0 &&
      (separate
        ? actual >= expected
        : actual >= expected - tolerance && actual <= expected + tolerance);
  function choose(s: Row) {
    setSourceId(s.id);
    const f = line.correctionRequired ? s.factor : 1;
    const q = Math.min(
      remaining,
      Math.floor((s.available / (f || 1)) * 1e6) / 1e6,
    );
    setCredit(String(q));
    const physical = Math.round(q * (f || 1) * 1e6) / 1e6;
    const rounded =
      line.packSize > 0
        ? Math.ceil(physical / line.packSize) * line.packSize
        : physical;
    setQuantity(String(Math.min(s.available, rounded)));
  }
  return (
    <>
      <ol className="prep-wizard-steps">
        <li aria-current={step === 1 ? "step" : undefined}>
          <span>1</span>Select stock
        </li>
        <li aria-current={step === 2 ? "step" : undefined}>
          <span>2</span>
          {separate ? "Confirm pick" : "Weigh & confirm"}
        </li>
      </ol>
      <div className="prep-modal-summary">
        <Boxes size={25} />
        <div>
          <strong>
            {line.code} · {line.name}
          </strong>
          <p>
            {num(remaining)} {line.uom} BOM credit still to prepare
          </p>
        </div>
      </div>
      {step === 1 ? (
        <>
          <label className="prep-search">
            <Search size={16} />
            <input
              aria-label="Scan or search preparation stock"
              placeholder="Scan label, batch or bin…"
              value={barcode}
              onChange={(e) => setBarcode(e.target.value)}
            />
          </label>
          <div className="prep-source-list">
            {sources.map((s, i) => (
              <button
                type="button"
                key={s.id}
                className={sourceId === s.id ? "selected" : ""}
                disabled={line.correctionRequired && !s.factor}
                onClick={() => choose(s)}
              >
                <span className="prep-source-icon">
                  <Warehouse size={20} />
                </span>
                <span>
                  <strong>
                    {s.location} · {s.batchNumber}
                  </strong>
                  <small>{s.code}</small>
                  <small>
                    {s.expiryDate
                      ? `Expires ${new Date(s.expiryDate).toLocaleDateString()}`
                      : "No expiry recorded"}
                    {line.correctionRequired
                      ? ` · Factor ${s.factor || "approval needed"}`
                      : ""}
                  </small>
                </span>
                <span>
                  <strong>
                    {num(s.available)} {line.uom}
                  </strong>
                  {i === 0 && <Status tone="green">FEFO first</Status>}
                  {sourceId === s.id && <CheckCircle2 size={18} />}
                </span>
              </button>
            ))}
          </div>
          {!sources.length && (
            <Empty title="No eligible stock">
              Receive or replenish this material, release it in Quality, or
              resolve its stock issues.
            </Empty>
          )}
          {detail.sourcesTruncated && (
            <p className="prep-hint">
              Showing the first 500 eligible source labels.
            </p>
          )}
          <footer className="prep-wizard-footer">
            <span>Released, unexpired and unreserved stock only.</span>
            <button
              className="prep-primary"
              disabled={!sourceId}
              onClick={() => setStep(2)}
            >
              Continue
              <ArrowRight size={16} />
            </button>
          </footer>
        </>
      ) : (
        source && (
          <Action
            title={separate ? "Confirm material pick" : "Confirm pick & kit"}
            endpoint={`staging/plans/${detail.plan.id}/pick`}
            plantId={plantId}
            disabled={!writable}
            blocked={!validWeight}
            onDone={close}
            build={(f) => ({
              palletId: source.id,
              palletCode: str(f, "palletCode"),
              ruleVersion: line.ruleVersion ?? 0,
              factorVersion: line.correctionRequired ? source.factorVersion : 0,
              sourceBin: str(f, "sourceBin"),
              quantity: actual,
              requirementQuantity: Number(credit),
              gross: separate ? null : Number(gross),
              tare: separate ? null : Number(tare),
              scaleReference: separate ? null : str(f, "scaleReference"),
              safetyConfirmed: f.has("safetyConfirmed"),
            })}
          >
            <div className="prep-selected-source">
              <Warehouse size={19} />
              <div>
                <strong>
                  {source.location} · {source.batchNumber}
                </strong>
                <small>
                  {num(source.available)} {line.uom} available
                </small>
              </div>
              <button
                className="prep-text-button"
                type="button"
                onClick={() => setStep(1)}
              >
                Change
              </button>
            </div>
            <Field title="Scan source bin" name="sourceBin" />
            <Field title="Scan material label" name="palletCode" />
            <label>
              BOM quantity to prepare
              <input
                aria-label="BOM quantity to prepare"
                type="number"
                min="0.000001"
                step="0.000001"
                max={remaining}
                required
                value={credit}
                onChange={(e) => setCredit(e.target.value)}
              />
            </label>
            <div className="prep-target">
              <span>
                Physical target
                {line.correctionRequired ? ` · factor ${num(factor)}` : ""}
              </span>
              <strong>
                {num(expected)} <small>{line.uom}</small>
              </strong>
              <p>
                {separate
                  ? "You may pick a full bag; weigh the exact amount at kitting."
                  : `Allowed range ${num(expected - tolerance)}–${num(expected + tolerance)} ${line.uom} (±${num(line.tolerancePercent)}%)`}
              </p>
            </div>
            {separate ? (
              <label>
                Physical quantity to pick
                <input
                  aria-label="Physical quantity to pick"
                  type="number"
                  min="0.000001"
                  step="0.000001"
                  max={source.available}
                  required
                  value={quantity}
                  onChange={(e) => setQuantity(e.target.value)}
                />
              </label>
            ) : (
              <>
                <WeightInputs
                  gross={gross}
                  tare={tare}
                  setGross={setGross}
                  setTare={setTare}
                  net={net}
                  uom={line.uom}
                  valid={validWeight}
                />
                <Field
                  title="Scale / reading reference"
                  name="scaleReference"
                  required={false}
                />
              </>
            )}
            {line.safetyNotes && (
              <div className="prep-safety">
                <ShieldCheck size={20} />
                <div>
                  <strong>Safety & PPE</strong>
                  <p>{line.safetyNotes}</p>
                  <Tick name="safetyConfirmed">
                    I have read the safety instructions
                  </Tick>
                </div>
              </div>
            )}
            {line.handlingNotes && (
              <p className="prep-handling">
                <ClipboardList size={18} />
                {line.handlingNotes}
              </p>
            )}
            <p className="prep-hint">
              {separate
                ? "Stock moves to staging and is reserved for this order. It is not consumed."
                : "The measured allocation moves to production supply and remains reserved until issued."}
            </p>
          </Action>
        )
      )}
    </>
  );
}
function WeightInputs({
  gross,
  tare,
  setGross,
  setTare,
  net,
  uom,
  valid,
}: {
  gross: string;
  tare: string;
  setGross: (s: string) => void;
  setTare: (s: string) => void;
  net: number;
  uom: string;
  valid: boolean;
}) {
  return (
    <>
      <div className="prep-field-grid">
        <label>
          Gross weight
          <input
            aria-label="Gross weight"
            type="number"
            min="0.000001"
            step="0.000001"
            required
            value={gross}
            onChange={(e) => setGross(e.target.value)}
          />
        </label>
        <label>
          Tare weight
          <input
            aria-label="Tare weight"
            type="number"
            min={0}
            step="0.000001"
            required
            value={tare}
            onChange={(e) => setTare(e.target.value)}
          />
        </label>
      </div>
      <div className={`prep-weight-display ${valid ? "within" : ""}`}>
        <div>
          <span>NET WEIGHT</span>
          <strong>
            {num(net)} <small>{uom}</small>
          </strong>
        </div>
        <Status tone={valid ? "green" : "amber"}>
          {valid ? "Within tolerance" : "Enter valid readings"}
        </Status>
      </div>
      <p className="prep-hint">
        Manual scale reading. No live weighing device is connected.
      </p>
    </>
  );
}
function WeightForm({
  plantId,
  pick,
  line,
  writable,
  close,
}: {
  plantId: string;
  pick: Row;
  line: Row;
  writable: boolean;
  close: () => void;
}) {
  const [gross, setGross] = useState(""),
    [tare, setTare] = useState("0");
  const net = Math.round((Number(gross) - Number(tare)) * 1e6) / 1e6,
    expected =
      Math.round(pick.requirementQuantity * pick.correctionFactor * 1e6) / 1e6,
    delta = (expected * pick.tolerancePercent) / 100;
  const valid = net > 0 && net >= expected - delta && net <= expected + delta;
  return (
    <Action
      title="Confirm weighed kit"
      endpoint={`staging/picks/${pick.id}/weigh`}
      plantId={plantId}
      disabled={!writable}
      blocked={!valid}
      onDone={close}
      build={(f) => ({
        gross: Number(gross),
        tare: Number(tare),
        scaleReference: str(f, "scaleReference"),
      })}
    >
      <div className="prep-target">
        <span>Physical target · factor {num(pick.correctionFactor)}</span>
        <strong>
          {num(expected)} <small>{line.uom}</small>
        </strong>
        <p>
          Allowed range {num(expected - delta)}–{num(expected + delta)}{" "}
          {line.uom}
        </p>
      </div>
      <WeightInputs
        {...{ gross, tare, setGross, setTare, net, uom: line.uom, valid }}
      />
      <Field
        title="Scale / reading reference"
        name="scaleReference"
        required={false}
      />
      <p className="prep-hint">
        Any unneeded stock from the picked bag remains unreserved in staging.
        The kit retains its own label and the original batch.
      </p>
    </Action>
  );
}
