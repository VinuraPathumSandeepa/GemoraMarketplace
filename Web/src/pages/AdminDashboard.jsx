import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { ArrowLeftRight, ArrowUpRight, ClipboardList, Gem, Truck } from "lucide-react";
import AdminLayout from "../layouts/AdminLayout";
import adminGemListingService from "../services/adminGemListingService";
import transactionService from "../services/marketplace/transactionService";
import { shipmentApi } from "../services/api";
import { nextShipmentAction, readableStatus, shortRef } from "../utils/shipmentQueue";

const terminalStatuses = new Set(["delivered", "cancelled"]);
const normalizedStatus = (status) => String(status ?? "").replace(/[\s_-]+/g, "").toLowerCase();

function buildRecentRecords({ listings, transactions, shipments }) {
  return [
    ...(listings || []).map((item) => ({
      key: `gem-${item.id}`, label: item.title || `Gem listing #${item.id}`,
      detail: `Gem listing created · #${item.id}`, status: item.status,
      createdAt: item.createdAt, to: "/admin/gem-listings", icon: Gem,
    })),
    ...(transactions || []).map((item) => ({
      key: `order-${item.id}`, label: item.orderNumber || `Order ${String(item.id).slice(0, 8)}`,
      detail: `Transaction created · ${item.gemTitle || "Gemstone order"}`, status: item.status,
      createdAt: item.createdAt, to: "/admin/transactions", icon: ArrowLeftRight,
    })),
    ...(shipments || []).map((item) => ({
      key: `shipment-${item.id}`, label: shortRef(item.id, "SHP", (shipments || []).map((shipment) => shipment.id)),
      detail: "Shipment created", status: item.status,
      createdAt: item.createdAt, to: `/admin/shipments/${item.id}`, icon: Truck,
    })),
  ].filter((item) => Number.isFinite(Date.parse(item.createdAt)))
    .sort((left, right) => Date.parse(right.createdAt) - Date.parse(left.createdAt))
    .slice(0, 6);
}

function StatCard({ icon: Icon, label, value, to, loading }) {
  return <Link to={to} className="admin-stat-card" aria-label={`View ${label}`}>
    <span className="admin-stat-card-top"><span className="admin-stat-icon"><Icon size={19} aria-hidden="true" /></span><ArrowUpRight className="admin-stat-arrow" size={18} aria-hidden="true" /></span>
    {loading ? <span className="admin-stat-skeleton" aria-label={`Loading ${label}`} /> : <strong>{value ?? "—"}</strong>}
    <span>{label}</span>
  </Link>;
}

async function fetchOverview() {
  const keys = ["listings", "transactions", "shipments"];
  const results = await Promise.allSettled([
    Promise.resolve().then(() => adminGemListingService.getAll()),
    Promise.resolve().then(() => transactionService.getAll()),
    Promise.resolve().then(() => shipmentApi.getMyShipments()).then((response) => response.data),
  ]);
  const data = {};
  const errors = {};
  results.forEach((result, index) => {
    const key = keys[index];
    if (result.status === "fulfilled" && Array.isArray(result.value)) {
      data[key] = result.value;
    } else {
      data[key] = null;
      const status = result.status === "rejected" ? result.reason?.response?.status : null;
      errors[key] = status === 401 || status === 403
        ? "Your session cannot access this Admin data. Sign in again or contact an administrator."
        : `Unable to load ${key}.`;
    }
  });
  return { data, errors };
}

