import React, { useState, type FormEvent } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter, NavLink, Route, Routes } from "react-router-dom";
import {
  QueryClient,
  QueryClientProvider,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  Activity,
  ArrowDownToLine,
  Boxes,
  Building2,
  ChartNoAxesCombined,
  Check,
  ChevronRight,
  Factory,
  FlaskConical,
  GitBranch,
  LayoutDashboard,
  LogOut,
  KeyRound,
  Menu,
  Moon,
  Package,
  Plug,
  Settings,
  ShieldCheck,
  Sun,
  Tag,
  Truck,
  Users,
  X,
} from "lucide-react";
import "./styles.css";
import { Operations, OperationalSummary } from "./Operations";
type User = {
  id: string;
  email: string;
  displayName: string;
  role: string;
  tenantId: string | null;
  plantIds: string[];
};
type Tenant = { id: string; name: string; code: string };
type Plant = Tenant & { tenantId: string };
type Dashboard = {
  tenantCount: number;
  plantCount: number;
  userCount: number;
  plantName: string | null;
};
async function api<T>(
  path: string,
  data?: unknown,
  method = "POST",
): Promise<T> {
  const response = await fetch(`/api${path}`, {
    credentials: "include",
    headers: data
      ? { "Content-Type": "application/json", "X-GreenInk-Request": "1" }
      : undefined,
    method: data ? method : "GET",
    body: data ? JSON.stringify(data) : undefined,
  });
  if (!response.ok) {
    let message = `Request failed (${response.status}).`;
    try {
      const body = await response.json();
      message = body.detail || body.title || body.message || message;
      if (body.errors) message = Object.values(body.errors).flat().join(" ");
    } catch {}
    throw new Error(message);
  }
  if (response.status === 204) return undefined as T;
  return response.json();
}
const modules = [
  ["", "Command Center", LayoutDashboard],
  ["inbound", "Inbound", ArrowDownToLine],
  ["inventory", "Inventory", Boxes],
  ["preparation", "Material Preparation", Package],
  ["production", "Production", Factory],
  ["quality", "Quality", FlaskConical],
  ["finished-goods", "Finished Goods", Package],
  ["dispatch", "Dispatch", Truck],
  ["traceability", "Traceability", GitBranch],
  ["labels", "Label Studio", Tag],
  ["integrations", "Integration Hub", Plug],
  ["reports", "Reports & Analytics", ChartNoAxesCombined],
] as const;
function Login({ onLogin }: { onLogin: () => void }) {
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setBusy(true);
    setError("");
    try {
      await api("/auth/login", {
        email: form.get("email"),
        password: form.get("password"),
      });
      onLogin();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <main className="login">
      <section className="login-story">
        <Logo />
        <div>
          <span className="eyebrow">YOUR FACTORY. CONNECTED.</span>
          <h1>
            A stronger foundation
            <br />
            for every operation.
          </h1>
          <p>
            One workspace for your people, plants, and the next generation of
            manufacturing.
          </p>
        </div>
        <small>GREEN INK TECHNOLOGIES LLP</small>
      </section>
      <section className="login-panel">
        <div className="login-form">
          <ShieldCheck size={36} className="green" />
          <h2>Welcome to SmartFactory</h2>
          <p>Sign in with your organization account.</p>
          <form onSubmit={submit}>
            <label>
              Email address
              <input
                name="email"
                type="email"
                autoComplete="username"
                required
                autoFocus
                placeholder="you@company.com"
              />
            </label>
            <label>
              Password
              <input
                name="password"
                type="password"
                autoComplete="current-password"
                required
              />
            </label>
            {error && (
              <p className="error" role="alert">
                {error}
              </p>
            )}
            <button className="primary" disabled={busy}>
              {busy ? "Signing in…" : "Sign in"}
              <ChevronRight size={16} />
            </button>
          </form>
          <p className="muted">
            Need access? Contact your organization administrator.
          </p>
        </div>
      </section>
    </main>
  );
}
function Logo() {
  return (
    <div className="logo">
      <div className="logo-brand">
        <img
          className="logo-wordmark"
          src="/greenink-wordmark.png"
          alt="Green Ink — Your thought. Our ink"
          width={2171}
          height={724}
        />
      </div>
      <span className="logo-product">SmartFactory</span>
    </div>
  );
}
function App() {
  const client = useQueryClient();
  const session = useQuery({
    queryKey: ["session"],
    queryFn: () => api<User>("/auth/me"),
    retry: false,
  });
  const [dark, setDark] = useState(
    () => localStorage.getItem("greenink-theme") === "dark",
  );
  const [menu, setMenu] = useState(false);
  const [tenant, setTenant] = useState("");
  const [plant, setPlant] = useState("");
  const [passwordOpen, setPasswordOpen] = useState(false);
  const [logoutError, setLogoutError] = useState("");
  const tenants = useQuery({
    queryKey: ["tenants"],
    queryFn: () => api<Tenant[]>("/tenants"),
    enabled: !!session.data,
  });
  const plants = useQuery({
    queryKey: ["plants"],
    queryFn: () => api<Plant[]>("/plants"),
    enabled: !!session.data,
  });
  const user = session.data;
  const availablePlants = (plants.data || []).filter(
    (p) => !tenant || p.tenantId === tenant,
  );
  function theme() {
    setDark(!dark);
    localStorage.setItem("greenink-theme", !dark ? "dark" : "light");
  }
  async function logout() {
    try {
      await api("/auth/logout", {});
      setTenant("");
      setPlant("");
      client.clear();
      await session.refetch();
    } catch (e) {
      setLogoutError((e as Error).message);
    }
  }
  if (session.isPending)
    return (
      <div className="loading" role="status">
        Loading SmartFactory…
      </div>
    );
  if (!user)
    return (
      <Login
        onLogin={() => {
          client.clear();
          void session.refetch();
        }}
      />
    );
  return (
    <div className={dark ? "app dark" : "app"}>
      <a className="skip" href="#main">
        Skip to content
      </a>
      <aside className={menu ? "sidebar open" : "sidebar"}>
        <Logo />
        <button
          className="close icon-button"
          aria-label="Close navigation"
          onClick={() => setMenu(false)}
        >
          <X />
        </button>
        <div className="nav-caption">WORKSPACE</div>
        <nav aria-label="Main navigation">
          {modules.map(([path, name, Icon]) => (
            <NavLink
              key={path}
              to={`/${path}`}
              end
              onClick={() => setMenu(false)}
            >
              <Icon size={18} />
              <span>{name}</span>
              {path && <span className="nav-dot" aria-hidden="true" />}
            </NavLink>
          ))}
          {["PlatformAdmin", "TenantAdmin"].includes(user.role) && (
            <>
              <div className="nav-caption admin-caption">MANAGE</div>
              <NavLink to="/administration" onClick={() => setMenu(false)}>
                <Settings size={18} />
                Administration
              </NavLink>
            </>
          )}
        </nav>
        <div className="sidebar-footer">
          <span className="status-dot" />
          Platform foundation <small>Milestone 1</small>
        </div>
      </aside>
      <div className="workspace">
        <header className="topbar">
          <button
            className="mobile-menu icon-button"
            aria-label="Open navigation"
            onClick={() => setMenu(true)}
          >
            <Menu />
          </button>
          <div className="context">
            <Building2 size={17} />
            <label className="sr-only" htmlFor="tenant">
              Organization
            </label>
            <select
              id="tenant"
              value={tenant}
              onChange={(e) => {
                setTenant(e.target.value);
                setPlant("");
              }}
            >
              <option value="">All organizations</option>
              {tenants.data?.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.name}
                </option>
              ))}
            </select>
            <span className="divider" />
            <Factory size={17} />
            <label className="sr-only" htmlFor="plant">
              Plant
            </label>
            <select
              id="plant"
              value={plant}
              onChange={(e) => setPlant(e.target.value)}
            >
              <option value="">All accessible plants</option>
              {availablePlants.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name}
                </option>
              ))}
            </select>
          </div>
          <div className="account">
            <button
              className="icon-button"
              aria-label="Change password"
              onClick={() => setPasswordOpen(true)}
            >
              <KeyRound size={18} />
            </button>
            <button
              className="icon-button"
              onClick={theme}
              aria-label={dark ? "Use light theme" : "Use dark theme"}
            >
              {dark ? <Sun size={19} /> : <Moon size={19} />}
            </button>
            <div className="avatar">
              {user.displayName.slice(0, 2).toUpperCase()}
            </div>
            <div className="account-name">
              {user.displayName}
              <small>{user.role.replace(/([a-z])([A-Z])/g, "$1 $2")}</small>
            </div>
            <button
              className="icon-button"
              onClick={logout}
              aria-label="Sign out"
            >
              <LogOut size={18} />
            </button>
          </div>
        </header>
        {passwordOpen && (
          <PasswordEditor
            close={() => setPasswordOpen(false)}
            complete={() => {
              setPasswordOpen(false);
              setTenant("");
              setPlant("");
              client.clear();
              void session.refetch();
            }}
          />
        )}
        {logoutError && (
          <p className="error" role="alert">
            {logoutError}
          </p>
        )}
        <main id="main" tabIndex={-1}>
          <Routes>
            <Route
              path="/"
              element={
                <DashboardView
                  plant={plant}
                  tenant={tenant}
                  plants={availablePlants}
                  user={user}
                />
              }
            />
            <Route
              path="/administration"
              element={
                <Administration
                  user={user}
                  tenants={tenants.data || []}
                  plants={plants.data || []}
                />
              }
            />
            {modules
              .filter((m) => m[0])
              .map(([path, name]) => (
                <Route
                  key={path}
                  path={`/${path}`}
                  element={
                    <Operations
                      module={path}
                      name={name}
                      plant={plant}
                      plants={availablePlants}
                      role={user.role}
                    />
                  }
                />
              ))}
            <Route
              path="*"
              element={
                <div className="empty">
                  <h1>Page not found</h1>
                  <NavLink to="/">Return to Command Center</NavLink>
                </div>
              }
            />
          </Routes>
        </main>
        <footer>
          © {new Date().getFullYear()} GREEN INK TECHNOLOGIES LLP{" "}
          <span>SmartFactory · Platform foundation</span>
        </footer>
      </div>
    </div>
  );
}
function DashboardView({
  plant,
  tenant,
  plants,
  user,
}: {
  plant: string;
  tenant: string;
  plants: Plant[];
  user: User;
}) {
  const data = useQuery({
    queryKey: ["dashboard", tenant, plant],
    queryFn: () =>
      api<Dashboard>(
        `/dashboard?${new URLSearchParams({ ...(tenant ? { tenantId: tenant } : {}), ...(plant ? { plantId: plant } : {}) }).toString()}`,
      ),
  });
  return (
    <>
      <div className="page-heading">
        <div>
          <span className="eyebrow">WORKSPACE OVERVIEW</span>
          <h1>Command Center</h1>
          <p>Your manufacturing platform, connected from the ground up.</p>
        </div>
        <span className="badge">
          <span className="status-dot" />
          Foundation active
        </span>
      </div>
      {data.isError && (
        <p className="error" role="alert">
          {data.error.message}
        </p>
      )}
      <section className="welcome">
        <div>
          <span className="eyebrow">
            WELCOME, {user.displayName.split(" ")[0].toUpperCase()}
          </span>
          <h2>
            Good operations start
            <br />
            with a solid foundation.
          </h2>
          <p>
            Manage your organizations, configure your plants,
            <br className="desktop" /> and bring your team together in one
            place.
          </p>
          {["PlatformAdmin", "TenantAdmin"].includes(user.role) && (
            <NavLink className="primary" to="/administration">
              Manage your workspace
              <ChevronRight size={17} />
            </NavLink>
          )}
        </div>
        <div className="factory-art" aria-hidden="true">
          <Factory size={140} strokeWidth={0.8} />
          <span className="art-tag">
            <Check size={13} /> Connected workspace
          </span>
        </div>
      </section>
      <section className="stats" aria-label="Platform totals">
        {[
          [Building2, "Organizations", data.data?.tenantCount],
          [Factory, "Plants", data.data?.plantCount],
          [Users, "Team members", data.data?.userCount],
        ].map(([Icon, label, value]) => {
          const StatIcon = Icon as typeof Building2;
          return (
            <article className="stat card" key={String(label)}>
              <div>
                <span>{String(label)}</span>
                <strong>
                  {data.isPending
                    ? "…"
                    : value === undefined
                      ? "—"
                      : String(value)}
                </strong>
                <small>Within your authorized scope</small>
              </div>
              <span className="stat-icon">
                <StatIcon size={23} />
              </span>
            </article>
          );
        })}
      </section>
      <OperationalSummary plantId={plant} />
      <div className="dashboard-grid">
        <section className="card">
          <div className="card-heading">
            <div>
              <h2>Your plants</h2>
              <p>{tenant ? "Selected organization" : "Accessible locations"}</p>
            </div>
            <Factory size={19} />
          </div>
          {plants.length ? (
            <div className="plant-list">
              {plants.map((p) => (
                <div className="plant-row" key={p.id}>
                  <span className="plant-icon">
                    <Factory size={21} />
                  </span>
                  <div>
                    <strong>{p.name}</strong>
                    <small>{p.code}</small>
                  </div>
                  <span className="badge subtle">
                    {plant === p.id ? "Selected" : "Configured"}
                  </span>
                </div>
              ))}
            </div>
          ) : (
            <div className="empty compact">
              <Building2 size={30} />
              <h3>No plants configured</h3>
              <p>An administrator can add your first plant.</p>
            </div>
          )}
        </section>
        <section className="card">
          <div className="card-heading">
            <div>
              <h2>Platform readiness</h2>
              <p>Your foundation at a glance</p>
            </div>
            <ShieldCheck size={20} />
          </div>
          <div className="readiness">
            {[
              "Secure account access",
              "Role-based permissions",
              "Organization & plant isolation",
            ].map((s) => (
              <div key={s}>
                <span className="check-icon">
                  <Check size={14} />
                </span>
                {s}
                <span className="ready">Available</span>
              </div>
            ))}
            <div>
              <span className="pending-icon">
                <Activity size={14} />
              </span>
              Manufacturing core workflows
              <span className="ready">Available</span>
            </div>
          </div>
          <div className="info-note">
            Reports use real plant transactions. Advanced OEE and equipment
            telemetry remain on the roadmap.
          </div>
        </section>
      </div>
      <section className="roadmap">
        <div>
          <span className="eyebrow">FUNCTIONAL MANUFACTURING CORE</span>
          <h2>From receipt to production and dispatch.</h2>
          <p>
            Manage purchase orders, receipts, quality, inventory, production,
            and shipments. Advanced workflows remain under development.
          </p>
        </div>
        <span className="badge subtle">Core transactions available</span>
      </section>
    </>
  );
}
function Administration({
  user,
  tenants,
  plants,
}: {
  user: User;
  tenants: Tenant[];
  plants: Plant[];
}) {
  const [editing, setEditing] = useState<User | null>(null);
  const [tab, setTab] = useState("plants");
  const client = useQueryClient();
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [busy, setBusy] = useState(false);
  const users = useQuery({
    queryKey: ["users"],
    queryFn: () => api<User[]>("/users"),
    enabled: ["PlatformAdmin", "TenantAdmin"].includes(user.role),
  });
  const roles = useQuery({
    queryKey: ["roles"],
    queryFn: () => api<string[]>("/roles"),
    enabled: ["PlatformAdmin", "TenantAdmin"].includes(user.role),
  });
  const [formTenant, setFormTenant] = useState(user.tenantId || "");
  const admin = ["PlatformAdmin", "TenantAdmin"].includes(user.role);
  if (!admin)
    return (
      <div className="empty">
        <ShieldCheck />
        <h1>Administrator access required</h1>
      </div>
    );
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    const values = Object.fromEntries(form.entries());
    const payload =
      tab === "users"
        ? {
            ...values,
            tenantId: values.tenantId || null,
            plantIds: form.getAll("plantIds"),
          }
        : values;
    setError("");
    setSuccess("");
    setBusy(true);
    try {
      await api(`/${tab}`, payload);
      await Promise.all(
        ["tenants", "plants", "users", "dashboard"].map((key) =>
          client.invalidateQueries({ queryKey: [key] }),
        ),
      );
      formElement.reset();
      setFormTenant(user.tenantId || "");
      setSuccess(
        `${tab === "users" ? "User" : tab === "plants" ? "Plant" : "Organization"} created successfully.`,
      );
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <>
      <span className="eyebrow">WORKSPACE SETTINGS</span>
      <h1>Administration</h1>
      {editing && (
        <AccessEditor
          target={editing}
          roles={roles.data || []}
          plants={plants}
          close={() => setEditing(null)}
        />
      )}
      <p className="intro">
        Configure your organization and control access to your factory
        workspace.
      </p>
      <div className="tabs" role="tablist" aria-label="Administration sections">
        {[
          ...(user.role === "PlatformAdmin" ? ["tenants"] : []),
          "plants",
          "users",
          "roles",
          "audit",
        ].map((t) => (
          <button
            key={t}
            role="tab"
            aria-selected={tab === t}
            onClick={() => {
              setTab(t);
              setError("");
              setSuccess("");
            }}
            className={tab === t ? "selected" : ""}
          >
            {t === "tenants"
              ? "Organizations"
              : t[0].toUpperCase() + t.slice(1)}
          </button>
        ))}
      </div>
      {tab === "audit" ? (
        <AuditView />
      ) : tab === "roles" ? (
        <section className="card role-list">
          <h2>Built-in access roles</h2>
          <p>
            Permissions are enforced by the API. Custom role editing is planned.
          </p>
          {roles.isError ? (
            <p className="error">{roles.error.message}</p>
          ) : (
            (roles.data || []).map((role) => (
              <div className="plant-row" key={role}>
                <ShieldCheck size={20} />
                <strong>{role}</strong>
              </div>
            ))
          )}
        </section>
      ) : (
        <div className="admin-grid">
          <section className="card">
            <div className="card-heading">
              <h2>
                {tab === "tenants"
                  ? "Organizations"
                  : tab === "plants"
                    ? "Plants"
                    : "Users"}
              </h2>
            </div>
            {tab === "users" && users.isError && (
              <p className="error" role="alert">
                {users.error.message}
              </p>
            )}
            <div className="table-scroll">
              <table>
                <thead>
                  <tr>
                    <th>{tab === "users" ? "Name / email" : "Name"}</th>
                    <th>{tab === "users" ? "Role" : "Code"}</th>
                  </tr>
                </thead>
                <tbody>
                  {(tab === "tenants"
                    ? tenants
                    : tab === "plants"
                      ? plants
                      : users.data || []
                  ).map((row) => (
                    <tr key={row.id}>
                      <td>
                        <strong>
                          {"displayName" in row ? row.displayName : row.name}
                        </strong>
                        {"email" in row && <small>{row.email}</small>}
                      </td>
                      <td>
                        {"role" in row ? (
                          <>
                            {row.role}
                            {row.role !== "PlatformAdmin" && (
                              <button
                                className="edit-link"
                                onClick={() => setEditing(row)}
                              >
                                Edit access
                              </button>
                            )}
                          </>
                        ) : (
                          row.code
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {(tab === "tenants"
              ? tenants
              : tab === "plants"
                ? plants
                : users.data || []
            ).length === 0 && (
              <div className="empty compact">
                <p>No {tab === "tenants" ? "organizations" : tab} yet.</p>
              </div>
            )}
          </section>
          <section className="card create-card">
            <h2>
              Add{" "}
              {tab === "tenants"
                ? "organization"
                : tab === "plants"
                  ? "plant"
                  : "user"}
            </h2>
            <p>New records are saved to your workspace.</p>
            <form onSubmit={submit} key={tab}>
              {tab === "users" ? (
                <>
                  <label>
                    Full name
                    <input name="displayName" required maxLength={150} />
                  </label>
                  <label>
                    Email
                    <input name="email" type="email" required maxLength={254} />
                  </label>
                  <label>
                    Initial password
                    <input
                      aria-label="Initial password"
                      name="password"
                      type="password"
                      required
                      minLength={14}
                      maxLength={128}
                      autoComplete="new-password"
                    />
                    <small>
                      14–128 characters with uppercase, lowercase, and a digit.
                    </small>
                  </label>
                  <label>
                    Role
                    <select aria-label="Role" name="role" required>
                      {(roles.data || [])
                        .filter(
                          (r) =>
                            user.role === "PlatformAdmin" ||
                            r !== "PlatformAdmin",
                        )
                        .map((role) => (
                          <option key={role}>{role}</option>
                        ))}
                    </select>
                  </label>
                </>
              ) : (
                <>
                  <label>
                    Name
                    <input name="name" required maxLength={150} />
                  </label>
                  <label>
                    Code
                    <input
                      name="code"
                      required
                      maxLength={32}
                      pattern="[A-Za-z0-9_-]+"
                      title="Letters, numbers, hyphens and underscores"
                    />
                  </label>
                </>
              )}
              {tab !== "tenants" && (
                <label>
                  Organization
                  <select
                    aria-label="Organization"
                    name="tenantId"
                    required={tab === "plants"}
                    value={formTenant}
                    onChange={(e) => setFormTenant(e.target.value)}
                  >
                    <option value="">
                      {tab === "users"
                        ? "No organization (platform administrator only)"
                        : "Select organization"}
                    </option>
                    {tenants
                      .filter(
                        (t) =>
                          user.role === "PlatformAdmin" ||
                          t.id === user.tenantId,
                      )
                      .map((t) => (
                        <option key={t.id} value={t.id}>
                          {t.name}
                        </option>
                      ))}
                  </select>
                </label>
              )}
              {tab === "users" && (
                <fieldset>
                  <legend>Plant access</legend>
                  {plants
                    .filter((p) => p.tenantId === formTenant)
                    .map((p) => (
                      <label className="checkbox" key={p.id}>
                        <input type="checkbox" name="plantIds" value={p.id} />
                        {p.name}
                      </label>
                    ))}
                  {!plants.some((p) => p.tenantId === formTenant) && (
                    <small>
                      Select an organization with configured plants.
                    </small>
                  )}
                </fieldset>
              )}
              {error && (
                <p className="error" role="alert">
                  {error}
                </p>
              )}
              {success && (
                <p className="success" role="status">
                  {success}
                </p>
              )}
              <button
                className="primary"
                disabled={busy || (tab === "users" && !roles.data?.length)}
              >
                {busy
                  ? "Saving…"
                  : "Create " +
                    (tab === "users"
                      ? "user"
                      : tab === "plants"
                        ? "plant"
                        : "organization")}
              </button>
            </form>
          </section>
        </div>
      )}
    </>
  );
}
function AccessEditor({
  target,
  roles,
  plants,
  close,
}: {
  target: User;
  roles: string[];
  plants: Plant[];
  close: () => void;
}) {
  const client = useQueryClient();
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setBusy(true);
    setError("");
    try {
      await api(
        `/users/${target.id}/access`,
        { role: form.get("role"), plantIds: form.getAll("plantIds") },
        "PUT",
      );
      await client.invalidateQueries({ queryKey: ["users"] });
      close();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <dialog
      className="card modal"
      ref={(node) => {
        if (node && !node.open) node.showModal();
      }}
      aria-labelledby="access-title"
      onCancel={close}
    >
      <div className="modal-heading">
        <h2 id="access-title">Edit access · {target.displayName}</h2>
        <button
          className="icon-button"
          onClick={close}
          aria-label="Close access editor"
        >
          <X size={18} />
        </button>
      </div>
      <form onSubmit={save}>
        <label>
          Role
          <select
            aria-label="Role"
            name="role"
            defaultValue={target.role}
            autoFocus
          >
            {roles
              .filter((r) => r !== "PlatformAdmin")
              .map((r) => (
                <option key={r}>{r}</option>
              ))}
          </select>
        </label>
        <fieldset>
          <legend>Plant access</legend>
          {plants
            .filter((p) => p.tenantId === target.tenantId)
            .map((p) => (
              <label className="checkbox" key={p.id}>
                <input
                  type="checkbox"
                  name="plantIds"
                  value={p.id}
                  defaultChecked={target.plantIds.includes(p.id)}
                />
                {p.name}
              </label>
            ))}
        </fieldset>
        {error && (
          <p className="error" role="alert">
            {error}
          </p>
        )}
        <button className="primary" disabled={busy}>
          {busy ? "Saving…" : "Save access"}
        </button>
        <button type="button" className="secondary" onClick={close}>
          Cancel
        </button>
      </form>
    </dialog>
  );
}
function PasswordEditor({
  close,
  complete,
}: {
  close: () => void;
  complete: () => void;
}) {
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setBusy(true);
    setError("");
    try {
      await api("/auth/password", {
        currentPassword: form.get("currentPassword"),
        newPassword: form.get("newPassword"),
      });
      complete();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <dialog
      className="card modal"
      ref={(node) => {
        if (node && !node.open) node.showModal();
      }}
      aria-labelledby="password-title"
      onCancel={close}
    >
      <div className="modal-heading">
        <h2 id="password-title">Change password</h2>
        <button
          className="icon-button"
          onClick={close}
          aria-label="Close password editor"
        >
          <X size={18} />
        </button>
      </div>
      <p>
        Changing your password signs you out of all sessions. Sign in again with
        your new password.
      </p>
      <form onSubmit={save}>
        <label>
          Current password
          <input
            name="currentPassword"
            type="password"
            autoComplete="current-password"
            required
            autoFocus
          />
        </label>
        <label>
          New password
          <input
            aria-label="New password"
            name="newPassword"
            type="password"
            autoComplete="new-password"
            required
            minLength={14}
            maxLength={128}
          />
          <small>
            14–128 characters with uppercase, lowercase, and a digit.
          </small>
        </label>
        {error && (
          <p className="error" role="alert">
            {error}
          </p>
        )}
        <button className="primary" disabled={busy}>
          {busy ? "Saving…" : "Change password and sign out"}
        </button>
        <button type="button" className="secondary" onClick={close}>
          Cancel
        </button>
      </form>
    </dialog>
  );
}
function AuditView() {
  const result = useQuery({
    queryKey: ["audit"],
    queryFn: () =>
      api<
        {
          id: string;
          createdAt: string;
          actorId: string | null;
          tenantId: string | null;
          action: string;
          entityId: string | null;
        }[]
      >("/audit"),
  });
  return (
    <section className="card">
      <div className="card-heading">
        <div>
          <h2>Audit history</h2>
          <p>Latest 100 events within your authorized scope.</p>
        </div>
      </div>
      {result.isPending ? (
        <p role="status" className="empty compact">
          Loading audit history…
        </p>
      ) : result.isError ? (
        <p className="error" role="alert">
          {result.error.message}
        </p>
      ) : !result.data?.length ? (
        <p className="empty compact">No audit events yet.</p>
      ) : (
        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <th>Time</th>
                <th>Action</th>
                <th>Actor</th>
                <th>Entity</th>
              </tr>
            </thead>
            <tbody>
              {result.data.map((event) => (
                <tr key={event.id}>
                  <td>{new Date(event.createdAt).toLocaleString()}</td>
                  <td>{event.action}</td>
                  <td>{event.actorId || "System"}</td>
                  <td>{event.entityId || "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, refetchOnWindowFocus: false } },
});
createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </QueryClientProvider>
  </React.StrictMode>,
);
