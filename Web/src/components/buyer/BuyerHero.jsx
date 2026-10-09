import { ArrowRight, Gem, ShieldCheck } from "lucide-react";
import { Link } from "react-router-dom";

export default function BuyerHero() {
  return <section className="gm-dashboard-hero"><div className="gm-hero-copy"><span className="gm-eyebrow">FROM THE HEART OF CEYLON</span><h1>Extraordinary gems.<br /><em>Meaningful discoveries.</em></h1><p>A place to discover Sri Lankan gemstones, understand their story, and find a stone to call your own.</p><div className="gm-hero-actions"><Link className="gm-button" to="/buyer/marketplace">Explore the collection <ArrowRight size={18} /></Link><Link className="gm-hero-secondary" to="/buyer/orders">View my orders</Link></div><div className="gm-hero-trust"><span><Gem size={16} />Ceylon gemstones</span><span><ShieldCheck size={16} />Details before decisions</span></div></div><div className="gm-hero-art"><img src="/images/gems/hero-poster.webp" alt="Sapphire, ruby and emerald gemstones" loading="lazy" width="1920" height="1080" /><div className="gm-hero-caption"><span>THE GEMORA EDIT</span><p>Nature's finest.<br />Yours to discover.</p></div></div></section>;
}
