import {
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  Link,
} from "react-router-dom";

import DashboardLayout from "../layouts/DashboardLayout";
import api from "../services/api";


// ============================================================
// HELPERS
// ============================================================

const apiUrl =
  import.meta.env.VITE_API_URL ||
  "http://localhost:5198/api";

const apiOrigin =
  apiUrl.replace(/\/api\/?$/, "");


function resolveMediaUrl(url) {
  if (!url) {
    return null;
  }

  if (
    url.startsWith("http://") ||
    url.startsWith("https://")
  ) {
    return url;
  }

  return `${apiOrigin}${
    url.startsWith("/")
      ? ""
      : "/"
  }${url}`;
}


function formatDate(value) {
  if (!value) {
    return "Unknown";
  }

  const date =
    new Date(value);

  if (
    Number.isNaN(
      date.getTime()
    )
  ) {
    return "Unknown";
  }

  return date.toLocaleString(
    undefined,
    {
      month: "short",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
    }
  );
}


function getAiStatusLabel(status) {
  const value =
    (
      status ||
      "NotStarted"
    )
      .replace(/\s/g, "")
      .toLowerCase();

  if (value === "completed") {
    return "AI Completed";
  }

  if (value === "processing") {
    return "AI Processing";
  }

  if (
    value ===
    "needsmoreevidence"
  ) {
    return "Needs Evidence";
  }

  if (value === "failed") {
    return "AI Failed";
  }

  return "AI Not Started";
}


function getAiStatusClass(status) {
  const value =
    (
      status ||
      "NotStarted"
    )
      .replace(/\s/g, "")
      .toLowerCase();

  if (value === "completed") {
    return "completed";
  }

  if (value === "processing") {
    return "processing";
  }

  if (
    value ===
    "needsmoreevidence"
  ) {
    return "warning";
  }

  if (value === "failed") {
    return "failed";
  }

  return "not-started";
}


// ============================================================
// GEMOLOGIST DASHBOARD
// ============================================================

