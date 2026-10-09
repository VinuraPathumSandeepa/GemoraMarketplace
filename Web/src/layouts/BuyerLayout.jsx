import OrderInbox from "../components/OrderInbox";
import { useEffect, useRef, useState } from "react";
import { NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { ArrowUpRight, ChevronDown, Gem, LogOut, Menu, Moon, Package, Sun, UserRound } from "lucide-react";
import { MotionConfig } from "framer-motion";
import { useAuth } from "../context/AuthContext";
import UserAvatar from "../components/UserAvatar";
import SiteFooter from "../components/buyer/SiteFooter";
import MarketplaceAiAssistant from "../components/buyer/MarketplaceAiAssistant";
import { Dialog } from "../components/buyer/BuyerUI";
import "../styles/buyer.css";
import "../styles/buyer-theme.css";
import { WishlistProvider } from "../context/WishlistContext";

const links = [["dashboard", "Dashboard"], ["marketplace", "Marketplace"], ["wishlist", "Wishlist"], ["orders", "My Orders"], ["profile", "Profile"]];

function BuyerLayoutContent() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const [drawer, setDrawer] = useState(false);
  const [account, setAccount] = useState(false);
  const [theme, setTheme] = useState(() => { try { return localStorage.getItem("gemora_buyer_theme") === "dark" ? "dark" : "light"; } catch { return "light"; } });
  const accountRef = useRef(null);
  const accountButton = useRef(null);
  useEffect(() => {
    const close = (e) => { if (!accountRef.current?.contains(e.target)) setAccount(false); };
    const escape = (e) => { if (e.key === "Escape") { setAccount(false); accountButton.current?.focus(); } };
    document.addEventListener("pointerdown", close);
    if (account) document.addEventListener("keydown", escape);
    return () => { document.removeEventListener("pointerdown", close); document.removeEventListener("keydown", escape); };
  }, [account]);
  useEffect(() => { window.scrollTo({ top: 0, behavior: "instant" }); }, [pathname]);
  const toggleTheme = () => { const next = theme === "light" ? "dark" : "light"; setTheme(next); try { localStorage.setItem("gemora_buyer_theme", next); } catch { /* Theme still works without storage. */ } };
  const signOut = () => { setAccount(false); setDrawer(false); logout(); navigate("/login", { replace: true }); };
  const navLinks = (mobile = false) => links.map(([path, label]) => <NavLink key={path} to={`/buyer/${path}`} onClick={() => { setAccount(false); setDrawer(false); }}>{label}{mobile && <ArrowUpRight size={18} />}</NavLink>);
  return <MotionConfig reducedMotion="user" transition={{ duration: .2 }}><div className="gm-buyer" data-theme={theme}>
    <a className="gm-skip" href="#buyer-content">Skip to content</a>
    <header className="gm-topbar"><div className="gm-nav-inner"><NavLink to="/buyer/dashboard" className="gm-brand" aria-label="Gemora buyer dashboard"><span className="gm-brand-symbol"><Gem size={25} strokeWidth={1.3} /></span><span><strong>GEMORA</strong><small>CEYLON GEM MARKETPLACE</small></span></NavLink><nav className="gm-desktop-nav" aria-label="Buyer navigation">{navLinks()}</nav>
      <div className="gm-nav-actions"><OrderInbox key={user?.userId || user?.id || user?.email} /><button className="gm-icon-button" aria-label={`Switch to ${theme === "light" ? "dark" : "light"} mode`} onClick={toggleTheme}>{theme === "light" ? <Moon size={19} /> : <Sun size={19} />}</button><div className="gm-account" ref={accountRef}><button className="gm-account-trigger" ref={accountButton} aria-expanded={account} aria-controls="buyer-account-menu" aria-label="Account options" onClick={() => setAccount(!account)}><UserAvatar user={user} size={36} /><span>{user?.fullName?.split(" ")[0] || "Buyer"}</span><ChevronDown size={15} /></button>{account && <div className="gm-account-menu gm-glass" id="buyer-account-menu"><strong>{user?.fullName || "Gemora buyer"}</strong><small>{user?.email}</small><NavLink to="/buyer/profile" onClick={() => setAccount(false)}><UserRound size={17} />My profile</NavLink><NavLink to="/buyer/orders" onClick={() => setAccount(false)}><Package size={17} />My orders</NavLink><button onClick={signOut}><LogOut size={17} />Sign out</button></div>}</div><button className="gm-icon-button gm-menu-toggle" aria-label="Open navigation" aria-expanded={drawer} onClick={() => setDrawer(true)}><Menu size={23} /></button></div>
    </div></header>
    <Dialog open={drawer} onClose={() => setDrawer(false)} title="Explore Gemora" className="gm-nav-drawer"><p>Your gateway to Ceylon gemstones.</p><nav className="gm-drawer-links" aria-label="Mobile buyer navigation">{navLinks(true)}</nav><button className="gm-button gm-button-secondary" onClick={signOut}><LogOut size={18} />Sign out</button></Dialog>
    <main id="buyer-content" className="gm-main" tabIndex={-1}><Outlet /></main><SiteFooter /><MarketplaceAiAssistant />
  </div></MotionConfig>;
}


// Wrap the entire buyer layout so all buyer-side components share one wishlist.
export default function BuyerLayout() {
  const { user } = useAuth();

  return (
    <WishlistProvider key={user?.userId || user?.id || user?.email || "buyer"}>
      <BuyerLayoutContent />
    </WishlistProvider>
  );
}
