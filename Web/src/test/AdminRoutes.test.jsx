import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, expect, it, vi } from "vitest";
import App from "../App";
import { useAuth } from "../context/AuthContext";

vi.mock("../context/AuthContext", () => ({ useAuth: vi.fn() }));
vi.mock("../pages/AdminDashboard", () => ({ default: () => <p>Admin overview route</p> }));
vi.mock("../pages/AdminShipments", () => ({ default: () => <p>Admin shipment queue route</p> }));
vi.mock("../pages/AdminShipmentDetail", () => ({ default: () => <p>Admin shipment detail route</p> }));
vi.mock("../pages/admin/TransactionDashboard", () => ({ default: () => <p>Admin transaction route</p> }));
vi.mock("../pages/admin/AdminGemListings", () => ({ default: () => <p>Admin gem listings route</p> }));

beforeEach(() => {
  useAuth.mockReturnValue({
    user: { role: "Admin", fullName: "Gemora Admin" },
    isAuthenticated: true, loading: false, logout: vi.fn(),
  });
});

it.each([
  ["/admin", "Admin overview route"],
  ["/admin/shipments", "Admin shipment queue route"],
  ["/admin/shipments/123", "Admin shipment detail route"],
  ["/admin/transactions", "Admin transaction route"],
  ["/admin/gem-listings", "Admin gem listings route"],
])("keeps %s available to Admin users", (path, content) => {
  render(<MemoryRouter initialEntries={[path]}><App /></MemoryRouter>);
  expect(screen.getByText(content)).toBeInTheDocument();
  if (path.startsWith("/admin/shipments")) {
    expect(screen.getByRole("navigation", { name: "Admin navigation" })).toBeInTheDocument();
  }
});
