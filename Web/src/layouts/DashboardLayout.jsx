import OrderInbox from "../components/OrderInbox";
import { useState } from "react";
import {
  NavLink,
  useNavigate,
  useLocation,
} from "react-router-dom";

import {
  useAuth,
} from "../context/AuthContext";

import UserAvatar
  from "../components/UserAvatar";

function DashboardLayout({ children }) {
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const shipmentDashboard = pathname === "/admin" || pathname === "/admin/shipments";

  const {
    user,
    logout,
  } = useAuth();

  const [
    profileOpen,
    setProfileOpen,
  ] = useState(false);


  // ============================================================
  // LOGOUT
  // ============================================================

  const handleLogout = () => {
    setProfileOpen(false);

    logout();

    navigate("/login");
  };


  // ============================================================
  // OPEN PROFILE
  // ============================================================

  const handleOpenProfile = () => {
    setProfileOpen(false);

    navigate("/profile");
  };


  // ============================================================
  // ROLE HOME ROUTE
  // ============================================================

  const getHomeRoute = () => {
    switch (user?.role) {
      case "Seller":
        return "/seller";

      case "Buyer":
        return "/buyer";

      case "Gemologist":
        return "/gemologist";

      case "Admin":
        return "/admin";

      case "ExportOfficer":
        return "/export-officer";

      default:
        return "/dashboard";
    }
  };


  // ============================================================
  // ROLE NAVIGATION
  // ============================================================

  const renderRoleNavigation = () => {
    switch (user?.role) {

      // ========================================================
      // SELLER
      // ========================================================

      case "Seller":
        return (
          <>
            <NavLink
              to="/seller"
              end
              className={({ isActive }) =>
                isActive
                  ? "gemora-nav-link active"
                  : "gemora-nav-link"
              }
            >
              Dashboard
            </NavLink>



            <NavLink
              to="/seller/listings"
              className={({ isActive }) =>
                isActive
                  ? "gemora-nav-link active"
                  : "gemora-nav-link"
              }
            >
              My Listings
            </NavLink>

            <NavLink
              to="/seller/listings/create"
              className={({ isActive }) =>
                isActive
                  ? "gemora-nav-link active"
                  : "gemora-nav-link"
              }
            >
              Create Listing
            </NavLink>
            <NavLink
              to="/seller/shipments"
              className={({ isActive }) =>
                isActive ? "gemora-nav-link active" : "gemora-nav-link"
              }
            >
              My Shipments
            </NavLink>
          </>
        );


      // ========================================================
      // GEMOLOGIST
      // ========================================================

      case "Gemologist":
        return (
          <>
            <NavLink
              to="/gemologist"
              end
              className={({ isActive }) =>
                isActive
                  ? "gemora-nav-link active"
                  : "gemora-nav-link"
              }
            >
              Dashboard
            </NavLink>

            <NavLink
              to="/gemologist/verifications"
              className={({ isActive }) =>
                isActive
                  ? "gemora-nav-link active"
                  : "gemora-nav-link"
              }
            >
              Verification Queue
            </NavLink>
          </>
        );


      // ========================================================
      // BUYER
      // ========================================================

      case "Buyer":
        return (
          <>
            <NavLink
              to="/buyer"
              end
              className={({ isActive }) =>
                isActive
                  ? "gemora-nav-link active"
                  : "gemora-nav-link"
              }
            >
              Dashboard
            </NavLink>
          </>
        );


      // ========================================================
      // ADMIN
      // ========================================================

      case "Admin":
        return (
          <>
            <NavLink
              to="/admin/shipments"
              className={({ isActive }) => isActive || pathname === "/admin" ? "gemora-nav-link active" : "gemora-nav-link"}
            >
              Shipment Dashboard
            </NavLink>
            <NavLink
              to="/admin/transactions"
              className={({ isActive }) => isActive ? "gemora-nav-link active" : "gemora-nav-link"}
            >
              Transactions
            </NavLink>
          </>
        );

      case "ExportOfficer":
        return (
          <>
            <NavLink
              to="/export-officer"
              end
              className={({ isActive }) =>
                isActive
                  ? "gemora-nav-link active"
                  : "gemora-nav-link"
              }
            >
              Dashboard
            </NavLink>
          </>
        );


      default:
        return null;
    }
  };


  // ============================================================
  // UI
  // ============================================================

  return (
    <div className={`gemora-app-shell${shipmentDashboard ? " shipment-dashboard-shell" : ""}`}>

      {/* ======================================================
          TOP NAVIGATION
          ====================================================== */}

      <header className="gemora-topbar">

        <div className="gemora-topbar-inner">

          {/* ==================================================
              BRAND
              ================================================== */}

          <button
            type="button"
            className="gemora-brand"
            onClick={() =>
              navigate(
                getHomeRoute()
              )
            }
            aria-label="Gemora home"
          >
            <div className="gemora-brand-symbol">
              <span>
                G
              </span>
            </div>

            <div className="gemora-brand-copy">

              <strong>
                GEMORA
              </strong>

              <small>
                CEYLON GEM MARKETPLACE
              </small>

            </div>
          </button>


          {/* ==================================================
              DESKTOP NAVIGATION
              ================================================== */}

          <nav className="gemora-main-nav">

            {renderRoleNavigation()}

          </nav>


          {/* ==================================================
              USER AREA
              ================================================== */}

          <div className="gemora-user-area">
            {user?.role === "Seller" && <OrderInbox seller key={user?.userId || user?.id || user?.email} />}

            {/* ROLE */}

            <div className="gemora-role-chip">

              <span className="gemora-role-dot" />

              {user?.role || "User"}

            </div>


            {/* PROFILE */}

            <div className="gemora-profile-wrapper">

              <button
                type="button"
                className="gemora-profile-button"
                onClick={() =>
                  setProfileOpen(
                    (previous) =>
                      !previous
                  )
                }
                aria-expanded={
                  profileOpen
                }
                aria-label="Open account menu"
              >

                <UserAvatar
                  user={user}
                  size={37}
                  className="gemora-dashboard-avatar"
                />


                <div className="gemora-profile-copy">

                  <strong>
                    {user?.fullName ||
                      "Gemora User"}
                  </strong>

                  <span>
                    {user?.email || ""}
                  </span>

                </div>


                <span
                  className={`gemora-profile-chevron ${
                    profileOpen
                      ? "open"
                      : ""
                  }`}
                >
                  ▾
                </span>

              </button>


              {/* ==============================================
                  PROFILE DROPDOWN
                  ============================================== */}

              {profileOpen && (

                <div className="gemora-profile-menu">

                  {/* USER DETAILS */}

                  <div className="gemora-profile-menu-header">

                    <UserAvatar
                      user={user}
                      size={45}
                      className="gemora-dashboard-avatar"
                    />

                    <div>

                      <strong>
                        {user?.fullName ||
                          "Gemora User"}
                      </strong>

                      <span>
                        {user?.email || ""}
                      </span>

                    </div>

                  </div>


                  {/* ROLE */}

                  <div className="gemora-profile-menu-role">

                    Signed in as{" "}

                    <strong>
                      {user?.role ||
                        "User"}
                    </strong>

                  </div>


                  {/* MY PROFILE */}

                  <button
                    type="button"
                    className="gemora-profile-menu-action"
                    onClick={
                      handleOpenProfile
                    }
                  >
                    <span>
                      ◇
                    </span>

                    <div>
                      <strong>
                        My Profile
                      </strong>

                      <small>
                        Personal details and photo
                      </small>
                    </div>
                  </button>


                  {/* SIGN OUT */}

                  <button
                    type="button"
                    className="gemora-logout-button"
                    onClick={
                      handleLogout
                    }
                  >
                    <span>
                      ↗
                    </span>

                    Sign Out
                  </button>

                </div>

              )}

            </div>

          </div>

        </div>

      </header>


      {/* ======================================================
          MOBILE NAVIGATION
          ====================================================== */}

      <div className="gemora-mobile-nav">

        {renderRoleNavigation()}

      </div>


      {/* ======================================================
          PAGE CONTENT
          ====================================================== */}

      <main className="gemora-main-content">

        {children}

      </main>

    </div>
  );
}

export default DashboardLayout;