export default function AdminDashboard() {
  const [data, setData] = useState({ listings: null, transactions: null, shipments: null });
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(true);
  const [request, setRequest] = useState(0);

  useEffect(() => {
    let active = true;
    fetchOverview().then(({ data: nextData, errors: nextErrors }) => {
      if (!active) return;
      setData(nextData);
      setErrors(nextErrors);
      setLoading(false);
    });
    return () => { active = false; };
  }, [request]);

  const recent = useMemo(() => buildRecentRecords(data), [data]);
  const activeShipments = data.shipments?.filter((item) => item.status && !terminalStatuses.has(normalizedStatus(item.status))).length;
  const priorityShipments = data.shipments?.filter((item) => nextShipmentAction(item).urgency === 0).length;
  const errorCount = Object.keys(errors).length;
  const dateLabel = new Intl.DateTimeFormat(undefined, { dateStyle: "full" }).format(new Date());
  const retryOverview = () => {
    setLoading(true);
    setErrors({});
    setRequest((current) => current + 1);
  };

  return <AdminLayout title="Overview">
    <div className="admin-overview">
      <header className="admin-overview-hero">
        <div><p className="admin-overview-eyebrow">MARKETPLACE OPERATIONS</p><h1>Admin Dashboard</h1><p>Marketplace operations and activity overview</p></div>
        <span className="admin-overview-date">{dateLabel}</span>
      </header>

      <section aria-labelledby="admin-stats-heading">
        <div className="admin-section-heading"><div><p className="admin-section-eyebrow">AT A GLANCE</p><h2 id="admin-stats-heading">Operations overview</h2></div><p>Live totals from Admin queues</p></div>
        <div className="admin-stat-grid">
          <StatCard icon={Gem} label="Current gem listings" value={data.listings?.length} to="/admin/gem-listings" loading={loading} />
          <StatCard icon={Truck} label="Active shipments" value={activeShipments} to="/admin/shipments" loading={loading} />
          <StatCard icon={ArrowLeftRight} label="Total transactions" value={data.transactions?.length} to="/admin/transactions" loading={loading} />
          <StatCard icon={ClipboardList} label="Priority shipments" value={priorityShipments} to="/admin/shipments" loading={loading} />
        </div>
      </section>

      {!loading && errorCount > 0 && <div className="admin-overview-notice" role="alert"><p>{errorCount === 3 ? "Overview data is unavailable." : "Some overview data is unavailable."} {Object.values(errors)[0]}</p><button type="button" className="admin-overview-retry" onClick={retryOverview}>Retry data</button></div>}

      <div className="admin-overview-columns">
        <section className="admin-overview-panel" aria-labelledby="admin-activity-heading">
          <div className="admin-overview-panel-head"><div><h2 id="admin-activity-heading">Recent marketplace records</h2><p>Newest listings, transactions and shipments</p></div></div>
          {loading ? <div role="status" aria-label="Loading recent records">{[1, 2, 3, 4].map((item) => <span key={item} className="admin-activity-skeleton" />)}</div>
            : recent.length === 0 ? <div className="admin-overview-state"><p>{errorCount === 3 ? "Recent records are unavailable." : "No recent marketplace records yet."}</p></div>
              : <ul className="admin-activity-list">{recent.map((item) => <li className="admin-activity-item" key={item.key}>
                <span className="admin-activity-icon"><item.icon size={17} aria-hidden="true" /></span>
                <div className="admin-activity-copy"><Link to={item.to}>{item.label}</Link><small>{item.detail}</small></div>
                <div className="admin-activity-meta"><span className="admin-status-pill">{readableStatus(item.status)}</span><time dateTime={item.createdAt}>{new Date(item.createdAt).toLocaleDateString()}</time></div>
              </li>)}</ul>}
        </section>

        <section className="admin-overview-panel" aria-labelledby="admin-actions-heading">
          <div className="admin-overview-panel-head"><div><h2 id="admin-actions-heading">Quick actions</h2><p>Go directly to a management queue</p></div></div>
          <div className="admin-quick-list">
            {[
              { to: "/admin/shipments", label: "View shipments", icon: Truck },
              { to: "/admin/transactions", label: "View transactions", icon: ArrowLeftRight },
              { to: "/admin/gem-listings", label: "Manage gem listings", icon: Gem },
            ].map(({ to, label, icon: Icon }) => <Link className="admin-quick-link" to={to} key={to}><Icon size={20} aria-hidden="true" /><span>{label}</span><ArrowUpRight size={17} aria-hidden="true" /></Link>)}
          </div>
        </section>
      </div>
    </div>
  </AdminLayout>;
}
