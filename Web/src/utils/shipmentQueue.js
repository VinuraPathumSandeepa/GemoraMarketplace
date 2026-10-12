const normalizeStatus = (value) =>
  String(value ?? "")
    .replace(/[\s_-]+/g, "")
    .toLowerCase();

export const STATUS_GROUPS = Object.freeze({
  Pending: ["Pending", "Planning", "PlanGenerated", "ReadyForBooking"],
  "In Transit": ["Booked", "PickedUp", "InTransit", "CustomsHold", "OutForDelivery"],
  Delivered: ["Delivered"],
  Exceptions: ["Exception", "DeliveryFailed", "CustomsHold"],
});

export function matchesStatus(shipment, filter) {
  if (!filter || filter === "All") return true;

  const status = normalizeStatus(shipment?.status);
  const groupedStatuses = STATUS_GROUPS[filter];

  if (groupedStatuses) {
    return groupedStatuses.some((candidate) => normalizeStatus(candidate) === status);
  }

  return normalizeStatus(filter) === status;
}

export function readableStatus(value) {
  if (!value) return "Not provided";
  if (STATUS_GROUPS[value]) return value;

  return String(value)
    .replace(/[_-]+/g, " ")
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .replace(/\s+/g, " ")
    .trim()
    .replace(/\b\w/g, (character) => character.toUpperCase());
}

export function shortRef(id, prefix = "REF", allIds = []) {
  if (!id) return `${prefix}-UNASSIGNED`;

  const compactId = String(id).replace(/[^a-zA-Z0-9]/g, "").toUpperCase();
  const compactIds = allIds
    .filter(Boolean)
    .map((value) => String(value).replace(/[^a-zA-Z0-9]/g, "").toUpperCase());

  let length = Math.min(8, compactId.length);
  while (
    length < compactId.length &&
    compactIds.filter((value) => value.startsWith(compactId.slice(0, length))).length > 1
  ) {
    length += 1;
  }

  return `${prefix}-${compactId.slice(0, length)}`;
}

export function nextShipmentAction(shipment) {
  const status = normalizeStatus(shipment?.status);

  const actions = {
    pending: {
      label: "Analyze risk",
      reason: "A shipping plan has not been generated.",
      tab: "plan",
      urgency: 0,
    },
    planning: {
      label: "Continue analysis",
      reason: "Shipping analysis is in progress or needs revision.",
      tab: "plan",
      urgency: 0,
    },
    plangenerated: {
      label: "Review plan",
      reason: "The generated plan is waiting for an admin decision.",
      tab: "plan",
      urgency: 0,
    },
    readyforbooking: {
      label: "Record booking",
      reason: "The approved plan is ready for courier booking.",
      tab: "booking",
      urgency: 1,
    },
    booked: {
      label: "Update tracking",
      reason: "The courier booking is ready for pickup updates.",
      tab: "tracking",
      urgency: 2,
    },
    pickedup: {
      label: "Update tracking",
      reason: "The parcel has been picked up by the courier.",
      tab: "tracking",
      urgency: 2,
    },
    intransit: {
      label: "Update tracking",
      reason: "Monitor the shipment and record its next event.",
      tab: "tracking",
      urgency: 3,
    },
    customshold: {
      label: "Resolve customs hold",
      reason: "The shipment needs customs attention.",
      tab: "tracking",
      urgency: 0,
    },
    outfordelivery: {
      label: "Confirm delivery",
      reason: "The parcel is with the final-mile courier.",
      tab: "tracking",
      urgency: 2,
    },
    deliveryfailed: {
      label: "Resolve delivery failure",
      reason: "The last delivery attempt failed.",
      tab: "tracking",
      urgency: 0,
    },
    exception: {
      label: "Resolve exception",
      reason: "An operational exception needs review.",
      tab: "tracking",
      urgency: 0,
    },
    delivered: {
      label: "View shipment",
      reason: "Delivery is complete.",
      tab: "overview",
      urgency: null,
    },
    cancelled: {
      label: "View shipment",
      reason: "This shipment was cancelled.",
      tab: "overview",
      urgency: null,
    },
  };

  return (
    actions[status] ?? {
      label: "Review shipment",
      reason: "Review this shipment before taking the next action.",
      tab: "overview",
      urgency: 4,
    }
  );
}

export function sortByUrgency(shipments) {
  return [...shipments].sort((left, right) => {
    const leftUrgency = nextShipmentAction(left).urgency;
    const rightUrgency = nextShipmentAction(right).urgency;
    const leftRank = leftUrgency ?? Number.MAX_SAFE_INTEGER;
    const rightRank = rightUrgency ?? Number.MAX_SAFE_INTEGER;

    if (leftRank !== rightRank) return leftRank - rightRank;

    const leftCreatedAt = Date.parse(left?.createdAt ?? "") || 0;
    const rightCreatedAt = Date.parse(right?.createdAt ?? "") || 0;
    return leftCreatedAt - rightCreatedAt;
  });
}
