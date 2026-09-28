import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { shipmentApi } from "../services/api";

function SellerShipments() {
  const [shipments, setShipments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    loadShipments();
  }, []);

  const loadShipments = async () => {
    try {
      setLoading(true);
      const response = await shipmentApi.getMyShipments();
      setShipments(response.data);
      setError("");
    } catch (err) {
      console.error("Failed to load shipments:", err);
      setError("Failed to load shipments. Please try again.");
    } finally {
      setLoading(false);
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

  if (loading) {
    return <div className="p-8 text-center">Loading shipments...</div>;
  }

  return (
    <div className="p-8">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold">My Shipments</h1>
        <Link
          to="/seller/shipments/create"
          className="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded"
        >
          Create New Shipment
        </Link>
      </div>

      {error && (
        <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      {shipments.length === 0 ? (
        <div className="text-center py-8 text-gray-500">
          No shipments found. Create your first shipment above.
        </div>
      ) : (
        <div className="grid gap-4">
          {shipments.map((shipment) => (
            <div
              key={shipment.id}
              className="border rounded-lg p-4 shadow-sm hover:shadow-md transition-shadow"
            >
              <div className="flex justify-between items-start">
                <div>
                  <h3 className="font-semibold text-lg">
                    Order: {shipment.orderId.substring(0, 8)}...
                  </h3>
                  <p className="text-sm text-gray-600 mt-1">
                    {shipment.originRegion}, {shipment.originCountryCode} →{" "}
                    {shipment.destinationRegion}, {shipment.destinationCountryCode}
                  </p>
                  <p className="text-sm text-gray-600 mt-1">
                    Value: {shipment.currency} {shipment.declaredValue.toFixed(2)}
                  </p>
                </div>
                <div className="text-right">
                  <span
                    className={`inline-block px-3 py-1 rounded-full text-xs font-semibold ${getStatusColor(
                      shipment.status
                    )}`}
                  >
                    {shipment.status}
                  </span>
                  <p className="text-xs text-gray-500 mt-2">
                    Created: {new Date(shipment.createdAt).toLocaleDateString()}
                  </p>
                </div>
              </div>
              <div className="mt-4 flex gap-2">
                <Link
                  to={`/seller/shipments/${shipment.id}`}
                  className="text-blue-500 hover:text-blue-700 text-sm"
                >
                  View Details →
                </Link>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export default SellerShipments;
