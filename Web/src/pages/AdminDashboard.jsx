import { useNavigate } from "react-router-dom";
import DashboardLayout from "../layouts/DashboardLayout";

function AdminDashboard() {
  const navigate = useNavigate();
  return (
    <DashboardLayout title="Admin Dashboard">
      <div style={{ padding: 24 }}>
        <h2>Gemora Administration</h2>
        <p>Review marketplace transactions and operational activity.</p>
        <div style={{ display: "flex", flexWrap: "wrap", gap: 12 }}>
        <button
          type="button"
          onClick={() => navigate("/admin/transactions")}
          style={{ padding: "12px 18px", borderRadius: 10, border: 0, cursor: "pointer" }}
        >
          Open Transaction Dashboard
        </button>
        <button
          type="button"
          onClick={() => navigate("/admin/gem-listings")}
          style={{ padding: "12px 18px", borderRadius: 10, border: 0, cursor: "pointer" }}
        >
          Manage Gem Listings
        </button>
        </div>
      </div>
    </DashboardLayout>
  );
}

export default AdminDashboard;
