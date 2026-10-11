import test from "node:test";
import assert from "node:assert/strict";
import { latestOrderMessage } from "./orderMessages.js";

test("shipping approval replaces the older payment notification", () => {
  const message = latestOrderMessage({
    status: "Paid",
    statusHistory: [{ newStatus: "Paid", reason: "Payment completed.", createdAt: "2026-10-10T09:00:00Z" }],
    fulfillmentHistory: [{ newStatus: "Preparing", note: "Your shipping plan has been approved. View My Orders for delivery progress.", createdAt: "2026-10-10T09:05:00" }],
  });
  assert.equal(message.label, "Shipping approved");
  assert.match(message.text, /shipping plan has been approved/);
});

test("delivery notification is selected even when histories arrive unsorted", () => {
  const message = latestOrderMessage({
    status: "Paid",
    statusHistory: [{ newStatus: "Paid", createdAt: "2026-10-10T09:00:00Z" }],
    fulfillmentHistory: [
      { newStatus: "Delivered", note: "Delivered. Confirm receipt.", createdAt: "2026-10-10T12:00:00Z" },
      { newStatus: "InTransit", note: "In transit.", createdAt: "2026-10-10T10:00:00Z" },
    ],
  });
  assert.equal(message.label, "Delivered");
  assert.equal(message.text, "Delivered. Confirm receipt.");
});

test("receipt confirmation supersedes the delivery message", () => {
  const message = latestOrderMessage({
    status: "Completed",
    statusHistory: [{ newStatus: "Completed", reason: "Buyer confirmed successful delivery.", createdAt: "2026-10-10T13:00:00Z" }],
    fulfillmentHistory: [{ newStatus: "Delivered", createdAt: "2026-10-10T12:00:00Z" }],
  });
  assert.equal(message.label, "Completed");
  assert.equal(message.text, "Buyer confirmed successful delivery.");
});
