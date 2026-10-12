import { Link } from "react-router-dom";

import {
  matchesStatus,
  nextShipmentAction,
  shortRef,
  sortByUrgency,
} from "../utils/shipmentQueue";

export default function ShippingAIAgentPanel({ shipments = [] }) {
  const shipmentIds = shipments.map((shipment) => shipment.id);
  const recommendations = sortByUrgency(shipments)
    .filter((shipment) => nextShipmentAction(shipment).urgency !== null)
    .slice(0, 5);
  const exceptionCount = shipments.filter((shipment) =>
    matchesStatus(shipment, "Exceptions"),
  ).length;

  return (
    <section className="admin-shipping-panel" aria-labelledby="shipping-agent-title">
      <p className="admin-shipping-eyebrow">Decision support</p>
      <h2 id="shipping-agent-title">Shipping AI work recommendations</h2>
      <p>
        The queue highlights the next operational action from current shipment
        data. An administrator still reviews and approves every plan, booking,
        insurance record, and tracking update.
      </p>

      <div className="shipment-dashboard-shortcuts" aria-label="Queue summary">
        <span>{recommendations.length} priority item{recommendations.length === 1 ? "" : "s"}</span>
        <span>{exceptionCount} exception{exceptionCount === 1 ? "" : "s"}</span>
      </div>

      {recommendations.length === 0 ? (
        <p>No shipment currently needs an administrative action.</p>
      ) : (
        <ul>
          {recommendations.map((shipment) => {
            const action = nextShipmentAction(shipment);
            const reference = shortRef(shipment.id, "SHP", shipmentIds);

            return (
              <li key={shipment.id}>
                <strong>{reference}</strong>: {action.reason}{" "}
                <Link to={`/admin/shipments/${shipment.id}?tab=${action.tab}`}>
                  {action.label}
                </Link>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}
