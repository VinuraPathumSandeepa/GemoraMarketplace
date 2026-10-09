
import { useState } from "react";
import { Heart, LoaderCircle } from "lucide-react";
import { useWishlist } from "../../context/WishlistContext";

export default function WishlistButton({ gem }) {
  const {
    items,
    loading,
    error: loadError,
    pending,
    toggle
  } = useWishlist();

  const [actionError, setActionError] = useState("");

  const id = Number(gem?.id ?? gem?.gemListingId);
  const validId = Number.isSafeInteger(id) && id > 0;

  const isSaved = items.some(
    (item) => Number(item.gemListingId) === id
  );

  const isBusy = pending.has(id);

  const disabled =
    !validId || loading || isBusy || Boolean(loadError);

  const title =
    gem?.title || gem?.gemTitle || "gemstone";

  const handleClick = async () => {
    setActionError("");

    try {
      await toggle(gem);
    } catch (err) {
      setActionError(
        err?.message || "Wishlist could not be updated."
      );
    }
  };

  return (
    <div className="gm-wishlist-action">
      <button
        type="button"
        className="gm-button gm-button-secondary"
        aria-pressed={isSaved}
        aria-label={
          `${isSaved ? "Remove from" : "Add to"} wishlist: ${title}`
        }
        disabled={disabled}
        onClick={handleClick}
      >
        {isBusy ? (
          <LoaderCircle size={18} aria-hidden="true" />
        ) : (
          <Heart
            size={18}
            fill={isSaved ? "currentColor" : "none"}
            aria-hidden="true"
          />
        )}

        {isBusy
          ? "Saving..."
          : isSaved
          ? "Saved"
          : "Save gem"}
      </button>

      {actionError && (
        <p role="alert" className="gm-wishlist-error">
          {actionError}
        </p>
      )}

      {loadError && !actionError && (
        <p role="alert" className="gm-wishlist-error">
          {loadError}
        </p>
      )}
    </div>
  );
}
  