import { Link } from "react-router-dom";

function Home() {
  return (
    <div className="gemora-home">

      {/* ======================================================
          NAVIGATION
          ====================================================== */}

      <header className="home-navbar">
        <div className="home-nav-inner">

          <Link to="/" className="home-logo">
            <span className="home-logo-mark">
              ◆
            </span>

            <div>
              <strong>Gemora</strong>

              <small>
                Intelligent Gem Marketplace
              </small>
            </div>
          </Link>


          <nav className="home-nav-links">
            <a href="#gem-journey">
              Features
            </a>

            <a href="#how-it-works">
              How It Works
            </a>

            <a href="#trust">
              Trust & Verification
            </a>

            <a href="#gemstones">
              Gemstones
            </a>
          </nav>


          <div className="home-nav-actions">
            <Link
              to="/login"
              className="home-login-link"
            >
              Sign In
            </Link>

            <Link
              to="/register"
              className="home-signup-button"
            >
              Create Account
            </Link>
          </div>

        </div>
      </header>


      <main>

        {/* ======================================================
            CINEMATIC HERO
            ====================================================== */}

        <section className="home-hero home-hero-cinematic">

          {/* VIDEO BACKGROUND */}

          <div className="home-video-layer">

            <video
              className="home-hero-video"
              autoPlay
              muted
              loop
              playsInline
              preload="auto"
              poster="/images/gems/hero-poster.webp"
            >
              <source
                src="/videos/gemora-hero.mp4"
                type="video/mp4"
              />

              Your browser does not support
              background video.
            </video>


            {/* Dark overlay for text readability */}

            <div className="home-video-overlay" />


            {/* Cinematic vignette */}

            <div className="home-video-vignette" />

          </div>


          {/* SUBTLE AMBIENT LIGHT */}

          <div
            className="
              home-gem-ambient
              home-gem-ambient-one
            "
          />

          <div
            className="
              home-gem-ambient
              home-gem-ambient-two
            "
          />


          {/* HERO CONTENT */}

          <div className="home-hero-inner">

            <div className="home-hero-content">

              <div className="home-eyebrow">
                <span />

                SRI LANKA'S INTELLIGENT
                GEMSTONE MARKETPLACE
              </div>


              <h1>
                Rare gems.

                <br />

                <span>
                  Trusted intelligence.
                </span>

                <br />

                Confident trade.
              </h1>


              <p className="home-hero-description">
                Discover gemstones, submit evidence,
                receive AI-assisted analysis and
                complete professional human
                verification through one secure
                digital marketplace.
              </p>


              {/* HERO BUTTONS */}

              <div className="home-hero-actions">

                <Link
                  to="/register"
                  className="home-primary-button"
                >
                  Enter Gemora

                  <span>
                    →
                  </span>
                </Link>


                <a
                  href="#gem-journey"
                  className="home-secondary-button"
                >
                  Discover the Platform
                </a>

              </div>


              {/* TRUST INDICATORS */}

              <div className="home-trust-row">

                <div>
                  <strong>
                    AI-Assisted
                  </strong>

                  <span>
                    Gem Analysis
                  </span>
                </div>


                <div className="home-trust-divider" />


                <div>
                  <strong>
                    Human Reviewed
                  </strong>

                  <span>
                    Gemologist Decision
                  </span>
                </div>


                <div className="home-trust-divider" />


                <div>
                  <strong>
                    Traceable
                  </strong>

                  <span>
                    Evidence Workflow
                  </span>
                </div>

              </div>

            </div>

          </div>


          {/* SCROLL INDICATOR */}

          <a
            href="#gem-journey"
            className="home-scroll-indicator"
          >
            <span>
              Explore
            </span>

            <div className="scroll-line">
              <div />
            </div>
          </a>

        </section>


        {/* ======================================================
            FEATURES
            ====================================================== */}

        <section
          id="gem-journey"
          className="
            home-section
            home-features-section
          "
        >

          <div className="home-section-heading">

            <span className="home-section-label">
              THE GEMORA ECOSYSTEM
            </span>

            <h2>
              More than a gemstone marketplace.
            </h2>

            <p>
              Gemora brings gemstone listing,
              intelligent verification,
              marketplace transactions,
              secure shipping and export workflows
              together in one connected platform.
            </p>

          </div>


          <div className="home-feature-grid">

            {/* FEATURE 1 */}

            <article className="home-feature-card">

              <div className="feature-number">
                01
              </div>

              <div className="feature-icon">
                ◆
              </div>

              <h3>
                Gem Listing &
                Verification
              </h3>

              <p>
                Sellers create detailed gemstone
                listings, upload images and
                certificate evidence, and submit
                them for professional review.
              </p>

              <span className="feature-tag">
                Seller + Gemologist
              </span>

            </article>


            {/* FEATURE 2 */}

            <article className="home-feature-card">

              <div className="feature-number">
                02
              </div>

              <div className="feature-icon">
                AI
              </div>

              <h3>
                AI-Assisted
                Gem Analysis
              </h3>

              <p>
                Gemora AI analyses gemstone images
                and submitted evidence to generate
                structured observations that assist
                the verification process.
              </p>

              <span className="feature-tag">
                Human-in-the-loop AI
              </span>

            </article>


            {/* FEATURE 3 */}

            <article className="home-feature-card">

              <div className="feature-number">
                03
              </div>

              <div className="feature-icon">
                ↗
              </div>

              <h3>
                Secure Shipping &
                Insurance
              </h3>

              <p>
                Shipping and insurance workflows
                help manage delivery planning,
                risk information and protection
                for valuable gemstones.
              </p>

              <span className="feature-tag">
                Protected Delivery
              </span>

            </article>


            {/* FEATURE 4 */}

            <article className="home-feature-card">

              <div className="feature-number">
                04
              </div>

              <div className="feature-icon">
                ✓
              </div>

              <h3>
                Export &
                Compliance
              </h3>

              <p>
                Gemora supports export
                documentation and compliance
                review before gemstones continue
                through international workflows.
              </p>

              <span className="feature-tag">
                Compliance Workflow
              </span>

            </article>

          </div>

        </section>


        {/* ======================================================
            HOW GEMORA WORKS
            ====================================================== */}

        <section
          id="how-it-works"
          className="
            home-section
            home-process-section
          "
        >

          <div className="home-process-wrapper">

            <div className="home-process-heading">

              <span className="home-section-label">
                HOW GEMORA WORKS
              </span>

              <h2>
                From gemstone to trusted
                marketplace listing.
              </h2>

              <p>
                Every stage focuses on evidence,
                traceability, intelligent assistance
                and professional human review.
              </p>


              <Link
                to="/register"
                className="home-outline-button"
              >
                Create your account →
              </Link>

            </div>


            <div className="home-process-list">

              {/* STEP 1 */}

              <div className="home-process-item">

                <div className="process-step">
                  01
                </div>

                <div>
                  <h3>
                    Seller creates a gemstone listing
                  </h3>

                  <p>
                    The seller enters gemstone
                    information and uploads
                    photographs and certificate
                    evidence.
                  </p>
                </div>

              </div>


              {/* STEP 2 */}

              <div className="home-process-item">

                <div className="process-step">
                  02
                </div>

                <div>
                  <h3>
                    AI analyses the evidence
                  </h3>

                  <p>
                    Gemora's AI verification agent
                    reviews submitted gemstone
                    imagery and supporting evidence
                    and produces structured findings.
                  </p>
                </div>

              </div>


              {/* STEP 3 */}

              <div className="home-process-item">

                <div className="process-step">
                  03
                </div>

                <div>
                  <h3>
                    Gemologist performs human review
                  </h3>

                  <p>
                    A gemologist evaluates the
                    listing, certificate evidence
                    and AI findings before making
                    the final verification decision.
                  </p>
                </div>

              </div>


              {/* STEP 4 */}

              <div className="home-process-item">

                <div className="process-step">
                  04
                </div>

                <div>
                  <h3>
                    Verified marketplace journey
                  </h3>

                  <p>
                    Approved gemstones can continue
                    through buyer discovery,
                    marketplace transactions,
                    shipping, insurance and export
                    workflows.
                  </p>
                </div>

              </div>

            </div>

          </div>

        </section>


        {/* ======================================================
            TRUST & VERIFICATION
            ====================================================== */}

        <section
          id="trust"
          className="
            home-section
            home-trust-section
          "
        >

          <div className="home-trust-panel">

            {/* LEFT */}

            <div className="home-trust-copy">

              <span className="home-section-label">
                TRUST THROUGH TECHNOLOGY
              </span>

              <h2>
                AI assists.
                <br />
                Humans decide.
              </h2>


              <p>
                Gemora uses artificial intelligence
                to support professional judgment,
                not replace it. Final gemstone
                verification remains under human
                review.
              </p>


              <div className="home-trust-points">

                <span>
                  ✓ Evidence-based workflow
                </span>

                <span>
                  ✓ Human approval
                </span>

                <span>
                  ✓ Verification history
                </span>

                <span>
                  ✓ Secure account access
                </span>

              </div>

            </div>


            {/* RIGHT */}

            <div className="home-trust-visual">

              <div className="trust-orbit">

                <div className="trust-center">

                  <span>
                    ◆
                  </span>

                  <strong>
                    GEMORA
                  </strong>

                </div>


                <div
                  className="
                    trust-node
                    trust-node-ai
                  "
                >
                  AI
                </div>


                <div
                  className="
                    trust-node
                    trust-node-human
                  "
                >
                  Human
                </div>


                <div
                  className="
                    trust-node
                    trust-node-secure
                  "
                >
                  Secure
                </div>

              </div>

            </div>

          </div>

        </section>


        {/* ======================================================
            GEMSTONE SHOWCASE
            ====================================================== */}

        <section
          id="gemstones"
          className="
            home-section
            home-gem-showcase-section
          "
        >

          <div className="home-section-heading">

            <span className="home-section-label">
              PREMIUM GEMSTONES
            </span>

            <h2>
              Designed around valuable gemstones.
            </h2>

            <p>
              Gemora combines premium gemstone
              imagery, supporting evidence,
              AI-assisted analysis and human
              verification in one digital
              experience.
            </p>

          </div>


          <div className="home-gem-showcase-grid">

            {/* SAPPHIRE */}

            <article className="home-gem-showcase-card">

              <div className="home-gem-image-wrap">

                <img
                  src="/images/gems/sapphire.webp"
                  alt="Blue sapphire gemstone"
                  loading="lazy"
                />

              </div>


              <div className="home-gem-showcase-copy">

                <span>
                  CEYLON GEMSTONE
                </span>

                <h3>
                  Blue Sapphire
                </h3>

                <p>
                  Detailed gemstone evidence can
                  be submitted for AI-assisted
                  analysis and professional
                  gemologist verification.
                </p>

              </div>

            </article>


            {/* RUBY */}

            <article className="home-gem-showcase-card">

              <div className="home-gem-image-wrap">

                <img
                  src="/images/gems/ruby.webp"
                  alt="Ruby gemstone"
                  loading="lazy"
                />

              </div>


              <div className="home-gem-showcase-copy">

                <span>
                  PRECIOUS GEMSTONE
                </span>

                <h3>
                  Ruby
                </h3>

                <p>
                  Listings can combine visual
                  evidence, certification details
                  and verification history to
                  support trusted marketplace use.
                </p>

              </div>

            </article>


            {/* EMERALD */}

            <article className="home-gem-showcase-card">

              <div className="home-gem-image-wrap">

                <img
                  src="/images/gems/emerald.webp"
                  alt="Emerald gemstone"
                  loading="lazy"
                />

              </div>


              <div className="home-gem-showcase-copy">

                <span>
                  PRECIOUS GEMSTONE
                </span>

                <h3>
                  Emerald
                </h3>

                <p>
                  Evidence-based workflows help
                  buyers and professionals
                  understand gemstone verification
                  status and supporting information.
                </p>

              </div>

            </article>

          </div>

        </section>


        {/* ======================================================
            FINAL CTA
            ====================================================== */}

        <section className="home-cta-section">

          <div className="home-cta-panel">

            <span>
              START YOUR GEMORA JOURNEY
            </span>

            <h2>
              Enter the intelligent
              gemstone marketplace.
            </h2>

            <p>
              Create a Buyer or Seller account,
              verify your email and begin your
              Gemora marketplace journey.
            </p>


            <div className="home-cta-buttons">

              <Link
                to="/register"
                className="home-primary-button"
              >
                Create Account

                <span>
                  →
                </span>
              </Link>


              <Link
                to="/login"
                className="home-cta-login"
              >
                Already registered? Sign In
              </Link>

            </div>

          </div>

        </section>

      </main>


      {/* ======================================================
          FOOTER
          ====================================================== */}

      <footer className="home-footer">

        <div className="home-footer-inner">

          <div className="home-footer-brand">

            <strong>
              ◆ Gemora
            </strong>

            <span>
              Intelligent Gemstone Marketplace
            </span>

          </div>


          <p>
            AI-assisted analysis with
            human verification.
          </p>

        </div>

      </footer>

    </div>
  );
}

export default Home;