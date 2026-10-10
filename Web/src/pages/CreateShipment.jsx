import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import api, { shipmentApi } from "../services/api";
import ShipmentHeader from "../components/ShipmentHeader";

function CreateShipment() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [orders, setOrders] = useState([]);
  const [ordersLoading, setOrdersLoading] = useState(true);
  const [ordersError, setOrdersError] = useState("");

  useEffect(() => {
    let active = true;
    api.get("/Orders/my-shipment-eligible")
      .then(({ data }) => {
        if (active) setOrders(data);
      })
      .catch((err) => {
        if (active) setOrdersError(err.response?.data?.message || "Could not load paid orders. Check your session and that the backend has been restarted with the Orders endpoint.");
      })
      .finally(() => {
        if (active) setOrdersLoading(false);
      });
    return () => { active = false; };
  }, []);

  const [formData, setFormData] = useState({
    orderId: "",
    originAddress: "",
    originRegion: "",
    originCountryCode: "",
    destinationAddress: "",
    destinationRegion: "",
    destinationCountryCode: "",
    declaredValue: "",
    currency: "",
    packageDescription: "",
    packageWeight: "",
    packageDimensions: "",
    preferredService: "Standard",
    specialHandlingNotes: "",
    exportRequired: false,
  });

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    if (name === "orderId") {
      const order = orders.find((item) => item.id === value);
      if (order) {
        setFormData((prev) => ({
          ...prev,
          orderId: order.id,
          destinationAddress: order.shippingAddress || "",
          destinationRegion: order.shippingRegion || "",
          destinationCountryCode: order.shippingCountryCode || "",
          declaredValue: String(order.totalAmount),
          currency: order.currency,
        }));
        return;
      }
    }
    setFormData((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
      ...(name === "orderId" ? { declaredValue: "", currency: "" } : {}),
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");

    if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(formData.orderId.trim()) ||
        formData.orderId.trim() === "00000000-0000-0000-0000-000000000000") {
      setError("Enter the full order ID (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx) for a paid order belonging to your seller account.");
      return;
    }

    if (![formData.originCountryCode, formData.destinationCountryCode].every(
      (code) => /^[A-Za-z]{2}$/.test(code.trim())
    )) {
      setError("Enter a two-letter country code for both addresses, such as LK or US.");
      return;
    }

    setLoading(true);

    try {
      // Convert numeric fields
      const payload = {
        ...formData,
        orderId: formData.orderId.trim(),
        originAddress: formData.originAddress.trim(),
        originRegion: formData.originRegion.trim(),
        originCountryCode: formData.originCountryCode.trim().toUpperCase(),
        destinationAddress: formData.destinationAddress.trim(),
        destinationRegion: formData.destinationRegion.trim(),
        destinationCountryCode: formData.destinationCountryCode.trim().toUpperCase(),
        packageDescription: formData.packageDescription.trim(),
        // The backend derives financial details from the paid order.
        declaredValue: undefined,
        currency: undefined,
        packageWeight: formData.packageWeight
          ? parseFloat(formData.packageWeight)
          : undefined,
      };

      const response = await shipmentApi.createShipment(payload);
      navigate(`/seller/shipments/${response.data.id}`);
    } catch (err) {
      console.error("Failed to create shipment:", err);
      const data = err.response?.data;
      const validationErrors = data?.errors
        ? Object.entries(data.errors).flatMap(([field, messages]) =>
            (Array.isArray(messages) ? messages : [messages]).map(
              (message) => `${field}: ${message}`
            )
          ).join(" ")
        : "";
      const statusMessage = {
        401: "Your session has expired. Sign in again to create a shipment.",
        403: "Only the seller who owns this order can create its shipment.",
        500: "The server could not create the shipment. Check the backend logs for the cause.",
      }[err.response?.status];
      setError(
        data?.message || validationErrors || statusMessage || data?.title ||
          (!err.response ? "Cannot reach the shipment API. Check that the backend is running at http://localhost:5198." : "") ||
          `Shipment API rejected the request (HTTP ${err.response?.status ?? "unknown"}). Check the request response in the browser Network tab.`
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="seller-shipping shipping-create">
      <ShipmentHeader title="Create Shipment" eyebrow="NEW GEMSTONE SHIPMENT"
        description="Prepare your gemstone for a secure journey. Add delivery details, package information, and your preferred shipping service." />

      {error && (
        <div role="alert" className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Order ID */}
        <section className="shipping-card shipping-order">
          <h2>Order Information</h2>
          <p className="shipping-section-description">Connect this shipment to a paid gemstone order.</p>
        <div className="form-group">
          <label className="block text-sm font-medium mb-2">
            Order ID *
          </label>
          <select
            name="orderId"
            value={formData.orderId}
            onChange={handleChange}
            required
            disabled={ordersLoading || orders.length === 0}
            className="w-full px-3 py-2 border rounded"
          >
            <option value="">{ordersLoading ? "Loading paid orders..." : "Select a paid order"}</option>
            {orders.map((order) => (
              <option key={order.id} value={order.id}>
                {order.gemTitle} — {order.currency} {order.totalAmount} — {order.id}
              </option>
            ))}
          </select>
          {ordersError ? <small role="alert">{ordersError}</small> :
            !ordersLoading && orders.length === 0 ?
              <small className="form-helper">No paid orders are available for shipment. Orders must belong to your seller account and have no existing shipment.</small> :
              <small className="form-helper">Selecting an order fills in its delivery address, value, and currency.</small>}
        </div>

        </section>
        {/* Origin */}
        <div className="shipping-card">
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
        <div className="shipping-card">
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
        <div className="shipping-card">
          <h2 className="text-lg font-semibold mb-4">Package Details</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="form-group">
              <label className="block text-sm font-medium mb-2">
                Paid Order Value
              </label>
              <input
                type="number"
                name="declaredValue"
                value={formData.declaredValue}
                readOnly
                step="0.01"
                min="0"
                placeholder="Select a paid order"
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
                disabled
                className="w-full px-3 py-2 border rounded"
              >
                <option value="">Select a paid order</option>
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
        <div className="shipping-card">
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
        <div className="shipping-form-actions">
          <button
            type="submit"
            disabled={loading || ordersLoading || orders.length === 0 || !formData.orderId}
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
