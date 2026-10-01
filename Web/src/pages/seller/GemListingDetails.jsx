import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import DashboardLayout from "../../layouts/DashboardLayout";
import gemListingService from "../../services/gemVerification/gemListingService";
import api from "../../services/api";

const API_ORIGIN = "https://gemora-api.onrender.com";

const STATUS_CONFIG = {
  Draft: {
    label: "Draft",
    className: "draft",
    message:
      "This listing is still being prepared. Review the gemstone information and evidence before submitting it.",
  },

  PendingVerification: {
    label: "Pending Review",
    className: "pending",
    message:
      "Your listing has entered the verification workflow and is waiting for Gemologist review.",
  },

  ChangesRequested: {
    label: "Changes Requested",
    className: "changes",
    message:
      "The Gemologist requested changes. Update the listing or evidence and resubmit it for review.",
  },

  Approved: {
    label: "Approved",
    className: "approved",
    message:
      "This listing has completed the Gemologist verification workflow and has been approved.",
  },

  Rejected: {
    label: "Rejected",
    className: "rejected",
    message:
      "This listing was rejected during Gemologist verification.",
  },
};

function buildFileUrl(url) {
  if (!url) {
    return null;
  }

  if (
    url.startsWith("http://") ||
    url.startsWith("https://")
  ) {
    return url;
  }

  return `${API_ORIGIN}${url.startsWith("/") ? "" : "/"}${url}`;
}

function EvidenceState({ available, children }) {
  return (
    <span
      className={`gem-detail-evidence-chip ${
        available ? "available" : "missing"
      }`}
    >
      <span>{available ? "✓" : "○"}</span>
      {children}
    </span>
  );
}

