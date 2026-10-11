import { useState } from "react";
import { shipmentApi } from "../services/api";

export default function CourierBookingForm({ shipment, onSaved }) {
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [form, setForm] = useState({
    courierName: "", trackingNumber: "", externalShipmentReference: "",
    selectedService: "", trackingUrl: "", bookedAt: "", confirmedWithCourier: false,
  });
  const change = event => setForm(current => ({
    ...current, [event.target.name]: event.target.type === "checkbox" ? event.target.checked : event.target.value,
  }));
  async function save(event) {
    event.preventDefault();
    setSaving(true); setError("");
    try {
      await shipmentApi.recordCourierBooking(shipment.id, {
        ...form, bookedAt: new Date(form.bookedAt).toISOString(),
      });
      await onSaved();
      window.dispatchEvent(new Event("gemora-orders-changed"));
    } catch (err) { setError(err.response?.data?.message || "Could not save the courier booking. Please try again."); }
    finally { setSaving(false); }
  }
  return <form onSubmit={save} className="admin-shipping-panel">
    <h3>Record courier booking</h3>
    <p>Enter the details issued by your courier after booking the shipment.</p>
    <div className="grid grid-cols-2 gap-4" style={{ marginTop: 20 }}>
      {[
        ["courierName", "Courier", "text", 150, true],
        ["trackingNumber", "Tracking number", "text", 150, true],
        ["externalShipmentReference", "Booking reference (optional)", "text", 150, false],
        ["selectedService", "Courier service", "text", 50, true],
        ["trackingUrl", "Tracking link (optional)", "url", 1000, false],
        ["bookedAt", "Booking date and time", "datetime-local", undefined, true],
      ].map(([name, label, type, maxLength, required]) => <div key={name}>
        <label htmlFor={`courier-${name}`}>{label}</label>
        <input id={`courier-${name}`} name={name} type={type} value={form[name]} onChange={change}
          required={required} maxLength={maxLength} disabled={saving}
          placeholder={name === "trackingUrl" ? "https://" : undefined} />
      </div>)}
    </div>
    <label style={{ display: "flex", gap: 10, marginBlock: 20, alignItems: "center" }}>
      <input name="confirmedWithCourier" type="checkbox" checked={form.confirmedWithCourier} onChange={change}
        required disabled={saving} style={{ width: "auto" }} />
      I confirm this booking was issued by the courier for this shipment.
    </label>
    {error && <p role="alert" className="text-red-600">{error}</p>}
    <button type="submit" className="bg-blue-600" disabled={saving}>{saving ? "Saving..." : "Save courier booking"}</button>
  </form>;
}
