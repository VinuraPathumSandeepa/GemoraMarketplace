import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { BuyingNotes, Dialog, EmptyState, GemImage, SectionHeading } from "../../components/buyer/BuyerUI";
import { ArrowLeft, ArrowUpRight, CheckCircle2, FileCheck2, MapPin, ScanSearch, Store } from "lucide-react";
import WishlistButton from "../../components/buyer/WishlistButton";

import {
  resolveMediaUrl,
  getMarketplaceGemById,
} from "../../services/buyerApi";

export default function GemDetailsPage() {
  const { id } = useParams();

  const [gem, setGem] = useState(null);
  const [zoom, setZoom] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let mounted = true;

    async function loadGem() {
      try {
        setLoading(true);
        setError("");

        const data = await getMarketplaceGemById(id);

        if (mounted) {
          setGem(data);
        }
      } catch (err) {
        console.error("Failed to load gem details:", err);
        setError(
          err?.response?.data?.message ||
          "Unable to load gemstone details right now."
        );
      } finally {
        if (mounted) {
          setLoading(false);
        }
      }
    }

    if (id) {
      loadGem();
    }

    return () => {
      mounted = false;
    };
  }, [id]);

  const normalized = useMemo(() => {
    if (!gem) return null;

    return {
      id: gem.id ?? gem.gemListingId ?? id,
      title:
        gem.gemTitle ||
        gem.title ||
        gem.name ||
        `${gem.gemType || "Premium"} Gemstone`,
      type: gem.gemType || gem.type || "Gemstone",
      description:
        gem.description ||
        "This verified gemstone is listed on Gemora with authenticated marketplace information, transparent seller details and secure order flow.",
      imageUrl: resolveMediaUrl(
        gem.gemImageUrl ||
        gem.imageUrl ||
        gem.primaryImageUrl ||
        ""
      ),
      price:
        gem.price ??
        gem.listedPrice ??
        gem.askingPrice ??
        gem.agreedPrice ??
        0,
      currency: gem.currency || "LKR",
      carat: gem.carat ?? gem.caratWeight ?? "—",
      color: gem.color || "—",
      clarity: gem.clarity || "—",
      cut: gem.cut || "—",
      shape: gem.shape || "—",
      origin: gem.origin || "Sri Lanka",
      treatment: gem.treatment || "Not specified",
      certificationStatus:
        gem.verificationStatus ||
        gem.certificateStatus ||
        "Verified",
      sellerName: gem.sellerName || gem.seller?.fullName || "Verified Seller",
      sellerRegion: gem.region || gem.sellerRegion || "Sri Lanka",
      isAvailable: gem.isAvailable === true,
      availability: gem.isAvailable === true ? "Available" : "Reserved or Purchased",
      certificateNumber:
        gem.certificateNumber || gem.certificationNumber || "Available on request",
      createdAt: gem.createdAt,
    };
  }, [gem, id]);

  const formatPrice = (value, currency = "LKR") => {
    const amount = Number(value || 0);

    return `${currency} ${amount.toLocaleString("en-LK", {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })}`;
  };

  if (loading) return <EmptyState title="A closer look is on its way" description="Loading the gemstone details…" />;
  if (error || !normalized) return <EmptyState title="This gemstone couldn't be loaded" description={error || "The listing is not available right now."}><Link to="/buyer/marketplace" className="gm-button">Back to the collection</Link></EmptyState>;
  const specs = [["Gem type", normalized.type], ["Carat weight", normalized.carat], ["Colour", normalized.color], ["Clarity", normalized.clarity], ["Cut", normalized.cut], ["Shape", normalized.shape], ["Treatment", normalized.treatment], ["Country / region", [gem.region, gem.countryCode === "LK" ? "Sri Lanka" : gem.countryCode].filter(Boolean).join(", ") || "Not specified"]];
  const certificate = gem.certificateNumber || gem.certificationNumber;
  return <div className="gm-page gm-details-page">
    <nav className="gm-breadcrumb" aria-label="Breadcrumb"><Link to="/buyer/marketplace"><ArrowLeft size={16} />The collection</Link><span>/</span><span aria-current="page">{normalized.title}</span></nav>
    <div className="gm-details-hero"><section className="gm-gallery"><button className="gm-gallery-trigger" onClick={() => setZoom(true)} aria-label={`Enlarge image of ${normalized.title}`}><GemImage src={normalized.imageUrl} alt={normalized.title} /><span><ScanSearch size={18} />Take a closer look</span></button><p>Every stone has its own character. Review the specifications and certificate details before choosing.</p></section>
      <section className="gm-purchase-panel gm-glass"><div className="gm-detail-topline"><span className="gm-eyebrow">THE CEYLON COLLECTION</span><span className="gm-badge">{normalized.isAvailable ? "Available" : "Reserved or purchased"}</span></div><h1>{normalized.title}</h1><div className="gm-chip-list"><span>{normalized.type}</span><span>{normalized.carat} ct</span><span>{normalized.color}</span></div><p className="gm-detail-description">{gem.description || "Explore this gemstone's individual specifications, seller information and certificate reference below."}</p><div className="gm-detail-price"><small>Listed price</small><p className="gm-price">{formatPrice(normalized.price, normalized.currency)}</p><span>Review all details before placing an order request.</span></div>
        {normalized.isAvailable ? <Link className="gm-button gm-full" to={`/buyer/checkout/${normalized.id}`}>Place an order request <ArrowUpRight size={18} /></Link> : <div className="gm-inline-notice"><CheckCircle2 size={20} /><p>This gemstone is currently reserved or purchased.</p></div>}
        <WishlistButton gem={{ ...gem, id: Number(normalized.id), title: normalized.title }} />
        <p className="gm-purchase-note">Seller confirmation comes first. Payment is available after your order is confirmed.</p><div className="gm-seller-mini"><Store size={22} /><div><small>Listed by</small><strong>{normalized.sellerName}</strong></div><a href="#gem-seller" className="gm-icon-button" aria-label="See seller information"><ArrowUpRight size={19} /></a></div>
      </section></div>
    <div className="gm-details-lower"><section className="gm-detail-section gm-glass"><SectionHeading eyebrow="THE FINER DETAILS" title="Gemstone specifications" /><dl className="gm-specs">{specs.map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{value}</dd></div>)}</dl></section><div className="gm-details-aside"><section className="gm-detail-section gm-glass"><SectionHeading eyebrow="KNOW WHAT YOU'RE CHOOSING" title="Certificate details" /><FileCheck2 size={30} strokeWidth={1.3} /><dl className="gm-specs"><div><dt>Certificate reference</dt><dd>{certificate || "Not supplied"}</dd></div><div><dt>Issuing authority</dt><dd>{gem.certificateAuthority || "Not supplied"}</dd></div></dl><p className="gm-detail-footnote">Compare the report reference and its findings with the listing information before making your decision.</p></section><section id="gem-seller" className="gm-detail-section gm-glass"><SectionHeading eyebrow="BEHIND THE LISTING" title="Meet the seller" /><div className="gm-security-item"><Store size={26} /><div><strong>{normalized.sellerName}</strong><p><MapPin size={15} /> {normalized.sellerRegion}</p></div></div><div className="gm-key-value"><span>Listed</span><strong>{normalized.createdAt ? new Date(normalized.createdAt).toLocaleDateString("en-LK") : "Date not supplied"}</strong></div><p>Order updates and seller confirmation appear in My Orders.</p></section></div></div>
    <BuyingNotes /><section className="gm-discover-more gm-glass"><div><span className="gm-eyebrow">THERE'S MORE TO DISCOVER</span><h2>Continue your search for something special.</h2></div><Link className="gm-button" to="/buyer/marketplace">Explore the collection <ArrowUpRight size={18} /></Link></section>
    <div className="gm-mobile-buy"><div><small>Listed price</small><strong>{formatPrice(normalized.price, normalized.currency)}</strong></div>{normalized.isAvailable ? <Link className="gm-button" to={`/buyer/checkout/${normalized.id}`}>Place order <ArrowUpRight size={17} /></Link> : <span className="gm-badge">Unavailable</span>}</div>
    <Dialog open={zoom} onClose={() => setZoom(false)} title={normalized.title} className="gm-zoom-dialog"><GemImage src={normalized.imageUrl} alt={normalized.title} /></Dialog>
  </div>;
}