function GemListingDetails() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [listing, setListing] = useState(null);

  const [loading, setLoading] = useState(true);
  const [pageError, setPageError] = useState("");

  const [imageFile, setImageFile] = useState(null);
  const [certificateFile, setCertificateFile] = useState(null);

  const [imageUploading, setImageUploading] = useState(false);
  const [certificateUploading, setCertificateUploading] =
    useState(false);

  const [submitting, setSubmitting] = useState(false);
  const [deleting, setDeleting] = useState(false);

  const [actionMessage, setActionMessage] = useState("");
  const [actionError, setActionError] = useState("");

  const [showSubmitModal, setShowSubmitModal] = useState(false);
  const [submitModalError, setSubmitModalError] = useState("");

  /* =========================================================
     LOAD LISTING
     ========================================================= */

  const loadListing = async () => {
    try {
      setLoading(true);
      setPageError("");

      const data = await gemListingService.getListingById(id);

      setListing(data);
    } catch (error) {
      console.error("Failed to load listing:", error);

      setPageError(
        error?.response?.data?.message ||
          error?.response?.data?.error ||
          "We couldn't load this gemstone listing."
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadListing();
  }, [id]);

  /* =========================================================
     HELPERS
     ========================================================= */

  const clearMessages = () => {
    setActionMessage("");
    setActionError("");
  };

  const getErrorMessage = (error, fallback) => {
    return (
      error?.response?.data?.message ||
      error?.response?.data?.error ||
      error?.message ||
      fallback
    );
  };

  /* =========================================================
     FILE VALIDATION
     ========================================================= */

  const handleImageSelection = (event) => {
    const file = event.target.files?.[0];

    if (!file) {
      return;
    }

    const allowedTypes = [
      "image/jpeg",
      "image/png",
      "image/webp",
    ];

    if (!allowedTypes.includes(file.type)) {
      setActionError(
        "Please choose a JPG, PNG, or WebP gemstone image."
      );

      event.target.value = "";
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      setActionError(
        "The gemstone image must be 5 MB or smaller."
      );

      event.target.value = "";
      return;
    }

    clearMessages();
    setImageFile(file);
  };

  const handleCertificateSelection = (event) => {
    const file = event.target.files?.[0];

    if (!file) {
      return;
    }

    const allowedTypes = [
      "application/pdf",
      "image/jpeg",
      "image/png",
    ];

    if (!allowedTypes.includes(file.type)) {
      setActionError(
        "Please choose a PDF, JPG, or PNG certificate file."
      );

      event.target.value = "";
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      setActionError(
        "The certificate file must be 10 MB or smaller."
      );

      event.target.value = "";
      return;
    }

    clearMessages();
    setCertificateFile(file);
  };

  /* =========================================================
     IMAGE UPLOAD
     ========================================================= */

  const handleImageUpload = async () => {
    if (!imageFile) {
      setActionError(
        "Select a gemstone image before uploading."
      );

      return;
    }

    try {
      clearMessages();
      setImageUploading(true);

      await gemListingService.uploadImage(id, imageFile);

      setImageFile(null);

      setActionMessage(
        "Gemstone image uploaded successfully."
      );

      await loadListing();
    } catch (error) {
      console.error("Image upload failed:", error);

      setActionError(
        getErrorMessage(
          error,
          "The gemstone image could not be uploaded."
        )
      );
    } finally {
      setImageUploading(false);
    }
  };

  /* =========================================================
     CERTIFICATE UPLOAD
     ========================================================= */

  const handleCertificateUpload = async () => {
    if (!certificateFile) {
      setActionError(
        "Select a certificate file before uploading."
      );

      return;
    }

    try {
      clearMessages();
      setCertificateUploading(true);

      await gemListingService.uploadCertificate(
        id,
        certificateFile
      );

      setCertificateFile(null);

      setActionMessage(
        "Certificate uploaded successfully."
      );

      await loadListing();
    } catch (error) {
      console.error("Certificate upload failed:", error);

      setActionError(
        getErrorMessage(
          error,
          "The certificate could not be uploaded."
        )
      );
    } finally {
      setCertificateUploading(false);
    }
  };

  /* =========================================================
     OPEN SUBMIT MODAL
     ========================================================= */

  const handleSubmitForVerification = () => {
    clearMessages();

    setSubmitModalError("");
    setShowSubmitModal(true);
  };

  const closeSubmitModal = () => {
    if (submitting) {
      return;
    }

    setShowSubmitModal(false);
    setSubmitModalError("");
  };

  /* =========================================================
     SUBMIT FOR VERIFICATION

     IMPORTANT:
     Correct ASP.NET route:
     POST /api/GemListings/{id}/submit-verification
     ========================================================= */

  const confirmSubmitForVerification = async () => {
    try {
      setSubmitModalError("");
      clearMessages();

      setSubmitting(true);

      const response = await api.post(
        `/GemListings/${id}/submit-verification`
      );

      console.log(
        "Submit verification response:",
        response.data
      );

      setShowSubmitModal(false);
      setSubmitModalError("");

      setActionMessage(
        listing?.status === "ChangesRequested"
          ? "Listing resubmitted successfully. It is now waiting for Gemologist review."
          : "Listing submitted successfully. It is now waiting for Gemologist review."
      );

      await loadListing();
    } catch (error) {
      console.error(
        "Submit verification failed:",
        error
      );

      setSubmitModalError(
        getErrorMessage(
          error,
          "The listing could not be submitted for verification."
        )
      );
    } finally {
      setSubmitting(false);
    }
  };

  /* =========================================================
     DELETE DRAFT
     ========================================================= */

  const handleDelete = async () => {
    const confirmed = window.confirm(
      "Delete this Draft permanently? This action cannot be undone."
    );

    if (!confirmed) {
      return;
    }

    try {
      clearMessages();
      setDeleting(true);

      await gemListingService.deleteListing(id);

      navigate("/seller/listings");
    } catch (error) {
      console.error("Delete failed:", error);

      setActionError(
        getErrorMessage(
          error,
          "The Draft could not be deleted."
        )
      );

      setDeleting(false);
    }
  };

  /* =========================================================
     LOADING
     ========================================================= */

  if (loading) {
    return (
      <DashboardLayout>
        <main className="gem-detail-page">
          <div className="gem-detail-state-card">
            <div className="gem-detail-loader" />

            <p className="gem-detail-eyebrow">
              GEMORA
            </p>

            <h2>
              Loading gemstone listing
            </h2>

            <p>
              Retrieving listing information and verification evidence.
            </p>
          </div>
        </main>
      </DashboardLayout>
    );
  }

  /* =========================================================
     ERROR
     ========================================================= */

  if (pageError || !listing) {
    return (
      <DashboardLayout>
        <main className="gem-detail-page">
          <div className="gem-detail-state-card">
            <div className="gem-detail-state-icon">
              !
            </div>

            <p className="gem-detail-eyebrow">
              LISTING UNAVAILABLE
            </p>

            <h2>
              We couldn't open this listing
            </h2>

            <p>
              {pageError ||
                "The requested listing was not found."}
            </p>

            <button
              type="button"
              className="gem-detail-primary-button"
              onClick={() =>
                navigate("/seller/listings")
              }
            >
              Back to My Listings →
            </button>
          </div>
        </main>
      </DashboardLayout>
    );
  }

  /* =========================================================
     DERIVED VALUES
     ========================================================= */

  const status = listing.status || "Draft";

  const statusConfig =
    STATUS_CONFIG[status] || STATUS_CONFIG.Draft;

  const canModify =
    status === "Draft" ||
    status === "ChangesRequested";

  const canDelete = status === "Draft";

  const canSubmit =
    status === "Draft" ||
    status === "ChangesRequested";

  const imageUrl = buildFileUrl(
    listing.primaryImageUrl
  );

  const certificateUrl = buildFileUrl(
    listing.certificateUrl
  );

  const hasImage = Boolean(imageUrl);
  const hasCertificate = Boolean(certificateUrl);

  /* =========================================================
     PREPARATION STAGE
     ========================================================= */

  let preparationStage;

  if (status === "Draft") {
    if (!hasImage) {
      preparationStage = {
        label: "Evidence Needed",
        className: "needs-evidence",
        description:
          "Your listing information is saved, but a gemstone image should be added before sending it for verification.",
      };
    } else {
      preparationStage = {
        label: "Ready to Submit",
        className: "ready",
        description:
          "The required gemstone image is available. Review the listing and submit this Draft when you are ready to begin Gemologist verification.",
      };
    }
  } else if (status === "ChangesRequested") {
    preparationStage = {
      label: "Changes Required",
      className: "attention",
      description:
        "A Gemologist requested corrections. Update the listing information or evidence and resubmit it for verification.",
    };
  } else if (status === "PendingVerification") {
    preparationStage = {
      label: "Pending Review",
      className: "pending",
      description:
        "The listing has left Draft preparation and is now waiting for Gemologist review.",
    };
  } else if (status === "Approved") {
    preparationStage = {
      label: "Verification Complete",
      className: "complete",
      description:
        "The Gemologist has completed the verification workflow and approved this listing.",
    };
  } else {
    preparationStage = {
      label: statusConfig.label,
      className: "attention",
      description: statusConfig.message,
    };
  }

  /* =========================================================
     PAGE
     ========================================================= */

  return (
    <DashboardLayout>
      <main className="gem-detail-page">

        {/* BACK */}

        <button
          type="button"
          className="gem-detail-back"
          onClick={() =>
            navigate("/seller/listings")
          }
        >
          ← My Gem Listings
        </button>

        {/* HEADER */}

        <section className="gem-detail-header">
          <div>
            <p className="gem-detail-eyebrow">
              GEMSTONE LISTING #{listing.id}
            </p>

            <h1>
              {listing.title}
            </h1>

            <div className="gem-detail-header-meta">
              <span
                className={`gem-detail-status ${statusConfig.className}`}
              >
                <span className="gem-detail-status-dot" />
                {statusConfig.label}
              </span>

              <span>
                {listing.gemType || "Gemstone"}
              </span>

              <span>
                {listing.caratWeight
                  ? `${listing.caratWeight} ct`
                  : "Weight not specified"}
              </span>
            </div>
          </div>

          {canModify && (
            <button
              type="button"
              className="gem-detail-secondary-button"
              onClick={() =>
                navigate(
                  `/seller/listings/${listing.id}/edit`
                )
              }
            >
              Edit Listing
              <span>↗</span>
            </button>
          )}
        </section>

        {/* SUCCESS / ERROR */}

        {actionMessage && (
          <div className="gem-detail-message success">
            <span>✓</span>
            <p>{actionMessage}</p>
          </div>
        )}

        {actionError && (
          <div className="gem-detail-message error">
            <span>!</span>
            <p>{actionError}</p>
          </div>
        )}

        {/* HERO */}

        <section className="gem-detail-hero-grid">

          {/* IMAGE */}

          <article className="gem-detail-image-card">
            <div className="gem-detail-image-stage">

              <span
                className={`gem-detail-floating-status ${statusConfig.className}`}
              >
                <span className="gem-detail-status-dot" />
                {statusConfig.label}
              </span>

              {imageUrl ? (
                <img
                  src={imageUrl}
                  alt={listing.title}
                  className="gem-detail-main-image"
                />
              ) : (
                <div className="gem-detail-image-placeholder">
                  <div className="gem-detail-gem-mark">
                    G
                  </div>

                  <p>
                    No gemstone photograph
                  </p>

                  <span>
                    Add a clear image before verification.
                  </span>
                </div>
              )}

              {listing.caratWeight && (
                <div className="gem-detail-weight-badge">
                  {listing.caratWeight} ct
                </div>
              )}
            </div>

            <div className="gem-detail-image-footer">
              <div>
                <p className="gem-detail-small-label">
                  GEMSTONE EVIDENCE
                </p>

                <h3>
                  {hasImage
                    ? "Gemstone photograph"
                    : "Image required"}
                </h3>

                <p>
                  {hasImage
                    ? "This photograph is included in the verification evidence."
                    : "Upload a clear gemstone photograph before verification."}
                </p>
              </div>

              {canModify && (
                <div className="gem-detail-upload-control">
                  <label className="gem-detail-file-picker">
                    <input
                      type="file"
                      accept=".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp"
                      onChange={handleImageSelection}
                    />

                    <span>
                      {imageFile
                        ? imageFile.name
                        : hasImage
                        ? "Choose replacement image"
                        : "Choose gemstone image"}
                    </span>
                  </label>

                  <button
                    type="button"
                    className="gem-detail-upload-button"
                    disabled={
                      !imageFile ||
                      imageUploading
                    }
                    onClick={handleImageUpload}
                  >
                    {imageUploading
                      ? "Uploading..."
                      : hasImage
                      ? "Replace Image"
                      : "Upload Image"}
                  </button>
                </div>
              )}
            </div>
          </article>

          {/* GEM INFO */}

          <article className="gem-detail-info-card">
            <div className="gem-detail-card-heading">
              <div>
                <p className="gem-detail-eyebrow">
                  GEMSTONE PROFILE
                </p>

                <h2>
                  Gem Information
                </h2>
              </div>

              <span className="gem-detail-id">
                #{listing.id}
              </span>
            </div>

            <div className="gem-detail-profile-grid">
              <div>
                <span>Gem Type</span>
                <strong>
                  {listing.gemType || "—"}
                </strong>
              </div>

              <div>
                <span>Carat Weight</span>
                <strong>
                  {listing.caratWeight
                    ? `${listing.caratWeight} ct`
                    : "—"}
                </strong>
              </div>

              <div>
                <span>Color</span>
                <strong>
                  {listing.color || "—"}
                </strong>
              </div>

              <div>
                <span>Clarity</span>
                <strong>
                  {listing.clarity || "—"}
                </strong>
              </div>

              <div>
                <span>Cut</span>
                <strong>
                  {listing.cut || "—"}
                </strong>
              </div>

              <div>
                <span>Status</span>
                <strong>
                  {statusConfig.label}
                </strong>
              </div>
            </div>

            <div className="gem-detail-description">
              <p className="gem-detail-small-label">
                DESCRIPTION
              </p>

              <p>
                {listing.description ||
                  "No description has been provided."}
              </p>
            </div>

            <div className="gem-detail-price">
              <span>
                LISTING PRICE
              </span>

              <strong>
                LKR{" "}
                {Number(
                  listing.price || 0
                ).toLocaleString("en-LK")}
              </strong>
            </div>
          </article>

        </section>

        {/* SUPPORTING EVIDENCE */}

        <section className="gem-detail-section">
          <div className="gem-detail-section-heading">
            <div>
              <p className="gem-detail-eyebrow">
                VERIFICATION EVIDENCE
              </p>

              <h2>
                Supporting evidence
              </h2>

              <p>
                Evidence supports AI-assisted analysis
                and the Gemologist's final human
                verification decision.
              </p>
            </div>

            <div className="gem-detail-evidence-summary">
              <EvidenceState available={hasImage}>
                Gem image
              </EvidenceState>

              <EvidenceState available={hasCertificate}>
                Certificate
              </EvidenceState>
            </div>
          </div>

          <div className="gem-detail-evidence-grid">

            <article className="gem-detail-evidence-card">
              <div className="gem-detail-evidence-icon">
                ◇
              </div>

              <div className="gem-detail-evidence-content">
                <p className="gem-detail-small-label">
                  GEMSTONE IMAGE
                </p>

                <h3>
                  {hasImage
                    ? "Image evidence attached"
                    : "No image evidence"}
                </h3>

                <p>
                  {hasImage
                    ? "The uploaded gemstone photograph is available for verification review."
                    : "A gemstone image should be provided before verification."}
                </p>
              </div>

              <EvidenceState available={hasImage}>
                {hasImage ? "Available" : "Missing"}
              </EvidenceState>
            </article>

            <article className="gem-detail-evidence-card">
              <div className="gem-detail-evidence-icon">
                ▤
              </div>

              <div className="gem-detail-evidence-content">
                <p className="gem-detail-small-label">
                  CERTIFICATE
                </p>

                <h3>
                  {listing.certificateNumber ||
                    "No certificate number"}
                </h3>

                <p>
                  {listing.certificateAuthority ||
                    "Certificate authority has not been provided."}
                </p>

                {certificateUrl && (
                  <a
                    href={certificateUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="gem-detail-text-link"
                  >
                    View uploaded certificate ↗
                  </a>
                )}
              </div>

              <EvidenceState available={hasCertificate}>
                {hasCertificate
                  ? "Available"
                  : "Missing"}
              </EvidenceState>
            </article>

          </div>

          {canModify && (
            <div className="gem-detail-certificate-upload">
              <div>
                <p className="gem-detail-small-label">
                  {hasCertificate
                    ? "REPLACE CERTIFICATE"
                    : "ADD CERTIFICATE"}
                </p>

                <p>
                  PDF, JPG, JPEG or PNG. Maximum 10 MB.
                </p>
              </div>

              <div className="gem-detail-upload-control horizontal">
                <label className="gem-detail-file-picker">
                  <input
                    type="file"
                    accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"
                    onChange={
                      handleCertificateSelection
                    }
                  />

                  <span>
                    {certificateFile
                      ? certificateFile.name
                      : "Choose certificate"}
                  </span>
                </label>

                <button
                  type="button"
                  className="gem-detail-upload-button"
                  disabled={
                    !certificateFile ||
                    certificateUploading
                  }
                  onClick={
                    handleCertificateUpload
                  }
                >
                  {certificateUploading
                    ? "Uploading..."
                    : hasCertificate
                    ? "Replace Certificate"
                    : "Upload Certificate"}
                </button>
              </div>
            </div>
          )}
        </section>

        {/* SELLER PREPARATION */}

        <section className="gem-detail-preparation">
          <div className="gem-detail-preparation-heading">
            <div>
              <p className="gem-detail-eyebrow">
                SELLER PREPARATION
              </p>

              <h2>
                Current stage:{" "}
                {preparationStage.label}
              </h2>

              <p>
                {preparationStage.description}
              </p>
            </div>

            <span
              className={`gem-detail-preparation-badge ${preparationStage.className}`}
            >
              {preparationStage.label}
            </span>
          </div>

          <div className="gem-detail-preparation-steps">

            <div className="gem-preparation-step completed">
              <span>✓</span>

              <div>
                <strong>
                  1. Listing Information
                </strong>

                <small>
                  Gemstone details are saved.
                </small>
              </div>
            </div>

            <div
              className={
                hasImage
                  ? "gem-preparation-step completed"
                  : "gem-preparation-step current"
              }
            >
              <span>
                {hasImage ? "✓" : "2"}
              </span>

              <div>
                <strong>
                  2. Gemstone Image
                </strong>

                <small>
                  {hasImage
                    ? "Required image evidence is available."
                    : "Add a gemstone image before review."}
                </small>
              </div>
            </div>

            <div
              className={
                hasCertificate
                  ? "gem-preparation-step completed"
                  : "gem-preparation-step optional"
              }
            >
              <span>
                {hasCertificate ? "✓" : "3"}
              </span>

              <div>
                <strong>
                  3. Certificate Evidence
                </strong>

                <small>
                  {hasCertificate
                    ? "Supporting certificate is attached."
                    : "Optional supporting evidence."}
                </small>
              </div>
            </div>

            <div
              className={
                status === "Draft" && hasImage
                  ? "gem-preparation-step current"
                  : status !== "Draft"
                  ? "gem-preparation-step completed"
                  : "gem-preparation-step locked"
              }
            >
              <span>
                {status !== "Draft" ? "✓" : "4"}
              </span>

              <div>
                <strong>
                  4. Submit for Review
                </strong>

                <small>
                  {status === "Draft" && hasImage
                    ? "You are ready to send this listing to the Gemologist."
                    : status === "Draft"
                    ? "Complete the required evidence first."
                    : "Listing has entered the verification workflow."}
                </small>
              </div>
            </div>

          </div>
        </section>

        {/* VERIFICATION WORKFLOW */}

        <section className="gem-detail-workflow">
          <div className="gem-detail-section-heading">
            <div>
              <p className="gem-detail-eyebrow">
                VERIFICATION WORKFLOW
              </p>

              <h2>
                From evidence to human decision
              </h2>

              <p>
                Gemora combines deterministic validation,
                AI-assisted observations, and final
                Gemologist review.
              </p>
            </div>
          </div>

          <div className="gem-detail-workflow-grid">

            <div
              className={`gem-detail-workflow-step ${
                status !== "Draft"
                  ? "completed"
                  : "active"
              }`}
            >
              <span>01</span>
              <div>◇</div>

              <h3>Listing</h3>

              <p>
                Gemstone information is prepared.
              </p>
            </div>

            <div
              className={`gem-detail-workflow-step ${
                hasImage ? "completed" : ""
              }`}
            >
              <span>02</span>
              <div>▤</div>

              <h3>Evidence</h3>

              <p>
                Image and supporting certificate evidence
                are prepared.
              </p>
            </div>

            <div
              className={`gem-detail-workflow-step ${
                status === "PendingVerification" ||
                status === "Approved" ||
                status === "ChangesRequested" ||
                status === "Rejected"
                  ? "active"
                  : ""
              }`}
            >
              <span>03</span>
              <div>✦</div>

              <h3>
                AI Assistance
              </h3>

              <p>
                Evidence may be analyzed to assist the review.
              </p>
            </div>

            <div
              className={`gem-detail-workflow-step ${
                status === "Approved"
                  ? "completed"
                  : status === "ChangesRequested" ||
                    status === "Rejected"
                  ? "attention"
                  : ""
              }`}
            >
              <span>04</span>
              <div>✓</div>

              <h3>
                Gemologist
              </h3>

              <p>
                A human Gemologist makes the final decision.
              </p>
            </div>

          </div>
        </section>

        {/* CURRENT STATUS */}

        <section
          className={`gem-detail-verification-panel ${statusConfig.className}`}
        >
          <div>
            <p className="gem-detail-panel-label">
              CURRENT LISTING STATUS
            </p>

            <div className="gem-detail-panel-title">
              <span className="gem-detail-status-dot" />

              <h2>
                {statusConfig.label}
              </h2>
            </div>

            <p>
              {statusConfig.message}
            </p>
          </div>

          <div className="gem-detail-panel-actions">
            {canModify && (
              <button
                type="button"
                className="gem-detail-dark-outline-button"
                onClick={() =>
                  navigate(
                    `/seller/listings/${listing.id}/edit`
                  )
                }
              >
                Edit Listing
              </button>
            )}

            {canSubmit && (
              <button
                type="button"
                className="gem-detail-gold-button"
                disabled={submitting}
                onClick={
                  handleSubmitForVerification
                }
              >
                {submitting
                  ? "Submitting..."
                  : status === "ChangesRequested"
                  ? "Resubmit for Verification →"
                  : "Submit for Verification →"}
              </button>
            )}
          </div>
        </section>

        {/* AI NOTICE */}

        <section className="gem-detail-ai-notice">
          <div className="gem-detail-ai-icon">
            ✦
          </div>

          <div>
            <p className="gem-detail-eyebrow">
              HUMAN-REVIEWED AI
            </p>

            <h3>
              AI assists. Gemologists decide.
            </h3>

            <p>
              AI-generated observations are advisory and do
              not independently certify gemstone authenticity,
              treatment, origin, or value. Final verification
              remains a human Gemologist decision.
            </p>
          </div>
        </section>

        {/* DELETE */}

        {canDelete && (
          <section className="gem-detail-danger-zone">
            <div>
              <p className="gem-detail-small-label">
                DRAFT MANAGEMENT
              </p>

              <h3>
                Delete this Draft
              </h3>

              <p>
                Permanently remove this listing.
                This action cannot be undone.
              </p>
            </div>

            <button
              type="button"
              className="gem-detail-delete-button"
              disabled={deleting}
              onClick={handleDelete}
            >
              {deleting
                ? "Deleting..."
                : "Delete Draft"}
            </button>
          </section>
        )}

        {/* SUBMIT MODAL */}

        {showSubmitModal && (
          <div
            className="gem-submit-modal-backdrop"
            onClick={closeSubmitModal}
          >
            <div
              className="gem-submit-modal"
              role="dialog"
              aria-modal="true"
              aria-labelledby="submit-verification-title"
              onClick={(event) =>
                event.stopPropagation()
              }
            >
              <div className="gem-submit-modal-icon">
                ✓
              </div>

              <p className="gem-detail-eyebrow">
                VERIFICATION WORKFLOW
              </p>

              <h2 id="submit-verification-title">
                Submit for Gemologist review?
              </h2>

              <p className="gem-submit-modal-description">
                Your listing will move from seller
                preparation into the verification workflow.
                Editing will be locked while the listing is
                pending review.
              </p>

              <div className="gem-submit-transition">
                <div>
                  <span>
                    CURRENT STAGE
                  </span>

                  <strong>
                    {status === "ChangesRequested"
                      ? "Changes Requested"
                      : preparationStage.label}
                  </strong>
                </div>

                <span className="gem-submit-transition-arrow">
                  →
                </span>

                <div>
                  <span>
                    NEXT STAGE
                  </span>

                  <strong>
                    Pending Review
                  </strong>
                </div>
              </div>

              <div className="gem-submit-modal-checklist">

                <div>
                  <span
                    className={
                      hasImage
                        ? "done"
                        : "missing"
                    }
                  >
                    {hasImage ? "✓" : "!"}
                  </span>

                  <p>
                    <strong>
                      Gemstone image
                    </strong>

                    <small>
                      {hasImage
                        ? "Required evidence attached"
                        : "Required image evidence missing"}
                    </small>
                  </p>
                </div>

                <div>
                  <span
                    className={
                      hasCertificate
                        ? "done"
                        : "optional"
                    }
                  >
                    {hasCertificate
                      ? "✓"
                      : "○"}
                  </span>

                  <p>
                    <strong>
                      Certificate
                    </strong>

                    <small>
                      {hasCertificate
                        ? "Supporting evidence attached"
                        : "Optional evidence not attached"}
                    </small>
                  </p>
                </div>

              </div>

              <div className="gem-submit-modal-note">
                <span>✦</span>

                <p>
                  After submission, AI-assisted analysis may
                  support the review, but the final
                  verification decision remains with a human
                  Gemologist.
                </p>
              </div>

              {submitModalError && (
                <div className="gem-submit-modal-error">
                  <span>!</span>

                  <div>
                    <strong>
                      Submission failed
                    </strong>

                    <p>
                      {submitModalError}
                    </p>
                  </div>
                </div>
              )}

              <div className="gem-submit-modal-actions">
                <button
                  type="button"
                  className="gem-submit-modal-cancel"
                  disabled={submitting}
                  onClick={closeSubmitModal}
                >
                  Keep Editing
                </button>

                <button
                  type="button"
                  className="gem-submit-modal-confirm"
                  disabled={
                    !hasImage ||
                    submitting
                  }
                  onClick={
                    confirmSubmitForVerification
                  }
                >
                  {submitting
                    ? "Submitting to Gemologist..."
                    : status === "ChangesRequested"
                    ? "Resubmit Listing →"
                    : "Submit Listing →"}
                </button>
              </div>

            </div>
          </div>
        )}

      </main>
    </DashboardLayout>
  );
}

export default GemListingDetails;
