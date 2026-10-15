import { render, screen } from "@testing-library/react";
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
