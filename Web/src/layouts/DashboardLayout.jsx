import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import "../styles/DashboardLayout.css";

function DashboardLayout({ title, children }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <div className="dashboard-layout">
      <header>
        <h1>Gemora</h1>

        <div>
          <span>
            {user?.fullName} ({user?.role})
          </span>

          <button onClick={handleLogout}>
            Logout
          </button>
        </div>
      </header>

      <main>
        <h2>{title}</h2>

        <p>
          Welcome, {user?.fullName}
        </p>

        <p>
          Email: {user?.email}
        </p>

        {children}
      </main>
    </div>
  );
}

export default DashboardLayout;