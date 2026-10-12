import { useEffect, useState } from "react";
import DashboardLayout from "../layouts/DashboardLayout";
import {
  getReviewQueue,
  getRequestDetail,
  startReview,
  makeDecision,
  downloadComplianceDocument,
} from "../services/exportOfficerService";
import {
  ShieldCheck,
  FileText,
  Clock,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  Send,
  Search,
  RotateCcw,
  ArrowLeft,
  Sparkles,
  Eye,
  Download,
  Package,
  Globe,
  FileCheck,
  AlertCircle,
  File,
  ChevronDown,
  ChevronUp,
} from "lucide-react";
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

  // Document action & error states
  const [documentActionState, setDocumentActionState] = useState({}); // { [docId]: 'viewing' | 'downloading' }
  const [documentErrorMessage, setDocumentErrorMessage] = useState("");

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
    setDocumentActionState({});
    setDocumentErrorMessage("");

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
    setDocumentActionState({});
    setDocumentErrorMessage("");
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
      await fetchQueue();
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
      await fetchQueue();
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
      getRequestDetail(selectedRequestId).then((res) => setRequestDetail(res.request)).catch(() => {});
    } else if (status === 400 && err.response?.data?.message) {
      setActionErrorMessage(err.response.data.message);
    } else {
      setActionErrorMessage("We couldn't complete the review action. Please try again.");
    }
  };

  // ==========================================
  // VIEW / DOWNLOAD DOCUMENT HANDLERS
  // ==========================================
  const handleViewDocument = async (doc) => {
    if (!selectedRequestId || !doc || !doc.id) return;

    setDocumentErrorMessage("");
    setDocumentActionState((prev) => ({ ...prev, [doc.id]: "viewing" }));

    try {
      const blob = await downloadComplianceDocument(selectedRequestId, doc.id);
      const objectUrl = window.URL.createObjectURL(blob);
      const newTab = window.open(objectUrl, "_blank");

      if (!newTab) {
        setDocumentErrorMessage("Pop-up window was blocked. Please allow pop-ups for this site to view the document.");
      }

      setTimeout(() => {
        window.URL.revokeObjectURL(objectUrl);
      }, 60000);
    } catch (err) {
      console.error("Failed to view compliance document:", err);
      handleDocumentError(err);
    } finally {
      setDocumentActionState((prev) => ({ ...prev, [doc.id]: null }));
    }
  };

  const handleDownloadDocument = async (doc) => {
    if (!selectedRequestId || !doc || !doc.id) return;

    setDocumentErrorMessage("");
    setDocumentActionState((prev) => ({ ...prev, [doc.id]: "downloading" }));

    try {
      const blob = await downloadComplianceDocument(selectedRequestId, doc.id);

      let extension = "";
      if (blob.type === "application/pdf") extension = ".pdf";
      else if (blob.type === "image/png") extension = ".png";
      else if (blob.type === "image/jpeg") extension = ".jpeg";

      const docTypeClean = (doc.documentType || "document").toLowerCase().replace(/[^a-z0-9]/g, "_");
      const docNumClean = (doc.documentNumber || doc.id.substring(0, 8)).toLowerCase().replace(/[^a-z0-9]/g, "_");
      const safeFileName = `${docTypeClean}_${docNumClean}${extension}`;

      const objectUrl = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = objectUrl;
      link.download = safeFileName;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);

      setTimeout(() => {
        window.URL.revokeObjectURL(objectUrl);
      }, 1000);
    } catch (err) {
      console.error("Failed to download compliance document:", err);
      handleDocumentError(err);
    } finally {
      setDocumentActionState((prev) => ({ ...prev, [doc.id]: null }));
    }
  };

  const handleDocumentError = (err) => {
    const status = err.response?.status;
    if (status === 401) {
      setDocumentErrorMessage("Your session has expired. Please sign in again.");
    } else if (status === 403) {
      setDocumentErrorMessage("You are not authorized to access this compliance document.");
    } else if (status === 404) {
      setDocumentErrorMessage("The requested compliance document file was not found.");
    } else {
      setDocumentErrorMessage(err.response?.data?.message || "Unable to load this compliance document. Please try again.");
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
  const revisionCount = queue.filter(
    (r) => r.status === "RevisionRequired" || r.status === "RevisionRequested"
  ).length;
  const approvedCount = queue.filter((r) => r.status === "Approved").length;
  const rejectedCount = queue.filter((r) => r.status === "Rejected").length;

  // Filter & Sort Logic
  const filteredAndSortedQueue = queue.filter((req) => {
    // Search matching: ID, requester name, email, gem title, order, destination
    if (searchTerm.trim()) {
      const term = searchTerm.trim().toLowerCase();
      const idMatch = (req.id || "").toLowerCase().includes(term);
      const nameMatch = (req.requesterName || "").toLowerCase().includes(term);
      const emailMatch = (req.requesterEmail || "").toLowerCase().includes(term);
      const gemMatch = (req.gemListingTitle || req.gemDescription || "").toLowerCase().includes(term);
      const orderMatch = (req.orderNumber || "").toLowerCase().includes(term);
      const destMatch = (req.destinationCountry || "").toLowerCase().includes(term);
      const origMatch = (req.originCountry || "").toLowerCase().includes(term);
      if (!idMatch && !nameMatch && !emailMatch && !gemMatch && !orderMatch && !destMatch && !origMatch) {
        return false;
      }
    }

    // Status filter matching
    if (statusFilter !== "all") {
      if (statusFilter === "underReview") {
        if (req.status !== "UnderComplianceReview" && req.status !== "UnderOfficerReview") {
          return false;
        }
      } else if (statusFilter === "RevisionRequired") {
        if (req.status !== "RevisionRequired" && req.status !== "RevisionRequested") {
          return false;
        }
      } else if (req.status !== statusFilter) {
        return false;
      }
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
      case "Draft":
        return "Draft";
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

  // Document Status Badge Helper
  const renderDocStatusBadge = (doc) => {
    const statusRaw = (doc.effectiveStatus || doc.status || "").toLowerCase();
    let badgeClass = "badge-draft";
    let label = doc.effectiveStatus || doc.status || "Unknown";

    if (statusRaw.includes("valid") && !statusRaw.includes("invalid")) {
      badgeClass = "badge-valid";
      label = "Valid";
    } else if (statusRaw.includes("expired")) {
      badgeClass = "badge-expired";
      label = "Expired";
    } else if (statusRaw.includes("invalid")) {
      badgeClass = "badge-invalid";
      label = "Invalid";
    } else if (statusRaw.includes("missing")) {
      badgeClass = "badge-missing";
      label = "Missing";
    }

    return (
      <div>
        <span className={`badge ${badgeClass}`}>{label}</span>
        {doc.effectiveStatusReason && (
          <div style={{ fontSize: "11px", color: "#C62828", marginTop: "3px", fontStyle: "italic" }}>
            {doc.effectiveStatusReason}
          </div>
        )}
      </div>
    );
  };

  // Workflow Stages Computation
  const getWorkflowStages = (req) => {
    if (!req) return [];

    // Stage 1: Submitted
    const stage1 = {
      name: "Submitted",
      actor: "Requester",
      status: "completed",
    };

    // Stage 2: Compliance Check
    let stage2Status = "pending";
    if (req.deterministicCompliance) {
      stage2Status = req.deterministicCompliance.isComplete ? "completed" : "failed";
    } else if (req.status !== "Submitted") {
      stage2Status = "completed";
    } else {
      stage2Status = "current";
    }
    const stage2 = {
      name: "Compliance Check",
      actor: "System",
      status: stage2Status,
    };

    // Stage 3: AI Assessment
    let stage3Status = "pending";
    if (req.agentWorkflow?.workflowStatus === "Failed") {
      stage3Status = "failed";
    } else if (req.agentWorkflow?.assessment) {
      stage3Status = "completed";
    } else if (req.status === "UnderComplianceReview") {
      stage3Status = "current";
    } else if (req.status === "Submitted") {
      stage3Status = "pending";
    } else {
      stage3Status = "completed";
    }
    const stage3 = {
      name: "AI Assessment",
      actor: "AI Agent",
      status: stage3Status,
    };

    // Stage 4: Export Officer Review
    let stage4Status = "pending";
    if (req.status === "UnderOfficerReview") {
      stage4Status = "current";
    } else if (["Approved", "Rejected", "RevisionRequired", "RevisionRequested"].includes(req.status)) {
      stage4Status = "completed";
    } else {
      stage4Status = "pending";
    }
    const stage4 = {
      name: "Officer Review",
      actor: "Export Officer",
      status: stage4Status,
    };

    // Stage 5: Final Decision
    let stage5Status = "pending";
    if (req.status === "Approved") {
      stage5Status = "completed";
    } else if (req.status === "Rejected") {
      stage5Status = "failed";
    } else if (["RevisionRequired", "RevisionRequested"].includes(req.status)) {
      stage5Status = "current";
    } else {
      stage5Status = "pending";
    }
    const stage5 = {
      name: "Final Decision",
      actor: "Export Officer",
      status: stage5Status,
    };

    return [stage1, stage2, stage3, stage4, stage5];
  };

  // Summary Cards Configuration
  const summaryCards = [
    {
      key: "all",
      label: "Total Requests",
      count: totalCount,
      icon: FileText,
      metricClass: "metric-total",
    },
    {
      key: "Submitted",
      label: "Submitted",
      count: submittedCount,
      icon: Send,
      metricClass: "metric-submitted",
    },
    {
      key: "underReview",
      label: "Under Review",
      count: underReviewCount,
      icon: Clock,
      metricClass: "metric-under-review",
    },
    {
      key: "RevisionRequired",
      label: "Revision Required",
      count: revisionCount,
      icon: AlertTriangle,
      metricClass: "metric-revision",
    },
    {
      key: "Approved",
      label: "Approved",
      count: approvedCount,
      icon: CheckCircle2,
      metricClass: "metric-approved",
    },
    {
      key: "Rejected",
      label: "Rejected",
      count: rejectedCount,
      icon: XCircle,
      metricClass: "metric-rejected",
    },
  ];

  return (
    <DashboardLayout
      title="Export Officer Dashboard"
      subtitle="Review export compliance requests and AI-assisted assessments."
    >
      <div className="export-officer-container">
        {/* ==========================================
            1. PAGE HEADER
        ========================================== */}
        <div className="dashboard-header-card">
          <div className="dashboard-header-main">
            <div className="header-badge-row">
              <span className="officer-badge">
                <ShieldCheck size={13} />
                EXPORT OFFICER
              </span>
            </div>
            <h1 className="dashboard-title">Export Compliance</h1>
            <p className="dashboard-subtitle">
              Review export requests, compliance findings and AI-assisted assessments.
            </p>
          </div>

          <div className="header-meta-box">
            <div>
              <div className="header-meta-label">Pending Reviews</div>
              <div className="header-meta-value">{underReviewCount + submittedCount} Active</div>
            </div>
          </div>
        </div>

        {/* FORBIDDEN ACCESS STATE */}
        {forbiddenError && (
          <div className="alert-box alert-error" role="alert" aria-live="assertive">
            <AlertCircle size={18} />
            <div>
              <strong>Access Denied:</strong> {queueError}
            </div>
          </div>
        )}

        {/* ==========================================
            VIEW 1: QUEUE LISTING
        ========================================== */}
        {!selectedRequestId && !forbiddenError && (
          <div className="queue-section">
            {loadingQueue && (
              <div className="alert-box alert-info">
                <Clock size={18} />
                <span>Loading export requests...</span>
              </div>
            )}

            {queueError && !loadingQueue && (
              <div className="alert-box alert-error" role="alert" aria-live="assertive">
                <AlertCircle size={18} />
                <div>
                  <p style={{ margin: 0 }}>{queueError}</p>
                  <button className="btn-retry" onClick={fetchQueue}>
                    <RotateCcw size={14} />
                    Retry
                  </button>
                </div>
              </div>
            )}

            {!loadingQueue && !queueError && (
              <>
                {/* 2. SUMMARY METRIC CARDS */}
                <div className="summary-cards-grid">
                  {summaryCards.map((card) => {
                    const CardIcon = card.icon;
                    const isActive =
                      card.key === "all"
                        ? statusFilter === "all"
                        : card.key === "underReview"
                        ? statusFilter === "underReview" ||
                          statusFilter === "UnderOfficerReview" ||
                          statusFilter === "UnderComplianceReview"
                        : statusFilter === card.key;

                    return (
                      <div
                        key={card.key}
                        className={`summary-card ${isActive ? "active" : ""}`}
                        onClick={() => handleCardClick(card.key)}
                        title={`Click to filter by ${card.label}`}
                      >
                        <div className="card-top">
                          <span className="card-title">{card.label}</span>
                          <div className="card-icon-bubble">
                            <CardIcon size={16} />
                          </div>
                        </div>
                        <span className={`card-value ${card.metricClass}`}>{card.count}</span>
                      </div>
                    );
                  })}
                </div>

                {/* 3. SEARCH / FILTER TOOLBAR */}
                <div className="controls-bar">
                  <div className="search-input-wrapper">
                    <Search size={16} className="search-icon-adornment" />
                    <input
                      type="text"
                      className="search-input"
                      placeholder="Search by request, seller, gem or destination..."
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
                      <option value="underReview">Under Review (All)</option>
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

                    {/* Clear Filters Button */}
                    {(Boolean(searchTerm.trim()) || statusFilter !== "all" || sortOption !== "newest") && (
                      <button
                        className="btn-clear-filters"
                        onClick={clearFilters}
                        title="Reset all search, status filter, and sort options"
                      >
                        <RotateCcw size={14} />
                        Clear Filters
                      </button>
                    )}
                  </div>
                </div>

                {/* EMPTY QUEUE STATE */}
                {queue.length === 0 && (
                  <div className="alert-box alert-info">
                    <FileText size={18} />
                    <span>No export requests are currently waiting for review.</span>
                  </div>
                )}

                {/* EMPTY FILTERED STATE */}
                {queue.length > 0 && filteredAndSortedQueue.length === 0 && (
                  <div className="empty-filtered-state">
                    <p style={{ margin: "0 0 12px 0" }}>No export requests match the selected filters.</p>
                    <button className="btn-clear-filters" onClick={clearFilters}>
                      <RotateCcw size={14} />
                      Clear Filters
                    </button>
                  </div>
                )}

                {/* 4. EXPORT REQUEST TABLE */}
                {filteredAndSortedQueue.length > 0 && (
                  <div className="queue-card">
                    <div className="table-responsive">
                      <table className="queue-table">
                        <thead>
                          <tr>
                            <th>Request</th>
                            <th>Seller / Requester</th>
                            <th>Gem / Order</th>
                            <th>Destination</th>
                            <th>Declared Value</th>
                            <th>Status</th>
                            <th>Submitted Date</th>
                            <th>Documents</th>
                            <th>Action</th>
                          </tr>
                        </thead>
                        <tbody>
                          {paginatedQueue.map((req) => {
                            const isReviewable =
                              req.status === "Submitted" ||
                              req.status === "UnderComplianceReview" ||
                              req.status === "UnderOfficerReview";

                            const gemTitle = (req.gemListingTitle || req.gemDescription || "").trim();
                            const orderRef = req.orderNumber ? `Order #${req.orderNumber}` : null;
                            const primaryItemTitle = gemTitle || orderRef || "Export Gemstone";
                            const secondaryItemRef = gemTitle && orderRef ? orderRef : null;

                            return (
                              <tr
                                key={req.id}
                                className="queue-row"
                                onClick={() => handleSelectRequest(req.id)}
                              >
                                <td className="table-id-cell">
                                  <code
                                    className="request-id-chip"
                                    title={req.id ? `Request ID: ${req.id}` : "Request ID"}
                                  >
                                    {req.id ? (req.id.length > 8 ? `${req.id.substring(0, 8)}...` : req.id) : "-"}
                                  </code>
                                </td>
                                <td>
                                  <div className="requester-name">{req.requesterName || "N/A"}</div>
                                  <div className="requester-email">{req.requesterEmail || ""}</div>
                                </td>
                                <td>
                                  <div className="gem-order-title">{primaryItemTitle}</div>
                                  {secondaryItemRef && (
                                    <div className="gem-order-meta">{secondaryItemRef}</div>
                                  )}
                                </td>
                                <td>
                                  <span className="route-text">
                                    <Globe size={13} style={{ color: "var(--gemora-muted)" }} />
                                    {req.destinationCountry}
                                  </span>
                                </td>
                                <td className="value-cell">
                                  {req.declaredValue?.toLocaleString()} {req.currency}
                                </td>
                                <td>{renderStatusBadge(req.status)}</td>
                                <td>
                                  {req.submittedAt
                                    ? new Date(req.submittedAt).toLocaleDateString()
                                    : "-"}
                                </td>
                                <td>
                                  <span className="docs-count-badge">
                                    <File size={12} />
                                    {req.documents?.length || 0}
                                  </span>
                                </td>
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
            VIEW 2: REQUEST DETAIL PAGE
        ========================================== */}
        {selectedRequestId && (
          <div className="detail-section">
            <button className="btn-back" onClick={handleBackToQueue}>
              <ArrowLeft size={16} />
              Back to Review Queue
            </button>

            {loadingDetail && (
              <div className="alert-box alert-info">
                <Clock size={18} />
                <span>Loading export request details...</span>
              </div>
            )}

            {detailError && !loadingDetail && (
              <div className="alert-box alert-error" role="alert" aria-live="assertive">
                <AlertCircle size={18} />
                <div>
                  <p style={{ margin: 0 }}>{detailError}</p>
                  <button
                    className="btn-retry"
                    onClick={() => handleSelectRequest(selectedRequestId)}
                  >
                    <RotateCcw size={14} />
                    Retry
                  </button>
                </div>
              </div>
            )}

            {!loadingDetail && !detailError && requestDetail && (
              <div className="detail-grid">
                {/* GLOBAL ACTION SUCCESS / ERROR FEEDBACK */}
                {actionSuccessMessage && (
                  <div className="alert-box alert-success" role="status" aria-live="polite">
                    <CheckCircle2 size={18} />
                    <span>{actionSuccessMessage}</span>
                  </div>
                )}
                {actionErrorMessage && (
                  <div className="alert-box alert-error" role="alert" aria-live="assertive">
                    <AlertCircle size={18} />
                    <span>{actionErrorMessage}</span>
                  </div>
                )}

                {/* 5. TOP SUMMARY BANNER */}
                <div className="detail-hero-card">
                  <div className="hero-left">
                    <div className="hero-request-id-row">
                      <code>{requestDetail.id}</code>
                      {renderStatusBadge(requestDetail.status)}
                    </div>
                    <div className="hero-route">
                      <Globe size={15} />
                      Destination: {requestDetail.destinationCountry}
                    </div>
                  </div>

                  <div className="hero-right">
                    <div className="hero-metric-item">
                      <span className="hero-metric-label">Declared Value</span>
                      <span className="hero-metric-value">
                        {requestDetail.declaredValue?.toLocaleString()}{" "}
                        <span style={{ fontSize: "14px", fontWeight: 600 }}>{requestDetail.currency}</span>
                      </span>
                    </div>

                    <div className="hero-metric-item">
                      <span className="hero-metric-label">Submitted On</span>
                      <span style={{ fontSize: "14px", fontWeight: 600, color: "var(--gemora-dark-green)" }}>
                        {requestDetail.submittedAt
                          ? new Date(requestDetail.submittedAt).toLocaleDateString()
                          : "N/A"}
                      </span>
                    </div>
                  </div>
                </div>

                {/* 9 & 10. EXPORT OFFICER DECISION SECTION (MOST IMPORTANT AREA) */}
                <div className="section-card decision-card">
                  <h3>
                    <span style={{ display: "flex", alignitems: "center", gap: "8px" }}>
                      <ShieldCheck size={18} style={{ color: "var(--gemora-dark-gold)" }} />
                      Export Officer Decision
                    </span>
                    {requestDetail.status === "UnderOfficerReview" && (
                      <span className="badge badge-underofficerreview">Active Review</span>
                    )}
                  </h3>

                  {/* CASE 1: REVIEW NOT STARTED YET */}
                  {(requestDetail.status === "Submitted" || requestDetail.status === "UnderComplianceReview") && (
                    <div>
                      <h4 style={{ margin: "0 0 6px 0", color: "var(--gemora-dark-green)" }}>
                        Start Human Review
                      </h4>
                      <p className="dashboard-subtitle" style={{ marginBottom: "14px" }}>
                        Begin the human review before making a final export decision.
                      </p>
                      <button
                        className="btn-action btn-start-review"
                        disabled={actionLoading}
                        onClick={handleStartReview}
                      >
                        <ShieldCheck size={16} />
                        {actionLoading && actionType === "start" ? "Starting review..." : "Start Review"}
                      </button>
                    </div>
                  )}

                  {/* CASE 2: UNDER OFFICER REVIEW (Active Decision Buttons) */}
                  {requestDetail.status === "UnderOfficerReview" && (
                    <div>
                      <p className="dashboard-subtitle" style={{ marginBottom: "14px" }}>
                        AI findings are advisory only. The final export decision must be made by an authorized Export Officer.
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
                            <CheckCircle2 size={16} />
                            Approve
                          </button>
                          <button
                            className="btn-action btn-revision"
                            disabled={actionLoading}
                            onClick={() => {
                              setActiveConfirmation("revision");
                              setNotesValidationError("");
                            }}
                          >
                            <AlertTriangle size={16} />
                            Request Revision
                          </button>
                          <button
                            className="btn-action btn-reject"
                            disabled={actionLoading}
                            onClick={() => {
                              setActiveConfirmation("reject");
                              setNotesValidationError("");
                            }}
                          >
                            <XCircle size={16} />
                            Reject
                          </button>
                        </div>
                      )}

                      {/* CONFIRMATION UI — APPROVE */}
                      {activeConfirmation === "approve" && (
                        <div className="confirmation-box">
                          <h4>Approve this export request?</h4>
                          <p className="dashboard-subtitle" style={{ margin: 0 }}>
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
                              <CheckCircle2 size={16} />
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
                          <p className="dashboard-subtitle" style={{ margin: 0 }}>
                            Explain why this export request cannot proceed.
                          </p>

                          <div className="form-group">
                            <label htmlFor="reject-notes">
                              Reason for rejection <span style={{ color: "var(--status-rejected)" }}>*</span>
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
                              <span style={{ color: "var(--status-rejected)", fontSize: "13px" }}>
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
                              <XCircle size={16} />
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
                          <h4>Request changes from the seller?</h4>
                          <p className="dashboard-subtitle" style={{ margin: 0 }}>
                            Explain what information or documents must be corrected or provided.
                          </p>

                          <div className="form-group">
                            <label htmlFor="revision-notes">
                              Revision instructions <span style={{ color: "var(--status-rejected)" }}>*</span>
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
                              <span style={{ color: "var(--status-rejected)", fontSize: "13px" }}>
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
                              <AlertTriangle size={16} />
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

                  {/* CASE 3: FINAL DECISION STATES (10. FINAL DECISION STATES) */}
                  {(requestDetail.status === "Approved" ||
                    requestDetail.status === "Rejected" ||
                    requestDetail.status === "RevisionRequired" ||
                    requestDetail.status === "RevisionRequested") && (
                    <div
                      className={`final-decision-panel ${
                        requestDetail.status === "Approved"
                          ? "decision-approved"
                          : requestDetail.status === "Rejected"
                          ? "decision-rejected"
                          : "decision-revision"
                      }`}
                    >
                      <div className="final-panel-header">
                        <span className="info-label" style={{ fontSize: "12px" }}>
                          Final Outcome:
                        </span>
                        {requestDetail.status === "Approved" && (
                          <span className="badge badge-approved">
                            <CheckCircle2 size={13} />
                            Decision: Approved
                          </span>
                        )}
                        {requestDetail.status === "Rejected" && (
                          <span className="badge badge-rejected">
                            <XCircle size={13} />
                            Decision: Rejected
                          </span>
                        )}
                        {(requestDetail.status === "RevisionRequired" ||
                          requestDetail.status === "RevisionRequested") && (
                          <span className="badge badge-revisionrequired">
                            <AlertTriangle size={13} />
                            Decision: Revision Requested
                          </span>
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
                            <span className="info-value" style={{ fontStyle: "italic" }}>
                              {requestDetail.reviewNotes}
                            </span>
                          </div>
                        )}
                      </div>
                    </div>
                  )}
                </div>

                {/* 6. WORKFLOW TIMELINE */}
                <div className="section-card">
                  <h3>
                    <span>Workflow Pipeline</span>
                    <span className="info-label">Standard Compliance Lifecycle</span>
                  </h3>

                  <div className="workflow-tracker">
                    {getWorkflowStages(requestDetail).map((stage, sIdx) => {
                      const stateClass =
                        stage.status === "completed"
                          ? "step-node-completed"
                          : stage.status === "current"
                          ? "step-node-current"
                          : stage.status === "failed"
                          ? "step-node-failed"
                          : "step-node-pending";

                      return (
                        <div key={sIdx} className={`workflow-step-node ${stateClass}`}>
                          <div className="step-node-line" />
                          <div className="step-node-bubble">
                            {stage.status === "completed" ? (
                              <CheckCircle2 size={18} />
                            ) : stage.status === "failed" ? (
                              <XCircle size={18} />
                            ) : (
                              sIdx + 1
                            )}
                          </div>
                          <div className="step-node-title">{stage.name}</div>
                          <div className="step-node-actor">{stage.actor}</div>
                        </div>
                      );
                    })}
                  </div>
                </div>

                {/* 5A & 5B: TWO-COLUMN DETAILS GRID */}
                <div className="detail-grid-two-col">
                  {/* CARD A: MARKETPLACE ORDER */}
                  <div className="section-card">
                    <h3>
                      <span style={{ display: "flex", alignItems: "center", gap: "8px" }}>
                        <Package size={17} style={{ color: "var(--gemora-dark-green)" }} />
                        Marketplace Order
                      </span>
                    </h3>
                    <div className="info-grid">
                      <div className="info-item">
                        <span className="info-label">Order ID</span>
                        <span className="info-value">
                          <code>{requestDetail.orderNumber || "Standard Export Order"}</code>
                        </span>
                      </div>
                      <div className="info-item">
                        <span className="info-label">Seller</span>
                        <span className="info-value">{requestDetail.requesterName}</span>
                      </div>
                      <div className="info-item">
                        <span className="info-label">Buyer</span>
                        <span className="info-value">
                          {requestDetail.buyerName || requestDetail.buyerEmail || "International Buyer"}
                        </span>
                      </div>
                      <div className="info-item">
                        <span className="info-label">Gem Title</span>
                        <span className="info-value">
                          {requestDetail.gemListingTitle || requestDetail.gemDescription || "Ceylon Gemstone"}
                        </span>
                      </div>
                      <div className="info-item">
                        <span className="info-label">Price / Declared</span>
                        <span className="info-value">
                          {requestDetail.declaredValue?.toLocaleString()} {requestDetail.currency}
                        </span>
                      </div>
                      <div className="info-item">
                        <span className="info-label">Order Destination</span>
                        <span className="info-value">Ship to {requestDetail.destinationCountry}</span>
                      </div>
                    </div>
                  </div>

                  {/* CARD B: EXPORT INFORMATION */}
                  <div className="section-card">
                    <h3>
                      <span style={{ display: "flex", alignItems: "center", gap: "8px" }}>
                        <Globe size={17} style={{ color: "var(--gemora-dark-green)" }} />
                        Export Information
                      </span>
                    </h3>
                    <div className="info-grid">
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
                          {requestDetail.declaredValue?.toLocaleString()}
                        </span>
                      </div>
                      <div className="info-item">
                        <span className="info-label">Currency</span>
                        <span className="info-value">{requestDetail.currency}</span>
                      </div>
                      <div className="info-item">
                        <span className="info-label">Submitted Date</span>
                        <span className="info-value">
                          {requestDetail.submittedAt
                            ? new Date(requestDetail.submittedAt).toLocaleString()
                            : "N/A"}
                        </span>
                      </div>
                      <div className="info-item">
                        <span className="info-label">Purpose</span>
                        <span className="info-value">{requestDetail.purpose || "Commercial Export"}</span>
                      </div>
                    </div>
                  </div>
                </div>

                {/* 7. COMPLIANCE DOCUMENTS SECTION */}
                <div className="section-card">
                  <h3>
                    <span style={{ display: "flex", alignItems: "center", gap: "8px" }}>
                      <FileCheck size={18} style={{ color: "var(--gemora-dark-green)" }} />
                      Compliance Documents ({requestDetail.documents?.length || 0})
                    </span>
                  </h3>

                  {documentErrorMessage && (
                    <div className="alert-box alert-error" role="alert" aria-live="assertive">
                      <AlertCircle size={18} />
                      <span>{documentErrorMessage}</span>
                    </div>
                  )}

                  {(!requestDetail.documents || requestDetail.documents.length === 0) ? (
                    <p className="dashboard-subtitle">No compliance documents attached.</p>
                  ) : (
                    <div className="table-responsive">
                      <table className="queue-table">
                        <thead>
                          <tr>
                            <th>Document Type</th>
                            <th>Certificate Number</th>
                            <th>Issuer</th>
                            <th>Issue Date</th>
                            <th>Expiry Date</th>
                            <th>Validation Status</th>
                            <th>File</th>
                            <th>Action</th>
                          </tr>
                        </thead>
                        <tbody>
                          {requestDetail.documents.map((doc) => {
                            const hasFile = Boolean(doc.fileUrl);
                            const isViewing = documentActionState[doc.id] === "viewing";
                            const isDownloading = documentActionState[doc.id] === "downloading";
                            const isBusy = isViewing || isDownloading;

                            return (
                              <tr key={doc.id}>
                                <td>
                                  <strong>{doc.documentType || "Not provided"}</strong>
                                </td>
                                <td>{doc.documentNumber || "Not provided"}</td>
                                <td>{doc.issuer || "Not provided"}</td>
                                <td>
                                  {doc.issueDate
                                    ? new Date(doc.issueDate).toLocaleDateString()
                                    : "Not provided"}
                                </td>
                                <td>
                                  {doc.expiryDate
                                    ? new Date(doc.expiryDate).toLocaleDateString()
                                    : "Not provided"}
                                </td>
                                <td>{renderDocStatusBadge(doc)}</td>
                                <td>
                                  {hasFile ? (
                                    <span className="badge badge-approved" style={{ fontSize: "11px", padding: "2px 8px" }}>
                                      Attached
                                    </span>
                                  ) : (
                                    <span className="badge badge-system" style={{ fontSize: "11px", padding: "2px 8px" }}>
                                      No File
                                    </span>
                                  )}
                                </td>
                                <td>
                                  {hasFile ? (
                                    <div className="doc-actions-cell">
                                      <button
                                        className="btn-doc-action btn-doc-view"
                                        disabled={isBusy}
                                        onClick={() => handleViewDocument(doc)}
                                      >
                                        <Eye size={13} />
                                        {isViewing ? "Opening..." : "View Document"}
                                      </button>
                                      <button
                                        className="btn-doc-action btn-doc-download"
                                        disabled={isBusy}
                                        onClick={() => handleDownloadDocument(doc)}
                                      >
                                        <Download size={13} />
                                        {isDownloading ? "Downloading..." : "Download"}
                                      </button>
                                    </div>
                                  ) : (
                                    <span className="info-label" style={{ fontSize: "12px" }}>
                                      No File Attached
                                    </span>
                                  )}
                                </td>
                              </tr>
                            );
                          })}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>

                {/* DETERMINISTIC COMPLIANCE CHECK */}
                {requestDetail.deterministicCompliance && (
                  <div className="section-card">
                    <h3>
                      <span>Deterministic Compliance Check</span>
                      <div>
                        {requestDetail.deterministicCompliance.isComplete ? (
                          <span className="badge badge-approved">Complete</span>
                        ) : (
                          <span className="badge badge-rejected">Incomplete</span>
                        )}
                      </div>
                    </h3>

                    <div>
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
                          <p className="dashboard-subtitle" style={{ fontSize: "13px", marginTop: "4px" }}>
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
                          <p className="dashboard-subtitle" style={{ fontSize: "13px", marginTop: "4px" }}>
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
                          <p className="dashboard-subtitle" style={{ fontSize: "13px", marginTop: "4px" }}>
                            No deterministic warnings reported.
                          </p>
                        )}
                      </div>
                    </div>
                  </div>
                )}

                {/* 8. AI COMPLIANCE ASSESSMENT (ADVISORY CARD) */}
                <div className="section-card ai-assessment-card">
                  <h3>
                    <span style={{ display: "flex", alignItems: "center", gap: "8px" }}>
                      <Sparkles size={18} style={{ color: "var(--gemora-dark-gold)" }} />
                      AI Compliance Assessment
                    </span>
                    <span className="ai-header-badge">ADVISORY ONLY</span>
                  </h3>

                  {!requestDetail.agentWorkflow ? (
                    <p className="dashboard-subtitle">No automated compliance workflow is available for this request.</p>
                  ) : !requestDetail.agentWorkflow.assessment ? (
                    <p className="dashboard-subtitle">
                      {requestDetail.agentWorkflow.workflowStatus === "Failed"
                        ? "Automated compliance analysis did not complete successfully. The Export Officer may continue with manual review."
                        : "AI assessment is not available for this workflow."}
                    </p>
                  ) : (
                    <div>
                      {/* PROMINENT ADVISORY NOTICE */}
                      <div className="ai-advisory-message">
                        <AlertCircle size={18} style={{ color: "var(--gemora-dark-gold)", flexShrink: 0, marginTop: "1px" }} />
                        <span>
                          <strong>Advisory Notice:</strong> AI findings are advisory only. The final export decision must be made by an authorized Export Officer.
                        </span>
                      </div>

                      <div className="info-grid" style={{ marginBottom: "16px" }}>
                        <div className="info-item">
                          <span className="info-label">Risk & Officer Attention</span>
                          <span className="info-value">
                            {requestDetail.agentWorkflow.assessment.requiresOfficerAttention ? (
                              <span className="badge badge-revisionrequired">Requires Officer Attention</span>
                            ) : (
                              <span className="badge badge-approved">Low Risk / Verified</span>
                            )}
                          </span>
                        </div>

                        <div className="info-item">
                          <span className="info-label">AI Confidence</span>
                          <div className="confidence-bar-wrapper">
                            <span className="info-value">
                              {(requestDetail.agentWorkflow.assessment.confidence * 100).toFixed(0)}%
                            </span>
                            <div className="confidence-bar-track">
                              <div
                                className="confidence-bar-fill"
                                style={{
                                  width: `${Math.round(requestDetail.agentWorkflow.assessment.confidence * 100)}%`,
                                }}
                              />
                            </div>
                          </div>
                        </div>
                      </div>

                      <div style={{ marginBottom: "16px" }}>
                        <strong className="info-label">Summary:</strong>
                        <p style={{ marginTop: "4px", fontSize: "14px", color: "var(--gemora-dark-green)", lineHeight: 1.5 }}>
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

                {/* WORKFLOW EXECUTION PROGRESS & CONTROLLED TOOLS */}
                {requestDetail.agentWorkflow?.steps?.length > 0 && (
                  <div className="section-card">
                    <h3>
                      <span>Workflow Execution Details</span>
                      <span className="info-label">Step Audit Trail</span>
                    </h3>

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
                                {showToolCalls ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
                                {showToolCalls
                                  ? "Hide Controlled Tools (" + step.toolCalls.length + ")"
                                  : "View Controlled Tools (" + step.toolCalls.length + ")"}
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