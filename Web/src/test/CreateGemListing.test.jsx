import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import CreateGemListing from "../pages/seller/CreateGemListing";
import gemListingService from "../services/gemVerification/gemListingService";

vi.mock("../layouts/DashboardLayout", () => ({
  default: ({ children }) => <div>{children}</div>,
}));
vi.mock("../services/gemVerification/gemListingService", () => ({
  default: { createListing: vi.fn(), uploadImage: vi.fn(), uploadCertificate: vi.fn() },
}));

function CurrentPath() {
  const location = useLocation();
  return <output data-testid="current-path">{location.pathname}</output>;
}

function renderCreate() {
  return render(
    <MemoryRouter initialEntries={["/seller/listings/create"]}>
      <CurrentPath />
      <Routes>
        <Route path="/seller/listings/create" element={<CreateGemListing />} />
        <Route path="/seller/listings" element={<p>Listings page</p>} />
      </Routes>
    </MemoryRouter>
  );
}

beforeEach(() => {
  vi.stubGlobal("URL", {
    ...URL,
    createObjectURL: vi.fn(() => "blob:gem-preview"),
    revokeObjectURL: vi.fn(),
  });
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.clearAllMocks();
});

it("removes and replaces only the gemstone image without submitting or navigating", async () => {
  const { container } = renderCreate();
  const title = screen.getByPlaceholderText("e.g. Natural Ceylon Blue Sapphire");
  fireEvent.change(title, { target: { value: "Saved title" } });
  const imageInput = container.querySelector('input[type="file"][accept*="webp"]');
  fireEvent.change(imageInput, {
    target: { files: [new File(["first"], "first.jpg", { type: "image/jpeg" })] },
  });
  expect(await screen.findByAltText("Selected gemstone preview")).toBeInTheDocument();
  fireEvent.change(container.querySelector('input[type="file"][accept*="pdf"]'), {
    target: { files: [new File(["pdf"], "report.pdf", { type: "application/pdf" })] },
  });

  fireEvent.click(screen.getAllByRole("button", { name: "Remove" })[0]);

  expect(screen.queryByAltText("Selected gemstone preview")).not.toBeInTheDocument();
  expect(screen.getByText("Choose gemstone image")).toBeInTheDocument();
  expect(screen.getByText("report.pdf")).toBeInTheDocument();
  expect(title).toHaveValue("Saved title");
  expect(screen.getByTestId("current-path")).toHaveTextContent("/seller/listings/create");
  expect(gemListingService.createListing).not.toHaveBeenCalled();

  fireEvent.change(container.querySelector('input[type="file"][accept*="webp"]'), {
    target: { files: [new File(["second"], "second.jpg", { type: "image/jpeg" })] },
  });
  await waitFor(() => expect(screen.getByText("second.jpg")).toBeInTheDocument());
});

it("removes only the certificate and restores its upload area", async () => {
  const { container } = renderCreate();
  const title = screen.getByPlaceholderText("e.g. Natural Ceylon Blue Sapphire");
  fireEvent.change(title, { target: { value: "Saved title" } });
  const certificateInput = container.querySelector('input[type="file"][accept*="pdf"]');
  fireEvent.change(certificateInput, {
    target: { files: [new File(["pdf"], "report.pdf", { type: "application/pdf" })] },
  });
  expect(screen.getByText("report.pdf")).toBeInTheDocument();
  fireEvent.change(container.querySelector('input[type="file"][accept*="webp"]'), {
    target: { files: [new File(["image"], "gem.jpg", { type: "image/jpeg" })] },
  });
  expect(await screen.findByAltText("Selected gemstone preview")).toBeInTheDocument();

  fireEvent.click(screen.getAllByRole("button", { name: "Remove" })[1]);

  expect(screen.queryByText("report.pdf")).not.toBeInTheDocument();
  expect(screen.getByText("Choose certificate")).toBeInTheDocument();
  expect(screen.getByAltText("Selected gemstone preview")).toBeInTheDocument();
  expect(title).toHaveValue("Saved title");
  expect(screen.getByTestId("current-path")).toHaveTextContent("/seller/listings/create");
  expect(gemListingService.createListing).not.toHaveBeenCalled();
});
