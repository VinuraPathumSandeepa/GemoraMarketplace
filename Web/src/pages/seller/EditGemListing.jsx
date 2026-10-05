import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import DashboardLayout from "../../layouts/DashboardLayout";

import gemListingService from "../../services/gemVerification/gemListingService";

import {
  resolveApiAssetUrl,
} from "../../services/api";

import {
  openProtectedCertificate,
} from "../../services/certificateAccess";

// ============================================================
// FILE URL - LOCAL + PRODUCTION
// ============================================================

function getFileUrl(url) {
  return resolveApiAssetUrl(url) || "";
}

// ============================================================
// ERROR MESSAGE
// ============================================================

function getErrorMessage(error, fallback) {
  const response = error?.response?.data;

  if (response?.message) {
    return response.message;
  }

  if (response?.error) {
    return response.error;
  }

  if (response?.errors) {
    const messages =
      Object.values(response.errors).flat();

    if (messages.length > 0) {
      return messages.join("\n");
    }
  }

  return fallback;
}

// ============================================================
// EDIT GEM LISTING
// ============================================================

function EditGemListing() {
  const { id } = useParams();

  const navigate = useNavigate();

  // ==========================================================
  // LISTING STATE
  // ==========================================================

  const [listing, setListing] =
    useState(null);

  const [formData, setFormData] =
    useState({
      title: "",
      description: "",
      gemType: "",
      caratWeight: "",
      color: "",
      clarity: "",
      cut: "",
      price: "",
      certificateNumber: "",
      certificateAuthority: "",
    });

  // ==========================================================
  // FILE STATES
  // ==========================================================

  const [imageFile, setImageFile] =
    useState(null);

  const [
    certificateFile,
    setCertificateFile,
  ] = useState(null);

  const [imagePreview, setImagePreview] =
    useState("");

  // Separate error states ensure a failed stored image
  // does not prevent a newly selected image from displaying.

  const [
    currentImageFailed,
    setCurrentImageFailed,
  ] = useState(false);

  const [
    previewImageFailed,
    setPreviewImageFailed,
  ] = useState(false);

  // ==========================================================
  // PAGE STATES
  // ==========================================================

  const [loading, setLoading] =
    useState(true);

  const [saving, setSaving] =
    useState(false);

  const [
    certificateOpening,
    setCertificateOpening,
  ] = useState(false);

  const [error, setError] =
    useState("");

  const [success, setSuccess] =
    useState("");

  // ==========================================================
  // PERMISSIONS
  // ==========================================================

  const editable =
    listing?.status === "Draft" ||
    listing?.status === "ChangesRequested";

  // ==========================================================
  // CURRENT IMAGE URL
  //
  // Gemstone images are public assets and can continue using
  // resolveApiAssetUrl().
  //
  // Certificates must NOT use this helper because private
  // certificate references are not normal browser URLs.
  // ==========================================================

  const currentImageUrl =
    useMemo(
      () =>
        getFileUrl(
          listing?.primaryImageUrl
        ),
      [listing?.primaryImageUrl]
    );

  const hasCurrentCertificate =
    Boolean(listing?.certificateUrl);

  // ==========================================================
  // RESET STORED IMAGE ERROR WHEN IMAGE CHANGES
  // ==========================================================

  useEffect(() => {
    setCurrentImageFailed(false);
  }, [listing?.primaryImageUrl]);

  // ==========================================================
  // LOCAL IMAGE PREVIEW
  //
  // URL.createObjectURL is used ONLY for local file previews.
  // It is released during cleanup to avoid memory leaks.
  // ==========================================================

  useEffect(() => {
    let objectUrl;

    if (imageFile) {
      objectUrl =
        URL.createObjectURL(imageFile);

      setImagePreview(objectUrl);

      setPreviewImageFailed(false);
    } else {
      setImagePreview("");

      setPreviewImageFailed(false);
    }

    return () => {
      if (objectUrl) {
        URL.revokeObjectURL(
          objectUrl
        );
      }
    };
  }, [imageFile]);

  // ==========================================================
  // LOAD LISTING FROM BACKEND
  // ==========================================================

  useEffect(() => {
    let cancelled = false;

    const loadListing = async () => {
      try {
        setLoading(true);
        setError("");

        const data =
          await gemListingService.getListingById(
            id
          );

        if (cancelled) {
          return;
        }

        setListing(data);

        setFormData({
          title:
            data.title ?? "",

          description:
            data.description ?? "",

          gemType:
            data.gemType ?? "",

          caratWeight:
            data.caratWeight ?? "",

          color:
            data.color ?? "",

          clarity:
            data.clarity ?? "",

          cut:
            data.cut ?? "",

          price:
            data.price ?? "",

          certificateNumber:
            data.certificateNumber ??
            "",

          certificateAuthority:
            data.certificateAuthority ??
            "",
        });
      } catch (err) {
        if (cancelled) {
          return;
        }

        console.error(
          "Failed to load listing:",
          err
        );

        setError(
          getErrorMessage(
            err,
            "We couldn't load this gemstone listing."
          )
        );
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    loadListing();

    return () => {
      cancelled = true;
    };
  }, [id]);

  // ==========================================================
  // FORM CHANGE
  // ==========================================================

  const handleChange = (event) => {
    const { name, value } =
      event.target;

    setFormData((previous) => ({
      ...previous,
      [name]: value,
    }));
  };

  // ==========================================================
  // IMAGE SELECTION
  // ==========================================================

  const handleImageChange = (
    event
  ) => {
    const file =
      event.target.files?.[0];

    if (!file) {
      return;
    }

    const allowedTypes = [
      "image/jpeg",
      "image/png",
      "image/webp",
    ];

    if (
      !allowedTypes.includes(
        file.type
      )
    ) {
      setError(
        "Please select a JPG, PNG, or WebP gemstone image."
      );

      event.target.value = "";

      return;
    }

    if (
      file.size >
      5 * 1024 * 1024
    ) {
      setError(
        "The gemstone image must be 5 MB or smaller."
      );

      event.target.value = "";

      return;
    }

    setError("");
    setSuccess("");

    setPreviewImageFailed(false);

    setImageFile(file);

    // Allows selecting the same file again after Undo.
    event.target.value = "";
  };

  // ==========================================================
  // CERTIFICATE SELECTION
  // ==========================================================

  const handleCertificateChange = (
    event
  ) => {
    const file =
      event.target.files?.[0];

    if (!file) {
      return;
    }

    const allowedTypes = [
      "application/pdf",
      "image/jpeg",
      "image/png",
    ];

    if (
      !allowedTypes.includes(
        file.type
      )
    ) {
      setError(
        "Please select a PDF, JPG, or PNG certificate file."
      );

      event.target.value = "";

      return;
    }

    if (
      file.size >
      10 * 1024 * 1024
    ) {
      setError(
        "The certificate file must be 10 MB or smaller."
      );

      event.target.value = "";

      return;
    }

    setError("");
    setSuccess("");

    setCertificateFile(file);

    event.target.value = "";
  };

  // ==========================================================
  // UNDO IMAGE REPLACEMENT
  // ==========================================================

  const undoImageReplacement =
    () => {
      setImageFile(null);

      setPreviewImageFailed(false);

      setError("");
    };

  // ==========================================================
  // UNDO CERTIFICATE REPLACEMENT
  // ==========================================================

  const undoCertificateReplacement =
    () => {
      setCertificateFile(null);

      setError("");
    };

  // ==========================================================
  // OPEN CURRENT PROTECTED CERTIFICATE
  //
  // Works with:
  //
  // - legacy local certificates
  // - future private Supabase certificates
  //
  // The backend checks Seller ownership before returning
  // certificate access.
  // ==========================================================

  const handleOpenCertificate =
    async (event) => {
      event?.preventDefault();

      if (
        certificateOpening ||
        !listing?.certificateUrl
      ) {
        return;
      }

      try {
        setError("");

        setCertificateOpening(true);

        await openProtectedCertificate(
          listing.id
        );
      } catch (err) {
        console.error(
          "Certificate access failed:",
          err
        );

        setError(
          getErrorMessage(
            err,
            "The current certificate could not be opened."
          )
        );
      } finally {
        setCertificateOpening(false);
      }
    };

  // ==========================================================
  // SAVE LISTING
  // ==========================================================

  const handleSubmit = async (
    event
  ) => {
    event.preventDefault();

    if (!editable) {
      setError(
        "This listing can no longer be edited because it has already entered the verification workflow."
      );

      return;
    }

    if (saving) {
      return;
    }

    try {
      setSaving(true);

      setError("");
      setSuccess("");

      // ------------------------------------------------------
      // PREPARE PAYLOAD
      // ------------------------------------------------------

      const payload = {
        title:
          formData.title.trim(),

        description:
          formData.description.trim(),

        gemType:
          formData.gemType.trim(),

        caratWeight:
          Number(
            formData.caratWeight
          ),

        color:
          formData.color.trim(),

        clarity:
          formData.clarity.trim(),

        cut:
          formData.cut.trim(),

        price:
          Number(
            formData.price
          ),

        certificateNumber:
          formData.certificateNumber
            .trim() || null,

        certificateAuthority:
          formData.certificateAuthority
            .trim() || null,
      };

      // ------------------------------------------------------
      // UPDATE LISTING INFORMATION
      // ------------------------------------------------------

      await gemListingService.updateListing(
        id,
        payload
      );

      // ------------------------------------------------------
      // UPLOAD NEW GEMSTONE IMAGE IF SELECTED
      // ------------------------------------------------------

      if (imageFile) {
        setSuccess(
          "Listing information saved. Uploading gemstone image..."
        );

        await gemListingService.uploadImage(
          id,
          imageFile
        );
      }

      // ------------------------------------------------------
      // UPLOAD NEW CERTIFICATE IF SELECTED
      // ------------------------------------------------------

      if (certificateFile) {
        setSuccess(
          "Uploading the selected certificate..."
        );

        await gemListingService.uploadCertificate(
          id,
          certificateFile
        );
      }

      // ------------------------------------------------------
      // SUCCESS
      // ------------------------------------------------------

      setSuccess(
        "Changes saved successfully."
      );

      navigate(
        `/seller/listings/${id}`
      );
    } catch (err) {
      console.error(
        "Failed to update listing:",
        err
      );

      setError(
        getErrorMessage(
          err,
          "The update could not be completed. Some listing information may have been saved. Please review the listing and try again."
        )
      );
    } finally {
      setSaving(false);
    }
  };

  // ==========================================================
  // LOADING SCREEN
  // ==========================================================

  if (loading) {
    return (
      <DashboardLayout>
        <main className="gem-form-page">
          <div className="gem-form-loading">
            <span className="gem-form-loader" />

            <p>
              Loading gemstone listing...
            </p>
          </div>
        </main>
      </DashboardLayout>
    );
  }

  // ==========================================================
  // ERROR SCREEN
  // ==========================================================

  if (!listing) {
    return (
      <DashboardLayout>
        <main className="gem-form-page">
          <div className="gem-form-state-card">
            <span className="gem-form-state-icon">
              !
            </span>

            <h2>
              Unable to load listing
            </h2>

            <p>
              {error ||
                "The requested gemstone listing could not be found."}
            </p>

            <button
              type="button"
              className="gem-secondary-button"
              onClick={() =>
                navigate(
                  "/seller/listings"
                )
              }
            >
              Back to My Listings
            </button>
          </div>
        </main>
      </DashboardLayout>
    );
  }

  // ==========================================================
  // LOCKED LISTING
  // ==========================================================

  if (!editable) {
    return (
      <DashboardLayout>
        <main className="gem-form-page">
          <div className="gem-form-state-card">
            <span className="gem-form-state-icon">
              ◇
            </span>

            <p className="gem-form-eyebrow">
              LISTING LOCKED
            </p>

            <h2>
              This listing is not editable
            </h2>

            <p>
              The current status is{" "}

              <strong>
                {listing.status}
              </strong>
              .

              Only Draft listings or
              listings returned with
              Changes Requested can be
              edited.
            </p>

            <div className="gem-form-state-actions">
              <button
                type="button"
                className="gem-secondary-button"
                onClick={() =>
                  navigate(
                    `/seller/listings/${id}`
                  )
                }
              >
                View Listing
              </button>

              <button
                type="button"
                className="gem-primary-button"
                onClick={() =>
                  navigate(
                    "/seller/listings"
                  )
                }
              >
                My Listings
              </button>
            </div>
          </div>
        </main>
      </DashboardLayout>
    );
  }

  // ==========================================================
  // IMAGE PRESENTATION
  // ==========================================================

  const showNewImage =
    Boolean(imagePreview) &&
    !previewImageFailed;

  const showCurrentImage =
    !imagePreview &&
    Boolean(currentImageUrl) &&
    !currentImageFailed;

  const hasImageDisplay =
    showNewImage ||
    showCurrentImage;

  // ==========================================================
  // MAIN PAGE
  // ==========================================================

  return (
    <DashboardLayout>
      <main className="gem-form-page">
        <div className="gem-form-container">

          {/* ==================================================
              BACK BUTTON
              ================================================== */}

          <button
            type="button"
            className="gem-form-back"
            onClick={() =>
              navigate(
                `/seller/listings/${id}`
              )
            }
          >
            ← Listing Details
          </button>

          {/* ==================================================
              PAGE HEADER
              ================================================== */}

          <header className="gem-form-header">
            <p className="gem-form-eyebrow">
              EDIT GEMSTONE LISTING
            </p>

            <h1>
              Edit Gem Listing
            </h1>

            <p className="gem-form-subtitle">
              Update gemstone information
              and verification evidence
              before sending the listing
              for Gemologist review.
            </p>
          </header>

          {/* ==================================================
              CHANGES REQUESTED BANNER
              ================================================== */}

          {listing.status ===
            "ChangesRequested" && (
            <section className="gem-changes-requested-banner">
              <div className="gem-changes-requested-icon">
                !
              </div>

              <div>
                <span>
                  GEMOLOGIST REVIEW
                </span>

                <h3>
                  Changes requested
                </h3>

                <p>
                  Review the requested
                  corrections, update the
                  listing information or
                  evidence, save your
                  changes, and then
                  resubmit the listing for
                  verification.
                </p>
              </div>
            </section>
          )}

          {/* ==================================================
              ERROR MESSAGE
              ================================================== */}

          {error && (
            <div className="gem-form-message gem-form-message-error">
              <span>
                !
              </span>

              <p>
                {error}
              </p>
            </div>
          )}

          {/* ==================================================
              SUCCESS MESSAGE
              ================================================== */}

          {success && (
            <div className="gem-form-message gem-form-message-success">
              <span>
                ✓
              </span>

              <p>
                {success}
              </p>
            </div>
          )}

          {/* ==================================================
              EDIT FORM
              ================================================== */}

          <form onSubmit={handleSubmit}>

            {/* ================================================
                SECTION 01 - GEMSTONE INFORMATION
                ================================================ */}

            <section className="gem-form-section">
              <div className="gem-form-section-number">
                01
              </div>

              <div className="gem-form-section-heading">
                <h2>
                  Gemstone Information
                </h2>

                <p>
                  Keep the primary
                  marketplace information
                  accurate and easy to
                  understand.
                </p>
              </div>

              <div className="gem-form-fields">

                {/* TITLE */}

                <label className="gem-form-field gem-form-field-full">
                  <span>
                    LISTING TITLE *
                  </span>

                  <input
                    type="text"
                    name="title"
                    value={
                      formData.title
                    }
                    onChange={
                      handleChange
                    }
                    required
                    maxLength={150}
                  />
                </label>

                {/* DESCRIPTION */}

                <label className="gem-form-field gem-form-field-full">
                  <span>
                    DESCRIPTION *
                  </span>

                  <textarea
                    name="description"
                    value={
                      formData.description
                    }
                    onChange={
                      handleChange
                    }
                    required
                    rows={5}
                  />
                </label>

                {/* GEM TYPE */}

                <label className="gem-form-field">
                  <span>
                    GEM TYPE *
                  </span>

                  <input
                    type="text"
                    name="gemType"
                    value={
                      formData.gemType
                    }
                    onChange={
                      handleChange
                    }
                    required
                  />
                </label>

                {/* CARAT WEIGHT */}

                <label className="gem-form-field">
                  <span>
                    CARAT WEIGHT *
                  </span>

                  <div className="gem-input-suffix">
                    <input
                      type="number"
                      name="caratWeight"
                      value={
                        formData.caratWeight
                      }
                      onChange={
                        handleChange
                      }
                      min="0.01"
                      step="0.01"
                      required
                    />

                    <span>
                      ct
                    </span>
                  </div>
                </label>
              </div>
            </section>

            {/* ================================================
                SECTION 02 - GEM CHARACTERISTICS
                ================================================ */}

            <section className="gem-form-section">
              <div className="gem-form-section-number">
                02
              </div>

              <div className="gem-form-section-heading">
                <h2>
                  Gem Characteristics
                </h2>

                <p>
                  Update the visible and
                  declared characteristics
                  used during listing
                  review.
                </p>
              </div>

              <div className="gem-form-fields gem-form-fields-three">

                {/* COLOR */}

                <label className="gem-form-field">
                  <span>
                    COLOR *
                  </span>

                  <input
                    type="text"
                    name="color"
                    value={
                      formData.color
                    }
                    onChange={
                      handleChange
                    }
                    required
                  />
                </label>

                {/* CLARITY */}

                <label className="gem-form-field">
                  <span>
                    CLARITY *
                  </span>

                  <input
                    type="text"
                    name="clarity"
                    value={
                      formData.clarity
                    }
                    onChange={
                      handleChange
                    }
                    required
                  />
                </label>

                {/* CUT */}

                <label className="gem-form-field">
                  <span>
                    CUT *
                  </span>

                  <input
                    type="text"
                    name="cut"
                    value={
                      formData.cut
                    }
                    onChange={
                      handleChange
                    }
                    required
                  />
                </label>
              </div>
            </section>

            {/* ================================================
                SECTION 03 - LISTING PRICE
                ================================================ */}

            <section className="gem-form-section">
              <div className="gem-form-section-number">
                03
              </div>

              <div className="gem-form-section-heading">
                <h2>
                  Listing Price
                </h2>

                <p>
                  Update the marketplace
                  asking price for this
                  gemstone.
                </p>
              </div>

              <div className="gem-form-price-row">
                <label className="gem-form-field">
                  <span>
                    PRICE *
                  </span>

                  <div className="gem-input-prefix">
                    <span>
                      LKR
                    </span>

                    <input
                      type="number"
                      name="price"
                      value={
                        formData.price
                      }
                      onChange={
                        handleChange
                      }
                      min="0"
                      step="0.01"
                      required
                    />
                  </div>
                </label>
              </div>
            </section>

            {/* ================================================
                SECTION 04 - CERTIFICATE INFORMATION
                ================================================ */}

            <section className="gem-form-section">
              <div className="gem-form-section-number">
                04
              </div>

              <div className="gem-form-section-heading">
                <h2>
                  Certificate Information
                </h2>

                <p>
                  Keep the structured
                  certificate information
                  consistent with the
                  supporting evidence.
                </p>
              </div>

              <div className="gem-form-fields">

                {/* CERTIFICATE NUMBER */}

                <label className="gem-form-field">
                  <span>
                    CERTIFICATE NUMBER
                  </span>

                  <input
                    type="text"
                    name="certificateNumber"
                    value={
                      formData.certificateNumber
                    }
                    onChange={
                      handleChange
                    }
                  />
                </label>

                {/* CERTIFICATE AUTHORITY */}

                <label className="gem-form-field">
                  <span>
                    CERTIFICATE AUTHORITY
                  </span>

                  <input
                    type="text"
                    name="certificateAuthority"
                    value={
                      formData.certificateAuthority
                    }
                    onChange={
                      handleChange
                    }
                  />
                </label>
              </div>
            </section>

            {/* ================================================
                SECTION 05 - VERIFICATION EVIDENCE
                ================================================ */}

            <section className="gem-form-section gem-evidence-section">
              <div className="gem-form-section-number">
                05
              </div>

              <div className="gem-form-section-heading">
                <h2>
                  Verification Evidence
                </h2>

                <p>
                  Review the existing
                  evidence or select
                  replacement files.
                  Replacements are uploaded
                  when you save.
                </p>
              </div>

              <div className="gem-edit-evidence-grid">

                {/* ============================================
                    GEMSTONE IMAGE CARD
                    ============================================ */}

                <article className="gem-edit-evidence-card">

                  {/* CARD HEADING */}

                  <div className="gem-evidence-card-heading">
                    <div className="gem-evidence-card-icon">
                      ◇
                    </div>

                    <div>
                      <h3>
                        Gemstone Image
                      </h3>

                      <p>
                        JPG, PNG or WebP ·
                        Maximum 5 MB
                      </p>
                    </div>
                  </div>

                  {/* ========================================
                      IMAGE PREVIEW
                      ======================================== */}

                  <div className="gem-edit-image-preview">

                    {/* NEW SELECTED IMAGE */}

                    {showNewImage ? (
                      <img
                        src={
                          imagePreview
                        }
                        alt="New gemstone preview"
                        onError={() =>
                          setPreviewImageFailed(
                            true
                          )
                        }
                      />

                    /* CURRENT SAVED IMAGE */

                    ) : showCurrentImage ? (
                      <img
                        src={
                          currentImageUrl
                        }
                        alt={
                          listing.title ||
                          "Current gemstone photograph"
                        }
                        onError={() =>
                          setCurrentImageFailed(
                            true
                          )
                        }
                      />

                    /* FALLBACK */

                    ) : (
                      <div className="gem-edit-image-empty">
                        <span>
                          ◇
                        </span>

                        <p>
                          {imagePreview &&
                          previewImageFailed
                            ? "Selected image preview unavailable"
                            : currentImageUrl &&
                              currentImageFailed
                            ? "Stored gemstone image unavailable"
                            : "No gemstone image"}
                        </p>
                      </div>
                    )}

                    {/* IMAGE LABEL */}

                    {hasImageDisplay && (
                      <div className="gem-edit-image-label">
                        {showNewImage
                          ? "NEW IMAGE"
                          : "CURRENT IMAGE"}
                      </div>
                    )}
                  </div>

                  {/* ========================================
                      IMAGE INFORMATION
                      ======================================== */}

                  <div className="gem-edit-file-information">
                    <div>
                      <span>
                        {imageFile
                          ? "Replacement selected"
                          : listing.primaryImageUrl
                          ? currentImageFailed
                            ? "Stored image unavailable"
                            : "Current evidence"
                          : "No image uploaded"}
                      </span>

                      <strong>
                        {imageFile
                          ? imageFile.name
                          : listing.primaryImageUrl
                          ? "Gemstone photograph"
                          : "Select an image below"}
                      </strong>
                    </div>

                    {/* UNDO REPLACEMENT */}

                    {imageFile && (
                      <button
                        type="button"
                        onClick={
                          undoImageReplacement
                        }
                      >
                        Undo
                      </button>
                    )}
                  </div>

                  {/* ========================================
                      IMAGE FILE PICKER
                      ======================================== */}

                  <label className="gem-file-select-button">
                    <input
                      type="file"
                      accept=".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp"
                      onChange={
                        handleImageChange
                      }
                    />

                    <span>
                      {listing.primaryImageUrl
                        ? "Replace Image"
                        : "Choose Image"}
                    </span>

                    <strong>
                      →
                    </strong>
                  </label>
                </article>

                {/* ============================================
                    CERTIFICATE CARD
                    ============================================ */}

                <article className="gem-edit-evidence-card">

                  {/* CARD HEADING */}

                  <div className="gem-evidence-card-heading">
                    <div className="gem-evidence-card-icon">
                      ▤
                    </div>

                    <div>
                      <h3>
                        Certificate File
                      </h3>

                      <p>
                        PDF, JPG or PNG ·
                        Maximum 10 MB
                      </p>
                    </div>
                  </div>

                  {/* ========================================
                      CERTIFICATE PREVIEW
                      ======================================== */}

                  <div className="gem-edit-certificate-preview">
                    <div className="gem-edit-certificate-symbol">
                      PDF
                    </div>

                    <div>
                      <span>
                        {certificateFile
                          ? "NEW CERTIFICATE"
                          : hasCurrentCertificate
                          ? "CURRENT CERTIFICATE"
                          : "CERTIFICATE EVIDENCE"}
                      </span>

                      <h4>
                        {certificateFile
                          ? certificateFile.name
                          : listing.certificateNumber ||
                            "No certificate uploaded"}
                      </h4>

                      <p>
                        {certificateFile
                          ? `${(
                              certificateFile.size /
                              1024 /
                              1024
                            ).toFixed(
                              2
                            )} MB`
                          : listing.certificateAuthority ||
                            "Supporting certificate can be added below."}
                      </p>
                    </div>
                  </div>

                  {/* ========================================
                      CERTIFICATE ACTIONS
                      ======================================== */}

                  <div className="gem-certificate-actions">

                    {/* EXISTING PROTECTED CERTIFICATE */}

                    {hasCurrentCertificate &&
                      !certificateFile && (
                        <a
                          href={`/api/GemCertificates/listings/${listing.id}/access`}
                          className="gem-view-certificate-button"
                          onClick={
                            handleOpenCertificate
                          }
                          aria-disabled={
                            certificateOpening
                          }
                        >
                          {certificateOpening
                            ? "Opening Certificate..."
                            : "View Current Certificate"}
                        </a>
                      )}

                    {/* UNDO CERTIFICATE REPLACEMENT */}

                    {certificateFile && (
                      <button
                        type="button"
                        className="gem-undo-file-button"
                        onClick={
                          undoCertificateReplacement
                        }
                      >
                        Undo Replacement
                      </button>
                    )}
                  </div>

                  {/* ========================================
                      CERTIFICATE FILE PICKER
                      ======================================== */}

                  <label className="gem-file-select-button">
                    <input
                      type="file"
                      accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"
                      onChange={
                        handleCertificateChange
                      }
                    />

                    <span>
                      {hasCurrentCertificate
                        ? "Replace Certificate"
                        : "Choose Certificate"}
                    </span>

                    <strong>
                      →
                    </strong>
                  </label>
                </article>
              </div>

              {/* ============================================
                  AI EVIDENCE NOTICE
                  ============================================ */}

              <div className="gem-ai-evidence-note">
                <span>
                  ✦
                </span>

                <div>
                  <strong>
                    Evidence supports
                    AI-assisted review
                  </strong>

                  <p>
                    Gemora may analyze the
                    submitted gemstone
                    image and listing
                    information to assist
                    a Gemologist. AI
                    observations are
                    advisory; the final
                    verification decision
                    remains human-reviewed.
                  </p>
                </div>
              </div>
            </section>

            {/* ================================================
                SAVE AREA
                ================================================ */}

            <section className="gem-edit-save-panel">
              <div>
                <span>
                  LISTING STATUS
                </span>

                <h3>
                  {listing.status ===
                  "ChangesRequested"
                    ? "Changes Requested"
                    : "Draft"}
                </h3>

                <p>
                  Saving updates the
                  listing but does not
                  submit it for
                  verification.
                </p>
              </div>

              <div className="gem-edit-save-actions">

                {/* CANCEL */}

                <button
                  type="button"
                  className="gem-edit-cancel-button"
                  disabled={saving}
                  onClick={() =>
                    navigate(
                      `/seller/listings/${id}`
                    )
                  }
                >
                  Cancel
                </button>

                {/* SAVE */}

                <button
                  type="submit"
                  className="gem-edit-save-button"
                  disabled={saving}
                >
                  {saving ? (
                    <>
                      <span className="gem-button-spinner" />
                      Saving...
                    </>
                  ) : (
                    <>
                      Save Changes
                      <span>
                        →
                      </span>
                    </>
                  )}
                </button>
              </div>
            </section>
          </form>
        </div>
      </main>
    </DashboardLayout>
  );
}

export default EditGemListing;