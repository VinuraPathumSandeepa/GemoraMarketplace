import {
  useEffect,
  useMemo,
  useState,
} from "react";

import { Link } from "react-router-dom";

import DashboardLayout from "../../layouts/DashboardLayout";
import api from "../../services/api";


// ============================================================
// API / MEDIA HELPERS
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


// ============================================================
// FORMAT HELPERS
// ============================================================

function formatMoney(
  amount,
  currency
) {
  const numericAmount =
    Number(amount || 0);

  try {
    return new Intl.NumberFormat(
      "en-US",
      {
        style: "currency",
        currency:
          currency || "LKR",
        maximumFractionDigits: 2,
      }
    ).format(numericAmount);
  } catch {
    return `${
      currency || "LKR"
    } ${numericAmount.toLocaleString()}`;
  }
}


function formatDate(value) {
  if (!value) {
    return "Not available";
  }

  const date =
    new Date(value);

  if (
    Number.isNaN(
      date.getTime()
    )
  ) {
    return value;
  }

  return date.toLocaleString(
    undefined,
    {
      year: "numeric",
      month: "short",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
    }
  );
}


function getAiLabel(aiStatus) {
  if (!aiStatus) {
    return "Not Started";
  }

  return aiStatus
    .replace(/([A-Z])/g, " $1")
    .trim();
}


// ============================================================
// VERIFICATION QUEUE
// ============================================================

