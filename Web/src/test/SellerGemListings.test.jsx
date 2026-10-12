import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, expect, it, vi } from "vitest";
import MyGemListings from "../pages/seller/MyGemListings";
import GemListingDetails from "../pages/seller/GemListingDetails";
import EditGemListing from "../pages/seller/EditGemListing";
import gemListingService from "../services/gemVerification/gemListingService";
import { resolveApiAssetUrl } from "../services/api";

vi.mock("../layouts/DashboardLayout", () => ({
  default: ({ children }) => <div>{children}</div>,
}));
vi.mock("../services/gemVerification/gemListingService", () => ({
  default: { getMyListings: vi.fn(), getListingById: vi.fn() },
}));

afterEach(() => vi.clearAllMocks());

const listing = {
  id: 17,
  title: "Blue Sapphire",
  status: "Draft",
  gemType: "Sapphire",
  price: 1000,
  primaryImageUrl: "/uploads/gem-images/sapphire.jpg",
  certificateUrl: null,
};

it("renders My Listings with relative, absolute, and missing image URLs", async () => {
  const supabaseUrl = "https://example.supabase.co/storage/v1/object/public/gem-images/ruby.jpg";
  gemListingService.getMyListings.mockResolvedValue([
    listing,
    { ...listing, id: 18, title: "Ruby", primaryImageUrl: supabaseUrl },
    { ...listing, id: 19, title: "No Image", primaryImageUrl: null },
  ]);

  render(<MemoryRouter><MyGemListings /></MemoryRouter>);

  expect(await screen.findByRole("heading", { name: "Blue Sapphire" })).toBeInTheDocument();
  expect(screen.getByRole("img", { name: "Blue Sapphire" }))
    .toHaveAttribute("src", resolveApiAssetUrl(listing.primaryImageUrl));
  expect(screen.getByRole("img", { name: "Ruby" })).toHaveAttribute("src", supabaseUrl);
  expect(screen.getByText("No gemstone image")).toBeInTheDocument();
});

it.each([
  ["/seller/listings/17", GemListingDetails],
  ["/seller/listings/17/edit", EditGemListing],
])("renders %s with an existing gemstone image", async (path, Page) => {
  gemListingService.getListingById.mockResolvedValue(listing);

  render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path={path} element={<Page />} />
      </Routes>
    </MemoryRouter>
  );

  expect(await screen.findByRole("img", { name: "Blue Sapphire" }))
    .toHaveAttribute("src", resolveApiAssetUrl(listing.primaryImageUrl));
});
