import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { shipmentApi } from "../services/api";
import "../styles/AdminShipping.css";
import "../styles/ShipmentDashboard.css";
import AdminAIDashboard from "./AdminAIDashboard";

function AdminShipments() {
  const [shipments, setShipments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [statusFilter, setStatusFilter] = useState("All");
  const [search, setSearch] = useState("");

  useEffect(() => {
    loadShipments();
  }, []);

  const loadShipments = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await shipmentApi.getMyShipments();
      setShipments(response.data);
    } catch (err) {
      console.error("Failed to load shipments:", err);
      setError(err.response?.data?.message || "Failed to load shipments");
    } finally {
      setLoading(false);
    }
  };

  // Filter shipments by status
  const filteredShipments = shipments.filter(shipment =>
    (statusFilter === "All" || shipment.status === statusFilter) &&
    [shipment.id, shipment.orderId, shipment.trackingNumber, shipment.packageDescription,
      shipment.destinationRegion, shipment.destinationCountryCode]
      .filter(Boolean).join(" ").toLowerCase().includes(search.trim().toLowerCase()));

  // Get unique statuses for filter dropdown
  const statuses = ["All", ...new Set(shipments.map(s => s.status))];

  const summary = [
    { label: "Total Shipments", value: shipments.length, tone: "total" },
    { label: "Pending", value: shipments.filter(s => ["Pending", "PlanGenerated"].includes(s.status)).length, tone: "pending" },
    { label: "In Transit", value: shipments.filter(s => s.status === "InTransit").length, tone: "transit" },
    { label: "Delivered", value: shipments.filter(s => s.status === "Delivered").length, tone: "delivered" },
    { label: "Exceptions", value: shipments.filter(s => ["Exception", "DeliveryFailed", "Cancelled"].includes(s.status)).length, tone: "exception" },
  ];

  if (loading) {
    return (
      <div className="admin-shipping admin-shipping-state">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading shipments...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="admin-shipping admin-shipping-state">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-800">{error}</p>
          <button 
            onClick={loadShipments}
            className="mt-2 text-sm text-red-600 hover:text-red-800 underline"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="admin-shipping admin-shipping-list">
      <div className="admin-shipping-header">
        <div>
        <p className="admin-shipping-eyebrow">Shipping operations</p>
        <h1>Shipment Dashboard</h1>
        <p>Manage fulfilment, insurance, and delivery from one place. Select Analyze risk on a shipment to review AI shipping recommendations.</p>
        <div className="shipment-dashboard-shortcuts">
          <Link to="/admin/transactions" className="shipment-manage">Transaction Dashboard</Link>
          <a href="#shipping-ai-analyzer" className="shipment-analyze">AI Shipping Plan Analyzer</a>
        </div>
        </div>
      </div>

      {/* Status Filter */}
      <section className="admin-shipping-stats" aria-label="Shipment summary">
        {summary.map(stat => (
          <article key={stat.label} className={`admin-shipping-stat ${stat.tone}`}>
            <h2>{stat.label}</h2>
            <p>{stat.value}</p>
          </article>
        ))}
      </section>
      <div className="shipment-filters">
        <div className="shipment-search">
          <label htmlFor="shipment-search">Search shipments</label>
          <input id="shipment-search" type="search" value={search} onChange={event => setSearch(event.target.value)} placeholder="Shipment ID, tracking, order, package or destination" />
        </div>
        <div className="shipment-status-filter">
        <label htmlFor="shipment-status">Status</label>
        <select
          id="shipment-status"
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          className="border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          {statuses.map(status => (
            <option key={status} value={status}>{status}</option>
          ))}
        </select>
        </div>
        <span className="text-sm text-gray-500">
          {filteredShipments.length} shipment{filteredShipments.length !== 1 ? 's' : ''} found
        </span>
      </div>

      {filteredShipments.length === 0 ? (
        <div className="bg-white rounded-lg shadow p-8 text-center">
          <p className="text-gray-500">No shipments found.</p>
        </div>
      ) : (
        <div className="admin-shipping-panel admin-shipping-table">
          <table className="w-full">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Shipment</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Order</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Seller</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Buyer</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Declared Value</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Destination</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Created</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {filteredShipments.map((shipment) => (
                <tr key={shipment.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm font-medium text-gray-900">{shipment.id.substring(0, 8)}...</div>
                    {shipment.trackingNumber && (
                      <div className="text-xs text-gray-500">TRK: {shipment.trackingNumber}</div>
                    )}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-900">{shipment.orderId.substring(0, 8)}...</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-900">{shipment.sellerId.substring(0, 8)}...</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-900">{shipment.buyerId.substring(0, 8)}...</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className={`px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full ${
                      shipment.status === 'Delivered' ? 'bg-green-100 text-green-800' :
                      shipment.status === 'InTransit' ? 'bg-blue-100 text-blue-800' :
                      shipment.status === 'Exception' ? 'bg-red-100 text-red-800' :
                      shipment.status === 'Cancelled' ? 'bg-gray-100 text-gray-800' :
                      'bg-yellow-100 text-yellow-800'
                    }`}>
                      {shipment.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-900">
                      {shipment.currency} {shipment.declaredValue.toLocaleString()}
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <div className="text-sm text-gray-900">
                      {shipment.destinationRegion}, {shipment.destinationCountryCode}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-900">
                      {new Date(shipment.createdAt).toLocaleDateString()}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                    <Link
                      to={`/admin/shipments/${shipment.id}`}
                      className="shipment-manage"
                    >
                      Manage
                    </Link>
                    <Link to={`/admin/shipments/${shipment.id}?tab=plan`} className="shipment-analyze">Analyze risk</Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <section id="shipping-ai-analyzer" className="shipment-dashboard-analyzer" aria-label="AI Shipping Plan Analyzer">
        <AdminAIDashboard embedded />
      </section>
    </div>
  );
}

export default AdminShipments;
