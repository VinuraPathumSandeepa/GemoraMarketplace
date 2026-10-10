import {
  useEffect,
  useState,
} from "react";

import { BuyerGuide } from "../../components/buyer/BuyerUI";
import { useAuth } from "../../context/AuthContext";

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


export default function BuyerDashboard() {
  const { user } = useAuth();
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

  return <div className="gm-page gm-dashboard"><div className="gm-welcome"><span>YOUR GEMORA</span><p>Welcome back, {user?.fullName?.split(" ")[0] || "collector"}.</p></div><BuyerHero /><BuyerStats stats={stats} loading={loading} /><div className="gm-dashboard-grid"><div className="gm-dashboard-main"><FeaturedGems gems={gems} loading={loading} /><BuyerOrdersPreview orders={orders} loading={loading} /></div><aside className="gm-dashboard-side"><BuyerQuickActions /><BuyerProfileSummary /></aside></div><BuyerGuide /></div>;
}
