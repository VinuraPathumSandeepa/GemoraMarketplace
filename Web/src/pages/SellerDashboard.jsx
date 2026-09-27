import { useEffect, useState } from "react";
import DashboardLayout from "../layouts/DashboardLayout";
import api from "../services/api";

const emptyForm = {
  orderId: "",
  origin: "",
  destination: "",
  declaredValue: "",
  currency: "USD",
  packageDescription: "",
  selectedService: "Standard Courier",
  courierName: "",
};

function SellerDashboard() {
  const [form, setForm] = useState(emptyForm);
  const [shipments, setShipments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const loadShipments = async () => {
    try {
      setLoading(true);
      const response = await api.get("/Shipments/my");
      setShipments(response.data || []);
    } catch (err) {
      console.error("Failed to load seller shipments:", err);
      setError(
        err?.response?.data?.message ||
          "Unable to load your shipment records right now."
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadShipments();
  }, []);

  const handleChange = (event) => {
    const { name, value } = event.target;
    setForm((current) => ({
      ...current,
      [name]: value,
    }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSubmitting(true);
    setError("");
    setSuccess("");

    try {
      const orderId = form.orderId.trim();
      const origin = form.origin.trim();
      const destination = form.destination.trim();
      const packageDescription = form.packageDescription.trim();
      const selectedService = form.selectedService.trim();

      if (!orderId || !origin || !destination || !packageDescription || !selectedService) {
        throw new Error("Please complete all required shipment details.");
      }

      const declaredValue = Number(form.declaredValue);
      if (!Number.isFinite(declaredValue) || declaredValue <= 0) {
        throw new Error("Declared value must be greater than zero.");
      }

      const payload = {
        orderId,
        origin,
        destination,
        declaredValue,
        currency: form.currency || "USD",
        packageDescription,
        selectedService,
        courierName: form.courierName.trim(),
      };

      await api.post("/Shipments", payload);

      setSuccess("Shipment created successfully.");
      setForm(emptyForm);
      await loadShipments();
    } catch (err) {
      console.error("Failed to create shipment:", err);
      setError(
        err?.response?.data?.message ||
          err?.message ||
          "Unable to create shipment right now."
      );
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <DashboardLayout title="Seller Dashboard">
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "1.2fr 0.8fr",
          gap: "24px",
          marginTop: "24px",
        }}
      >
        <section
          style={{
            background: "#fff",
            border: "1px solid #e2e8f0",
            borderRadius: "12px",
            padding: "20px",
          }}
        >
          <h3 style={{ marginTop: 0, marginBottom: "16px" }}>
            Create shipment
          </h3>

          {error && (
            <div
              style={{
                background: "#fff4f4",
                color: "#b42318",
                border: "1px solid #fecdca",
                borderRadius: "8px",
                padding: "12px 14px",
                marginBottom: "14px",
              }}
            >
              {error}
            </div>
          )}

          {success && (
            <div
              style={{
                background: "#ecfdf5",
                color: "#027a48",
                border: "1px solid #a7f3d0",
                borderRadius: "8px",
                padding: "12px 14px",
                marginBottom: "14px",
              }}
            >
              {success}
            </div>
          )}

          <form onSubmit={handleSubmit}>
            <div
              style={{
                display: "grid",
                gridTemplateColumns: "repeat(2, minmax(0, 1fr))",
                gap: "16px",
              }}
            >
              <label style={{ display: "grid", gap: "8px" }}>
                <span>Order ID</span>
                <input
                  type="text"
                  name="orderId"
                  value={form.orderId}
                  onChange={handleChange}
                  placeholder="UUID for the marketplace order"
                  style={inputStyle}
                />
              </label>

              <label style={{ display: "grid", gap: "8px" }}>
                <span>Currency</span>
                <select
                  name="currency"
                  value={form.currency}
                  onChange={handleChange}
                  style={inputStyle}
                >
                  <option value="USD">USD</option>
                  <option value="LKR">LKR</option>
                  <option value="EUR">EUR</option>
                </select>
              </label>

              <label style={{ display: "grid", gap: "8px" }}>
                <span>Origin</span>
                <input
                  type="text"
                  name="origin"
                  value={form.origin}
                  onChange={handleChange}
                  placeholder="Colombo, Sri Lanka"
                  style={inputStyle}
                />
              </label>

              <label style={{ display: "grid", gap: "8px" }}>
                <span>Destination</span>
                <input
                  type="text"
                  name="destination"
                  value={form.destination}
                  onChange={handleChange}
                  placeholder="Dubai, UAE"
                  style={inputStyle}
                />
              </label>

              <label style={{ display: "grid", gap: "8px" }}>
                <span>Declared value</span>
                <input
                  type="number"
                  name="declaredValue"
                  value={form.declaredValue}
                  onChange={handleChange}
                  min="0.01"
                  step="0.01"
                  placeholder="2500"
                  style={inputStyle}
                />
              </label>

              <label style={{ display: "grid", gap: "8px" }}>
                <span>Service</span>
                <select
                  name="selectedService"
                  value={form.selectedService}
                  onChange={handleChange}
                  style={inputStyle}
                >
                  <option value="Standard Courier">Standard Courier</option>
                  <option value="Priority Air Freight">Priority Air Freight</option>
                  <option value="Express Shipping">Express Shipping</option>
                  <option value="Sea Freight">Sea Freight</option>
                </select>
              </label>

              <label style={{ display: "grid", gap: "8px", gridColumn: "1 / -1" }}>
                <span>Package description</span>
                <textarea
                  name="packageDescription"
                  value={form.packageDescription}
                  onChange={handleChange}
                  rows="4"
                  placeholder="Describe the package contents and shipping context"
                  style={{ ...inputStyle, resize: "vertical" }}
                />
              </label>

              <label style={{ display: "grid", gap: "8px", gridColumn: "1 / -1" }}>
                <span>Courier name (optional)</span>
                <input
                  type="text"
                  name="courierName"
                  value={form.courierName}
                  onChange={handleChange}
                  placeholder="DHL, FedEx, etc."
                  style={inputStyle}
                />
              </label>
            </div>

            <div style={{ marginTop: "20px", display: "flex", gap: "12px" }}>
              <button type="submit" disabled={submitting}>
                {submitting ? "Creating..." : "Create shipment"}
              </button>
            </div>
          </form>
        </section>

        <aside
          style={{
            background: "#f8fafc",
            border: "1px solid #e2e8f0",
            borderRadius: "12px",
            padding: "20px",
          }}
        >
          <h3 style={{ marginTop: 0 }}>Recent shipments</h3>

          {loading ? (
            <p>Loading shipments...</p>
          ) : shipments.length === 0 ? (
            <p>No shipments created yet.</p>
          ) : (
            <div style={{ display: "grid", gap: "12px" }}>
              {shipments.slice(0, 5).map((shipment) => (
                <div
                  key={shipment.id}
                  style={{
                    background: "#fff",
                    border: "1px solid #e2e8f0",
                    borderRadius: "10px",
                    padding: "12px",
                  }}
                >
                  <strong>{shipment.shipmentNumber || shipment.id}</strong>
                  <div style={{ color: "#475467", marginTop: "6px" }}>
                    {shipment.origin} → {shipment.destination}
                  </div>
                  <div style={{ color: "#475467", marginTop: "4px" }}>
                    {shipment.status} • {shipment.selectedService}
                  </div>
                </div>
              ))}
            </div>
          )}
        </aside>
      </div>
    </DashboardLayout>
  );
}

const inputStyle = {
  width: "100%",
  padding: "10px 12px",
  border: "1px solid #cbd5e1",
  borderRadius: "8px",
  fontSize: "14px",
  boxSizing: "border-box",
};

export default SellerDashboard;