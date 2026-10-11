import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { shipmentApi } from "../services/api";
import ShippingAIAgentPanel from "../components/ShippingAIAgentPanel";
import { STATUS_GROUPS, matchesStatus, nextShipmentAction, readableStatus, shortRef, sortByUrgency } from "../utils/shipmentQueue";
import "../styles/AdminShipping.css";
import "../styles/ShipmentDashboard.css";

function AdminShipments() {
  const [shipments, setShipments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [statusFilter, setStatusFilter] = useState("All");
  const [search, setSearch] = useState("");
  const [copyMessage, setCopyMessage] = useState("");
  useEffect(() => { loadShipments(); }, []);

  async function loadShipments() {
    try {
      setLoading(true);
      setError(null);
      const response = await shipmentApi.getMyShipments();
      if (!Array.isArray(response.data)) throw new Error("Unexpected shipment response. Please retry.");
      setShipments(response.data);
    } catch (err) {
      setError(err.response?.data?.message || err.message || "We couldn't load the shipment queue.");
    } finally { setLoading(false); }
  }

  const ids = shipments.map(shipment => shipment.id);
  const orderIds = shipments.map(shipment => shipment.orderId);
  const sellerIds = shipments.map(shipment => shipment.sellerId);
  const buyerIds = shipments.map(shipment => shipment.buyerId);
  const filtered = sortByUrgency(shipments.filter(shipment => matchesStatus(shipment, statusFilter) && [
    shipment.id, shortRef(shipment.id, "SHP", ids), shipment.orderId, shortRef(shipment.orderId, "ORD", orderIds),
    shipment.sellerName, shipment.buyerName, shortRef(shipment.sellerId, "SELLER", sellerIds),
    shortRef(shipment.buyerId, "BUYER", buyerIds), shipment.trackingNumber, shipment.packageDescription,
    shipment.destinationRegion, shipment.destinationCountryCode,
  ].filter(Boolean).join(" ").toLowerCase().includes(search.trim().toLowerCase())));
  const needsAction = filtered.filter(shipment => nextShipmentAction(shipment).urgency !== null);
  const otherShipments = filtered.filter(shipment => nextShipmentAction(shipment).urgency === null);
  const summary = [
    { label: "Total Shipments", filter: "All", tone: "total" },
    { label: "Pending", filter: "Pending", tone: "pending" },
    { label: "In Transit", filter: "In Transit", tone: "transit" },
    { label: "Delivered", filter: "Delivered", tone: "delivered" },
    { label: "Exceptions", filter: "Exceptions", tone: "exception" },
  ];
  const statuses = [...new Set(shipments.map(shipment => shipment.status))].filter(status => !Object.hasOwn(STATUS_GROUPS, status));

  async function copyId(id, reference) {
    try {
      await navigator.clipboard.writeText(id);
      setCopyMessage(`Copied full ID for ${reference}.`);
    } catch { setCopyMessage("Couldn't copy the ID. Check clipboard permissions and try again."); }
  }

  function reference(id, prefix, allIds) {
    const label = shortRef(id, prefix, allIds);
    return <span className="shipment-reference">{label}{id && <button type="button" className="shipment-copy" onClick={() => copyId(id, label)} aria-label={`Copy full ID for ${label}`} title="Copy full ID">Copy</button>}</span>;
  }

  function shipmentRow(shipment) {
    const action = nextShipmentAction(shipment);
    return <tr key={shipment.id}>
      <td>{reference(shipment.id, "SHP", ids)}{shipment.trackingNumber && <div className="text-xs text-gray-500">Tracking: {shipment.trackingNumber}</div>}</td>
      <td>{reference(shipment.orderId, "ORD", orderIds)}</td>
      <td>{shipment.sellerName || shortRef(shipment.sellerId, "SELLER", sellerIds)}</td>
      <td>{shipment.buyerName || shortRef(shipment.buyerId, "BUYER", buyerIds)}</td>
      <td><span className={`px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full ${
        matchesStatus(shipment, "Exceptions") ? "bg-red-100 text-red-800" :
        shipment.status === "Delivered" ? "bg-green-100 text-green-800" :
        matchesStatus(shipment, "In Transit") ? "bg-blue-100 text-blue-800" :
        shipment.status === "Cancelled" ? "bg-gray-100 text-gray-800" : "bg-yellow-100 text-yellow-800"
      }`}>{readableStatus(shipment.status)}</span><div className="text-xs text-gray-500">{action.reason}</div></td>
      <td>{shipment.currency} {Number(shipment.declaredValue || 0).toLocaleString()}</td>
      <td>{[shipment.destinationRegion, shipment.destinationCountryCode].filter(Boolean).join(", ") || "Not provided"}</td>
      <td>{shipment.createdAt ? new Date(shipment.createdAt).toLocaleDateString() : "Not provided"}</td>
      <td><div className="shipment-row-actions"><Link to={`/admin/shipments/${shipment.id}?tab=${action.tab}`} className="shipment-analyze">{action.label}</Link><Link to={`/admin/shipments/${shipment.id}`} className="shipment-manage" aria-label={`Open ${shortRef(shipment.id, "SHP", ids)}`}>Open</Link></div></td>
    </tr>;
  }

  if (loading) return <div className="admin-shipping admin-shipping-state" role="status"><p>Loading shipment queue...</p></div>;
  if (error) return <div className="admin-shipping admin-shipping-state"><div className="admin-shipping-panel" role="alert"><p>{error}</p><button type="button" onClick={loadShipments}>Retry</button></div></div>;

  return <div className="admin-shipping admin-shipping-list">
    <header className="admin-shipping-header"><div>
      <p className="admin-shipping-eyebrow">Shipping operations</p><h1>Shipment work queue</h1>
      <p>Review urgent work first, then follow the next action for each shipment. Booking and insurance are simulated and require admin approval.</p>
      <div className="shipment-dashboard-shortcuts"><Link to="/admin/transactions" className="shipment-manage">Transaction Dashboard</Link></div>
    </div></header>
    <section className="admin-shipping-stats" aria-label="Filter shipment queue">
      {summary.map(stat => <button type="button" key={stat.filter} className={`admin-shipping-stat ${stat.tone}`} aria-pressed={statusFilter === stat.filter} onClick={() => setStatusFilter(stat.filter)}>
        <h2>{stat.label}</h2><p>{shipments.filter(shipment => matchesStatus(shipment, stat.filter)).length}</p>
      </button>)}
    </section>
    <div className="shipment-filters">
      <div className="shipment-search"><label htmlFor="shipment-search">Search shipments</label><input id="shipment-search" type="search" value={search} onChange={event => setSearch(event.target.value)} placeholder="Reference, name, tracking, package or destination" /></div>
      <div className="shipment-status-filter"><label htmlFor="shipment-status">Status</label><select id="shipment-status" value={statusFilter} onChange={event => setStatusFilter(event.target.value)}>
        {["All", ...Object.keys(STATUS_GROUPS), ...statuses].map(status => <option key={status} value={status}>{readableStatus(status)}</option>)}
      </select></div>
      <span role="status">{filtered.length} shipment{filtered.length === 1 ? "" : "s"} found</span>
    </div>
    <p role="status" className="shipment-copy-message">{copyMessage}</p>
    {filtered.length === 0 ? <div className="admin-shipping-panel"><p>{shipments.length ? "No shipments match these filters." : "No shipments yet. Shipments appear here after a paid order is prepared for shipping."}</p>{shipments.length > 0 && <button type="button" onClick={() => { setSearch(""); setStatusFilter("All"); }}>Clear filters</button>}</div> :
      <div className="admin-shipping-panel admin-shipping-table"><table>
        <caption className="shipment-queue-caption">Shipment queue — urgent actions first</caption>
        <thead><tr>{["Shipment", "Order", "Seller", "Buyer", "Status / next step", "Declared value", "Destination", "Created", "Actions"].map(label => <th scope="col" key={label}>{label}</th>)}</tr></thead>
        <tbody><tr className="shipment-queue-heading"><th scope="rowgroup" colSpan={9}>Needs your action ({needsAction.length})</th></tr>
          {needsAction.length ? needsAction.map(shipmentRow) : <tr><td colSpan={9}>No urgent admin actions for the current filters.</td></tr>}
        </tbody>
        {otherShipments.length > 0 && <tbody><tr className="shipment-queue-heading"><th scope="rowgroup" colSpan={9}>Other shipments ({otherShipments.length})</th></tr>{otherShipments.map(shipmentRow)}</tbody>}
      </table></div>}
    <ShippingAIAgentPanel shipments={shipments} />
  </div>;
}

export default AdminShipments;
