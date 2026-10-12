import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, useLocation } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import SellerDashboard from "../pages/SellerDashboard";
import gemListingService from "../services/gemVerification/gemListingService";

vi.mock("../layouts/DashboardLayout", () => ({ default: ({ children }) => <div>{children}</div> }));
vi.mock("../services/gemVerification/gemListingService", () => ({ default: { getMyListings: vi.fn() } }));

beforeEach(() => {
  gemListingService.getMyListings.mockResolvedValue([
    { status: "Draft" },
    { status: "Draft" },
    { status: "PendingVerification" },
    { status: "Approved" },
    { status: "ChangesRequested" },
  ]);
});

afterEach(() => vi.unstubAllGlobals());

function CurrentPath() {
  const location = useLocation();
  return <output data-testid="current-path">{location.pathname}</output>;
}

describe("SellerDashboard", () => {
  it("shows live listing counts, actionable routes, and one centered gemstone", async () => {
    const { container } = render(<MemoryRouter><CurrentPath /><SellerDashboard /></MemoryRouter>);
    await waitFor(() => expect(within(screen.getByText("Total Listings").closest("article")).getByText("5")).toBeInTheDocument());
    expect(within(screen.getByText("Drafts").closest("article")).getByText("2")).toBeInTheDocument();
    expect(within(screen.getByText("Pending Review").closest("article")).getByText("1")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /1 listing requires your attention/ })).toBeInTheDocument();
    expect(container.querySelectorAll(".seller-dashboard-gem-rotator img")).toHaveLength(1);
    fireEvent.click(screen.getByRole("button", { name: "View My Listings" }));
    expect(screen.getByTestId("current-path")).toHaveTextContent("/seller/listings");
  });

  it("navigates to the create listing form", () => {
    render(<MemoryRouter><CurrentPath /><SellerDashboard /></MemoryRouter>);
    fireEvent.click(screen.getByRole("button", { name: /Create Gem Listing/ }));
    expect(screen.getByTestId("current-path")).toHaveTextContent("/seller/listings/create");
  });

  it("shows the gemstone without playback controls", () => {
    vi.stubGlobal("matchMedia", vi.fn(() => ({ matches: true, addEventListener() {}, removeEventListener() {} })));
    const { container } = render(<MemoryRouter><SellerDashboard /></MemoryRouter>);
    expect(container.querySelector(".seller-dashboard-gem-rotator img")).toBeInTheDocument();
    expect(container.querySelector(".seller-gem-motion-control")).not.toBeInTheDocument();
  });
});
