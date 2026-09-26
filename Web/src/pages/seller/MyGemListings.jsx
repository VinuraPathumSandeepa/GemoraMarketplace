import {
  useEffect,
  useState,
} from "react";

import {
  Link,
} from "react-router-dom";

import gemListingService from "../../services/gemVerification/gemListingService";

const API_ORIGIN = "http://localhost:5198";

function MyGemListings() {
  const [listings, setListings] =
    useState([]);

  const [loading, setLoading] =
    useState(true);

  const [error, setError] =
    useState("");

  // ============================================================
  // LOAD SELLER LISTINGS
  // ============================================================

  useEffect(() => {
    const loadListings = async () => {
      try {
        setLoading(true);
        setError("");

        const data =
          await gemListingService.getMyListings();

        setListings(
          Array.isArray(data)
            ? data
            : []
        );
      } catch (err) {
        console.error(err);

        setError(
          err.response?.data?.message ||
            "Unable to load your gemstone listings."
        );
      } finally {
        setLoading(false);
      }
    };

    loadListings();
  }, []);

  // ============================================================
  // STATUS
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
      <div className="seller-listings-page">
        <p>
          Loading your gemstone listings...
        </p>
      </div>
    );
  }

  // ============================================================
  // PAGE
  // ============================================================

  return (
    <div className="seller-listings-page">

      {/* ======================================================
          HEADER
          ====================================================== */}

      <div className="seller-listings-header">

        <div>
          <h1>
            My Gem Listings
          </h1>

          <p>
            Create and manage your gemstone
            listings and verification workflow.
          </p>
        </div>

        <Link
          to="/seller/listings/create"
          className="primary-button"
        >
          + Add Gem Listing
        </Link>

      </div>

      {/* ======================================================
          ERROR
          ====================================================== */}

      {error && (
        <div className="error-message">
          {error}
        </div>
      )}

      {/* ======================================================
          EMPTY STATE
          ====================================================== */}

      {!error &&
        listings.length === 0 && (
          <div className="empty-state">

            <h2>
              No Gem Listings Yet
            </h2>

            <p>
              Create your first gemstone listing
              to begin the verification process.
            </p>

            <Link
              to="/seller/listings/create"
              className="primary-button"
            >
              Create First Listing
            </Link>

          </div>
        )}

      {/* ======================================================
          LISTING GRID
          ====================================================== */}

      {listings.length > 0 && (
        <div className="listing-grid">

          {listings.map((listing) => {
            const canEdit =
              listing.status === "Draft" ||
              listing.status ===
                "ChangesRequested";

            return (
              <article
                key={listing.id}
                className="listing-card"
              >

                {/* ============================================
                    IMAGE
                    ============================================ */}

                <div className="listing-image-wrapper">

                  {listing.primaryImageUrl ? (
                    <img
                      src={`${API_ORIGIN}${listing.primaryImageUrl}`}
                      alt={listing.title}
                      className="listing-image"
                      onError={(event) => {
                        event.currentTarget.style.display =
                          "none";

                        const parent =
                          event.currentTarget.parentElement;

                        if (
                          parent &&
                          !parent.querySelector(
                            ".image-placeholder"
                          )
                        ) {
                          const placeholder =
                            document.createElement(
                              "div"
                            );

                          placeholder.className =
                            "image-placeholder";

                          placeholder.textContent =
                            "Image unavailable";

                          parent.appendChild(
                            placeholder
                          );
                        }
                      }}
                    />
                  ) : (
                    <div className="image-placeholder">
                      No Image
                    </div>
                  )}

                </div>

                {/* ============================================
                    CARD BODY
                    ============================================ */}

                <div className="listing-card-body">

                  <div className="listing-card-heading">

                    <h2>
                      {listing.title}
                    </h2>

                    <span
                      className={`status ${getStatusClass(
                        listing.status
                      )}`}
                    >
                      {formatStatus(
                        listing.status
                      )}
                    </span>

                  </div>

                  {/* ==========================================
                      INFORMATION
                      ========================================== */}

                  <div className="listing-information">

                    <p>
                      <strong>
                        Gem Type:
                      </strong>{" "}
                      {listing.gemType || "—"}
                    </p>

                    <p>
                      <strong>
                        Carat:
                      </strong>{" "}
                      {listing.caratWeight ??
                        "—"}
                    </p>

                    <p>
                      <strong>
                        Color:
                      </strong>{" "}
                      {listing.color || "—"}
                    </p>

                    <p>
                      <strong>
                        Clarity:
                      </strong>{" "}
                      {listing.clarity || "—"}
                    </p>

                    <p>
                      <strong>
                        Cut:
                      </strong>{" "}
                      {listing.cut || "—"}
                    </p>

                    <p>
                      <strong>
                        Price:
                      </strong>{" "}
                      {listing.currency ||
                        "LKR"}{" "}
                      {Number(
                        listing.price || 0
                      ).toLocaleString()}
                    </p>

                  </div>

                  {/* ==========================================
                      ACTIONS
                      ========================================== */}

                  <div className="listing-actions">

                    <Link
                      to={`/seller/listings/${listing.id}`}
                      className="secondary-button"
                    >
                      View Details
                    </Link>

                    {canEdit && (
                      <Link
                        to={`/seller/listings/${listing.id}/edit`}
                        className="edit-button"
                      >
                        Edit
                      </Link>
                    )}

                  </div>

                </div>

              </article>
            );
          })}

        </div>
      )}

    </div>
  );
}

export default MyGemListings;