import {
  Navigate,
  Route,
  Routes,
} from "react-router-dom";

// ============================================================
// PUBLIC PAGES
// ============================================================

import Login from "./pages/Login";
import Register from "./pages/Register";

// ============================================================
// GENERAL DASHBOARD
// ============================================================

import Dashboard from "./pages/Dashboard";

// ============================================================
// ROLE DASHBOARDS
// ============================================================

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

      {/* ======================================================
          HOME
          ====================================================== */}

      <Route
        path="/"
        element={
          <Navigate
            to="/login"
            replace
          />
        }
      />


      {/* ======================================================
          PUBLIC ROUTES
          ====================================================== */}

      <Route
        path="/login"
        element={<Login />}
      />

      <Route
        path="/register"
        element={<Register />}
      />


      {/* ======================================================
          GENERAL DASHBOARD

          After login, Dashboard.jsx checks the role and
          redirects the user to the correct role dashboard.
          ====================================================== */}

      <Route
        path="/dashboard"
        element={
          <ProtectedRoute>
            <Dashboard />
          </ProtectedRoute>
        }
      />


      {/* ======================================================
          BUYER DASHBOARD
          ====================================================== */}

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


      {/* ======================================================
          SELLER DASHBOARD
          ====================================================== */}

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

          Editing is permitted by the UI only for:

          - Draft
          - ChangesRequested

          Backend rules remain the final security enforcement.
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

          - View listing information
          - View gemstone image
          - Upload/replace gemstone image
          - View certificate
          - Upload/replace certificate
          - Navigate to Edit
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
          ADMIN DASHBOARD
          ====================================================== */}

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


      {/* ======================================================
          GEMOLOGIST DASHBOARD
          ====================================================== */}

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


      {/* ======================================================
          COMPONENT 1 — GEMOLOGIST VERIFICATION

          Next phase:

          /gemologist/verifications
              → VerificationQueue

          /gemologist/verifications/:id
              → VerificationDetails

          These will provide:

          - Pending verification queue
          - Seller evidence
          - Gemstone image
          - Certificate information
          - Run AI analysis
          - AI visual observations
          - AI risk flags
          - AI suggested gem type
          - Confidence score
          - Approve
          - Request Changes
          - Reject
          ====================================================== */}


      {/* ======================================================
          EXPORT OFFICER DASHBOARD
          ====================================================== */}

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


      {/* ======================================================
          UNKNOWN ROUTES

          Invalid URLs redirect through /dashboard.
          Dashboard.jsx then handles role-based redirection.
          ====================================================== */}

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