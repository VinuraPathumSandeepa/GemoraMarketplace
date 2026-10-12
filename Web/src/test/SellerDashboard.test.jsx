import { render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import SellerDashboard from "../pages/SellerDashboard";
import gemListingService from "../services/gemVerification/gemListingService";

vi.mock("../layouts/DashboardLayout", () => ({ default: ({ children }) => <div>{children}</div> }));
vi.mock("../services/gemVerification/gemListingService", () => ({ default: { getMyListings: vi.fn() } }));

beforeEach(() => {
  gemListingService.getMyListings.mockResolvedValue([
    { status: "Draft" },
    { status: "draft" },
    { status: "PendingVerification" },
    { status: "Approved" },
    { status: "ChangesRequested" },
  ]);
});

afterEach(() => vi.unstubAllGlobals());

describe("SellerDashboard", () => {
  it("shows live listing counts, actionable routes, and one centered gemstone", async () => {
    const { container } = render(<MemoryRouter><SellerDashboard /></MemoryRouter>);
    await waitFor(() => expect(within(screen.getByText("Total listings").closest("article")).getByText("5")).toBeInTheDocument());
    expect(within(screen.getByText("Drafts").closest("article")).getByText("2")).toBeInTheDocument();
    expect(within(screen.getByText("Pending review").closest("article")).getByText("1")).toBeInTheDocument();
    expect(screen.getByText("1 listing needs your attention").closest("a")).toHaveAttribute("href", "/seller/listings");
    expect(screen.getByRole("link", { name: /Create a listing/ })).toHaveAttribute("href", "/seller/listings/create");
    expect(container.querySelectorAll(".seller-dashboard-gem-rotator img")).toHaveLength(1);
  });

  it("shows the gemstone without playback controls", () => {
    vi.stubGlobal("matchMedia", vi.fn(() => ({ matches: true, addEventListener() {}, removeEventListener() {} })));
    const { container } = render(<MemoryRouter><SellerDashboard /></MemoryRouter>);
    expect(container.querySelector(".seller-dashboard-gem-rotator img")).toBeInTheDocument();
    expect(container.querySelector(".seller-gem-motion-control")).not.toBeInTheDocument();
  });
});
