import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";

import DashboardLayout from "../layouts/DashboardLayout";
import gemListingService from "../services/gemVerification/gemListingService";

import sapphireStone from "../assets/blue-sapphire.png";

function SellerDashboard() {
  const navigate = useNavigate();

  const [listings, setListings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");

  /* =========================================================
     LOAD SELLER LISTINGS
     ========================================================= */

  useEffect(() => {
    const loadListings = async () => {
      try {
        setLoading(true);
        setLoadError("");

        const data =
          await gemListingService.getMyListings();

        setListings(
          Array.isArray(data) ? data : []
        );
      } catch (error) {
        console.error(
          "Failed to load seller dashboard listings:",
          error
        );

        setLoadError(
          "We couldn't load your listing statistics."
        );
      } finally {
        setLoading(false);
      }
    };

    loadListings();
  }, []);

  /* =========================================================
     LIVE STATISTICS
     ========================================================= */

  const stats = useMemo(() => {
    return {
      total: listings.length,

      draft: listings.filter(
        (listing) =>
          listing.status === "Draft"
      ).length,

      pending: listings.filter(
        (listing) =>
          listing.status ===
          "PendingVerification"
      ).length,

      approved: listings.filter(
        (listing) =>
          listing.status === "Approved"
      ).length,

      changesRequested: listings.filter(
        (listing) =>
          listing.status ===
          "ChangesRequested"
      ).length,
    };
  }, [listings]);

  return (
    <DashboardLayout>
      <div className="seller-dashboard-page">

        {/* ==================================================
            HERO
            ================================================== */}

        <section className="seller-dashboard-hero">

          {/* HERO CONTENT */}

          <div className="seller-dashboard-hero-content">

            <span className="seller-dashboard-eyebrow">
              GEMORA SELLER PORTAL
            </span>

            <h1>
              Manage your gemstone
              <br />
              business
            </h1>

            <p>
              Create gemstone listings, provide
              verification evidence, and track
              Gemologist review from one place.
            </p>

            <div className="seller-dashboard-hero-actions">

              <button
                type="button"
                className="seller-dashboard-primary-button"
                onClick={() =>
                  navigate(
                    "/seller/listings/create"
                  )
                }
              >
                <span>+</span>
                Create Gem Listing
              </button>

              <button
                type="button"
                className="seller-dashboard-secondary-button"
                onClick={() =>
                  navigate("/seller/listings")
                }
              >
                View My Listings
              </button>

            </div>

          </div>


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