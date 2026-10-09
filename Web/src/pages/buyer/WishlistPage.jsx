
import { Heart } from "lucide-react";
import { Link } from "react-router-dom";

import { useWishlist } from "../../context/WishlistContext";
import GemCard from "../../components/buyer/GemCard";
import WishlistButton from "../../components/buyer/WishlistButton";
import {
  PageHeading,
  SkeletonGrid
} from "../../components/buyer/BuyerUI";

export default function WishlistPage() {
  const { items, loading, error, reload } = useWishlist();

  return (
    <div className="gm-page">
      <PageHeading
        eyebrow="YOUR PERSONAL COLLECTION"
        title="Gems worth remembering."
        description="Save your favourites, compare their details, and return when you are ready. Saving a gem does not reserve it."
      />

      {loading ? (
        <SkeletonGrid count={3} />
      ) : error ? (
        <section className="gm-glass gm-notes" role="alert">
          <p>{error}</p>

          <button
            className="gm-button"
            onClick={() => reload().catch(() => {})}
          >
            Try again
          </button>
        </section>
      ) : items.length > 0 ? (
        <>
          <p>
            {items.length} saved gemstone
            {items.length === 1 ? "" : "s"}
          </p>

          <div className="gm-gem-grid">
            {items.map((item) =>
              item.gem ? (
                <GemCard
                  key={item.gemListingId}
                  gem={item.gem}
                />
              ) : (
                <article
                  key={item.gemListingId}
                  className="gm-glass gm-notes"
                >
                  <Heart />

                  <h2>Listing unavailable</h2>

                  <p>
                    This gem is no longer an approved listing.
                    You can remove it from your wishlist
                    without affecting any orders.
                  </p>

                  <WishlistButton
                    gem={{
                      id: item.gemListingId,
                      title: "Unavailable gemstone"
                    }}
                  />
                </article>
              )
            )}
          </div>
        </>
      ) : (
        <section className="gm-glass gm-notes">
          <Heart size={32} />

          <h2>Your next discovery starts here</h2>

          <p>
            Select “Save gem” on a listing to keep it
            in your wishlist.
          </p>

          <Link
            className="gm-button"
            to="/buyer/marketplace"
          >
            Explore gemstones
          </Link>
        </section>
      )}
    </div>
  );
}
  