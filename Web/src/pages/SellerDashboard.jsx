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
<<<<<<< HEAD


          {/* =================================================
              ROTATING BLUE SAPPHIRE
              ================================================= */}

          <div
            className="seller-dashboard-hero-visual"
            aria-hidden="true"
          >
            <div className="seller-dashboard-gem-float">

              <div
                className="
                  seller-dashboard-gem-ring
                  seller-dashboard-gem-ring-one
                "
              />

              <div
                className="
                  seller-dashboard-gem-ring
                  seller-dashboard-gem-ring-two
                "
              />

              <div className="seller-dashboard-gem-glow" />
              <div className="seller-dashboard-gem-rotator">
              <img
                src={sapphireStone}
                alt=""
                className="seller-dashboard-gem"
              />
              </div>

            </div>
          </div>

        </section>


        {/* ==================================================
            LIVE STATISTICS
            ================================================== */}

        <section className="seller-stats-section">

          <div className="seller-section-heading seller-stats-heading">

            <div>
              <span>
                YOUR MARKETPLACE
              </span>

              <h2>
                Listing overview
              </h2>
            </div>


            <button
              type="button"
              className="seller-stats-view-all"
              onClick={() =>
                navigate("/seller/listings")
              }
            >
              View all listings
              <span>→</span>
            </button>

          </div>


          {loadError && (
            <div className="seller-dashboard-error">
              {loadError}
            </div>
          )}


          <div className="seller-stats-grid">

            {/* TOTAL */}

            <article
              className="
                seller-stat-card
                seller-stat-card-total
              "
              style={{
                "--stat-delay": "0ms",
              }}
            >

              <div className="seller-stat-top">

                <div className="seller-stat-icon">
                  ◆
                </div>

                <span className="seller-stat-label">
                  Total Listings
                </span>

              </div>

              <strong className="seller-stat-number">
                {loading
                  ? "—"
                  : stats.total}
              </strong>

              <p>
                All gemstone listings created
                by your seller account.
              </p>

            </article>


            {/* DRAFTS */}

            <article
              className="seller-stat-card"
              style={{
                "--stat-delay": "70ms",
              }}
            >

              <div className="seller-stat-top">

                <div className="seller-stat-icon">
                  ◇
                </div>

                <span className="seller-stat-label">
                  Drafts
                </span>

              </div>

              <strong className="seller-stat-number">
                {loading
                  ? "—"
                  : stats.draft}
              </strong>

              <p>
                Listings still being prepared
                before verification.
              </p>

            </article>


            {/* PENDING */}

            <article
              className="seller-stat-card"
              style={{
                "--stat-delay": "140ms",
              }}
            >

              <div className="seller-stat-top">

                <div className="seller-stat-icon pending">
                  ◌
                </div>

                <span className="seller-stat-label">
                  Pending Review
                </span>

              </div>

              <strong className="seller-stat-number">
                {loading
                  ? "—"
                  : stats.pending}
              </strong>

              <p>
                Listings currently waiting for
                verification review.
              </p>

            </article>


            {/* APPROVED */}

            <article
              className="seller-stat-card"
              style={{
                "--stat-delay": "210ms",
              }}
            >

              <div className="seller-stat-top">

                <div className="seller-stat-icon approved">
                  ✓
                </div>

                <span className="seller-stat-label">
                  Approved
                </span>

              </div>

              <strong className="seller-stat-number">
                {loading
                  ? "—"
                  : stats.approved}
              </strong>

              <p>
                Listings approved through the
                Gemologist review workflow.
              </p>

            </article>

          </div>


          {/* CHANGES REQUESTED ALERT */}

          {!loading &&
            stats.changesRequested > 0 && (
              <button
                type="button"
                className="seller-changes-alert"
                onClick={() =>
                  navigate("/seller/listings")
                }
              >

                <span className="seller-changes-alert-icon">
                  !
                </span>

                <span>

                  <strong>
                    {stats.changesRequested}{" "}
                    {stats.changesRequested === 1
                      ? "listing requires"
                      : "listings require"}{" "}
                    your attention
                  </strong>

                  <small>
                    A Gemologist requested
                    changes. Review the feedback
                    and resubmit the listing.
                  </small>

                </span>

                <span className="seller-changes-arrow">
                  →
                </span>

              </button>
            )}

        </section>


        {/* ==================================================
            VERIFICATION PROCESS
            ================================================== */}

        <section className="seller-dashboard-section">

          <div className="seller-section-heading">

            <span>
              VERIFICATION PROCESS
            </span>

            <h2>
              From gemstone to verified listing
            </h2>

          </div>


          <div className="seller-workflow-grid">

            {/* STEP 1 */}

            <article
              className="seller-workflow-card"
              style={{
                "--card-delay": "0ms",
              }}
            >

              <div className="seller-workflow-card-top">

                <div className="seller-workflow-icon">
                  ◆
                </div>

                <span>01</span>

              </div>

              <h3>
                Create Listing
              </h3>

              <p>
                Add gemstone characteristics,
                pricing, certificate information
                and marketplace details.
              </p>

            </article>


            {/* STEP 2 */}

            <article
              className="seller-workflow-card"
              style={{
                "--card-delay": "70ms",
              }}
            >

              <div className="seller-workflow-card-top">

                <div className="seller-workflow-icon">
                  ◇
                </div>

                <span>02</span>

              </div>

              <h3>
                Add Evidence
              </h3>

              <p>
                Upload a clear gemstone
                photograph and supporting
                certificate evidence.
              </p>

            </article>


            {/* STEP 3 */}

            <article
              className="seller-workflow-card"
              style={{
                "--card-delay": "140ms",
              }}
            >

              <div className="seller-workflow-card-top">

                <div className="seller-workflow-icon">
                  ✦
                </div>

                <span>03</span>

              </div>

              <h3>
                AI Analysis
              </h3>

              <p>
                Submitted evidence may be
                analyzed to assist the
                Gemologist verification
                process.
              </p>

            </article>


            {/* STEP 4 */}

            <article
              className="seller-workflow-card"
              style={{
                "--card-delay": "210ms",
              }}
            >

              <div className="seller-workflow-card-top">

                <div className="seller-workflow-icon">
                  ✓
                </div>

                <span>04</span>

              </div>

              <h3>
                Gemologist Review
              </h3>

              <p>
                A human Gemologist makes the
                final verification decision.
              </p>

            </article>

          </div>

        </section>


        {/* ==================================================
            QUICK ACTIONS
            ================================================== */}

        <section className="seller-dashboard-section">

          <div className="seller-section-heading">

            <span>
              QUICK ACTIONS
            </span>

            <h2>
              Continue your work
            </h2>

          </div>


          <div className="seller-action-grid">
            <button
              type="button"
              className="seller-action-card"
              onClick={() => navigate("/seller/shipments")}
            >
              <div className="seller-action-icon">↗</div>
              <div>
                <h3>My Shipments</h3>
                <p>Create shipments and review shipping plans, tracking, and insurance.</p>
              </div>
              <span className="seller-action-arrow">→</span>
            </button>

            {/* MY LISTINGS */}

            <button
              type="button"
              className="seller-action-card"
              onClick={() =>
                navigate("/seller/listings")
              }
            >

              <div className="seller-action-icon">
                ◇
              </div>

              <div>
                <h3>
                  My Gem Listings
                </h3>

                <p>
                  Review listing status,
                  evidence, and verification
                  progress.
                </p>
              </div>

              <span className="seller-action-arrow">
                →
              </span>

            </button>


            {/* CREATE */}

            <button
              type="button"
              className="seller-action-card"
              onClick={() =>
                navigate(
                  "/seller/listings/create"
                )
              }
            >

              <div className="seller-action-icon">
                +
              </div>

              <div>
                <h3>
                  Create New Listing
                </h3>

                <p>
                  Add a gemstone and prepare
                  its evidence for verification.
                </p>
              </div>

              <span className="seller-action-arrow">
                →
              </span>

            </button>

          </div>

        </section>


        {/* ==================================================
            HUMAN REVIEW NOTICE
            ================================================== */}

        <section className="seller-verification-note">

          <div className="seller-verification-note-icon">
            ✦
          </div>

          <div>

            <span>
              HUMAN-REVIEWED AI
            </span>

            <h3>
              AI assists. Gemologists decide.
            </h3>

            <p>
              Gemora's AI analysis provides
              advisory observations and risk
              indicators. Final verification
              remains with a human Gemologist.
            </p>

          </div>

        </section>

      </div>
    </DashboardLayout>
  );
}

export default SellerDashboard;
=======
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
>>>>>>> origin/dev
