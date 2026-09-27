import {
  Navigate,
  Route,
  Routes,
} from "react-router-dom";

// Public pages
import Login from "./pages/Login";
import Register from "./pages/Register";

// Role redirect page
import Dashboard from "./pages/Dashboard";

// Role dashboards
import BuyerDashboard from "./pages/BuyerDashboard";
import SellerDashboard from "./pages/SellerDashboard";
import AdminDashboard from "./pages/AdminDashboard";
import GemologistDashboard from "./pages/GemologistDashboard";
import ExportOfficerDashboard from "./pages/ExportOfficerDashboard";
import ShipmentDetailPage from "./pages/ShipmentDetailPage";

// Route protection
import ProtectedRoute from "./routes/ProtectedRoute";
import RoleProtectedRoute from "./routes/RoleProtectedRoute";
import { useAuth } from "./context/AuthContext";

function AppWithAuth() {
  const { user, loading } = useAuth();

  // While loading auth, show nothing
  if (loading) {
    return <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>Loading...</div>;
  }

  // If already logged in, redirect to dashboard
  if (user) {
    return (
      <Routes>
        <Route path="/" element={<Navigate to="/admin" replace />} />
        <Route path="/login" element={<Navigate to="/admin" replace />} />
        <Route path="/register" element={<Navigate to="/admin" replace />} />
        
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/admin" element={<AdminDashboard />} />
        <Route path="/admin/shipments/:id" element={<ShipmentDetailPage />} />
        <Route path="/buyer" element={<BuyerDashboard />} />
        <Route path="/seller" element={<SellerDashboard />} />
        <Route path="/gemologist" element={<GemologistDashboard />} />
        <Route path="/export-officer" element={<ExportOfficerDashboard />} />
        <Route path="*" element={<Navigate to="/admin" replace />} />
      </Routes>
    );
  }

  // Not logged in, show login/register
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/login" replace />} />
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  );
}

function App() {
  return <AppWithAuth />;
}

export default App;