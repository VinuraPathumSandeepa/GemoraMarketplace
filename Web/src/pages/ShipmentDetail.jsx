import { useState, useEffect } from "react";
import { useParams, useNavigate, Link } from "react-router-dom";
import { shipmentApi } from "../services/api";

function ShipmentDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [shipment, setShipment] = useState(null);
  const [shippingPlan, setShippingPlan] = useState(null);
  const [insurance, setInsurance] = useState(null);
  const [trackingEvents, setTrackingEvents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [userRole, setUserRole] = useState("");

  useEffect(() => {
    const user = JSON.parse(localStorage.getItem("gemora_user") || "{}");
    setUserRole(user.role || "");
    loadShipmentData();
  }, [id]);

  const loadShipmentData = async () => {
    try {
      setLoading(true);
      const [shipmentRes, trackingRes] = await Promise.all([
        shipmentApi.getShipmentById(id),
        shipmentApi.getTrackingEvents(id).catch(() => ({ data: [] })),
      ]);

      setShipment(shipmentRes.data);
      setTrackingEvents(trackingRes.data);

      // Try to get shipping plan and insurance (may not exist yet)
      try {
        const planRes = await fetch(
          `http://localhost:5198/api/Shipments/${id}/plan`,
          {
            headers: {
              Authorization: `Bearer ${localStorage.getItem("gemora_token")}`,
            },
          }
        );
        if (planRes.ok) {
          setShippingPlan(await planRes.json());
        }
      } catch (err) {
        console.log("No shipping plan yet");
      }

      try {
        const insuranceRes = await shipmentApi.getInsurance(id);
        setInsurance(insuranceRes.data);
      } catch (err) {
        console.log("No insurance record yet");
      }

      setError("");
    } catch (err) {
      console.error("Failed to load shipment:", err);
      setError("Failed to load shipment details.");
    } finally {
      setLoading(false);
    }
  };

  const handleGeneratePlan = async () => {
    try {
      const response = await shipmentApi.generateShippingPlan(id);
      setShippingPlan(response.data);
      alert("Shipping plan generated successfully!");
    } catch (err) {
      alert(err.response?.data?.message || "Failed to generate shipping plan");
    }
  };

  const getStatusColor = (status) => {
    const colors = {
      Pending: "bg-yellow-100 text-yellow-800",
      PlanGenerated: "bg-blue-100 text-blue-800",
      PlanApproved: "bg-green-100 text-green-800",
      InTransit: "bg-purple-100 text-purple-800",
      Delivered: "bg-gray-100 text-gray-800",
      Cancelled: "bg-red-100 text-red-800",
      Exception: "bg-orange-100 text-orange-800",
    };
    return colors[status] || "bg-gray-100 text-gray-800";
  };

  const getRiskColor = (risk) => {
    const colors = {
      Low: "bg-green-100 text-green-800",
      Medium: "bg-yellow-100 text-yellow-800",
      High: "bg-orange-100 text-orange-800",
      Critical: "bg-red-100 text-red-800",
    };
    return colors[risk] || "bg-gray-100 text-gray-800";
  };

  if (loading) {
    return <div className="p-8 text-center">Loading shipment details...</div>;
  }

  if (!shipment) {
    return (
      <div className="p-8 text-center">
        <p className="text-red-500">{error || "Shipment not found"}</p>
        <button
          onClick={() => navigate("/seller/shipments")}
          className="mt-4 text-blue-500 hover:text-blue-700"
        >
          ← Back to Shipments
        </button>
      </div>
    );
  }

  return (
    <div className="p-8 max-w-6xl mx-auto">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold">Shipment Details</h1>
        <button
          onClick={() => navigate(-1)}
          className="text-blue-500 hover:text-blue-700"
        >
          ← Back
        </button>
      </div>

      {error && (
        <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      {/* Shipment Overview */}
      <div className="border rounded-lg p-6 mb-6 shadow-sm">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <h2 className="font-semibold text-lg mb-2">Shipment Information</h2>
            <p className="text-sm">
              <span className="font-medium">ID:</span>{" "}
              {shipment.id.substring(0, 8)}...
            </p>
            <p className="text-sm">
              <span className="font-medium">Order ID:</span>{" "}
              {shipment.orderId.substring(0, 8)}...
            </p>
            <p className="text-sm">
              <span className="font-medium">Status:</span>{" "}
              <span
                className={`inline-block px-2 py-1 rounded text-xs ${getStatusColor(
                  shipment.status
                )}`}
              >
                {shipment.status}
              </span>
            </p>
            {shipment.riskLevel && (
              <p className="text-sm mt-1">
                <span className="font-medium">Risk Level:</span>{" "}
                <span
                  className={`inline-block px-2 py-1 rounded text-xs ${getRiskColor(
                    shipment.riskLevel
                  )}`}
                >
                  {shipment.riskLevel}
                </span>
              </p>
            )}
          </div>
          <div>
            <h2 className="font-semibold text-lg mb-2">Route</h2>
            <p className="text-sm">
              <span className="font-medium">From:</span>{" "}
              {shipment.originAddress}, {shipment.originRegion},{" "}
              {shipment.originCountryCode}
            </p>
            <p className="text-sm">
              <span className="font-medium">To:</span>{" "}
              {shipment.destinationAddress}, {shipment.destinationRegion},{" "}
              {shipment.destinationCountryCode}
            </p>
          </div>
        </div>
      </div>

      {/* Package Details */}
      <div className="border rounded-lg p-6 mb-6 shadow-sm">
        <h2 className="font-semibold text-lg mb-4">Package Details</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <p className="text-sm">
              <span className="font-medium">Declared Value:</span>{" "}
              {shipment.currency} {shipment.declaredValue.toFixed(2)}
            </p>
            <p className="text-sm">
              <span className="font-medium">Description:</span>{" "}
              {shipment.packageDescription}
            </p>
          </div>
          <div>
            {shipment.packageWeight && (
              <p className="text-sm">
                <span className="font-medium">Weight:</span>{" "}
                {shipment.packageWeight} kg
              </p>
            )}
            {shipment.packageDimensions && (
              <p className="text-sm">
                <span className="font-medium">Dimensions:</span>{" "}
                {shipment.packageDimensions}
              </p>
            )}
            <p className="text-sm">
              <span className="font-medium">Service:</span>{" "}
              {shipment.preferredService}
            </p>
            {shipment.exportRequired && (
              <p className="text-sm text-orange-600">
                ⚠ Export documentation required
              </p>
            )}
          </div>
        </div>
      </div>

      {/* AI Shipping Plan */}
      {shippingPlan ? (
        <div className="border rounded-lg p-6 mb-6 shadow-sm bg-blue-50">
          <h2 className="font-semibold text-lg mb-4">AI Shipping Plan</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <p className="text-sm">
                <span className="font-medium">Risk Level:</span>{" "}
                <span
                  className={`inline-block px-2 py-1 rounded text-xs ${getRiskColor(
                    shippingPlan.riskLevel
                  )}`}
                >
                  {shippingPlan.riskLevel}
                </span>
              </p>
              {shippingPlan.riskReasons && (
                <p className="text-sm mt-2">
                  <span className="font-medium">Risk Reasons:</span>{" "}
                  {shippingPlan.riskReasons}
                </p>
              )}
              <p className="text-sm">
                <span className="font-medium">Recommended Service:</span>{" "}
                {shippingPlan.recommendedServiceType}
              </p>
            </div>
            <div>
              <p className="text-sm">
                <span className="font-medium">Insurance Recommended:</span>{" "}
                {shippingPlan.insuranceRecommended ? "Yes ✓" : "No"}
              </p>
              {shippingPlan.recommendedCoverageAmount && (
                <p className="text-sm">
                  <span className="font-medium">Recommended Coverage:</span>{" "}
                  {shipment.currency}{" "}
                  {shippingPlan.recommendedCoverageAmount.toFixed(2)}
                </p>
              )}
              {shippingPlan.handlingRequirements && (
                <p className="text-sm mt-2">
                  <span className="font-medium">Handling:</span>{" "}
                  {shippingPlan.handlingRequirements}
                </p>
              )}
              {shippingPlan.warnings && (
                <p className="text-sm text-orange-600 mt-2">
                  ⚠ {shippingPlan.warnings}
                </p>
              )}
            </div>
          </div>
          {shippingPlan.isApproved && (
            <div className="mt-4 p-3 bg-green-100 rounded">
              <p className="text-sm text-green-800">
                ✓ Plan approved by admin
                {shippingPlan.approvedAt &&
                  ` on ${new Date(shippingPlan.approvedAt).toLocaleDateString()}`}
              </p>
            </div>
          )}
        </div>
      ) : (
        userRole === "Seller" && (
          <div className="border rounded-lg p-6 mb-6 shadow-sm">
            <h2 className="font-semibold text-lg mb-4">AI Shipping Plan</h2>
            <p className="text-sm text-gray-600 mb-4">
              Generate an AI-powered shipping plan to assess risk and get
              recommendations.
            </p>
            <button
              onClick={handleGeneratePlan}
              className="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded"
            >
              Generate Shipping Plan
            </button>
          </div>
        )
      )}

      {/* Insurance Record */}
      {insurance && (
        <div className="border rounded-lg p-6 mb-6 shadow-sm bg-green-50">
          <h2 className="font-semibold text-lg mb-4">Insurance Record</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <p className="text-sm">
                <span className="font-medium">Coverage Amount:</span>{" "}
                {insurance.currency} {insurance.coverageAmount.toFixed(2)}
              </p>
              <p className="text-sm">
                <span className="font-medium">Coverage Type:</span>{" "}
                {insurance.coverageType}
              </p>
            </div>
            <div>
              <p className="text-sm">
                <span className="font-medium">Status:</span>{" "}
                <span className="text-green-600">{insurance.status}</span>
              </p>
              {insurance.policyNumber && (
                <p className="text-sm">
                  <span className="font-medium">Policy Number:</span>{" "}
                  {insurance.policyNumber}
                </p>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Tracking Timeline */}
      <div className="border rounded-lg p-6 mb-6 shadow-sm">
        <h2 className="font-semibold text-lg mb-4">Tracking Timeline</h2>
        {trackingEvents.length === 0 ? (
          <p className="text-sm text-gray-600">No tracking events yet.</p>
        ) : (
          <div className="space-y-3">
            {trackingEvents.map((event, index) => (
              <div key={event.id} className="flex items-start gap-3">
                <div className="flex-shrink-0 w-8 h-8 bg-blue-500 rounded-full flex items-center justify-center text-white text-sm">
                  {trackingEvents.length - index}
                </div>
                <div className="flex-1">
                  <p className="font-medium">{event.eventType}</p>
                  <p className="text-sm text-gray-600">{event.description}</p>
                  <p className="text-xs text-gray-500">
                    {event.location} •{" "}
                    {new Date(event.eventTimestamp).toLocaleString()}
                  </p>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

export default ShipmentDetail;
