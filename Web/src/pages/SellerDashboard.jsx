import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  AlertCircle,
  ArrowRight,
  CheckCircle2,
  ClipboardList,
  Clock3,
  FilePlus2,
  FileText,
  Gem,
  ShieldCheck,
  Sparkles,
  Truck,
} from "lucide-react";

import DashboardLayout from "../layouts/DashboardLayout";
import {
  SellerActionCard,
  SapphireVisual,
  SellerSectionHeading,
  SellerStatCard,
  SellerWorkflowCard,
} from "../components/seller/SellerDashboardParts";
import gemListingService from "../services/gemVerification/gemListingService";
import "../styles/dashboard-experience.css";

const workflow = [
  { icon: FilePlus2, number: "01", title: "Create a listing", description: "Add characteristics, pricing, and the story behind your stone." },
  { icon: FileText, number: "02", title: "Add evidence", description: "Upload a clear gemstone image and supporting certificate details." },
  { icon: Sparkles, number: "03", title: "AI assistance", description: "Gemora AI provides observations to support professional review." },
  { icon: ShieldCheck, number: "04", title: "Human review", description: "A Gemologist makes the final verification decision." },
];

export default function SellerDashboard() {
  const [listings, setListings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");

  useEffect(() => {
    let active = true;
    gemListingService.getMyListings().then((data) => {
      if (active) setListings(Array.isArray(data) ? data : []);
    }).catch(() => {
      if (active) setLoadError("We couldn't load your listing overview. Please try again shortly.");
    }).finally(() => {
      if (active) setLoading(false);
    });
    return () => { active = false; };
  }, []);

  const stats = useMemo(() => {
    const count = (status) => listings.filter((listing) => listing.status?.toLowerCase() === status).length;
    return {
      total: listings.length,
      draft: count("draft"),
      pending: count("pendingverification"),
      approved: count("approved"),
      changesRequested: count("changesrequested"),
    };
  }, [listings]);

  const statCards = [
    { icon: Gem, label: "Total listings", value: stats.total, description: "Every stone in your inventory", tone: "total" },
    { icon: FileText, label: "Drafts", value: stats.draft, description: "Ready for your finishing touches" },
    { icon: Clock3, label: "Pending review", value: stats.pending, description: "With a Gemologist for review" },
    { icon: CheckCircle2, label: "Approved", value: stats.approved, description: "Ready for the marketplace" },
  ];

  return <DashboardLayout>
    <div className="seller-dashboard-backdrop">
    <div className="seller-dashboard-page seller-dashboard-refined">
      <section className="seller-dashboard-hero" aria-labelledby="seller-dashboard-title">
        <div className="seller-dashboard-hero-content">
          <span className="seller-dashboard-eyebrow"><span className="seller-hero-spark" /> GEMORA SELLER STUDIO</span>
          <h1 id="seller-dashboard-title">Your gems.<br /><em>Your story.</em></h1>
          <p>Prepare exceptional listings, follow verification, and manage your marketplace presence from one considered workspace.</p>
          <div className="seller-dashboard-hero-actions">
            <Link className="seller-dashboard-primary-button" to="/seller/listings/create"><FilePlus2 size={18} /> Create a listing <ArrowRight size={17} /></Link>
            <Link className="seller-dashboard-secondary-button" to="/seller/listings">View my listings <ArrowRight size={17} /></Link>
          </div>
          <div className="seller-hero-footnote"><ShieldCheck size={16} /> Every approved listing receives a human Gemologist review</div>
        </div>
        <SapphireVisual />
      </section>

      <section className="seller-stats-section" aria-labelledby="seller-overview-title">
        <SellerSectionHeading id="seller-overview-title" eyebrow="YOUR MARKETPLACE" title="Listing overview" description="A clear view of your inventory and verification progress." action={{ to: "/seller/listings", label: "View all listings" }} />
        {loadError && <div className="seller-dashboard-error" role="alert">{loadError}</div>}
        <div className="seller-stats-grid">
          {statCards.map((card, index) => <SellerStatCard key={card.label} {...card} loading={loading} index={index} />)}
        </div>
        {!loading && stats.changesRequested > 0 && <Link className="seller-changes-alert" to="/seller/listings">
          <span className="seller-changes-alert-icon"><AlertCircle size={22} /></span>
          <span><strong>{stats.changesRequested} {stats.changesRequested === 1 ? "listing needs" : "listings need"} your attention</strong><small>Review Gemologist feedback and resubmit when ready.</small></span>
          <ArrowRight className="seller-changes-arrow" size={20} />
        </Link>}
      </section>

      <section className="seller-dashboard-section" aria-labelledby="seller-workflow-title">
        <SellerSectionHeading id="seller-workflow-title" eyebrow="A SIMPLE PATH TO MARKET" title="From stone to storefront" description="Each step gives buyers the context they need to choose well." />
        <div className="seller-workflow-grid">
          {workflow.map((step, index) => <SellerWorkflowCard key={step.number} {...step} index={index} />)}
        </div>
      </section>

      <section className="seller-dashboard-section" aria-labelledby="seller-actions-title">
        <SellerSectionHeading id="seller-actions-title" eyebrow="PICK UP WHERE YOU LEFT OFF" title="Your next move" />
        <div className="seller-action-grid">
          <SellerActionCard icon={ClipboardList} title="Manage listings" description="Review status, evidence, and feedback for every gem." to="/seller/listings" />
          <SellerActionCard icon={FilePlus2} title="Add a gemstone" description="Build a new listing and prepare it for verification." to="/seller/listings/create" />
          <SellerActionCard icon={Truck} title="My shipments" description="Create shipments and review shipping plans, tracking, and insurance." to="/seller/shipments" />
        </div>
      </section>

      <section className="seller-verification-note">
        <span className="seller-verification-note-icon"><ShieldCheck size={25} /></span>
        <div><span>CONFIDENCE IN EVERY DETAIL</span><h3>AI assists. Gemologists decide.</h3><p>Gemora AI offers advisory observations. A human Gemologist makes every final verification decision.</p></div>
      </section>
    </div>
    </div>
  </DashboardLayout>;
}
