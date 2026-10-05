import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { motion } from "framer-motion";
import {
  ArrowLeft,
  BadgeCheck,
  Gem,
  MapPin,
  ShieldCheck,
  Store,
  Sparkles,
  Truck,
  WalletCards,
  CalendarDays,
  ScanSearch,
} from "lucide-react";

import {
  resolveMediaUrl,
  getMarketplaceGemById,
} from "../../services/buyerApi";

export default function GemDetailsPage() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [gem, setGem] = useState(null);
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
      availability: gem.status || gem.availabilityStatus || "Available",
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

  if (loading) {
    return (
      <div className="gem-details-page">
        <div className="gem-details-loading glass-card">
          <p>Loading gemstone details...</p>
        </div>
      </div>
    );
  }

  if (error || !normalized) {
    return (
      <div className="gem-details-page">
        <div className="gem-details-error glass-card">
          <h2>Unable to load gemstone</h2>
          <p>{error || "Gem details could not be found."}</p>

          <div className="gem-details-error-actions">
            <button
              className="ghost-btn"
              onClick={() => navigate(-1)}
            >
              Go Back
            </button>

            <Link
              to="/buyer/marketplace"
              className="hero-btn primary"
            >
              Back to Marketplace
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="gem-details-page">
      {/* top breadcrumb */}
      <motion.div
        className="gem-details-breadcrumb"
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.35 }}
      >
        <button
          className="back-inline-btn"
          onClick={() => navigate(-1)}
        >
          <ArrowLeft size={18} />
          Back
        </button>

        <span>/</span>

        <Link to="/buyer/marketplace">Marketplace</Link>

        <span>/</span>

        <strong>{normalized.title}</strong>
      </motion.div>

      {/* hero */}
      <motion.section
        className="gem-details-hero glass-card"
        initial={{ opacity: 0, y: 22 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.45 }}
      >
        <div className="gem-details-hero-grid">
          {/* left image */}
          <div className="gem-visual-panel">
            <div className="gem-main-image-wrap">
              {normalized.imageUrl ? (
                <img
                  src={normalized.imageUrl}
                  alt={normalized.title}
                  className="gem-main-image"
                />
              ) : (
                <div className="gem-main-image-fallback">
                  <Gem size={56} />
                  <p>Image unavailable</p>
                </div>
              )}

              <div className="gem-image-glow"></div>

              <div className="gem-floating-badge verified">
                <BadgeCheck size={16} />
                {normalized.certificationStatus}
              </div>

              <div className="gem-floating-badge availability">
                <Sparkles size={16} />
                {normalized.availability}
              </div>
            </div>
          </div>

          {/* right content */}
          <div className="gem-content-panel">
            <div className="gem-details-topline">
              <span className="section-mini-title">Luxury Marketplace Selection</span>
            </div>

            <h1>{normalized.title}</h1>

            <p className="gem-details-description">
              {normalized.description}
            </p>

            <div className="gem-price-showcase">
              <span className="price-caption">Listed Price</span>
              <strong>{formatPrice(normalized.price, normalized.currency)}</strong>
            </div>

            <div className="gem-details-highlights">
              <div className="detail-highlight-item">
                <Gem size={18} />
                <div>
                  <span>Gem Type</span>
                  <strong>{normalized.type}</strong>
                </div>
              </div>

              <div className="detail-highlight-item">
                <Store size={18} />
                <div>
                  <span>Seller</span>
                  <strong>{normalized.sellerName}</strong>
                </div>
              </div>

              <div className="detail-highlight-item">
                <MapPin size={18} />
                <div>
                  <span>Origin</span>
                  <strong>{normalized.origin}</strong>
                </div>
              </div>

              <div className="detail-highlight-item">
                <CalendarDays size={18} />
                <div>
                  <span>Listed</span>
                  <strong>
                    {normalized.createdAt
                      ? new Date(normalized.createdAt).toLocaleDateString()
                      : "Recently added"}
                  </strong>
                </div>
              </div>
            </div>

            <div className="gem-details-actions">
              {normalized.isAvailable ? (
                <button
                  className="hero-btn primary"
                  onClick={() =>
                    navigate(
                      `/buyer/checkout/${normalized.id}`
                    )
                  }
                >
                  Request Purchase
                </button>
              ) : (
                <div className="gem-unavailable-notice">
                  <ShieldCheck size={18} />
                  This gemstone is currently reserved or purchased.
                </div>
              )}
            </div>

            

            <div className="trust-strip">
              <div>
                <ShieldCheck size={18} />
                <span>Verified marketplace listing</span>
              </div>
              <div>
                <Truck size={18} />
                <span>Secure delivery coordination</span>
              </div>
              <div>
                <WalletCards size={18} />
                <span>Protected transaction flow</span>
              </div>
            </div>
          </div>
        </div>
      </motion.section>

      {/* lower section */}
      <div className="gem-details-lower-grid">
        <motion.section
          className="glass-card gem-specs-card"
          initial={{ opacity: 0, y: 26 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.45, delay: 0.08 }}
        >
          <div className="section-head">
            <div>
              <span className="section-mini-title">Specifications</span>
              <h3>Gemstone Details</h3>
            </div>
          </div>

          <div className="spec-grid">
            <div className="spec-item">
              <span>Carat</span>
              <strong>{normalized.carat}</strong>
            </div>

            <div className="spec-item">
              <span>Color</span>
              <strong>{normalized.color}</strong>
            </div>

            <div className="spec-item">
              <span>Clarity</span>
              <strong>{normalized.clarity}</strong>
            </div>

            <div className="spec-item">
              <span>Cut</span>
              <strong>{normalized.cut}</strong>
            </div>

            <div className="spec-item">
              <span>Shape</span>
              <strong>{normalized.shape}</strong>
            </div>

            <div className="spec-item">
              <span>Treatment</span>
              <strong>{normalized.treatment}</strong>
            </div>
          </div>
        </motion.section>

        <motion.aside
          className="glass-card gem-side-card"
          initial={{ opacity: 0, y: 26 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.45, delay: 0.12 }}
        >
          <div className="section-head">
            <div>
              <span className="section-mini-title">Trust & Seller</span>
              <h3>Marketplace Confidence</h3>
            </div>
          </div>

          <div className="mini-info-stack">
            <div className="mini-info-box">
              <Store size={18} />
              <div>
                <span>Seller Name</span>
                <strong>{normalized.sellerName}</strong>
              </div>
            </div>

            <div className="mini-info-box">
              <MapPin size={18} />
              <div>
                <span>Seller Region</span>
                <strong>{normalized.sellerRegion}</strong>
              </div>
            </div>

            <div className="mini-info-box">
              <BadgeCheck size={18} />
              <div>
                <span>Certification</span>
                <strong>{normalized.certificationStatus}</strong>
              </div>
            </div>

            <div className="mini-info-box">
              <ScanSearch size={18} />
              <div>
                <span>Certificate Ref</span>
                <strong>{normalized.certificateNumber}</strong>
              </div>
            </div>
          </div>
        </motion.aside>
      </div>
    </div>
  );
}