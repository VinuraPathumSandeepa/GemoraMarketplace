import { Link } from "react-router-dom";
import "../styles/SellerShipping.css";

export default function ShipmentHeader({ title, eyebrow, description, backTo = "/seller/shipments", backLabel = "My Shipments", children }) {
  return (
    <header className="shipping-header">
      <Link className="shipping-back" to={backTo}>← {backLabel}</Link>
      <p className="shipping-eyebrow">{eyebrow}</p>
      <h1>{title}</h1>
      <p className="shipping-description">{description}</p>
      {children && <div className="shipping-header-actions">{children}</div>}
    </header>
  );
}
