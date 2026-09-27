import { Link } from "react-router-dom";
import DashboardLayout from "../layouts/DashboardLayout";

function AdminDashboard() {
  return (
    <DashboardLayout title="Admin Dashboard">
      <div
        style={{
          display: "grid",
          gap: "16px",
          gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))",
          marginTop: "24px",
        }}
      >
        <div
          style={{
            border: "1px solid #e2e8f0",
            borderRadius: "12px",
            padding: "20px",
            background: "#fff",
          }}
        >
          <h3 style={{ marginTop: 0 }}>Shipping Operations</h3>
          <p style={{ marginBottom: "16px" }}>
            View shipment records, tracking history, and operational shipment status.
          </p>
          <Link to="/admin/shipping">Open Shipping Dashboard</Link>
        </div>

        <div
          style={{
            border: "1px solid #e2e8f0",
            borderRadius: "12px",
            padding: "20px",
            background: "#fff",
          }}
        >
          <h3 style={{ marginTop: 0 }}>Insurance Review</h3>
          <p style={{ marginBottom: "16px" }}>
            Inspect coverage, declared value, and policy state across shipments.
          </p>
          <span style={{ color: "#64748b" }}>Coming next</span>
        </div>
      </div>
    </DashboardLayout>
  );
}

export default AdminDashboard;