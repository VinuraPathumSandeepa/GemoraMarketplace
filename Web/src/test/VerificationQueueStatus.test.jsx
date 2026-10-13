import { render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, expect, it, vi } from "vitest";
import VerificationQueue from "../pages/gemologist/VerificationQueue";
import api from "../services/api";

vi.mock("../layouts/DashboardLayout", () => ({
  default: ({ children }) => <div>{children}</div>,
}));

vi.mock("../services/api", () => ({
  default: { get: vi.fn() },
  resolveApiAssetUrl: () => null,
}));

beforeEach(() => vi.clearAllMocks());

it("shows model confidence only for completed analyses in the queue", async () => {
  api.get.mockResolvedValue({ data: [
    { verificationId: 1, title: "Failed sample", decision: "Pending",
      aiStatus: "Failed", aiConfidenceScore: 0.85 },
    { verificationId: 2, title: "Completed sample", decision: "Pending",
      aiStatus: "Completed", aiConfidenceScore: 0.85 },
  ] });

  render(<MemoryRouter><VerificationQueue /></MemoryRouter>);

  const failed = (await screen.findByText("Failed sample")).closest(".verification-queue-card");
  const completed = screen.getByText("Completed sample").closest(".verification-queue-card");
  expect(within(failed).getByText("Failed")).toBeInTheDocument();
  expect(within(failed).queryByText("85%")).not.toBeInTheDocument();
  expect(within(completed).getByText("Model score")).toBeInTheDocument();
  expect(within(completed).getByText("85%")).toBeInTheDocument();
});
