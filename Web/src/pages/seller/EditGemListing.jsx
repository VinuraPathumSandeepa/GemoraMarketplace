import { useEffect, useState } from "react";
import {
  useNavigate,
  useParams,
} from "react-router-dom";

import gemListingService from "../../services/gemVerification/gemListingService";

function EditGemListing() {
  const { id } = useParams();
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
    currency: "LKR",
    certificateNumber: "",
    certificateAuthority: "",
  });

  const [listingStatus, setListingStatus] =
    useState("");

  const [loading, setLoading] =
    useState(true);

  const [saving, setSaving] =
    useState(false);

  const [error, setError] =
    useState("");

  // ============================================================
  // LOAD LISTING
  // ============================================================

  useEffect(() => {
    const loadListing = async () => {
      try {
        setLoading(true);
        setError("");

        const listing =
          await gemListingService.getListingById(id);

        // IMPORTANT:
        // Backend API returns "status", not "listingStatus".
        setListingStatus(listing.status);

        if (
          listing.status !== "Draft" &&
          listing.status !== "ChangesRequested"
        ) {
          setError(
            "This listing cannot be edited while it is in its current verification state."
          );

          return;
        }

        setFormData({
          title:
            listing.title || "",

          description:
            listing.description || "",

          gemType:
            listing.gemType || "",

          caratWeight:
            listing.caratWeight ?? "",

          color:
            listing.color || "",

          clarity:
            listing.clarity || "",

          cut:
            listing.cut || "",

          price:
            listing.price ?? "",

          currency:
            listing.currency || "LKR",

          certificateNumber:
            listing.certificateNumber || "",

          certificateAuthority:
            listing.certificateAuthority || "",
        });
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

    loadListing();
  }, [id]);

  // ============================================================
  // INPUT CHANGE
  // ============================================================

  const handleChange = (event) => {
    const {
      name,
      value,
    } = event.target;

    setFormData((previous) => ({
      ...previous,
      [name]: value,
    }));
  };

  // ============================================================
  // SAVE CHANGES
  // ============================================================

  const handleSubmit = async (event) => {
    event.preventDefault();

    try {
      setSaving(true);
      setError("");

      const payload = {
        title:
          formData.title.trim(),

        description:
          formData.description.trim(),

        gemType:
          formData.gemType.trim(),

        caratWeight:
          Number(formData.caratWeight),

        color:
          formData.color.trim(),

        clarity:
          formData.clarity.trim(),

        cut:
          formData.cut.trim(),

        price:
          Number(formData.price),

        currency:
          formData.currency,

        certificateNumber:
          formData.certificateNumber.trim() ||
          null,

        certificateAuthority:
          formData.certificateAuthority.trim() ||
          null,
      };

      await gemListingService.updateListing(
        id,
        payload
      );

      navigate(
        `/seller/listings/${id}`
      );
    } catch (err) {
      console.error(err);

      const apiMessage =
        err.response?.data?.message;

      const validationErrors =
        err.response?.data?.errors;

      if (apiMessage) {
        setError(apiMessage);
      } else if (validationErrors) {
        const firstError =
          Object.values(validationErrors)
            .flat()
            .find(Boolean);

        setError(
          firstError ||
            "Please check the listing information."
        );
      } else {
        setError(
          "Unable to update the gemstone listing. Please try again."
        );
      }
    } finally {
      setSaving(false);
    }
  };

  // ============================================================
  // LOADING
  // ============================================================

  if (loading) {
    return (
      <div className="gem-form-page">
        <p>
          Loading gemstone listing...
        </p>
      </div>
    );
  }

  // ============================================================
  // EDIT PERMISSION
  // ============================================================

  const canEdit =
    listingStatus === "Draft" ||
    listingStatus === "ChangesRequested";

  if (!canEdit) {
    return (
      <div className="gem-form-page">
        <div className="gem-form-container">

          <div className="error-message">
            {error ||
              "This listing cannot currently be edited."}
          </div>

          <button
            type="button"
            className="secondary-button"
            onClick={() =>
              navigate(
                `/seller/listings/${id}`
              )
            }
          >
            Back to Listing
          </button>

        </div>
      </div>
    );
  }

  // ============================================================
  // EDIT FORM
  // ============================================================

  return (
    <div className="gem-form-page">
      <div className="gem-form-container">

        <div className="gem-form-header">
          <div>
            <h1>
              Edit Gem Listing
            </h1>

            <p>
              Update the gemstone information
              before submitting it for Gemologist
              verification.
            </p>
          </div>

          <button
            type="button"
            className="secondary-button"
            onClick={() =>
              navigate(
                `/seller/listings/${id}`
              )
            }
          >
            Back to Listing
          </button>
        </div>

        {/* ====================================================
            CHANGES REQUESTED NOTICE
            ==================================================== */}

        {listingStatus ===
          "ChangesRequested" && (
          <div className="verification-warning">
            <strong>
              Changes requested by Gemologist.
            </strong>

            <p>
              Update the required listing
              information or evidence, save your
              changes, and then resubmit the
              listing for verification.
            </p>
          </div>
        )}

        {error && (
          <div className="error-message">
            {error}
          </div>
        )}

        <form
          className="gem-form"
          onSubmit={handleSubmit}
        >

          {/* ==================================================
              BASIC INFORMATION
              ================================================== */}

          <div className="form-section">
            <h2>
              Basic Information
            </h2>

            <div className="form-grid">

              <div className="form-group form-group-full">
                <label htmlFor="title">
                  Listing Title *
                </label>

                <input
                  id="title"
                  name="title"
                  type="text"
                  value={formData.title}
                  onChange={handleChange}
                  required
                />
              </div>

              <div className="form-group form-group-full">
                <label htmlFor="description">
                  Description
                </label>

                <textarea
                  id="description"
                  name="description"
                  rows="4"
                  value={formData.description}
                  onChange={handleChange}
                />
              </div>

              <div className="form-group">
                <label htmlFor="gemType">
                  Gem Type *
                </label>

                <input
                  id="gemType"
                  name="gemType"
                  type="text"
                  value={formData.gemType}
                  onChange={handleChange}
                  required
                />
              </div>

              <div className="form-group">
                <label htmlFor="caratWeight">
                  Carat Weight *
                </label>

                <input
                  id="caratWeight"
                  name="caratWeight"
                  type="number"
                  min="0.01"
                  step="0.01"
                  value={formData.caratWeight}
                  onChange={handleChange}
                  required
                />
              </div>

            </div>
          </div>

          {/* ==================================================
              CHARACTERISTICS
              ================================================== */}

          <div className="form-section">
            <h2>
              Gem Characteristics
            </h2>

            <div className="form-grid">

              <div className="form-group">
                <label htmlFor="color">
                  Color
                </label>

                <input
                  id="color"
                  name="color"
                  type="text"
                  value={formData.color}
                  onChange={handleChange}
                />
              </div>

              <div className="form-group">
                <label htmlFor="clarity">
                  Clarity
                </label>

                <input
                  id="clarity"
                  name="clarity"
                  type="text"
                  value={formData.clarity}
                  onChange={handleChange}
                />
              </div>

              <div className="form-group">
                <label htmlFor="cut">
                  Cut
                </label>

                <input
                  id="cut"
                  name="cut"
                  type="text"
                  value={formData.cut}
                  onChange={handleChange}
                />
              </div>

            </div>
          </div>

          {/* ==================================================
              PRICE
              ================================================== */}

          <div className="form-section">
            <h2>
              Price
            </h2>

            <div className="form-grid">

              <div className="form-group">
                <label htmlFor="price">
                  Price *
                </label>

                <input
                  id="price"
                  name="price"
                  type="number"
                  min="0.01"
                  step="0.01"
                  value={formData.price}
                  onChange={handleChange}
                  required
                />
              </div>

              <div className="form-group">
                <label htmlFor="currency">
                  Currency *
                </label>

                <select
                  id="currency"
                  name="currency"
                  value={formData.currency}
                  onChange={handleChange}
                  required
                >
                  <option value="LKR">
                    LKR
                  </option>

                  <option value="USD">
                    USD
                  </option>
                </select>
              </div>

            </div>
          </div>

          {/* ==================================================
              CERTIFICATE METADATA
              ================================================== */}

          <div className="form-section">
            <h2>
              Certificate Information
            </h2>

            <p className="section-help">
              Edit the certificate metadata here.
              The actual certificate file is
              uploaded from the listing details
              page.
            </p>

            <div className="form-grid">

              <div className="form-group">
                <label htmlFor="certificateNumber">
                  Certificate Number
                </label>

                <input
                  id="certificateNumber"
                  name="certificateNumber"
                  type="text"
                  value={
                    formData.certificateNumber
                  }
                  onChange={handleChange}
                />
              </div>

              <div className="form-group">
                <label htmlFor="certificateAuthority">
                  Certificate Authority
                </label>

                <input
                  id="certificateAuthority"
                  name="certificateAuthority"
                  type="text"
                  value={
                    formData.certificateAuthority
                  }
                  onChange={handleChange}
                />
              </div>

            </div>
          </div>

          {/* ==================================================
              ACTIONS
              ================================================== */}

          <div className="form-actions">

            <button
              type="button"
              className="secondary-button"
              disabled={saving}
              onClick={() =>
                navigate(
                  `/seller/listings/${id}`
                )
              }
            >
              Cancel
            </button>

            <button
              type="submit"
              className="primary-button"
              disabled={saving}
            >
              {saving
                ? "Saving..."
                : "Save Changes"}
            </button>

          </div>

        </form>
      </div>
    </div>
  );
}

export default EditGemListing;