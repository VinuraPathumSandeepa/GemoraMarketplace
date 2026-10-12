import { fireEvent, render, screen, within } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { beforeEach, expect, it, vi } from "vitest";
import AdminLayout from "../layouts/AdminLayout";
import RoleProtectedRoute from "../routes/RoleProtectedRoute";
import { useAuth } from "../context/AuthContext";

vi.mock("../context/AuthContext", () => ({ useAuth: vi.fn() }));

function Path() {
  return <output data-testid="path">{useLocation().pathname}</output>;
}

beforeEach(() => {
  useAuth.mockReturnValue({
    user: { role: "Admin", fullName: "Gemora Admin", email: "admin@example.test" },
    isAuthenticated: true, loading: false, logout: vi.fn(),
  });
});

it("shows real Admin routes in the desktop sidebar and highlights the active route", () => {
  const { container } = render(<MemoryRouter initialEntries={["/admin/shipments/42"]}>
    <Path /><AdminLayout title="Shipment details"><p>Shipment detail content</p></AdminLayout>
  </MemoryRouter>);

  const desktop = within(container.querySelector(".admin-sidebar-desktop"));
  expect(desktop.getAllByRole("link").filter((link) => link.closest("nav")).map((link) => link.textContent))
    .toEqual(["Overview", "Shipment Dashboard", "Transactions", "Manage Gem Listings"]);
  expect(desktop.getByRole("link", { name: "Shipment Dashboard" })).toHaveClass("active");
  expect(desktop.getByRole("link", { name: "Admin profile" })).toHaveAttribute("href", "/profile");
  expect(screen.getByText("Shipment detail content")).toBeInTheDocument();

  fireEvent.click(desktop.getByRole("link", { name: "Transactions" }));
  expect(screen.getByTestId("path")).toHaveTextContent("/admin/transactions");
  expect(desktop.getByRole("link", { name: "Transactions" })).toHaveClass("active");
});

it("opens and closes the mobile drawer by link, close button, overlay, and Escape", () => {
  render(<MemoryRouter initialEntries={["/admin"]}><Path /><AdminLayout><p>Overview</p></AdminLayout></MemoryRouter>);

  const open = () => fireEvent.click(screen.getByRole("button", { name: "Open navigation" }));
  open();
  let drawer = screen.getByRole("dialog", { name: "Admin navigation menu" });
  expect(document.querySelector(".admin-portal-workspace")).toHaveAttribute("inert");
  expect(within(drawer).getByRole("link", { name: "Manage Gem Listings" })).toHaveAttribute("href", "/admin/gem-listings");
  fireEvent.click(within(drawer).getByRole("link", { name: "Manage Gem Listings" }));
  expect(screen.getByTestId("path")).toHaveTextContent("/admin/gem-listings");
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();

  open();
  drawer = screen.getByRole("dialog");
  fireEvent.click(within(drawer).getByRole("button", { name: "Close navigation" }));
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();

  open();
  fireEvent.keyDown(window, { key: "Escape" });
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();

  open();
  fireEvent.click(document.querySelector(".admin-mobile-backdrop"));
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
});

it("keeps logout behavior and sends unauthorized roles away from protected Admin routes", () => {
  const logout = vi.fn();
  useAuth.mockReturnValue({ user: { role: "Admin" }, isAuthenticated: true, loading: false, logout });
  const { container } = render(<MemoryRouter initialEntries={["/admin"]}>
    <Path /><AdminLayout><p>Overview</p></AdminLayout>
  </MemoryRouter>);
  fireEvent.click(within(container.querySelector(".admin-sidebar-desktop")).getByRole("button", { name: "Log out" }));
  expect(logout).toHaveBeenCalledTimes(1);
  expect(screen.getByTestId("path")).toHaveTextContent("/login");
});

it.each(["Seller", "Buyer", "Gemologist"])("does not render the Admin portal for a %s", (role) => {
  useAuth.mockReturnValue({ user: { role }, isAuthenticated: true, loading: false });
  render(<MemoryRouter initialEntries={["/admin"]}><Path /><Routes>
    <Route path="/admin" element={<RoleProtectedRoute allowedRoles={["Admin"]}><AdminLayout><p>Private Admin content</p></AdminLayout></RoleProtectedRoute>} />
    <Route path="/dashboard" element={<p>Role dashboard</p>} />
  </Routes></MemoryRouter>);
  expect(screen.queryByText("Private Admin content")).not.toBeInTheDocument();
  expect(screen.getByText("Role dashboard")).toBeInTheDocument();
  expect(screen.getByTestId("path")).toHaveTextContent("/dashboard");
});

it("redirects signed-out visitors from Admin pages to login", () => {
  useAuth.mockReturnValue({ user: null, isAuthenticated: false, loading: false });
  render(<MemoryRouter initialEntries={["/admin"]}><Path /><Routes>
    <Route path="/admin" element={<RoleProtectedRoute allowedRoles={["Admin"]}><AdminLayout><p>Private Admin content</p></AdminLayout></RoleProtectedRoute>} />
    <Route path="/login" element={<p>Login page</p>} />
  </Routes></MemoryRouter>);
  expect(screen.getByText("Login page")).toBeInTheDocument();
  expect(screen.queryByText("Private Admin content")).not.toBeInTheDocument();
  expect(screen.getByTestId("path")).toHaveTextContent("/login");
});
