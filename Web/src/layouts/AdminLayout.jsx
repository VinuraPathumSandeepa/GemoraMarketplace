import { useEffect, useRef, useState } from "react";
import { Link, NavLink, useNavigate } from "react-router-dom";
import { ArrowLeftRight, Gem, LayoutDashboard, LogOut, Menu, Truck, UserRound, X } from "lucide-react";
import { useAuth } from "../context/AuthContext";
import "../styles/AdminPortal.css";

const navigation = [
  { to: "/admin", label: "Overview", icon: LayoutDashboard, end: true },
  { to: "/admin/shipments", label: "Shipment Dashboard", icon: Truck },
  { to: "/admin/transactions", label: "Transactions", icon: ArrowLeftRight },
  { to: "/admin/gem-listings", label: "Manage Gem Listings", icon: Gem },
];

function AdminSidebar({ user, onNavigate, onLogout, mobile = false, closeRef }) {
  return (
    <aside className={`admin-sidebar${mobile ? " admin-sidebar-mobile" : " admin-sidebar-desktop"}`} aria-label="Admin sidebar">
      <div className="admin-sidebar-head">
        <Link to="/admin" className="admin-sidebar-brand" onClick={onNavigate} aria-label="Gemora Admin Portal home">
          <span className="admin-sidebar-mark" aria-hidden="true">G</span>
          <span><strong>GEMORA</strong><small>CEYLON GEM MARKETPLACE</small></span>
        </Link>
        {mobile && <button ref={closeRef} type="button" className="admin-sidebar-close" onClick={onNavigate} aria-label="Close navigation"><X size={21} /></button>}
        <span className="admin-sidebar-portal">ADMIN PORTAL</span>
      </div>

      <nav className="admin-sidebar-nav" aria-label="Admin navigation">
        <span className="admin-sidebar-label">MAIN</span>
        {navigation.map(({ to, label, icon: Icon, end }) => (
          <NavLink key={to} to={to} end={end} onClick={onNavigate}
            className={({ isActive }) => `admin-sidebar-link${isActive ? " active" : ""}`}>
            <Icon size={19} strokeWidth={1.8} aria-hidden="true" />
            <span>{label}</span>
          </NavLink>
        ))}
      </nav>

      <div className="admin-sidebar-bottom">
        <div className="admin-sidebar-identity">
          <span className="admin-sidebar-avatar" aria-hidden="true">{(user?.fullName || user?.email || "A").trim().charAt(0).toUpperCase()}</span>
          <span><strong>{user?.fullName || "Administrator"}</strong><small>{user?.email || "Admin account"}</small></span>
        </div>
        <Link className="admin-sidebar-bottom-link" to="/profile" onClick={onNavigate}><UserRound size={18} aria-hidden="true" /> Admin profile</Link>
        <button className="admin-sidebar-bottom-link" type="button" onClick={onLogout}><LogOut size={18} aria-hidden="true" /> Log out</button>
      </div>
    </aside>
  );
}

export default function AdminLayout({ children, title = "Overview" }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [mobileOpen, setMobileOpen] = useState(false);
  const closeRef = useRef(null);
  const drawerRef = useRef(null);

  useEffect(() => {
    if (!mobileOpen) return undefined;
    closeRef.current?.focus();
    const onKeyDown = (event) => {
      if (event.key === "Escape") setMobileOpen(false);
      if (event.key !== "Tab") return;
      const focusable = drawerRef.current?.querySelectorAll('a[href], button:not([disabled])');
      if (!focusable?.length) return;
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [mobileOpen]);

  const handleLogout = () => {
    setMobileOpen(false);
    logout();
    navigate("/login");
  };

  return (
    <div className="admin-portal">
      <AdminSidebar user={user} onLogout={handleLogout} />
      {mobileOpen && (
        <div className="admin-mobile-layer">
          <button type="button" className="admin-mobile-backdrop" onClick={() => setMobileOpen(false)} aria-label="Close navigation" />
          <div ref={drawerRef} role="dialog" aria-modal="true" aria-label="Admin navigation menu">
            <AdminSidebar mobile user={user} closeRef={closeRef} onNavigate={() => setMobileOpen(false)} onLogout={handleLogout} />
          </div>
        </div>
      )}
      <div className="admin-portal-workspace" inert={mobileOpen}>
        <header className="admin-portal-topbar">
          <button type="button" className="admin-menu-toggle" onClick={() => setMobileOpen(true)} aria-label="Open navigation"><Menu size={22} /></button>
          <div className="admin-portal-crumb"><span>Administration</span><span aria-hidden="true">/</span><strong>{title}</strong></div>
          <div className="admin-portal-topbar-user"><span className="admin-portal-user-dot" aria-hidden="true" />{user?.fullName || "Administrator"}</div>
        </header>
        <main className="admin-portal-content" id="admin-main-content">{children}</main>
      </div>
    </div>
  );
}
