import { useNavigate } from "react-router-dom";
import DashboardLayout from "../layouts/DashboardLayout";

function AdminDashboard() {
  const navigate = useNavigate();
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
    </DashboardLayout>
  );
}

export default AdminDashboard;
