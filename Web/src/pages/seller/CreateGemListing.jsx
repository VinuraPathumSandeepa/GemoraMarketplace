import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";

import DashboardLayout from "../../layouts/DashboardLayout";
import gemListingService from "../../services/gemVerification/gemListingService";

function CreateGemListing() {
  const navigate = useNavigate();

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

  const [gemImage, setGemImage] = useState(null);
  const [certificateFile, setCertificateFile] = useState(null);

  const [gemImagePreview, setGemImagePreview] = useState("");

  const [submitting, setSubmitting] = useState(false);
  const [progressStep, setProgressStep] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    if (!gemImage) {
      setGemImagePreview("");
      return undefined;
    }

    const previewUrl = URL.createObjectURL(gemImage);
    setGemImagePreview(previewUrl);

    return () => {
      URL.revokeObjectURL(previewUrl);
    };
  }, [gemImage]);

  const handleChange = (event) => {
    const { name, value } = event.target;

    setFormData((previous) => ({
      ...previous,
      [name]: value,
    }));
  };

  const handleGemImageChange = (event) => {
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
      setError(
        "Please choose a JPG, PNG, or WebP gemstone image."
      );
      event.target.value = "";
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      setError(
        "The gemstone image must be 5 MB or smaller."
      );
      event.target.value = "";
      return;
    }

    setError("");
    setGemImage(file);
  };

  const handleCertificateChange = (event) => {
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
      setError(
        "Please choose a PDF, JPG, or PNG certificate file."
      );
      event.target.value = "";
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      setError(
        "The certificate file must be 10 MB or smaller."
      );
      event.target.value = "";
      return;
    }

    setError("");
    setCertificateFile(file);
  };

  const removeGemImage = () => {
    setGemImage(null);
  };

  const removeCertificate = () => {
    setCertificateFile(null);
  };

  const validateForm = () => {
    if (!formData.title.trim()) {
      return "Enter a title for the gemstone listing.";
    }

    if (!formData.description.trim()) {
      return "Enter a description of the gemstone.";
    }

    if (!formData.gemType.trim()) {
      return "Enter the gemstone type.";
    }

    const caratWeight = Number(formData.caratWeight);

    if (
      !formData.caratWeight ||
      Number.isNaN(caratWeight) ||
      caratWeight <= 0
    ) {
      return "Enter a valid carat weight greater than 0.";
    }

    if (!formData.color.trim()) {
      return "Enter the gemstone color.";
    }

    if (!formData.clarity.trim()) {
      return "Enter the gemstone clarity.";
    }

    if (!formData.cut.trim()) {
      return "Enter the gemstone cut.";
    }

    const price = Number(formData.price);

    if (
      !formData.price ||
      Number.isNaN(price) ||
      price <= 0
    ) {
      return "Enter a valid listing price greater than 0.";
    }

    return "";
  };

  const getApiErrorMessage = (requestError) => {
    const responseMessage =
      requestError?.response?.data?.message;

    const responseError =
      requestError?.response?.data?.error;

    return (
      responseMessage ||
      responseError ||
      "Something went wrong while creating the listing. Please try again."
    );
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    const validationMessage = validateForm();

    if (validationMessage) {
      setError(validationMessage);
      return;
    }

    setSubmitting(true);
    setError("");

    let createdListingId = null;

    try {
      /*
       * Stage 1:
       * Create the Draft first so the backend gives us
       * the listing ID required by the upload endpoints.
       */
      setProgressStep("Creating gemstone listing...");

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

      const createdListing =
        await gemListingService.createListing(payload);

      createdListingId = createdListing.id;

      if (!createdListingId) {
        throw new Error(
          "The listing was created but its ID was not returned."
        );
      }

      /*
       * Stage 2:
       * Upload gemstone evidence.
       */
      if (gemImage) {
        setProgressStep("Uploading gemstone image...");

        await gemListingService.uploadImage(
          createdListingId,
          gemImage
        );
      }

      /*
       * Stage 3:
       * Upload certificate evidence.
       */
      if (certificateFile) {
        setProgressStep("Uploading certificate...");

        await gemListingService.uploadCertificate(
          createdListingId,
          certificateFile
        );
      }

      setProgressStep("Listing created successfully.");

      navigate(`/seller/listings/${createdListingId}`, {
        state: {
          message:
            "Gemstone listing created successfully.",
        },
      });
    } catch (requestError) {
      console.error(
        "Create gemstone listing failed:",
        requestError
      );

      /*
       * If the Draft was already created but an evidence
       * upload failed, do NOT pretend the entire operation
       * disappeared. The seller can continue from Details.
       */
      if (createdListingId) {
        setError(
          "The Draft was created, but one of the evidence files could not be uploaded. Open the Draft from My Listings to upload or replace the missing evidence."
        );

        setProgressStep("");
      } else {
        setError(getApiErrorMessage(requestError));
        setProgressStep("");
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <DashboardLayout>
      <div className="seller-create-page">

        {/* HEADER */}

        <section className="seller-create-header">

          <button
            type="button"
            className="seller-create-back"
            onClick={() =>
              navigate("/seller/listings")
            }
          >
            ← My Listings
          </button>

          <span>NEW GEMSTONE LISTING</span>

          <h1>Create Gem Listing</h1>

          <p>
            Add gemstone information and verification evidence.
            Your listing will begin as a Draft and can be
            submitted for Gemologist review when ready.
          </p>

        </section>


        <form
          className="seller-create-form"
          onSubmit={handleSubmit}
        >

          {/* =================================================
              SECTION 01 — BASIC INFORMATION
              ================================================= */}

          <section className="seller-create-section">

            <div className="seller-create-section-heading">
              <span>01</span>

              <div>
                <h2>Gemstone Information</h2>

                <p>
                  Provide the primary information buyers and
                  Gemologists need to understand this stone.
                </p>
              </div>
            </div>


            <div className="seller-create-fields">

              <label className="seller-create-field seller-create-field-full">
                <span>
                  Listing Title
                  <strong>*</strong>
                </span>

                <input
                  type="text"
                  name="title"
                  value={formData.title}
                  onChange={handleChange}
                  placeholder="e.g. Natural Ceylon Blue Sapphire"
                  disabled={submitting}
                />
              </label>


              <label className="seller-create-field seller-create-field-full">
                <span>
                  Description
                  <strong>*</strong>
                </span>

                <textarea
                  name="description"
                  value={formData.description}
                  onChange={handleChange}
                  placeholder="Describe the gemstone, its appearance, condition, and other useful information..."
                  rows="5"
                  disabled={submitting}
                />
              </label>


              <label className="seller-create-field">
                <span>
                  Gem Type
                  <strong>*</strong>
                </span>

                <input
                  type="text"
                  name="gemType"
                  value={formData.gemType}
                  onChange={handleChange}
                  placeholder="e.g. Sapphire"
                  disabled={submitting}
                />
              </label>


              <label className="seller-create-field">
                <span>
                  Carat Weight
                  <strong>*</strong>
                </span>

                <div className="seller-create-input-suffix">
                  <input
                    type="number"
                    name="caratWeight"
                    value={formData.caratWeight}
                    onChange={handleChange}
                    min="0.01"
                    step="0.01"
                    placeholder="2.75"
                    disabled={submitting}
                  />

                  <span>ct</span>
                </div>
              </label>

            </div>

          </section>


          {/* =================================================
              SECTION 02 — CHARACTERISTICS
              ================================================= */}

          <section className="seller-create-section">

            <div className="seller-create-section-heading">
              <span>02</span>

              <div>
                <h2>Gem Characteristics</h2>

                <p>
                  Record visible and declared characteristics
                  used during listing review.
                </p>
              </div>
            </div>


            <div className="seller-create-fields seller-create-three-columns">

              <label className="seller-create-field">
                <span>
                  Color
                  <strong>*</strong>
                </span>

                <input
                  type="text"
                  name="color"
                  value={formData.color}
                  onChange={handleChange}
                  placeholder="e.g. Royal Blue"
                  disabled={submitting}
                />
              </label>


              <label className="seller-create-field">
                <span>
                  Clarity
                  <strong>*</strong>
                </span>

                <input
                  type="text"
                  name="clarity"
                  value={formData.clarity}
                  onChange={handleChange}
                  placeholder="e.g. Eye Clean"
                  disabled={submitting}
                />
              </label>


              <label className="seller-create-field">
                <span>
                  Cut
                  <strong>*</strong>
                </span>

                <input
                  type="text"
                  name="cut"
                  value={formData.cut}
                  onChange={handleChange}
                  placeholder="e.g. Oval"
                  disabled={submitting}
                />
              </label>

            </div>

          </section>


          {/* =================================================
              SECTION 03 — PRICE
              ================================================= */}

          <section className="seller-create-section">

            <div className="seller-create-section-heading">
              <span>03</span>

              <div>
                <h2>Listing Price</h2>

                <p>
                  Set the marketplace asking price for this
                  gemstone.
                </p>
              </div>
            </div>


            <div className="seller-create-fields">

              <label className="seller-create-field">
                <span>
                  Price
                  <strong>*</strong>
                </span>

                <div className="seller-create-input-prefix">
                  <span>LKR</span>

                  <input
                    type="number"
                    name="price"
                    value={formData.price}
                    onChange={handleChange}
                    min="1"
                    step="0.01"
                    placeholder="850000"
                    disabled={submitting}
                  />
                </div>
              </label>

            </div>

          </section>


          {/* =================================================
              SECTION 04 — CERTIFICATE METADATA
              ================================================= */}

          <section className="seller-create-section">

            <div className="seller-create-section-heading">
              <span>04</span>

              <div>
                <h2>Certificate Information</h2>

                <p>
                  Add structured certificate details when they
                  are available.
                </p>
              </div>
            </div>


            <div className="seller-create-fields">

              <label className="seller-create-field">
                <span>Certificate Number</span>

                <input
                  type="text"
                  name="certificateNumber"
                  value={formData.certificateNumber}
                  onChange={handleChange}
                  placeholder="e.g. GIA-123456"
                  disabled={submitting}
                />
              </label>


              <label className="seller-create-field">
                <span>Certificate Authority</span>

                <input
                  type="text"
                  name="certificateAuthority"
                  value={formData.certificateAuthority}
                  onChange={handleChange}
                  placeholder="e.g. GIA"
                  disabled={submitting}
                />
              </label>

            </div>

          </section>


          {/* =================================================
              SECTION 05 — VERIFICATION EVIDENCE
              ================================================= */}

          <section className="seller-create-section seller-create-evidence-section">

            <div className="seller-create-section-heading">
              <span>05</span>

              <div>
                <h2>Verification Evidence</h2>

                <p>
                  Add a clear gemstone photograph and supporting
                  certificate. Evidence can also be added or
                  replaced while the listing remains editable.
                </p>
              </div>
            </div>


            <div className="seller-create-evidence-grid">

              {/* GEM IMAGE */}

              <div className="seller-create-upload-card">

                <div className="seller-create-upload-top">
                  <div className="seller-create-upload-icon">
                    ◇
                  </div>

                  <div>
                    <h3>Gemstone Image</h3>

                    <p>
                      JPG, PNG or WebP · Maximum 5 MB
                    </p>
                  </div>
                </div>


                {gemImagePreview ? (
                  <div className="seller-create-image-preview">

                    <img
                      src={gemImagePreview}
                      alt="Selected gemstone preview"
                    />

                    <div className="seller-create-file-info">
                      <div>
                        <strong>{gemImage.name}</strong>

                        <span>
                          {(gemImage.size / 1024 / 1024).toFixed(2)} MB
                        </span>
                      </div>

                      <button
                        type="button"
                        onClick={removeGemImage}
                        disabled={submitting}
                      >
                        Remove
                      </button>
                    </div>

                  </div>
                ) : (
                  <label className="seller-create-dropzone">

                    <input
                      type="file"
                      accept=".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp"
                      onChange={handleGemImageChange}
                      disabled={submitting}
                    />

                    <div className="seller-create-dropzone-icon">
                      +
                    </div>

                    <strong>
                      Choose gemstone image
                    </strong>

                    <span>
                      Use a clear, well-lit photograph of the
                      actual stone.
                    </span>

                  </label>
                )}

              </div>


              {/* CERTIFICATE */}

              <div className="seller-create-upload-card">

                <div className="seller-create-upload-top">
                  <div className="seller-create-upload-icon">
                    ▤
                  </div>

                  <div>
                    <h3>Certificate File</h3>

                    <p>
                      PDF, JPG or PNG · Maximum 10 MB
                    </p>
                  </div>
                </div>


                {certificateFile ? (
                  <div className="seller-create-certificate-preview">

                    <div className="seller-create-document-icon">
                      PDF
                    </div>

                    <div className="seller-create-document-copy">
                      <strong>
                        {certificateFile.name}
                      </strong>

                      <span>
                        {(
                          certificateFile.size /
                          1024 /
                          1024
                        ).toFixed(2)}{" "}
                        MB
                      </span>
                    </div>

                    <button
                      type="button"
                      onClick={removeCertificate}
                      disabled={submitting}
                    >
                      Remove
                    </button>

                  </div>
                ) : (
                  <label className="seller-create-dropzone">

                    <input
                      type="file"
                      accept=".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png"
                      onChange={handleCertificateChange}
                      disabled={submitting}
                    />

                    <div className="seller-create-dropzone-icon">
                      +
                    </div>

                    <strong>
                      Choose certificate
                    </strong>

                    <span>
                      Upload the supporting gem certificate when
                      one is available.
                    </span>

                  </label>
                )}

              </div>

            </div>


            <div className="seller-create-ai-note">

              <span>✦</span>

              <div>
                <strong>
                  Evidence supports AI-assisted review
                </strong>

                <p>
                  Gemora may analyze the submitted gemstone
                  image and listing information to assist a
                  Gemologist. AI observations are advisory;
                  final verification remains a human decision.
                </p>
              </div>

            </div>

          </section>


          {/* =================================================
              ERROR
              ================================================= */}

          {error && (
            <div className="seller-create-error">
              <span>!</span>

              <div>
                <strong>
                  We couldn't complete everything
                </strong>

                <p>{error}</p>
              </div>
            </div>
          )}


          {/* =================================================
              SUBMIT AREA
              ================================================= */}

          <section className="seller-create-submit-area">

            <div>
              <span>LISTING STATUS</span>

              <strong>Draft</strong>

              <p>
                Creating this listing does not submit it for
                verification. You can review the evidence before
                submitting it to a Gemologist.
              </p>
            </div>


            <div className="seller-create-submit-actions">

              <button
                type="button"
                className="seller-create-cancel-button"
                onClick={() =>
                  navigate("/seller/listings")
                }
                disabled={submitting}
              >
                Cancel
              </button>


              <button
                type="submit"
                className="seller-create-submit-button"
                disabled={submitting}
              >
                {submitting ? (
                  <>
                    <span className="seller-create-button-loader" />

                    {progressStep || "Creating listing..."}
                  </>
                ) : (
                  <>
                    Create Draft
                    <span>→</span>
                  </>
                )}
              </button>

            </div>

          </section>

        </form>

      </div>
    </DashboardLayout>
  );
}

export default CreateGemListing;