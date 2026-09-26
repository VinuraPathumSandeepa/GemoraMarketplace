import { useEffect, useState } from "react";
import {
  Link,
  useNavigate,
  useParams,
} from "react-router-dom";

import gemListingService from "../../services/gemVerification/gemListingService";

const API_ORIGIN = "http://localhost:5198";

function GemListingDetails() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [listing, setListing] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const [imageFile, setImageFile] = useState(null);
  const [certificateFile, setCertificateFile] = useState(null);

  const [uploadingImage, setUploadingImage] = useState(false);
  const [uploadingCertificate, setUploadingCertificate] =
    useState(false);

  const [submitting, setSubmitting] = useState(false);
  const [deleting, setDeleting] = useState(false);

  // ============================================================
  // LOAD LISTING
  // ============================================================

  const loadListing = async () => {
    try {
      setLoading(true);
      setError("");

      const data =
        await gemListingService.getListingById(id);

      setListing(data);
    } catch (err) {
      console.error(err);

      setError(
        err.response?.data?.message ||
          "Unable to load this gemstone listing."
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadListing();
  }, [id]);

  // ============================================================
  // WORKFLOW PERMISSIONS
  // ============================================================

  const canModify =
    listing?.status === "Draft" ||
    listing?.status === "ChangesRequested";

  // ============================================================
  // IMAGE UPLOAD
  // ============================================================

  const handleImageUpload = async () => {
    if (!imageFile) {
      setError(
        "Please select a gemstone image first."
      );
      return;
    }

    try {
      setUploadingImage(true);
      setError("");
      setSuccess("");

      const updated =
        await gemListingService.uploadImage(
          id,
          imageFile
        );

      setListing(updated);
      setImageFile(null);

      setSuccess(
        "Gemstone image uploaded successfully."
      );
    } catch (err) {
      console.error(err);

      setError(
        err.response?.data?.message ||
          "Unable to upload the gemstone image."
      );
    } finally {
      setUploadingImage(false);
    }
  };

  // ============================================================
  // CERTIFICATE UPLOAD
  // ============================================================

  const handleCertificateUpload = async () => {
    if (!certificateFile) {
      setError(
        "Please select a certificate file first."
      );
      return;
    }

    try {
      setUploadingCertificate(true);
      setError("");
      setSuccess("");

      const updated =
        await gemListingService.uploadCertificate(
          id,
          certificateFile
        );

      setListing(updated);
      setCertificateFile(null);

      setSuccess(
        "Certificate uploaded successfully."
      );
    } catch (err) {
      console.error(err);

      setError(
        err.response?.data?.message ||
          "Unable to upload the certificate."
      );
    } finally {
      setUploadingCertificate(false);
    }
  };

  // ============================================================
  // SUBMIT / RESUBMIT FOR VERIFICATION
  // ============================================================

  const handleSubmitForVerification = async () => {
    const message =
      listing.status === "ChangesRequested"
        ? "Resubmit this gemstone listing for Gemologist verification?"
        : "Submit this gemstone listing for Gemologist verification? You will not be able to edit the listing while verification is pending.";

    const confirmed =
      window.confirm(message);

    if (!confirmed) {
      return;
    }

    try {
      setSubmitting(true);
      setError("");
      setSuccess("");

      const updated =
        await gemListingService.submitListing(id);

      setListing(updated);

      setSuccess(
        listing.status === "ChangesRequested"
          ? "Listing resubmitted for Gemologist verification."
          : "Listing submitted for Gemologist verification."
      );
    } catch (err) {
      console.error(err);

      setError(
        err.response?.data?.message ||
          "Unable to submit the listing for verification."
      );
    } finally {
      setSubmitting(false);
    }
  };

  // ============================================================
  // DELETE DRAFT
  // ============================================================

  const handleDelete = async () => {
    const confirmed = window.confirm(
      "Are you sure you want to permanently delete this draft listing?"
    );

    if (!confirmed) {
      return;
    }

    try {
      setDeleting(true);
      setError("");
      setSuccess("");

      await gemListingService.deleteListing(id);

      navigate("/seller/listings");
    } catch (err) {
      console.error(err);

      setError(
        err.response?.data?.message ||
          "Unable to delete this listing."
      );
    } finally {
      setDeleting(false);
    }
  };

  // ============================================================
  // STATUS DISPLAY
  // ============================================================

  const formatStatus = (status) => {
    switch (status) {
      case "PendingVerification":
        return "Pending Verification";

      case "ChangesRequested":
        return "Changes Requested";

      case "Approved":
        return "Approved";

      case "Rejected":
        return "Rejected";

      case "Draft":
        return "Draft";

      default:
        return status || "Unknown";
    }
  };

  const getStatusClass = (status) => {
    switch (status) {
      case "Approved":
        return "approved";

      case "PendingVerification":
        return "pending";

      case "ChangesRequested":
        return "changes";

      case "Rejected":
        return "rejected";

      default:
        return "draft";
    }
  };

  // ============================================================
  // LOADING
  // ============================================================

  if (loading) {
    return (
      <div className="gem-details-page">
        <p>Loading gemstone listing...</p>
      </div>
    );
  }

  // ============================================================
  // LOAD ERROR
  // ============================================================

  if (error && !listing) {
    return (
      <div className="gem-details-page">
        <div className="error-message">
          {error}
        </div>

        <Link
          to="/seller/listings"
          className="secondary-button"
        >
          Back to Listings
        </Link>
      </div>
    );
  }

  if (!listing) {
    return null;
  }

  return (
    <div className="gem-details-page">
      <div className="gem-details-container">

        {/* ====================================================
            HEADER
            ==================================================== */}

        <div className="gem-details-header">
          <div>
            <Link
              to="/seller/listings"
              className="details-back-link"
            >
              ← My Gem Listings
            </Link>

            <h1>{listing.title}</h1>

            <span
              className={`status ${getStatusClass(
                listing.status
              )}`}
            >
              {formatStatus(listing.status)}
            </span>
          </div>

          {canModify && (
            <Link
              to={`/seller/listings/${listing.id}/edit`}
              className="secondary-button"
            >
              Edit Listing
            </Link>
          )}
        </div>

        {/* ====================================================
            MESSAGES
            ==================================================== */}

        {error && (
          <div className="error-message">
            {error}
          </div>
        )}

        {success && (
          <div className="success-message">
            {success}
          </div>
        )}

        <div className="gem-details-grid">

          {/* ==================================================
              GEMSTONE IMAGE
              ================================================== */}

          <section className="details-card">
            <h2>Gemstone Image</h2>

            {listing.primaryImageUrl ? (
              <img
                src={`${API_ORIGIN}${listing.primaryImageUrl}`}
                alt={listing.title}
                className="details-gem-image"
                onError={(event) => {
                  event.currentTarget.style.display =
                    "none";
                }}
              />
            ) : (
              <div className="details-image-placeholder">
                No gemstone image uploaded
              </div>
            )}

            {canModify && (
              <div className="upload-area">
                <label>
                  Upload gemstone photograph
                </label>

                <input
                  type="file"
                  accept=".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp"
                  onChange={(event) =>
                    setImageFile(
                      event.target.files?.[0] ||
                        null
                    )
                  }
                />

                <small>
                  JPG, JPEG, PNG or WEBP.
                  Maximum 5 MB.
                </small>

                <button
                  type="button"
                  className="primary-button"
                  disabled={
                    !imageFile ||
                    uploadingImage
                  }
                  onClick={handleImageUpload}
                >
                  {uploadingImage
                    ? "Uploading..."
                    : listing.primaryImageUrl
                      ? "Replace Image"
                      : "Upload Image"}
                </button>
              </div>
            )}
          </section>

          {/* ==================================================
              GEM INFORMATION
              ================================================== */}

          <section className="details-card">
            <h2>Gem Information</h2>

            <div className="details-information">
              <p>
                <strong>Gem Type:</strong>{" "}
                {listing.gemType || "—"}
              </p>

              <p>
                <strong>Carat Weight:</strong>{" "}
                {listing.caratWeight ?? "—"}
              </p>

              <p>
                <strong>Color:</strong>{" "}
                {listing.color || "—"}
              </p>

              <p>
                <strong>Clarity:</strong>{" "}
                {listing.clarity || "—"}
              </p>

              <p>
                <strong>Cut:</strong>{" "}
                {listing.cut || "—"}
              </p>

              <p>
                <strong>Price:</strong>{" "}
                {listing.currency || "LKR"}{" "}
                {Number(
                  listing.price || 0
                ).toLocaleString()}
              </p>
            </div>

            {listing.description && (
              <>
                <h3>Description</h3>

                <p className="details-description">
                  {listing.description}
                </p>
              </>
            )}
          </section>

          {/* ==================================================
              CERTIFICATE
              ================================================== */}

          <section className="details-card">
            <h2>Certificate Evidence</h2>

            <div className="details-information">
              <p>
                <strong>
                  Certificate Number:
                </strong>{" "}
                {listing.certificateNumber ||
                  "Not provided"}
              </p>

              <p>
                <strong>
                  Certificate Authority:
                </strong>{" "}
                {listing.certificateAuthority ||
                  "Not provided"}
              </p>
            </div>

            {listing.certificateUrl ? (
              <a
                href={`${API_ORIGIN}${listing.certificateUrl}`}
                target="_blank"
                rel="noreferrer"
                className="secondary-button"
              >
                View Uploaded Certificate
              </a>
            ) : (
              <p>
                No certificate file uploaded.
              </p>
            )}

            {canModify && (
              <div className="upload-area">
                <label>
                  Upload certificate
                </label>

                <input
                  type="file"
                  accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"
                  onChange={(event) =>
                    setCertificateFile(
                      event.target.files?.[0] ||
                        null
                    )
                  }
                />

                <small>
                  PDF, JPG, JPEG or PNG.
                  Maximum 10 MB.
                </small>

                <button
                  type="button"
                  className="primary-button"
                  disabled={
                    !certificateFile ||
                    uploadingCertificate
                  }
                  onClick={
                    handleCertificateUpload
                  }
                >
                  {uploadingCertificate
                    ? "Uploading..."
                    : listing.certificateUrl
                      ? "Replace Certificate"
                      : "Upload Certificate"}
                </button>
              </div>
            )}
          </section>

          {/* ==================================================
              VERIFICATION
              ================================================== */}

          <section className="details-card">
            <h2>Verification</h2>

            <p>
              Current status:{" "}
              <strong>
                {formatStatus(listing.status)}
              </strong>
            </p>

            {listing.status === "Draft" && (
              <div className="verification-info">
                This listing is currently a draft.
                Upload the gemstone evidence and
                submit it when it is ready for
                Gemologist review.
              </div>
            )}

            {listing.status ===
              "PendingVerification" && (
              <div className="verification-info">
                This listing is currently waiting
                for Gemologist review. Editing and
                evidence replacement are locked.
              </div>
            )}

            {listing.status ===
              "ChangesRequested" && (
              <div className="verification-warning">
                The Gemologist requested changes.
                Update the listing information or
                evidence and resubmit it for
                verification.
              </div>
            )}

            {listing.status === "Approved" && (
              <div className="verification-success">
                This gemstone listing has been
                approved by a Gemologist.
              </div>
            )}

            {listing.status === "Rejected" && (
              <div className="verification-warning">
                This gemstone listing was rejected
                during Gemologist review.
              </div>
            )}

            {canModify && (
              <button
                type="button"
                className="primary-button submit-verification-button"
                disabled={submitting}
                onClick={
                  handleSubmitForVerification
                }
              >
                {submitting
                  ? "Submitting..."
                  : listing.status ===
                      "ChangesRequested"
                    ? "Resubmit for Verification"
                    : "Submit for Verification"}
              </button>
            )}
          </section>
        </div>

        {/* ====================================================
            DELETE DRAFT
            ==================================================== */}

        {listing.status === "Draft" && (
          <div className="details-danger-zone">
            <div>
              <h3>Delete Draft</h3>

              <p>
                Permanently remove this draft
                listing and its locally stored
                evidence.
              </p>
            </div>

            <button
              type="button"
              className="danger-button"
              disabled={deleting}
              onClick={handleDelete}
            >
              {deleting
                ? "Deleting..."
                : "Delete Listing"}
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

export default GemListingDetails;