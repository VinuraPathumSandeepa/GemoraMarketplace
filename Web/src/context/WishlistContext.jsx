
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
} from "react";
import api from "../services/api";

const WishlistContext = createContext(null);

function wishlistError(error) {
  const status = error?.response?.status;

  if (status === 401)
    return "Your session expired. Please log in again.";

  if (status === 403)
    return "Only buyer accounts can use the wishlist.";

  if (status === 503)
    return "Wishlist storage is not ready. Please try again after the backend is restarted.";

  if (!error?.response && error?.code === "ERR_NETWORK") {
    return "Could not reach the API. Check that the backend is running on port 5198.";
  }

  return (
    error?.response?.data?.message ||
    error?.response?.data?.detail ||
    error?.message ||
    "Wishlist request failed. Please try again."
  );
}

export function WishlistProvider({ children }) {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [pending, setPending] = useState(new Set());

  const active = useRef(new Set());

  const reload = useCallback(async (signal) => {
    setLoading(true);
    setError("");

    try {
      const response = await api.get("/wishlist", { signal });

      if (signal?.aborted) return;

      setItems(
        Array.isArray(response.data) ? response.data : []
      );
    } catch (err) {
      if (signal?.aborted || err?.code === "ERR_CANCELED")
        return;

      const message = wishlistError(err);
      setError(message);
      throw new Error(message);
    } finally {
      if (!signal?.aborted)
        setLoading(false);
    }
  }, []);

  useEffect(() => {
    const controller = new AbortController();

    reload(controller.signal).catch(() => {});

    return () => controller.abort();
  }, [reload]);

  const toggle = useCallback(async (gem) => {
    const id = Number(gem?.id ?? gem?.gemListingId);

    if (!Number.isSafeInteger(id) || id <= 0) {
      throw new Error(
        "This gemstone does not have a valid ID."
      );
    }

    if (loading || error) {
      throw new Error(
        error || "Wishlist is still loading. Try again in a moment."
      );
    }

    if (active.current.has(id)) return;

    const wasSaved = items.some(
      (x) => Number(x.gemListingId) === id
    );

    active.current.add(id);
    setPending(new Set(active.current));

    try {
      if (wasSaved) {
        await api.delete(`/wishlist/${id}`);
      } else {
        await api.put(`/wishlist/${id}`);
      }

      await reload();
    } catch (err) {
      throw new Error(wishlistError(err));
    } finally {
      active.current.delete(id);
      setPending(new Set(active.current));
    }
  }, [items, loading, error, reload]);

  return (
    <WishlistContext.Provider
      value={{
        items,
        loading,
        error,
        pending,
        reload,
        toggle
      }}
    >
      {children}
    </WishlistContext.Provider>
  );
}

export function useWishlist() {
  const context = useContext(WishlistContext);

  if (!context) {
    throw new Error(
      "useWishlist must be used inside WishlistProvider."
    );
  }

  return context;
}
  