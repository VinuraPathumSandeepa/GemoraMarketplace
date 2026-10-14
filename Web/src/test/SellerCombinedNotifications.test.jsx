import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, useLocation } from "react-router-dom";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import NotificationBell from "../components/NotificationBell";
import { NotificationProvider } from "../context/NotificationContext";
import { useAuth } from "../context/AuthContext";
import { notificationService } from "../services/notificationService";
import { orderInboxRequest } from "../services/buyerApi";

vi.mock("../context/AuthContext", () => ({ useAuth: vi.fn() }));
vi.mock("../services/notificationService", () => ({
  notificationService: { list: vi.fn(), markRead: vi.fn(), markAllRead: vi.fn() },
  notificationError: () => "Notifications are unavailable right now. Please try again.",
}));
vi.mock("../services/buyerApi", () => ({ orderInboxRequest: vi.fn() }));

const initialOrder = () => ({
  id: 42, gemTitle: "Blue Sapphire", orderNumber: "ORD-42", buyerName: "A buyer",
  createdAt: "2026-10-14T10:00:00Z", agreedPrice: 12500, currency: "LKR",
  status: "Pending", sellerReadAt: null,
});
const initialNotifications = () => [
  { id: 1, type: "GemApproved", title: "Gemstone Approved", message: "Your ruby was approved", isRead: false, createdAt: "2026-10-14T10:00:00Z", relatedEntityType: "GemListing", relatedEntityId: 7 },
  { id: 2, type: "Order", title: "Duplicate order alert", message: "Order 42", isRead: false, createdAt: "2026-10-14T10:00:00Z", relatedEntityType: "Order", relatedEntityId: 42 },
  { id: 3, type: "GemSubmitted", title: "Listing Submitted", message: "Your listing was submitted", isRead: true, createdAt: "2026-10-14T10:00:00Z", relatedEntityType: "GemListing", relatedEntityId: 8 },
];

let orders;
let notifications;

