import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, useLocation } from "react-router-dom";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import AdminDashboard from "../pages/AdminDashboard";
import { useAuth } from "../context/AuthContext";
import adminGemListingService from "../services/adminGemListingService";
import transactionService from "../services/marketplace/transactionService";
import { shipmentApi } from "../services/api";

vi.mock("../context/AuthContext", () => ({ useAuth: vi.fn() }));
vi.mock("../services/adminGemListingService", () => ({ default: { getAll: vi.fn() } }));
vi.mock("../services/marketplace/transactionService", () => ({ default: { getAll: vi.fn() } }));
vi.mock("../services/api", () => ({ shipmentApi: { getMyShipments: vi.fn() } }));

const listings = [
  { id: 17, title: "Blue Sapphire", status: "Approved", createdAt: "2026-09-11T08:00:00Z" },
  { id: 18, title: "Red Ruby", status: "Draft", createdAt: "2026-09-09T08:00:00Z" },
];
const transactions = [
  { id: "order-1", orderNumber: "GEM-ORDER1", gemTitle: "Blue Sapphire", status: "Paid", createdAt: "2026-09-12T08:00:00Z" },
];
const shipments = [
  { id: "shipment-1", status: "PlanGenerated", createdAt: "2026-09-13T08:00:00Z" },
  { id: "shipment-2", status: "Delivered", createdAt: "2026-09-10T08:00:00Z" },
];

function Path() { return <output data-testid="path">{useLocation().pathname}</output>; }
function renderOverview() {
  return render(<MemoryRouter initialEntries={["/admin"]}><Path /><AdminDashboard /></MemoryRouter>);
}

beforeEach(() => {
  useAuth.mockReturnValue({ user: { role: "Admin", fullName: "Gemora Admin" }, logout: vi.fn() });
  adminGemListingService.getAll.mockResolvedValue(listings);
  transactionService.getAll.mockResolvedValue(transactions);
  shipmentApi.getMyShipments.mockResolvedValue({ data: shipments });
});
afterEach(() => vi.clearAllMocks());

it("renders real totals, recent records, and working quick actions", async () => {
  renderOverview();
  expect(await screen.findByText("Blue Sapphire", { selector: "a" })).toBeInTheDocument();
  expect(within(screen.getByRole("link", { name: "View Current gem listings" })).getByText("2")).toBeInTheDocument();
  expect(within(screen.getByRole("link", { name: "View Active shipments" })).getByText("1")).toBeInTheDocument();
  expect(within(screen.getByRole("link", { name: "View Total transactions" })).getByText("1")).toBeInTheDocument();
  expect(within(screen.getByRole("link", { name: "View Priority shipments" })).getByText("1")).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "SHP-SHIPMENT1" })).toHaveAttribute("href", "/admin/shipments/shipment-1");

  fireEvent.click(screen.getByRole("link", { name: "View shipments" }));
  expect(screen.getByTestId("path")).toHaveTextContent("/admin/shipments");
  expect(screen.getByRole("link", { name: "View transactions" })).toHaveAttribute("href", "/admin/transactions");
  expect(screen.getByRole("link", { name: "Manage gem listings" })).toHaveAttribute("href", "/admin/gem-listings");
});

it("shows loading skeletons until API requests settle", async () => {
  let finishListings;
  adminGemListingService.getAll.mockReturnValue(new Promise((resolve) => { finishListings = resolve; }));
  renderOverview();
  expect(screen.getByRole("status", { name: "Loading recent records" })).toBeInTheDocument();
  expect(screen.getAllByLabelText(/^Loading /)).toHaveLength(5);
  finishListings([]);
  expect(await screen.findByRole("link", { name: "GEM-ORDER1" })).toBeInTheDocument();
  expect(screen.queryByRole("status", { name: "Loading recent records" })).not.toBeInTheDocument();
});

it("shows zero and an empty state for empty API data", async () => {
  adminGemListingService.getAll.mockResolvedValue([]);
  transactionService.getAll.mockResolvedValue([]);
  shipmentApi.getMyShipments.mockResolvedValue({ data: [] });
  renderOverview();
  expect(await screen.findByText("No recent marketplace records yet.")).toBeInTheDocument();
  for (const label of ["Current gem listings", "Active shipments", "Total transactions", "Priority shipments"]) {
    expect(within(screen.getByRole("link", { name: `View ${label}` })).getByText("0")).toBeInTheDocument();
  }
});

it("keeps available sections visible when one endpoint fails and retries", async () => {
  adminGemListingService.getAll.mockRejectedValueOnce(new Error("network failure")).mockResolvedValueOnce(listings);
  renderOverview();
  expect(await screen.findByRole("alert")).toHaveTextContent("Some overview data is unavailable");
  expect(within(screen.getByRole("link", { name: "View Current gem listings" })).getByText("—")).toBeInTheDocument();
  expect(within(screen.getByRole("link", { name: "View Total transactions" })).getByText("1")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Retry data" }));
  await waitFor(() => expect(screen.queryByRole("alert")).not.toBeInTheDocument());
  expect(within(screen.getByRole("link", { name: "View Current gem listings" })).getByText("2")).toBeInTheDocument();
});

it("explains an unauthorized data response without crashing", async () => {
  adminGemListingService.getAll.mockRejectedValue({ response: { status: 403 } });
  renderOverview();
  expect(await screen.findByRole("alert")).toHaveTextContent("Your session cannot access this Admin data");
  expect(screen.getByRole("heading", { name: "Admin Dashboard" })).toBeInTheDocument();
});

it("does not present a paginated or malformed response as a complete listing count", async () => {
  adminGemListingService.getAll.mockResolvedValue({ items: listings, totalCount: 200 });
  renderOverview();
  expect(await screen.findByRole("alert")).toHaveTextContent("Unable to load listings.");
  expect(within(screen.getByRole("link", { name: "View Current gem listings" })).getByText("—")).toBeInTheDocument();
});
