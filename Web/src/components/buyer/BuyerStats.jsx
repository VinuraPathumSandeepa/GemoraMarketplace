import {
  BadgeDollarSign,
  Gem,
  ShieldCheck,
  Users,
} from "lucide-react";

import BuyerStatCard
  from "./BuyerStatCard";

export default function BuyerStats({
  stats,
  loading,
}) {
  const data = [
    {
      title: "Authorized Sellers",
      value:
        stats?.authorizedSellers ?? 0,
      icon:
        <ShieldCheck size={22} />,
      subtitle:
        "Verified marketplace sellers",
    },

    {
      title: "Registered Buyers",
      value:
        stats?.registeredBuyers ?? 0,
      icon:
        <Users size={22} />,
      subtitle:
        "Gemora buyer community",
    },

    {
      title: "Available Gems",
      value:
        stats?.activeGemListings ?? 0,
      icon:
        <Gem size={22} />,
      subtitle:
        "Verified gems available now",
    },

    {
      title: "Successful Transactions",
      value:
        stats?.successfulTransactions ?? 0,
      icon:
        <BadgeDollarSign size={22} />,
      subtitle:
        "Completed marketplace purchases",
    },
  ];

  return (
    <div className="buyer-stats-grid">
      {data.map((item, index) => (
        <BuyerStatCard
          key={item.title}
          {...item}
          loading={loading}
          index={index}
        />
      ))}
    </div>
  );
}