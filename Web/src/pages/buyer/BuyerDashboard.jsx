import {
  useEffect,
  useState,
} from "react";

import { motion } from "framer-motion";

import BuyerHero from "../../components/buyer/BuyerHero";
import BuyerStats from "../../components/buyer/BuyerStats";
import FeaturedGems from "../../components/buyer/FeaturedGems";
import BuyerQuickActions from "../../components/buyer/BuyerQuickActions";
import BuyerOrdersPreview from "../../components/buyer/BuyerOrdersPreview";
import BuyerProfileSummary from "../../components/buyer/BuyerProfileSummary";

import {
  getBuyerDashboardStats,
  getMarketplaceGems,
  getMyOrders,
} from "../../services/buyerApi";

const reveal = {
  hidden: {
    opacity: 0,
    y: 70,
    scale: 0.96,
  },

  visible: {
    opacity: 1,
    y: 0,
    scale: 1,

    transition: {
      duration: 0.8,
      ease: [0.16, 1, 0.3, 1],
    },
  },
};

export default function BuyerDashboard() {
  const [stats, setStats] =
    useState(null);

  const [gems, setGems] =
    useState([]);

  const [orders, setOrders] =
    useState([]);

  const [loading, setLoading] =
    useState(true);

  useEffect(() => {
    async function loadDashboard() {
      setLoading(true);

      const results =
        await Promise.allSettled([
          getBuyerDashboardStats(),

          getMarketplaceGems({
            page: 1,
            pageSize: 3,
          }),

          getMyOrders(),
        ]);

      const [
        statsResult,
        gemsResult,
        ordersResult,
      ] = results;

      if (
        statsResult.status ===
        "fulfilled"
      ) {
        setStats(
          statsResult.value
        );
      }

      if (
        gemsResult.status ===
        "fulfilled"
      ) {
        setGems(
          gemsResult.value?.items ||
          []
        );
      }

      if (
        ordersResult.status ===
        "fulfilled"
      ) {
        setOrders(
          ordersResult.value || []
        );
      }

      setLoading(false);
    }

    loadDashboard();
  }, []);

  return (
    <div className="buyer-dashboard-page">
      <BuyerHero />

      <motion.section
        variants={reveal}
        initial="hidden"
        whileInView="visible"
        viewport={{
          once: true,
          amount: 0.16,
        }}
      >
        <BuyerStats
          stats={stats}
          loading={loading}
        />
      </motion.section>

      <motion.section
        className="buyer-dashboard-grid"
        variants={reveal}
        initial="hidden"
        whileInView="visible"
        viewport={{
          once: true,
          amount: 0.08,
        }}
      >
        <div className="buyer-left-column">
          <FeaturedGems
            gems={gems}
            loading={loading}
          />

          <BuyerOrdersPreview
            orders={orders}
            loading={loading}
          />
        </div>

        <div className="buyer-right-column">
          <BuyerQuickActions />

          <BuyerProfileSummary />
        </div>
      </motion.section>
    </div>
  );
}