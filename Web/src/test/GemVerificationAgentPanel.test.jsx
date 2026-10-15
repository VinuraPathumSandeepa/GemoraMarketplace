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
  expect(screen.getByText("Analysis Workflow").closest("details")).not.toHaveAttribute("open");
  fireEvent.click(screen.getByText("Analysis Workflow"));
  expect(screen.getByText("Analysis Workflow").closest("details")).toHaveAttribute("open");
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

it("puts AI findings and paired insights before the expandable workflow", () => {
  const { container } = render(<GemVerificationAgentPanel
    verification={{ decision: "Pending", aiStatus: "Completed" }}
    aiResult={{ status: "Completed", imageAnalyzed: true, findings: "Color differs from seller description.",
      visualObservations: ["Oval cut", "Blue tone"], riskFlags: ["Color inconsistency"],
      evidenceWarnings: ["Certificate not supplied"], validationIssues: ["Certificate review needed"],
      workflowSteps: steps.map((step) => ({ ...step, status: "Completed" })) }}
    analyzing={false} onRunAnalysis={vi.fn()} />);
  const report = container.querySelector(".gemora-agent-results");
  const workflow = container.querySelector(".gemora-agent-execution");
  expect(report.compareDocumentPosition(workflow) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  expect(within(report).getByText("AI FINDINGS").closest(".agent-findings-card"))
    .toHaveTextContent("Color differs from seller description.");
  const insights = report.querySelector(".agent-insight-grid");
  expect(within(insights).getByText("Oval cut")).toBeInTheDocument();
  expect(within(insights).getByText("Color inconsistency")).toBeInTheDocument();
  expect(within(insights).queryByText("Certificate not supplied")).not.toBeInTheDocument();
  expect(within(report).getByText("Evidence Warnings").closest(".agent-validation-report"))
    .toHaveTextContent("Certificate not supplied");
  expect(screen.getByText("Human decision required")).toBeInTheDocument();
});

it("labels deterministic evidence results without implying Gemini image analysis", () => {
  const { container } = render(<GemVerificationAgentPanel
    verification={{ decision: "Pending", aiStatus: "NeedsMoreEvidence" }}
    aiResult={{ status: "NeedsMoreEvidence", findings: "Certificate is missing.",
      imageAnalyzed: false, evidenceWarnings: ["Certificate is missing."],
      riskFlags: ["Old model flag"] }} analyzing={false} onRunAnalysis={vi.fn()} />);
  expect(screen.getByText("EVIDENCE VALIDATION OUTCOME")).toBeInTheDocument();
  expect(screen.queryByText("✓ Image Analyzed")).not.toBeInTheDocument();
  expect(container.querySelector(".agent-insight-grid")).not.toHaveTextContent("Old model flag");
  expect(screen.getByText("Evidence Warnings").closest(".agent-validation-report"))
    .toHaveTextContent("Certificate is missing.");
});

it("shows readable provider failure and retry guidance without successful findings", () => {
  const { container } = render(<GemVerificationAgentPanel
    verification={{ decision: "Pending", aiStatus: "Failed" }}
    aiResult={{ status: "Failed", findings: "Rate limit reached.", retryAfterSeconds: 45,
      confidenceScore: 0.91, imageAnalyzed: false }}
    analyzing={false} onRunAnalysis={vi.fn()} />);
  const failure = screen.getByRole("alert");
  expect(failure).toHaveTextContent("Analysis could not be completed");
  expect(failure).toHaveTextContent("Rate limit reached.");
  expect(failure).toHaveTextContent("wait at least 45 seconds");
  expect(container.querySelector(".gemora-agent-results")).not.toBeInTheDocument();
  expect(screen.queryByText("91%")).not.toBeInTheDocument();
});

it("shows an empty report message when completed analysis has no detail", () => {
  render(<GemVerificationAgentPanel
    verification={{ decision: "Pending", aiStatus: "Completed" }}
    aiResult={{ status: "Completed", imageAnalyzed: false }}
    analyzing={false} onRunAnalysis={vi.fn()} />);
  expect(screen.getByText(/No detailed findings were returned/)).toBeInTheDocument();
  expect(screen.getByText("Image Analysis Status").nextElementSibling).toHaveTextContent("Not Performed");
});
