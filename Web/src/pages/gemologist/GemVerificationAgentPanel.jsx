function GemVerificationAgentPanel({
  verification,
  aiResult,
  analyzing,
  onRunAnalysis,
}) {
  // ============================================================
  // CURRENT AGENT STATUS
  // ============================================================

  const rawStatus =
    aiResult?.status ||
    verification?.aiStatus ||
    "NotStarted";


  const normalizedStatus =
    rawStatus
      .replace(/\s/g, "")
      .toLowerCase();


  const isCompleted =
    normalizedStatus ===
    "completed";


  const needsMoreEvidence =
    normalizedStatus ===
    "needsmoreevidence";


  const isFailed =
    normalizedStatus ===
    "failed";


  const isProcessing =
    analyzing ||
    normalizedStatus ===
      "processing";


  const isPendingReview =
    (
      verification?.decision ||
      ""
    ).toLowerCase() ===
    "pending";


  // ============================================================
  // STATUS LABEL
  // ============================================================

  const getStatusLabel = () => {
    if (isProcessing) {
      return "PROCESSING";
    }

    if (isCompleted) {
      return "COMPLETED";
    }

    if (needsMoreEvidence) {
      return "NEEDS MORE EVIDENCE";
    }

    if (isFailed) {
      return "FAILED";
    }

    return "NOT STARTED";
  };


  // ============================================================
  // STATUS CLASS
  // ============================================================

  const getStatusClass = () => {
    if (isProcessing) {
      return "agent-status-processing";
    }

    if (isCompleted) {
      return "agent-status-completed";
    }

    if (needsMoreEvidence) {
      return "agent-status-warning";
    }

    if (isFailed) {
      return "agent-status-failed";
    }

    return "agent-status-ready";
  };


  // ============================================================
  // RUN BUTTON TEXT
  // ============================================================

  const getRunButtonText = () => {
    if (isProcessing) {
      return "Agent Analyzing...";
    }

    if (isCompleted) {
      return "Run Agent Again";
    }

    if (
      needsMoreEvidence ||
      isFailed
    ) {
      return "Retry Agent Analysis";
    }

    return "Start Agent Analysis";
  };


  // ============================================================
  // CONFIDENCE
  // ============================================================

  const formatConfidence = (
    value
  ) => {

    if (
      value === null ||
      value === undefined
    ) {
      return "—";
    }

    const numeric =
      Number(value);


    if (
      Number.isNaN(
        numeric
      )
    ) {
      return "—";
    }


    /*
      Supports:
      0.87
      OR
      87
    */

    if (numeric <= 1) {
      return `${Math.round(
        numeric * 100
      )}%`;
    }


    return `${Math.round(
      numeric
    )}%`;
  };


  // ============================================================
  // BACKEND WORKFLOW
  // ============================================================

  const defaultSteps = [
    ["load", "Load verification and gemstone listing"],
    ["state", "Check verification workflow state"],
    ["validation", "Validate submitted evidence"],
    ["image", "Load gemstone image"],
    ["ai", "Run AI analysis"],
    ["findings", "Prepare structured findings"],
    ["review", "Return findings for human review"],
  ];


  const workflowSteps = !analyzing && Array.isArray(aiResult?.workflowSteps)
    ? aiResult.workflowSteps : [];

  const evidenceWarnings = Array.isArray(aiResult?.evidenceWarnings)
    ? aiResult.evidenceWarnings : [];
  const riskFlags = Array.isArray(aiResult?.riskFlags)
    ? aiResult.riskFlags.filter((flag) => !evidenceWarnings.includes(flag)) : [];
  const hasCompletedAnalysis = isCompleted && !isProcessing;


  // ============================================================
  // TIMELINE STATE
  // ============================================================

  const getStepState = (id) =>
    workflowSteps.find((step) => step.id === id)?.status?.toLowerCase() || "pending";


  // ============================================================
  // UI
  // ============================================================

  return (
    <section className="gemora-agent-panel">

      {/* ======================================================
          AGENT HEADER
          ====================================================== */}

      <div className="gemora-agent-header">

        <div className="gemora-agent-identity">

          {/* AGENT ORB */}

          <div
            className={`gemora-agent-orb ${
              isProcessing
                ? "agent-orb-processing"
                : ""
            }`}
          >

            <div className="agent-orb-ring" />

            <div className="agent-orb-core">
              ◆
            </div>

          </div>


          {/* IDENTITY */}

          <div>

            <div className="gemora-agent-title-row">

              <span className="gemora-agent-label">
                GEMORA AGENTIC AI
              </span>


              <span
                className={`gemora-agent-status ${getStatusClass()}`}
              >

                <i />

                {
                  getStatusLabel()
                }

              </span>

            </div>


            <h2>
              Gemora Verification Agent
            </h2>


            <p>
              Intelligent gemstone
              evidence analysis designed
              to assist professional
              Gemologist review.
            </p>

          </div>

        </div>


        {/* ====================================================
            RUN AGENT BUTTON
            ==================================================== */}

        <button
          type="button"
          className={`gemora-agent-run-button ${
            isProcessing
              ? "agent-button-processing"
              : ""
          }`}
          onClick={
            onRunAnalysis
          }
          disabled={isProcessing || !isPendingReview}
        >

          {isProcessing ? (
            <>
              <span className="agent-button-spinner" />

              Agent Analyzing...
            </>
          ) : (
            <>

              <span className="agent-button-symbol">
                ◆
              </span>


              {
                getRunButtonText()
              }


              <span>
                →
              </span>

            </>
          )}

        </button>

      </div>

      <div className="agent-overview" aria-label="AI analysis summary">
        <div className="agent-overview-heading">
          <span>ANALYSIS SNAPSHOT</span>
          {isPendingReview && <strong>Human Review Required</strong>}
        </div>
        <div className="agent-result-summary">
          <article><span>Suggested Gemstone Type</span>
            <strong>{hasCompletedAnalysis ? aiResult?.suggestedGemType || "No suggestion" : "—"}</strong></article>
          <article><span>Model-Reported Confidence</span>
            <strong>{hasCompletedAnalysis ? formatConfidence(aiResult?.confidenceScore) : "—"}</strong></article>
          <article><span>Image Analysis Status</span>
            <strong>{isProcessing ? "Processing" : hasCompletedAnalysis
              ? aiResult?.imageAnalyzed ? "Completed" : "Not Performed"
              : isFailed ? "Failed" : needsMoreEvidence ? "Not Performed" : "Not Started"}</strong></article>
          <article><span>Risk Flags Count</span>
            <strong>{hasCompletedAnalysis ? riskFlags.length : "—"}</strong></article>
        </div>
        <p className="agent-confidence-note">
          Model confidence is advisory only, not a measure of authenticity or approval. A Gemologist makes the final decision.
        </p>
      </div>


      {/* ======================================================
          PROCESSING DISPLAY
          ====================================================== */}

      {isProcessing && (
        <div className="gemora-agent-processing">

          <div className="agent-processing-visual">

            <div className="agent-scan-ring agent-scan-ring-one" />

            <div className="agent-scan-ring agent-scan-ring-two" />


            <div className="agent-processing-core">
              ◆
            </div>

          </div>


          <div>

            <span>
              AGENT WORKING
            </span>

            <h3>
              Analyzing gemstone evidence
            </h3>

            <p>
              Gemora is validating the
              submission, examining the
              gemstone evidence and
              preparing AI-assisted
              findings for human review.
            </p>

          </div>

        </div>
      )}


      {/* ======================================================
          FAILED STATE
          ====================================================== */}

      {isFailed &&
        !isProcessing && (
          <div className="verification-ai-warning">

            {aiResult?.findings || "The previous agent execution could not be completed. Human review remains available."}

          </div>
        )}


      {/* ======================================================
          NEEDS MORE EVIDENCE
          ====================================================== */}

      {needsMoreEvidence &&
        !isProcessing && (
          <div className="verification-ai-warning">

            Required evidence did not
            pass deterministic validation.
            Full AI gemstone analysis was
            safely stopped. Review the
            evidence issues below before
            making a human decision.

          </div>
        )}


      {/* ======================================================
          AGENT ANALYSIS REPORT
          ====================================================== */}

      {!isProcessing && (isCompleted ||
        needsMoreEvidence) &&
        aiResult && (
          <div className="gemora-agent-results">

            {/* ================================================
                REPORT HEADER
                ================================================ */}

            <div className="agent-section-heading">

              <div>

                <span>
                  AGENT OUTPUT
                </span>

                <h3>
                  Analysis Report
                </h3>

              </div>


              {isCompleted && aiResult.imageAnalyzed && (
                <div className="agent-image-analyzed">
                  ✓ Image Analyzed
                </div>
              )}

            </div>


            {/* ================================================
                FINDINGS
                ================================================ */}

            {aiResult.findings && (
              <div className="agent-findings-card">

                <span>{isCompleted ? "AI FINDINGS" : "EVIDENCE VALIDATION OUTCOME"}</span>

                <p>
                  {
                    aiResult.findings
                  }
                </p>

              </div>
            )}


            {/* ================================================
                VISUAL OBSERVATIONS
                ================================================ */}

            <div className="agent-insight-grid">
            {Array.isArray(
              aiResult.visualObservations
            ) &&
              aiResult
                .visualObservations
                .length > 0 && (
                <div className="agent-report-block">

                  <h4>
                    Visual Observations
                  </h4>


                  <ul>

                    {aiResult.visualObservations.map(
                      (
                        observation,
                        index
                      ) => (
                        <li
                          key={
                            `${observation}-${index}`
                          }
                        >

                          <span>
                            ✓
                          </span>

                          {
                            observation
                          }

                        </li>
                      )
                    )}

                  </ul>

                </div>
              )}


            {/* ================================================
                RISK FLAGS
                ================================================ */}

            {isCompleted && riskFlags.length > 0 && (
                <div className="agent-report-block agent-risk-report">

                  <h4>
                    Risk Flags
                  </h4>


                  <ul>

                    {riskFlags.map(
                      (
                        risk,
                        index
                      ) => (
                        <li
                          key={
                            `${risk}-${index}`
                          }
                        >

                          <span>
                            !
                          </span>

                          {risk}

                        </li>
                      )
                    )}

                  </ul>

                </div>
              )}
            </div>

            {evidenceWarnings.length > 0 && (
              <div className="agent-report-block agent-validation-report">
                <h4>Evidence Warnings</h4>
                <ul>
                  {evidenceWarnings.map((warning, index) => (
                    <li key={`${warning}-${index}`}><span>!</span>{warning}</li>
                  ))}
                </ul>
              </div>
            )}


            {/* ================================================
                VALIDATION ISSUES
                ================================================ */}

            {Array.isArray(
              aiResult.validationIssues
            ) &&
              aiResult
                .validationIssues
                .length > 0 && (
                <div className="agent-report-block agent-validation-report">

                  <h4>
                    Evidence Validation Issues
                  </h4>


                  <ul>

                    {aiResult.validationIssues.map(
                      (
                        issue,
                        index
                      ) => (
                        <li
                          key={
                            `${issue}-${index}`
                          }
                        >

                          <span>
                            !
                          </span>

                          {issue}

                        </li>
                      )
                    )}

                  </ul>

                </div>
              )}

          </div>
        )}

      {(isProcessing || isCompleted || needsMoreEvidence || isFailed) && (
        <details className="gemora-agent-execution">
          <summary className="agent-workflow-summary">
            <span>Analysis Workflow</span>
            <small>{workflowSteps.length > 0
              ? `${workflowSteps.filter((step) => step.status?.toLowerCase() === "completed").length} of ${workflowSteps.length} steps completed`
              : "View execution steps"}</small>
          </summary>
          {workflowSteps.length === 0 ? <p className="agent-workflow-empty">{isProcessing
            ? "Waiting for backend step results."
            : "Detailed step history was not recorded for this analysis."}</p> :
            <div className="agent-timeline">
              {defaultSteps.map(([id, step], index) => {
                const state = getStepState(id);
                return <div key={id} className={`agent-timeline-step agent-step-${state}`}>
                  <div className="agent-timeline-marker" aria-hidden="true">
                    {state === "completed" ? "✓" : state === "running" ? "◆" :
                      state === "failed" ? "!" : state === "skipped" ? "–" : index + 1}
                  </div>
                  <div className="agent-timeline-content">
                    <strong>{step}</strong>
                    <small>{state.charAt(0).toUpperCase() + state.slice(1)}</small>
                  </div>
                </div>;
              })}
            </div>}
        </details>
      )}


      {/* ======================================================
          HUMAN AUTHORITY BOUNDARY
          ====================================================== */}

      <div className="gemora-agent-human-boundary">

        <div className="agent-human-icon">
          H
        </div>


        <div>

          <strong>
            Human decision required
          </strong>


          <p>
            The Gemora Verification
            Agent provides advisory
            findings only. The final
            Approve, Request Changes or
            Reject decision remains with
            the Gemologist.
          </p>

        </div>

      </div>

    </section>
  );
}

export default GemVerificationAgentPanel;
