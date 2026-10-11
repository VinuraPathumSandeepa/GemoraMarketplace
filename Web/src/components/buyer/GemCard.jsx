import { useState } from "react";
import { ArrowUpRight, FileCheck2, MapPin, ScanSearch } from "lucide-react";
import { Link } from "react-router-dom";
import { Dialog, GemImage } from "./BuyerUI";
import WishlistButton from "./WishlistButton";

export default function GemCard({ gem }) {
  const [quickView, setQuickView] = useState(false);
  const title = gem.title || gem.gemTitle || "Gemstone";
  const type = gem.gemType || gem.type || "Gemstone";
  const carat = gem.caratWeight ?? gem.carat;
  const source = gem.primaryImageUrl || gem.imageUrl || gem.gemImageUrl;
  const price = `${gem.currency || "LKR"} ${Number(gem.price ?? 0).toLocaleString("en-LK", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  const origin = [gem.region, gem.countryCode === "LK" ? "Sri Lanka" : gem.countryCode].filter(Boolean).join(", ");
  return <article className="gm-gem-card gm-glass">
    <div className="gm-card-visual"><Link to={`/buyer/marketplace/${gem.id}`} aria-label={`View ${title}`}><GemImage src={source} alt={title} /></Link><span className="gm-card-type">{type}</span><button className="gm-quick-view" type="button" aria-label={`Quick view ${title}`} onClick={() => setQuickView(true)}><ScanSearch size={18} /><span>Quick view</span></button></div>
    <div className="gm-gem-content"><div className="gm-card-meta"><span>{carat != null ? `${carat} ct` : "Carat not listed"}</span>{gem.certificateNumber && <span title="Certificate reference supplied"><FileCheck2 size={14} /> Certificate</span>}</div><h3><Link to={`/buyer/marketplace/${gem.id}`}>{title}</Link></h3><p className="gm-card-location"><MapPin size={14} />{origin || "Origin not specified"}</p><div className="gm-card-bottom"><div><small>Listed price</small><strong>{price}</strong></div><Link className="gm-icon-button" to={`/buyer/marketplace/${gem.id}`} aria-label={`View details of ${title}`}><ArrowUpRight size={21} /></Link></div></div>
    <Dialog open={quickView} onClose={() => setQuickView(false)} title={title}><GemImage src={source} alt={title} /><dl className="gm-specs"><div><dt>Gem type</dt><dd>{type}</dd></div><div><dt>Carat weight</dt><dd>{carat ?? "Not listed"}</dd></div><div><dt>Colour</dt><dd>{gem.color || "Not listed"}</dd></div><div><dt>Seller</dt><dd>{gem.sellerName || "Not listed"}</dd></div></dl><p className="gm-price">{price}</p><Link to={`/buyer/marketplace/${gem.id}`} className="gm-button" onClick={() => setQuickView(false)}>Explore gemstone <ArrowUpRight size={18} /></Link></Dialog>
    <WishlistButton gem={gem} />
  </article>;
}
