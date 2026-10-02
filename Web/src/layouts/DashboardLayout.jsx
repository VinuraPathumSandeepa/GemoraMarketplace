import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import "./DashboardLayout.css";

function DashboardLayout({ title, subtitle, children }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  const formatRole = (role) => {
    if (!role) return "";
    if (role === "ExportOfficer") return "Export Officer";
    return role;
  };

  return (
    <div className="dashboard-layout">
      <header className="app-header-nav">
        <div className="header-left">
          <span className="brand-badge">Gemora</span>
          <h1 className="header-title">{title || "Dashboard"}</h1>
          {subtitle && <p className="header-subtitle">{subtitle}</p>}
        </div>

        <div className="header-right">
          <div className="user-profile-info">
            <span className="user-role-badge">{formatRole(user?.role)}</span>
            <div className="user-details">
              <span className="user-name">{user?.fullName || "User"}</span>
              <span className="user-email">{user?.email || ""}</span>
            </div>
          </div>

          <button className="btn-logout" onClick={handleLogout}>
            Logout
          </button>
        </div>
      </header>

      <main className="dashboard-main-content">{children}</main>
    </div>
  );
}

export default DashboardLayout;