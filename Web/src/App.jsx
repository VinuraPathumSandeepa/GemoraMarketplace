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

// ============================================================
// COMPONENT 1 — SELLER GEM LISTING PAGES
// ============================================================

import MyGemListings from "./pages/seller/MyGemListings";
import CreateGemListing from "./pages/seller/CreateGemListing";
import GemListingDetails from "./pages/seller/GemListingDetails";
import EditGemListing from "./pages/seller/EditGemListing";

// ============================================================
// COMPONENT 3 — SHIPPING & INSURANCE PAGES
// ============================================================

import SellerShipments from "./pages/SellerShipments";
import CreateShipment from "./pages/CreateShipment";
import ShipmentDetail from "./pages/ShipmentDetail";
import BuyerShipments from "./pages/BuyerShipments";
import AdminShipments from "./pages/AdminShipments";

// ============================================================
// COMPONENT 1 — GEMOLOGIST PAGES
//
// We will enable these after finishing/testing the Seller UI.
// ============================================================

// import VerificationQueue from "./pages/gemologist/VerificationQueue";
// import VerificationDetails from "./pages/gemologist/VerificationDetails";

// ============================================================
// ROUTE PROTECTION
// ============================================================

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


      {/* ======================================================
          COMPONENT 1 — SELLER: MY GEM LISTINGS

          URL:
          /seller/listings
          ====================================================== */}

      <Route
        path="/seller/listings"
        element={
          <RoleProtectedRoute
            allowedRoles={["Seller"]}
          >
            <MyGemListings />
          </RoleProtectedRoute>
        }
      />


      {/* ======================================================
          COMPONENT 1 — SELLER: CREATE GEM LISTING

          URL:
          /seller/listings/create
          ====================================================== */}

      <Route
        path="/seller/listings/create"
        element={
          <RoleProtectedRoute
            allowedRoles={["Seller"]}
          >
            <CreateGemListing />
          </RoleProtectedRoute>
        }
      />


      {/* ======================================================
          COMPONENT 1 — SELLER: EDIT GEM LISTING

          Example:
          /seller/listings/10/edit

          Editing is permitted in the UI only for:
          - Draft
          - ChangesRequested

          Backend remains the final security enforcement.
          ====================================================== */}

      <Route
        path="/seller/listings/:id/edit"
        element={
          <RoleProtectedRoute
            allowedRoles={["Seller"]}
          >
            <EditGemListing />
          </RoleProtectedRoute>
        }
      />


      {/* ======================================================
          COMPONENT 1 — SELLER: GEM LISTING DETAILS

          Example:
          /seller/listings/10

          Supports:
          - View listing
          - View gemstone image
          - Upload/replace image
          - View certificate
          - Upload/replace certificate
          - Edit listing
          - Submit for verification
          - Resubmit after ChangesRequested
          - Delete Draft
          ====================================================== */}

      <Route
        path="/seller/listings/:id"
        element={
          <RoleProtectedRoute
            allowedRoles={["Seller"]}
          >
            <GemListingDetails />
          </RoleProtectedRoute>
        }
      />


      {/* ======================================================
          COMPONENT 3 — SELLER: SHIPMENTS

          URL:
          /seller/shipments
              → SellerShipments (list)

          /seller/shipments/create
              → CreateShipment (form)

          /seller/shipments/:id
              → ShipmentDetail (view with AI plan, tracking, insurance)
          ====================================================== */}

      <Route
        path="/seller/shipments"
        element={
          <RoleProtectedRoute
            allowedRoles={["Seller"]}
          >
            <SellerShipments />
          </RoleProtectedRoute>
        }
      />

      <Route
        path="/seller/shipments/create"
        element={
          <RoleProtectedRoute
            allowedRoles={["Seller"]}
          >
            <CreateShipment />
          </RoleProtectedRoute>
        }
      />

      <Route
        path="/seller/shipments/:id"
        element={
          <RoleProtectedRoute
            allowedRoles={["Seller"]}
          >
            <ShipmentDetail />
          </RoleProtectedRoute>
        }
      />


      {/* ======================================================
          COMPONENT 3 — BUYER: SHIPMENTS

          URL:
          /buyer/shipments
              → BuyerShipments (list)

          /buyer/shipments/:id
              → ShipmentDetail (view tracking & insurance)
          ====================================================== */}

      <Route
        path="/buyer/shipments"
        element={
          <RoleProtectedRoute
            allowedRoles={["Buyer"]}
          >
            <BuyerShipments />
          </RoleProtectedRoute>
        }
      />

      <Route
        path="/buyer/shipments/:id"
        element={
          <RoleProtectedRoute
            allowedRoles={["Buyer"]}
          >
            <ShipmentDetail />
          </RoleProtectedRoute>
        }
      />


      {/* ======================================================
          COMPONENT 3 — ADMIN: SHIPMENTS

          URL:
          /admin/shipments
              → AdminShipments (dashboard with all shipments)

          /admin/shipments/:id
              → ShipmentDetail (manage tracking, status, insurance)
          ====================================================== */}

      <Route
        path="/admin/shipments"
        element={
          <RoleProtectedRoute
            allowedRoles={["Admin"]}
          >
            <AdminShipments />
          </RoleProtectedRoute>
        }
      />

      <Route
        path="/admin/shipments/:id"
        element={
          <RoleProtectedRoute
            allowedRoles={["Admin"]}
          >
            <ShipmentDetail />
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