import {
  useEffect,
  useRef,
  useState,
  type ReactNode,
  type FormEvent,
} from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Activity,
  ArrowRight,
  Boxes,
  CalendarClock,
  Check,
  CheckCircle2,
  ClipboardCheck,
  ClipboardList,
  Download,
  FlaskConical,
  History,
  Layers3,
  PackageCheck,
  Pause,
  Play,
  Plus,
  Printer,
  Search,
  ShieldCheck,
  SlidersHorizontal,
  Timer,
  X,
} from "lucide-react";
import { operationApi } from "./Operations";
import "./manufacturing.css";

type Row = Record<string, any> & { id: string };
type Block = { items: Row[]; hasMore: boolean };
type Modal = { kind: string; row?: Row } | null;
const num = (v: unknown) =>
  Number(v || 0).toLocaleString(undefined, { maximumFractionDigits: 6 });
const when = (v: string) =>
  v
    ? new Date(v).toLocaleString(undefined, {
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      })
    : "—";
const read = (v: string, fallback: any = []) => {
  try {
    return JSON.parse(v);
  } catch {
    return fallback;
  }
};
const str = (f: FormData, n: string) => String(f.get(n) || "");
const eventNotes = (text: string) => {
  const value = read(text, null);
  if (value?.evidence)
    return `Confirmed checks: ${(value.evidence.checks || []).join(", ") || "None"}. Readings: ${(value.evidence.parameters || []).map((p: any) => `${p.code}: ${num(p.value)}`).join(", ") || "None"}. ${value.evidence.notes || ""}`;
  if (value?.newSchedule)
    return `Moved from ${when(value.oldSchedule)} to ${when(value.newSchedule)}. ${value.reason}`;
  return text;
};

