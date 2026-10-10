import { useEffect, useMemo, useState } from "react";
import DashboardLayout from "../../layouts/DashboardLayout";
import transactionService from "../../services/marketplace/transactionService";

function TransactionDashboard() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [search, setSearch] = useState("");

  useEffect(() => {
    const load = async () => {
      try {
        setLoading(true);
        setOrders(await transactionService.getAll());
      } catch (e) {
        console.error(e);
        setError("Unable to load transactions.");
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const visible = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return orders;
    return orders.filter((o) =>
      [o.orderNumber, o.gemTitle, o.buyerName, o.sellerName, o.status]
        .some((v) => String(v ?? "").toLowerCase().includes(q))
    );
  }, [orders, search]);

  return (
    <DashboardLayout title="Transaction Dashboard">
      <div style={{ maxWidth: 1180, margin: "0 auto", padding: "12px" }}>
        <div style={{ display: "flex", justifyContent: "space-between", gap: 16, alignItems: "center", marginBottom: 20 }}>
          <div>
            <h2 style={{ margin: 0 }}>Marketplace Transactions</h2>
            <p style={{ marginTop: 6, opacity: 0.7 }}>Admin oversight for Component 2 orders and status.</p>
          </div>
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search order, gem, buyer..."
            style={{ minWidth: 280, padding: "11px 13px", border: "1px solid #d8d8d8", borderRadius: 10 }}
          />
        </div>

        {loading && <p>Loading transactions...</p>}
        {error && <p style={{ color: "crimson" }}>{error}</p>}

        {!loading && !error && (
          <div style={{ overflowX: "auto", background: "white", border: "1px solid #ececec", borderRadius: 14 }}>
            <table style={{ width: "100%", borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ textAlign: "left", background: "#f7f7f8" }}>
                  {['Order','Gem','Buyer','Seller','Amount','Status','Created'].map((h) => (
                    <th key={h} style={{ padding: 14, borderBottom: "1px solid #e8e8e8" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {visible.map((o) => (
                  <tr key={o.id}>
                    <td style={{ padding: 14, borderBottom: "1px solid #f0f0f0" }}>{o.orderNumber}</td>
                    <td style={{ padding: 14, borderBottom: "1px solid #f0f0f0" }}>{o.gemTitle}</td>
                    <td style={{ padding: 14, borderBottom: "1px solid #f0f0f0" }}>{o.buyerName}</td>
                    <td style={{ padding: 14, borderBottom: "1px solid #f0f0f0" }}>{o.sellerName}</td>
                    <td style={{ padding: 14, borderBottom: "1px solid #f0f0f0" }}>{o.currency} {Number(o.agreedPrice).toLocaleString()}</td>
                    <td style={{ padding: 14, borderBottom: "1px solid #f0f0f0" }}><strong>{o.status}</strong></td>
                    <td style={{ padding: 14, borderBottom: "1px solid #f0f0f0" }}>{new Date(o.createdAt).toLocaleString()}</td>
                  </tr>
                ))}
                {visible.length === 0 && (
                  <tr><td colSpan="7" style={{ padding: 28, textAlign: "center" }}>No transactions found.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </DashboardLayout>
  );
}

export default TransactionDashboard;
