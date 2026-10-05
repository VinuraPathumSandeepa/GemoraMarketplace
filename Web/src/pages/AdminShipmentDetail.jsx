import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { shipmentApi } from "../services/api";
import "../styles/AdminShipping.css";

function AdminShipmentDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  
  const [shipment, setShipment] = useState(null);
  const [plan, setPlan] = useState(null);
  const [insurance, setInsurance] = useState(null);
  const [trackingEvents, setTrackingEvents] = useState([]);
  const [auditEvents, setAuditEvents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [activeTab, setActiveTab] = useState("overview");
  
  // Action states
  const [approving, setApproving] = useState(false);
  const [rejecting, setRejecting] = useState(false);
  const [requestingRevision, setRequestingRevision] = useState(false);
  const [booking, setBooking] = useState(false);
  const [creatingInsurance, setCreatingInsurance] = useState(false);
  const [addingEvent, setAddingEvent] = useState(false);
  const [analyzing, setAnalyzing] = useState(false);
  const [analysisError, setAnalysisError] = useState(null);
  const [analysisCompletedAt, setAnalysisCompletedAt] = useState(null);
  
  // Review notes state
  const [reviewNotes, setReviewNotes] = useState("");
  const [rejectionReason, setRejectionReason] = useState("");
  const [revisionNotes, setRevisionNotes] = useState("");
  
  // Operational status update states
  const [updatingStatus, setUpdatingStatus] = useState(false);
  const [operationLocation, setOperationLocation] = useState("");
  const [operationDescription, setOperationDescription] = useState("");
  
  let agentEvidence = null;
  try {
    agentEvidence = plan?.generationSource === "AI" && plan.executionSummary
      ? JSON.parse(plan.executionSummary) : null;
  } catch { /* Older execution summaries are plain text. */ }
  
  // Form states
  const [eventType, setEventType] = useState("");
  const [eventLocation, setEventLocation] = useState("");
  const [eventDescription, setEventDescription] = useState("");
  const [externalEventCode, setExternalEventCode] = useState("");

  useEffect(() => {
    setAnalysisError(null);
    setAnalysisCompletedAt(null);
    loadDetails();
  }, [id]);

  const loadDetails = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const [shipmentRes, planRes, insuranceRes, trackingRes, auditRes] = await Promise.all([
        shipmentApi.getShipmentById(id).catch(() => null),
        shipmentApi.getShippingPlan(id).catch(() => null),
        shipmentApi.getInsurance(id).catch(() => null),
        shipmentApi.getTrackingEvents(id).catch(() => null),
        shipmentApi.getShipmentAuditHistory(id).catch(() => null),
      ]);
      
      setShipment(shipmentRes?.data || null);
      setPlan(planRes?.data || null);
      setInsurance(insuranceRes?.data || null);
      setTrackingEvents(trackingRes?.data || []);
      setAuditEvents(auditRes?.data || []);
    } catch (err) {
      console.error("Failed to load details:", err);
      setError(err.response?.data?.message || "Failed to load shipment details");
    } finally {
      setLoading(false);
    }
  };

  const handleAnalyzeRisk = async () => {
    if (analyzing) return;
    const previewOnly = !!shipment?.trackingNumber || ["Booked", "PickedUp", "InTransit", "OutForDelivery", "Delivered", "Cancelled"].includes(shipment?.status);
    if (!previewOnly && plan?.isApproved && !window.confirm("Analyzing again replaces this plan and clears its approval. Continue?")) return;
    setAnalyzing(true);
    setAnalysisError(null);
    setAnalysisCompletedAt(null);
    try {
      const response = await shipmentApi.generateShippingPlan(id);
      if (!response.data || response.data.shipmentId !== id || !response.data.riskLevel) {
        throw new Error("The backend returned an unexpected shipping plan.");
      }
      setPlan(response.data);
      if (!response.data.isPreview) setShipment(current => ({
        ...current,
        riskLevel: response.data.riskLevel,
        status: current.status === "Pending" ? "PlanGenerated" : current.status,
      }));
      setAnalysisCompletedAt(new Date().toISOString());
    } catch (err) {
      setAnalysisError(err.response?.data?.message || err.message || "Risk analysis failed. Please try again.");
    } finally {
      setAnalyzing(false);
    }
  };

  const handleApprovePlan = async () => {
    if (!confirm("Are you sure you want to approve this shipping plan?")) return;
    
    try {
      setApproving(true);
      await shipmentApi.approveShippingPlan(id, reviewNotes || undefined);
      await loadDetails(); // Reload to show updated approval state
      setReviewNotes(""); // Clear notes after approval
      alert("Shipping plan approved successfully!");
    } catch (err) {
      console.error("Failed to approve plan:", err);
      alert(err.response?.data?.message || "Failed to approve plan");
    } finally {
      setApproving(false);
    }
  };

  const handleRejectPlan = async () => {
    if (!confirm("Are you sure you want to reject this shipping plan? This will reset the shipment status to Pending.")) return;
    
    try {
      setRejecting(true);
      await shipmentApi.rejectShippingPlan(id, rejectionReason || undefined);
      await loadDetails(); // Reload to show updated rejection state
      setRejectionReason(""); // Clear reason after rejection
      alert("Shipping plan rejected. Shipment status reset to Pending.");
    } catch (err) {
      console.error("Failed to reject plan:", err);
      alert(err.response?.data?.message || "Failed to reject plan");
    } finally {
      setRejecting(false);
    }
  };

  const handleRequestRevision = async () => {
    if (!confirm("Are you sure you want to request a revision? This will set the shipment status to Planning so the seller can regenerate the plan.")) return;
    
    try {
      setRequestingRevision(true);
      await shipmentApi.requestRevisionShippingPlan(id, revisionNotes || undefined);
      await loadDetails(); // Reload to show updated revision state
      setRevisionNotes(""); // Clear notes after revision request
      alert("Revision requested. Shipment status set to Planning.");
    } catch (err) {
      console.error("Failed to request revision:", err);
      alert(err.response?.data?.message || "Failed to request revision");
    } finally {
      setRequestingRevision(false);
    }
  };

  const handleUpdateOperationalStatus = async (newStatus) => {
    if (!operationLocation && !operationDescription) {
      alert("Please provide location and/or description for this operational update.");
      return;
    }
    
    try {
      setUpdatingStatus(true);
      await shipmentApi.updateShipmentStatus(id, {
        status: newStatus,
        location: operationLocation || undefined,
        notes: operationDescription || undefined
      });
      await loadDetails(); // Reload to show updated status
      setOperationLocation("");
      setOperationDescription("");
      alert(`Shipment status updated to ${newStatus}`);
    } catch (err) {
      console.error("Failed to update shipment status:", err);
      alert(err.response?.data?.message || "Failed to update shipment status");
    } finally {
      setUpdatingStatus(false);
    }
  };

  const handleBookShipment = async () => {
    if (!confirm("Are you sure you want to book this shipment with the courier?")) return;
    
    try {
      setBooking(true);
      const response = await shipmentApi.bookShipment(id);
      await loadDetails(); // Reload to show updated booking info
      alert(`Shipment booked successfully! Tracking: ${response.data.trackingNumber}`);
    } catch (err) {
      console.error("Failed to book shipment:", err);
      alert(err.response?.data?.message || "Failed to book shipment");
    } finally {
      setBooking(false);
    }
  };

  const handleCreateInsurance = async () => {
    if (!shipment) return;
    
    try {
      setCreatingInsurance(true);
      const insuranceData = {
        shipmentId: id,
        declaredValue: shipment.declaredValue,
        coverageAmount: shipment.declaredValue,
        currency: shipment.currency,
        coverageType: "Standard",
        providerName: "DEMO Gemora Insurance Sandbox"
      };
      
      await shipmentApi.createInsurance(id, insuranceData);
      await loadDetails();
      alert("Insurance record created successfully!");
    } catch (err) {
      console.error("Failed to create insurance:", err);
      alert(err.response?.data?.message || "Failed to create insurance");
    } finally {
      setCreatingInsurance(false);
    }
  };

  const handleAddTrackingEvent = async (e) => {
    e.preventDefault();
    
    if (!eventType || !eventLocation || !eventDescription) {
      alert("Please fill in all required fields");
      return;
    }
    
    try {
      setAddingEvent(true);
      await shipmentApi.addTrackingEvent(id, {
        eventType,
        location: eventLocation,
        description: eventDescription,
        externalEventCode: externalEventCode || undefined
      });
      
      // Clear form
      setEventType("");
      setEventLocation("");
      setEventDescription("");
      setExternalEventCode("");
      
      await loadDetails();
      alert("Tracking event added successfully!");
    } catch (err) {
      console.error("Failed to add tracking event:", err);
      alert(err.response?.data?.message || "Failed to add tracking event");
    } finally {
      setAddingEvent(false);
    }
  };

  if (loading) {
    return (
      <div className="admin-shipping admin-shipping-state">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading shipment details...</p>
        </div>
      </div>
    );
  }

  if (error || !shipment) {
    return (
      <div className="admin-shipping admin-shipping-state">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <h3 className="text-red-800 font-semibold">Error</h3>
          <p className="text-red-700 mt-1">{error || "Shipment not found"}</p>
          <button 
            onClick={() => navigate("/admin/shipments")}
            className="mt-3 text-sm text-red-600 hover:text-red-800 underline"
          >
            ← Back to Shipments
          </button>
        </div>
      </div>
    );
  }

  const canBook = plan?.isApproved && shipment.status === "ReadyForBooking" && !shipment.trackingNumber;
  const isBooked = !!shipment.trackingNumber;

  return (
    <div className="admin-shipping">
      {/* Header */}
      <div className="admin-shipping-header">
        <div>
          <button 
            onClick={() => navigate("/admin/shipments")}
            className="text-sm text-blue-600 hover:text-blue-800 mb-2"
          >
            ← Back to Shipments
          </button>
          <p className="admin-shipping-eyebrow">Shipping operations</p>
          <h1>Shipment Details</h1>
          <p className="text-gray-600 text-sm mt-1">ID: {shipment.id}</p>
        </div>
        <div className="flex gap-2">
          <span className={`px-3 py-1 rounded-full text-sm font-semibold ${
            shipment.status === 'Delivered' ? 'bg-green-100 text-green-800' :
            shipment.status === 'InTransit' ? 'bg-blue-100 text-blue-800' :
            shipment.status === 'Exception' ? 'bg-red-100 text-red-800' :
            shipment.status === 'Cancelled' ? 'bg-gray-100 text-gray-800' :
            'bg-yellow-100 text-yellow-800'
          }`}>
            {shipment.status}
          </span>
        </div>
      </div>

      {/* Tabs */}
      <div className="admin-shipping-tabs">
        <nav aria-label="Shipment sections">
          {[
            { id: "overview", label: "Overview" },
            { id: "plan", label: "Shipping Plan" },
            { id: "booking", label: "Courier Booking" },
            { id: "operations", label: "Operations" },
            { id: "insurance", label: "Insurance" },
            { id: "tracking", label: "Tracking Timeline" },
            { id: "audit", label: "Audit History" },
            { id: "exceptions", label: "Exceptions" },
          ].map(tab => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={activeTab === tab.id ? "active" : ""}
              aria-current={activeTab === tab.id ? "page" : undefined}
            >
              {tab.label}
            </button>
          ))}
        </nav>
      </div>

      {/* Tab Content */}
      <div className="admin-shipping-panel">
        {/* OVERVIEW TAB */}
        {activeTab === "overview" && (
          <div>
            <h2 className="text-xl font-semibold mb-4">Shipment Overview</h2>
            <div className="grid grid-cols-2 gap-6">
              <div>
                <label className="text-sm font-medium text-gray-500">Order ID</label>
                <p className="text-gray-900">{shipment.orderId}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Seller ID</label>
                <p className="text-gray-900">{shipment.sellerId}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Buyer ID</label>
                <p className="text-gray-900">{shipment.buyerId}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Declared Value</label>
                <p className="text-gray-900">{shipment.currency} {shipment.declaredValue.toLocaleString()}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Origin</label>
                <p className="text-gray-900">{shipment.originAddress}, {shipment.originRegion} ({shipment.originCountryCode})</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Destination</label>
                <p className="text-gray-900">{shipment.destinationAddress}, {shipment.destinationRegion} ({shipment.destinationCountryCode})</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Package Description</label>
                <p className="text-gray-900">{shipment.packageDescription}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Preferred Service</label>
                <p className="text-gray-900">{shipment.preferredService}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Risk Level</label>
                <p className="text-gray-900">{shipment.riskLevel || "Not assessed"}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Export Required</label>
                <p className="text-gray-900">{shipment.exportRequired ? "Yes" : "No"}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Created At</label>
                <p className="text-gray-900">{new Date(shipment.createdAt).toLocaleString()}</p>
              </div>
              <div>
                <label className="text-sm font-medium text-gray-500">Last Updated</label>
                <p className="text-gray-900">{shipment.updatedAt ? new Date(shipment.updatedAt).toLocaleString() : "N/A"}</p>
              </div>
            </div>
          </div>
        )}

        {/* PLAN TAB */}
        {activeTab === "plan" && (
          <div>
            <div className="admin-analysis-heading">
              <div>
                <h2>Shipping Plan</h2>
                <p>Run the backend risk planner and review its latest recommendations.</p>
              </div>
              <button onClick={handleAnalyzeRisk} disabled={analyzing || approving} className="bg-blue-600">
                {analyzing ? "Analyzing risk..." : "Analyze Risk"}
              </button>
            </div>
            {analyzing && <p className="admin-analysis-notice" role="status">Waiting for the backend analysis. The previous plan remains visible until a new result arrives.</p>}
            {analysisError && <p className="admin-analysis-notice bg-red-50" role="alert">{analysisError}</p>}
            {analysisCompletedAt && (
              <div className="admin-analysis-notice" role="status">
                <strong>{plan?.generationSource === "FallbackRules" ? "AI analysis failed — rules fallback result received" : "Fresh backend result received"}</strong>
                {plan?.isPreview && <p>Analysis preview only. The saved shipping plan, approval, and shipment status have not changed.</p>}
                <p>Received: {new Date(analysisCompletedAt).toLocaleString()}</p>
                <p>Source: {plan?.generationSource === "MockProvider" ? "Mock provider (simulation; no real LLM call)" : plan?.generationSource === "FallbackRules" ? "Deterministic rules fallback" : plan?.generationSource || "Not reported by backend; model execution unverified"}</p>
                <p>Generated: {plan?.updatedAt || plan?.createdAt ? new Date(plan.updatedAt || plan.createdAt).toLocaleString() : "Not reported"}</p>
                {agentEvidence?.provider === "Gemini" ? (
                  <>
                    <p>Provider: {agentEvidence.provider} · Model: {agentEvidence.model}</p>
                    <p>Model requests: {agentEvidence.modelCalls} · Validation: {agentEvidence.validation}</p>
                    <p>Tools executed: {agentEvidence.tools?.join(" → ")}</p>
                    <p>Run ID: {agentEvidence.runId}</p>
                  </>
                ) : plan?.executionSummary && <p>{plan.executionSummary}</p>}
              </div>
            )}
            
            {!plan ? (
              <div className="text-center py-8">
                <p className="text-gray-500">No shipping plan has been generated yet.</p>
              </div>
            ) : (
              <div>
                <div className="grid grid-cols-2 gap-6 mb-6">
                  <div>
                    <label className="text-sm font-medium text-gray-500">Risk Level</label>
                    {analysisCompletedAt ? <p className={`text-lg font-semibold ${
                      plan.riskLevel === 'Critical' ? 'text-red-600' :
                      plan.riskLevel === 'High' ? 'text-orange-600' :
                      plan.riskLevel === 'Medium' ? 'text-yellow-600' :
                      'text-green-600'
                    }`}>
                      {plan.riskLevel}
                    </p> : <p className="text-gray-500">Click Analyze Risk to see the risk level.</p>}
                  </div>
                  <div>
                    <label className="text-sm font-medium text-gray-500">Recommended Service</label>
                    <p className="text-gray-900">{plan.recommendedServiceType}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-gray-500">Insurance Recommended</label>
                    <p className="text-gray-900">{plan.insuranceRecommended ? "Yes" : "No"}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-gray-500">Recommended Coverage</label>
                    <p className="text-gray-900">
                      {plan.recommendedCoverageAmount ? `${shipment.currency} ${plan.recommendedCoverageAmount.toLocaleString()}` : "N/A"}
                    </p>
                  </div>
                </div>

                {(
                  <div className="mb-4">
                    <label className="text-sm font-medium text-gray-500">Risk Reasons</label>
                    <p className="text-gray-900 mt-1">{analysisCompletedAt ? plan.riskReasons || "No risk reasons returned." : "Click Analyze Risk to see the risk reasons."}</p>
                  </div>
                )}

                {plan.handlingRequirements && (
                  <div className="mb-4">
                    <label className="text-sm font-medium text-gray-500">Handling Requirements</label>
                    <p className="text-gray-900 mt-1">{plan.handlingRequirements}</p>
                  </div>
                )}

                {plan.requiredDocuments && (
                  <div className="mb-4">
                    <label className="text-sm font-medium text-gray-500">Required Documents</label>
                    <p className="text-gray-900 mt-1">{plan.requiredDocuments}</p>
                  </div>
                )}

                {plan.warnings && (
                  <div className="mb-4 p-3 bg-yellow-50 border border-yellow-200 rounded">
                    <label className="text-sm font-medium text-yellow-800">Warnings</label>
                    <p className="text-yellow-700 mt-1">{plan.warnings}</p>
                  </div>
                )}

                {/* Approval Status */}
                <div className="mt-6 pt-6 border-t">
                  <div className="flex items-center justify-between mb-4">
                    <div>
                      <label className="text-sm font-medium text-gray-500">Approval Status</label>
                      <p className={`text-lg font-semibold ${plan.isApproved ? 'text-green-600' : 'text-orange-600'}`}>
                        {plan.isApproved ? "✓ Approved" : "Pending Review"}
                      </p>
                      {plan.isApproved && plan.approvedAt && (
                        <p className="text-sm text-gray-500">
                          Approved on {new Date(plan.approvedAt).toLocaleString()}
                        </p>
                      )}
                      {plan.adminNotes && (
                        <div className="mt-2 p-3 bg-blue-50 border border-blue-200 rounded">
                          <label className="text-xs font-medium text-blue-800">Admin Notes:</label>
                          <p className="text-sm text-blue-900 mt-1">{plan.adminNotes}</p>
                        </div>
                      )}
                    </div>
                  </div>
                  
                  {!plan.isApproved && !plan.isPreview && (
                    <div className="space-y-4">
                      {/* Review Notes Textarea */}
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">
                          Review Notes (optional)
                        </label>
                        <textarea
                          value={reviewNotes}
                          onChange={(e) => setReviewNotes(e.target.value)}
                          placeholder="Add any notes about your review decision..."
                          rows={3}
                          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        />
                      </div>

                      {/* Action Buttons */}
                      <div className="flex gap-3 flex-wrap">
                        <button
                          onClick={handleApprovePlan}
                          disabled={approving || analyzing}
                          className="px-6 py-2 bg-green-600 text-white rounded hover:bg-green-700 disabled:opacity-50"
                        >
                          {approving ? "Approving..." : "Approve Plan"}
                        </button>
                        
                        <button
                          onClick={handleRejectPlan}
                          disabled={rejecting || analyzing}
                          className="px-6 py-2 bg-red-600 text-white rounded hover:bg-red-700 disabled:opacity-50"
                        >
                          {rejecting ? "Rejecting..." : "Reject Plan"}
                        </button>
                        
                        <button
                          onClick={handleRequestRevision}
                          disabled={requestingRevision || analyzing}
                          className="px-6 py-2 bg-yellow-600 text-white rounded hover:bg-yellow-700 disabled:opacity-50"
                        >
                          {requestingRevision ? "Requesting..." : "Request Revision"}
                        </button>
                      </div>

                      {/* Rejection Reason Textarea (conditionally shown) */}
                      {rejecting && (
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-2">
                            Rejection Reason (optional)
                          </label>
                          <textarea
                            value={rejectionReason}
                            onChange={(e) => setRejectionReason(e.target.value)}
                            placeholder="Explain why this plan is being rejected..."
                            rows={3}
                            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-red-500"
                          />
                        </div>
                      )}

                      {/* Revision Notes Textarea (conditionally shown) */}
                      {requestingRevision && (
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-2">
                            Revision Notes (optional)
                          </label>
                          <textarea
                            value={revisionNotes}
                            onChange={(e) => setRevisionNotes(e.target.value)}
                            placeholder="Provide guidance for what should be revised..."
                            rows={3}
                            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-yellow-500"
                          />
                        </div>
                      )}

                      <p className="text-sm text-orange-600">
                        Note: If the plan is regenerated, any previous approval will be invalidated.
                      </p>
                    </div>
                  )}
                </div>
              </div>
            )}
          </div>
        )}

        {/* BOOKING TAB */}
        {activeTab === "booking" && (
          <div>
            <h2 className="text-xl font-semibold mb-4">Courier Booking</h2>
            
            {isBooked ? (
              <div className="bg-green-50 border border-green-200 rounded-lg p-6">
                <h3 className="text-green-800 font-semibold text-lg mb-4">✓ Shipment Booked</h3>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="text-sm font-medium text-gray-500">Courier</label>
                    <p className="text-gray-900 font-semibold">{shipment.courierName}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-gray-500">Tracking Number</label>
                    <p className="text-gray-900 font-mono">{shipment.trackingNumber}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-gray-500">External Reference</label>
                    <p className="text-gray-900 font-mono">{shipment.externalShipmentReference || "N/A"}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-gray-500">Service</label>
                    <p className="text-gray-900">{shipment.selectedService || "N/A"}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-gray-500">Booked At</label>
                    <p className="text-gray-900">
                      {shipment.bookedAt ? new Date(shipment.bookedAt).toLocaleString() : "N/A"}
                    </p>
                  </div>
                </div>
              </div>
            ) : (
              <div>
                {!canBook ? (
                  <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-6">
                    <h3 className="text-yellow-800 font-semibold mb-2">Booking Not Available</h3>
                    <ul className="text-sm text-yellow-700 space-y-1">
                      {!plan?.isApproved && <li>• Shipping plan must be approved first</li>}
                      {shipment.status !== "ReadyForBooking" && <li>• Shipment status must be "ReadyForBooking" (current: {shipment.status})</li>}
                      {isBooked && <li>• Shipment is already booked</li>}
                    </ul>
                  </div>
                ) : (
                  <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
                    <h3 className="text-blue-800 font-semibold mb-4">Ready to Book</h3>
                    <p className="text-blue-700 mb-4">
                      The shipping plan has been approved and the shipment is ready for courier booking.
                    </p>
                    <button
                      onClick={handleBookShipment}
                      disabled={booking}
                      className="px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 font-semibold"
                    >
                      {booking ? "Booking..." : "Book with Courier (SIMULATION)"}
                    </button>
                    <p className="text-xs text-blue-600 mt-3">
                      Note: This is a simulated booking using DEMO Gemora Courier Sandbox
                    </p>
                  </div>
                )}
              </div>
            )}
          </div>
        )}

        {/* OPERATIONS TAB */}
        {activeTab === "operations" && (
          <div>
            <h2 className="text-xl font-semibold mb-4">Operational Status Management</h2>
            
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-6 mb-6">
              <h3 className="text-blue-800 font-semibold mb-2">Current Status</h3>
              <p className="text-2xl font-bold text-blue-900">{shipment.status}</p>
              {shipment.trackingNumber && (
                <p className="text-sm text-blue-700 mt-2">Tracking: {shipment.trackingNumber}</p>
              )}
            </div>

            {/* Operational Actions based on current status */}
            <div className="space-y-4">
              {/* Booked → PickedUp / InTransit */}
              {shipment.status === "Booked" && (
                <div className="bg-white border border-gray-200 rounded-lg p-4">
                  <h4 className="font-semibold mb-3">Available Actions from Booked</h4>
                  <div className="grid grid-cols-2 gap-3">
                    <button
                      onClick={() => handleUpdateOperationalStatus("PickedUp")}
                      disabled={updatingStatus}
                      className="px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700 disabled:opacity-50"
                    >
                      Mark Picked Up
                    </button>
                    <button
                      onClick={() => handleUpdateOperationalStatus("InTransit")}
                      disabled={updatingStatus}
                      className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
                    >
                      Mark In Transit
                    </button>
                  </div>
                </div>
              )}

              {/* PickedUp → InTransit */}
              {shipment.status === "PickedUp" && (
                <div className="bg-white border border-gray-200 rounded-lg p-4">
                  <h4 className="font-semibold mb-3">Available Actions from Picked Up</h4>
                  <button
                    onClick={() => handleUpdateOperationalStatus("InTransit")}
                    disabled={updatingStatus}
                    className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
                  >
                    Mark In Transit
                  </button>
                </div>
              )}

              {/* InTransit → CustomsHold / OutForDelivery / Exception / DeliveryFailed */}
              {shipment.status === "InTransit" && (
                <div className="bg-white border border-gray-200 rounded-lg p-4">
                  <h4 className="font-semibold mb-3">Available Actions from In Transit</h4>
                  <div className="grid grid-cols-2 gap-3">
                    <button
                      onClick={() => handleUpdateOperationalStatus("CustomsHold")}
                      disabled={updatingStatus}
                      className="px-4 py-2 bg-yellow-600 text-white rounded hover:bg-yellow-700 disabled:opacity-50"
                    >
                      Customs Hold
                    </button>
                    <button
                      onClick={() => handleUpdateOperationalStatus("OutForDelivery")}
                      disabled={updatingStatus}
                      className="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700 disabled:opacity-50"
                    >
                      Out for Delivery
                    </button>
                    <button
                      onClick={() => handleUpdateOperationalStatus("Exception")}
                      disabled={updatingStatus}
                      className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 disabled:opacity-50"
                    >
                      Report Exception
                    </button>
                    <button
                      onClick={() => handleUpdateOperationalStatus("DeliveryFailed")}
                      disabled={updatingStatus}
                      className="px-4 py-2 bg-orange-600 text-white rounded hover:bg-orange-700 disabled:opacity-50"
                    >
                      Delivery Failed
                    </button>
                  </div>
                </div>
              )}

              {/* CustomsHold → InTransit / Exception */}
              {shipment.status === "CustomsHold" && (
                <div className="bg-white border border-gray-200 rounded-lg p-4">
                  <h4 className="font-semibold mb-3">Available Actions from Customs Hold</h4>
                  <div className="grid grid-cols-2 gap-3">
                    <button
                      onClick={() => handleUpdateOperationalStatus("InTransit")}
                      disabled={updatingStatus}
                      className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
                    >
                      Release to In Transit
                    </button>
                    <button
                      onClick={() => handleUpdateOperationalStatus("Exception")}
                      disabled={updatingStatus}
                      className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 disabled:opacity-50"
                    >
                      Report Exception
                    </button>
                  </div>
                </div>
              )}

              {/* OutForDelivery → Delivered / DeliveryFailed / Exception */}
              {shipment.status === "OutForDelivery" && (
                <div className="bg-white border border-gray-200 rounded-lg p-4">
                  <h4 className="font-semibold mb-3">Available Actions from Out for Delivery</h4>
                  <div className="grid grid-cols-2 gap-3">
                    <button
                      onClick={() => handleUpdateOperationalStatus("Delivered")}
                      disabled={updatingStatus}
                      className="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700 disabled:opacity-50"
                    >
                      Mark Delivered
                    </button>
                    <button
                      onClick={() => handleUpdateOperationalStatus("DeliveryFailed")}
                      disabled={updatingStatus}
                      className="px-4 py-2 bg-orange-600 text-white rounded hover:bg-orange-700 disabled:opacity-50"
                    >
                      Delivery Failed
                    </button>
                    <button
                      onClick={() => handleUpdateOperationalStatus("Exception")}
                      disabled={updatingStatus}
                      className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 disabled:opacity-50"
                    >
                      Report Exception
                    </button>
                  </div>
                </div>
              )}

              {/* Exception → InTransit / Cancelled */}
              {shipment.status === "Exception" && (
                <div className="bg-white border border-gray-200 rounded-lg p-4">
                  <h4 className="font-semibold mb-3">Available Actions from Exception</h4>
                  <div className="grid grid-cols-2 gap-3">
                    <button
                      onClick={() => handleUpdateOperationalStatus("InTransit")}
                      disabled={updatingStatus}
                      className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
                    >
                      Resolve & Return to Transit
                    </button>
                    <button
                      onClick={() => handleUpdateOperationalStatus("Cancelled")}
                      disabled={updatingStatus}
                      className="px-4 py-2 bg-gray-600 text-white rounded hover:bg-gray-700 disabled:opacity-50"
                    >
                      Cancel Shipment
                    </button>
                  </div>
                </div>
              )}

              {/* DeliveryFailed → Cancelled / Exception */}
              {shipment.status === "DeliveryFailed" && (
                <div className="bg-white border border-gray-200 rounded-lg p-4">
                  <h4 className="font-semibold mb-3">Available Actions from Delivery Failed</h4>
                  <div className="grid grid-cols-2 gap-3">
                    <button
                      onClick={() => handleUpdateOperationalStatus("Cancelled")}
                      disabled={updatingStatus}
                      className="px-4 py-2 bg-gray-600 text-white rounded hover:bg-gray-700 disabled:opacity-50"
                    >
                      Cancel Shipment
                    </button>
                    <button
                      onClick={() => handleUpdateOperationalStatus("Exception")}
                      disabled={updatingStatus}
                      className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 disabled:opacity-50"
                    >
                      Report Exception
                    </button>
                  </div>
                </div>
              )}

              {/* No operational actions available */}
              {!["Booked", "PickedUp", "InTransit", "CustomsHold", "OutForDelivery", "Exception", "DeliveryFailed"].includes(shipment.status) && (
                <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
                  <p className="text-gray-700">No operational actions available for current status: {shipment.status}</p>
                </div>
              )}
            </div>

            {/* Operation Details Form */}
            <div className="mt-6 pt-6 border-t">
              <h4 className="font-semibold mb-3">Operation Details</h4>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Location (optional)
                  </label>
                  <input
                    type="text"
                    value={operationLocation}
                    onChange={(e) => setOperationLocation(e.target.value)}
                    placeholder="e.g., Colombo Warehouse, Customs Office"
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Description/Reason (optional)
                  </label>
                  <textarea
                    value={operationDescription}
                    onChange={(e) => setOperationDescription(e.target.value)}
                    placeholder="Provide details about this operation..."
                    rows={3}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>
              <p className="text-xs text-gray-500 mt-3">
                Note: All operational changes create tracking events and are saved atomically.
              </p>
            </div>
          </div>
        )}

        {/* INSURANCE TAB */}
        {activeTab === "insurance" && (
          <div>
            <h2 className="text-xl font-semibold mb-4">Insurance</h2>
            
            {insurance ? (
              <div className="bg-green-50 border border-green-200 rounded-lg p-6">
                <div className="flex items-start justify-between mb-4">
                  <div>
                    <h3 className="text-green-800 font-semibold text-lg">SIMULATED INSURANCE</h3>
                    <p className="text-sm text-green-600">This is a demo/simulation - not a real insurance policy</p>
                  </div>
                  <span className="px-3 py-1 bg-green-100 text-green-800 rounded-full text-sm font-semibold">
                    {insurance.status}
                  </span>
                </div>
                
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="text-sm font-medium text-gray-500">Provider</label>
                    <p className="text-gray-900 font-semibold">{insurance.providerName}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-gray-500">Policy Reference</label>
                    <p className="text-gray-900 font-mono">{insurance.policyReference}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-gray-500">Declared Value</label>
                    <p className="text-gray-900">{insurance.currency} {insurance.declaredValue.toLocaleString()}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-gray-500">Coverage Amount</label>
                    <p className="text-gray-900 font-semibold">{insurance.currency} {insurance.coverageAmount.toLocaleString()}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-gray-500">Coverage Type</label>
                    <p className="text-gray-900">{insurance.coverageType}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-gray-500">Premium Amount</label>
                    <p className="text-gray-900">{insurance.currency} {insurance.premiumAmount.toLocaleString()}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-gray-500">Policy Start Date</label>
                    <p className="text-gray-900">
                      {insurance.policyStartDate ? new Date(insurance.policyStartDate).toLocaleDateString() : "N/A"}
                    </p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-gray-500">Created At</label>
                    <p className="text-gray-900">{new Date(insurance.createdAt).toLocaleString()}</p>
                  </div>
                </div>
              </div>
            ) : (
              <div className="text-center py-8">
                <p className="text-gray-500 mb-4">No insurance record exists for this shipment.</p>
                <button
                  onClick={handleCreateInsurance}
                  disabled={creatingInsurance}
                  className="px-6 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
                >
                  {creatingInsurance ? "Creating..." : "Create Simulated Insurance"}
                </button>
                <p className="text-xs text-gray-500 mt-3">
                  Creates a DEMO insurance record with simulated values
                </p>
              </div>
            )}
          </div>
        )}

        {/* TRACKING TAB */}
        {activeTab === "tracking" && (
          <div>
            <h2 className="text-xl font-semibold mb-4">Tracking Timeline</h2>
            
            {/* Add Tracking Event Form */}
            <div className="mb-6 p-4 bg-gray-50 rounded-lg border">
              <h3 className="font-semibold mb-3">Add Tracking Update</h3>
              <form onSubmit={handleAddTrackingEvent} className="space-y-3">
                <div className="grid grid-cols-2 gap-3">
                  <select
                    value={eventType}
                    onChange={(e) => setEventType(e.target.value)}
                    className="border border-gray-300 rounded px-3 py-2"
                    required
                  >
                    <option value="">Select Event Type</option>
                    <option value="PickedUp">Picked Up</option>
                    <option value="InTransit">In Transit</option>
                    <option value="CustomsHold">Customs Hold</option>
                    <option value="OutForDelivery">Out for Delivery</option>
                    <option value="Delivered">Delivered</option>
                    <option value="DeliveryFailed">Delivery Failed</option>
                    <option value="Exception">Exception</option>
                    <option value="Returned">Returned</option>
                  </select>
                  <input
                    type="text"
                    placeholder="Location (e.g., Colombo Hub)"
                    value={eventLocation}
                    onChange={(e) => setEventLocation(e.target.value)}
                    className="border border-gray-300 rounded px-3 py-2"
                    required
                  />
                </div>
                <textarea
                  placeholder="Description of event"
                  value={eventDescription}
                  onChange={(e) => setEventDescription(e.target.value)}
                  className="w-full border border-gray-300 rounded px-3 py-2"
                  rows="2"
                  required
                />
                <input
                  type="text"
                  placeholder="External Event Code (optional)"
                  value={externalEventCode}
                  onChange={(e) => setExternalEventCode(e.target.value)}
                  className="w-full border border-gray-300 rounded px-3 py-2"
                />
                <button
                  type="submit"
                  disabled={addingEvent}
                  className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
                >
                  {addingEvent ? "Adding..." : "Add Event"}
                </button>
              </form>
            </div>

            {/* Timeline */}
            {trackingEvents.length === 0 ? (
              <p className="text-gray-500 text-center py-8">No tracking events recorded yet.</p>
            ) : (
              <div className="space-y-4">
                {trackingEvents.map((event, index) => (
                  <div key={event.id} className="flex gap-4">
                    <div className="flex-shrink-0 w-12 text-right">
                      <div className="text-sm font-semibold text-gray-900">
                        {new Date(event.occurredAt).toLocaleDateString()}
                      </div>
                      <div className="text-xs text-gray-500">
                        {new Date(event.occurredAt).toLocaleTimeString()}
                      </div>
                    </div>
                    <div className="flex-shrink-0 w-8 flex justify-center">
                      <div className="w-3 h-3 rounded-full bg-blue-600 mt-1.5"></div>
                    </div>
                    <div className="flex-1 pb-4 border-b border-gray-200">
                      <div className="flex items-start justify-between">
                        <div>
                          <span className="inline-block px-2 py-1 text-xs font-semibold rounded bg-blue-100 text-blue-800 mb-1">
                            {event.eventType}
                          </span>
                          <p className="text-sm font-medium text-gray-900">{event.location}</p>
                          <p className="text-sm text-gray-600 mt-1">{event.description}</p>
                          {event.externalEventCode && (
                            <p className="text-xs text-gray-500 mt-1 font-mono">
                              External Code: {event.externalEventCode}
                            </p>
                          )}
                        </div>
                        <span className="text-xs text-gray-400">
                          Recorded: {new Date(event.recordedAt).toLocaleString()}
                        </span>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* AUDIT HISTORY TAB */}
        {activeTab === "audit" && (
          <div>
            <h2 className="text-xl font-semibold mb-4">Operational Audit History</h2>
            
            {auditEvents.length === 0 ? (
              <div className="bg-gray-50 border border-gray-200 rounded-lg p-6 text-center">
                <p className="text-gray-600">No audit history available for this shipment.</p>
              </div>
            ) : (
              <div className="space-y-4">
                {auditEvents.map((event, index) => (
                  <div key={event.id} className="bg-white border border-gray-200 rounded-lg p-4">
                    <div className="flex items-start justify-between mb-3">
                      <div className="flex-1">
                        <h3 className="font-semibold text-lg">{event.eventType}</h3>
                        <p className="text-sm text-gray-600">{event.description}</p>
                      </div>
                      <span className="text-xs text-gray-500 whitespace-nowrap ml-4">
                        {new Date(event.occurredAt).toLocaleString()}
                      </span>
                    </div>
                    
                    <div className="grid grid-cols-2 gap-4 text-sm">
                      {event.performedByUserId && (
                        <div>
                          <label className="text-xs font-medium text-gray-500">Performed By</label>
                          <p className="text-gray-900">
                            User: {event.performedByUserId.toString().substring(0, 8)}...
                            {event.performedByRole && ` (${event.performedByRole})`}
                          </p>
                        </div>
                      )}
                      
                      {(event.previousState || event.newState) && (
                        <div>
                          <label className="text-xs font-medium text-gray-500">State Transition</label>
                          <p className="text-gray-900">
                            {event.previousState || 'N/A'} → {event.newState || 'N/A'}
                          </p>
                        </div>
                      )}
                      
                      {event.location && event.location !== "System" && (
                        <div>
                          <label className="text-xs font-medium text-gray-500">Location</label>
                          <p className="text-gray-900">{event.location}</p>
                        </div>
                      )}
                      
                      {event.reason && (
                        <div className="col-span-2">
                          <label className="text-xs font-medium text-gray-500">Reason/Notes</label>
                          <p className="text-gray-900 italic">{event.reason}</p>
                        </div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* EXCEPTIONS TAB */}
        {activeTab === "exceptions" && (
          <div>
            <h2 className="text-xl font-semibold mb-4">Exceptions & Issues</h2>
            
            {shipment.status === "Exception" ? (
              <div className="bg-red-50 border border-red-200 rounded-lg p-6 mb-6">
                <h3 className="text-red-800 font-semibold text-lg mb-2">⚠ Shipment Exception</h3>
                <p className="text-red-700">This shipment currently has an exception status.</p>
                <p className="text-sm text-red-600 mt-2">
                  Check the tracking timeline for details about the exception.
                </p>
              </div>
            ) : (
              <div className="bg-green-50 border border-green-200 rounded-lg p-6 mb-6">
                <h3 className="text-green-800 font-semibold text-lg">✓ No Active Exceptions</h3>
                <p className="text-green-700">Shipment status: {shipment.status}</p>
              </div>
            )}

            {/* Exception-related tracking events */}
            <h3 className="font-semibold mb-3">Exception History</h3>
            {trackingEvents.filter(e => ['Exception', 'DeliveryFailed', 'CustomsHold', 'Cancelled'].includes(e.eventType)).length === 0 ? (
              <p className="text-gray-500 text-center py-4">No exceptions or delivery failures recorded.</p>
            ) : (
              <div className="space-y-3">
                {trackingEvents
                  .filter(e => ['Exception', 'DeliveryFailed', 'CustomsHold', 'Cancelled'].includes(e.eventType))
                  .map(event => (
                    <div key={event.id} className={`p-4 rounded-lg border ${
                      event.eventType === 'Exception' ? 'bg-red-50 border-red-200' :
                      event.eventType === 'DeliveryFailed' ? 'bg-orange-50 border-orange-200' :
                      event.eventType === 'CustomsHold' ? 'bg-yellow-50 border-yellow-200' :
                      'bg-gray-50 border-gray-200'
                    }`}>
                      <div className="flex items-start justify-between">
                        <div>
                          <span className={`inline-block px-2 py-1 text-xs font-semibold rounded mb-2 ${
                            event.eventType === 'Exception' ? 'bg-red-100 text-red-800' :
                            event.eventType === 'DeliveryFailed' ? 'bg-orange-100 text-orange-800' :
                            event.eventType === 'CustomsHold' ? 'bg-yellow-100 text-yellow-800' :
                            'bg-gray-100 text-gray-800'
                          }`}>
                            {event.eventType}
                          </span>
                          <p className="text-sm font-medium text-gray-900">{event.location}</p>
                          <p className="text-sm text-gray-600 mt-1">{event.description}</p>
                        </div>
                        <span className="text-xs text-gray-500">
                          {new Date(event.occurredAt).toLocaleString()}
                        </span>
                      </div>
                    </div>
                  ))}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

export default AdminShipmentDetail;
