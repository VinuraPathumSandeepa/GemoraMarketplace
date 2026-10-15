import { fireEvent, render, screen, within } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import GemVerificationAgentPanel from "../pages/gemologist/GemVerificationAgentPanel";

const verification = { decision: "Pending", aiStatus: "Failed" };
const steps = [
  ["load", "Completed"], ["state", "Completed"],
  ["validation", "Completed"], ["image", "Skipped"],
  ["ai", "Failed"], ["findings", "Skipped"], ["review", "Skipped"],
].map(([id, status]) => ({ id, status }));

it("shows backend failure and skipped steps without inventing completions", () => {
  const onRunAnalysis = vi.fn();
  const { container } = render(<GemVerificationAgentPanel
    verification={verification}
    aiResult={{ status: "Failed", findings: "Provider rate limited", workflowSteps: steps,
      stepsCompleted: Array(10).fill("audit message"), confidenceScore: 0.85 }}
    analyzing={false}
    onRunAnalysis={onRunAnalysis}
  />);

  const timeline = container.querySelector(".agent-timeline");
  expect(timeline).not.toBeNull();
  expect(within(timeline).getByText("Run AI analysis").closest(".agent-timeline-step"))
    .toHaveClass("agent-step-failed");
  expect(within(timeline).getByText("Validate submitted evidence").closest(".agent-timeline-step"))
    .toHaveClass("agent-step-completed");
  expect(within(timeline).getByText("Prepare structured findings").closest(".agent-timeline-step"))
    .toHaveClass("agent-step-skipped");
  expect(screen.getByText("Provider rate limited")).toBeInTheDocument();
  expect(screen.queryByText("85%")).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: /Retry Agent Analysis/ }));
  expect(onRunAnalysis).toHaveBeenCalledTimes(1);
});

it("labels completed confidence as advisory even when risk flags exist", () => {
  render(<GemVerificationAgentPanel
    verification={{ decision: "Pending", aiStatus: "Completed" }}
    aiResult={{ status: "Completed", confidenceScore: 0.85,
      findings: "Declared blue color conflicts with the yellow image.",
      evidenceWarnings: ["No gemstone certificate file has been provided."],
      riskFlags: ["No gemstone certificate file has been provided.", "Color inconsistency"] }}
    analyzing={false} onRunAnalysis={vi.fn()} />);

  expect(screen.getByText("Model-Reported Confidence")).toBeInTheDocument();
  expect(screen.getByText("85%")).toBeInTheDocument();
  expect(screen.getByText(/not a measure of authenticity or approval/)).toBeInTheDocument();
  expect(screen.getByText("Color inconsistency")).toBeInTheDocument();
  const warnings = screen.getByText("Evidence Warnings").closest(".agent-report-block");
  const risks = screen.getByText("Risk Flags").closest(".agent-report-block");
  expect(within(warnings).getByText("No gemstone certificate file has been provided.")).toBeInTheDocument();
  expect(within(risks).queryByText("No gemstone certificate file has been provided.")).not.toBeInTheDocument();
});

it("places real completed analysis metrics directly under the header", () => {
  const { container } = render(<GemVerificationAgentPanel
    verification={{ decision: "Pending", aiStatus: "Completed" }}
    aiResult={{ status: "Completed", suggestedGemType: "Sapphire", confidenceScore: 0.82,
      imageAnalyzed: true, riskFlags: ["Color inconsistency", "Certificate mismatch"] }}
    analyzing={false} onRunAnalysis={vi.fn()} />);

  const overview = container.querySelector(".agent-overview");
  expect(overview).toBeInTheDocument();
  expect(overview.compareDocumentPosition(container.querySelector(".gemora-agent-results")) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  expect(within(overview).getByText("Sapphire")).toBeInTheDocument();
  expect(within(overview).getByText("82%")).toBeInTheDocument();
  expect(within(overview).getByText("Image Analysis Status")).toBeInTheDocument();
  expect(within(overview).getByText("Risk Flags Count").nextElementSibling).toHaveTextContent("2");
  expect(within(overview).getByText("Human Review Required")).toBeInTheDocument();
});

it.each([
  ["NotStarted", "NOT STARTED", "Not Started"],
  ["Processing", "PROCESSING", "Processing"],
  ["Failed", "FAILED", "Failed"],
])("shows %s truthfully without stale successful metrics", (status, badge, imageStatus) => {
  const { container } = render(<GemVerificationAgentPanel
    verification={{ decision: "Pending", aiStatus: status }}
    aiResult={{ status, confidenceScore: 0.99, suggestedGemType: "Ruby", imageAnalyzed: true }}
    analyzing={status === "Processing"} onRunAnalysis={vi.fn()} />);
  expect(container.querySelector(".gemora-agent-status")).toHaveTextContent(badge);
  const overview = container.querySelector(".agent-overview");
  expect(within(overview).getByText("Image Analysis Status").nextElementSibling).toHaveTextContent(imageStatus);
  expect(within(overview).queryByText("99%")).not.toBeInTheDocument();
  expect(within(overview).queryByText("Ruby")).not.toBeInTheDocument();
});

it("does not reconstruct detailed steps from a legacy status or audit-message count", () => {
  render(<GemVerificationAgentPanel verification={verification}
    aiResult={{ status: "Failed", stepsCompleted: Array(8).fill("old audit") }}
    analyzing={false} onRunAnalysis={vi.fn()} />);
  expect(screen.getByText(/Detailed step history was not recorded/)).toBeInTheDocument();
  expect(screen.queryByText("Run AI analysis")).not.toBeInTheDocument();
});
