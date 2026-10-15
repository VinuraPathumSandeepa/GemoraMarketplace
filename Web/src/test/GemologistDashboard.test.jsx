import { render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, expect, it, vi } from "vitest";
import GemologistDashboard from "../pages/GemologistDashboard";
import api from "../services/api";

vi.mock("../layouts/DashboardLayout", () => ({
  default: ({ children }) => <div>{children}</div>,
}));

vi.mock("../services/api", () => ({
  default: { get: vi.fn() },
  resolveApiAssetUrl: (url) => url || null,
}));

beforeEach(() => {
  vi.clearAllMocks();
  api.get.mockResolvedValue({ data: [] });
});

it("keeps the verification workspace purpose and queue route visible", async () => {
  render(<MemoryRouter><GemologistDashboard /></MemoryRouter>);

  expect(screen.getByRole("heading", { name: "Gemstone Verification" })).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Open Verification Queue" }))
    .toHaveAttribute("href", "/gemologist/verifications");
  expect(await screen.findByText("Queue is clear")).toBeInTheDocument();
  expect(api.get).toHaveBeenCalledWith("/GemVerifications/pending");
});

it("calculates the four workload cards from the pending API records", async () => {
  api.get.mockResolvedValue({ data: [
    { verificationId: 1, title: "Blue Sapphire", sellerName: "Seller A", aiStatus: "Completed",
      primaryImageUrl: "/image-a.jpg", certificateUrl: "/certificate-a.pdf" },
    { verificationId: 2, title: "Ruby", sellerName: "Seller B", aiStatus: "NotStarted",
      primaryImageUrl: "/image-b.jpg", certificateUrl: "/certificate-b.pdf" },
    { verificationId: 3, title: "Garnet", sellerName: "Seller C", aiStatus: "Failed",
      primaryImageUrl: null, certificateUrl: null },
  ] });

  render(<MemoryRouter><GemologistDashboard /></MemoryRouter>);
  await screen.findByText("Blue Sapphire");

  const countFor = (label) => within(screen.getByText(label, { selector: ".gemologist-stat-label" }).closest("article"));
  expect(countFor("Pending Reviews").getByText("3")).toBeInTheDocument();
  expect(countFor("AI Completed").getByText("1")).toBeInTheDocument();
  expect(countFor("AI Not Started").getByText("1")).toBeInTheDocument();
  expect(countFor("Attention Required").getByText("1")).toBeInTheDocument();
});

it("shows real review details, evidence issues, and the human decision boundary", async () => {
  api.get.mockResolvedValue({ data: [
    { verificationId: 12, title: "Blue Sapphire", gemType: "Sapphire", sellerName: "Maya",
      caratWeight: 1.25, createdAt: "2026-10-10T09:00:00Z", aiStatus: "Completed",
      primaryImageUrl: "/images/sapphire.jpg", certificateUrl: "/certificates/sapphire.pdf" },
    { verificationId: 13, title: "Ruby", sellerName: "Nimal", aiStatus: "Failed",
      primaryImageUrl: null, certificateUrl: null },
  ] });

  render(<MemoryRouter><GemologistDashboard /></MemoryRouter>);
  const sapphire = (await screen.findByText("Blue Sapphire")).closest(".gemologist-preview-item");
  expect(within(sapphire).getByRole("img", { name: "Blue Sapphire" }))
    .toHaveAttribute("src", "/images/sapphire.jpg");
  expect(within(sapphire).getByText("Maya")).toBeInTheDocument();
  expect(within(sapphire).getByText("1.25 ct")).toBeInTheDocument();
  expect(within(sapphire).getByText("AI Completed")).toBeInTheDocument();
  expect(within(sapphire).getByRole("link", { name: "Open Review" }))
    .toHaveAttribute("href", "/gemologist/verifications/12");

  const issue = screen.getByText("Gemstone image missing").closest(".gemologist-attention-item");
  expect(within(issue).getByText("Certificate file missing")).toBeInTheDocument();
  expect(within(issue).getByText("Previous AI execution failed")).toBeInTheDocument();
  expect(within(issue).getByText("AI Failed")).toBeInTheDocument();
  expect(screen.getByText("AI assists. Humans decide.")).toBeInTheDocument();
});
