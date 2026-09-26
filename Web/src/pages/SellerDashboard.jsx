import { Link } from "react-router-dom";
import DashboardLayout from "../layouts/DashboardLayout";

function SellerDashboard() {
  return (
    <DashboardLayout>
      <div className="seller-dashboard-page">

        {/* HERO */}
        <section className="seller-dashboard-hero">
          <div>
            <span className="seller-eyebrow">
              GEMORA SELLER PORTAL
            </span>

            <h1>
              Manage your gemstone business
            </h1>

            <p>
              Create gemstone listings, provide
              verification evidence, and track
              Gemologist review from one place.
            </p>

            <div className="seller-hero-actions">
              <Link
                to="/seller/listings/create"
                className="gemora-button-primary"
              >
                + Create Gem Listing
              </Link>

              <Link
                to="/seller/listings"
                className="gemora-button-outline"
              >
                View My Listings
              </Link>
            </div>
          </div>

          <div className="seller-hero-mark">
            <span>G</span>
          </div>
        </section>

        {/* WORKFLOW */}
        <section className="seller-dashboard-section">
          <div className="section-heading">
            <div>
              <span className="section-label">
                VERIFICATION PROCESS
              </span>

              <h2>
                From gemstone to verified listing
              </h2>
            </div>
          </div>

          <div className="seller-workflow-grid">

            <div className="workflow-card">
              <span className="workflow-number">
                01
              </span>

              <div className="workflow-icon">
                ◆
              </div>

              <h3>Create Listing</h3>

              <p>
                Add gemstone characteristics,
                pricing and certificate information.
              </p>
            </div>

            <div className="workflow-card">
              <span className="workflow-number">
                02
              </span>

              <div className="workflow-icon">
                ◈
              </div>

              <h3>Add Evidence</h3>

              <p>
                Upload a clear gemstone photograph
                and supporting certificate.
              </p>
            </div>

            <div className="workflow-card">
              <span className="workflow-number">
                03
              </span>

              <div className="workflow-icon">
                ✦
              </div>

              <h3>AI Analysis</h3>

              <p>
                Submitted evidence can be analyzed
                to assist the Gemologist review.
              </p>
            </div>

            <div className="workflow-card">
              <span className="workflow-number">
                04
              </span>

              <div className="workflow-icon">
                ✓
              </div>

              <h3>Gemologist Review</h3>

              <p>
                A Gemologist makes the final human
                verification decision.
              </p>
            </div>

          </div>
        </section>

        {/* QUICK ACTIONS */}
        <section className="seller-dashboard-section">
          <div className="section-heading">
            <div>
              <span className="section-label">
                QUICK ACTIONS
              </span>

              <h2>
                Seller workspace
              </h2>
            </div>
          </div>

          <div className="seller-action-grid">

            <Link
              to="/seller/listings"
              className="seller-action-card"
            >
              <div className="action-icon">
                ◇
              </div>

              <div>
                <h3>My Gem Listings</h3>

                <p>
                  View drafts, pending reviews and
                  verified gemstone listings.
                </p>
              </div>

              <span className="action-arrow">
                →
              </span>
            </Link>

            <Link
              to="/seller/listings/create"
              className="seller-action-card"
            >
              <div className="action-icon">
                +
              </div>

              <div>
                <h3>Create New Listing</h3>

                <p>
                  Start a new gemstone listing and
                  prepare its verification evidence.
                </p>
              </div>

              <span className="action-arrow">
                →
              </span>
            </Link>

          </div>
        </section>

        {/* IMPORTANT NOTICE */}
        <section className="seller-verification-note">
          <div className="verification-note-icon">
            i
          </div>

          <div>
            <strong>
              Human-reviewed gemstone verification
            </strong>

            <p>
              Gemora AI provides supporting analysis
              only. AI observations do not prove
              gemstone authenticity, treatment,
              origin, value, or certification.
              Final verification decisions are made
              by a Gemologist.
            </p>
          </div>
        </section>

      </div>
    </DashboardLayout>
  );
}

export default SellerDashboard;