import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi, beforeEach } from "vitest";
import ExportOfficerDashboard from "../../pages/ExportOfficerDashboard";
import * as exportOfficerService from "../../services/exportOfficerService";
import * as AuthContextModule from "../../context/AuthContext";

vi.mock("../../context/AuthContext", () => ({
  useAuth: vi.fn(),
}));

vi.mock("../../services/exportOfficerService", () => ({
  getReviewQueue: vi.fn(),
  getRequestDetail: vi.fn(),
  startReview: vi.fn(),
  makeDecision: vi.fn(),
  downloadComplianceDocument: vi.fn(),
}));

describe("ExportOfficerDashboard Component", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(AuthContextModule, "useAuth").mockReturnValue({
      user: { fullName: "Officer User", role: "ExportOfficer" },
    });
  });

  it("renders loading state initially while fetching review queue", () => {
    exportOfficerService.getReviewQueue.mockReturnValue(new Promise(() => {}));

    render(
      <MemoryRouter>
        <ExportOfficerDashboard />
      </MemoryRouter>
    );

    expect(screen.getByText(/Export Compliance/i)).toBeInTheDocument();
    expect(screen.getByText(/Loading export requests/i)).toBeInTheDocument();
  });

  it("renders review queue list with export requests", async () => {
    const mockQueue = [
      {
        id: "req-101",
        requesterName: "seller2",
        requesterEmail: "seller2@example.com",
        originCountry: "Sri Lanka",
        destinationCountry: "United States",
        declaredValue: 2000,
        currency: "USD",
        status: "UnderOfficerReview",
        statusName: "UnderOfficerReview",
        submittedAt: "2026-10-04T10:00:00Z",
        documents: [{}],
      },
    ];

    exportOfficerService.getReviewQueue.mockResolvedValue({
      requests: mockQueue,
      totalCount: 1,
    });

    render(
      <MemoryRouter>
        <ExportOfficerDashboard />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText("seller2")).toBeInTheDocument();
    });

    expect(screen.getByText(/2,000 USD/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Review" })).toBeInTheDocument();
  });

  it("renders empty state message when no requests match", async () => {
    exportOfficerService.getReviewQueue.mockResolvedValue({
      requests: [],
      totalCount: 0,
    });

    render(
      <MemoryRouter>
        <ExportOfficerDashboard />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(
        screen.getByText(/No export requests are currently waiting for review/i)
      ).toBeInTheDocument();
    });
  });

  it("renders permission error when API returns 403 Forbidden", async () => {
    const error403 = new Error("Forbidden");
    error403.response = { status: 403 };
    exportOfficerService.getReviewQueue.mockRejectedValue(error403);

    render(
      <MemoryRouter>
        <ExportOfficerDashboard />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(
        screen.getByText(/You don't have permission to view export compliance reviews/i)
      ).toBeInTheDocument();
    });
  });

  it("loads request details and renders AI compliance assessment", async () => {
    const mockQueueItem = {
      id: "req-200",
      requesterName: "seller1",
      status: "UnderOfficerReview",
      statusName: "UnderOfficerReview",
      declaredValue: 5000,
      currency: "USD",
      documents: [{}],
    };

    const mockDetail = {
      id: "req-200",
      status: "UnderOfficerReview",
      statusName: "UnderOfficerReview",
      requesterName: "seller1",
      requesterEmail: "seller1@example.com",
      originCountry: "Sri Lanka",
      destinationCountry: "Germany",
      gemDescription: "Blue Sapphire 3.5cts",
      declaredValue: 5000,
      currency: "USD",
      agentWorkflow: {
        assessment: {
          summary: "AI Verified export documentation complete.",
          deterministicComplete: true,
          confidence: 0.95,
          documentFindings: [],
          recommendedOfficerChecks: ["Verify physical seal"],
        },
        steps: [],
      },
      documents: [
        {
          id: "doc-55",
          documentType: "Export License",
          fileUrl: "/api/ExportRequests/req-200/documents/doc-55/file",
          documentNumber: "LIC-8899",
          status: "Valid",
        },
      ],
    };

    exportOfficerService.getReviewQueue.mockResolvedValue({
      requests: [mockQueueItem],
    });
    exportOfficerService.getRequestDetail.mockResolvedValue({ request: mockDetail });

    render(
      <MemoryRouter>
        <ExportOfficerDashboard />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Review" })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole("button", { name: "Review" }));

    await waitFor(() => {
      expect(screen.getByText("AI Verified export documentation complete.")).toBeInTheDocument();
    });

    expect(screen.getByText("Sri Lanka")).toBeInTheDocument();
    expect(screen.getByText("Germany")).toBeInTheDocument();
    expect(screen.getByText("Export License")).toBeInTheDocument();
  });

  it("triggers secure document download API call when download button clicked", async () => {
    const mockDetail = {
      id: "req-300",
      status: "UnderOfficerReview",
      statusName: "UnderOfficerReview",
      documents: [
        {
          id: "doc-99",
          documentType: "CITES Certificate",
          fileUrl: "/api/ExportRequests/req-300/documents/doc-99/file",
          fileSizeBytes: 1024,
        },
      ],
    };

    exportOfficerService.getReviewQueue.mockResolvedValue({
      requests: [{ id: "req-300", status: "UnderOfficerReview", statusName: "UnderOfficerReview", documents: [{}] }],
    });
    exportOfficerService.getRequestDetail.mockResolvedValue({ request: mockDetail });
    exportOfficerService.downloadComplianceDocument.mockResolvedValue(new Blob(["fake-pdf"]));

    render(
      <MemoryRouter>
        <ExportOfficerDashboard />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Review" })).toBeInTheDocument();
    });
    fireEvent.click(screen.getByRole("button", { name: "Review" }));

    await waitFor(() => {
      expect(screen.getByText("CITES Certificate")).toBeInTheDocument();
    });

    const downloadBtn = screen.getByRole("button", { name: /Download/i });
    fireEvent.click(downloadBtn);

    await waitFor(() => {
      expect(exportOfficerService.downloadComplianceDocument).toHaveBeenCalledWith(
        "req-300",
        "doc-99"
      );
    });
  });

  it("executes Approve decision flow and refreshes data on confirmation", async () => {
    const mockDetail = {
      id: "req-400",
      status: "UnderOfficerReview",
      statusName: "UnderOfficerReview",
    };

    exportOfficerService.getReviewQueue.mockResolvedValue({
      requests: [{ id: "req-400", status: "UnderOfficerReview", statusName: "UnderOfficerReview", documents: [{}] }],
    });
    exportOfficerService.getRequestDetail.mockResolvedValue({ request: mockDetail });
    exportOfficerService.makeDecision.mockResolvedValue({ success: true });

    render(
      <MemoryRouter>
        <ExportOfficerDashboard />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Review" })).toBeInTheDocument();
    });
    fireEvent.click(screen.getByRole("button", { name: "Review" }));

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Approve" })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole("button", { name: "Approve" }));
    expect(screen.getByRole("button", { name: "Approve Request" })).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Approve Request" }));

    await waitFor(() => {
      expect(exportOfficerService.makeDecision).toHaveBeenCalledWith("req-400", "Approve", "");
    });
  });
});
