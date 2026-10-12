import { ArrowUpRight } from "lucide-react";
import { Link } from "react-router-dom";
import GemCard from "./GemCard";
import { EmptyState, SectionHeading, SkeletonGrid } from "./BuyerUI";
export default function FeaturedGems({ gems, loading }) {
  return <section><SectionHeading eyebrow="WORTH A CLOSER LOOK" title="Fresh from the collection"><Link to="/buyer/marketplace" className="gm-text-link">View all <ArrowUpRight size={18} /></Link></SectionHeading>{loading ? <SkeletonGrid count={3} /> : gems.length === 0 ? <EmptyState title="New discoveries are on their way" description="Available listings will appear here. Explore the marketplace to see the latest collection." /> : <div className="gm-featured-grid">{gems.map(gem => <GemCard key={gem.id} gem={gem} />)}</div>}</section>;
}
