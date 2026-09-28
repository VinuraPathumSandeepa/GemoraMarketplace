import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { shipmentApi } from "../services/api";

function AdminShipments() {
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
      <h1 className="text-2xl font-bold mb-6">Admin - All Shipments</h1>
      {shipments.length === 0 ? (
        <p className="text-gray-500">No shipments found.</p>
      ) : (
        <table className="w-full border-collapse">
          <thead>
            <tr className="bg-gray-100">
              <th className="border p-2 text-left">Shipment ID</th>
              <th className="border p-2 text-left">Order ID</th>
              <th className="border p-2 text-left">Status</th>
              <th className="border p-2 text-left">Risk</th>
              <th className="border p-2 text-left">Actions</th>
            </tr>
          </thead>
          <tbody>
            {shipments.map((s) => (
              <tr key={s.id}>
                <td className="border p-2">{s.id.substring(0, 8)}...</td>
                <td className="border p-2">{s.orderId.substring(0, 8)}...</td>
                <td className="border p-2">
                  <span className="px-2 py-1 rounded text-xs bg-blue-100 text-blue-800">
                    {s.status}
                  </span>
                </td>
                <td className="border p-2">{s.riskLevel || "N/A"}</td>
                <td className="border p-2">
                  <Link
                    to={`/admin/shipments/${s.id}`}
                    className="text-blue-500 text-sm"
                  >
                    Manage →
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

export default AdminShipments;
