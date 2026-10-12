import { useEffect, useMemo, useState } from "react";
import AdminLayout from "../../layouts/AdminLayout";
import transactionService from "../../services/marketplace/transactionService";

function TransactionDashboard() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [search, setSearch] = useState("");
  const [request, setRequest] = useState(0);

  useEffect(() => {
    let active = true;
    Promise.resolve().then(() => transactionService.getAll())
      .then((response) => {
        if (!Array.isArray(response)) throw new Error("Unexpected transaction response.");
        if (active) { setOrders(response); setError(""); }
      })
      .catch((cause) => {
        if (active) setError(cause.response?.status === 401 || cause.response?.status === 403
          ? "Your session cannot access transactions. Sign in again or contact an administrator."
          : "Unable to load transactions.");
      })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [request]);
  const retry = () => { setLoading(true); setError(""); setRequest((current) => current + 1); };

  const visible = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return orders;
    return orders.filter((o) =>
      [o.orderNumber, o.gemTitle, o.buyerName, o.sellerName, o.status]
        .some((v) => String(v ?? "").toLowerCase().includes(q))
    );
  }, [orders, search]);

  return (
    <AdminLayout title="Transactions">
      <section className="admin-transactions">
        <div className="admin-transactions-header">
          <div><p className="admin-section-eyebrow">MARKETPLACE · ORDERS</p><h1>Marketplace Transactions</h1><p>Review marketplace orders and their current status.</p></div>
          <input className="admin-transactions-search" aria-label="Search transactions" value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search order, gem, buyer..." />
        </div>
        {loading && <div className="admin-transactions-state" role="status">Loading transactions...</div>}
        {error && <div className="admin-transactions-state" role="alert">{error} <button type="button" onClick={retry}>Retry</button></div>}
        {!loading && !error && <div className="admin-transactions-table"><table>
          <thead><tr>{["Order", "Gem", "Buyer", "Seller", "Amount", "Status", "Created"].map((heading) => <th scope="col" key={heading}>{heading}</th>)}</tr></thead>
          <tbody>{visible.map((order) => <tr key={order.id}>
            <td>{order.orderNumber}</td><td>{order.gemTitle}</td><td>{order.buyerName}</td><td>{order.sellerName}</td>
            <td>{order.currency} {Number(order.agreedPrice).toLocaleString()}</td><td><strong>{order.status}</strong></td>
            <td>{order.createdAt ? new Date(order.createdAt).toLocaleString() : "—"}</td>
          </tr>)}
          {visible.length === 0 && <tr><td colSpan="7" className="admin-transactions-state">No transactions found.</td></tr>}</tbody>
        </table></div>}
      </section>
    </AdminLayout>
  );
}

export default TransactionDashboard;
