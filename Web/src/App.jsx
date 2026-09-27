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
import ShippingDashboard from "./pages/ShippingDashboard";
import GemologistDashboard from "./pages/GemologistDashboard";
import ExportOfficerDashboard from "./pages/ExportOfficerDashboard";

// Route protection
import ProtectedRoute from "./routes/ProtectedRoute";
import RoleProtectedRoute from "./routes/RoleProtectedRoute";

function App() {
  return (
    <Routes>

      {/* ==========================================
          HOME
      ========================================== */}

      <Route
        path="/"
        element={
          <Navigate
            to="/login"
            replace
          />
        }
      />


      {/* ==========================================
          PUBLIC ROUTES
      ========================================== */}

      <Route
        path="/login"
        element={<Login />}
      />

      <Route
        path="/register"
        element={<Register />}
      />

      {/* ==========================================
          DEV ONLY ROUTES
          Use these to preview the Admin/Shipping UI
          while auth is still in place.
      ========================================== */}

      <Route
        path="/dev-admin"
        element={<AdminDashboard />}
      />

      <Route
        path="/dev-shipping"
        element={<ShippingDashboard />}
      />

      <Route
        path="/dev-seller"
        element={<SellerDashboard />}
      />

      <Route
        path="/dev-buyer-tracking"
        element={<BuyerDashboard />}
      />


      {/* ==========================================
          GENERAL DASHBOARD

          After login, Login.jsx sends the user here.

          Dashboard.jsx checks the user's role and
          redirects to the correct dashboard.
      ========================================== */}

      <Route
        path="/dashboard"
        element={
          <ProtectedRoute>
            <Dashboard />
          </ProtectedRoute>
        }
      />


      {/* ==========================================
          BUYER DASHBOARD
      ========================================== */}

      <Route
        path="/buyer"
        element={
          <RoleProtectedRoute
            allowedRoles={["Buyer"]}
          >
            <BuyerDashboard />
          </RoleProtectedRoute>
        }
      />


      {/* ==========================================
          SELLER DASHBOARD
      ========================================== */}

      <Route
        path="/seller"
        element={
          <RoleProtectedRoute
            allowedRoles={["Seller"]}
          >
            <SellerDashboard />
          </RoleProtectedRoute>
        }
      />


      {/* ==========================================
          ADMIN DASHBOARD
      ========================================== */}

      <Route
        path="/admin"
        element={
          <RoleProtectedRoute
            allowedRoles={["Admin"]}
          >
            <AdminDashboard />
          </RoleProtectedRoute>
        }
      />

      <Route
        path="/admin/shipping"
        element={
          <RoleProtectedRoute
            allowedRoles={["Admin"]}
          >
            <ShippingDashboard />
          </RoleProtectedRoute>
        }
      />


      {/* ==========================================
          GEMOLOGIST DASHBOARD
      ========================================== */}

      <Route
        path="/gemologist"
        element={
          <RoleProtectedRoute
            allowedRoles={["Gemologist"]}
          >
            <GemologistDashboard />
          </RoleProtectedRoute>
        }
      />


      {/* ==========================================
          EXPORT OFFICER DASHBOARD
      ========================================== */}

      <Route
        path="/export-officer"
        element={
          <RoleProtectedRoute
            allowedRoles={["ExportOfficer"]}
          >
            <ExportOfficerDashboard />
          </RoleProtectedRoute>
        }
      />


      {/* ==========================================
          UNKNOWN ROUTES
      ========================================== */}

      <Route
        path="*"
        element={
          <Navigate
            to="/dashboard"
            replace
          />
        }
      />

    </Routes>
  );
}

export default App;