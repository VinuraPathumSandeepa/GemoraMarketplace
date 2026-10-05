
import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";

import DashboardLayout from "../../layouts/DashboardLayout";

import gemListingService from "../../services/gemVerification/gemListingService";

import {
  resolveApiAssetUrl,
} from "../../services/api";

// ============================================================
// STATUS FILTERS
// ============================================================

const FILTERS = [
  { value: "All", label: "All" },
  { value: "Draft", label: "Drafts" },
  {
    value: "PendingVerification",
    label: "Pending Review",
  },
  {
    value: "ChangesRequested",
    label: "Changes Requested",
  },
  { value: "Approved", label: "Approved" },
  { value: "Rejected", label: "Rejected" },
];

// ============================================================
// FORMAT STATUS
// ============================================================

function formatStatus(status) {
  switch (status) {
    case "PendingVerification":
      return "Pending Review";

    case "ChangesRequested":
      return "Changes Requested";

    case "Approved":
      return "Approved";

    case "Rejected":
      return "Rejected";

    case "Draft":
    default:
      return "Draft";
  }
}

// ============================================================
// FORMAT PRICE
// ============================================================

function formatPrice(price) {
  const numericPrice = Number(price);

  if (!Number.isFinite(numericPrice)) {
    return "Price unavailable";
  }

  return new Intl.NumberFormat("en-LK", {
    style: "currency",
    currency: "LKR",
    maximumFractionDigits: 0,
  }).format(numericPrice);
}

// ============================================================
// IMAGE URL - LOCAL + RENDER
//
// Uses the shared API configuration instead of hardcoding
// http://localhost:5198.
//
// Local:
// http://localhost:5198/uploads/gem-images/filename.png
//
// Production:
// https://gemora-api.onrender.com/uploads/gem-images/filename.png
// ============================================================

function getImageUrl(imageUrl) {
  return resolveApiAssetUrl(imageUrl);
}

// ============================================================
// GEM LISTING CARD
// ============================================================

