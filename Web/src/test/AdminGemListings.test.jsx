import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import AdminGemListings from "../pages/admin/AdminGemListings";
import adminGemListingService from "../services/adminGemListingService";

vi.mock("../layouts/DashboardLayout", () => ({ default: ({ children }) => <div>{children}</div> }));
vi.mock("../services/adminGemListingService", () => ({
  default: { getAll: vi.fn(), remove: vi.fn() },
}));

afterEach(() => vi.restoreAllMocks());

it("shows a sold gem and removes it after admin confirmation", async () => {
  adminGemListingService.getAll.mockResolvedValue([{
    id: 11, title: "Blue Sapphire", gemType: "Sapphire", sellerName: "Gem Seller",
    status: "Sold", price: 1000, currency: "LKR", primaryImageUrl: null,
  }]);
  adminGemListingService.remove.mockResolvedValue();
  vi.spyOn(window, "confirm").mockReturnValue(true);

  render(<AdminGemListings />);
  expect(await screen.findByText("Blue Sapphire")).toBeInTheDocument();
  expect(screen.getByText("Sold")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Delete Blue Sapphire" }));
  await waitFor(() => expect(adminGemListingService.remove).toHaveBeenCalledWith(11));
  await waitFor(() => expect(screen.queryByText("Blue Sapphire")).not.toBeInTheDocument());
});
