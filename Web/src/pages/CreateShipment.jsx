import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { shipmentApi } from "../services/api";

function CreateShipment() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const [formData, setFormData] = useState({
    orderId: "",
    originAddress: "",
    originRegion: "",
    originCountryCode: "",
    destinationAddress: "",
    destinationRegion: "",
    destinationCountryCode: "",
    declaredValue: "",
    currency: "USD",
    packageDescription: "",
    packageWeight: "",
    packageDimensions: "",
    preferredService: "Standard",
    specialHandlingNotes: "",
    exportRequired: false,
  });

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      // Convert numeric fields
      const payload = {
        ...formData,
        orderId: formData.orderId.trim(),
        declaredValue: formData.declaredValue
          ? parseFloat(formData.declaredValue)
          : undefined,
        packageWeight: formData.packageWeight
          ? parseFloat(formData.packageWeight)
          : undefined,
      };

      const response = await shipmentApi.createShipment(payload);
      navigate(`/seller/shipments/${response.data.id}`);
    } catch (err) {
      console.error("Failed to create shipment:", err);
      setError(
        err.response?.data?.message ||
          "Failed to create shipment. Please check your input and try again."
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-8 max-w-4xl mx-auto">
      <h1 className="text-2xl font-bold mb-6">Create New Shipment</h1>

      {error && (
        <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Order ID */}
        <div className="form-group">
          <label className="block text-sm font-medium mb-2">
            Order ID *
          </label>
          <input
            type="text"
            name="orderId"
            value={formData.orderId}
            onChange={handleChange}
            placeholder="Enter the paid order ID"
            required
            className="w-full px-3 py-2 border rounded"
          />
        </div>

        {/* Origin */}
        <div className="border-t pt-4">
          <h2 className="text-lg font-semibold mb-4">Origin Address</h2>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="form-group">
              <label className="block text-sm font-medium mb-2">
                Address *
              </label>
              <input
                type="text"
                name="originAddress"
                value={formData.originAddress}
                onChange={handleChange}
                required
                className="w-full px-3 py-2 border rounded"
              />
            </div>
            <div className="form-group">
              <label className="block text-sm font-medium mb-2">
                Region *
              </label>
              <input
                type="text"
                name="originRegion"
                value={formData.originRegion}
                onChange={handleChange}
                required
                className="w-full px-3 py-2 border rounded"
              />
            </div>
            <div className="form-group">
              <label className="block text-sm font-medium mb-2">
                Country Code *
              </label>
              <input
                type="text"
                name="originCountryCode"
                value={formData.originCountryCode}
                onChange={handleChange}
                placeholder="e.g., LK, US"
                maxLength={2}
                required
                className="w-full px-3 py-2 border rounded"
              />
            </div>
          </div>
        </div>

        {/* Destination */}
        <div className="border-t pt-4">
          <h2 className="text-lg font-semibold mb-4">Destination Address</h2>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="form-group">
              <label className="block text-sm font-medium mb-2">
                Address *
              </label>
              <input
                type="text"
                name="destinationAddress"
                value={formData.destinationAddress}
                onChange={handleChange}
                required
                className="w-full px-3 py-2 border rounded"
              />
            </div>
            <div className="form-group">
              <label className="block text-sm font-medium mb-2">
                Region *
              </label>
              <input
                type="text"
                name="destinationRegion"
                value={formData.destinationRegion}
                onChange={handleChange}
                required
                className="w-full px-3 py-2 border rounded"
              />
            </div>
            <div className="form-group">
              <label className="block text-sm font-medium mb-2">
                Country Code *
              </label>
              <input
                type="text"
                name="destinationCountryCode"
                value={formData.destinationCountryCode}
                onChange={handleChange}
                placeholder="e.g., LK, US"
                maxLength={2}
                required
                className="w-full px-3 py-2 border rounded"
              />
            </div>
          </div>
        </div>

        {/* Package Details */}
        <div className="border-t pt-4">
          <h2 className="text-lg font-semibold mb-4">Package Details</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="form-group">
              <label className="block text-sm font-medium mb-2">
                Declared Value
              </label>
              <input
                type="number"
                name="declaredValue"
                value={formData.declaredValue}
                onChange={handleChange}
                step="0.01"
                min="0"
                placeholder="Leave empty to use order total"
                className="w-full px-3 py-2 border rounded"
              />
            </div>
            <div className="form-group">
              <label className="block text-sm font-medium mb-2">
                Currency
              </label>
              <select
                name="currency"
                value={formData.currency}
                onChange={handleChange}
                className="w-full px-3 py-2 border rounded"
              >
                <option value="USD">USD</option>
                <option value="LKR">LKR</option>
                <option value="EUR">EUR</option>
                <option value="GBP">GBP</option>
              </select>
            </div>
            <div className="form-group md:col-span-2">
              <label className="block text-sm font-medium mb-2">
                Package Description *
              </label>
              <textarea
                name="packageDescription"
                value={formData.packageDescription}
                onChange={handleChange}
                rows={3}
                required
                className="w-full px-3 py-2 border rounded"
              />
            </div>
            <div className="form-group">
              <label className="block text-sm font-medium mb-2">
                Weight (kg)
              </label>
              <input
                type="number"
                name="packageWeight"
                value={formData.packageWeight}
                onChange={handleChange}
                step="0.01"
                min="0"
                className="w-full px-3 py-2 border rounded"
              />
            </div>
            <div className="form-group">
              <label className="block text-sm font-medium mb-2">
                Dimensions
              </label>
              <input
                type="text"
                name="packageDimensions"
                value={formData.packageDimensions}
                onChange={handleChange}
                placeholder="e.g., 30x20x10 cm"
                className="w-full px-3 py-2 border rounded"
              />
            </div>
          </div>
        </div>

        {/* Shipping Options */}
        <div className="border-t pt-4">
          <h2 className="text-lg font-semibold mb-4">Shipping Options</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="form-group">
              <label className="block text-sm font-medium mb-2">
                Preferred Service
              </label>
              <select
                name="preferredService"
                value={formData.preferredService}
                onChange={handleChange}
                className="w-full px-3 py-2 border rounded"
              >
                <option value="Standard">Standard</option>
                <option value="Express">Express</option>
                <option value="Priority">Priority</option>
              </select>
            </div>
            <div className="form-group flex items-center">
              <input
                type="checkbox"
                name="exportRequired"
                checked={formData.exportRequired}
                onChange={handleChange}
                className="mr-2"
              />
              <label className="text-sm font-medium">
                Export Documentation Required
              </label>
            </div>
            <div className="form-group md:col-span-2">
              <label className="block text-sm font-medium mb-2">
                Special Handling Notes
              </label>
              <textarea
                name="specialHandlingNotes"
                value={formData.specialHandlingNotes}
                onChange={handleChange}
                rows={2}
                placeholder="Any special instructions for handling this shipment"
                className="w-full px-3 py-2 border rounded"
              />
            </div>
          </div>
        </div>

        {/* Submit Button */}
        <div className="border-t pt-4 flex gap-4">
          <button
            type="submit"
            disabled={loading}
            className="bg-blue-500 hover:bg-blue-600 text-white px-6 py-2 rounded disabled:opacity-50"
          >
            {loading ? "Creating..." : "Create Shipment"}
          </button>
          <button
            type="button"
            onClick={() => navigate("/seller/shipments")}
            className="bg-gray-300 hover:bg-gray-400 text-gray-800 px-6 py-2 rounded"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}

export default CreateShipment;
