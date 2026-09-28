import { useEffect, useState } from "react";
import DashboardLayout from "../layouts/DashboardLayout";
import {
  getReviewQueue,
  getRequestDetail,
  downloadComplianceDocument,
} from "../services/exportOfficerService";
import "./ExportOfficerDashboard.css";

function ExportOfficerDashboard() {
  // Queue state
  const [queue, setQueue] = useState([]);
  const [loadingQueue, setLoadingQueue] = useState(true);
  const [queueError, setQueueError] = useState("");
  const [forbiddenError, setForbiddenError] = useState(false);

  // Selected request state
  const [selectedRequestId, setSelectedRequestId] = useState(null);
  const [requestDetail, setRequestDetail] = useState(null);
  const [loadingDetail, setLoadingDetail] = useState(false);
  const [detailError, setDetailError] = useState("");

  // Expandable state for tool calls
  const [showToolCalls, setShowToolCalls] = useState(false);

  // ==========================================
  // FETCH REVIEW QUEUE
  // ==========================================
  const fetchQueue = async () => {
    setLoadingQueue(true);
    setQueueError("");
    setForbiddenError(false);

    try {
      const data = await getReviewQueue();
      setQueue(data.requests || []);
    } catch (err) {
      console.error("Failed to load review queue:", err);
      if (err.response?.status === 403) {
        setForbiddenError(true);
        setQueueError("You don't have permission to view export compliance reviews.");
      } else if (err.response?.status === 401) {
        setQueueError("Your session has expired. Please sign in again.");
      } else {
        setQueueError(
          err.response?.data?.message ||
            "We couldn't load export requests. Please try again."
        );
      }
    } finally {
      setLoadingQueue(false);
    }
  };

  useEffect(() => {
    fetchQueue();
  }, []);

  // ==========================================
  // FETCH REQUEST DETAIL
  // ==========================================
  const handleSelectRequest = async (requestId) => {
    setSelectedRequestId(requestId);
    setLoadingDetail(true);
    setDetailError("");
    setRequestDetail(null);
    setShowToolCalls(false);

    try {
      const data = await getRequestDetail(requestId);
      setRequestDetail(data.request);
    } catch (err) {
      console.error("Failed to load request detail:", err);
      if (err.response?.status === 404) {
        setDetailError("This export request could not be found.");
      } else if (err.response?.status === 403) {
        setDetailError("You don't have permission to view export compliance reviews.");
      } else {
        setDetailError(
          err.response?.data?.message ||
            "We couldn't load this export request."
        );
      }
    } finally {
      setLoadingDetail(false);
    }
  };

  const handleBackToQueue = () => {
    setSelectedRequestId(null);
    setRequestDetail(null);
    setDetailError("");
  };

  // ==========================================
  // VIEW / DOWNLOAD DOCUMENT
  // ==========================================
  const handleViewDocument = async (documentId, fileName) => {
    if (!selectedRequestId || !documentId) return;

    try {
      const blob = await downloadComplianceDocument(selectedRequestId, documentId);
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.target = "_blank";
      link.download = fileName || `document-${documentId}.pdf`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    } catch (err) {
      console.error("Failed to download compliance document:", err);
      alert(err.response?.data?.message || "Failed to download document file.");
    }
  };

  // Status Badge Helper
  const renderStatusBadge = (status) => {
    if (!status) return null;
    const cleanStatus = status.toLowerCase();
    return <span className={`badge badge-${cleanStatus}`}>{status}</span>;
  };

  // Actor Badge Helper
  const renderActorBadge = (actorType) => {
    if (!actorType) return null;
    const cleanActor = actorType.toLowerCase();
    return <span className={`badge badge-${cleanActor}`}>{actorType}</span>;
  };

  return (
    <DashboardLayout title="Export Officer Dashboard">
      <div className="export-officer-container">
        {/* PAGE HEADER */}
        <div className="dashboard-header">
          <h2>Export Compliance</h2>
          <p className="subtitle">
            Review submitted export requests and compliance assessments.
          </p>
        </div>

        {/* FORBIDDEN ACCESS STATE */}
        {forbiddenError && (
          <div className="alert-box alert-error">
            <strong>Access Denied:</strong> {queueError}
          </div>
        )}

        {/* ==========================================
            VIEW 1: QUEUE LISTING
        ========================================== */}
        {!selectedRequestId && !forbiddenError && (
          <div className="queue-section">
            {loadingQueue && (
              <div className="alert-box alert-info">
                Loading export requests...
              </div>
            )}

            {queueError && !loadingQueue && (
              <div className="alert-box alert-error">
                <p>{queueError}</p>
                <button className="btn-retry" onClick={fetchQueue}>
                  Retry
                </button>
              </div>
            )}

            {!loadingQueue && !queueError && queue.length === 0 && (
              <div className="alert-box alert-info">
                No export requests are currently waiting for review.
              </div>
            )}

            {!loadingQueue && !queueError && queue.length > 0 && (
              <div className="queue-card">
                <div className="table-responsive">
                  <table className="queue-table">
                    <thead>
                      <tr>
                        <th>Request ID</th>
                        <th>Requester</th>
                        <th>Origin / Destination</th>
                        <th>Declared Value</th>
                        <th>Status</th>
                        <th>Submitted Date</th>
                        <th>Docs</th>
                      </tr>
                    </thead>
                    <tbody>
                      {queue.map((req) => (
                        <tr
                          key={req.id}
                          className="queue-row"
                          onClick={() => handleSelectRequest(req.id)}
                        >
                          <td>
                            <code>{req.id ? req.id.substring(0, 8) + "..." : "-"}</code>
                          </td>
                          <td>
                            <div>{req.requesterName || "N/A"}</div>
                            <div className="info-label">{req.requesterEmail || ""}</div>
                          </td>
                          <td>
                            {req.originCountry} &rarr; {req.destinationCountry}
                          </td>
                          <td>
                            {req.declaredValue?.toLocaleString()}{" "}
                            {req.currency}
                          </td>
                          <td>{renderStatusBadge(req.status)}</td>
                          <td>
                            {req.submittedAt
                              ? new Date(req.submittedAt).toLocaleDateString()
                              : "-"}
                          </td>
                          <td>{req.documents?.length || 0}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </div>
        )}

        {/* ==========================================
            VIEW 2: REQUEST DETAIL
        ========================================== */}
        {selectedRequestId && (
          <div className="detail-section">
            <button className="btn-back" onClick={handleBackToQueue}>
              &larr; Back to Queue
            </button>

            {loadingDetail && (
              <div className="alert-box alert-info">
                Loading export request details...
              </div>
            )}

            {detailError && !loadingDetail && (
              <div className="alert-box alert-error">
                <p>{detailError}</p>
                <button
                  className="btn-retry"
                  onClick={() => handleSelectRequest(selectedRequestId)}
                >
                  Retry
                </button>
              </div>
            )}

            {!loadingDetail && !detailError && requestDetail && (
              <div className="detail-grid">
                {/* SECTION A: EXPORT REQUEST BASIC INFORMATION */}
                <div className="section-card">
                  <h3>Export Request Details</h3>
                  <div className="info-grid">
                    <div className="info-item">
                      <span className="info-label">Request ID</span>
                      <span className="info-value">
                        <code>{requestDetail.id}</code>
                      </span>
                    </div>
                    <div className="info-item">
                      <span className="info-label">Current Status</span>
                      <span className="info-value">
                        {renderStatusBadge(requestDetail.status)}
                      </span>
                    </div>
                    <div className="info-item">
                      <span className="info-label">Requester</span>
                      <span className="info-value">
                        {requestDetail.requesterName} ({requestDetail.requesterEmail})
                      </span>
                    </div>
                    <div className="info-item">
                      <span className="info-label">Origin Country</span>
                      <span className="info-value">{requestDetail.originCountry}</span>
                    </div>
                    <div className="info-item">
                      <span className="info-label">Destination Country</span>
                      <span className="info-value">{requestDetail.destinationCountry}</span>
                    </div>
                    <div className="info-item">
                      <span className="info-label">Declared Value</span>
                      <span className="info-value">
                        {requestDetail.declaredValue?.toLocaleString()} {requestDetail.currency}
                      </span>
                    </div>
                    <div className="info-item">
                      <span className="info-label">Purpose</span>
                      <span className="info-value">{requestDetail.purpose || "N/A"}</span>
                    </div>
                    <div className="info-item">
                      <span className="info-label">Submitted Date</span>
                      <span className="info-value">
                        {requestDetail.submittedAt
                          ? new Date(requestDetail.submittedAt).toLocaleString()
                          : "N/A"}
                      </span>
                    </div>
                    {requestDetail.reviewNotes && (
                      <div className="info-item" style={{ gridColumn: "1 / -1" }}>
                        <span className="info-label">Review Notes</span>
                        <span className="info-value">{requestDetail.reviewNotes}</span>
                      </div>
                    )}
                  </div>
                </div>

                {/* SECTION B: DOCUMENTS METADATA */}
                <div className="section-card">
                  <h3>Compliance Documents ({requestDetail.documents?.length || 0})</h3>
                  {(!requestDetail.documents || requestDetail.documents.length === 0) ? (
                    <p className="subtitle">No compliance documents attached.</p>
                  ) : (
                    <div className="table-responsive">
                      <table className="queue-table">
                        <thead>
                          <tr>
                            <th>Type</th>
                            <th>Number</th>
                            <th>Issuer</th>
                            <th>Issue Date</th>
                            <th>Expiry Date</th>
                            <th>Status</th>
                            <th>Action</th>
                          </tr>
                        </thead>
                        <tbody>
                          {requestDetail.documents.map((doc) => (
                            <tr key={doc.id}>
                              <td><strong>{doc.documentType}</strong></td>
                              <td>{doc.documentNumber || "N/A"}</td>
                              <td>{doc.issuer || "N/A"}</td>
                              <td>
                                {doc.issueDate
                                  ? new Date(doc.issueDate).toLocaleDateString()
                                  : "-"}
                              </td>
                              <td>
                                {doc.expiryDate
                                  ? new Date(doc.expiryDate).toLocaleDateString()
                                  : "-"}
                              </td>
                              <td>{renderStatusBadge(doc.status)}</td>
                              <td>
                                {doc.fileUrl ? (
                                  <button
                                    className="btn-back"
                                    style={{ margin: 0, padding: "4px 8px", fontSize: "12px" }}
                                    onClick={() => handleViewDocument(doc.id, `doc-${doc.id}.pdf`)}
                                  >
                                    View File
                                  </button>
                                ) : (
                                  <span className="info-label">No File</span>
                                )}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>

                {/* SECTION C: DETERMINISTIC COMPLIANCE CHECK */}
                <div className="section-card">
                  <h3>Deterministic Compliance Check</h3>
                  {!requestDetail.deterministicCompliance ? (
                    <p className="subtitle">Deterministic compliance check is not available.</p>
                  ) : (
                    <div>
                      <div style={{ marginBottom: "16px", display: "flex", alignItems: "center", gap: "10px" }}>
                        <span className="info-label">Check Result:</span>
                        {requestDetail.deterministicCompliance.isComplete ? (
                          <span className="badge badge-approved">Complete</span>
                        ) : (
                          <span className="badge badge-rejected">Incomplete</span>
                        )}
                      </div>

                      {/* Missing Requirements */}
                      <div style={{ marginBottom: "12px" }}>
                        <strong className="info-label">Missing Requirements:</strong>
                        {requestDetail.deterministicCompliance.missingRequirements?.length > 0 ? (
                          <ul className="finding-list" style={{ marginTop: "6px" }}>
                            {requestDetail.deterministicCompliance.missingRequirements.map((req, i) => (
                              <li key={i} className="finding-item severity-Critical">{req}</li>
                            ))}
                          </ul>
                        ) : (
                          <p className="subtitle" style={{ fontSize: "13px", marginTop: "4px" }}>
                            No missing requirements detected by deterministic checks.
                          </p>
                        )}
                      </div>

                      {/* Invalid Documents */}
                      <div style={{ marginBottom: "12px" }}>
                        <strong className="info-label">Invalid Documents:</strong>
                        {requestDetail.deterministicCompliance.invalidDocuments?.length > 0 ? (
                          <ul className="finding-list" style={{ marginTop: "6px" }}>
                            {requestDetail.deterministicCompliance.invalidDocuments.map((doc, i) => (
                              <li key={i} className="finding-item severity-Critical">{doc}</li>
                            ))}
                          </ul>
                        ) : (
                          <p className="subtitle" style={{ fontSize: "13px", marginTop: "4px" }}>
                            No invalid documents detected.
                          </p>
                        )}
                      </div>

                      {/* Warnings */}
                      <div>
                        <strong className="info-label">Warnings:</strong>
                        {requestDetail.deterministicCompliance.warnings?.length > 0 ? (
                          <ul className="finding-list" style={{ marginTop: "6px" }}>
                            {requestDetail.deterministicCompliance.warnings.map((warn, i) => (
                              <li key={i} className="finding-item severity-Warning">{warn}</li>
                            ))}
                          </ul>
                        ) : (
                          <p className="subtitle" style={{ fontSize: "13px", marginTop: "4px" }}>
                            No deterministic warnings reported.
                          </p>
                        )}
                      </div>
                    </div>
                  )}
                </div>

                {/* TWO-COLUMN GRID FOR WORKFLOW & AI ASSESSMENT */}
                <div className="detail-grid-two-col">
                  {/* SECTION D: WORKFLOW STATUS SUMMARY */}
                  <div className="section-card">
                    <h3>Compliance Workflow State</h3>
                    {!requestDetail.agentWorkflow ? (
                      <div className="alert-box alert-info">
                        No automated compliance workflow is available for this request.
                      </div>
                    ) : (
                      <div>
                        <div className="info-grid" style={{ marginBottom: "16px" }}>
                          <div className="info-item">
                            <span className="info-label">Workflow Status</span>
                            <span className="info-value">
                              {renderStatusBadge(requestDetail.agentWorkflow.workflowStatus)}
                            </span>
                          </div>
                          <div className="info-item">
                            <span className="info-label">Approval Status</span>
                            <span className="info-value">
                              {renderStatusBadge(requestDetail.agentWorkflow.approvalStatus)}
                            </span>
                          </div>
                          <div className="info-item">
                            <span className="info-label">Current Step</span>
                            <span className="info-value">{requestDetail.agentWorkflow.currentStep}</span>
                          </div>
                          <div className="info-item">
                            <span className="info-label">Started At</span>
                            <span className="info-value">
                              {requestDetail.agentWorkflow.createdAt
                                ? new Date(requestDetail.agentWorkflow.createdAt).toLocaleString()
                                : "-"}
                            </span>
                          </div>
                        </div>

                        {requestDetail.agentWorkflow.finalSummary && (
                          <div className="alert-box alert-success" style={{ marginBottom: "16px" }}>
                            <strong>Final Summary:</strong> {requestDetail.agentWorkflow.finalSummary}
                          </div>
                        )}

                        {requestDetail.agentWorkflow.workflowStatus === "Failed" && (
                          <div className="alert-box alert-error" style={{ marginBottom: "16px" }}>
                            Automated compliance analysis did not complete successfully. The Export Officer may continue with manual review.
                          </div>
                        )}
                      </div>
                    )}
                  </div>

                  {/* SECTION E & F: AI COMPLIANCE ASSESSMENT (ADVISORY SUMMARY ONLY) */}
                  <div className="section-card">
                    <h3>AI Compliance Assessment</h3>
                    {!requestDetail.agentWorkflow ? (
                      <p className="subtitle">No automated compliance workflow is available for this request.</p>
                    ) : !requestDetail.agentWorkflow.assessment ? (
                      <p className="subtitle">
                        {requestDetail.agentWorkflow.workflowStatus === "Failed"
                          ? "Automated compliance analysis did not complete successfully. The Export Officer may continue with manual review."
                          : "AI assessment is not available for this workflow."}
                      </p>
                    ) : (
                      <div>
                        {/* PROMINENT DISCLAIMER BOX */}
                        <div className="alert-box alert-warning">
                          <strong>Advisory Assessment:</strong> AI-generated advisory assessment. Final export decisions must be made by an authorized Export Officer.
                        </div>

                        <div className="info-grid" style={{ marginBottom: "16px" }}>
                          <div className="info-item">
                            <span className="info-label">Confidence Score</span>
                            <span className="info-value">
                              {(requestDetail.agentWorkflow.assessment.confidence * 100).toFixed(0)}%
                            </span>
                          </div>
                          <div className="info-item">
                            <span className="info-label">Officer Attention</span>
                            <span className="info-value">
                              {requestDetail.agentWorkflow.assessment.requiresOfficerAttention ? (
                                <span className="badge badge-rejected">Yes</span>
                              ) : (
                                <span className="badge badge-approved">No</span>
                              )}
                            </span>
                          </div>
                        </div>

                        <div style={{ marginBottom: "16px" }}>
                          <strong className="info-label">Summary:</strong>
                          <p style={{ marginTop: "4px", fontSize: "14px", color: "var(--text-h)" }}>
                            {requestDetail.agentWorkflow.assessment.summary}
                          </p>
                        </div>

                        {/* Document Findings */}
                        {requestDetail.agentWorkflow.assessment.documentFindings?.length > 0 && (
                          <div style={{ marginBottom: "16px" }}>
                            <strong className="info-label">Document Findings:</strong>
                            <ul className="finding-list" style={{ marginTop: "6px" }}>
                              {requestDetail.agentWorkflow.assessment.documentFindings.map((finding, idx) => (
                                <li key={idx} className={`finding-item severity-${finding.severity}`}>
                                  <div>
                                    <strong>{finding.documentType}:</strong> {finding.finding}
                                  </div>
                                </li>
                              ))}
                            </ul>
                          </div>
                        )}

                        {/* Recommended Checks */}
                        {requestDetail.agentWorkflow.assessment.recommendedOfficerChecks?.length > 0 && (
                          <div>
                            <strong className="info-label">Recommended Officer Checks:</strong>
                            <ul className="finding-list" style={{ marginTop: "6px" }}>
                              {requestDetail.agentWorkflow.assessment.recommendedOfficerChecks.map((check, idx) => (
                                <li key={idx} className="finding-item severity-Info">
                                  {check}
                                </li>
                              ))}
                            </ul>
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                </div>

                {/* SECTION G & H: WORKFLOW PROGRESS TIMELINE & CONTROLLED TOOLS */}
                {requestDetail.agentWorkflow?.steps?.length > 0 && (
                  <div className="section-card">
                    <h3>Workflow Execution Progress</h3>
                    <div className="timeline">
                      {requestDetail.agentWorkflow.steps.map((step) => (
                        <div key={step.stepNumber} className="timeline-step">
                          <div className="step-header">
                            <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
                              <span className="step-title">
                                Step {step.stepNumber}: {step.actor}
                              </span>
                              {renderActorBadge(step.actorType)}
                            </div>
                            <div>{renderStatusBadge(step.status)}</div>
                          </div>

                          <div className="step-action">
                            <strong>Action:</strong> {step.action}
                          </div>

                          {/* Controlled Tool Calls for Step 1 */}
                          {step.stepNumber === 1 && step.toolCalls?.length > 0 && (
                            <div style={{ marginTop: "8px" }}>
                              <button
                                className="tool-calls-toggle"
                                onClick={() => setShowToolCalls(!showToolCalls)}
                              >
                                {showToolCalls
                                  ? "▲ Hide Controlled Tools (" + step.toolCalls.length + ")"
                                  : "▼ View Controlled Tools (" + step.toolCalls.length + ")"}
                              </button>

                              {showToolCalls && (
                                <table className="tool-calls-table">
                                  <thead>
                                    <tr>
                                      <th>Tool Name</th>
                                      <th>Attempt</th>
                                      <th>Status</th>
                                      <th>Duration</th>
                                      <th>Error Code</th>
                                    </tr>
                                  </thead>
                                  <tbody>
                                    {step.toolCalls.map((tc, tidx) => (
                                      <tr key={tidx}>
                                        <td><code>{tc.toolName}</code></td>
                                        <td>{tc.attemptNumber}</td>
                                        <td>
                                          {tc.succeeded ? (
                                            <span className="badge badge-approved">Succeeded</span>
                                          ) : (
                                            <span className="badge badge-rejected">Failed</span>
                                          )}
                                        </td>
                                        <td>{tc.durationMs} ms</td>
                                        <td>{tc.errorCode || "-"}</td>
                                      </tr>
                                    ))}
                                  </tbody>
                                </table>
                              )}
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        )}
      </div>
    </DashboardLayout>
  );
}

export default ExportOfficerDashboard;