import { useEffect, useState } from "react";

import {
  Link,
  useNavigate,
  useParams,
} from "react-router-dom";

import DashboardLayout from "../../layouts/DashboardLayout";

import api, {
  resolveApiAssetUrl,
} from "../../services/api";

import { openProtectedCertificate } from "../../services/certificateAccess";

import GemVerificationAgentPanel from "./GemVerificationAgentPanel";

// ============================================================
// FORMAT HELPERS
// ============================================================

function formatMoney(amount, currency) {
  const numericAmount = Number(amount || 0);

  try {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: currency || "LKR",
      maximumFractionDigits: 2,
    }).format(numericAmount);
  } catch {
    return `${currency || "LKR"} ${numericAmount.toLocaleString()}`;
  }
}

function formatDate(value) {
  if (!value) {
    return "Not available";
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleString(undefined, {
    year: "numeric",
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
}

// ============================================================
// PARSE PERSISTED AI INFORMATION
//
// Backend stores these values as JSON in AiRiskFlags:
//
// - imageAnalyzed
// - visualObservations
// - riskFlags
// - validationIssues
// - stepsCompleted
// ============================================================

function parsePersistedAiDetails(value) {
  const emptyResult = {
    imageAnalyzed: false,
    visualObservations: [],
    riskFlags: [],
    validationIssues: [],
    stepsCompleted: [],
  };

  if (!value) {
    return emptyResult;
  }

  try {
    const parsed = JSON.parse(value);

    return {
      imageAnalyzed: Boolean(parsed.imageAnalyzed),

      visualObservations: Array.isArray(
        parsed.visualObservations
      )
        ? parsed.visualObservations
        : [],

      riskFlags: Array.isArray(parsed.riskFlags)
        ? parsed.riskFlags
        : [],

      validationIssues: Array.isArray(
        parsed.validationIssues
      )
        ? parsed.validationIssues
        : [],

      stepsCompleted: Array.isArray(
        parsed.stepsCompleted
      )
        ? parsed.stepsCompleted
        : [],
    };
  } catch {
    // Backward compatibility with previously stored text.

    return {
      ...emptyResult,
      riskFlags: [value],
    };
  }
}

// ============================================================
// BUILD AI RESULT FROM DATABASE DTO
// ============================================================

function buildStoredAiResult(verification) {
  if (!verification) {
    return null;
  }

  const details = parsePersistedAiDetails(
    verification.aiRiskFlags
  );

  return {
    status:
      verification.aiStatus || "NotStarted",

    suggestedGemType:
      verification.aiSuggestedGemType,

    confidenceScore:
      verification.aiConfidenceScore,

    findings:
      verification.aiFindings,

    imageAnalyzed:
      details.imageAnalyzed,

    visualObservations:
      details.visualObservations,

    riskFlags:
      details.riskFlags,

    validationIssues:
      details.validationIssues,

    stepsCompleted:
      details.stepsCompleted,

    processedAt:
      verification.aiProcessedAt,
  };
}

// ============================================================
// VERIFICATION DETAILS COMPONENT
// ============================================================

function VerificationDetails() {
  const { id } = useParams();

  const navigate = useNavigate();

  // ==========================================================
  // MAIN STATE
  // ==========================================================

  const [verification, setVerification] =
    useState(null);

  const [aiResult, setAiResult] =
    useState(null);

  const [loading, setLoading] =
    useState(true);

  const [analyzing, setAnalyzing] =
    useState(false);

  const [reviewing, setReviewing] =
    useState(false);

  const [error, setError] =
    useState("");

  const [success, setSuccess] =
    useState("");

  // ==========================================================
  // PROTECTED CERTIFICATE STATE
  // ==========================================================

  const [
    certificateOpening,
    setCertificateOpening,
  ] = useState(false);

  // ==========================================================
  // IMAGE FALLBACK STATE
  // ==========================================================

  const [imageFailed, setImageFailed] =
    useState(false);

  useEffect(() => {
    setImageFailed(false);
  }, [verification?.primaryImageUrl]);

  // ==========================================================
  // HUMAN REVIEW STATE
  // ==========================================================

  const [reviewNotes, setReviewNotes] =
    useState("");

  const [
    pendingDecision,
    setPendingDecision,
  ] = useState(null);

  // ==========================================================
  // LOAD VERIFICATION
  // ==========================================================

  const loadVerification = async ({
    silent = false,
  } = {}) => {
    if (!silent) {
      setLoading(true);
    }

    setError("");

    try {
      const response = await api.get(
        `/GemVerifications/${id}`
      );

      const data = response.data;

      setVerification(data);

      setReviewNotes(
        data.reviewNotes || ""
      );

      setAiResult(
        buildStoredAiResult(data)
      );

      return data;
    } catch (err) {
      console.error(
        "Failed to load verification:",
        err
      );

      setError(
        err.response?.data?.message ||
          err.response?.data?.title ||
          "Unable to load this gemstone verification."
      );

      return null;
    } finally {
      if (!silent) {
        setLoading(false);
      }
    }
  };

  useEffect(() => {
    loadVerification();
  }, [id]);

  // ==========================================================
  // RUN GEMORA VERIFICATION AGENT
  // ==========================================================

  const handleRunAiAnalysis = async () => {
    if (!verification || analyzing) {
      return;
    }

    setError("");
    setSuccess("");
    setAnalyzing(true);

    try {
      const response = await api.post(
        `/GemVerifications/${verification.verificationId}/ai-analysis`
      );

      // Display the returned AI result.

      setAiResult(response.data);

      const aiStatus = (
        response.data?.status || ""
      ).toLowerCase();

      // Reload persisted information so the
      // React UI stays synchronized with PostgreSQL.

      await loadVerification({
        silent: true,
      });

      if (
        aiStatus === "needsmoreevidence"
      ) {
        setSuccess(
          "The Gemora Verification Agent completed evidence validation and determined that additional or corrected evidence is required."
        );
      } else {
        setSuccess(
          "Gemora Verification Agent completed the gemstone analysis successfully."
        );
      }
    } catch (err) {
      console.error(
        "Gemora Verification Agent failed:",
        err
      );

      const message =
        err.response?.data?.message ||
        err.response?.data?.title ||
        "The Gemora Verification Agent could not complete the analysis. The Gemologist may still review the submitted evidence.";

      // Backend may have updated AiStatus to Failed.

      await loadVerification({
        silent: true,
      });

      // Keep the original AI failure message
      // after the silent refresh.

      setError(message);
    } finally {
      setAnalyzing(false);
    }
  };

  // ==========================================================
  // OPEN AUTHORIZED CERTIFICATE
  //
  // Uses:
  // GET /api/GemCertificates/listings/{listingId}/access
  //
  // Supports:
  // - Protected legacy certificate files
  // - Private Supabase signed URLs
  // ==========================================================

  const handleOpenCertificate = async (
    event
  ) => {
    event?.preventDefault();

    if (
      certificateOpening ||
      !verification?.certificateUrl
    ) {
      return;
    }

    try {
      setError("");

      setCertificateOpening(true);

      await openProtectedCertificate(
        verification.gemListingId
      );
    } catch (err) {
      console.error(
        "Certificate access failed:",
        err
      );

      setError(
        err.response?.data?.message ||
          err.response?.data?.title ||
          err.message ||
          "The certificate could not be opened."
      );
    } finally {
      setCertificateOpening(false);
    }
  };

  // ==========================================================
  // REQUEST HUMAN DECISION
  // ==========================================================

  const requestDecision = (decision) => {
    setError("");
    setSuccess("");

    const requiresNotes =
      decision === "ChangesRequested" ||
      decision === "Rejected";

    if (
      requiresNotes &&
      !reviewNotes.trim()
    ) {
      setError(
        decision === "ChangesRequested"
          ? "Please explain what the seller needs to correct before requesting changes."
          : "Please explain why this gemstone listing is being rejected."
      );

      return;
    }

    setPendingDecision(decision);
  };

  // ==========================================================
  // CONFIRM HUMAN DECISION
  // ==========================================================

  const confirmDecision = async () => {
    if (
      !pendingDecision ||
      !verification ||
      reviewing
    ) {
      return;
    }

    setReviewing(true);

    setError("");
    setSuccess("");

    try {
      const response = await api.put(
        `/GemVerifications/${verification.verificationId}/review`,
        {
          decision: pendingDecision,

          reviewNotes:
            reviewNotes.trim() || null,
        }
      );

      setVerification(response.data);

      const completedDecision =
        pendingDecision;

      setPendingDecision(null);

      if (
        completedDecision === "Approved"
      ) {
        setSuccess(
          "Gemstone verification approved successfully."
        );
      } else if (
        completedDecision ===
        "ChangesRequested"
      ) {
        setSuccess(
          "Changes have been requested from the seller."
        );
      } else {
        setSuccess(
          "Gemstone verification has been rejected."
        );
      }

      // Return to the verification queue after success.

      setTimeout(() => {
        navigate(
          "/gemologist/verifications",
          {
            replace: true,
          }
        );
      }, 1400);
    } catch (err) {
      console.error(
        "Gemologist review failed:",
        err
      );

      setError(
        err.response?.data?.message ||
          err.response?.data?.title ||
          "The Gemologist decision could not be saved."
      );
    } finally {
      setReviewing(false);
    }
  };

  // ==========================================================
  // LOADING PAGE
  // ==========================================================

  if (loading) {
    return (
      <DashboardLayout title="Gem Verification">
        <div className="verification-details-loading">
          <div className="verification-loader" />

          <h3>
            Loading gemstone verification
          </h3>

          <p>
            Retrieving listing evidence and
            verification information...
          </p>
        </div>
      </DashboardLayout>
    );
  }

  // ==========================================================
  // FAILED LOAD
  // ==========================================================

  if (!verification) {
    return (
      <DashboardLayout title="Gem Verification">
        <div className="verification-details-error">
          <h2>
            Verification unavailable
          </h2>

          <p>
            {error ||
              "The requested gemstone verification could not be found."}
          </p>

          <Link to="/gemologist/verifications">
            ← Back to Verification Queue
          </Link>
        </div>
      </DashboardLayout>
    );
  }

  // ==========================================================
  // DERIVED VALUES
  // ==========================================================

  const imageUrl =
    resolveApiAssetUrl(
      verification.primaryImageUrl
    );

  // IMPORTANT:
  // CertificateUrl may soon contain:
  //
  // supabase-private://gem-certificates/...
  //
  // Therefore, do NOT pass certificateUrl through
  // resolveApiAssetUrl().

  const hasCertificate = Boolean(
    verification.certificateUrl
  );

  const isPending =
    (
      verification.decision || ""
    ).toLowerCase() === "pending";

  // ==========================================================
  // MAIN PAGE
  // ==========================================================

  return (
    <DashboardLayout title="Gem Verification">
      <div className="verification-details-page">

        {/* ====================================================
            BACK BUTTON
            ==================================================== */}

        <Link
          to="/gemologist/verifications"
          className="verification-back-link"
        >
          ← Back to Verification Queue
        </Link>

        {/* ====================================================
            HEADER
            ==================================================== */}

        <section className="verification-details-header">
          <div>
            <span className="verification-eyebrow">
              VERIFICATION #
              {verification.verificationId}
            </span>

            <h1>
              {verification.title}
            </h1>

            <p>
              Submitted by{" "}

              <strong>
                {verification.sellerName}
              </strong>

              {" · "}

              Listing #
              {verification.gemListingId}
            </p>
          </div>

          <div className="verification-header-statuses">
            <span className="verification-detail-status">
              {verification.listingStatus}
            </span>

            <span className="verification-detail-decision">
              {verification.decision}
            </span>
          </div>
        </section>

        {/* ====================================================
            MESSAGES
            ==================================================== */}

        {error && (
          <div className="verification-error">
            {error}
          </div>
        )}

        {success && (
          <div className="verification-details-success">
            {success}
          </div>
        )}

        {/* ====================================================
            GEMSTONE IMAGE AND LISTING INFORMATION
            ==================================================== */}

        <section className="verification-evidence-layout">

          {/* IMAGE PANEL */}

          <article className="verification-detail-panel">
            <div className="verification-panel-heading">
              <div>
                <span>
                  VISUAL EVIDENCE
                </span>

                <h2>
                  Gemstone Image
                </h2>
              </div>

              {imageUrl &&
                !imageFailed && (
                  <a
                    href={imageUrl}
                    target="_blank"
                    rel="noreferrer"
                  >
                    Open Original ↗
                  </a>
                )}
            </div>

            <div className="verification-large-image">
              {imageUrl &&
              !imageFailed ? (
                <img
                  src={imageUrl}
                  alt={
                    verification.title ||
                    "Gemstone evidence"
                  }
                  onError={() =>
                    setImageFailed(true)
                  }
                />
              ) : (
                <div className="verification-no-image">
                  <span>
                    ◆
                  </span>

                  <small>
                    {imageFailed
                      ? "Gemstone image unavailable"
                      : "No gemstone image supplied"}
                  </small>
                </div>
              )}
            </div>
          </article>

          {/* LISTING DETAILS PANEL */}

          <article className="verification-detail-panel">
            <div className="verification-panel-heading">
              <div>
                <span>
                  SELLER SUBMISSION
                </span>

                <h2>
                  Gemstone Details
                </h2>
              </div>
            </div>

            {/* GEM FACTS */}

            <div className="verification-detail-facts">

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
                  Carat Weight
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
                  Clarity
                </span>

                <strong>
                  {verification.clarity ||
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

              <div>
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
            </div>

            {/* DESCRIPTION */}

            <div className="verification-description">
              <span>
                DESCRIPTION
              </span>

              <p>
                {verification.description ||
                  "No description was supplied."}
              </p>
            </div>

            {/* SUBMISSION METADATA */}

            <div className="verification-submission-meta">
              <div>
                <span>
                  Submitted
                </span>

                <strong>
                  {formatDate(
                    verification.createdAt
                  )}
                </strong>
              </div>

              <div>
                <span>
                  Seller
                </span>

                <strong>
                  {verification.sellerName}
                </strong>
              </div>
            </div>
          </article>
        </section>

        {/* ====================================================
            CERTIFICATE EVIDENCE
            ==================================================== */}

        <section className="verification-detail-panel">
          <div className="verification-panel-heading">
            <div>
              <span>
                SUPPORTING EVIDENCE
              </span>

              <h2>
                Certificate Information
              </h2>
            </div>

            {hasCertificate && (
              <a
                href={`/api/GemCertificates/listings/${verification.gemListingId}/access`}
                onClick={
                  handleOpenCertificate
                }
                aria-disabled={
                  certificateOpening
                }
              >
                {certificateOpening
                  ? "Opening Certificate..."
                  : "Open Certificate ↗"}
              </a>
            )}
          </div>

          <div className="verification-certificate-grid">

            <div>
              <span>
                Certificate Authority
              </span>

              <strong>
                {verification.certificateAuthority ||
                  "Not provided"}
              </strong>
            </div>

            <div>
              <span>
                Certificate Number
              </span>

              <strong>
                {verification.certificateNumber ||
                  "Not provided"}
              </strong>
            </div>

            <div>
              <span>
                Certificate File
              </span>

              <strong>
                {hasCertificate
                  ? "Available"
                  : "Not uploaded"}
              </strong>
            </div>
          </div>
        </section>

        {/* ====================================================
            GEMORA VERIFICATION AGENT
            ==================================================== */}

        <GemVerificationAgentPanel
          verification={verification}
          aiResult={aiResult}
          analyzing={analyzing}
          onRunAnalysis={
            handleRunAiAnalysis
          }
        />

        {/* ====================================================
            HUMAN GEMOLOGIST DECISION
            ==================================================== */}

        <section className="verification-human-review-panel">

          {/* REVIEW HEADING */}

          <div className="verification-human-review-heading">
            <div>
              <span>
                HUMAN-IN-THE-LOOP APPROVAL
              </span>

              <h2>
                Gemologist Decision
              </h2>

              <p>
                Review the seller evidence and Gemora
                Verification Agent findings before recording
                your professional decision.
              </p>
            </div>

            <div className="verification-human-badge">
              Human Authority
            </div>
          </div>

          {/* COMPLETED REVIEW */}

          {!isPending ? (
            <div className="verification-review-completed">
              <span>
                REVIEW COMPLETED
              </span>

              <h3>
                {verification.decision}
              </h3>

              <p>
                {verification.reviewNotes ||
                  "No additional review notes were provided."}
              </p>

              <small>
                Reviewed{" "}

                {formatDate(
                  verification.reviewedAt
                )}

                {verification.gemologistName
                  ? ` by ${verification.gemologistName}`
                  : ""}
              </small>
            </div>
          ) : (
            <>

              {/* ==============================================
                  REVIEW NOTES
                  ============================================== */}

              <div className="verification-review-notes">
                <label htmlFor="reviewNotes">
                  Review Notes
                </label>

                <textarea
                  id="reviewNotes"
                  value={reviewNotes}
                  onChange={(event) => {
                    setReviewNotes(
                      event.target.value.slice(
                        0,
                        2000
                      )
                    );

                    setError("");
                  }}
                  rows={6}
                  maxLength={2000}
                  placeholder="Record professional observations. Notes are required when requesting changes or rejecting the gemstone listing."
                />

                <div className="verification-note-footer">
                  <span>
                    Required for Request Changes and Reject.
                  </span>

                  <span>
                    {reviewNotes.length}/2000
                  </span>
                </div>
              </div>

              {/* ==============================================
                  HUMAN DECISION BUTTONS
                  ============================================== */}

              <div className="verification-decision-buttons">

                {/* APPROVE */}

                <button
                  type="button"
                  className="verification-approve-button"
                  onClick={() =>
                    requestDecision(
                      "Approved"
                    )
                  }
                  disabled={reviewing}
                >
                  ✓ Approve
                </button>

                {/* REQUEST CHANGES */}

                <button
                  type="button"
                  className="verification-changes-button"
                  onClick={() =>
                    requestDecision(
                      "ChangesRequested"
                    )
                  }
                  disabled={reviewing}
                >
                  ↻ Request Changes
                </button>

                {/* REJECT */}

                <button
                  type="button"
                  className="verification-reject-button"
                  onClick={() =>
                    requestDecision(
                      "Rejected"
                    )
                  }
                  disabled={reviewing}
                >
                  × Reject
                </button>
              </div>

              {/* ==============================================
                  CONFIRMATION PANEL
                  ============================================== */}

              {pendingDecision && (
                <div className="verification-decision-confirm">
                  <div>
                    <span>
                      CONFIRM HUMAN DECISION
                    </span>

                    <strong>
                      {pendingDecision ===
                      "Approved"
                        ? "Approve this gemstone verification?"
                        : pendingDecision ===
                          "ChangesRequested"
                        ? "Request changes from the seller?"
                        : "Reject this gemstone verification?"}
                    </strong>

                    <p>
                      This action updates both the
                      verification record and gemstone
                      listing status.
                    </p>
                  </div>

                  <div className="verification-confirm-actions">

                    {/* CANCEL */}

                    <button
                      type="button"
                      onClick={() =>
                        setPendingDecision(
                          null
                        )
                      }
                      disabled={reviewing}
                    >
                      Cancel
                    </button>

                    {/* CONFIRM */}

                    <button
                      type="button"
                      className="verification-confirm-button"
                      onClick={
                        confirmDecision
                      }
                      disabled={reviewing}
                    >
                      {reviewing
                        ? "Saving Decision..."
                        : pendingDecision ===
                          "ChangesRequested"
                        ? "Confirm Request Changes"
                        : `Confirm ${pendingDecision}`}
                    </button>
                  </div>
                </div>
              )}
            </>
          )}
        </section>
      </div>
    </DashboardLayout>
  );
}

export default VerificationDetails;