const localDate = () => {
  const d = new Date();
  return new Date(d.getTime() - d.getTimezoneOffset() * 60000)
    .toISOString()
    .slice(0, 16);
};
function useBlock(plantId: string, path: string) {
  return useQuery({
    queryKey: ["operations", plantId, "manufacturing", path],
    queryFn: () =>
      operationApi<Block>(`manufacturing/${path}?plantId=${plantId}`),
    retry: false,
  });
}
function Badge({ children }: { children: ReactNode }) {
  return (
    <span className={`mfg-badge ${String(children).toLowerCase()}`}>
      {children}
    </span>
  );
}
function Empty({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="mfg-empty">
      <Layers3 size={32} />
      <h3>{title}</h3>
      <p>{children}</p>
    </div>
  );
}
function Select({
  label,
  name,
  rows,
  value,
  onChange,
}: {
  label: string;
  name: string;
  rows: Row[];
  value?: string;
  onChange?: (v: string) => void;
}) {
  return (
    <label>
      {label}
      <select
        name={name}
        aria-label={label}
        required
        value={value}
        onChange={onChange ? (e) => onChange(e.target.value) : undefined}
        defaultValue={value === undefined ? "" : undefined}
      >
        <option value="">Select {label.toLowerCase()}</option>
        {rows.map((r) => (
          <option key={r.id} value={r.id}>
            {r.label ||
              r.number ||
              `${r.code || r.name}${r.code && r.name ? ` · ${r.name}` : ""}`}
          </option>
        ))}
      </select>
    </label>
  );
}
function Input({
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
        name={name}
        maxLength={2000}
        rows={3}
        required={required}
        defaultValue={value}
      />
    </label>
  );
}
function CheckBox({
  label,
  name,
  checked = false,
}: {
  label: string;
  name: string;
  checked?: boolean;
}) {
  return (
    <label className="mfg-check">
      <input type="checkbox" name={name} defaultChecked={checked} />
      <span>{label}</span>
    </label>
  );
}
function Panel({
  title,
  children,
  wide = false,
  onClose,
}: {
  title: string;
  children: ReactNode;
  wide?: boolean;
  onClose: () => void;
}) {
  const dialog = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const previous = document.activeElement as HTMLElement;
    dialog.current?.showModal();
    return () => previous?.focus();
  }, []);
  return (
    <dialog
      ref={dialog}
      className={`mfg-dialog ${wide ? "wide" : ""}`}
      aria-labelledby="mfg-dialog-title"
      onCancel={onClose}
    >
      <header>
        <div>
          <span className="eyebrow">GREEN INK · CONTROLLED WORKFLOW</span>
          <h2 id="mfg-dialog-title">{title}</h2>
        </div>
        <button
          className="mfg-icon"
          aria-label="Close dialog"
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
  children,
  onDone,
  disabled = false,
}: {
  title: string;
  path: string;
  plantId: string;
  build: (f: FormData) => object;
  children?: ReactNode;
  onDone?: (id: string) => void;
  disabled?: boolean;
}) {
  const cache = useQueryClient(),
    [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  const retry = useRef({ signature: "", id: "" });
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError("");
    setBusy(true);
    try {
      const input = { plantId, ...build(new FormData(e.currentTarget)) };
      const signature = JSON.stringify({ path, input });
      if (signature !== retry.current.signature)
        retry.current = { signature, id: crypto.randomUUID() };
      const result = await operationApi<{ resultId?: string; id?: string }>(
        path,
        { ...input, commandId: retry.current.id },
      );
      retry.current = { signature: "", id: "" };
      await cache.invalidateQueries({ queryKey: ["operations"] });
      onDone?.(result.resultId || result.id || "");
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <form className="mfg-form" onSubmit={submit}>
      <fieldset disabled={disabled || busy}>{children}</fieldset>
      {error && (
        <p role="alert" className="mfg-error">
          {error}
        </p>
      )}
      <footer>
        <button className="primary" disabled={disabled || busy}>
          {busy ? "Saving…" : title}
        </button>
      </footer>
    </form>
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
const testDefault = () => ({
  code: "",
  name: "",
  kind: "Numeric",
  uom: "",
  minimum: "",
  maximum: "",
});
function SpecificationEditor({
  plantId,
  materials,
  onDone,
}: {
  plantId: string;
  materials: Row[];
  onDone: () => void;
}) {
  const [tests, setTests] = useState([testDefault()]);
  return (
    <Action
      title="Save specification draft"
      path="manufacturing/specifications"
      plantId={plantId}
      onDone={onDone}
      build={(f) => ({
        materialId: str(f, "material"),
        name: str(f, "name"),
        tests: tests.map((t) => ({
          ...t,
          minimum: t.kind === "Numeric" ? Number(t.minimum) : null,
          maximum: t.kind === "Numeric" ? Number(t.maximum) : null,
        })),
      })}
    >
      <p className="mfg-note">
        Create a revision, then publish it for sampling. Each collected sample
        keeps its own copy of these test limits.
      </p>
      <div className="mfg-fields">
        <Select
          label="Specification material"
          name="material"
          rows={materials}
        />
        <Input label="Specification name" name="name" />
      </div>
      {tests.map((t, i) => (
        <div className="mfg-editor-row" key={i}>
          <h3>Test {i + 1}</h3>
          <div className="mfg-fields">
            {(
              [
                ["code", "Test code"],
                ["name", "Test name"],
                ["uom", "Unit"],
              ] as const
            ).map(([key, label]) => (
              <label key={key}>
                {label} {i + 1}
                <input
                  value={t[key]}
                  required={key !== "uom"}
                  maxLength={key === "code" ? 32 : 160}
                  onChange={(e) =>
                    setTests(
                      tests.map((x, j) =>
                        j === i
                          ? {
                              ...x,
                              [key]:
                                key === "code"
                                  ? e.target.value.toUpperCase()
                                  : e.target.value,
                            }
                          : x,
                      ),
                    )
                  }
                />
              </label>
            ))}
            <label>
              Test type {i + 1}
              <select
                value={t.kind}
                onChange={(e) =>
                  setTests(
                    tests.map((x, j) =>
                      j === i ? { ...x, kind: e.target.value } : x,
                    ),
                  )
                }
              >
                <option value="Numeric">Numeric limits</option>
                <option value="PassFail">Pass / fail</option>
              </select>
            </label>
            {t.kind === "Numeric" &&
              (
                [
                  ["minimum", "Lower limit"],
                  ["maximum", "Upper limit"],
                ] as const
              ).map(([key, label]) => (
                <label key={key}>
                  {label} {i + 1}
                  <input
                    type="number"
                    step="0.000001"
                    required
                    value={t[key]}
                    onChange={(e) =>
                      setTests(
                        tests.map((x, j) =>
                          j === i ? { ...x, [key]: e.target.value } : x,
                        ),
                      )
                    }
                  />
                </label>
              ))}
          </div>
          {tests.length > 1 && (
            <Button onClick={() => setTests(tests.filter((_, j) => j !== i))}>
              Remove test {i + 1}
            </Button>
          )}
        </div>
      ))}
      <button
        type="button"
        className="secondary"
        disabled={tests.length >= 30}
        onClick={() => setTests([...tests, testDefault()])}
      >
        <Plus size={16} />
        Add test
      </button>
    </Action>
  );
}
const phaseDefault = () => ({
  name: "",
  instructions: "",
  requiresSample: false,
  checks: "",
  parameters: [] as ReturnType<typeof testDefault>[],
});
function RecipeEditor({
  plantId,
  materials,
  onDone,
}: {
  plantId: string;
  materials: Row[];
  onDone: () => void;
}) {
  const [phases, setPhases] = useState([phaseDefault()]);
  const update = (i: number, values: object) =>
    setPhases(phases.map((p, j) => (j === i ? { ...p, ...values } : p)));
  return (
    <Action
      title="Save SOP draft"
      path="manufacturing/recipes"
      plantId={plantId}
      onDone={onDone}
      build={(f) => ({
        code: str(f, "code"),
        name: str(f, "name"),
        materialId: str(f, "material"),
        precautions: str(f, "precautions"),
        phases: phases.map((p) => ({
          ...p,
          checks: p.checks
            .split("\n")
            .map((x) => x.trim())
            .filter(Boolean),
          parameters: p.parameters.map((t) => ({
            code: t.code,
            name: t.name,
            uom: t.uom,
            minimum: Number(t.minimum),
            maximum: Number(t.maximum),
          })),
        })),
      })}
    >
      <p className="mfg-note">
        A published revision is immutable. Reuse its code to create the next
        revision. Runs keep their selected revision and phase requirements.
      </p>
      <div className="mfg-fields">
        <Input label="SOP code" name="code" />
        <Input label="SOP name" name="name" />
        <Select
          label="Finished material for SOP"
          name="material"
          rows={materials.filter((x) => x.kind === "Finished")}
        />
      </div>
      <Notes
        label="Safety precautions and production notes"
        name="precautions"
      />
      {phases.map((p, i) => (
        <div className="mfg-editor-row" key={i}>
          <div className="mfg-row">
            <h3>Phase {String(i + 1).padStart(2, "0")}</h3>
            {phases.length > 1 && (
              <Button
                onClick={() => setPhases(phases.filter((_, j) => j !== i))}
              >
                Remove phase {i + 1}
              </Button>
            )}
          </div>
          <div className="mfg-fields">
            <label>
              Phase name {i + 1}
              <input
                required
                maxLength={160}
                value={p.name}
                onChange={(e) => update(i, { name: e.target.value })}
              />
            </label>
            <label>
              Instructions {i + 1}
              <textarea
                maxLength={2000}
                value={p.instructions}
                onChange={(e) => update(i, { instructions: e.target.value })}
              />
            </label>
          </div>
          <label>
            Checklist prompts {i + 1}
            <textarea
              placeholder="One required check per line"
              value={p.checks}
              onChange={(e) => update(i, { checks: e.target.value })}
            />
          </label>
          <label className="mfg-check">
            <input
              type="checkbox"
              checked={p.requiresSample}
              onChange={(e) => update(i, { requiresSample: e.target.checked })}
            />
            <span>Require passed laboratory sample for phase {i + 1}</span>
          </label>
          {p.parameters.map((t, k) => (
            <div className="mfg-param-editor" key={k}>
              {(
                [
                  ["code", "Parameter code"],
                  ["name", "Parameter name"],
                  ["uom", "Parameter unit"],
                  ["minimum", "Parameter minimum"],
                  ["maximum", "Parameter maximum"],
                ] as const
              ).map(([key, label]) => (
                <label key={key}>
                  {label} {i + 1}.{k + 1}
                  <input
                    required={key !== "uom"}
                    maxLength={key === "code" ? 32 : 160}
                    type={
                      ["minimum", "maximum"].includes(key) ? "number" : "text"
                    }
                    step="0.000001"
                    value={t[key]}
                    onChange={(e) =>
                      update(i, {
                        parameters: p.parameters.map((x, j) =>
                          j === k
                            ? {
                                ...x,
                                [key]:
                                  key === "code"
                                    ? e.target.value.toUpperCase()
                                    : e.target.value,
                              }
                            : x,
                        ),
                      })
                    }
                  />
                </label>
              ))}
              <button
                type="button"
                className="mfg-icon"
                aria-label={`Remove parameter ${i + 1}.${k + 1}`}
                onClick={() =>
                  update(i, {
                    parameters: p.parameters.filter((_, j) => j !== k),
                  })
                }
              >
                <X size={16} />
              </button>
            </div>
          ))}
          <button
            type="button"
            className="secondary"
            disabled={p.parameters.length >= 20}
            onClick={() =>
              update(i, { parameters: [...p.parameters, testDefault()] })
            }
          >
            Add parameter to phase {i + 1}
          </button>
        </div>
      ))}
      <button
        type="button"
        className="secondary"
        disabled={phases.length >= 30}
        onClick={() => setPhases([...phases, phaseDefault()])}
      >
        <Plus size={16} />
        Add phase
      </button>
    </Action>
  );
}
export function ManufacturingWorkspace({
  module,
  plantId,
  role,
  data,
  children,
}: {
  module: string;
  plantId: string;
  role: string;
  data: Record<string, any[]>;
  children: ReactNode;
}) {
  const manager = ["PlatformAdmin", "TenantAdmin", "PlantManager"].includes(
      role,
    ),
    writable = role !== "Viewer";
  const resources = useBlock(plantId, "resources"),
    recipes = useBlock(plantId, "recipes"),
    runs = useBlock(plantId, "runs"),
    specs = useBlock(plantId, "specifications"),
    samples = useBlock(plantId, "samples"),
    cases = useBlock(plantId, "quality-cases"),
    packs = useBlock(plantId, "packs");
  const [tab, setTab] = useState(
      module === "production"
        ? "Execution"
        : module === "quality"
          ? "Laboratory"
          : "Stock",
    ),
    [modal, setModal] = useState<Modal>(null),
    [search, setSearch] = useState(""),
    [filter, setFilter] = useState("Active"),
    [runId, setRunId] = useState(""),
    [sampleId, setSampleId] = useState(""),
    [label, setLabel] = useState<any>(null),
    [labelError, setLabelError] = useState("");
  const run = useQuery({
    queryKey: ["operations", plantId, "run", runId],
    queryFn: () =>
      operationApi<any>(`manufacturing/runs/${runId}?plantId=${plantId}`),
    enabled: !!runId,
    retry: false,
  });
  const sample = useQuery({
    queryKey: ["operations", plantId, "sample", sampleId],
    queryFn: () =>
      operationApi<any>(`manufacturing/samples/${sampleId}?plantId=${plantId}`),
    enabled: !!sampleId,
    retry: false,
  });
  const rows = (q: { data?: Block }) => q.data?.items || [],
    materials = data.materials || [],
    orders = data["production-orders"] || [],
    batches = data.batches || [],
    stock = data["finished-goods"] || [];
  const material = (id: string) => materials.find((m) => m.id === id),
    order = (id: string) => orders.find((o) => o.id === id);
  const close = () => setModal(null),
    M = "manufacturing/";
  const queries = [resources, recipes, runs, specs, samples, cases, packs];
  const error = queries.find((q) => q.error)?.error?.message;
  const tabs =
    module === "production"
      ? ["Execution", "Orders & output", "SOP library", "Resources"]
      : module === "quality"
        ? ["Laboratory", "Nonconformances", "Specifications", "Batch decisions"]
        : ["Stock", "Packing history", "Inventory records"];
  const activeRuns = rows(runs).filter((r) => r.status !== "Completed"),
    openCases = rows(cases).filter((c) => c.status === "Open");
  const metrics =
    module === "production"
      ? [
          [
            "Scheduled",
            rows(runs).filter((r) => r.status === "Scheduled").length,
            CalendarClock,
          ],
          [
            "Running",
            rows(runs).filter((r) => r.status === "Running").length,
            Activity,
          ],
          [
            "Paused",
            rows(runs).filter((r) => r.status === "Paused").length,
            Pause,
          ],
          [
            "Completed",
            rows(runs).filter((r) => r.status === "Completed").length,
            CheckCircle2,
          ],
        ]
      : module === "quality"
        ? [
            [
              "Awaiting lab",
              rows(samples).filter((r) =>
                ["Collected", "HandedOver", "Received"].includes(r.status),
              ).length,
              ClipboardList,
            ],
            [
              "Testing",
              rows(samples).filter((r) => r.status === "Testing").length,
              FlaskConical,
            ],
            [
              "Passed",
              rows(samples).filter((r) => r.status === "Passed").length,
              CheckCircle2,
            ],
            ["Open deviations", openCases.length, ShieldCheck],
          ]
        : [
            [
              "Pallets on hand",
              stock.filter((r) => r.quantity > 0).length,
              Boxes,
            ],
            [
              "Released pallets",
              stock.filter(
                (r) => r.quantity > 0 && r.qualityStatus === "Released",
              ).length,
              PackageCheck,
            ],
            [
              "On quality hold",
              stock.filter((r) => r.quantity > 0 && r.qualityStatus === "Hold")
                .length,
              ShieldCheck,
            ],
            ["Packed records", rows(packs).length, Layers3],
          ];
  function match(r: Row) {
    return JSON.stringify(r).toLowerCase().includes(search.toLowerCase());
  }
  function exportEvents() {
    const events = run.data?.events || [];
    const safe = (v: unknown) => {
      const text = String(v ?? "");
      return (
        '"' +
        (/^[\s=+\-@]/.test(text) ? "'" : "") +
        text.replaceAll('"', '""') +
        '"'
      );
    };
    const csv = [
      ["Time", "Event", "Notes"],
      ...events.map((e: Row) => [e.createdAt, e.kind, e.notes]),
    ]
      .map((row) => row.map(safe).join(","))
      .join("\r\n");
    const url = URL.createObjectURL(
      new Blob([csv], { type: "text/csv;charset=utf-8" }),
    );
    const a = document.createElement("a");
    a.href = url;
    a.download = "GreenInk-execution-history.csv";
    a.click();
    URL.revokeObjectURL(url);
  }
  return (
    <section className="mfg-workspace">
      <div className="mfg-banner">
        <div>
          <span className="eyebrow">
            {module === "production"
              ? "EXECUTE WITH CONFIDENCE"
              : module === "quality"
                ? "EVIDENCE BEFORE RELEASE"
                : "FROM OUTPUT TO AVAILABLE STOCK"}
          </span>
          <h2>
            {module === "production"
              ? "Every phase. One clear path."
              : module === "quality"
                ? "Quality, with a complete trail."
                : "Finished, checked, ready."}
          </h2>
          <p>
            {module === "production"
              ? "Bring SOPs, materials and people together on the shop floor."
              : module === "quality"
                ? "Follow each sample from collection to a reviewed decision."
                : "Pack accurately and move every pallet to its destination."}
          </p>
        </div>
        <div className="mfg-banner-mark">
          {module === "production" ? (
            <Activity />
          ) : module === "quality" ? (
            <FlaskConical />
          ) : (
            <PackageCheck />
          )}
        </div>
      </div>
      {error && (
        <p role="alert" className="mfg-error">
          {error} Refresh to load the workspace.
        </p>
      )}
      <div className="mfg-metrics">
        {metrics.map(([title, value, Icon]: any) => (
          <article key={title}>
            <Icon size={21} />
            <span>{title}</span>
            <strong>{value}</strong>
            <small>Recent plant records</small>
          </article>
        ))}
      </div>
      <nav className="mfg-tabs" aria-label="Module workspaces">
        {tabs.map((t) => (
          <button
            role="tab"
            aria-selected={tab === t}
            key={t}
            onClick={() => {
              setTab(t);
              setSearch("");
            }}
          >
            {t}
          </button>
        ))}
      </nav>
      {queries.some((q) => q.data?.hasMore) && (
        <p className="mfg-note">
          Showing up to 200 recent records per list. Older records are not
          included in these counts.
        </p>
      )}
      {tab === "Orders & output" ||
      tab === "Batch decisions" ||
      tab === "Inventory records" ? (
        <div className="mfg-existing">{children}</div>
      ) : null}
      {tab === "Execution" && (
        <>
          <div className="mfg-toolbar">
            <div>
              <h2>Production execution</h2>
              <p>Choose a run to work through its saved SOP.</p>
            </div>
            <button
              className="primary"
              disabled={!manager}
              onClick={() => setModal({ kind: "schedule" })}
            >
              <Plus size={17} />
              Schedule execution
            </button>
          </div>
          <div className="mfg-split">
            <aside className="mfg-list">
              <label className="mfg-search">
                <Search size={17} />
                <input
                  aria-label="Search production runs"
                  placeholder="Search order or resource"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </label>
              <select
                aria-label="Run status filter"
                value={filter}
                onChange={(e) => setFilter(e.target.value)}
              >
                <option>Active</option>
                <option>All</option>
                <option>Scheduled</option>
                <option>Running</option>
                <option>Paused</option>
                <option>Completed</option>
              </select>
              {rows(runs)
                .filter((r) =>
                  filter === "All" || filter === "Active"
                    ? filter === "All" || r.status !== "Completed"
                    : r.status === filter,
                )
                .filter((r) =>
                  match({
                    ...r,
                    order: order(r.productionOrderId)?.number,
                    resource: rows(resources).find(
                      (x) => x.id === r.productionResourceId,
                    )?.name,
                  }),
                )
                .map((r) => (
                  <button
                    key={r.id}
                    className={`mfg-list-item ${runId === r.id ? "selected" : ""}`}
                    onClick={() => setRunId(r.id)}
                  >
                    <div>
                      <strong>
                        {order(r.productionOrderId)?.number ||
                          r.productionOrderId.slice(0, 8)}
                      </strong>
                      <Badge>{r.status}</Badge>
                    </div>
                    <p>
                      {
                        rows(resources).find(
                          (x) => x.id === r.productionResourceId,
                        )?.name
                      }
                    </p>
                    <small>
                      {when(r.scheduledAt)} {r.priority ? " · Priority" : ""}
                    </small>
                  </button>
                ))}
              {!activeRuns.length && filter === "Active" && (
                <Empty title="Ready for the next run">
                  Release an order, publish its SOP and schedule execution.
                </Empty>
              )}
            </aside>
            <div className="mfg-detail">
              {run.isPending && runId ? (
                <p>Loading execution…</p>
              ) : run.error ? (
                <p role="alert">{run.error.message}</p>
              ) : !run.data ? (
                <Empty title="Your execution desk">
                  Select a production run to see its phases, controls and
                  history.
                </Empty>
              ) : (
                <>
                  <div className="mfg-detail-head">
                    <div>
                      <span className="eyebrow">
                        {run.data.resource.code} · {run.data.recipe.code} / REV{" "}
                        {run.data.recipe.revision}
                      </span>
                      <h2>{run.data.order.number}</h2>
                      <p>
                        {material(run.data.order.materialId)?.name} · Planned{" "}
                        {num(run.data.order.plannedQuantity)}{" "}
                        {material(run.data.order.materialId)?.uom}
                      </p>
                    </div>
                    <Badge>{run.data.run.status}</Badge>
                  </div>
                  <div className="mfg-row mfg-controls">
                    {run.data.run.status === "Scheduled" && (
                      <button
                        className="primary"
                        disabled={!writable}
                        onClick={() =>
                          setModal({ kind: "start", row: run.data.run })
                        }
                      >
                        <Play size={16} />
                        Start production
                      </button>
                    )}
                    {run.data.run.status === "Scheduled" && (
                      <Button
                        disabled={!manager}
                        onClick={() =>
                          setModal({ kind: "reschedule", row: run.data.run })
                        }
                      >
                        Reschedule execution
                      </Button>
                    )}
                    {["Running", "Paused"].includes(run.data.run.status) && (
                      <Button
                        disabled={!writable}
                        onClick={() =>
                          setModal({
                            kind:
                              run.data.run.status === "Running"
                                ? "pause"
                                : "resume",
                            row: run.data.run,
                          })
                        }
                      >
                        {run.data.run.status === "Running" ? (
                          <Pause size={16} />
                        ) : (
                          <Play size={16} />
                        )}{" "}
                        {run.data.run.status === "Running"
                          ? "Pause production"
                          : "Resume production"}
                      </Button>
                    )}
                    <Button onClick={() => setModal({ kind: "events" })}>
                      <History size={16} />
                      Execution history
                    </Button>
                  </div>
                  {run.data.cases.length > 0 && (
                    <div className="mfg-warning">
                      {run.data.cases.length} open quality deviation(s). Resolve
                      these in Quality before completing a phase or final
                      output.
                    </div>
                  )}
                  <div className="mfg-progress">
                    <span
                      style={{
                        width: `${(100 * run.data.phases.filter((p: Row) => p.status === "Completed").length) / run.data.phases.length}%`,
                      }}
                    />
                  </div>
                  <p className="mfg-note">
                    {
                      run.data.phases.filter(
                        (p: Row) => p.status === "Completed",
                      ).length
                    }{" "}
                    of {run.data.phases.length} phases complete · Recorded
                    paused time: {num(run.data.run.pausedMinutes)} min
                  </p>
                  <div className="mfg-phases">
                    {run.data.phases.map((p: Row) => (
                      <article
                        key={p.id}
                        className={`mfg-phase ${p.status.toLowerCase()}`}
                      >
                        <span className="mfg-phase-number">
                          {p.status === "Completed" ? (
                            <Check size={20} />
                          ) : (
                            String(p.sequence).padStart(2, "0")
                          )}
                        </span>
                        <div className="mfg-phase-body">
                          <div className="mfg-row">
                            <h3>{p.name}</h3>
                            <Badge>{p.status}</Badge>
                          </div>
                          <p>
                            {p.instructions ||
                              "Follow the approved production instructions."}
                          </p>
                          <div className="mfg-phase-tags">
                            <span>
                              <ClipboardCheck size={14} />
                              {read(p.checksJson).length} checks
                            </span>
                            <span>
                              <SlidersHorizontal size={14} />
                              {read(p.parametersJson).length} parameters
                            </span>
                            {p.requiresSample && (
                              <span>
                                <FlaskConical size={14} />
                                Laboratory approval
                              </span>
                            )}
                          </div>
                          <small>
                            Started {when(p.startedAt)} · Completed{" "}
                            {when(p.completedAt)}
                          </small>
                          <div className="mfg-row">
                            {p.status === "Pending" && (
                              <Action
                                title={`Start phase ${p.sequence}`}
                                path={M + `phases/${p.id}/start`}
                                plantId={plantId}
                                build={() => ({})}
                                disabled={
                                  !writable || run.data.run.status !== "Running"
                                }
                              />
                            )}{" "}
                            {p.status === "Running" && (
                              <>
                                <Button
                                  disabled={
                                    !writable ||
                                    run.data.run.status !== "Running"
                                  }
                                  onClick={() =>
                                    setModal({ kind: "evidence", row: p })
                                  }
                                >
                                  Record phase evidence
                                </Button>
                                {run.data.run.qualitySpecificationId && (
                                  <Button
                                    disabled={
                                      !writable ||
                                      run.data.run.status !== "Running"
                                    }
                                    onClick={() =>
                                      setModal({ kind: "phase-sample", row: p })
                                    }
                                  >
                                    <FlaskConical size={16} />
                                    Collect phase sample
                                  </Button>
                                )}
                                <Action
                                  title={`Complete phase ${p.sequence}`}
                                  path={M + `phases/${p.id}/complete`}
                                  plantId={plantId}
                                  build={() => ({})}
                                  disabled={
                                    !writable ||
                                    run.data.run.status !== "Running"
                                  }
                                />
                              </>
                            )}
                          </div>
                          {run.data.samples
                            .filter((s: Row) => s.productionPhaseId === p.id)
                            .slice(0, 3)
                            .map((s: Row) => (
                              <p className="mfg-note" key={s.id}>
                                {s.number} · <Badge>{s.status}</Badge>
                              </p>
                            ))}
                        </div>
                      </article>
                    ))}
                  </div>
                  {run.data.run.status === "Running" &&
                    run.data.phases.every(
                      (p: Row) => p.status === "Completed",
                    ) && (
                      <div className="mfg-next">
                        <CheckCircle2 />
                        <div>
                          <h3>SOP phases complete</h3>
                          <p>
                            Report actual good output and scrap in Orders &
                            output. The whole order must reconcile to planned
                            output.
                          </p>
                        </div>
                        <Button onClick={() => setTab("Orders & output")}>
                          Report output <ArrowRight size={16} />
                        </Button>
                      </div>
                    )}
                </>
              )}
            </div>
          </div>
        </>
      )}
      {tab === "SOP library" && (
        <>
          <div className="mfg-toolbar">
            <div>
              <h2>Versioned SOP library</h2>
              <p>
                Published revisions retain their instructions, checks and
                limits.
              </p>
            </div>
            <button
              className="primary"
              disabled={!manager}
              onClick={() => setModal({ kind: "recipe" })}
            >
              <Plus size={17} />
              Create SOP revision
            </button>
          </div>
          <div className="mfg-card-grid">
            {rows(recipes).map((r) => (
              <article className="mfg-tile" key={r.id}>
                <div className="mfg-row">
                  <Badge>{r.status}</Badge>
                  <span>REV {r.revision}</span>
                </div>
                <h3>{r.name}</h3>
                <p>
                  {r.code} · {material(r.materialId)?.name}
                </p>
                <p>
                  {read(r.phasesJson).length} phases ·{" "}
                  {r.precautions || "No additional precautions recorded"}
                </p>
                <Button
                  onClick={() => setModal({ kind: "recipe-view", row: r })}
                >
                  View SOP
                </Button>
                {r.status === "Draft" && (
                  <Action
                    title="Publish SOP"
                    path={M + `recipes/${r.id}/publish`}
                    plantId={plantId}
                    build={() => ({})}
                    disabled={!manager}
                  />
                )}
              </article>
            ))}
          </div>
          {!rows(recipes).length && (
            <Empty title="Make the process repeatable">
              Create an SOP revision with ordered phases, checklists, process
              limits and laboratory gates.
            </Empty>
          )}
        </>
      )}
      {tab === "Resources" && (
        <>
          <div className="mfg-toolbar">
            <div>
              <h2>Production resources</h2>
              <p>Running and paused orders keep their resource occupied.</p>
            </div>
            <button
              className="primary"
              disabled={!manager}
              onClick={() => setModal({ kind: "resource" })}
            >
              <Plus size={17} />
              Add resource
            </button>
          </div>
          <div className="mfg-card-grid">
            {rows(resources).map((r) => (
              <article className="mfg-tile" key={r.id}>
                <Activity />
                <h3>{r.name}</h3>
                <p>{r.code}</p>
                <Badge>
                  {r.maintenance
                    ? "Maintenance"
                    : activeRuns.some(
                          (x) =>
                            x.productionResourceId === r.id &&
                            ["Running", "Paused"].includes(x.status),
                        )
                      ? "Occupied"
                      : "Available"}
                </Badge>
                <Button
                  disabled={!manager}
                  onClick={() => setModal({ kind: "resource", row: r })}
                >
                  Manage resource
                </Button>
              </article>
            ))}
          </div>
          {!rows(resources).length && (
            <Empty title="Set up the shop floor">
              Register the resources where production orders will run.
            </Empty>
          )}
        </>
      )}
      {tab === "Laboratory" && (
        <>
          <div className="mfg-toolbar">
            <div>
              <h2>Laboratory work queue</h2>
              <p>Collect → hand over → receive → test → review.</p>
            </div>
            <button
              className="primary"
              disabled={!writable}
              onClick={() => setModal({ kind: "batch-sample" })}
            >
              <Plus size={17} />
              Collect batch sample
            </button>
          </div>
          <div className="mfg-split">
            <aside className="mfg-list">
              <label className="mfg-search">
                <Search size={17} />
                <input
                  aria-label="Search samples"
                  placeholder="Search sample or material"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </label>
              {rows(samples)
                .filter((s) =>
                  match({ ...s, material: material(s.materialId)?.name }),
                )
                .map((s) => (
                  <button
                    className={`mfg-list-item ${sampleId === s.id ? "selected" : ""}`}
                    key={s.id}
                    onClick={() => setSampleId(s.id)}
                  >
                    <div>
                      <strong>{s.number}</strong>
                      <Badge>{s.status}</Badge>
                    </div>
                    <p>{material(s.materialId)?.name}</p>
                    <small>
                      {s.productionPhaseId
                        ? "In-process sample"
                        : "Batch sample"}{" "}
                      · {when(s.createdAt)}
                    </small>
                  </button>
                ))}
              {!rows(samples).length && (
                <Empty title="A clear view of every sample">
                  Collect a batch sample here, or create an in-process sample
                  from Production.
                </Empty>
              )}
            </aside>
            <div className="mfg-detail">
              {sample.error ? (
                <p role="alert">{sample.error.message}</p>
              ) : !sample.data ? (
                <Empty title="Your laboratory desk">
                  Select a sample to see its test limits, results and review
                  history.
                </Empty>
              ) : (
                <>
                  <div className="mfg-detail-head">
                    <div>
                      <span className="eyebrow">
                        {sample.data.material.code} ·{" "}
                        {sample.data.sample.productionPhaseId
                          ? "IN PROCESS"
                          : "BATCH QUALITY"}
                      </span>
                      <h2>{sample.data.sample.number}</h2>
                      <p>{sample.data.material.name}</p>
                    </div>
                    <Badge>{sample.data.sample.status}</Badge>
                  </div>
                  <div className="mfg-milestones">
                    {[
                      ["Collected", sample.data.sample.createdAt],
                      ["Handed over", sample.data.sample.handedOverAt],
                      ["Received", sample.data.sample.receivedAt],
                      ["Testing", sample.data.sample.testingAt],
                      ["Reviewed", sample.data.sample.reviewedAt],
                    ].map(([t, d]) => (
                      <div key={t}>
                        <span className={d ? "done" : ""} />
                        <b>{t}</b>
                        <small>{when(d)}</small>
                      </div>
                    ))}
                  </div>
                  <p className="mfg-note">{sample.data.sample.notes}</p>
                  <div className="mfg-row mfg-controls">
                    {(
                      {
                        Collected: ["Handover", "Hand over sample"],
                        HandedOver: ["Receive", "Receive at laboratory"],
                        Received: ["StartTesting", "Start laboratory testing"],
                      } as Record<string, string[]>
                    )[sample.data.sample.status] && (
                      <Action
                        title={
                          (
                            {
                              Collected: "Hand over sample",
                              HandedOver: "Receive at laboratory",
                              Received: "Start laboratory testing",
                            } as Record<string, string>
                          )[sample.data.sample.status]
                        }
                        path={M + `samples/${sampleId}/state`}
                        plantId={plantId}
                        build={() => ({
                          action: (
                            {
                              Collected: "Handover",
                              HandedOver: "Receive",
                              Received: "StartTesting",
                            } as Record<string, string>
                          )[sample.data.sample.status],
                        })}
                        disabled={!writable}
                      />
                    )}{" "}
                    {sample.data.sample.status === "Testing" && (
                      <>
                        <Button
                          disabled={!writable}
                          onClick={() =>
                            setModal({
                              kind: "reading",
                              row: sample.data.sample,
                            })
                          }
                        >
                          Record test result
                        </Button>
                        <Button
                          disabled={!manager}
                          onClick={() =>
                            setModal({
                              kind: "review",
                              row: sample.data.sample,
                            })
                          }
                        >
                          <ShieldCheck size={16} />
                          Review sample
                        </Button>
                      </>
                    )}
                    {sample.data.sample.status === "Failed" && (
                      <Button
                        disabled={!writable}
                        onClick={() =>
                          setModal({ kind: "retest", row: sample.data.sample })
                        }
                      >
                        Collect retest sample
                      </Button>
                    )}
                  </div>
                  <h3>Required tests and latest results</h3>
                  <div className="mfg-test-list">
                    {read(sample.data.sample.testsJson).map((t: any) => {
                      const result = sample.data.readings.find(
                        (r: Row) => r.testCode === t.code,
                      );
                      return (
                        <article key={t.code}>
                          <div>
                            <b>{t.name}</b>
                            <p>
                              {t.code} ·{" "}
                              {t.kind === "Numeric"
                                ? `${num(t.minimum)} – ${num(t.maximum)} ${t.uom}`
                                : "Pass required"}
                            </p>
                          </div>
                          <div>
                            <strong>
                              {result
                                ? result.numericValue !== null
                                  ? num(result.numericValue)
                                  : result.passedValue
                                    ? "Pass"
                                    : "Fail"
                                : "—"}
                            </strong>
                            <Badge>
                              {!result
                                ? "Pending"
                                : result.inSpec
                                  ? "In spec"
                                  : "Out of spec"}
                            </Badge>
                          </div>
                        </article>
                      );
                    })}
                  </div>
                  {sample.data.sample.reviewedAt && (
                    <div className="mfg-next">
                      <ShieldCheck />
                      <div>
                        <h3>
                          {sample.data.sample.status} ·{" "}
                          {when(sample.data.sample.reviewedAt)}
                        </h3>
                        <p>{sample.data.sample.reviewNotes}</p>
                        <small>Reviewer {sample.data.sample.reviewedBy}</small>
                      </div>
                    </div>
                  )}
                  <details className="mfg-history">
                    <summary>
                      All result recordings ({sample.data.readings.length})
                    </summary>
                    {sample.data.readings.map((r: Row) => (
                      <article key={r.id}>
                        <b>
                          {r.testCode} ·{" "}
                          {r.numericValue !== null
                            ? num(r.numericValue)
                            : r.passedValue
                              ? "Pass"
                              : "Fail"}
                        </b>
                        <Badge>{r.inSpec ? "In spec" : "Out of spec"}</Badge>
                        <small>
                          {when(r.createdAt)} · {r.notes} · Actor {r.actorId}
                        </small>
                      </article>
                    ))}
                  </details>
                  {sample.data.readingsHasMore && (
                    <p>Additional readings are not displayed.</p>
                  )}
                  <p className="mfg-note">
                    A passed sample is laboratory evidence. Release the batch
                    separately in Batch decisions; open deviations continue to
                    block release.
                  </p>
                </>
              )}
            </div>
          </div>
        </>
      )}
      {tab === "Nonconformances" && (
        <>
          <div className="mfg-toolbar">
            <div>
              <h2>Nonconformance review</h2>
              <p>
                Investigate the cause, record a correction and close with a
                reviewed resolution.
              </p>
            </div>
            <button
              className="primary"
              disabled={!writable}
              onClick={() => setModal({ kind: "case" })}
            >
              <Plus size={17} />
              Report deviation
            </button>
          </div>
          <div className="mfg-card-grid">
            {rows(cases).map((c) => (
              <article className="mfg-tile" key={c.id}>
                <div className="mfg-row">
                  <Badge>{c.status}</Badge>
                  <span>{c.severity}</span>
                </div>
                <h3>{c.reason}</h3>
                <p>
                  {c.batchId
                    ? batches.find((b) => b.id === c.batchId)?.number ||
                      "Batch deviation"
                    : "Production deviation"}
                </p>
                <small>{when(c.createdAt)}</small>
                {c.status === "Open" ? (
                  <Button
                    disabled={!manager}
                    onClick={() => setModal({ kind: "close-case", row: c })}
                  >
                    Review and close
                  </Button>
                ) : (
                  <details>
                    <summary>Resolution</summary>
                    <p>
                      <b>Root cause:</b> {c.rootCause}
                    </p>
                    <p>
                      <b>Corrective action:</b> {c.correctiveAction}
                    </p>
                    <p>
                      <b>Resolution:</b> {c.resolution}
                    </p>
                    <small>
                      Reviewed {when(c.closedAt)} · {c.reviewedBy}
                    </small>
                  </details>
                )}
              </article>
            ))}
          </div>
          {!rows(cases).length && (
            <Empty title="No deviations recorded">
              Out-of-range process and laboratory results automatically open
              deviations. You can also report an issue here.
            </Empty>
          )}
        </>
      )}
      {tab === "Specifications" && (
        <>
          <div className="mfg-toolbar">
            <div>
              <h2>Quality specifications</h2>
              <p>Published limits are copied into each new sample.</p>
            </div>
            <button
              className="primary"
              disabled={!manager}
              onClick={() => setModal({ kind: "specification" })}
            >
              <Plus size={17} />
              Create specification revision
            </button>
          </div>
          <div className="mfg-card-grid">
            {rows(specs).map((s) => (
              <article className="mfg-tile" key={s.id}>
                <div className="mfg-row">
                  <Badge>{s.status}</Badge>
                  <span>REV {s.revision}</span>
                </div>
                <h3>{s.name}</h3>
                <p>
                  {material(s.materialId)?.code} ·{" "}
                  {material(s.materialId)?.name}
                </p>
                {read(s.testsJson).map((t: any) => (
                  <div className="mfg-spec-test" key={t.code}>
                    <b>{t.name}</b>
                    <span>
                      {t.kind === "Numeric"
                        ? `${num(t.minimum)} – ${num(t.maximum)} ${t.uom}`
                        : "Pass / fail"}
                    </span>
                  </div>
                ))}
                {s.status === "Draft" && (
                  <Action
                    title="Publish specification"
                    path={M + `specifications/${s.id}/publish`}
                    plantId={plantId}
                    build={() => ({})}
                    disabled={!manager}
                  />
                )}
              </article>
            ))}
          </div>
          {!rows(specs).length && (
            <Empty title="Define what good looks like">
              Add material-specific tests, limits and units, then publish the
              specification for use.
            </Empty>
          )}
        </>
      )}
      {tab === "Stock" && (
        <>
          <div className="mfg-toolbar">
            <div>
              <h2>Finished stock</h2>
              <p>
                Inspect each pallet’s batch, quality and location before packing
                or putaway.
              </p>
            </div>
            <label className="mfg-search">
              <Search size={17} />
              <input
                aria-label="Search finished stock"
                placeholder="Search batch, pallet or material"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </label>
          </div>
          <div className="mfg-stock-grid">
            {stock
              .filter(
                (s) =>
                  s.quantity > 0 &&
                  match({ ...s, material: material(s.materialId)?.name }),
              )
              .map((p) => (
                <article className="mfg-stock-card" key={p.id}>
                  <div className="mfg-row">
                    <span className="mfg-stock-icon">
                      <Boxes size={23} />
                    </span>
                    <Badge>{p.qualityStatus}</Badge>
                  </div>
                  <h3>{material(p.materialId)?.name || p.materialId}</h3>
                  <p>{p.batchNumber}</p>
                  <div className="mfg-stock-qty">
                    {num(p.quantity)}{" "}
                    <small>{material(p.materialId)?.uom}</small>
                  </div>
                  <dl>
                    <div>
                      <dt>Location</dt>
                      <dd>{p.location}</dd>
                    </div>
                    <div>
                      <dt>Reserved</dt>
                      <dd>{num(p.reservedQuantity)}</dd>
                    </div>
                    <div>
                      <dt>Expiry</dt>
                      <dd>
                        {p.expiryDate
                          ? new Date(p.expiryDate).toLocaleDateString()
                          : "Not specified"}
                      </dd>
                    </div>
                  </dl>
                  <code>{p.code}</code>
                  <div className="mfg-row">
                    <Button
                      disabled={!writable || p.qualityStatus !== "Released"}
                      onClick={() => setModal({ kind: "pack", row: p })}
                    >
                      Pack pallet
                    </Button>
                    <Button
                      disabled={!writable || p.qualityStatus !== "Released"}
                      onClick={() => setModal({ kind: "putaway", row: p })}
                    >
                      <ArrowRight size={16} />
                      Put away
                    </Button>
                  </div>
                </article>
              ))}
          </div>
          {!stock.some((s) => s.quantity > 0) && (
            <Empty title="Finished stock starts in Production">
              Complete a production order, then test and release its batch
              through Quality.
            </Empty>
          )}
        </>
      )}
      {tab === "Packing history" && (
        <>
          <div className="mfg-toolbar">
            <div>
              <h2>Packed pallet records</h2>
              <p>
                Quantities and packaging references are retained with each
                packed pallet.
              </p>
            </div>
          </div>
          <div className="mfg-card-grid">
            {rows(packs).map((p) => (
              <article className="mfg-tile" key={p.id}>
                <PackageCheck />
                <h3>{num(p.quantity)} packed</h3>
                <p>
                  {p.packCount} × {num(p.packSize)} + {num(p.looseQuantity)}{" "}
                  loose
                </p>
                <p>{p.packagingReference}</p>
                <small>{when(p.createdAt)}</small>
                <Button
                  onClick={async () => {
                    setLabel(null);
                    setLabelError("");
                    setModal({ kind: "label" });
                    try {
                      setLabel(
                        await operationApi(
                          M + `packs/${p.id}/label?plantId=${plantId}`,
                        ),
                      );
                    } catch (e) {
                      setLabelError((e as Error).message);
                    }
                  }}
                >
                  <Printer size={16} />
                  Preview packed label
                </Button>
              </article>
            ))}
          </div>
          {!rows(packs).length && (
            <Empty title="Pack with a record">
              Select a released finished pallet from Stock to create a packed
              pallet and label preview.
            </Empty>
          )}
        </>
      )}
      {modal && (
        <Panel
          title={
            (
              {
                reschedule: "Reschedule execution",
                schedule: "Schedule production execution",
                start: "Start production",
                pause: "Pause production",
                resume: "Resume production",
                resource: modal.row
                  ? "Manage production resource"
                  : "Add production resource",
                recipe: "Create SOP revision",
                specification: "Create specification revision",
                evidence: "Record phase evidence",
                "phase-sample": "Collect phase sample",
                "batch-sample": "Collect batch sample",
                retest: "Collect retest sample",
                reading: "Record laboratory test result",
                review: "Review laboratory sample",
                case: "Report quality deviation",
                "close-case": "Review nonconformance",
                pack: "Pack finished pallet",
                putaway: "Scan finished putaway",
                events: "Execution history",
                label: "Packed pallet label",
                "recipe-view": "Published SOP contents",
              } as Record<string, string>
            )[modal.kind] || "Workflow"
          }
          wide={["recipe", "specification", "events", "recipe-view"].includes(
            modal.kind,
          )}
          onClose={close}
        >
          {modal.kind === "resource" && (
            <Action
              title="Save production resource"
              path={M + "resources"}
              plantId={plantId}
              onDone={close}
              build={(f) => ({
                code: str(f, "code"),
                name: str(f, "name"),
                maintenance: f.has("maintenance"),
                version: modal.row?.version || 0,
              })}
            >
              <Input
                label="Resource code"
                name="code"
                value={modal.row?.code}
              />
              <Input
                label="Resource name"
                name="name"
                value={modal.row?.name}
              />
              <CheckBox
                label="Under maintenance"
                name="maintenance"
                checked={modal.row?.maintenance}
              />
            </Action>
          )}
          {modal.kind === "recipe" && (
            <RecipeEditor
              plantId={plantId}
              materials={materials}
              onDone={close}
            />
          )}
          {modal.kind === "specification" && (
            <SpecificationEditor
              plantId={plantId}
              materials={materials}
              onDone={close}
            />
          )}
          {modal.kind === "recipe-view" && (
            <div className="mfg-modal-content">
              <p>{modal.row?.precautions}</p>
              {read(modal.row?.phasesJson || "[]").map((p: any, i: number) => (
                <article className="mfg-editor-row" key={i}>
                  <h3>
                    {i + 1}. {p.name}
                  </h3>
                  <p>{p.instructions}</p>
                  <ul>
                    {p.checks.map((x: string) => (
                      <li key={x}>{x}</li>
                    ))}
                  </ul>
                  {p.parameters.map((x: any) => (
                    <p key={x.code}>
                      {x.name}: {num(x.minimum)} – {num(x.maximum)} {x.uom}
                    </p>
                  ))}
                  {p.requiresSample && (
                    <Badge>Laboratory approval required</Badge>
                  )}
                </article>
              ))}
            </div>
          )}
          {modal.kind === "reschedule" && (
            <Action
              title="Save execution schedule"
              path={M + `runs/${modal.row!.id}/reschedule`}
              plantId={plantId}
              onDone={close}
              build={(f) => ({
                productionResourceId: str(f, "resource"),
                scheduledAt: new Date(str(f, "scheduled")).toISOString(),
                priority: f.has("priority"),
                reason: str(f, "reason"),
                version: modal.row!.version,
              })}
            >
              <Select
                label="New execution resource"
                name="resource"
                rows={rows(resources)}
              />
              <Input
                label="Revised production start"
                name="scheduled"
                type="datetime-local"
                value={localDate()}
              />
              <CheckBox
                label="Priority order"
                name="priority"
                checked={modal.row!.priority}
              />
              <Notes label="Reschedule reason" name="reason" required />
            </Action>
          )}
          {modal.kind === "schedule" && (
            <Action
              title="Schedule run"
              path={M + "runs"}
              plantId={plantId}
              onDone={(id) => {
                setRunId(id);
                close();
              }}
              build={(f) => ({
                productionOrderId: str(f, "order"),
                productionRecipeId: str(f, "recipe"),
                productionResourceId: str(f, "resource"),
                scheduledAt: new Date(str(f, "scheduled")).toISOString(),
                priority: f.has("priority"),
              })}
            >
              <p className="mfg-note">
                Start requires issued materials, any engineering approval, and a
                free resource. Scheduling does not reserve a calendar time slot.
              </p>
              <Select
                label="Released order for execution"
                name="order"
                rows={orders.filter(
                  (o) =>
                    o.status === "Released" &&
                    !rows(runs).some((r) => r.productionOrderId === o.id),
                )}
              />
              <Select
                label="Published SOP revision"
                name="recipe"
                rows={rows(recipes)
                  .filter((r) => r.status === "Published")
                  .map((r) => ({
                    ...r,
                    label: `${r.code} · ${r.name} · Rev ${r.revision}`,
                  }))}
              />
              <Select
                label="Execution resource"
                name="resource"
                rows={rows(resources)}
              />
              <Input
                label="Scheduled production start"
                name="scheduled"
                type="datetime-local"
                value={localDate()}
              />
              <CheckBox label="Priority order" name="priority" />
            </Action>
          )}
          {["start", "pause", "resume"].includes(modal.kind) && (
            <Action
              title={
                modal.kind === "start"
                  ? "Confirm and start"
                  : modal.kind === "pause"
                    ? "Confirm pause"
                    : "Confirm resume"
              }
              path={M + `runs/${modal.row!.id}/state`}
              plantId={plantId}
              onDone={close}
              build={(f) => ({
                action:
                  modal.kind === "start"
                    ? "Start"
                    : modal.kind === "pause"
                      ? "Pause"
                      : "Resume",
                safetyConfirmed: f.has("safety"),
                notes: str(f, "notes"),
              })}
            >
              {modal.kind === "start" && (
                <>
                  <div className="mfg-warning">
                    {run.data?.recipe.precautions ||
                      "Review the SOP and required safety precautions before starting."}
                  </div>
                  <CheckBox
                    label="I confirm the production notes, safety precautions and PPE requirements"
                    name="safety"
                  />
                </>
              )}
              <Notes
                label={
                  modal.kind === "pause" ? "Downtime reason" : "Execution notes"
                }
                required={modal.kind === "pause"}
              />
            </Action>
          )}
          {modal.kind === "evidence" && (
            <Action
              title="Save phase evidence"
              path={M + `phases/${modal.row!.id}/evidence`}
              plantId={plantId}
              onDone={close}
              build={(f) => ({
                checks: read(modal.row!.checksJson).filter(
                  (_: string, i: number) => f.has(`check-${i}`),
                ),
                parameters: read(modal.row!.parametersJson)
                  .filter(
                    (_: any, i: number) => str(f, `parameter-${i}`) !== "",
                  )
                  .map((p: any) => ({
                    code: p.code,
                    value: Number(
                      f.get(
                        `parameter-${read(modal.row!.parametersJson).findIndex((x: any) => x.code === p.code)}`,
                      ),
                    ),
                  })),
                notes: str(f, "notes"),
                version: modal.row!.version,
              })}
            >
              <p className="mfg-note">{modal.row!.instructions}</p>
              {read(modal.row!.checksJson).map((c: string, i: number) => (
                <CheckBox
                  key={c}
                  label={c}
                  name={`check-${i}`}
                  checked={read(modal.row!.evidenceJson, {}).checks?.includes(
                    c,
                  )}
                />
              ))}
              {read(modal.row!.parametersJson).map((p: any, i: number) => (
                <div key={p.code}>
                  <Input
                    label={`${p.name} (${p.uom})`}
                    name={`parameter-${i}`}
                    type="number"
                    required={false}
                    value={
                      read(modal.row!.evidenceJson, {}).parameters?.find(
                        (x: any) => x.code === p.code,
                      )?.value
                    }
                  />
                  <small>
                    Allowed {num(p.minimum)} – {num(p.maximum)} {p.uom}.
                    Out-of-range readings are retained and open a deviation.
                  </small>
                </div>
              ))}
              <Notes value={read(modal.row!.evidenceJson, {}).notes || ""} />
            </Action>
          )}
          {["phase-sample", "batch-sample", "retest"].includes(modal.kind) && (
            <Action
              title="Collect sample"
              path={M + "samples"}
              plantId={plantId}
              onDone={(id) => {
                setSampleId(id);
                close();
              }}
              build={(f) => ({
                qualitySpecificationId:
                  modal.kind === "phase-sample"
                    ? run.data.run.qualitySpecificationId
                    : modal.kind === "retest" && modal.row!.productionPhaseId
                      ? modal.row!.qualitySpecificationId
                      : str(f, "spec"),
                batchId:
                  modal.kind === "batch-sample"
                    ? str(f, "batch")
                    : modal.kind === "retest"
                      ? modal.row!.batchId
                      : null,
                productionPhaseId:
                  modal.kind === "phase-sample"
                    ? modal.row!.id
                    : modal.kind === "retest"
                      ? modal.row!.productionPhaseId
                      : null,
                retestOfId: modal.kind === "retest" ? modal.row!.id : null,
                notes: str(f, "notes"),
              })}
            >
              <p className="mfg-note">
                Batch samples quarantine on-hand stock. Laboratory review and
                batch disposition are separate controlled decisions.
              </p>
              {modal.kind === "batch-sample" && (
                <Select
                  label="Batch to sample"
                  name="batch"
                  rows={batches.filter((b) => b.qualityStatus !== "Rejected")}
                />
              )}{" "}
              {(modal.kind === "batch-sample" ||
                (modal.kind === "retest" && modal.row!.batchId)) && (
                <Select
                  label="Published quality specification"
                  name="spec"
                  rows={rows(specs)
                    .filter((s) => s.status === "Published")
                    .map((s) => ({
                      ...s,
                      label: `${s.name} · ${material(s.materialId)?.code} · Rev ${s.revision}`,
                    }))}
                />
              )}
              <Notes
                label={
                  modal.kind === "retest" ? "Retest reason" : "Collection notes"
                }
                required={modal.kind === "retest"}
              />
            </Action>
          )}
          {modal.kind === "reading" && (
            <ReadingForm
              plantId={plantId}
              sample={sample.data.sample}
              onDone={close}
            />
          )}
          {modal.kind === "review" && (
            <Action
              title="Save laboratory review"
              path={M + `samples/${modal.row!.id}/review`}
              plantId={plantId}
              onDone={close}
              build={(f) => ({
                decision: str(f, "decision"),
                notes: str(f, "notes"),
              })}
            >
              <label>
                Laboratory decision
                <select
                  name="decision"
                  aria-label="Laboratory decision"
                  required
                >
                  <option value="">Select decision</option>
                  <option>Passed</option>
                  <option>Failed</option>
                </select>
              </label>
              <Notes label="Review justification" required />
            </Action>
          )}
          {modal.kind === "case" && (
            <CaseForm
              plantId={plantId}
              batches={batches}
              runs={rows(runs)
                .filter((r) => r.status !== "Completed")
                .map((r) => ({
                  ...r,
                  label: order(r.productionOrderId)?.number || r.id,
                }))}
              onDone={close}
            />
          )}
          {modal.kind === "close-case" && (
            <Action
              title="Close nonconformance"
              path={M + `quality-cases/${modal.row!.id}/close`}
              plantId={plantId}
              onDone={close}
              build={(f) => ({
                rootCause: str(f, "cause"),
                correctiveAction: str(f, "action"),
                resolution: str(f, "resolution"),
              })}
            >
              <div className="mfg-warning">{modal.row!.reason}</div>
              <Notes label="Root cause" name="cause" required />
              <Notes label="Corrective action" name="action" required />
              <Notes label="Review resolution" name="resolution" required />
              <p className="mfg-note">
                Closing records the investigation. It does not automatically
                pass a sample or release stock.
              </p>
            </Action>
          )}
          {modal.kind === "pack" && (
            <PackingForm
              plantId={plantId}
              pallet={modal.row!}
              uom={material(modal.row!.materialId)?.uom}
              onDone={close}
            />
          )}
          {modal.kind === "putaway" && (
            <Action
              title="Confirm finished putaway"
              path={M + "putaway"}
              plantId={plantId}
              onDone={close}
              build={(f) => ({
                palletId: modal.row!.id,
                palletCode: str(f, "pallet"),
                destinationBin: str(f, "bin"),
              })}
            >
              <div className="mfg-note">
                {modal.row!.code} · {num(modal.row!.quantity)}{" "}
                {material(modal.row!.materialId)?.uom}
                <br />
                Current location: {modal.row!.location}
              </div>
              <Input label="Scan finished pallet label" name="pallet" />
              <Input label="Scan destination bin" name="bin" />
              <p className="mfg-note">
                Use a keyboard barcode scanner or type the exact pallet and bin
                codes.
              </p>
            </Action>
          )}
          {modal.kind === "events" && (
            <div className="mfg-modal-content">
              <div className="mfg-row">
                <p>Recorded events for {run.data?.order.number}</p>
                <Button onClick={exportEvents}>
                  <Download size={16} />
                  Export shown events
                </Button>
              </div>
              {run.data?.events.map((e: Row) => (
                <article className="mfg-event" key={e.id}>
                  <span />
                  <div>
                    <h3>{e.kind}</h3>
                    <p>{eventNotes(e.notes)}</p>
                    <small>
                      {when(e.createdAt)} · Actor {e.actorId}
                    </small>
                  </div>
                </article>
              ))}
              {run.data?.historyHasMore && (
                <p>Only the latest 200 events are shown and exported.</p>
              )}
            </div>
          )}
          {modal.kind === "label" && (
            <div className="mfg-modal-content">
              <p className="mfg-note">
                Local label preview. Browser printing does not confirm a
                physical printer job.
              </p>
              {labelError && <p role="alert">{labelError}</p>}
              {!label && !labelError ? (
                <p>Loading label…</p>
              ) : (
                label && (
                  <>
                    <div className="mfg-label" id="finished-label">
                      <h3>GREEN INK · FINISHED PRODUCT</h3>
                      <h2>{label.materialName}</h2>
                      <p>{label.material}</p>
                      <div dangerouslySetInnerHTML={{ __html: label.qrSvg }} />
                      <strong>{label.palletCode}</strong>
                      <p>Batch {label.batchNumber}</p>
                      <p>
                        {num(label.pack.quantity)} {label.uom} ·{" "}
                        {label.pack.packCount} × {num(label.pack.packSize)} +{" "}
                        {num(label.pack.looseQuantity)} loose
                      </p>
                      <p>{label.pack.packagingReference}</p>
                      <p>Packed {when(label.pack.createdAt)}</p>
                    </div>
                    <Button onClick={() => window.print()}>
                      <Printer size={16} />
                      Print label preview
                    </Button>
                  </>
                )
              )}
            </div>
          )}
        </Panel>
      )}
    </section>
  );
}
function ReadingForm({
  plantId,
  sample,
  onDone,
}: {
  plantId: string;
  sample: Row;
  onDone: () => void;
}) {
  const tests = read(sample.testsJson),
    [code, setCode] = useState(tests[0]?.code || "");
  const test = tests.find((t: any) => t.code === code);
  return (
    <Action
      title="Save test result"
      path={`manufacturing/samples/${sample.id}/readings`}
      plantId={plantId}
      onDone={onDone}
      build={(f) => ({
        testCode: code,
        numericValue: test?.kind === "Numeric" ? Number(f.get("value")) : null,
        passedValue:
          test?.kind === "PassFail" ? f.get("pass") === "true" : null,
        notes: str(f, "notes"),
      })}
    >
      <label>
        Sample test
        <select required value={code} onChange={(e) => setCode(e.target.value)}>
          {tests.map((t: any) => (
            <option key={t.code} value={t.code}>
              {t.name} · {t.code}
            </option>
          ))}
        </select>
      </label>
      {test?.kind === "Numeric" ? (
        <>
          <Input label="Measured result" name="value" type="number" />
          <p className="mfg-note">
            Required range: {num(test.minimum)} – {num(test.maximum)} {test.uom}
          </p>
        </>
      ) : (
        <label>
          Pass or fail
          <select name="pass" aria-label="Pass or fail" required>
            <option value="">Select result</option>
            <option value="true">Pass</option>
            <option value="false">Fail</option>
          </select>
        </label>
      )}
      <Notes label="Result notes" />
    </Action>
  );
}
function CaseForm({
  plantId,
  batches,
  runs,
  onDone,
}: {
  plantId: string;
  batches: Row[];
  runs: Row[];
  onDone: () => void;
}) {
  const [target, setTarget] = useState("Batch");
  return (
    <Action
      title="Save deviation"
      path="manufacturing/quality-cases"
      plantId={plantId}
      onDone={onDone}
      build={(f) => ({
        batchId: target === "Batch" ? str(f, "target") : null,
        productionRunId: target === "Run" ? str(f, "target") : null,
        severity: str(f, "severity"),
        reason: str(f, "reason"),
      })}
    >
      <label>
        Deviation target
        <select value={target} onChange={(e) => setTarget(e.target.value)}>
          <option>Batch</option>
          <option value="Run">Production run</option>
        </select>
      </label>
      <Select
        label={target === "Batch" ? "Affected batch" : "Affected execution run"}
        name="target"
        rows={target === "Batch" ? batches : runs}
      />
      <label>
        Severity
        <select name="severity">
          <option>Minor</option>
          <option>Major</option>
          <option>Critical</option>
        </select>
      </label>
      <Notes label="Deviation description" name="reason" required />
      <p className="mfg-note">
        Batch deviations quarantine stock. Run deviations block phase completion
        and final output.
      </p>
    </Action>
  );
}
function PackingForm({
  plantId,
  pallet,
  uom,
  onDone,
}: {
  plantId: string;
  pallet: Row;
  uom: string;
  onDone: () => void;
}) {
  const [size, setSize] = useState(""),
    [count, setCount] = useState(""),
    [loose, setLoose] = useState("0");
  const total = Number(size) * Number(count) + Number(loose);
  return (
    <Action
      title="Create packed pallet"
      path="manufacturing/packs"
      plantId={plantId}
      onDone={onDone}
      build={(f) => ({
        sourcePalletId: pallet.id,
        palletCode: str(f, "scan"),
        packSize: Number(size),
        packCount: Number(count),
        looseQuantity: Number(loose),
        packagingReference: str(f, "reference"),
      })}
    >
      <div className="mfg-note">
        Source {pallet.code}
        <br />
        {num(pallet.quantity - pallet.reservedQuantity)} {uom} free ·{" "}
        {pallet.batchNumber}
      </div>
      <Input label="Scan packing source pallet" name="scan" />
      <div className="mfg-fields">
        {[
          ["Pack size", size, setSize],
          ["Whole pack count", count, setCount],
          ["Loose quantity", loose, setLoose],
        ].map(([title, v, set]: any) => (
          <label key={title}>
            {title}
            <input
              required
              type="number"
              step={title === "Whole pack count" ? "1" : "0.000001"}
              min="0"
              value={v}
              onChange={(e) => set(e.target.value)}
            />
          </label>
        ))}
      </div>
      <div className="mfg-calculation">
        <span>Packed quantity</span>
        <strong>
          {num(total)} <small>{uom}</small>
        </strong>
        <p>
          Source balance after packing: {num(pallet.quantity - total)} {uom}
        </p>
      </div>
      <Input label="Packaging reference" name="reference" />
      <p className="mfg-note">
        Creates one new pallet of the same batch. Source and packed quantities
        reconcile through paired stock movements.
      </p>
    </Action>
  );
}