beforeEach(() => {
  vi.clearAllMocks();
  orders = [initialOrder()];
  notifications = initialNotifications();
  useAuth.mockReturnValue({ user: { id: "seller-1", role: "Seller" } });
  notificationService.list.mockImplementation(async () => notifications.map((item) => ({ ...item })));
  notificationService.markRead.mockImplementation(async (id) => {
    notifications = notifications.map((item) => item.id === id ? { ...item, isRead: true } : item);
  });
  notificationService.markAllRead.mockImplementation(async () => {
    notifications = notifications.map((item) => ({ ...item, isRead: true }));
  });
  orderInboxRequest.mockImplementation(async (url, options) => {
    if (!options) return orders.map((order) => ({ ...order }));
    const id = Number(url.match(/\/orders\/(\d+)\//)?.[1]);
    orders = orders.map((order) => order.id !== id ? order : {
      ...order,
      sellerReadAt: url.endsWith("/message/read") ? "2026-10-14T10:01:00Z" : order.sellerReadAt,
      status: url.endsWith("/approve") ? "Confirmed" : url.endsWith("/reject") ? "Rejected" : order.status,
    });
    return {};
  });
});
afterEach(() => vi.unstubAllGlobals());

function renderBell() {
  function CurrentPath() {
    return <output data-testid="current-path">{useLocation().pathname}</output>;
  }
  return render(<MemoryRouter><NotificationProvider><NotificationBell seller /><CurrentPath /></NotificationProvider></MemoryRouter>);
}

it("shows one bell with de-duplicated order and general unread counts", async () => {
  renderBell();
  const bell = await screen.findByRole("button", { name: "Notifications, 2 unread" });
  expect(screen.queryByRole("button", { name: /^Orders,/ })).not.toBeInTheDocument();
  fireEvent.click(bell);
  const dialog = screen.getByRole("dialog", { name: "Notifications" });
  expect(within(dialog).getByText("Order Requests")).toBeInTheDocument();
  expect(within(dialog).getByText("Blue Sapphire")).toBeInTheDocument();
  expect(within(dialog).getByText("A buyer placed an order", { exact: false })).toBeInTheDocument();
  expect(within(dialog).getByText("LKR 12,500.00")).toBeInTheDocument();
  expect(within(dialog).getByText("General Notifications")).toBeInTheDocument();
  expect(await within(dialog).findByText("Gemstone Approved")).toBeInTheDocument();
  expect(within(dialog).getByText("Listing Submitted")).toBeInTheDocument();
  expect(within(dialog).queryByText("Duplicate order alert")).not.toBeInTheDocument();
  expect(within(dialog).getByRole("button", { name: "View all orders" })).toBeInTheDocument();
  expect(within(dialog).getByRole("button", { name: /View all notifications/ })).toBeInTheDocument();
  fireEvent.pointerDown(document.body);
  expect(screen.queryByRole("dialog", { name: "Notifications" })).not.toBeInTheDocument();
  expect(bell).toHaveAttribute("aria-expanded", "false");
});

it("updates the badge immediately after general and order read actions", async () => {
  renderBell();
  fireEvent.click(await screen.findByRole("button", { name: "Notifications, 2 unread" }));
  fireEvent.click(screen.getByRole("button", { name: "Mark all read" }));
  await waitFor(() => expect(screen.getByRole("button", { name: "Notifications, 1 unread" })).toBeInTheDocument());
  expect(notificationService.markAllRead).toHaveBeenCalledOnce();
  fireEvent.click(screen.getByRole("button", { name: "Mark read" }));
  await waitFor(() => expect(screen.getByRole("button", { name: "Notifications" })).toBeInTheDocument());
  expect(orderInboxRequest).toHaveBeenCalledWith("/api/seller/orders/42/message/read", expect.objectContaining({ method: "POST" }));
});

it("marks an individual general notification read and opens its listing", async () => {
  renderBell();
  fireEvent.click(await screen.findByRole("button", { name: "Notifications, 2 unread" }));
  fireEvent.click(await screen.findByRole("button", { name: /Gemstone Approved/ }));
  await waitFor(() => expect(notificationService.markRead).toHaveBeenCalledWith(1));
  await waitFor(() => expect(screen.getByTestId("current-path")).toHaveTextContent("/seller/listings/7"));
  expect(screen.queryByRole("dialog", { name: "Notifications" })).not.toBeInTheDocument();
});

it.each([
  ["Approve", "approve", "Confirmed"],
  ["Reject", "reject", "Rejected"],
  ["Already sold", "reject", "Rejected"],
])("keeps the %s order action in the combined dropdown", async (label, action, status) => {
  renderBell();
  fireEvent.click(await screen.findByRole("button", { name: "Notifications, 2 unread" }));
  fireEvent.click(screen.getByRole("button", { name: label }));
  if (label === "Reject") fireEvent.change(screen.getByRole("textbox", { name: "Rejection reason" }), { target: { value: "Unable to fulfill" } });
  fireEvent.click(screen.getByRole("button", { name: "Confirm decision" }));
  await waitFor(() => expect(orderInboxRequest).toHaveBeenCalledWith(`/api/seller/orders/42/${action}`, expect.objectContaining({ method: "POST" })));
  expect(JSON.parse(orderInboxRequest.mock.calls.find(([url]) => url.endsWith(`/${action}`))[1].body).alreadySold).toBe(label === "Already sold");
  fireEvent.click(screen.getByRole("button", { name: "View all orders" }));
  expect(await screen.findByText(status)).toBeInTheDocument();
  await waitFor(() => expect(screen.getByRole("button", { name: "Notifications, 1 unread" })).toBeInTheDocument());
});

it("shows order errors and keeps the Seller dropdown available", async () => {
  orderInboxRequest.mockRejectedValueOnce(new Error("Order service unavailable"));
  renderBell();
  fireEvent.click(screen.getByRole("button", { name: "Notifications" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Order service unavailable");
  expect(screen.getByText("General Notifications")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Refresh" }));
  expect(await screen.findByText("Blue Sapphire")).toBeInTheDocument();
});

it("shows both empty states and supports opening and closing at mobile width", async () => {
  vi.stubGlobal("innerWidth", 375);
  orders = [];
  notifications = [];
  renderBell();
  fireEvent.click(screen.getByRole("button", { name: "Notifications" }));
  expect(await screen.findByText("No pending requests. New buyer orders will appear here.")).toBeInTheDocument();
  expect(await screen.findByText("You’re all caught up.")).toBeInTheDocument();
  expect(screen.getByRole("dialog", { name: "Notifications" })).toHaveClass("seller-combined-notification-popover");
  fireEvent.keyDown(document, { key: "Escape" });
  expect(screen.queryByRole("dialog", { name: "Notifications" })).not.toBeInTheDocument();
});
