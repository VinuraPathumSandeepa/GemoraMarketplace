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

    return "READY";
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
  // DEFAULT AGENT WORKFLOW
  // ============================================================

  const defaultSteps = [
    "Load verification and gemstone listing",
    "Check verification workflow state",
    "Validate submitted evidence",
    "Load gemstone image",
    "Run multimodal AI analysis",
    "Prepare structured findings",
    "Return findings for human review",
  ];


  const completedSteps =
    Array.isArray(
      aiResult?.stepsCompleted
    )
      ? aiResult.stepsCompleted
      : [];


  // ============================================================
  // TIMELINE STATE
  // ============================================================

  const getStepState = (
    index
  ) => {

    /*
      When completed, all high-level
      stages are considered complete.
    */

    if (isCompleted) {
      return "completed";
    }


    /*
      For NeedsMoreEvidence,
      deterministic validation ran,
      but full AI model analysis
      may not have run.
    */

    if (needsMoreEvidence) {
      if (index <= 2) {
        return "completed";
      }

      return "pending";
    }


    /*
      While processing, use available
      backend step count to give a
      progressive visualization.
    */

    if (isProcessing) {
      if (
        index <
        completedSteps.length
      ) {
        return "completed";
      }

      if (
        index ===
        Math.min(
          completedSteps.length,
          defaultSteps.length - 1
        )
      ) {
        return "active";
      }
    }


    /*
      Failed run:
      retain steps the backend
      completed before failure.
    */

    if (isFailed) {
      if (
        index <
        completedSteps.length
      ) {
        return "completed";
      }

      return "pending";
    }


    return "pending";
  };


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
          disabled={
            isProcessing ||
            !isPendingReview
          }
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


      {/* ======================================================
          AGENT RESPONSIBILITIES
          ====================================================== */}

      <div className="gemora-agent-role-grid">

        <div>

          <span className="agent-role-number">
            01
          </span>

          <strong>
            Validate Evidence
          </strong>

          <p>
            Check required listing,
            image and certificate
            evidence before AI model
            analysis.
          </p>

        </div>


        <div>

          <span className="agent-role-number">
            02
          </span>

          <strong>
            Analyze Gemstone
          </strong>

          <p>
            Review gemstone imagery
            together with the seller's
            submitted listing data.
          </p>

        </div>


        <div>

          <span className="agent-role-number">
            03
          </span>

          <strong>
            Identify Risks
          </strong>

          <p>
            Surface inconsistencies,
            evidence problems and
            relevant verification
            risk indicators.
          </p>

        </div>


        <div>

          <span className="agent-role-number">
            04
          </span>

          <strong>
            Assist Human Review
          </strong>

          <p>
            Prepare structured findings
            for the Gemologist without
            making the final approval
            decision.
          </p>

        </div>

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

            The previous agent execution
            could not be completed.
            Review the submitted evidence
            and retry the Gemora
            Verification Agent when ready.

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
          AGENT EXECUTION TIMELINE
          ====================================================== */}

      {(isProcessing ||
        isCompleted ||
        needsMoreEvidence ||
        isFailed) && (
        <div className="gemora-agent-execution">

          <div className="agent-section-heading">

            <div>

              <span>
                AGENT EXECUTION
              </span>

              <h3>
                Analysis Workflow
              </h3>

            </div>


            <small>
              Structured agent steps
            </small>

          </div>


          <div className="agent-timeline">

            {defaultSteps.map(
              (
                step,
                index
              ) => {

                const state =
                  getStepState(
                    index
                  );


                return (
                  <div
                    key={step}
                    className={`agent-timeline-step agent-step-${state}`}
                  >

                    {/* MARKER */}

                    <div className="agent-timeline-marker">

                      {state ===
                      "completed"
                        ? "✓"
                        : state ===
                          "active"
                        ? "◆"
                        : index +
                          1}

                    </div>


                    {/* STEP CONTENT */}

                    <div className="agent-timeline-content">

                      <strong>
                        {step}
                      </strong>


                      {completedSteps[
                        index
                      ] && (
                        <small>
                          {
                            completedSteps[
                              index
                            ]
                          }
                        </small>
                      )}

                    </div>

                  </div>
                );
              }
            )}

          </div>

        </div>
      )}


      {/* ======================================================
          AGENT ANALYSIS REPORT
          ====================================================== */}

      {(isCompleted ||
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


              {aiResult.imageAnalyzed && (
                <div className="agent-image-analyzed">
                  ✓ Image Analyzed
                </div>
              )}

            </div>


            {/* ================================================
                SUMMARY
                ================================================ */}

            <div className="agent-result-summary">

              <article>

                <span>
                  Suggested Gem Type
                </span>

                <strong>
                  {
                    aiResult.suggestedGemType ||
                    "No suggestion"
                  }
                </strong>

              </article>


              <article>

                <span>
                  Confidence
                </span>

                <strong>
                  {formatConfidence(
                    aiResult.confidenceScore
                  )}
                </strong>

              </article>


              <article>

                <span>
                  Image Analysis
                </span>

                <strong>
                  {aiResult.imageAnalyzed
                    ? "Completed"
                    : "Not Performed"}
                </strong>

              </article>

            </div>


            {/* ================================================
                FINDINGS
                ================================================ */}

            {aiResult.findings && (
              <div className="agent-findings-card">

                <span>
                  AGENT FINDINGS
                </span>

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

            {Array.isArray(
              aiResult.riskFlags
            ) &&
              aiResult
                .riskFlags
                .length > 0 && (
                <div className="agent-report-block agent-risk-report">

                  <h4>
                    Risk Flags
                  </h4>


                  <ul>

                    {aiResult.riskFlags.map(
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