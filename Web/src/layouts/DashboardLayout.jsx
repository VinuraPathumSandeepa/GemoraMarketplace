import { useState } from "react";
import {
  NavLink,
  useNavigate,
} from "react-router-dom";

import { useAuth } from "../context/AuthContext";
import "../styles/DashboardLayout.css";

function DashboardLayout({ children }) {
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  const [profileOpen, setProfileOpen] =
    useState(false);

  const getInitials = () => {
    if (!user?.fullName) {
      return "GU";
    }

    return user.fullName
      .split(" ")
      .filter(Boolean)
      .slice(0, 2)
      .map((name) => name[0])
      .join("")
      .toUpperCase();
  };

  const handleLogout = () => {
    setProfileOpen(false);
    logout();
    navigate("/login");
  };

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

  const renderRoleNavigation = () => {
    switch (user?.role) {
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
          </>
        );

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
          </>
        );

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

      case "Admin":
        return (
          <>
            <NavLink
              to="/admin"
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

  return (
    <div className="gemora-app-shell">

      {/* ======================================================
          TOP NAVIGATION
          ====================================================== */}

      <header className="gemora-topbar">

        <div className="gemora-topbar-inner">

          {/* BRAND */}

          <button
            type="button"
            className="gemora-brand"
            onClick={() =>
              navigate(getHomeRoute())
            }
            aria-label="Gemora home"
          >
            <div className="gemora-brand-symbol">
              <span>G</span>
            </div>

            <div className="gemora-brand-copy">
              <strong>GEMORA</strong>

              <small>
                CEYLON GEM MARKETPLACE
              </small>
            </div>
          </button>


          {/* DESKTOP NAVIGATION */}

          <nav className="gemora-main-nav">
            {renderRoleNavigation()}
          </nav>


          {/* USER AREA */}

          <div className="gemora-user-area">

            <div className="gemora-role-chip">
              <span className="gemora-role-dot" />

              {user?.role || "User"}
            </div>

            <div className="gemora-profile-wrapper">

              <button
                type="button"
                className="gemora-profile-button"
                onClick={() =>
                  setProfileOpen(
                    (previous) => !previous
                  )
                }
                aria-expanded={profileOpen}
              >
                <div className="gemora-avatar">
                  {getInitials()}
                </div>

                <div className="gemora-profile-copy">
                  <strong>
                    {user?.fullName || "Gemora User"}
                  </strong>

                  <span>
                    {user?.email || ""}
                  </span>
                </div>

                <span
                  className={`gemora-profile-chevron ${
                    profileOpen ? "open" : ""
                  }`}
                >
                  ▾
                </span>
              </button>


              {/* PROFILE DROPDOWN */}

              {profileOpen && (
                <div className="gemora-profile-menu">

                  <div className="gemora-profile-menu-header">
                    <div className="gemora-avatar large">
                      {getInitials()}
                    </div>

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

                  <div className="gemora-profile-menu-role">
                    Signed in as{" "}
                    <strong>
                      {user?.role || "User"}
                    </strong>
                  </div>

                  <button
                    type="button"
                    className="gemora-logout-button"
                    onClick={handleLogout}
                  >
                    <span>↗</span>
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