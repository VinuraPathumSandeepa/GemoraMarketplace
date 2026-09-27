import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
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
      setError(err.response?.data?.error || "Failed to load shipments. Please ensure the backend is running.");
    } finally {
      setLoading(false);
    }
  };

  // Convert status enum number to readable string
  const getStatusText = (statusCode) => {
    const statusMap = {
      0: "Pending",
      1: "Planning",
      2: "ReadyForBooking",
      3: "Booked",
      4: "PickedUp",
      5: "InTransit",
      6: "CustomsHold",
      7: "OutForDelivery",
      8: "Delivered",
      9: "DeliveryFailed",
      10: "Cancelled",
      11: "Exception"
    };
    return statusMap[statusCode] || "Unknown";
  };

  // Get CSS class for status badge
  const getStatusClass = (statusCode) => {
    if (statusCode === 0 || statusCode === 1) return "pending";
    if (statusCode >= 2 && statusCode <= 4) return "booked";
    if (statusCode === 5 || statusCode === 7) return "in-transit";
    if (statusCode === 8) return "delivered";
    if (statusCode >= 9 && statusCode <= 11) return "exception";
    return "unknown";
  };

  const filteredShipments = filterStatus === "all"
    ? shipments
    : shipments.filter(s => getStatusText(s.status).toLowerCase() === filterStatus.toLowerCase());

  const getStatusCounts = () => {
    const counts = {
      total: shipments.length,
      pending: shipments.filter(s => s.status === 0 || s.status === 1).length,
      inTransit: shipments.filter(s => s.status === 5).length,
      delivered: shipments.filter(s => s.status === 8).length,
      exceptions: shipments.filter(s =>
        [6, 9, 11].includes(s.status)
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
    <DashboardLayout title="Admin Shipping Dashboard">
      <div className="shipping-dashboard">
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
            <h2>Shipments</h2>
            <div className="filters">
              <label>Filter by status:</label>
              <select
                value={filterStatus}
                onChange={(e) => setFilterStatus(e.target.value)}
              >
                <option value="all">All</option>
                <option value="Pending">Pending</option>
                <option value="Planning">Planning</option>
                <option value="ReadyForBooking">Ready for Booking</option>
                <option value="Booked">Booked</option>
                <option value="PickedUp">Picked Up</option>
                <option value="InTransit">In Transit</option>
                <option value="CustomsHold">Customs Hold</option>
                <option value="OutForDelivery">Out for Delivery</option>
                <option value="Delivered">Delivered</option>
                <option value="DeliveryFailed">Delivery Failed</option>
                <option value="Cancelled">Cancelled</option>
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
                    <th>Order ID</th>
                    <th>Status</th>
                    <th>Origin</th>
                    <th>Destination</th>
                    <th>Service</th>
                    <th>Tracking #</th>
                    <th>Created</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredShipments.map((shipment) => (
                    <tr key={shipment.id}>
                      <td>{shipment.shipmentNumber}</td>
                      <td>{shipment.orderId.substring(0, 8)}...</td>
                      <td>
                        <span className={`status-badge ${getStatusClass(shipment.status)}`}>
                          {getStatusText(shipment.status)}
                        </span>
                      </td>
                      <td>{shipment.origin}</td>
                      <td>{shipment.destination}</td>
                      <td>{shipment.selectedService}</td>
                      <td>{shipment.trackingNumber || "N/A"}</td>
                      <td>{new Date(shipment.createdAt).toLocaleDateString()}</td>
                      <td>
                        <button
                          className="btn-link"
                          onClick={() => navigate(`/admin/shipments/${shipment.id}`)}
                        >
                          View Details
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
