import { ArrowUpRight, Gem, Package, Sparkles, UserRound } from "lucide-react";
import { Link } from "react-router-dom";
import { SectionHeading } from "./BuyerUI";
export default function BuyerQuickActions() {
  return <section className="gm-quick-actions gm-glass"><SectionHeading eyebrow="YOUR NEXT STEP" title="Make yourself at home" /><div className="gm-action-grid">{[[Gem, "/buyer/marketplace", "Find a gemstone"], [Package, "/buyer/orders", "Track my orders"], [UserRound, "/buyer/profile", "My profile"]].map(([Icon, to, label]) => <Link key={to} to={to}><Icon size={22} strokeWidth={1.5} /><span>{label}</span><ArrowUpRight size={16} /></Link>)}<button disabled><Sparkles size={22} strokeWidth={1.5} /><span>Personal gem advisor<small>Coming soon</small></span></button></div><p>For questions about the collection, open the Gemora AI assistant.</p></section>;
}
