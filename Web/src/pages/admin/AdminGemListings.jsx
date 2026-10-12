import { useEffect, useMemo, useState } from "react";
import { Search, Trash2 } from "lucide-react";
import DashboardLayout from "../../layouts/DashboardLayout";
import adminGemListingService from "../../services/adminGemListingService";
import { resolveApiAssetUrl } from "../../services/api";
import "../../styles/admin-gem-listings.css";

export default function AdminGemListings() {
  const [listings, setListings] = useState([]);
  const [query, setQuery] = useState("");
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    adminGemListingService.getAll()
      .then(setListings)
      .catch((cause) => setError(cause.response?.data?.message || "Unable to load gem listings."))
      .finally(() => setLoading(false));
  }, []);

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

  return <DashboardLayout title="Manage Gem Listings">
    <main className="admin-gems-page">
      <header className="admin-gems-header"><div><span>ADMIN · MARKETPLACE</span><h1>Gem listings</h1><p>Review every seller listing, including sold gemstones. Deleted listings leave the marketplace while order records remain.</p></div><strong>{listings.length} listings</strong></header>
      <label className="admin-gems-search"><Search size={20} aria-hidden="true" /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search gems, sellers, status or ID" aria-label="Search admin gem listings" /></label>
      {error && <p className="admin-gems-error" role="alert">{error}</p>}
      {loading ? <p className="admin-gems-empty">Loading gem listings…</p> : visible.length === 0 ? <p className="admin-gems-empty">No gem listings found.</p> : <div className="admin-gems-list">{visible.map((listing) => <article className="admin-gem-row" key={listing.id}>
        <div className="admin-gem-image">{listing.primaryImageUrl ? <img src={resolveApiAssetUrl(listing.primaryImageUrl)} alt="" /> : <span>◆</span>}</div>
        <div className="admin-gem-copy"><small>#{listing.id} · {listing.gemType}</small><h2>{listing.title}</h2><p>Sold by {listing.sellerName || "Unknown seller"} · {listing.currency} {Number(listing.price).toLocaleString()}</p></div>
        <span className={`admin-gem-status ${listing.status === "Sold" ? "is-sold" : ""}`}>{listing.status === "PendingVerification" ? "Pending review" : listing.status}</span>
        <button type="button" className="admin-gem-delete" disabled={busyId !== null} onClick={() => remove(listing)} aria-label={`Delete ${listing.title}`}><Trash2 size={17} />{busyId === listing.id ? "Deleting…" : "Delete"}</button>
      </article>)}</div>}
    </main>
  </DashboardLayout>;
}
