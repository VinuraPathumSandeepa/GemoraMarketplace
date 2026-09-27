import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import DashboardLayout from "../../layouts/DashboardLayout";
import gemListingService from "../../services/gemVerification/gemListingService";

const API_ORIGIN = "http://localhost:5198";

function getFileUrl(url) {
  if (!url) return "";
  if (url.startsWith("http://") || url.startsWith("https://")) {
    return url;
  }

  return `${API_ORIGIN}${url.startsWith("/") ? "" : "/"}${url}`;
}

function getErrorMessage(error, fallback) {
  return (
    error?.response?.data?.message ||
    error?.response?.data?.error ||
    fallback
  );
}

function EditGemListing() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [listing, setListing] = useState(null);

  const [formData, setFormData] = useState({
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

  const [imageFile, setImageFile] = useState(null);
  const [certificateFile, setCertificateFile] = useState(null);

  const [imagePreview, setImagePreview] = useState("");
  const [imageBroken, setImageBroken] = useState(false);

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const editable =
    listing?.status === "Draft" ||
    listing?.status === "ChangesRequested";

  const currentImageUrl = useMemo(
    () => getFileUrl(listing?.primaryImageUrl),
    [listing?.primaryImageUrl]
  );

  const currentCertificateUrl = useMemo(
    () => getFileUrl(listing?.certificateUrl),
    [listing?.certificateUrl]
  );

  useEffect(() => {
    let objectUrl;

    if (imageFile) {
      objectUrl = URL.createObjectURL(imageFile);
      setImagePreview(objectUrl);
      setImageBroken(false);
    } else {
      setImagePreview("");
    }

    return () => {
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
      }
    };
  }, [imageFile]);

  useEffect(() => {
    const loadListing = async () => {
      try {
        setLoading(true);
        setError("");

        const data = await gemListingService.getListingById(id);

        setListing(data);

        setFormData({
          title: data.title ?? "",
          description: data.description ?? "",
          gemType: data.gemType ?? "",
          caratWeight: data.caratWeight ?? "",
          color: data.color ?? "",
          clarity: data.clarity ?? "",
          cut: data.cut ?? "",
          price: data.price ?? "",
          certificateNumber: data.certificateNumber ?? "",
          certificateAuthority: data.certificateAuthority ?? "",
        });
      } catch (err) {
        console.error("Failed to load listing:", err);

        setError(
          getErrorMessage(
            err,
            "We couldn't load this gemstone listing."
          )
        );
      } finally {
        setLoading(false);
      }
    };

    loadListing();
  }, [id]);

  const handleChange = (event) => {
    const { name, value } = event.target;

    setFormData((previous) => ({
      ...previous,
      [name]: value,
    }));
  };

  const handleImageChange = (event) => {
    const file = event.target.files?.[0];

    if (!file) return;

    const allowedTypes = [
      "image/jpeg",
      "image/png",
      "image/webp",
    ];

    if (!allowedTypes.includes(file.type)) {
      setError("Please select a JPG, PNG, or WebP gemstone image.");
      event.target.value = "";
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      setError("The gemstone image must be 5 MB or smaller.");
      event.target.value = "";
      return;
    }

    setError("");
    setImageFile(file);
  };

  const handleCertificateChange = (event) => {
    const file = event.target.files?.[0];

    if (!file) return;

    const allowedTypes = [
      "application/pdf",
      "image/jpeg",
      "image/png",
    ];

    if (!allowedTypes.includes(file.type)) {
      setError(
        "Please select a PDF, JPG, or PNG certificate file."
      );
      event.target.value = "";
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      setError("The certificate file must be 10 MB or smaller.");
      event.target.value = "";
      return;
    }

    setError("");
    setCertificateFile(file);
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    if (!editable) {
      setError(
        "This listing can no longer be edited because it has already entered the verification workflow."
      );
      return;
    }

    try {
      setSaving(true);
      setError("");
      setSuccess("");

      const payload = {
        title: formData.title.trim(),
        description: formData.description.trim(),
        gemType: formData.gemType.trim(),
        caratWeight: Number(formData.caratWeight),
        color: formData.color.trim(),
        clarity: formData.clarity.trim(),
        cut: formData.cut.trim(),
        price: Number(formData.price),
        certificateNumber:
          formData.certificateNumber.trim() || null,
        certificateAuthority:
          formData.certificateAuthority.trim() || null,
      };

      await gemListingService.updateListing(id, payload);

      if (imageFile) {
        setSuccess("Listing saved. Uploading gemstone image...");
        await gemListingService.uploadImage(id, imageFile);
      }

      if (certificateFile) {
        setSuccess("Uploading certificate...");
        await gemListingService.uploadCertificate(
          id,
          certificateFile
        );
      }

      setSuccess("Changes saved successfully.");

      navigate(`/seller/listings/${id}`);
    } catch (err) {
      console.error("Failed to update listing:", err);

      setError(
        getErrorMessage(
          err,
          "We couldn't save your changes. Please check the information and try again."
        )
      );
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <DashboardLayout>
        <main className="gem-form-page">
          <div className="gem-form-loading">
            <span className="gem-form-loader" />
            <p>Loading gemstone listing...</p>
          </div>
        </main>
      </DashboardLayout>
    );
  }

  if (error && !listing) {
    return (
      <DashboardLayout>
        <main className="gem-form-page">
          <div className="gem-form-state-card">
            <span className="gem-form-state-icon">!</span>
            <h2>Unable to load listing</h2>
            <p>{error}</p>

            <button
              type="button"
              className="gem-secondary-button"
              onClick={() => navigate("/seller/listings")}
            >
              Back to My Listings
            </button>
          </div>
        </main>
      </DashboardLayout>
    );
  }

  if (!editable) {
    return (
      <DashboardLayout>
        <main className="gem-form-page">
          <div className="gem-form-state-card">
            <span className="gem-form-state-icon">◇</span>

            <p className="gem-form-eyebrow">LISTING LOCKED</p>

            <h2>This listing is not editable</h2>

            <p>
              The current status is{" "}
              <strong>{listing?.status}</strong>. Only Draft listings
              or listings returned with Changes Requested can be edited.
            </p>

            <div className="gem-form-state-actions">
              <button
                type="button"
                className="gem-secondary-button"
                onClick={() =>
                  navigate(`/seller/listings/${id}`)
                }
              >
                View Listing
              </button>

              <button
                type="button"
                className="gem-primary-button"
                onClick={() => navigate("/seller/listings")}
              >
                My Listings
              </button>
            </div>
          </div>
        </main>
      </DashboardLayout>
    );
  }

  return (
    <DashboardLayout>
      <main className="gem-form-page">
        <div className="gem-form-container">
          <button
            type="button"
            className="gem-form-back"
            onClick={() => navigate(`/seller/listings/${id}`)}
          >
            ← Listing Details
          </button>

        <div className="gem-form-header">
        <p className="gem-form-eyebrow">
        EDIT GEMSTONE LISTING
         </p>

         <h1>Edit Gem Listing</h1>

        <p className="gem-form-subtitle">
            Update gemstone information and verification evidence
            before sending the listing for Gemologist review.
        </p>
        </div>

          {listing?.status === "ChangesRequested" && (
            <section className="gem-changes-requested-banner">
              <div className="gem-changes-requested-icon">!</div>

              <div>
                <span>GEMOLOGIST REVIEW</span>

                <h3>Changes requested</h3>

                <p>
                  Review the requested corrections, update the listing
                  information or evidence, save your changes, and then
                  resubmit the listing for verification.
                </p>
              </div>
            </section>
          )}

          {error && (
            <div className="gem-form-message gem-form-message-error">
              <span>!</span>
              <p>{error}</p>
            </div>
          )}

          {success && (
            <div className="gem-form-message gem-form-message-success">
              <span>✓</span>
              <p>{success}</p>
            </div>
          )}

          <form onSubmit={handleSubmit}>
            {/* SECTION 01 */}
            <section className="gem-form-section">
              <div className="gem-form-section-number">01</div>

              <div className="gem-form-section-heading">
                <h2>Gemstone Information</h2>
                <p>
                  Keep the primary marketplace information accurate and
                  easy to understand.
                </p>
              </div>

              <div className="gem-form-fields">
                <label className="gem-form-field gem-form-field-full">
                  <span>LISTING TITLE *</span>

                  <input
                    type="text"
                    name="title"
                    value={formData.title}
                    onChange={handleChange}
                    required
                    maxLength={150}
                  />
                </label>

                <label className="gem-form-field gem-form-field-full">
                  <span>DESCRIPTION *</span>

                  <textarea
                    name="description"
                    value={formData.description}
                    onChange={handleChange}
                    required
                    rows={5}
                  />
                </label>

                <label className="gem-form-field">
                  <span>GEM TYPE *</span>

                  <input
                    type="text"
                    name="gemType"
                    value={formData.gemType}
                    onChange={handleChange}
                    required
                  />
                </label>

                <label className="gem-form-field">
                  <span>CARAT WEIGHT *</span>

                  <div className="gem-input-suffix">
                    <input
                      type="number"
                      name="caratWeight"
                      value={formData.caratWeight}
                      onChange={handleChange}
                      min="0.01"
                      step="0.01"
                      required
                    />

                    <span>ct</span>
                  </div>
                </label>
              </div>
            </section>

            {/* SECTION 02 */}
            <section className="gem-form-section">
              <div className="gem-form-section-number">02</div>

              <div className="gem-form-section-heading">
                <h2>Gem Characteristics</h2>
                <p>
                  Update the visible and declared characteristics used
                  during listing review.
                </p>
              </div>

              <div className="gem-form-fields gem-form-fields-three">
                <label className="gem-form-field">
                  <span>COLOR *</span>

                  <input
                    type="text"
                    name="color"
                    value={formData.color}
                    onChange={handleChange}
                    required
                  />
                </label>

                <label className="gem-form-field">
                  <span>CLARITY *</span>

                  <input
                    type="text"
                    name="clarity"
                    value={formData.clarity}
                    onChange={handleChange}
                    required
                  />
                </label>

                <label className="gem-form-field">
                  <span>CUT *</span>

                  <input
                    type="text"
                    name="cut"
                    value={formData.cut}
                    onChange={handleChange}
                    required
                  />
                </label>
              </div>
            </section>

            {/* SECTION 03 */}
            <section className="gem-form-section">
              <div className="gem-form-section-number">03</div>

              <div className="gem-form-section-heading">
                <h2>Listing Price</h2>
                <p>
                  Update the marketplace asking price for this gemstone.
                </p>
              </div>

              <div className="gem-form-price-row">
                <label className="gem-form-field">
                  <span>PRICE *</span>

                  <div className="gem-input-prefix">
                    <span>LKR</span>

                    <input
                      type="number"
                      name="price"
                      value={formData.price}
                      onChange={handleChange}
                      min="0"
                      step="0.01"
                      required
                    />
                  </div>
                </label>
              </div>
            </section>

            {/* SECTION 04 */}
            <section className="gem-form-section">
              <div className="gem-form-section-number">04</div>

              <div className="gem-form-section-heading">
                <h2>Certificate Information</h2>
                <p>
                  Keep the structured certificate information consistent
                  with the supporting evidence.
                </p>
              </div>

              <div className="gem-form-fields">
                <label className="gem-form-field">
                  <span>CERTIFICATE NUMBER</span>

                  <input
                    type="text"
                    name="certificateNumber"
                    value={formData.certificateNumber}
                    onChange={handleChange}
                  />
                </label>

                <label className="gem-form-field">
                  <span>CERTIFICATE AUTHORITY</span>

                  <input
                    type="text"
                    name="certificateAuthority"
                    value={formData.certificateAuthority}
                    onChange={handleChange}
                  />
                </label>
              </div>
            </section>

            {/* SECTION 05 */}
            <section className="gem-form-section gem-evidence-section">
              <div className="gem-form-section-number">05</div>

              <div className="gem-form-section-heading">
                <h2>Verification Evidence</h2>

                <p>
                  Review the existing evidence or select replacement
                  files. Replacements are uploaded when you save.
                </p>
              </div>

              <div className="gem-edit-evidence-grid">
                {/* IMAGE */}
                <article className="gem-edit-evidence-card">
                  <div className="gem-evidence-card-heading">
                    <div className="gem-evidence-card-icon">◇</div>

                    <div>
                      <h3>Gemstone Image</h3>
                      <p>JPG, PNG or WebP · Maximum 5 MB</p>
                    </div>
                  </div>

                  <div className="gem-edit-image-preview">
                    {imagePreview && !imageBroken ? (
                      <img
                        src={imagePreview}
                        alt="New gemstone preview"
                        onError={() => setImageBroken(true)}
                      />
                    ) : currentImageUrl && !imageBroken ? (
                      <img
                        src={currentImageUrl}
                        alt={listing.title}
                        onError={() => setImageBroken(true)}
                      />
                    ) : (
                      <div className="gem-edit-image-empty">
                        <span>◇</span>
                        <p>No gemstone image</p>
                      </div>
                    )}

                    {(imagePreview || currentImageUrl) &&
                      !imageBroken && (
                        <div className="gem-edit-image-label">
                          {imageFile
                            ? "NEW IMAGE"
                            : "CURRENT IMAGE"}
                        </div>
                      )}
                  </div>

                  <div className="gem-edit-file-information">
                    <div>
                      <span>
                        {imageFile
                          ? "Replacement selected"
                          : listing.primaryImageUrl
                            ? "Current evidence"
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

                    {imageFile && (
                      <button
                        type="button"
                        onClick={() => {
                          setImageFile(null);
                          setImageBroken(false);
                        }}
                      >
                        Undo
                      </button>
                    )}
                  </div>

                  <label className="gem-file-select-button">
                    <input
                      type="file"
                      accept=".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp"
                      onChange={handleImageChange}
                    />

                    <span>
                      {listing.primaryImageUrl
                        ? "Replace Image"
                        : "Choose Image"}
                    </span>

                    <strong>→</strong>
                  </label>
                </article>

                {/* CERTIFICATE */}
                <article className="gem-edit-evidence-card">
                  <div className="gem-evidence-card-heading">
                    <div className="gem-evidence-card-icon">▤</div>

                    <div>
                      <h3>Certificate File</h3>
                      <p>PDF, JPG or PNG · Maximum 10 MB</p>
                    </div>
                  </div>

                  <div className="gem-edit-certificate-preview">
                    <div className="gem-edit-certificate-symbol">
                      PDF
                    </div>

                    <div>
                      <span>
                        {certificateFile
                          ? "NEW CERTIFICATE"
                          : currentCertificateUrl
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
                            ).toFixed(2)} MB`
                          : listing.certificateAuthority ||
                            "Supporting certificate can be added below."}
                      </p>
                    </div>
                  </div>

                  <div className="gem-certificate-actions">
                    {currentCertificateUrl &&
                      !certificateFile && (
                        <a
                          href={currentCertificateUrl}
                          target="_blank"
                          rel="noreferrer"
                          className="gem-view-certificate-button"
                        >
                          View Current Certificate
                        </a>
                      )}

                    {certificateFile && (
                      <button
                        type="button"
                        className="gem-undo-file-button"
                        onClick={() =>
                          setCertificateFile(null)
                        }
                      >
                        Undo Replacement
                      </button>
                    )}
                  </div>

                  <label className="gem-file-select-button">
                    <input
                      type="file"
                      accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"
                      onChange={handleCertificateChange}
                    />

                    <span>
                      {currentCertificateUrl
                        ? "Replace Certificate"
                        : "Choose Certificate"}
                    </span>

                    <strong>→</strong>
                  </label>
                </article>
              </div>

              <div className="gem-ai-evidence-note">
                <span>✦</span>

                <div>
                  <strong>
                    Evidence supports AI-assisted review
                  </strong>

                  <p>
                    Gemora may analyze the submitted gemstone image and
                    listing information to assist a Gemologist. AI
                    observations are advisory; the final verification
                    decision remains human-reviewed.
                  </p>
                </div>
              </div>
            </section>

            {/* SAVE AREA */}
            <section className="gem-edit-save-panel">
              <div>
                <span>LISTING STATUS</span>

                <h3>
                  {listing.status === "ChangesRequested"
                    ? "Changes Requested"
                    : "Draft"}
                </h3>

                <p>
                  Saving updates the Draft but does not submit it for
                  verification.
                </p>
              </div>

              <div className="gem-edit-save-actions">
                <button
                  type="button"
                  className="gem-edit-cancel-button"
                  disabled={saving}
                  onClick={() =>
                    navigate(`/seller/listings/${id}`)
                  }
                >
                  Cancel
                </button>

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
                      <span>→</span>
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