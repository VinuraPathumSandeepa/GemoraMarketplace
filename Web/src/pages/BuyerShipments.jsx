import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { shipmentApi } from "../services/api";

function BuyerShipments() {
  const [shipments, setShipments] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadShipments();
  }, []);

  const loadShipments = async () => {
    try {
      const response = await shipmentApi.getMyShipments();
      setShipments(response.data);
    } catch (err) {
      console.error("Failed to load shipments:", err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div className="p-8 text-center">Loading...</div>;

  return (
    <div className="p-8">
      <h1 className="text-2xl font-bold mb-6">My Shipments</h1>
      {shipments.length === 0 ? (
        <p className="text-gray-500">No shipments found.</p>
      ) : (
        <div className="grid gap-4">
          {shipments.map((s) => (
            <div key={s.id} className="border rounded p-4">
              <h3 className="font-semibold">Order: {s.orderId.substring(0, 8)}...</h3>
              <p className="text-sm text-gray-600">
                {s.originRegion} → {s.destinationRegion}
              </p>
              <span className="inline-block mt-2 px-3 py-1 rounded-full text-xs bg-blue-100 text-blue-800">
                {s.status}
              </span>
              <div className="mt-2">
                <Link to={`/buyer/shipments/${s.id}`} className="text-blue-500 text-sm">
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

export default BuyerShipments;
