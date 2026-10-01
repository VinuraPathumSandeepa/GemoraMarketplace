import {
  Navigate,
  Route,
  Routes,
} from "react-router-dom";

// ============================================================
// PUBLIC PAGES
// ============================================================

import Home from "./pages/Home";
import Login from "./pages/Login";
import Register from "./pages/Register";
import VerifyEmail from "./pages/VerifyEmail";

// ============================================================
// GENERAL DASHBOARD / SHARED PAGES
// ============================================================

import Dashboard from "./pages/Dashboard";
import Profile from "./pages/Profile";

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
// COMPONENT 1 — GEMOLOGIST VERIFICATION PAGES
// ============================================================

import VerificationQueue from "./pages/gemologist/VerificationQueue";
import VerificationDetails from "./pages/gemologist/VerificationDetails";

// ============================================================
// ROUTE PROTECTION
// ============================================================

import ProtectedRoute from "./routes/ProtectedRoute";
import RoleProtectedRoute from "./routes/RoleProtectedRoute";


function App() {
  return (
    <Routes>

      {/* ======================================================
          PUBLIC HOME
          ====================================================== */}

      <Route
        path="/"
        element={<Home />}
      />

      {/* ======================================================
          PUBLIC AUTH ROUTES
          ====================================================== */}

      <Route
        path="/login"
        element={<Login />}
      />

      <Route
        path="/register"
        element={<Register />}
      />

      <Route
        path="/verify-email"
        element={<VerifyEmail />}
      />

      {/* ======================================================
          GENERAL DASHBOARD

          After login:
          Dashboard.jsx checks the logged-in user's role
          and redirects them to the appropriate dashboard.
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
          SHARED USER PROFILE

          Available to every authenticated role:
          - Buyer
          - Seller
          - Gemologist
          - ExportOfficer
          - Admin
          ====================================================== */}

      <Route
        path="/profile"
        element={
          <ProtectedRoute>
            <Profile />
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

          Editing is intended for:
          - Draft
          - ChangesRequested

          Backend rules remain the final enforcement.
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
          COMPONENT 1 — GEMOLOGIST VERIFICATION QUEUE

          URL:
          /gemologist/verifications

          Supports:
          - Pending verification queue
          - Search
          - Sort
          - Evidence indicators
          - AI status indicators
          - Open verification
          ====================================================== */}

      <Route
        path="/gemologist/verifications"
        element={
          <RoleProtectedRoute
            allowedRoles={["Gemologist"]}
          >
            <VerificationQueue />
          </RoleProtectedRoute>
        }
      />

      {/* ======================================================
          COMPONENT 1 — GEMOLOGIST VERIFICATION DETAILS

          Example:
          /gemologist/verifications/5

          Supports:
          - Seller information
          - Gemstone details
          - Gemstone image evidence
          - Certificate evidence
          - AI-assisted analysis
          - AI suggested gem type
          - Confidence score
          - AI findings
          - Visual observations
          - Risk flags
          - Validation issues
          - Agent execution steps
          - Review notes
          - Approve
          - Request Changes
          - Reject

          AI remains advisory.
          Final decision belongs to the Gemologist.
          ====================================================== */}

      <Route
        path="/gemologist/verifications/:id"
        element={
          <RoleProtectedRoute
            allowedRoles={["Gemologist"]}
          >
            <VerificationDetails />
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