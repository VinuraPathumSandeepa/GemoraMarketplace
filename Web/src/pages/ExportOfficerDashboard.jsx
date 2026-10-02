import { useEffect, useState } from "react";
import DashboardLayout from "../layouts/DashboardLayout";
import {
  getReviewQueue,
  getRequestDetail,
  startReview,
  makeDecision,
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

  // Action / Decision state
  const [actionLoading, setActionLoading] = useState(false);
  const [actionType, setActionType] = useState(null); // 'start', 'approve', 'reject', 'revision'
  const [activeConfirmation, setActiveConfirmation] = useState(null); // 'approve', 'reject', 'revision'
  const [reviewNotes, setReviewNotes] = useState("");
  const [notesValidationError, setNotesValidationError] = useState("");
  const [actionSuccessMessage, setActionSuccessMessage] = useState("");
  const [actionErrorMessage, setActionErrorMessage] = useState("");

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
    setActiveConfirmation(null);
    setReviewNotes("");
    setNotesValidationError("");
    setActionSuccessMessage("");
    setActionErrorMessage("");

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
    setActiveConfirmation(null);
    setReviewNotes("");
    setNotesValidationError("");
    setActionSuccessMessage("");
    setActionErrorMessage("");
  };

  // ==========================================
  // START REVIEW ACTION
  // ==========================================
  const handleStartReview = async () => {
    if (!selectedRequestId || actionLoading) return;

    setActionLoading(true);
    setActionType("start");
    setActionSuccessMessage("");
    setActionErrorMessage("");

    try {
      await startReview(selectedRequestId);
      setActionSuccessMessage("Human review started successfully.");
      
      // Refresh request detail and queue
      const updated = await getRequestDetail(selectedRequestId);
      setRequestDetail(updated.request);
      fetchQueue();
    } catch (err) {
      console.error("Failed to start review:", err);
      handleActionError(err);
    } finally {
      setActionLoading(false);
      setActionType(null);
    }
  };

  // ==========================================
  // MAKE DECISION ACTION (Approve, Reject, Revision)
  // ==========================================
  const handleConfirmDecision = async (decisionStr) => {
    if (!selectedRequestId || actionLoading) return;

    // Validate required notes for Reject and RequestRevision
    const trimmedNotes = reviewNotes.trim();
    if (decisionStr === "Reject" && !trimmedNotes) {
      setNotesValidationError("Please provide a reason for rejection.");
      return;
    }

    if (decisionStr === "RequestRevision" && !trimmedNotes) {
      setNotesValidationError("Please describe the required revisions.");
      return;
    }

    setNotesValidationError("");
    setActionLoading(true);
    setActionType(decisionStr.toLowerCase());
    setActionSuccessMessage("");
    setActionErrorMessage("");

    try {
      await makeDecision(selectedRequestId, decisionStr, trimmedNotes);

      let successMsg = "Decision submitted successfully.";
      if (decisionStr === "Approve") successMsg = "Export request approved successfully.";
      if (decisionStr === "Reject") successMsg = "Export request rejected successfully.";
      if (decisionStr === "RequestRevision") successMsg = "Revision requested successfully.";

      setActionSuccessMessage(successMsg);
      setActiveConfirmation(null);
      setReviewNotes("");

      // Refresh request detail and queue
      const updated = await getRequestDetail(selectedRequestId);
      setRequestDetail(updated.request);
      fetchQueue();
    } catch (err) {
      console.error(`Failed to submit decision (${decisionStr}):`, err);
      handleActionError(err);
    } finally {
      setActionLoading(false);
      setActionType(null);
    }
  };

  // ==========================================
  // ERROR HANDLING HELPER
  // ==========================================
  const handleActionError = (err) => {
    const status = err.response?.status;
    if (status === 401) {
      setActionErrorMessage("Your session has expired. Please sign in again.");
    } else if (status === 403) {
      setActionErrorMessage("You don't have permission to perform this export review action.");
    } else if (status === 404) {
      setActionErrorMessage("This export request could not be found.");
    } else if (status === 409) {
      setActionErrorMessage("The request state has changed. Refreshing the latest information.");
      // Refresh request detail on conflict
      getRequestDetail(selectedRequestId).then((res) => setRequestDetail(res.request)).catch(() => {});
    } else if (status === 400 && err.response?.data?.message) {
      setActionErrorMessage(err.response.data.message);
    } else {
      setActionErrorMessage("We couldn't complete the review action. Please try again.");
    }
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

  // Search, Filter, Sort & Pagination state
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [sortOption, setSortOption] = useState("newest");
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10;

  // Calculate Summary Metric Counts
  const totalCount = queue.length;
  const submittedCount = queue.filter((r) => r.status === "Submitted").length;
  const underReviewCount = queue.filter(
    (r) => r.status === "UnderComplianceReview" || r.status === "UnderOfficerReview"
  ).length;
  const revisionCount = queue.filter((r) => r.status === "RevisionRequired").length;
  const approvedCount = queue.filter((r) => r.status === "Approved").length;

  // Filter & Sort Logic
  const filteredAndSortedQueue = queue.filter((req) => {
    // Search matching: ID, requester full name, email
    if (searchTerm.trim()) {
      const term = searchTerm.trim().toLowerCase();
      const idMatch = (req.id || "").toLowerCase().includes(term);
      const nameMatch = (req.requesterName || "").toLowerCase().includes(term);
      const emailMatch = (req.requesterEmail || "").toLowerCase().includes(term);
      if (!idMatch && !nameMatch && !emailMatch) return false;
    }

    // Status filter matching
    if (statusFilter !== "all" && req.status !== statusFilter) {
      return false;
    }

    return true;
  }).sort((a, b) => {
    if (sortOption === "newest") {
      const dateA = new Date(a.submittedAt || a.createdAt || 0);
      const dateB = new Date(b.submittedAt || b.createdAt || 0);
      return dateB - dateA;
    }
    if (sortOption === "oldest") {
      const dateA = new Date(a.submittedAt || a.createdAt || 0);
      const dateB = new Date(b.submittedAt || b.createdAt || 0);
      return dateA - dateB;
    }
    if (sortOption === "value-high-low") {
      return (b.declaredValue || 0) - (a.declaredValue || 0);
    }
    if (sortOption === "value-low-high") {
      return (a.declaredValue || 0) - (b.declaredValue || 0);
    }
    if (sortOption === "requester-az") {
      return (a.requesterName || "").localeCompare(b.requesterName || "");
    }
    return 0;
  });

  // Pagination calculation
  const totalPages = Math.ceil(filteredAndSortedQueue.length / itemsPerPage) || 1;
  const startIndex = (currentPage - 1) * itemsPerPage;
  const paginatedQueue = filteredAndSortedQueue.slice(startIndex, startIndex + itemsPerPage);

  // Handlers for controls
  const handleSearchChange = (e) => {
    setSearchTerm(e.target.value);
    setCurrentPage(1);
  };

  const handleStatusFilterChange = (e) => {
    setStatusFilter(e.target.value);
    setCurrentPage(1);
  };

  const handleSortChange = (e) => {
    setSortOption(e.target.value);
    setCurrentPage(1);
  };

  const handleCardClick = (statusValue) => {
    if (statusFilter === statusValue) {
      setStatusFilter("all");
    } else {
      setStatusFilter(statusValue);
    }
    setCurrentPage(1);
  };

  const clearFilters = () => {
    setSearchTerm("");
    setStatusFilter("all");
    setSortOption("newest");
    setCurrentPage(1);
  };

  // Status Label Helper
  const getStatusLabel = (status) => {
    if (!status) return "";
    switch (status) {
      case "Submitted":
        return "Submitted";
      case "UnderComplianceReview":
        return "Under Compliance Review";
      case "UnderOfficerReview":
        return "Under Officer Review";
      case "RevisionRequired":
      case "RevisionRequested":
        return "Revision Required";
      case "Approved":
        return "Approved";
      case "Rejected":
        return "Rejected";
      default:
        return status;
    }
  };

  // Status Badge Helper
  const renderStatusBadge = (status) => {
    if (!status) return null;
    const cleanStatus = status.toLowerCase();
    const label = getStatusLabel(status);
    return <span className={`badge badge-${cleanStatus}`}>{label}</span>;
  };

  // Actor Badge Helper
  const renderActorBadge = (actorType) => {
    if (!actorType) return null;
    const cleanActor = actorType.toLowerCase();
    return <span className={`badge badge-${cleanActor}`}>{actorType}</span>;
  };

  return (
    <DashboardLayout
      title="Export Officer Dashboard"
      subtitle="Review export compliance requests and AI-assisted assessments."
    >
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
          <div className="alert-box alert-error" role="alert" aria-live="assertive">
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
              <div className="alert-box alert-error" role="alert" aria-live="assertive">
                <p>{queueError}</p>
                <button className="btn-retry" onClick={fetchQueue}>
                  Retry
                </button>
              </div>
            )}

            {!loadingQueue && !queueError && (
              <>
                {/* SUMMARY METRIC CARDS */}
                <div className="summary-cards-grid">
                  <div
                    className={`summary-card ${statusFilter === "all" ? "active" : ""}`}
                    onClick={() => handleCardClick("all")}
                    title="Click to view all requests"
                  >
                    <span className="card-title">Total Requests</span>
                    <span className="card-value">{totalCount}</span>
                  </div>

                  <div
                    className={`summary-card ${statusFilter === "Submitted" ? "active" : ""}`}
                    onClick={() => handleCardClick("Submitted")}
                    title="Click to filter by Submitted"
                  >
                    <span className="card-title">Submitted</span>
                    <span className="card-value" style={{ color: "#2563eb" }}>
                      {submittedCount}
                    </span>
                  </div>

                  <div
                    className={`summary-card ${
                      statusFilter === "UnderOfficerReview" || statusFilter === "UnderComplianceReview"
                        ? "active"
                        : ""
                    }`}
                    onClick={() => handleCardClick("UnderOfficerReview")}
                    title="Click to filter by Under Officer Review"
                  >
                    <span className="card-title">Under Review</span>
                    <span className="card-value" style={{ color: "#d97706" }}>
                      {underReviewCount}
                    </span>
                  </div>

                  <div
                    className={`summary-card ${statusFilter === "RevisionRequired" ? "active" : ""}`}
                    onClick={() => handleCardClick("RevisionRequired")}
                    title="Click to filter by Revision Required"
                  >
                    <span className="card-title">Revision Required</span>
                    <span className="card-value" style={{ color: "#ca8a04" }}>
                      {revisionCount}
                    </span>
                  </div>

                  <div
                    className={`summary-card ${statusFilter === "Approved" ? "active" : ""}`}
                    onClick={() => handleCardClick("Approved")}
                    title="Click to filter by Approved"
                  >
                    <span className="card-title">Approved</span>
                    <span className="card-value" style={{ color: "#059669" }}>
                      {approvedCount}
                    </span>
                  </div>
                </div>

                {/* CONTROLS BAR: SEARCH, FILTERS & SORTING */}
                <div className="controls-bar">
                  <div className="search-input-wrapper">
                    <input
                      type="text"
                      className="search-input"
                      placeholder="Search by Request ID, Requester Name, or Email..."
                      value={searchTerm}
                      onChange={handleSearchChange}
                    />
                  </div>

                  <div className="filters-wrapper">
                    {/* Status Filter */}
                    <select
                      className="filter-select"
                      value={statusFilter}
                      onChange={handleStatusFilterChange}
                      aria-label="Filter by Status"
                    >
                      <option value="all">All Statuses</option>
                      <option value="Submitted">Submitted</option>
                      <option value="UnderComplianceReview">Under Compliance Review</option>
                      <option value="UnderOfficerReview">Under Officer Review</option>
                      <option value="RevisionRequired">Revision Required</option>
                      <option value="Approved">Approved</option>
                      <option value="Rejected">Rejected</option>
                    </select>

                    {/* Sorting Dropdown */}
                    <select
                      className="filter-select"
                      value={sortOption}
                      onChange={handleSortChange}
                      aria-label="Sort requests"
                    >
                      <option value="newest">Newest Submitted</option>
                      <option value="oldest">Oldest Submitted</option>
                      <option value="value-high-low">Declared Value: High to Low</option>
                      <option value="value-low-high">Declared Value: Low to High</option>
                      <option value="requester-az">Requester Name A-Z</option>
                    </select>
                  </div>
                </div>

                {/* EMPTY QUEUE STATE */}
                {queue.length === 0 && (
                  <div className="alert-box alert-info">
                    No export requests are currently waiting for review.
                  </div>
                )}

                {/* EMPTY FILTERED STATE */}
                {queue.length > 0 && filteredAndSortedQueue.length === 0 && (
                  <div className="empty-filtered-state">
                    <p>No export requests match the selected filters.</p>
                    <button className="btn-retry" style={{ marginTop: "12px" }} onClick={clearFilters}>
                      Clear Filters
                    </button>
                  </div>
                )}

                {/* REQUESTS TABLE */}
                {filteredAndSortedQueue.length > 0 && (
                  <div className="queue-card">
                    <div className="table-responsive">
                      <table className="queue-table">
                        <thead>
                          <tr>
                            <th>Request</th>
                            <th>Requester</th>
                            <th>Route</th>
                            <th>Declared Value</th>
                            <th>Status</th>
                            <th>Submitted</th>
                            <th>Docs</th>
                            <th>Action</th>
                          </tr>
                        </thead>
                        <tbody>
                          {paginatedQueue.map((req) => {
                            const isReviewable =
                              req.status === "Submitted" ||
                              req.status === "UnderComplianceReview" ||
                              req.status === "UnderOfficerReview";

                            return (
                              <tr
                                key={req.id}
                                className="queue-row"
                                onClick={() => handleSelectRequest(req.id)}
                              >
                                <td>
                                  <code title={req.id}>
                                    {req.id ? req.id.substring(0, 8) + "..." : "-"}
                                  </code>
                                </td>
                                <td>
                                  <div className="requester-name">{req.requesterName || "N/A"}</div>
                                  <div className="requester-email">{req.requesterEmail || ""}</div>
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
                                <td>
                                  <button
                                    className={`btn-table-action ${isReviewable ? "btn-review" : "btn-view"}`}
                                    onClick={(e) => {
                                      e.stopPropagation();
                                      handleSelectRequest(req.id);
                                    }}
                                  >
                                    {isReviewable ? "Review" : "View"}
                                  </button>
                                </td>
                              </tr>
                            );
                          })}
                        </tbody>
                      </table>
                    </div>

                    {/* PAGINATION CONTROLS */}
                    <div className="pagination-bar">
                      <div className="pagination-info">
                        Showing {startIndex + 1}–
                        {Math.min(startIndex + itemsPerPage, filteredAndSortedQueue.length)} of{" "}
                        {filteredAndSortedQueue.length} request{filteredAndSortedQueue.length === 1 ? "" : "s"} (Page {currentPage} of {totalPages})
                      </div>

                      <div className="pagination-buttons">
                        <button
                          className="btn-page"
                          disabled={currentPage === 1}
                          onClick={() => setCurrentPage((prev) => Math.max(prev - 1, 1))}
                        >
                          &larr; Previous
                        </button>
                        <button
                          className="btn-page"
                          disabled={currentPage >= totalPages}
                          onClick={() => setCurrentPage((prev) => Math.min(prev + 1, totalPages))}
                        >
                          Next &rarr;
                        </button>
                      </div>
                    </div>
                  </div>
                )}
              </>
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
              <div className="alert-box alert-error" role="alert" aria-live="assertive">
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
                {/* GLOBAL ACTION SUCCESS / ERROR FEEDBACK */}
                {actionSuccessMessage && (
                  <div className="alert-box alert-success" role="status" aria-live="polite">
                    {actionSuccessMessage}
                  </div>
                )}
                {actionErrorMessage && (
                  <div className="alert-box alert-error" role="alert" aria-live="assertive">
                    {actionErrorMessage}
                  </div>
                )}

                {/* ==========================================
                    AUTHORITATIVE EXPORT OFFICER DECISION CONTROL SECTION
                ========================================== */}
                <div className="section-card decision-card">
                  <h3>Export Officer Decision</h3>

                  {/* CASE 1: REVIEW NOT STARTED YET (Submitted or UnderComplianceReview) */}
                  {(requestDetail.status === "Submitted" || requestDetail.status === "UnderComplianceReview") && (
                    <div>
                      <p className="subtitle" style={{ marginBottom: "12px" }}>
                        Begin the human review before making a final export decision.
                      </p>
                      <button
                        className="btn-action btn-start-review"
                        disabled={actionLoading}
                        onClick={handleStartReview}
                      >
                        {actionLoading && actionType === "start" ? "Starting review..." : "Start Review"}
                      </button>
                    </div>
                  )}

                  {/* CASE 2: UNDER OFFICER REVIEW (Active Decision Buttons) */}
                  {requestDetail.status === "UnderOfficerReview" && (
                    <div>
                      <p className="subtitle" style={{ marginBottom: "14px" }}>
                        AI findings are advisory. The final decision is made by the authorized Export Officer.
                      </p>

                      {/* BUTTON ROW */}
                      {!activeConfirmation && (
                        <div className="decision-actions">
                          <button
                            className="btn-action btn-approve"
                            disabled={actionLoading}
                            onClick={() => {
                              setActiveConfirmation("approve");
                              setNotesValidationError("");
                            }}
                          >
                            Approve
                          </button>
                          <button
                            className="btn-action btn-reject"
                            disabled={actionLoading}
                            onClick={() => {
                              setActiveConfirmation("reject");
                              setNotesValidationError("");
                            }}
                          >
                            Reject
                          </button>
                          <button
                            className="btn-action btn-revision"
                            disabled={actionLoading}
                            onClick={() => {
                              setActiveConfirmation("revision");
                              setNotesValidationError("");
                            }}
                          >
                            Request Revision
                          </button>
                        </div>
                      )}

                      {/* CONFIRMATION UI — APPROVE */}
                      {activeConfirmation === "approve" && (
                        <div className="confirmation-box">
                          <h4>Approve this export request?</h4>
                          <p className="subtitle">
                            This records your final human approval for this compliance review.
                          </p>

                          <div className="form-group">
                            <label htmlFor="approve-notes">Approval Notes (Optional)</label>
                            <textarea
                              id="approve-notes"
                              placeholder="Add optional notes for the approval decision..."
                              value={reviewNotes}
                              onChange={(e) => setReviewNotes(e.target.value)}
                              disabled={actionLoading}
                            />
                          </div>

                          <div className="decision-actions">
                            <button
                              className="btn-action btn-approve"
                              disabled={actionLoading}
                              onClick={() => handleConfirmDecision("Approve")}
                            >
                              {actionLoading && actionType === "approve"
                                ? "Approving..."
                                : "Approve Request"}
                            </button>
                            <button
                              className="btn-action btn-cancel"
                              disabled={actionLoading}
                              onClick={() => setActiveConfirmation(null)}
                            >
                              Cancel
                            </button>
                          </div>
                        </div>
                      )}

                      {/* CONFIRMATION UI — REJECT */}
                      {activeConfirmation === "reject" && (
                        <div className="confirmation-box">
                          <h4>Reject this export request?</h4>

                          <div className="form-group">
                            <label htmlFor="reject-notes">
                              Reason for rejection <span style={{ color: "#ef4444" }}>*</span>
                            </label>
                            <textarea
                              id="reject-notes"
                              placeholder="Explain why this export request cannot proceed."
                              value={reviewNotes}
                              onChange={(e) => {
                                setReviewNotes(e.target.value);
                                if (e.target.value.trim()) setNotesValidationError("");
                              }}
                              disabled={actionLoading}
                              required
                            />
                            {notesValidationError && (
                              <span style={{ color: "#ef4444", fontSize: "13px" }}>
                                {notesValidationError}
                              </span>
                            )}
                          </div>

                          <div className="decision-actions">
                            <button
                              className="btn-action btn-reject"
                              disabled={actionLoading}
                              onClick={() => handleConfirmDecision("Reject")}
                            >
                              {actionLoading && actionType === "reject"
                                ? "Rejecting..."
                                : "Reject Request"}
                            </button>
                            <button
                              className="btn-action btn-cancel"
                              disabled={actionLoading}
                              onClick={() => setActiveConfirmation(null)}
                            >
                              Cancel
                            </button>
                          </div>
                        </div>
                      )}

                      {/* CONFIRMATION UI — REQUEST REVISION */}
                      {activeConfirmation === "revision" && (
                        <div className="confirmation-box">
                          <h4>Request Revision for this export request?</h4>

                          <div className="form-group">
                            <label htmlFor="revision-notes">
                              Revision instructions <span style={{ color: "#ef4444" }}>*</span>
                            </label>
                            <textarea
                              id="revision-notes"
                              placeholder="Explain what information or documents must be corrected or provided."
                              value={reviewNotes}
                              onChange={(e) => {
                                setReviewNotes(e.target.value);
                                if (e.target.value.trim()) setNotesValidationError("");
                              }}
                              disabled={actionLoading}
                              required
                            />
                            {notesValidationError && (
                              <span style={{ color: "#ef4444", fontSize: "13px" }}>
                                {notesValidationError}
                              </span>
                            )}
                          </div>

                          <div className="decision-actions">
                            <button
                              className="btn-action btn-revision"
                              disabled={actionLoading}
                              onClick={() => handleConfirmDecision("RequestRevision")}
                            >
                              {actionLoading && actionType === "requestrevision"
                                ? "Requesting revision..."
                                : "Send Revision Request"}
                            </button>
                            <button
                              className="btn-action btn-cancel"
                              disabled={actionLoading}
                              onClick={() => setActiveConfirmation(null)}
                            >
                              Cancel
                            </button>
                          </div>
                        </div>
                      )}
                    </div>
                  )}

                  {/* CASE 3: COMPLETED DECISION STATE (Approved, Rejected, RevisionRequired) */}
                  {(requestDetail.status === "Approved" ||
                    requestDetail.status === "Rejected" ||
                    requestDetail.status === "RevisionRequired") && (
                    <div>
                      <div style={{ marginBottom: "14px", display: "flex", alignItems: "center", gap: "10px" }}>
                        <span className="info-label">Final Outcome:</span>
                        {requestDetail.status === "Approved" && (
                          <span className="badge badge-approved">Decision: Approved</span>
                        )}
                        {requestDetail.status === "Rejected" && (
                          <span className="badge badge-rejected">Decision: Rejected</span>
                        )}
                        {requestDetail.status === "RevisionRequired" && (
                          <span className="badge badge-revisionrequired">Decision: Revision Requested</span>
                        )}
                      </div>

                      <div className="info-grid">
                        <div className="info-item">
                          <span className="info-label">Reviewed At</span>
                          <span className="info-value">
                            {requestDetail.reviewedAt
                              ? new Date(requestDetail.reviewedAt).toLocaleString()
                              : "N/A"}
                          </span>
                        </div>
                        {requestDetail.reviewNotes && (
                          <div className="info-item" style={{ gridColumn: "1 / -1" }}>
                            <span className="info-label">Officer Review Notes</span>
                            <span className="info-value">{requestDetail.reviewNotes}</span>
                          </div>
                        )}
                      </div>
                    </div>
                  )}
                </div>

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