import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import DashboardLayout from "../layouts/DashboardLayout";
import { shipmentApi } from "../services/api";
import { useAuth } from "../context/AuthContext";
import ShipmentHeader from "../components/ShipmentHeader";
import "../styles/ShipmentDetail.css";

function ShipmentDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [shipment, setShipment] = useState(null);
  const [plan, setPlan] = useState(null);
  const [tracking, setTracking] = useState([]);
  const [insurance, setInsurance] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [approving, setApproving] = useState(false);

  useEffect(() => {
    loadShipmentDetails();
  }, [id]);

  const loadShipmentDetails = async () => {
    try {
      setLoading(true);
      setError(null);

      const [shipmentRes, planRes, trackingRes, insuranceRes] = await Promise.all([
        shipmentApi.getShipmentById(id).catch(() => null),
        shipmentApi.getShippingPlan(id).catch(() => null),
        shipmentApi.getTrackingEvents(id).catch(() => ({ data: [] })),
        shipmentApi.getInsurance(id).catch(() => ({ data: [] })),
      ]);

      if (!shipmentRes) {
        setError("Shipment not found or access denied");
        return;
      }

      setShipment(shipmentRes.data);
      setPlan(planRes?.data || null);
      setTracking(trackingRes.data || []);
      setInsurance(Array.isArray(insuranceRes.data)
        ? insuranceRes.data
        : insuranceRes.data ? [insuranceRes.data] : []);
    } catch (err) {
      console.error("Failed to load shipment details:", err);
      setError(err.response?.data?.error || "Failed to load shipment details");
    } finally {
      setLoading(false);
    }
  };

  const handleApprovePlan = async () => {
    if (!window.confirm("Are you sure you want to approve this shipping plan? This action cannot be undone.")) {
      return;
    }

    try {
      setApproving(true);
      await shipmentApi.approveShippingPlan(id);
      alert("Shipping plan approved successfully!");
      await loadShipmentDetails(); // Reload to get updated plan status
    } catch (err) {
      console.error("Failed to approve plan:", err);
      alert(err.response?.data?.error || "Failed to approve shipping plan");
    } finally {
      setApproving(false);
    }
  };

  if (loading) {
    return (
      <DashboardLayout title="Shipment Details">
        <div className="loading-state">Loading shipment details...</div>
      </DashboardLayout>
    );
  }

  if (error || !shipment) {
    return (
      <DashboardLayout title="Shipment Details">
        <div className="error-state">
          <p>{error || "Shipment not found"}</p>
          <button onClick={() => navigate("/admin")}>Back to Dashboard</button>
        </div>
      </DashboardLayout>
    );
  }

  return (
    <DashboardLayout title={`Shipment ${shipment.shipmentNumber}`}>
      <div className="shipment-detail seller-shipping shipping-overview">
        <ShipmentHeader title="Shipping Overview" eyebrow="SECURE SHIPPING & INSURANCE"
          description="A complete view of your gemstone's shipping plan, tracking history, and insurance coverage."
          backTo={user?.role === "Seller" ? `/seller/shipments/${id}` : "/admin"}
          backLabel={user?.role === "Seller" ? "Shipment Details" : "Dashboard"} />

        {/* Shipment Information */}
        <section className="detail-section">
          <h2>Shipment Information</h2>
          <div className="info-grid">
            <div className="info-item">
              <label>Shipment Number</label>
              <p>{shipment.shipmentNumber}</p>
            </div>
            <div className="info-item">
              <label>Order ID</label>
              <p>{shipment.orderId}</p>
            </div>
            <div className="info-item">
              <label>Status</label>
              <p>
                <span className={`status-badge ${String(shipment.status).toLowerCase()}`}>
                  {shipment.status}
                </span>
              </p>
            </div>
            <div className="info-item">
              <label>Origin</label>
              <p>{shipment.origin}</p>
            </div>
            <div className="info-item">
              <label>Destination</label>
              <p>{shipment.destination}</p>
            </div>
            <div className="info-item">
              <label>Declared Value</label>
              <p>
                {shipment.currency} {shipment.declaredValue.toFixed(2)}
              </p>
            </div>
            <div className="info-item">
              <label>Service</label>
              <p>{shipment.selectedService}</p>
            </div>
            <div className="info-item">
              <label>Courier</label>
              <p>{shipment.courierName}</p>
            </div>
            <div className="info-item">
              <label>Tracking Number</label>
              <p>{shipment.trackingNumber || "Not assigned"}</p>
            </div>
            <div className="info-item full-width">
              <label>Package Description</label>
              <p>{shipment.packageDescription}</p>
            </div>
          </div>
        </section>

        {/* Shipping Plan */}
        {plan && (
          <section className="detail-section">
            <h2>Shipping Plan</h2>
            <div className="plan-card">
              <div className="plan-header">
                <div className="plan-status">
                  <label>Approval Status</label>
                  <p>
                    <span className={`status-badge ${plan.approvalStatus.toLowerCase().replace(/_/g, "")}`}>
                      {plan.approvalStatus}
                    </span>
                  </p>
                </div>
                {user?.role === "Admin" && plan.approvalStatus === "PendingAdminApproval" && (
                  <button
                    className="btn-approve"
                    onClick={handleApprovePlan}
                    disabled={approving}
                  >
                    {approving ? "Approving..." : "Approve Plan"}
                  </button>
                )}
              </div>

              <div className="plan-details">
                <div className="info-item">
                  <label>Risk Level</label>
                  <p>
                    <span className={`risk-badge ${plan.riskLevel.toLowerCase()}`}>
                      {plan.riskLevel}
                    </span>
                  </p>
                </div>
                <div className="info-item">
                  <label>Recommended Service</label>
                  <p>{plan.recommendedServiceType}</p>
                </div>
                <div className="info-item">
                  <label>Insurance Recommended</label>
                  <p>{plan.insuranceRecommended ? "Yes" : "No"}</p>
                </div>
                {plan.insuranceRecommended && (
                  <div className="info-item">
                    <label>Recommended Coverage</label>
                    <p>{shipment.currency} {plan.recommendedCoverage.toFixed(2)}</p>
                  </div>
                )}
              </div>

              {plan.riskReasons && plan.riskReasons.length > 0 && (
                <div className="plan-warnings">
                  <h3>Risk Reasons</h3>
                  <ul>
                    {plan.riskReasons.map((reason, idx) => (
                      <li key={idx}>{reason}</li>
                    ))}
                  </ul>
                </div>
              )}

              {plan.warnings && plan.warnings.length > 0 && (
                <div className="plan-warnings warning">
                  <h3>Warnings</h3>
                  <ul>
                    {plan.warnings.map((warning, idx) => (
                      <li key={idx}>{warning}</li>
                    ))}
                  </ul>
                </div>
              )}

              {plan.requirements && plan.requirements.length > 0 && (
                <div className="plan-warnings info">
                  <h3>Requirements</h3>
                  <ul>
                    {plan.requirements.map((req, idx) => (
                      <li key={idx}>{req}</li>
                    ))}
                  </ul>
                </div>
              )}

              {plan.approvedAt && (
                <div className="approval-info">
                  <p>Approved on: {new Date(plan.approvedAt).toLocaleString()}</p>
                  {plan.approvedByUserId && (
                    <p>Approved by: {plan.approvedByUserId}</p>
                  )}
                </div>
              )}

              {plan.rejectionReason && (
                <div className="rejection-info">
                  <p><strong>Rejection Reason:</strong> {plan.rejectionReason}</p>
                </div>
              )}
            </div>
          </section>
        )}

        {/* Tracking Timeline */}
        <section className="detail-section">
          <h2>Tracking History</h2>
          {tracking.length === 0 ? (
            <p className="empty-state">No tracking events available yet.</p>
          ) : (
            <div className="tracking-timeline">
              {tracking.map((event, idx) => (
                <div key={event.id || idx} className="timeline-event">
                  <div className="timeline-marker"></div>
                  <div className="timeline-content">
                    <div className="timeline-header">
                      <span className="timeline-status">{event.status}</span>
                      <span className="timeline-time">
                        {new Date(event.occurredAt).toLocaleString()}
                      </span>
                    </div>
                    <p className="timeline-description">{event.description}</p>
                    {event.locationText && (
                      <p className="timeline-location">📍 {event.locationText}</p>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>

        {/* Insurance Information */}
        {insurance.length > 0 && (
          <section className="detail-section">
            <h2>Insurance</h2>
            <div className="insurance-list">
              {insurance.map((record) => (
                <div key={record.id} className="insurance-card">
                  <h3>{record.provider}</h3>
                  <div className="info-grid">
                    <div className="info-item">
                      <label>Policy Reference</label>
                      <p>{record.policyReference}</p>
                    </div>
                    <div className="info-item">
                      <label>Coverage Amount</label>
                      <p>
                        {record.currency} {record.coverageAmount.toFixed(2)}
                      </p>
                    </div>
                    <div className="info-item">
                      <label>Coverage Type</label>
                      <p>{record.coverageType}</p>
                    </div>
                    <div className="info-item">
                      <label>Status</label>
                      <p>{record.status}</p>
                    </div>
                    <div className="info-item">
                      <label>Premium</label>
                      <p>
                        {record.currency} {record.premiumAmount.toFixed(2)}
                      </p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </section>
        )}
      </div>
    </DashboardLayout>
  );
}

export default ShipmentDetailPage;