function GemListingCard({
  listing,
  index,
  onView,
  onEdit,
}) {
  const [imageFailed, setImageFailed] = useState(false);

  // ----------------------------------------------------------
  // RESOLVE IMAGE URL
  // ----------------------------------------------------------

  const imageUrl = getImageUrl(
    listing.primaryImageUrl
  );

  // ----------------------------------------------------------
  // RESET IMAGE ERROR WHEN IMAGE URL CHANGES
  //
  // Important for replacement images.
  // ----------------------------------------------------------

  useEffect(() => {
    setImageFailed(false);
  }, [imageUrl]);

  // ----------------------------------------------------------
  // LISTING PERMISSIONS
  // ----------------------------------------------------------

  const canEdit =
    listing.status === "Draft" ||
    listing.status === "ChangesRequested";

  // These indicate stored evidence references.
  // imageFailed separately tracks actual browser image errors.

  const hasImage = Boolean(
    listing.primaryImageUrl
  );

  const hasCertificate = Boolean(
    listing.certificateUrl
  );

  const displayImage =
    Boolean(imageUrl) && !imageFailed;

  // ==========================================================
  // CARD UI
  // ==========================================================

  return (
    <article
      className="seller-listing-card"
      style={{
        "--listing-delay": `${Math.min(
          index * 70,
          420
        )}ms`,
      }}
    >
      {/* ======================================================
          GEMSTONE IMAGE
          ====================================================== */}

      <div className="seller-listing-image-area">

        {/* IMAGE WITH ERROR FALLBACK */}

        {displayImage ? (
          <img
            src={imageUrl}
            alt={
              listing.title ||
              "Gemstone listing"
            }
            className="seller-listing-image"
            loading="lazy"
            onError={() => {
              setImageFailed(true);
            }}
          />
        ) : (
          <div className="seller-listing-image-placeholder">

            <div className="seller-listing-placeholder-gem">
              G
            </div>

            <span>
              {imageFailed
                ? "Photograph unavailable"
                : hasImage
                ? "Image unavailable"
                : "No gemstone image"}
            </span>

          </div>
        )}

        {/* ====================================================
            STATUS BADGE
            ==================================================== */}

        <span
          className={`seller-listing-status status-${listing.status}`}
        >
          <span className="seller-listing-status-dot" />

          {formatStatus(listing.status)}
        </span>

        {/* ====================================================
            CARAT WEIGHT BADGE
            ==================================================== */}

        {listing.caratWeight && (
          <span className="seller-listing-carat">
            {listing.caratWeight} ct
          </span>
        )}

      </div>

      {/* ======================================================
          LISTING CONTENT
          ====================================================== */}

      <div className="seller-listing-card-content">

        {/* ====================================================
            HEADING
            ==================================================== */}

        <div className="seller-listing-card-heading">

          <div>
            <span className="seller-listing-type">
              {listing.gemType || "Gemstone"}
            </span>

            <h2>
              {listing.title || "Untitled Gemstone"}
            </h2>
          </div>

          <span className="seller-listing-id">
            #{listing.id}
          </span>

        </div>

        {/* ====================================================
            GEM CHARACTERISTICS
            ==================================================== */}

        <div className="seller-listing-characteristics">

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

        </div>

        {/* ====================================================
            LISTING PRICE
            ==================================================== */}

        <div className="seller-listing-price-row">

          <div>
            <span>Listing Price</span>

            <strong>
              {formatPrice(listing.price)}
            </strong>
          </div>

        </div>

        {/* ====================================================
            VERIFICATION EVIDENCE
            ==================================================== */}

        <div className="seller-listing-evidence">

          <span className="seller-evidence-title">
            Verification evidence
          </span>

          <div className="seller-evidence-items">

            {/* GEM IMAGE */}

            <span
              className={
                hasImage
                  ? "seller-evidence-item complete"
                  : "seller-evidence-item missing"
              }
            >
              <span>
                {hasImage ? "✓" : "○"}
              </span>

              Gem image
            </span>

            {/* CERTIFICATE */}

            <span
              className={
                hasCertificate
                  ? "seller-evidence-item complete"
                  : "seller-evidence-item missing"
              }
            >
              <span>
                {hasCertificate ? "✓" : "○"}
              </span>

              Certificate
            </span>

          </div>

        </div>

        {/* ====================================================
            LISTING ACTIONS
            ==================================================== */}

        <div className="seller-listing-actions">

          {/* VIEW DETAILS */}

          <button
            type="button"
            className="seller-listing-view-button"
            onClick={() => onView(listing.id)}
          >
            View Details

            <span>→</span>
          </button>

          {/* EDIT */}

          {canEdit && (
            <button
              type="button"
              className="seller-listing-edit-button"
              onClick={() => onEdit(listing.id)}
            >
              Edit
            </button>
          )}

        </div>

      </div>
    </article>
  );
}

// ============================================================
// MY GEM LISTINGS PAGE
// ============================================================

