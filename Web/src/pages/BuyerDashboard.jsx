import { useEffect, useMemo, useState } from "react";
import DashboardLayout from "../layouts/DashboardLayout";
import api from "../services/api";

function BuyerDashboard() {
  const [shipments, setShipments] = useState([]);
  const [selectedShipmentId, setSelectedShipmentId] = useState("");
  const [trackingEvents, setTrackingEvents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [trackingLoading, setTrackingLoading] = useState(false);
  const [error, setError] = useState("");

  const selectedShipment = useMemo(
    () => shipments.find((shipment) => shipment.id === selectedShipmentId) || null,
    [shipments, selectedShipmentId]
  );

  const loadShipments = async () => {
    try {
      setLoading(true);
      setError("");
      const response = await api.get("/Shipments/my");
      const allShipments = response.data || [];
      setShipments(allShipments);

      if (allShipments.length > 0 && !selectedShipmentId) {
        setSelectedShipmentId(allShipments[0].id);
      }
    } catch (err) {
      console.error("Failed to load buyer shipments:", err);
      setError(
        err?.response?.data?.message ||
          "Unable to load your shipment tracking information right now."
      );
    } finally {
      setLoading(false);
    }
  };

  const loadTracking = async (shipmentId) => {
    if (!shipmentId) {
      setTrackingEvents([]);
      return;
    }

    try {
      setTrackingLoading(true);
      const response = await api.get(`/Shipments/${shipmentId}/tracking`);
      setTrackingEvents(response.data || []);
    } catch (err) {
      console.error("Failed to load tracking events:", err);
      setTrackingEvents([]);
      setError(
        err?.response?.data?.message ||
          "Unable to load tracking updates for this shipment."
      );
    } finally {
      setTrackingLoading(false);
    }
  };

  useEffect(() => {
    loadShipments();
  }, []);

  useEffect(() => {
    if (selectedShipmentId) {
      loadTracking(selectedShipmentId);
    }
  }, [selectedShipmentId]);

  return (
    <DashboardLayout title="Buyer Tracking">
      <div style={{ marginTop: "24px", display: "grid", gap: "24px" }}>
        {error && (
          <div
            style={{
              background: "#fff4f4",
              color: "#b42318",
              border: "1px solid #fecdca",
              borderRadius: "8px",
              padding: "12px 14px",
            }}
          >
            {error}
          </div>
        )}

        {loading ? (
          <p>Loading your shipments...</p>
        ) : shipments.length === 0 ? (
          <div
            style={{
              background: "#f8fafc",
              border: "1px solid #e2e8f0",
              borderRadius: "10px",
              padding: "18px",
            }}
          >
            You do not have any shipments yet.
          </div>
        ) : (
          <>
            <div
              style={{
                display: "grid",
                gridTemplateColumns: "minmax(0, 2fr) minmax(0, 3fr)",
                gap: "20px",
              }}
            >
              <div
                style={{
                  background: "#fff",
                  border: "1px solid #e2e8f0",
                  borderRadius: "12px",
                  padding: "20px",
                }}
              >
                <h3 style={{ marginTop: 0 }}>My shipments</h3>

                <div style={{ display: "grid", gap: "12px" }}>
                  {shipments.map((shipment) => (
                    <button
                      key={shipment.id}
                      type="button"
                      onClick={() => setSelectedShipmentId(shipment.id)}
                      style={{
                        textAlign: "left",
                        background:
                          selectedShipmentId === shipment.id ? "#eef2ff" : "#f8fafc",
                        border:
                          selectedShipmentId === shipment.id
                            ? "1px solid #c7d2fe"
                            : "1px solid #e2e8f0",
                        borderRadius: "10px",
                        padding: "14px",
                        cursor: "pointer",
                      }}
                    >
                      <div style={{ fontWeight: 700 }}>
                        {shipment.shipmentNumber || shipment.id}
                      </div>
                      <div style={{ color: "#475467", marginTop: "6px" }}>
                        {shipment.origin} → {shipment.destination}
                      </div>
                      <div style={{ color: "#475467", marginTop: "4px" }}>
                        {shipment.status}
                      </div>
                    </button>
                  ))}
                </div>
              </div>

              <div
                style={{
                  background: "#fff",
                  border: "1px solid #e2e8f0",
                  borderRadius: "12px",
                  padding: "20px",
                }}
              >
                {selectedShipment ? (
                  <>
                    <h3 style={{ marginTop: 0 }}>{selectedShipment.shipmentNumber || selectedShipment.id}</h3>

                    <div
                      style={{
                        display: "grid",
                        gridTemplateColumns: "repeat(2, minmax(0, 1fr))",
                        gap: "12px",
                        marginBottom: "20px",
                      }}
                    >
                      <div style={summaryCardStyle}>
                        <div style={labelStyle}>Status</div>
                        <div style={valueStyle}>{selectedShipment.status}</div>
                      </div>
                      <div style={summaryCardStyle}>
                        <div style={labelStyle}>Service</div>
                        <div style={valueStyle}>{selectedShipment.selectedService}</div>
                      </div>
                      <div style={summaryCardStyle}>
                        <div style={labelStyle}>Declared value</div>
                        <div style={valueStyle}>
                          {selectedShipment.declaredValue} {selectedShipment.currency || "USD"}
                        </div>
                      </div>
                      <div style={summaryCardStyle}>
                        <div style={labelStyle}>Tracking number</div>
                        <div style={valueStyle}>{selectedShipment.trackingNumber || "Pending"}</div>
                      </div>
                    </div>

                    <h4 style={{ marginBottom: "12px" }}>Tracking timeline</h4>

                    {trackingLoading ? (
                      <p>Loading tracking updates...</p>
                    ) : trackingEvents.length === 0 ? (
                      <div
                        style={{
                          background: "#f8fafc",
                          border: "1px solid #e2e8f0",
                          borderRadius: "8px",
                          padding: "14px",
                        }}
                      >
                        No tracking events have been recorded yet.
                      </div>
                    ) : (
                      <div style={{ display: "grid", gap: "12px" }}>
                        {trackingEvents.map((event) => (
                          <div
                            key={event.id}
                            style={{
                              borderLeft: "4px solid #7c3aed",
                              background: "#f8fafc",
                              borderRadius: "8px",
                              padding: "12px 14px",
                            }}
                          >
                            <div style={{ fontWeight: 700 }}>{event.status}</div>
                            <div style={{ color: "#475467", marginTop: "4px" }}>
                              {event.description}
                            </div>
                            <div style={{ color: "#475467", marginTop: "4px", fontSize: "12px" }}>
                              {event.locationText} • {new Date(event.occurredAt).toLocaleString()}
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </>
                ) : (
                  <p>Select a shipment to view its tracking history.</p>
                )}
              </div>
            </div>
          </>
        )}
      </div>
    </DashboardLayout>
  );
}

const summaryCardStyle = {
  background: "#f8fafc",
  border: "1px solid #e2e8f0",
  borderRadius: "10px",
  padding: "12px",
};

const labelStyle = {
  color: "#475467",
  fontSize: "12px",
  marginBottom: "4px",
};

const valueStyle = {
  fontWeight: 700,
};

export default BuyerDashboard;