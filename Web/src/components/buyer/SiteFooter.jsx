import { Gem, Mail, MapPin, ArrowUpRight } from "lucide-react";
import { Link } from "react-router-dom";

export default function SiteFooter() {
  return <footer className="gm-footer"><div className="gm-footer-inner"><div className="gm-footer-brand"><Link className="gm-brand" to="/buyer/dashboard"><span className="gm-brand-symbol"><Gem size={25} strokeWidth={1.3} /></span><span><strong>GEMORA</strong><small>CEYLON GEM MARKETPLACE</small></span></Link><p>Extraordinary stones.<br />A more considered way to discover them.</p><span className="gm-footer-origin"><MapPin size={16} />Rooted in Sri Lanka</span></div>
    <details open className="gm-footer-links"><summary>Explore Gemora</summary><div><Link to="/buyer/marketplace">The collection <ArrowUpRight size={15} /></Link><Link to="/buyer/orders">Your orders</Link><Link to="/buyer/profile">Your profile</Link><Link to="/buyer/dashboard">Buyer dashboard</Link></div></details>
    <details className="gm-footer-links"><summary>Here to help</summary><div><p>Questions about your Gemora experience?</p><a href="mailto:support@gemora.lk"><Mail size={16} />support@gemora.lk</a><span>Colombo, Sri Lanka</span><span>Mon–Fri · 9:00 AM–5:00 PM</span></div></details></div>
    <div className="gm-footer-bottom"><span>© {new Date().getFullYear()} Gemora. All rights reserved.</span><span>Discover with curiosity. Choose with confidence.</span></div></footer>;
}
