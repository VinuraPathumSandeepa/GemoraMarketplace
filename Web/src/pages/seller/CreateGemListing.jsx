import { useState } from "react";
import { useNavigate } from "react-router-dom";
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
    currency: "LKR",
    certificateNumber: "",
    certificateAuthority: "",
  });

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const handleChange = (event) => {
    const { name, value } = event.target;

    setFormData((previous) => ({
      ...previous,
      [name]: value,
    }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    try {
      setSaving(true);
      setError("");

      const payload = {
        title: formData.title.trim(),
        description: formData.description.trim(),
        gemType: formData.gemType.trim(),

        caratWeight: Number(formData.caratWeight),

        color: formData.color.trim(),
        clarity: formData.clarity.trim(),
        cut: formData.cut.trim(),

        price: Number(formData.price),

        currency: formData.currency,

        certificateNumber:
          formData.certificateNumber.trim() || null,

        certificateAuthority:
          formData.certificateAuthority.trim() || null,
      };

      const createdListing =
        await gemListingService.createListing(payload);

      navigate(
        `/seller/listings/${createdListing.id}`
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
          "Unable to create the gemstone listing. Please try again."
        );
      }
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="gem-form-page">
      <div className="gem-form-container">
        <div className="gem-form-header">
          <div>
            <h1>Create Gem Listing</h1>

            <p>
              Enter the gemstone information first.
              You can upload the gemstone photograph
              and certificate after creating the draft.
            </p>
          </div>

          <button
            type="button"
            className="secondary-button"
            onClick={() =>
              navigate("/seller/listings")
            }
          >
            Back to Listings
          </button>
        </div>

        {error && (
          <div className="error-message">
            {error}
          </div>
        )}

        <form
          className="gem-form"
          onSubmit={handleSubmit}
        >
          <div className="form-section">
            <h2>Basic Information</h2>

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
                  placeholder="Example: Natural Blue Sapphire"
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
                  value={formData.description}
                  onChange={handleChange}
                  placeholder="Describe the gemstone..."
                  rows="4"
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
                  placeholder="Sapphire"
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
                  placeholder="2.50"
                  required
                />
              </div>
            </div>
          </div>

          <div className="form-section">
            <h2>Gem Characteristics</h2>

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
                  placeholder="Royal Blue"
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
                  placeholder="Eye Clean"
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
                  placeholder="Oval"
                />
              </div>
            </div>
          </div>

          <div className="form-section">
            <h2>Price</h2>

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
                  placeholder="850000"
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
                  <option value="LKR">LKR</option>
                  <option value="USD">USD</option>
                </select>
              </div>
            </div>
          </div>

          <div className="form-section">
            <h2>Certificate Information</h2>

            <p className="section-help">
              Certificate metadata is optional while
              creating the draft. The actual certificate
              file can be uploaded afterward.
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
                  value={formData.certificateNumber}
                  onChange={handleChange}
                  placeholder="Example: GEM-2026-001"
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
                  value={formData.certificateAuthority}
                  onChange={handleChange}
                  placeholder="Example: Gem Laboratory"
                />
              </div>
            </div>
          </div>

          <div className="form-actions">
            <button
              type="button"
              className="secondary-button"
              disabled={saving}
              onClick={() =>
                navigate("/seller/listings")
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
                ? "Creating..."
                : "Create Draft Listing"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default CreateGemListing;