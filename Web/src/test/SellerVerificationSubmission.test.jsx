import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import GemListingDetails from "../pages/seller/GemListingDetails";
import MyGemListings from "../pages/seller/MyGemListings";
import gemListingService from "../services/gemVerification/gemListingService";

vi.mock("../layouts/DashboardLayout", () => ({
  default: ({ children }) => <div>{children}</div>,
}));
vi.mock("../services/gemVerification/gemListingService", () => ({
  default: {
    getListingById: vi.fn(),
    getMyListings: vi.fn(),
    submitListing: vi.fn(),
  },
}));

const listing = {
  id: 69,
  title: "Blue Sapphire",
  gemType: "Sapphire",
  status: "Draft",
  price: 1000,
  primaryImageUrl: "/uploads/gem-images/sapphire.jpg",
  certificateUrl: "/uploads/certificates/sapphire.pdf",
};

function renderDetails() {
  return render(
    <MemoryRouter initialEntries={["/seller/listings/69"]}>
      <Routes>
        <Route path="/seller/listings/:id" element={<GemListingDetails />} />
        <Route path="/seller/listings" element={<MyGemListings />} />
      </Routes>
    </MemoryRouter>
  );
}

async function openConfirmation() {
  fireEvent.click(await screen.findByRole("button", { name: /Submit for Verification/ }));
  return screen.getByRole("dialog");
}

beforeEach(() => {
  gemListingService.getListingById.mockResolvedValue(listing);
  vi.spyOn(console, "error").mockImplementation(() => {});
});

afterEach(() => {
  vi.clearAllMocks();
  vi.restoreAllMocks();
});

it("renders the confirmation, submits once, and shows Pending Review after confirmation", async () => {
  gemListingService.submitListing.mockResolvedValue({
    ...listing,
    status: "PendingVerification",
  });
  gemListingService.getMyListings.mockResolvedValue([
    { ...listing, status: "PendingVerification" },
  ]);
  renderDetails();

  const dialog = await openConfirmation();
  expect(within(dialog).getByText("Ready to Submit")).toBeInTheDocument();
  expect(within(dialog).getByText("Supporting evidence attached")).toBeInTheDocument();
  fireEvent.click(within(dialog).getByRole("button", { name: /Submit Listing/ }));

  expect(await screen.findByText(/Listing submitted successfully/)).toBeInTheDocument();
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  expect(screen.getAllByText("Pending Review").length).toBeGreaterThan(0);
  expect(gemListingService.submitListing).toHaveBeenCalledExactlyOnceWith("69");

  fireEvent.click(screen.getByRole("button", { name: /My Gem Listings/ }));
  expect(await screen.findByRole("heading", { name: "Blue Sapphire" })).toBeInTheDocument();
  expect(screen.getAllByText("Pending Review").length).toBeGreaterThan(0);
  expect(gemListingService.getMyListings).toHaveBeenCalledTimes(1);
});

it("disables submission when the gemstone image is missing", async () => {
  gemListingService.getListingById.mockResolvedValueOnce({
    ...listing,
    primaryImageUrl: null,
    certificateUrl: null,
  });
  renderDetails();
  const dialog = await openConfirmation();
  expect(within(dialog).getByText("Required image evidence missing")).toBeInTheDocument();
  expect(within(dialog).getByRole("button", { name: /Submit Listing/ })).toBeDisabled();
  expect(gemListingService.submitListing).not.toHaveBeenCalled();

});

it("allows submission without an optional certificate", async () => {
  gemListingService.getListingById.mockResolvedValue({ ...listing, certificateUrl: null });
  gemListingService.submitListing.mockResolvedValue({
    ...listing,
    certificateUrl: null,
    status: "PendingVerification",
  });
  renderDetails();
  const dialog = await openConfirmation();
  expect(within(dialog).getByText("Optional evidence not attached")).toBeInTheDocument();
  fireEvent.click(within(dialog).getByRole("button", { name: /Submit Listing/ }));
  expect(await screen.findByText(/Listing submitted successfully/)).toBeInTheDocument();
});

it("blocks double clicks while the request is pending", async () => {
  let resolveRequest;
  gemListingService.submitListing.mockReturnValue(new Promise((resolve) => {
    resolveRequest = resolve;
  }));
  renderDetails();

  const dialog = await openConfirmation();
  const button = within(dialog).getByRole("button", { name: /Submit Listing/ });
  fireEvent.click(button);
  fireEvent.click(button);
  expect(gemListingService.submitListing).toHaveBeenCalledTimes(1);
  expect(within(dialog).getByRole("button", { name: /Submitting to Gemologist/ })).toBeDisabled();
  expect(screen.queryByText(/Listing submitted successfully/)).not.toBeInTheDocument();
  resolveRequest({ ...listing, status: "PendingVerification" });
  await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
});

it.each([
  [400, "Please review the listing details and try again."],
  [401, "Your session has expired. Please sign in again."],
  [403, "You do not have permission to submit this listing."],
  [404, "This listing could not be found. Return to My Listings and try again."],
  [409, "This listing can no longer be submitted. Refresh it to check its current status."],
  [500, "Unable to submit your listing. Please try again."],
])("shows a safe message for HTTP %i and keeps the modal open", async (status, expected) => {
  gemListingService.submitListing.mockRejectedValue({ response: { status } });
  renderDetails();
  const dialog = await openConfirmation();
  fireEvent.click(within(dialog).getByRole("button", { name: /Submit Listing/ }));
  expect(await within(dialog).findByText(expected)).toBeInTheDocument();
  expect(within(dialog).getByRole("button", { name: /Submit Listing/ })).toBeEnabled();
});

it("shows the backend image rule when evidence is missing", async () => {
  gemListingService.submitListing.mockRejectedValue({
    response: { status: 409, data: { message: "A gemstone image is required before submission." } },
  });
  renderDetails();
  const dialog = await openConfirmation();
  fireEvent.click(within(dialog).getByRole("button", { name: /Submit Listing/ }));
  expect(await within(dialog).findByText("A gemstone image is required before submission.")).toBeInTheDocument();
});

it("handles network failures without showing raw JavaScript errors", async () => {
  gemListingService.submitListing.mockRejectedValue({ code: "ERR_NETWORK", message: "api is not defined" });
  renderDetails();
  const dialog = await openConfirmation();
  fireEvent.click(within(dialog).getByRole("button", { name: /Submit Listing/ }));
  expect(await within(dialog).findByText("Unable to reach the server. Check your connection and try again.")).toBeInTheDocument();
  expect(within(dialog).queryByText(/api is not defined/)).not.toBeInTheDocument();
});
