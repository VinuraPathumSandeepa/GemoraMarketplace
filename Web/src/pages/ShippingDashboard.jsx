import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import DashboardLayout from "../layouts/DashboardLayout";
import api from "../services/api";

function ShippingDashboard() {
  const [shipments, setShipments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const loadShipments = async () => {
    try {
      setLoading(true);
      setError("");

      const response = await api.get("/Shipments/my");
      setShipments(response.data || []);
    } catch (err) {
      console.error("Failed to load shipments:", err);
      setError(
        err?.response?.data?.message ||
          "Unable to load shipment records right now."
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadShipments();
  }, []);

  return (
    <DashboardLayout title="Shipping Dashboard">
      <div style={{ marginTop: "24px" }}>
        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            gap: "16px",
            marginBottom: "20px",
            flexWrap: "wrap",
          }}
        >
          <h3 style={{ margin: 0 }}>Shipment overview</h3>

          <div style={{ display: "flex", gap: "12px", alignItems: "center" }}>
            <button onClick={loadShipments} type="button">
              Refresh
            </button>
            <Link to="/admin">Back to Admin</Link>
          </div>
        </div>

        {error && (
          <div
            style={{
              background: "#fff4f4",
              color: "#b42318",
              border: "1px solid #fecdca",
              borderRadius: "8px",
              padding: "12px 14px",
              marginBottom: "16px",
            }}
          >
            {error}
          </div>
        )}

        {loading ? (
          <p>Loading shipments...</p>
        ) : shipments.length === 0 ? (
          <div
            style={{
              background: "#f8fafc",
              border: "1px solid #e2e8f0",
              borderRadius: "10px",
              padding: "18px",
            }}
          >
            No shipments are available yet.
          </div>
        ) : (
          <div
            style={{
              overflowX: "auto",
              border: "1px solid #e2e8f0",
              borderRadius: "10px",
              background: "#fff",
            }}
          >
            <table style={{ width: "100%", borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "#f8fafc" }}>
                  <th style={{ padding: "12px", textAlign: "left" }}>Shipment</th>
                  <th style={{ padding: "12px", textAlign: "left" }}>Order</th>
                  <th style={{ padding: "12px", textAlign: "left" }}>Route</th>
                  <th style={{ padding: "12px", textAlign: "left" }}>Status</th>
                  <th style={{ padding: "12px", textAlign: "left" }}>Value</th>
                  <th style={{ padding: "12px", textAlign: "left" }}>Service</th>
                </tr>
              </thead>
              <tbody>
                {shipments.map((shipment) => (
                  <tr key={shipment.id} style={{ borderTop: "1px solid #edf2f7" }}>
                    <td style={{ padding: "12px" }}>{shipment.shipmentNumber || shipment.id}</td>
                    <td style={{ padding: "12px" }}>{shipment.orderId}</td>
                    <td style={{ padding: "12px" }}>
                      {shipment.origin} → {shipment.destination}
                    </td>
                    <td style={{ padding: "12px" }}>
                      <span
                        style={{
                          background: "#ecfeff",
                          color: "#0f172a",
                          padding: "5px 8px",
                          borderRadius: "999px",
                          fontSize: "12px",
                          display: "inline-block",
                        }}
                      >
                        {shipment.status}
                      </span>
                    </td>
                    <td style={{ padding: "12px" }}>
                      {shipment.declaredValue} {shipment.currency || "USD"}
                    </td>
                    <td style={{ padding: "12px" }}>{shipment.selectedService}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </DashboardLayout>
  );
}

export default ShippingDashboard;
