import { act, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, expect, it, vi } from "vitest";
import VerificationDetails from "../pages/gemologist/VerificationDetails";
import api from "../services/api";
import { openProtectedCertificate } from "../services/certificateAccess";

vi.mock("../layouts/DashboardLayout", () => ({ default: ({ children }) => <div>{children}</div> }));
vi.mock("../pages/gemologist/GemVerificationAgentPanel", () => ({
  default: ({ onRunAnalysis }) => <button type="button" onClick={onRunAnalysis}>Run test agent</button>,
}));
vi.mock("../services/api", () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn() },
  resolveApiAssetUrl: (url) => url ? `https://example.test${url}` : null,
}));
vi.mock("../services/certificateAccess", () => ({ openProtectedCertificate: vi.fn() }));

const verification = {
  verificationId: 12, gemListingId: 34, title: "Blue Sapphire", sellerName: "Seller",
  decision: "Pending", listingStatus: "UnderReview", primaryImageUrl: "/uploads/sapphire.jpg",
  certificateUrl: "supabase-private://certificate", certificateAuthority: "Lab",
};

beforeEach(() => {
  vi.clearAllMocks();
  api.get.mockResolvedValue({ data: verification });
  openProtectedCertificate.mockResolvedValue(undefined);
});

function renderDetails() {
  return render(<MemoryRouter initialEntries={["/gemologist/verifications/12"]}>
    <Routes><Route path="/gemologist/verifications/:id" element={<VerificationDetails />} /></Routes>
  </MemoryRouter>);
}

it("shows the complete image preview and preserves Open Original and certificate access", async () => {
  const { container } = renderDetails();
  const image = await screen.findByRole("img", { name: "Blue Sapphire" });
  expect(image.closest(".verification-large-image")).toBeInTheDocument();
  expect(image).toHaveAttribute("src", "https://example.test/uploads/sapphire.jpg");
  expect(screen.getByRole("link", { name: /Open Original/ }))
    .toHaveAttribute("href", "https://example.test/uploads/sapphire.jpg");
  fireEvent.click(screen.getByRole("link", { name: /Open Certificate/ }));
  expect(openProtectedCertificate).toHaveBeenCalledWith(34);
  expect(container.querySelector(".verification-certificate-grid")).toHaveTextContent("Available");
});

it("shows a useful fallback when no gemstone image was supplied", async () => {
  api.get.mockResolvedValue({ data: { ...verification, primaryImageUrl: null } });
  renderDetails();
  expect(await screen.findByText("No gemstone image supplied")).toBeInTheDocument();
  expect(screen.queryByRole("link", { name: /Open Original/ })).not.toBeInTheDocument();
});

it("replaces a failed image with the existing unavailable state", async () => {
  renderDetails();
  fireEvent.error(await screen.findByRole("img", { name: "Blue Sapphire" }));
  expect(screen.getByText("Gemstone image unavailable")).toBeInTheDocument();
  expect(screen.queryByRole("link", { name: /Open Original/ })).not.toBeInTheDocument();
});

it("sends only one AI request after rapid repeated clicks", async () => {
  let finishAnalysis;
  api.post.mockImplementation(() => new Promise((resolve) => { finishAnalysis = resolve; }));
  renderDetails();
  const run = await screen.findByRole("button", { name: "Run test agent" });
  fireEvent.click(run);
  fireEvent.click(run);
  expect(api.post).toHaveBeenCalledTimes(1);
  expect(api.post).toHaveBeenCalledWith("/GemVerifications/12/ai-analysis");
  await act(async () => finishAnalysis({ data: { status: "Completed" } }));
});
