export default function BuyerStatCard({ title, value, icon, subtitle, loading }) {
  return <article className="gm-stat gm-glass"><span className="gm-stat-icon">{icon}</span><div><strong aria-busy={loading}>{loading ? "—" : value}</strong><h3>{title}</h3><p>{subtitle}</p></div></article>;
}
