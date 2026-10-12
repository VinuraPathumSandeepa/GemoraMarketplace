import { ArrowUpRight } from "lucide-react";
import { Link } from "react-router-dom";
import sapphireStone from "../../assets/blue-sapphire.png";

export function SapphireVisual() {
  return <div className="seller-dashboard-hero-visual">
    <div className="seller-dashboard-gem-scene" aria-hidden="true">
      <div className="seller-dashboard-gem-float">
        <div className="seller-dashboard-gem-ring seller-dashboard-gem-ring-one" />
        <div className="seller-dashboard-gem-ring seller-dashboard-gem-ring-two" />
        <div className="seller-dashboard-gem-glow" />
        <div className="seller-dashboard-gem-rotator">
          <img src={sapphireStone} alt="" className="seller-dashboard-gem" />
        </div>
      </div>
      <span className="seller-gem-label">CEYLON SAPPHIRE <span>◆</span> GEMORA</span>
    </div>
  </div>;
}

export function SellerSectionHeading({ id, eyebrow, title, description, action }) {
  return <div className="seller-section-heading seller-section-heading-modern">
    <div><span>{eyebrow}</span><h2 id={id}>{title}</h2>{description && <p>{description}</p>}</div>
    {action && <Link to={action.to} className="seller-stats-view-all">{action.label}<ArrowUpRight size={17} /></Link>}
  </div>;
}

export function SellerStatCard({ icon: Icon, label, value, description, loading, tone, index }) {
  return <article className={`seller-stat-card seller-stat-card-modern ${tone ? `seller-stat-card-${tone}` : ""}`} style={{ "--stat-delay": `${index * 70}ms` }}>
    <div className="seller-stat-top"><span className="seller-stat-icon"><Icon size={21} strokeWidth={1.8} /></span><span className="seller-stat-label">{label}</span></div>
    <strong className="seller-stat-number" aria-busy={loading}>{loading ? "—" : value}</strong>
    <p>{description}</p>
  </article>;
}

export function SellerWorkflowCard({ icon: Icon, number, title, description, index }) {
  return <article className="seller-workflow-card seller-workflow-card-modern" style={{ "--card-delay": `${index * 70}ms` }}>
    <div className="seller-workflow-card-top"><span className="seller-workflow-icon"><Icon size={22} strokeWidth={1.7} /></span><span>{number}</span></div>
    <h3>{title}</h3><p>{description}</p>
  </article>;
}

export function SellerActionCard({ icon: Icon, title, description, to }) {
  return <Link className="seller-action-card seller-action-card-modern" to={to}>
    <span className="seller-action-icon"><Icon size={23} strokeWidth={1.7} /></span>
    <span className="seller-action-copy"><strong>{title}</strong><small>{description}</small></span>
    <ArrowUpRight className="seller-action-arrow" size={20} />
  </Link>;
}
