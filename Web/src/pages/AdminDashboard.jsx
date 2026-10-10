import { useState, useEffect } from "react";
import { useNavigate, Link } from "react-router-dom";
import DashboardLayout from "../layouts/DashboardLayout";
import { shipmentApi } from "../services/api";
import "../styles/ShippingDashboard.css";

function AdminDashboard() {
  
  const navigate = useNavigate();
  const [shipments, setShipments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [filterStatus, setFilterStatus] = useState("all");

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
      setError(err.response?.data?.message || "Failed to load shipments. Please ensure the backend is running.");
    } finally {
      setLoading(false);
    }
  };

  // Get CSS class for status badge
  const getStatusClass = (status) => {
    switch(status.toLowerCase()) {
      case 'pending':
      case 'plangenerated':
        return "pending";
      case 'booked':
      case 'intransit':
      case 'outfordelivery':
        return "in-transit";
      case 'delivered':
        return "delivered";
      case 'exception':
      case 'deliveryfailed':
      case 'cancelled':
        return "exception";
      default:
        return "unknown";
    }
  };

  const filteredShipments = filterStatus === "all"
    ? shipments
    : shipments.filter(s => s.status.toLowerCase() === filterStatus.toLowerCase());

  const getStatusCounts = () => {
    const counts = {
      total: shipments.length,
      pending: shipments.filter(s => ['Pending', 'PlanGenerated'].includes(s.status)).length,
      inTransit: shipments.filter(s => s.status === 'InTransit').length,
      delivered: shipments.filter(s => s.status === 'Delivered').length,
      exceptions: shipments.filter(s =>
        ['Exception', 'DeliveryFailed', 'Cancelled'].includes(s.status)
      ).length,
    };
    return counts;
  };

  const counts = getStatusCounts();

  if (loading) {
    return (
      <DashboardLayout title="Admin Shipping Dashboard">
        <div className="loading-state">Loading shipments...</div>
      </DashboardLayout>
    );
  }

  if (error) {
    return (
      <DashboardLayout title="Admin Shipping Dashboard">
        <div className="error-state">
          <p>Error: {error}</p>
          <button onClick={loadShipments}>Retry</button>
        </div>
      </DashboardLayout>
    );
  }

  return (
    <DashboardLayout title="Admin Dashboard">
      <div style={{ padding: 24 }}>
        <h2>Gemora Administration</h2>
        <p>Review marketplace transactions and operational activity.</p>
        <button
          type="button"
          onClick={() => navigate("/admin/transactions")}
          style={{ padding: "12px 18px", borderRadius: 10, border: 0, cursor: "pointer" }}
        >
          Open Transaction Dashboard
        </button>
      </div>
      <div className="shipping-dashboard">
        {/* Quick Navigation */}
        <div className="mb-6 flex gap-4">
          <Link
            to="/admin/shipments"
            className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 font-medium"
          >
            View All Shipments →
          </Link>
          <Link
            to="/admin/ai-dashboard"
            className="px-4 py-2 bg-purple-600 text-white rounded hover:bg-purple-700 font-medium"
          >
            🤖 AI Agent Dashboard →
          </Link>
        </div>

        {/* Statistics Cards */}
        <div className="stats-grid">
          <div className="stat-card">
            <h3>Total Shipments</h3>
            <p className="stat-value">{counts.total}</p>
          </div>
          <div className="stat-card pending">
            <h3>Pending</h3>
            <p className="stat-value">{counts.pending}</p>
          </div>
          <div className="stat-card transit">
            <h3>In Transit</h3>
            <p className="stat-value">{counts.inTransit}</p>
          </div>
          <div className="stat-card delivered">
            <h3>Delivered</h3>
            <p className="stat-value">{counts.delivered}</p>
          </div>
          <div className="stat-card exception">
            <h3>Exceptions</h3>
            <p className="stat-value">{counts.exceptions}</p>
          </div>
        </div>

        {/* Shipment List */}
        <div className="shipment-section">
          <div className="section-header">
            <h2>Recent Shipments</h2>
            <div className="filters">
              <label>Filter by status:</label>
              <select
                value={filterStatus}
                onChange={(e) => setFilterStatus(e.target.value)}
              >
                <option value="all">All</option>
                <option value="Pending">Pending</option>
                <option value="PlanGenerated">Plan Generated</option>
                <option value="ReadyForBooking">Ready for Booking</option>
                <option value="Booked">Booked</option>
                <option value="InTransit">In Transit</option>
                <option value="Delivered">Delivered</option>
                <option value="Exception">Exception</option>
              </select>
            </div>
          </div>

          {filteredShipments.length === 0 ? (
            <div className="empty-state">
              <p>No shipments found{filterStatus !== "all" ? ` with status "${filterStatus}"` : ""}.</p>
            </div>
          ) : (
            <div className="shipment-table">
              <table>
                <thead>
                  <tr>
                    <th>Shipment #</th>
                    <th>Status</th>
                    <th>Declared Value</th>
                    <th>Destination</th>
                    <th>Tracking #</th>
                    <th>Created</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredShipments.map((shipment) => (
                    <tr key={shipment.id}>
                      <td>{shipment.id.substring(0, 8)}...</td>
                      <td>
                        <span className={`status-badge ${getStatusClass(shipment.status)}`}>
                          {shipment.status}
                        </span>
                      </td>
                      <td>{shipment.currency} {shipment.declaredValue.toLocaleString()}</td>
                      <td>{shipment.destinationRegion}, {shipment.destinationCountryCode}</td>
                      <td>{shipment.trackingNumber || "N/A"}</td>
                      <td>{new Date(shipment.createdAt).toLocaleDateString()}</td>
                      <td>
                        <button
                          className="btn-link"
                          onClick={() => navigate(`/admin/shipments/${shipment.id}`)}
                        >
                          Manage
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </DashboardLayout>
  );
}

export default AdminDashboard;
