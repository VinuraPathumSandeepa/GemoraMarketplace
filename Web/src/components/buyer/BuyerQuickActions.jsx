import { Link } from "react-router-dom";

export default function BuyerQuickActions() {
  return (
    <section className="glass-card section-block">
      <div className="section-head">
        <div>
          <span className="section-mini-title">Quick Access</span>
          <h3>Buyer Actions</h3>
        </div>
      </div>

      <div className="quick-actions-list">
        <Link to="/buyer/marketplace" className="quick-action-item">
          Browse marketplace
        </Link>
        <Link to="/buyer/orders" className="quick-action-item">
          Track my orders
        </Link>
        <Link to="/buyer/profile" className="quick-action-item">
          Update profile
        </Link>
        <button className="quick-action-item disabled-action">
          Ask AI gemstone assistant (coming soon)
        </button>
      </div>
    </section>
  );
}