function MyGemListings() {
  const navigate = useNavigate();

  // ==========================================================
  // PAGE STATES
  // ==========================================================

  const [listings, setListings] = useState([]);

  const [loading, setLoading] = useState(true);

  const [error, setError] = useState("");

  // ==========================================================
  // FILTER STATES
  // ==========================================================

  const [searchTerm, setSearchTerm] = useState("");

  const [statusFilter, setStatusFilter] =
    useState("All");

  const [sortBy, setSortBy] = useState("newest");

  // ==========================================================
  // LOAD SELLER LISTINGS
  // ==========================================================

  useEffect(() => {
    let cancelled = false;

    const loadListings = async () => {
      try {
        setLoading(true);
        setError("");

        const data =
          await gemListingService.getMyListings();

        if (cancelled) {
          return;
        }

        setListings(
          Array.isArray(data) ? data : []
        );
      } catch (err) {
        if (cancelled) {
          return;
        }

        console.error(
          "Failed to load gem listings:",
          err
        );

        setError(
          "We couldn't load your gemstone listings. Please try again."
        );
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    loadListings();

    return () => {
      cancelled = true;
    };
  }, []);

  // ==========================================================
  // STATUS COUNTS
  // ==========================================================

  const counts = useMemo(() => {
    return {
      All: listings.length,

      Draft: listings.filter(
        (listing) =>
          listing.status === "Draft"
      ).length,

      PendingVerification: listings.filter(
        (listing) =>
          listing.status === "PendingVerification"
      ).length,

      ChangesRequested: listings.filter(
        (listing) =>
          listing.status === "ChangesRequested"
      ).length,

      Approved: listings.filter(
        (listing) =>
          listing.status === "Approved"
      ).length,

      Rejected: listings.filter(
        (listing) =>
          listing.status === "Rejected"
      ).length,
    };
  }, [listings]);

  // ==========================================================
  // SEARCH + FILTER + SORT
  // ==========================================================

  const visibleListings = useMemo(() => {
    const normalizedSearch = searchTerm
      .trim()
      .toLowerCase();

    // --------------------------------------------------------
    // FILTER LISTINGS
    // --------------------------------------------------------

    const filtered = listings.filter((listing) => {
      const matchesStatus =
        statusFilter === "All" ||
        listing.status === statusFilter;

      const searchableText = [
        listing.title,
        listing.gemType,
        listing.color,
        listing.clarity,
        listing.cut,
        listing.certificateNumber,
        listing.certificateAuthority,
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();

      const matchesSearch =
        normalizedSearch === "" ||
        searchableText.includes(normalizedSearch);

      return matchesStatus && matchesSearch;
    });

    // --------------------------------------------------------
    // SORT LISTINGS
    // --------------------------------------------------------

    return [...filtered].sort((first, second) => {
      switch (sortBy) {
        case "oldest":
          return (
            Number(first.id) -
            Number(second.id)
          );

        case "price-high":
          return (
            Number(second.price || 0) -
            Number(first.price || 0)
          );

        case "price-low":
          return (
            Number(first.price || 0) -
            Number(second.price || 0)
          );

        case "newest":
        default:
          return (
            Number(second.id) -
            Number(first.id)
          );
      }
    });
  }, [
    listings,
    searchTerm,
    statusFilter,
    sortBy,
  ]);

  // ==========================================================
  // RESET FILTERS
  // ==========================================================

  const clearFilters = () => {
    setSearchTerm("");
    setStatusFilter("All");
    setSortBy("newest");
  };

  // ==========================================================
  // PAGE UI
  // ==========================================================

  return (
    <DashboardLayout>

      <div className="seller-listings-page">

        {/* ====================================================
            PAGE HEADER
            ==================================================== */}

        <section className="seller-listings-header">

          <div>
            <span className="seller-listings-eyebrow">
              SELLER INVENTORY
            </span>

            <h1>My Gem Listings</h1>

            <p>
              Manage gemstone evidence, verification
              progress, and marketplace listing information.
            </p>
          </div>

          <button
            type="button"
            className="seller-listings-create-button"
            onClick={() =>
              navigate("/seller/listings/create")
            }
          >
            <span>+</span>

            Create Gem Listing
          </button>

        </section>

        {/* ====================================================
            SUMMARY STRIP
            ==================================================== */}

        <section className="seller-listings-summary">

          <div>
            <span>Total</span>

            <strong>
              {loading ? "—" : counts.All}
            </strong>
          </div>

          <div>
            <span>Draft</span>

            <strong>
              {loading ? "—" : counts.Draft}
            </strong>
          </div>

          <div>
            <span>Pending</span>

            <strong>
              {loading
                ? "—"
                : counts.PendingVerification}
            </strong>
          </div>

          <div>
            <span>Approved</span>

            <strong>
              {loading ? "—" : counts.Approved}
            </strong>
          </div>

          <div>
            <span>Needs Attention</span>

            <strong>
              {loading
                ? "—"
                : counts.ChangesRequested}
            </strong>
          </div>

        </section>

        {/* ====================================================
            SEARCH + SORT TOOLBAR
            ==================================================== */}

        <section className="seller-listings-toolbar">

          {/* SEARCH */}

          <div className="seller-listings-search">

            <span className="seller-search-icon">
              ⌕
            </span>

            <input
              type="search"
              value={searchTerm}
              onChange={(event) =>
                setSearchTerm(event.target.value)
              }
              placeholder="Search by title, gem type, color, certificate..."
              aria-label="Search gemstone listings"
            />

            {searchTerm && (
              <button
                type="button"
                className="seller-search-clear"
                onClick={() => setSearchTerm("")}
                aria-label="Clear search"
              >
                ×
              </button>
            )}

          </div>

          {/* SORT */}

          <div className="seller-listings-sort">

            <label htmlFor="listing-sort">
              Sort
            </label>

            <select
              id="listing-sort"
              value={sortBy}
              onChange={(event) =>
                setSortBy(event.target.value)
              }
            >
              <option value="newest">
                Newest first
              </option>

              <option value="oldest">
                Oldest first
              </option>

              <option value="price-high">
                Price: High to Low
              </option>

              <option value="price-low">
                Price: Low to High
              </option>
            </select>

          </div>

        </section>

        {/* ====================================================
            STATUS FILTER BUTTONS
            ==================================================== */}

        <div className="seller-listing-filter-row">

          {FILTERS.map((filter) => (
            <button
              key={filter.value}
              type="button"
              className={
                statusFilter === filter.value
                  ? "seller-filter-button active"
                  : "seller-filter-button"
              }
              onClick={() =>
                setStatusFilter(filter.value)
              }
            >
              {filter.label}

              <span>
                {counts[filter.value] || 0}
              </span>
            </button>
          ))}

        </div>

        {/* ====================================================
            RESULT INFORMATION
            ==================================================== */}

        {!loading && !error && (
          <div className="seller-listings-result-info">

            <span>
              Showing{" "}
              <strong>
                {visibleListings.length}
              </strong>{" "}
              of{" "}
              <strong>
                {listings.length}
              </strong>{" "}
              listings
            </span>

            {(searchTerm ||
              statusFilter !== "All" ||
              sortBy !== "newest") && (
              <button
                type="button"
                onClick={clearFilters}
              >
                Reset filters
              </button>
            )}

          </div>
        )}

        {/* ====================================================
            LOADING STATE
            ==================================================== */}

        {loading && (
          <div className="seller-listings-loading">

            <div className="seller-listings-loader" />

            <h3>
              Loading your gemstones
            </h3>

            <p>
              Retrieving listing and verification
              information.
            </p>

          </div>
        )}

        {/* ====================================================
            ERROR STATE
            ==================================================== */}

        {!loading && error && (
          <div className="seller-listings-error">

            <div>!</div>

            <h3>
              Unable to load listings
            </h3>

            <p>{error}</p>

            <button
              type="button"
              onClick={() =>
                window.location.reload()
              }
            >
              Try Again
            </button>

          </div>
        )}

        {/* ====================================================
            EMPTY ACCOUNT STATE
            ==================================================== */}

        {!loading &&
          !error &&
          listings.length === 0 && (
            <div className="seller-listings-empty">

              <div className="seller-empty-gem">
                G
              </div>

              <span>
                YOUR GEM COLLECTION
              </span>

              <h2>
                Create your first gemstone listing
              </h2>

              <p>
                Add gemstone characteristics, evidence
                and certificate information to begin
                the verification workflow.
              </p>

              <button
                type="button"
                onClick={() =>
                  navigate("/seller/listings/create")
                }
              >
                + Create Gem Listing
              </button>

            </div>
          )}

        {/* ====================================================
            NO FILTER RESULTS
            ==================================================== */}

        {!loading &&
          !error &&
          listings.length > 0 &&
          visibleListings.length === 0 && (
            <div className="seller-listings-no-results">

              <div>⌕</div>

              <h3>
                No matching listings
              </h3>

              <p>
                Try another search term or choose a
                different verification status.
              </p>

              <button
                type="button"
                onClick={clearFilters}
              >
                Clear Filters
              </button>

            </div>
          )}

        {/* ====================================================
            LISTING GRID
            ==================================================== */}

        {!loading &&
          !error &&
          visibleListings.length > 0 && (
            <section className="seller-listing-grid">

              {visibleListings.map((listing, index) => (
                <GemListingCard
                  key={listing.id}
                  listing={listing}
                  index={index}
                  onView={(listingId) =>
                    navigate(
                      `/seller/listings/${listingId}`
                    )
                  }
                  onEdit={(listingId) =>
                    navigate(
                      `/seller/listings/${listingId}/edit`
                    )
                  }
                />
              ))}

            </section>
          )}

      </div>

    </DashboardLayout>
  );
}

export default MyGemListings;
