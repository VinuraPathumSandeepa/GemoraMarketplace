import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import TransactionDashboard from "../pages/admin/TransactionDashboard";
import transactionService from "../services/marketplace/transactionService";
import { useAuth } from "../context/AuthContext";

vi.mock("../context/AuthContext", () => ({ useAuth: vi.fn() }));
vi.mock("../services/marketplace/transactionService", () => ({ default: { getAll: vi.fn() } }));

beforeEach(() => {
  useAuth.mockReturnValue({ user: { role: "Admin", fullName: "Gemora Admin" }, logout: vi.fn() });
  transactionService.getAll.mockResolvedValue([{ id: "1", orderNumber: "GEM-0001", gemTitle: "Blue Sapphire", buyerName: "A Buyer", sellerName: "A Seller", currency: "LKR", agreedPrice: 1200, status: "Paid", createdAt: "2026-09-12T08:00:00Z" }]);
});
afterEach(() => vi.clearAllMocks());

it("keeps transaction data and search working inside the Admin shell", async () => {
  render(<MemoryRouter initialEntries={["/admin/transactions"]}><TransactionDashboard /></MemoryRouter>);
  expect(await screen.findByText("GEM-0001")).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Transactions" })).toHaveClass("active");
  fireEvent.change(screen.getByRole("textbox", { name: "Search transactions" }), { target: { value: "unknown" } });
  expect(screen.getByText("No transactions found.")).toBeInTheDocument();
  fireEvent.change(screen.getByRole("textbox", { name: "Search transactions" }), { target: { value: "sapphire" } });
  expect(screen.getByText("GEM-0001")).toBeInTheDocument();
});

it("shows an error with a working retry", async () => {
  transactionService.getAll.mockRejectedValueOnce(new Error("network")).mockResolvedValueOnce([]);
  render(<MemoryRouter initialEntries={["/admin/transactions"]}><TransactionDashboard /></MemoryRouter>);
  expect(await screen.findByRole("alert")).toHaveTextContent("Unable to load transactions.");
  fireEvent.click(screen.getByRole("button", { name: "Retry" }));
  await waitFor(() => expect(screen.getByText("No transactions found.")).toBeInTheDocument());
});
