import {
  Routes,
  Route,
  Navigate,
} from "react-router-dom";

// ============================================================
// BUYER AREA
// ============================================================

import BuyerDashboard from "./pages/buyer/BuyerDashboard";
import BuyerLayout from "./layouts/BuyerLayout";
import MarketplacePage from "./pages/buyer/MarketplacePage";
import MyOrdersPage from "./pages/buyer/MyOrdersPage";
import BuyerProfilePage from "./pages/buyer/BuyerProfilePage";
import WishlistPage from "./pages/buyer/WishlistPage";
import GemDetailsPage from "./pages/buyer/GemDetailsPage";
import CheckoutPage from "./pages/buyer/CheckoutPage";
import PaymentPage from "./pages/buyer/PaymentPage";

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
// ADMIN TRANSACTION PAGE
// ============================================================

import TransactionDashboard from "./pages/admin/TransactionDashboard";

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
          BUYER AREA

          All buyer routes are nested under BuyerLayout.

          URLs:
          /buyer/dashboard
          /buyer/marketplace
          /buyer/marketplace/:id
          /buyer/checkout/:gemId
          /buyer/orders
          /buyer/orders/:orderId/payment
          /buyer/profile
          ====================================================== */}

      <Route
        path="/buyer"
        element={
          <RoleProtectedRoute
            allowedRoles={["Buyer"]}
          >
            <BuyerLayout />
          </RoleProtectedRoute>
        }
      >
        {/* ----------------------------------------------------
            DEFAULT BUYER ROUTE
            ---------------------------------------------------- */}

        <Route
          index
          element={
            <Navigate
              to="dashboard"
              replace
            />
          }
        />

        {/* ----------------------------------------------------
            BUYER DASHBOARD
            ---------------------------------------------------- */}

        <Route
          path="dashboard"
          element={<BuyerDashboard />}
        />

        {/* ----------------------------------------------------
            MARKETPLACE
            ---------------------------------------------------- */}

        <Route
          path="marketplace"
          element={<MarketplacePage />}
        />

        {/* ----------------------------------------------------
            GEM DETAILS
            ---------------------------------------------------- */}

        <Route
          path="marketplace/:id"
          element={<GemDetailsPage />}
        />

        {/* ----------------------------------------------------
            CHECKOUT
            ---------------------------------------------------- */}

        <Route
          path="checkout/:gemId"
          element={<CheckoutPage />}
        />

        {/* ----------------------------------------------------
            MY ORDERS
            ---------------------------------------------------- */}

        <Route
          path="orders"
          element={<MyOrdersPage />}
        />

        {/* ----------------------------------------------------
            PAYMENT
            ---------------------------------------------------- */}

        <Route
          path="orders/:orderId/payment"
          element={<PaymentPage />}
        />

        {/* ----------------------------------------------------
            BUYER PROFILE
            ---------------------------------------------------- */}

        <Route
          path="profile"
          element={<BuyerProfilePage />}
        />
        <Route path="wishlist" element={<WishlistPage />} />
      </Route>


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
          ADMIN TRANSACTIONS
          ====================================================== */}

      <Route
        path="/admin/transactions"
        element={
          <RoleProtectedRoute
            allowedRoles={["Admin"]}
          >
            <TransactionDashboard />
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

          Unknown URLs redirect through /dashboard.
          Dashboard.jsx then performs role-based routing.
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
