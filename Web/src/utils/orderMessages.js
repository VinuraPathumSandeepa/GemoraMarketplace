import { utcTime } from "./orderTime.js";

const label = value => (value || "Pending").replace(/([a-z])([A-Z])/g, "$1 $2");

export function latestOrderMessage(order) {
  const events = [
    ...(order.statusHistory || []).map(event => ({
      at: utcTime(event.createdAt),
      text: event.reason || `Order ${label(event.newStatus).toLowerCase()}.`,
      label: event.newStatus === "Confirmed" ? "Approved" : label(event.newStatus),
    })),
    ...(order.fulfillmentHistory || []).map(event => ({
      at: utcTime(event.createdAt),
      text: event.note || `Delivery status: ${label(event.newStatus)}.`,
      label: event.note?.startsWith("Your shipping plan has been approved.")
        ? "Shipping approved" : label(event.newStatus),
    })),
  ];
  return events.sort((a, b) => (b.at || 0) - (a.at || 0))[0] || {
    text: `Order ${label(order.status).toLowerCase()}.`,
    label: order.status === "Confirmed" ? "Approved" : label(order.status),
  };
}
