import { Link }
  from "react-router-dom";

import GemCard from "./GemCard";

export default function FeaturedGems({
  gems,
  loading,
}) {
  return (
    <section className="section-block liquid-card">
      <div className="section-head">
        <div>
          <span className="section-mini-title">
            Live Marketplace
          </span>

          <h3>
            Available Verified Gems
          </h3>
        </div>

        <Link
          to="/buyer/marketplace"
          className="ghost-btn"
        >
          View All
        </Link>
      </div>

      {loading ? (
        <div className="buyer-loading-block">
          Loading marketplace...
        </div>
      ) : gems.length === 0 ? (
        <div className="buyer-empty-state">
          <h4>
            No verified gems available
            right now.
          </h4>

          <p>
            Approved listings will appear
            here automatically.
          </p>
        </div>
      ) : (
        <div className="gems-grid">
          {gems.map((gem) => (
            <GemCard
              key={gem.id}
              gem={gem}
            />
          ))}
        </div>
      )}
    </section>
  );
}