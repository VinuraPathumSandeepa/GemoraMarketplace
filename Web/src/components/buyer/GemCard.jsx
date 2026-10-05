import { motion }
  from "framer-motion";

import { Link }
  from "react-router-dom";

import {
  ArrowUpRight,
  BadgeCheck,
} from "lucide-react";

import {
  resolveMediaUrl,
} from "../../services/buyerApi";

export default function GemCard({ gem }) {
  return (
    <motion.div
      className="gem-card"
      whileHover={{ y: -8, rotateX: 2, rotateY: -2 }}
      transition={{ duration: 0.25 }}
    >
      <div className="gem-card-image-wrap">
        {gem.imageUrl ? (
          <img
            src={gem.imageUrl}
            alt={gem.title}
            className="gem-card-image"
          />
        ) : (
          <div className="gem-main-image-fallback">
            <p>Image unavailable</p>
          </div>
        )}
      </div>

      <div className="gem-card-content">
        <span className="gem-type">{gem.type}</span>
        <h4>{gem.title}</h4>
        <p>{gem.priceText}</p>

        <Link
          to={`/buyer/marketplace/${gem.id}`}
          className="primary-small-btn"
        >
          View Details
        </Link>
      </div>
    </motion.div>
  );
}