import { fireEvent, render, screen, within } from "@testing-library/react";
import { MemoryRouter, useLocation } from "react-router-dom";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import DashboardLayout from "../layouts/DashboardLayout";
import BuyerLayout from "../layouts/BuyerLayout";
import { useAuth } from "../context/AuthContext";

vi.mock("../context/AuthContext", () => ({ useAuth: vi.fn() }));
vi.mock("../context/NotificationContext", () => ({
  NotificationProvider: ({ children }) => children,
}));
vi.mock("../context/WishlistContext", () => ({
  WishlistProvider: ({ children }) => children,
}));
vi.mock("../components/OrderInbox", () => ({
  default: () => <span>Orders</span>,
}));
vi.mock("../components/NotificationBell", () => ({
  default: () => <span>Notifications</span>,
}));
vi.mock("../components/UserAvatar", () => ({
  default: () => <span>Avatar</span>,
}));
vi.mock("../components/buyer/SiteFooter", () => ({ default: () => null }));
vi.mock("../components/buyer/MarketplaceAiAssistant", () => ({ default: () => null }));
vi.mock("../components/buyer/BuyerUI", () => ({
  Dialog: ({ children }) => <div>{children}</div>,
}));

function CurrentPath() {
  const location = useLocation();
  return <output data-testid="current-path">{location.pathname}</output>;
}

beforeEach(() => {
  vi.stubGlobal("scrollTo", vi.fn());
  useAuth.mockReturnValue({
    user: { id: "seller-1", role: "Seller", fullName: "Gem Seller" },
    logout: vi.fn(),
  });
});

afterEach(() => vi.unstubAllGlobals());

it("shows the four Seller links in desktop and mobile navigation without Gem Listings", () => {
  const { container } = render(
    <MemoryRouter initialEntries={["/seller"]}>
      <CurrentPath />
      <DashboardLayout><p>Seller content</p></DashboardLayout>
    </MemoryRouter>
  );

  for (const selector of [".gemora-main-nav", ".gemora-mobile-nav"]) {
    const navigation = within(container.querySelector(selector));
    expect(navigation.getAllByRole("link").map(link => link.textContent))
      .toEqual(["Dashboard", "My Listings", "Create Listing", "My Shipments"]);
    expect(navigation.queryByRole("link", { name: "Gem Listings" })).not.toBeInTheDocument();
  }

  expect(screen.getByText("Orders")).toBeInTheDocument();
  expect(screen.getByText("Notifications")).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Open account menu" })).toBeInTheDocument();
  fireEvent.click(within(container.querySelector(".gemora-main-nav"))
    .getByRole("link", { name: "My Listings" }));
  expect(screen.getByTestId("current-path")).toHaveTextContent("/seller/listings");
});

it("preserves Buyer marketplace and orders navigation", () => {
  useAuth.mockReturnValue({
    user: { id: "buyer-1", role: "Buyer", fullName: "Gem Buyer" },
    logout: vi.fn(),
  });

  render(<MemoryRouter initialEntries={["/buyer/dashboard"]}><BuyerLayout /></MemoryRouter>);

  const navigation = screen.getByRole("navigation", { name: "Buyer navigation" });
  expect(within(navigation).getByRole("link", { name: "Marketplace" }))
    .toHaveAttribute("href", "/buyer/marketplace");
  expect(within(navigation).getByRole("link", { name: "My Orders" }))
    .toHaveAttribute("href", "/buyer/orders");
});
