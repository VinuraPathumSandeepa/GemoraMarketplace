import { ArrowUpRight, Package } from "lucide-react";
import { Link } from "react-router-dom";
import { SectionHeading } from "./BuyerUI";
export default function BuyerOrdersPreview({ orders, loading }) {
  const latest = [...(orders || [])].slice(0, 3);
  return <section className="gm-recent-orders gm-glass"><SectionHeading eyebrow="YOUR COLLECTION IN THE MAKING" title="Recent orders"><Link to="/buyer/orders" className="gm-text-link">All orders <ArrowUpRight size={17} /></Link></SectionHeading>{loading ? <p role="status">Loading your orders…</p> : latest.length ? <div className="gm-order-preview-list">{latest.map(order => <Link to="/buyer/orders" key={order.id}><span className="gm-preview-icon"><Package size={22} strokeWidth={1.5} /></span><span><strong>{order.gemTitle || "Gemstone purchase"}</strong><small>#{order.orderNumber}</small></span><span className="gm-badge">{order.status}</span><ArrowUpRight size={18} /></Link>)}</div> : <div className="gm-inline-empty"><Package size={28} strokeWidth={1.3} /><p>Your gemstone journey starts here. Your purchases will appear in this space.</p><Link className="gm-text-link" to="/buyer/marketplace">Discover the collection <ArrowUpRight size={17} /></Link></div>}</section>;
}
