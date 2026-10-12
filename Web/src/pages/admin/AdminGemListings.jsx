import { useEffect, useMemo, useState } from "react";
import { Search, Trash2 } from "lucide-react";
import AdminLayout from "../../layouts/AdminLayout";
import adminGemListingService from "../../services/adminGemListingService";
import { resolveApiAssetUrl } from "../../services/api";
import "../../styles/admin-gem-listings.css";

export default function AdminGemListings() {
  const [listings, setListings] = useState([]);
  const [query, setQuery] = useState("");
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState(null);
  const [error, setError] = useState("");
  const [request, setRequest] = useState(0);

  useEffect(() => {
    let active = true;
    Promise.resolve().then(() => adminGemListingService.getAll())
      .then((response) => {
        if (!Array.isArray(response)) throw new Error("Unexpected gem listing response.");
        if (active) { setListings(response); setError(""); }
      })
      .catch((cause) => {
        if (active) setError(cause.response?.status === 401 || cause.response?.status === 403
          ? "Your session cannot access gem listings. Sign in again or contact an administrator."
          : cause.response?.data?.message || "Unable to load gem listings.");
      })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [request]);
  const retry = () => { setLoading(true); setError(""); setRequest((current) => current + 1); };

  const visible = useMemo(() => {
    const needle = query.trim().toLowerCase();
    return listings.filter((listing) => [listing.title, listing.sellerName, listing.gemType, listing.status, String(listing.id)]
      .some((value) => value?.toLowerCase().includes(needle)));
  }, [listings, query]);

  const remove = async (listing) => {
    if (!window.confirm(`Delete “${listing.title}” from gem listings? Its order history will remain available.`)) return;
    setBusyId(listing.id);
    setError("");
    try {
      await adminGemListingService.remove(listing.id);
      setListings((current) => current.filter((item) => item.id !== listing.id));
    } catch (cause) {
      setError(cause.response?.data?.message || "Unable to delete this gem listing.");
    } finally {
      setBusyId(null);
    }
  };

  return <AdminLayout title="Manage Gem Listings">
    <div className="admin-gems-page">
      <header className="admin-gems-header"><div><span>ADMIN · MARKETPLACE</span><h1>Gem listings</h1><p>Review every seller listing, including sold gemstones. Deleted listings leave the marketplace while order records remain.</p></div><strong>{listings.length} listings</strong></header>
      <label className="admin-gems-search"><Search size={20} aria-hidden="true" /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search gems, sellers, status or ID" aria-label="Search admin gem listings" /></label>
      {error && <div className="admin-gems-error" role="alert">{error}{!loading && listings.length === 0 && <button type="button" onClick={retry}>Retry</button>}</div>}
      {loading ? <p className="admin-gems-empty" role="status">Loading gem listings…</p> : error && listings.length === 0 ? null : visible.length === 0 ? <p className="admin-gems-empty">No gem listings found.</p> : <div className="admin-gems-list">{visible.map((listing) => <article className="admin-gem-row" key={listing.id}>
        <div className="admin-gem-image">{listing.primaryImageUrl ? <img src={resolveApiAssetUrl(listing.primaryImageUrl)} alt="" /> : <span>◆</span>}</div>
        <div className="admin-gem-copy"><small>#{listing.id} · {listing.gemType}</small><h2>{listing.title}</h2><p>Sold by {listing.sellerName || "Unknown seller"} · {listing.currency} {Number(listing.price).toLocaleString()}</p></div>
        <span className={`admin-gem-status ${listing.status === "Sold" ? "is-sold" : ""}`}>{listing.status === "PendingVerification" ? "Pending review" : listing.status}</span>
        <button type="button" className="admin-gem-delete" disabled={busyId !== null} onClick={() => remove(listing)} aria-label={`Delete ${listing.title}`}><Trash2 size={17} />{busyId === listing.id ? "Deleting…" : "Delete"}</button>
      </article>)}</div>}
    </div>
  </AdminLayout>;
}