function VerificationQueue() {
  // ==========================================================
  // STATE
  // ==========================================================

  const [
    verifications,
    setVerifications,
  ] = useState([]);

  const [
    loading,
    setLoading,
  ] = useState(true);

  const [
    refreshing,
    setRefreshing,
  ] = useState(false);

  const [
    error,
    setError,
  ] = useState("");

  const [
    search,
    setSearch,
  ] = useState("");

  const [
    sort,
    setSort,
  ] = useState("newest");


  // ==========================================================
  // LOAD PENDING VERIFICATIONS
  // ==========================================================

  const loadPendingVerifications =
    async ({
      showRefresh = false,
    } = {}) => {
      if (showRefresh) {
        setRefreshing(true);
      } else {
        setLoading(true);
      }

      setError("");

      try {
        const response =
          await api.get(
            "/GemVerifications/pending"
          );

        const items =
          Array.isArray(response.data)
            ? response.data
            : [];

        setVerifications(items);

      } catch (error) {
        console.error(
          "Failed to load pending verifications:",
          error
        );

        setError(
          error.response?.data?.message ||
            "Unable to load the verification queue. Please try again."
        );

      } finally {
        setLoading(false);
        setRefreshing(false);
      }
    };


  useEffect(() => {
    loadPendingVerifications();
  }, []);


  // ==========================================================
  // FILTER / SORT
  // ==========================================================

  const filteredVerifications =
    useMemo(() => {
      const query =
        search
          .trim()
          .toLowerCase();

      let result =
        [...verifications];


      if (query) {
        result =
          result.filter(
            (item) => {
              const searchable =
                [
                  item.title,
                  item.gemType,
                  item.sellerName,
                  item.color,
                  item.clarity,
                  item.cut,
                  item.certificateNumber,
                  item.certificateAuthority,
                  item.listingStatus,
                  item.aiStatus,
                ]
                  .filter(Boolean)
                  .join(" ")
                  .toLowerCase();

              return searchable.includes(
                query
              );
            }
          );
      }


      result.sort(
        (a, b) => {
          if (sort === "oldest") {
            return (
              new Date(a.createdAt) -
              new Date(b.createdAt)
            );
          }

          if (sort === "title") {
            return (
              a.title || ""
            ).localeCompare(
              b.title || ""
            );
          }

          if (sort === "carat") {
            return (
              Number(b.caratWeight || 0) -
              Number(a.caratWeight || 0)
            );
          }

          return (
            new Date(b.createdAt) -
            new Date(a.createdAt)
          );
        }
      );


      return result;
    }, [
      verifications,
      search,
      sort,
    ]);


  // ==========================================================
  // SUMMARY STATS
  // ==========================================================

  const totalPending =
    verifications.length;

  const withImage =
    verifications.filter(
      (item) =>
        Boolean(
          item.primaryImageUrl
        )
    ).length;

  const withCertificate =
    verifications.filter(
      (item) =>
        Boolean(
          item.certificateUrl ||
            item.certificateNumber
        )
    ).length;

  const aiNotStarted =
    verifications.filter(
      (item) => {
        const status =
          (
            item.aiStatus || ""
          ).toLowerCase();

        return (
          !status ||
          status ===
            "notstarted"
        );
      }
    ).length;


  // ==========================================================
  // PAGE
  // ==========================================================

  return (
    <DashboardLayout title="Verification Queue">

      <div className="verification-queue-page">

        {/* ====================================================
            PAGE HEADER
            ==================================================== */}

        <section className="verification-queue-header">

          <div>

            <span className="verification-eyebrow">
              COMPONENT 1 · GEMOLOGIST WORKSPACE
            </span>

            <h1>
              Pending Gemstone
              Verifications
            </h1>

            <p>
              Review seller evidence,
              inspect gemstone details,
              run AI-assisted analysis
              and make the final human
              verification decision.
            </p>

          </div>


          <button
            type="button"
            className="verification-refresh-button"
            onClick={() =>
              loadPendingVerifications({
                showRefresh: true,
              })
            }
            disabled={refreshing}
          >
            {refreshing
              ? "Refreshing..."
              : "↻ Refresh Queue"}
          </button>

        </section>


        {/* ====================================================
            HUMAN DECISION NOTICE
            ==================================================== */}

        <section className="verification-human-notice">

          <div className="verification-human-icon">
            AI
          </div>

          <div>
            <strong>
              AI assists the review.
              The Gemologist makes the
              final decision.
            </strong>

            <p>
              AI findings are advisory
              evidence only. Approval,
              rejection or requested
              changes remain under human
              gemologist control.
            </p>
          </div>

        </section>


        {/* ====================================================
            SUMMARY
            ==================================================== */}

        <section className="verification-stat-grid">

          <article>
            <span>
              Pending Reviews
            </span>

            <strong>
              {totalPending}
            </strong>

            <small>
              Awaiting gemologist action
            </small>
          </article>


          <article>
            <span>
              Images Available
            </span>

            <strong>
              {withImage}
            </strong>

            <small>
              Listings with visual evidence
            </small>
          </article>


          <article>
            <span>
              Certificates
            </span>

            <strong>
              {withCertificate}
            </strong>

            <small>
              Certificate evidence supplied
            </small>
          </article>


          <article>
            <span>
              AI Not Started
            </span>

            <strong>
              {aiNotStarted}
            </strong>

            <small>
              Ready for assisted analysis
            </small>
          </article>

        </section>


        {/* ====================================================
            SEARCH / SORT
            ==================================================== */}

        <section className="verification-toolbar">

          <div className="verification-search">

            <span>
              ⌕
            </span>

            <input
              type="search"
              value={search}
              onChange={(event) =>
                setSearch(
                  event.target.value
                )
              }
              placeholder="Search gem, seller, certificate..."
            />

          </div>


          <div className="verification-sort">

            <label htmlFor="verification-sort">
              Sort
            </label>

            <select
              id="verification-sort"
              value={sort}
              onChange={(event) =>
                setSort(
                  event.target.value
                )
              }
            >
              <option value="newest">
                Newest first
              </option>

              <option value="oldest">
                Oldest first
              </option>

              <option value="title">
                Gem title A–Z
              </option>

              <option value="carat">
                Highest carat
              </option>
            </select>

          </div>

        </section>


        {/* ====================================================
            ERROR
            ==================================================== */}

        {error && (
          <div className="verification-error">
            {error}
          </div>
        )}


        {/* ====================================================
            LOADING
            ==================================================== */}

        {loading && (
          <div className="verification-loading">

            <div className="verification-loader" />

            <h3>
              Loading verification queue
            </h3>

            <p>
              Retrieving pending gemstone
              submissions...
            </p>

          </div>
        )}


        {/* ====================================================
            EMPTY QUEUE
            ==================================================== */}

        {!loading &&
          !error &&
          filteredVerifications.length ===
            0 && (
            <div className="verification-empty">

              <div className="verification-empty-icon">
                ✓
              </div>

              <h3>
                {search
                  ? "No matching verifications"
                  : "Verification queue is clear"}
              </h3>

              <p>
                {search
                  ? "Try another gemstone, seller, or certificate search."
                  : "There are currently no pending gemstone submissions requiring review."}
              </p>

            </div>
          )}


        {/* ====================================================
            VERIFICATION CARDS
            ==================================================== */}

        {!loading &&
          !error &&
          filteredVerifications.length >
            0 && (
            <section className="verification-card-grid">

              {filteredVerifications.map(
                (verification) => {
                  const imageUrl =
                    resolveMediaUrl(
                      verification.primaryImageUrl
                    );

                  const certificateAvailable =
                    Boolean(
                      verification.certificateUrl ||
                        verification.certificateNumber
                    );

                  return (
                    <article
                      key={
                        verification.verificationId
                      }
                      className="verification-queue-card"
                    >

                      {/* IMAGE */}

                      <div className="verification-card-image">

                        {imageUrl ? (
                          <img
                            src={imageUrl}
                            alt={
                              verification.title ||
                              "Gemstone evidence"
                            }
                          />
                        ) : (
                          <div className="verification-no-image">
                            <span>
                              ◆
                            </span>

                            <small>
                              No gemstone image
                            </small>
                          </div>
                        )}


                        <span className="verification-status-chip">
                          {verification.decision ||
                            "Pending"}
                        </span>

                      </div>


                      {/* BODY */}

                      <div className="verification-card-body">

                        <div className="verification-card-topline">

                          <span>
                            Verification #
                            {
                              verification.verificationId
                            }
                          </span>

                          <small>
                            {formatDate(
                              verification.createdAt
                            )}
                          </small>

                        </div>


                        <h2>
                          {verification.title ||
                            "Untitled Gemstone"}
                        </h2>


                        <p className="verification-seller">
                          Submitted by{" "}
                          <strong>
                            {verification.sellerName ||
                              "Seller"}
                          </strong>
                        </p>


                        {/* BASIC GEM DATA */}

                        <div className="verification-gem-facts">

                          <div>
                            <span>
                              Gem Type
                            </span>

                            <strong>
                              {verification.gemType ||
                                "—"}
                            </strong>
                          </div>


                          <div>
                            <span>
                              Carat
                            </span>

                            <strong>
                              {verification.caratWeight
                                ? `${verification.caratWeight} ct`
                                : "—"}
                            </strong>
                          </div>


                          <div>
                            <span>
                              Color
                            </span>

                            <strong>
                              {verification.color ||
                                "—"}
                            </strong>
                          </div>


                          <div>
                            <span>
                              Cut
                            </span>

                            <strong>
                              {verification.cut ||
                                "—"}
                            </strong>
                          </div>

                        </div>


                        {/* PRICE */}

                        <div className="verification-price-row">

                          <span>
                            Listed Value
                          </span>

                          <strong>
                            {formatMoney(
                              verification.price,
                              verification.currency
                            )}
                          </strong>

                        </div>


                        {/* EVIDENCE */}

                        <div className="verification-evidence-row">

                          <span
                            className={
                              imageUrl
                                ? "evidence-ready"
                                : "evidence-missing"
                            }
                          >
                            {imageUrl
                              ? "✓ Image"
                              : "○ No image"}
                          </span>


                          <span
                            className={
                              certificateAvailable
                                ? "evidence-ready"
                                : "evidence-missing"
                            }
                          >
                            {certificateAvailable
                              ? "✓ Certificate"
                              : "○ No certificate"}
                          </span>

                        </div>


                        {/* AI */}

                        <div className="verification-ai-row">

                          <div>
                            <span>
                              AI Analysis
                            </span>

                            <strong>
                              {getAiLabel(
                                verification.aiStatus
                              )}
                            </strong>
                          </div>


                          {verification.aiConfidenceScore !=
                            null && (
                            <div>
                              <span>
                                Confidence
                              </span>

                              <strong>
                                {Number(
                                  verification.aiConfidenceScore
                                ) <= 1
                                  ? `${(
                                      Number(
                                        verification.aiConfidenceScore
                                      ) * 100
                                    ).toFixed(
                                      0
                                    )}%`
                                  : `${Number(
                                      verification.aiConfidenceScore
                                    ).toFixed(
                                      0
                                    )}%`}
                              </strong>
                            </div>
                          )}

                        </div>


                        {/* ACTION */}

                        <Link
                          to={`/gemologist/verifications/${verification.verificationId}`}
                          className="verification-open-button"
                        >
                          Open Verification
                          <span>
                            →
                          </span>
                        </Link>

                      </div>

                    </article>
                  );
                }
              )}

            </section>
          )}

      </div>

    </DashboardLayout>
  );
}

export default VerificationQueue;