import { useState } from "react";
import { shipmentApi } from "../services/api";

export default function IssuedInsuranceForm({ shipment, onSaved, replacingDemo }) {
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [form, setForm] = useState({
    providerName: "Jewelers Mutual Group – JM Shipping Solution", policyNumber: "",
    coverageAmount: String(shipment.declaredValue), premiumAmount: "", coverageType: "",
    policyStartDate: "", policyEndDate: "",
  });
  const change = event => setForm(current => ({ ...current, [event.target.name]: event.target.value }));
  const save = async event => {
    event.preventDefault();
    setError("");
    if (form.policyEndDate <= form.policyStartDate) {
      setError("The policy end date must be after its start date."); return;
    }
    setSaving(true);
    try {
      await shipmentApi.createInsurance(shipment.id, {
        ...form, shipmentId: shipment.id, declaredValue: shipment.declaredValue,
        currency: shipment.currency, coverageAmount: Number(form.coverageAmount),
        premiumAmount: Number(form.premiumAmount),
        policyStartDate: `${form.policyStartDate}T00:00:00Z`,
        policyEndDate: `${form.policyEndDate}T00:00:00Z`,
      });
      await onSaved();
    } catch (err) { setError(err.response?.data?.message || "Could not record the policy. Please try again."); }
    finally { setSaving(false); }
  };
  return <form onSubmit={save} className="admin-shipping-panel">
    <h3>Record an issued insurance policy</h3>
    <p>Enter the details from your insurer's policy or coverage certificate. Saving records the policy for this shipment; it does not purchase insurance or verify coverage with the insurer.</p>
    {replacingDemo && <p>This replaces the simulated record. The previous simulation reference is retained in the audit history.</p>}
    <div className="grid grid-cols-2 gap-4" style={{ marginTop: 20 }}>
      {[
        ["providerName", "Insurance provider", "text"], ["policyNumber", "Issued policy / certificate number", "text"],
        ["coverageAmount", `Coverage amount (${shipment.currency})`, "number"],
        ["premiumAmount", `Premium paid (${shipment.currency})`, "number"],
        ["coverageType", "Coverage type (as issued)", "text"],
        ["policyStartDate", "Policy start date", "date"], ["policyEndDate", "Policy end date", "date"],
      ].map(([name, label, type]) => <div key={name}>
        <label htmlFor={`insurance-${name}`}>{label}</label>
        <input id={`insurance-${name}`} name={name} type={type} value={form[name]} onChange={change} required
          maxLength={type === "text" ? (name === "providerName" ? 200 : name === "policyNumber" ? 100 : 50) : undefined}
          min={type === "number" ? (name === "coverageAmount" ? "0.01" : "0") : undefined}
          step={type === "number" ? "0.01" : undefined} />
      </div>)}
    </div>
    <label style={{ display: "flex", gap: 10, marginBlock: 20, alignItems: "center" }}>
      <input type="checkbox" required style={{ width: "auto" }} /> I confirm this policy has been issued and applies to this shipment.
    </label>
    {error && <p role="alert" className="text-red-600">{error}</p>}
    <button type="submit" className="bg-blue-600" disabled={saving}>{saving ? "Saving policy…" : "Record issued policy"}</button>
  </form>;
}