function GemologistDashboard() {
  const [
    verifications,
    setVerifications,
  ] = useState([]);

  const [
    loading,
    setLoading,
  ] = useState(true);

  const [
    error,
    setError,
  ] = useState("");


  // ==========================================================
  // LOAD PENDING VERIFICATIONS
  // ==========================================================

  const loadDashboard =
    async () => {

      try {
        setLoading(true);
        setError("");

        const response =
          await api.get(
            "/GemVerifications/pending"
          );

        setVerifications(
          Array.isArray(
            response.data
          )
            ? response.data
            : []
        );

      } catch (error) {
        console.error(
          "Failed to load Gemologist dashboard:",
          error
        );

        setError(
          error.response?.data?.message ||
            "We couldn't load the verification workload. Please try again."
        );

      } finally {
        setLoading(false);
      }
    };


  useEffect(() => {
    loadDashboard();
  }, []);


  // ==========================================================
  // LIVE STATISTICS
  // ==========================================================

  const stats =
    useMemo(() => {

      const pending =
        verifications.length;


      const aiCompleted =
        verifications.filter(
          (item) =>
            (
              item.aiStatus ||
              ""
            ).toLowerCase() ===
            "completed"
        ).length;


      const aiNotStarted =
        verifications.filter(
          (item) => {

            const status =
              (
                item.aiStatus ||
                ""
              )
                .replace(/\s/g, "")
                .toLowerCase();

            return (
              !status ||
              status ===
                "notstarted"
            );
          }
        ).length;


      const attentionRequired =
        verifications.filter(
          (item) => {

            const status =
              (
                item.aiStatus ||
                ""
              )
                .replace(/\s/g, "")
                .toLowerCase();


            const missingImage =
              !item.primaryImageUrl;


            const missingCertificate =
              !item.certificateUrl;


            const aiProblem =
              status ===
                "needsmoreevidence" ||
              status ===
                "failed";


            return (
              missingImage ||
              missingCertificate ||
              aiProblem
            );
          }
        ).length;


      return {
        pending,
        aiCompleted,
        aiNotStarted,
        attentionRequired,
      };
    }, [
      verifications,
    ]);


  // ==========================================================
  // RECENT PENDING ITEMS
  // ==========================================================

  const recentVerifications =
    useMemo(() => {

      return [
        ...verifications,
      ]
        .sort(
          (
            first,
            second
          ) =>
            new Date(
              second.createdAt
            ) -
            new Date(
              first.createdAt
            )
        )
        .slice(
          0,
          4
        );

    }, [
      verifications,
    ]);


  // ==========================================================
  // ATTENTION ITEMS
  // ==========================================================

  const attentionItems =
    useMemo(() => {

      return verifications
        .filter(
          (item) => {

            const status =
              (
                item.aiStatus ||
                ""
              )
                .replace(/\s/g, "")
                .toLowerCase();


            return (
              !item.primaryImageUrl ||
              !item.certificateUrl ||
              status ===
                "needsmoreevidence" ||
              status ===
                "failed"
            );
          }
        )
        .slice(
          0,
          4
        );

    }, [
      verifications,
    ]);


  // ==========================================================
  // UI
  // ==========================================================

  return (
    <DashboardLayout>

      <main className="gemologist-dashboard-page">

        {/* ====================================================
            HERO
            ==================================================== */}

        <section className="gemologist-dashboard-hero">

          <div className="gemologist-dashboard-hero-content">

            <span className="gemologist-dashboard-eyebrow">
              PROFESSIONAL VERIFICATION WORKSPACE
            </span>


            <h1>
              Gemstone Verification
            </h1>


            <p>
              Review seller submissions,
              inspect evidence, run
              AI-assisted analysis and
              make the final professional
              verification decision.
            </p>

          </div>


          <Link
            to="/gemologist/verifications"
            className="gemologist-queue-button"
          >
            Open Verification Queue

            <span>
              →
            </span>
          </Link>

        </section>


        {/* ====================================================
            LIVE STATISTICS
            ==================================================== */}

        <section className="gemologist-dashboard-section">

          <div className="gemologist-section-heading">

            <div>

              <span>
                LIVE WORKLOAD
              </span>

              <h2>
                Verification Overview
              </h2>

            </div>


            <button
              type="button"
              className="gemologist-refresh-button"
              onClick={
                loadDashboard
              }
              disabled={
                loading
              }
            >
              {loading
                ? "Refreshing..."
                : "↻ Refresh"}
            </button>

          </div>


          {error && (
            <div className="gemologist-dashboard-error">
              {error}
            </div>
          )}


          <div className="gemologist-stats-grid">

            {/* PENDING */}

            <article className="gemologist-stat-card gemologist-stat-primary">

              <div className="gemologist-stat-top">

                <span className="gemologist-stat-icon">
                  ◇
                </span>

                <span className="gemologist-stat-label">
                  Pending Reviews
                </span>

              </div>


              <strong>
                {
                  loading
                    ? "—"
                    : stats.pending
                }
              </strong>


              <p>
                Seller submissions
                currently waiting for
                Gemologist review.
              </p>

            </article>


            {/* AI COMPLETE */}

            <article className="gemologist-stat-card">

              <div className="gemologist-stat-top">

                <span className="gemologist-stat-icon completed">
                  AI
                </span>

                <span className="gemologist-stat-label">
                  AI Completed
                </span>

              </div>


              <strong>
                {
                  loading
                    ? "—"
                    : stats.aiCompleted
                }
              </strong>


              <p>
                Pending requests with
                agent analysis ready for
                human assessment.
              </p>

            </article>


            {/* NOT STARTED */}

            <article className="gemologist-stat-card">

              <div className="gemologist-stat-top">

                <span className="gemologist-stat-icon pending">
                  ◆
                </span>

                <span className="gemologist-stat-label">
                  AI Not Started
                </span>

              </div>


              <strong>
                {
                  loading
                    ? "—"
                    : stats.aiNotStarted
                }
              </strong>


              <p>
                Pending verifications
                where the Gemora Agent
                has not yet been run.
              </p>

            </article>


            {/* ATTENTION */}

            <article className="gemologist-stat-card">

              <div className="gemologist-stat-top">

                <span className="gemologist-stat-icon attention">
                  !
                </span>

                <span className="gemologist-stat-label">
                  Attention Required
                </span>

              </div>


              <strong>
                {
                  loading
                    ? "—"
                    : stats.attentionRequired
                }
              </strong>


              <p>
                Items with missing
                evidence or AI execution
                issues.
              </p>

            </article>

          </div>

        </section>


        {/* ====================================================
            OPERATIONAL DASHBOARD
            ==================================================== */}

        <section className="gemologist-dashboard-two-column">

          {/* ==================================================
              PENDING PREVIEW
              ================================================== */}

          <article className="gemologist-dashboard-panel">

            <div className="gemologist-panel-heading">

              <div>

                <span>
                  WORK QUEUE
                </span>

                <h2>
                  Pending Verification
                </h2>

                <p>
                  Most recently submitted
                  gemstones awaiting
                  professional review.
                </p>

              </div>


              <Link
                to="/gemologist/verifications"
                className="gemologist-panel-link"
              >
                View All →
              </Link>

            </div>


            {loading ? (
              <div className="gemologist-panel-state">

                <span className="gemologist-dashboard-loader" />

                <p>
                  Loading verification
                  queue...
                </p>

              </div>

            ) : recentVerifications.length ===
              0 ? (
              <div className="gemologist-panel-state">

                <span className="gemologist-empty-symbol">
                  ✓
                </span>

                <strong>
                  Queue is clear
                </strong>

                <p>
                  There are currently no
                  pending gemstone
                  verifications.
                </p>

              </div>

            ) : (
              <div className="gemologist-preview-list">

                {recentVerifications.map(
                  (
                    item
                  ) => {

                    const imageUrl =
                      resolveMediaUrl(
                        item.primaryImageUrl
                      );


                    return (
                      <div
                        key={
                          item.verificationId
                        }
                        className="gemologist-preview-item"
                      >

                        {/* IMAGE */}

                        <div className="gemologist-preview-image">

                          {imageUrl ? (
                            <img
                              src={
                                imageUrl
                              }
                              alt={
                                item.title
                              }
                            />
                          ) : (
                            <span>
                              ◆
                            </span>
                          )}

                        </div>


                        {/* DETAILS */}

                        <div className="gemologist-preview-content">

                          <div className="gemologist-preview-title-row">

                            <div>

                              <span>
                                {
                                  item.gemType ||
                                  "Gemstone"
                                }
                              </span>

                              <h3>
                                {
                                  item.title
                                }
                              </h3>

                            </div>


                            <span
                              className={`gemologist-ai-chip ${getAiStatusClass(
                                item.aiStatus
                              )}`}
                            >
                              {getAiStatusLabel(
                                item.aiStatus
                              )}
                            </span>

                          </div>


                          <div className="gemologist-preview-meta">

                            <span>
                              Seller:{" "}
                              <strong>
                                {
                                  item.sellerName
                                }
                              </strong>
                            </span>


                            <span>
                              {
                                item.caratWeight
                              }{" "}
                              ct
                            </span>


                            <span>
                              {formatDate(
                                item.createdAt
                              )}
                            </span>

                          </div>


                          <div className="gemologist-preview-evidence">

                            <span
                              className={
                                item.primaryImageUrl
                                  ? "available"
                                  : "missing"
                              }
                            >
                              {item.primaryImageUrl
                                ? "✓ Image"
                                : "× Image"}
                            </span>


                            <span
                              className={
                                item.certificateUrl
                                  ? "available"
                                  : "missing"
                              }
                            >
                              {item.certificateUrl
                                ? "✓ Certificate"
                                : "× Certificate"}
                            </span>

                          </div>

                        </div>


                        {/* ACTION */}

                        <Link
                          to={`/gemologist/verifications/${item.verificationId}`}
                          className="gemologist-preview-open"
                        >
                          Open Review
                          <span>
                            →
                          </span>
                        </Link>

                      </div>
                    );
                  }
                )}

              </div>
            )}

          </article>


          {/* ==================================================
              ATTENTION REQUIRED
              ================================================== */}

          <article className="gemologist-dashboard-panel">

            <div className="gemologist-panel-heading">

              <div>

                <span>
                  REVIEW PRIORITIES
                </span>

                <h2>
                  Attention Required
                </h2>

                <p>
                  Evidence or AI states
                  that may need closer
                  human inspection.
                </p>

              </div>

            </div>


            {loading ? (
              <div className="gemologist-panel-state">

                <span className="gemologist-dashboard-loader" />

                <p>
                  Checking verification
                  evidence...
                </p>

              </div>

            ) : attentionItems.length ===
              0 ? (
              <div className="gemologist-panel-state">

                <span className="gemologist-empty-symbol">
                  ✓
                </span>

                <strong>
                  No immediate issues
                </strong>

                <p>
                  Pending submissions
                  currently have no
                  detected missing
                  evidence or AI failures.
                </p>

              </div>

            ) : (
              <div className="gemologist-attention-list">

                {attentionItems.map(
                  (
                    item
                  ) => {

                    const normalizedAi =
                      (
                        item.aiStatus ||
                        ""
                      )
                        .replace(
                          /\s/g,
                          ""
                        )
                        .toLowerCase();


                    const issues = [];


                    if (
                      !item.primaryImageUrl
                    ) {
                      issues.push(
                        "Gemstone image missing"
                      );
                    }


                    if (
                      !item.certificateUrl
                    ) {
                      issues.push(
                        "Certificate file missing"
                      );
                    }


                    if (
                      normalizedAi ===
                      "needsmoreevidence"
                    ) {
                      issues.push(
                        "Agent requested more evidence"
                      );
                    }


                    if (
                      normalizedAi ===
                      "failed"
                    ) {
                      issues.push(
                        "Previous AI execution failed"
                      );
                    }


                    return (
                      <Link
                        key={
                          item.verificationId
                        }
                        to={`/gemologist/verifications/${item.verificationId}`}
                        className="gemologist-attention-item"
                      >

                        <div className="gemologist-attention-icon">
                          !
                        </div>


                        <div>

                          <span>
                            VERIFICATION #
                            {
                              item.verificationId
                            }
                          </span>

                          <strong>
                            {
                              item.title
                            }
                          </strong>


                          <ul>

                            {issues.map(
                              (
                                issue
                              ) => (
                                <li
                                  key={
                                    issue
                                  }
                                >
                                  {issue}
                                </li>
                              )
                            )}

                          </ul>

                        </div>


                        <span className="gemologist-attention-arrow">
                          →
                        </span>

                      </Link>
                    );
                  }
                )}

              </div>
            )}

          </article>

        </section>


        {/* ====================================================
            WORKFLOW
            ==================================================== */}

        <section className="gemologist-dashboard-section">

          <div className="gemologist-section-heading">

            <div>

              <span>
                VERIFICATION PROCESS
              </span>

              <h2>
                Human-Controlled Workflow
              </h2>

            </div>

          </div>


          <div className="gemologist-workflow-grid">

            <article>

              <span>
                01
              </span>

              <h3>
                Review Evidence
              </h3>

              <p>
                Inspect gemstone images,
                certificate details and
                seller-provided
                information.
              </p>

            </article>


            <article>

              <span>
                02
              </span>

              <h3>
                Run AI Analysis
              </h3>

              <p>
                Use the Gemora
                Verification Agent for
                advisory visual findings,
                suggestions and risk
                flags.
              </p>

            </article>


            <article>

              <span>
                03
              </span>

              <h3>
                Human Decision
              </h3>

              <p>
                Approve, reject or
                request changes after
                completing professional
                review.
              </p>

            </article>

          </div>

        </section>


        {/* ====================================================
            HUMAN AUTHORITY NOTICE
            ==================================================== */}

        <section className="gemologist-human-notice">

          <div className="gemologist-human-notice-icon">
            H
          </div>


          <div>

            <h3>
              AI assists. Humans decide.
            </h3>

            <p>
              Gemora AI does not
              independently approve
              gemstones. Final
              verification authority
              remains with the
              Gemologist.
            </p>

          </div>

        </section>

      </main>

    </DashboardLayout>
  );
}


export default GemologistDashboard;