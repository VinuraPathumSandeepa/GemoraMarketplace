import { useState, useEffect } from "react";

import DashboardLayout from "../layouts/DashboardLayout";
import api from "../services/api";
import "../styles/AdminShipping.css";
import "../styles/AdminAIDashboard.css";

const API_BASE = "";

function AdminAIDashboard() {

  const [shipments, setShipments] = useState([]);
  const [selectedShipment, setSelectedShipment] = useState(null);
  const [plan, setPlan] = useState(null);
  const [loading, setLoading] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState(null);
  const [stats, setStats] = useState(null);

  useEffect(() => {
    loadShipments();
    loadStats();
  }, []);

  const getToken = () => localStorage.getItem("gemora_token");

  const loadShipments = async () => {
    try {
      const token = getToken();
      const response = await api.get(`${API_BASE}/Shipments/my`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      setShipments(response.data.slice(0, 10)); // Show first 10
    } catch (err) {
      console.error("Failed to load shipments:", err);
      setError("Failed to load shipments");
    }
  };

  const loadStats = async () => {
    try {
      const token = getToken();
      const response = await api.get(`${API_BASE}/Shipments/my`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      const allShipments = response.data;

      // Calculate stats from all shipments
      const plansWithAI = allShipments.filter(
        (s) => s.generationSource === "AI"
      ).length;
      const plansWithFallback = allShipments.filter(
        (s) => s.generationSource === "FallbackRules"
      ).length;
      const totalPlans = plansWithAI + plansWithFallback;

      setStats({
        total: allShipments.length,
        aiGenerated: plansWithAI,
        fallbackUsed: plansWithFallback,
        aiSuccessRate:
          totalPlans > 0 ? ((plansWithAI / totalPlans) * 100).toFixed(1) : 0,
      });
    } catch (err) {
      console.error("Failed to load stats:", err);
    }
  };

  const loadPlan = async (shipmentId) => {
    try {
      setLoading(true);
      setError(null);
      const token = getToken();
      const response = await api.get(
        `${API_BASE}/Shipments/${shipmentId}/plan`,
        {
          headers: { Authorization: `Bearer ${token}` },
        }
      );
      setPlan(response.data);
    } catch (err) {
      console.error("Failed to load plan:", err);
      if (err.response?.status === 404) {
        setPlan(null);
        setError("No plan generated yet for this shipment");
      } else {
        setError("Failed to load plan");
      }
    } finally {
      setLoading(false);
    }
  };

  const handleSelectShipment = async (shipment) => {
    setSelectedShipment(shipment);
    setPlan(null);
    setError(null);
    await loadPlan(shipment.id);
  };

  const generatePlan = async () => {
    if (!selectedShipment) return;

    try {
      setGenerating(true);
      setError(null);
      const token = getToken();
      const response = await api.post(
        `${API_BASE}/Shipments/${selectedShipment.id}/plan/generate`,
        {},
        {
          headers: { Authorization: `Bearer ${token}` },
        }
      );
      setPlan(response.data);
      await loadStats(); // Refresh stats
    } catch (err) {
      console.error("Failed to generate plan:", err);
      setError(err.response?.data?.message || "Failed to generate plan");
    } finally {
      setGenerating(false);
    }
  };

  const getRiskColor = (riskLevel) => {
    switch (riskLevel?.toLowerCase()) {
      case "low":
        return "bg-green-100 text-green-800";
      case "medium":
        return "bg-yellow-100 text-yellow-800";
      case "high":
        return "bg-orange-100 text-orange-800";
      case "critical":
        return "bg-red-100 text-red-800";
      default:
        return "bg-gray-100 text-gray-800";
    }
  };

  const getSourceBadge = (source) => {
    if (source === "AI") {
      return (
        <span className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-purple-100 text-purple-800">
          🤖 AI Generated
        </span>
      );
    } else if (source === "FallbackRules") {
      return (
        <span className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-blue-100 text-blue-800">
          📋 Deterministic Fallback
        </span>
      );
    }
    return null;
  };

  return (
    <DashboardLayout title="AI Shipping Plan Analyzer">
      <div className="admin-shipping admin-ai-dashboard">
        <header className="admin-shipping-header">
          <div>
            <p className="admin-shipping-eyebrow">Shipping operations</p>
            <h1>AI Shipping Plan Analyzer</h1>
            <p>Review shipment risks, insurance coverage, and AI shipping recommendations.</p>
          </div>
        </header>
        {/* Stats Cards */}
        {stats && (
          <div className="admin-shipping-stats admin-ai-stats">
            <div className="admin-shipping-stat">
              <h3 className="text-sm font-medium text-gray-500">
                Total Shipments
              </h3>
              <p className="mt-2 text-3xl font-bold text-gray-900">
                {stats.total}
              </p>
            </div>
            <div className="admin-shipping-stat">
              <h3 className="text-sm font-medium text-gray-500">
                AI Generated Plans
              </h3>
              <p className="mt-2 text-3xl font-bold text-purple-600">
                {stats.aiGenerated}
              </p>
            </div>
            <div className="admin-shipping-stat">
              <h3 className="text-sm font-medium text-gray-500">
                Fallback Used
              </h3>
              <p className="mt-2 text-3xl font-bold text-blue-600">
                {stats.fallbackUsed}
              </p>
            </div>
            <div className="admin-shipping-stat">
              <h3 className="text-sm font-medium text-gray-500">
                AI Success Rate
              </h3>
              <p className="mt-2 text-3xl font-bold text-green-600">
                {stats.aiSuccessRate}%
              </p>
            </div>
          </div>
        )}

        {/* How It Works */}
        <div className="admin-shipping-panel admin-ai-workflow">
          <h2 className="text-xl font-bold text-gray-900 mb-4">
            🧠 How the AI Agent Works
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
            <div className="space-y-2">
              <div className="font-semibold text-purple-700">Step 1: Input</div>
              <p className="text-gray-700">
                AI receives trusted backend data: declared value, origin/destination,
                package weight, export requirements, gem type
              </p>
            </div>
            <div className="space-y-2">
              <div className="font-semibold text-purple-700">
                Step 2: Analysis
              </div>
              <p className="text-gray-700">
                LLM analyzes risk factors and generates structured recommendation
                with risk level, service type, coverage amount, handling requirements
              </p>
            </div>
            <div className="space-y-2">
              <div className="font-semibold text-purple-700">
                Step 3: Validation & Fallback
              </div>
              <p className="text-gray-700">
                Output validated against strict schema. If invalid or timeout → falls
                back to deterministic rule-based planner
              </p>
            </div>
          </div>
        </div>

        <div className="admin-ai-workspace">
          {/* Shipment List */}
          <div className="admin-shipping-panel admin-ai-shipments">
            <div className="p-4 border-b">
              <h3 className="text-lg font-semibold text-gray-900">Shipments</h3>
              <p className="text-sm text-gray-500 mt-1">
                Select a shipment to analyze
              </p>
            </div>
            <div className="admin-ai-shipment-list">
              {shipments.map((shipment) => (
                <button
                  key={shipment.id}
                  onClick={() => handleSelectShipment(shipment)}
                  disabled={loading || generating} aria-pressed={selectedShipment?.id === shipment.id} className={`admin-ai-shipment ${
                    selectedShipment?.id === shipment.id
                      ? "is-selected"
                      : ""
                  }`}
                >
                  <div className="flex justify-between items-start">
                    <div>
                      <p className="font-medium text-gray-900">
                        {shipment.packageDescription?.substring(0, 30) ||
                          "Shipment"}
                        ...
                      </p>
                      <p className="text-xs text-gray-500 mt-1">
                        {shipment.status} • {shipment.riskLevel || "No risk level"}
                      </p>
                    </div>
                    {shipment.trackingNumber && (
                      <span className="text-xs bg-gray-100 text-gray-600 px-2 py-1 rounded">
                        {shipment.trackingNumber.substring(0, 12)}...
                      </span>
                    )}
                  </div>
                </button>
              ))}
            </div>
          </div>

          {/* AI Analysis Results */}
          <div className="admin-shipping-panel admin-ai-results">
            <div className="p-4 border-b flex justify-between items-center">
              <div>
                <h3 className="text-lg font-semibold text-gray-900">
                  AI Analysis Results
                </h3>
                <p className="text-sm text-gray-500 mt-1">
                  {selectedShipment
                    ? `Shipment: ${selectedShipment.id.substring(0, 8)}...`
                    : "Select a shipment to view analysis"}
                </p>
              </div>
              {selectedShipment && (
                <button
                  onClick={generatePlan}
                  disabled={generating || loading}
                  className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center gap-2"
                >
                  {generating ? (
                    <>
                      <svg
                        className="animate-spin h-4 w-4"
                        viewBox="0 0 24 24"
                      >
                        <circle
                          className="opacity-25"
                          cx="12"
                          cy="12"
                          r="10"
                          stroke="currentColor"
                          strokeWidth="4"
                          fill="none"
                        />
                        <path
                          className="opacity-75"
                          fill="currentColor"
                          d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
                        />
                      </svg>
                      Generating...
                    </>
                  ) : (
                    <>🔄 Regenerate Plan</>
                  )}
                </button>
              )}
            </div>

            <div className="p-6">
              {!selectedShipment ? (
                <div className="text-center py-12 text-gray-500">
                  <div className="text-6xl mb-4">📦</div>
                  <p>Select a shipment from the list to view AI analysis</p>
                </div>
              ) : loading ? (
                <div className="text-center py-12">
                  <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-purple-600 mx-auto"></div>
                  <p className="mt-4 text-gray-500">Loading plan...</p>
                </div>
              ) : error ? (
                <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                  <p className="text-red-800">{error}</p>
                  <button
                    onClick={generatePlan}
                    className="mt-2 text-sm text-red-600 hover:text-red-800 underline"
                  >
                    Generate plan now →
                  </button>
                </div>
              ) : plan ? (
                <div className="space-y-6">
                  {/* Generation Source Badge */}
                  <div className="flex items-center gap-3">
                    <span className="text-sm font-medium text-gray-700">
                      Generated by:
                    </span>
                    {getSourceBadge(plan.generationSource)}
                  </div>

                  {/* Risk Level */}
                  <div>
                    <h4 className="text-sm font-medium text-gray-700 mb-2">
                      Risk Assessment
                    </h4>
                    <div className="flex items-center gap-3">
                      <span
                        className={`px-4 py-2 rounded-lg font-semibold ${getRiskColor(
                          plan.riskLevel
                        )}`}
                      >
                        {plan.riskLevel}
                      </span>
                      {plan.insuranceRecommended && (
                        <span className="text-sm text-orange-600 font-medium">
                          ⚠️ Insurance Recommended
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Risk Reasons */}
                  {plan.riskReasons && (
                    <div>
                      <h4 className="text-sm font-medium text-gray-700 mb-2">
                        Risk Factors
                      </h4>
                      <ul className="space-y-1">
                        {plan.riskReasons.split(";").map((reason, idx) => (
                          <li key={idx} className="text-sm text-gray-600 flex items-start gap-2">
                            <span className="text-purple-500 mt-0.5">•</span>
                            <span>{reason.trim()}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}

                  {/* Service Recommendation */}
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div className="bg-blue-50 rounded-lg p-4">
                      <h4 className="text-sm font-medium text-blue-900 mb-2">
                        Recommended Service
                      </h4>
                      <p className="text-lg font-semibold text-blue-700">
                        {plan.recommendedServiceType}
                      </p>
                    </div>
                    {plan.recommendedCoverageAmount && (
                      <div className="bg-green-50 rounded-lg p-4">
                        <h4 className="text-sm font-medium text-green-900 mb-2">
                          Coverage Amount
                        </h4>
                        <p className="text-lg font-semibold text-green-700">
                          {new Intl.NumberFormat("en-US", {
                            style: "currency",
                            currency: plan.currency || "USD",
                          }).format(plan.recommendedCoverageAmount)}
                        </p>
                      </div>
                    )}
                  </div>

                  {/* Handling Requirements */}
                  {plan.handlingRequirements && (
                    <div>
                      <h4 className="text-sm font-medium text-gray-700 mb-2">
                        Handling Requirements
                      </h4>
                      <ul className="space-y-1">
                        {plan.handlingRequirements.split(";").map((req, idx) => (
                          <li
                            key={idx}
                            className="text-sm text-gray-600 flex items-start gap-2"
                          >
                            <span className="text-blue-500 mt-0.5">✓</span>
                            <span>{req.trim()}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}

                  {/* Required Documents */}
                  {plan.requiredDocuments && (
                    <div>
                      <h4 className="text-sm font-medium text-gray-700 mb-2">
                        Required Documents
                      </h4>
                      <ul className="space-y-1">
                        {plan.requiredDocuments.split(";").map((doc, idx) => (
                          <li
                            key={idx}
                            className="text-sm text-gray-600 flex items-start gap-2"
                          >
                            <span className="text-orange-500 mt-0.5">📄</span>
                            <span>{doc.trim()}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}

                  {/* Warnings */}
                  {plan.warnings && (
                    <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
                      <h4 className="text-sm font-medium text-yellow-900 mb-2 flex items-center gap-2">
                        ⚠️ Warnings
                      </h4>
                      <ul className="space-y-1">
                        {plan.warnings.split(";").map((warning, idx) => (
                          <li key={idx} className="text-sm text-yellow-800">
                            • {warning.trim()}
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}

                  {/* Execution Summary */}
                  {plan.executionSummary && (
                    <div className="bg-gray-50 rounded-lg p-4">
                      <h4 className="text-sm font-medium text-gray-700 mb-2">
                        Execution Summary
                      </h4>
                      <p className="text-sm text-gray-600 italic">
                        {plan.executionSummary}
                      </p>
                    </div>
                  )}

                  {/* Approval Status */}
                  <div className="border-t pt-4">
                    <div className="flex items-center justify-between">
                      <div>
                        <h4 className="text-sm font-medium text-gray-700">
                          Approval Status
                        </h4>
                        <p
                          className={`text-sm mt-1 ${
                            plan.isApproved
                              ? "text-green-600"
                              : "text-orange-600"
                          }`}
                        >
                          {plan.isApproved
                            ? "✓ Approved by Admin"
                            : "⏳ Pending Admin Approval"}
                        </p>
                      </div>
                      {plan.approvedAt && (
                        <div className="text-right text-xs text-gray-500">
                          <p>Approved at:</p>
                          <p>
                            {new Date(plan.approvedAt).toLocaleDateString()}{" "}
                            {new Date(plan.approvedAt).toLocaleTimeString()}
                          </p>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              ) : (
                <div className="text-center py-12">
                  <div className="text-6xl mb-4">🤖</div>
                  <p className="text-gray-500 mb-4">No plan generated yet</p>
                  <button
                    onClick={generatePlan}
                    className="px-6 py-3 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition-colors"
                  >
                    Generate AI Plan
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* AI vs Fallback Comparison */}
        {plan && (
          <div className="admin-shipping-panel admin-ai-sources">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">
              📊 Understanding Generation Sources
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div
                className={`rounded-lg p-4 border-2 ${
                  plan.generationSource === "AI"
                    ? "border-purple-500 bg-purple-50"
                    : "border-gray-200 bg-gray-50 opacity-60"
                }`}
              >
                <h4 className="font-semibold text-purple-900 mb-2">
                  🤖 AI Generator
                </h4>
                <ul className="text-sm text-gray-700 space-y-1">
                  <li>• Uses LLM provider (Gemini/OpenAI)</li>
                  <li>• Analyzes complex risk patterns</li>
                  <li>• Provides nuanced recommendations</li>
                  <li>• May timeout or fail (network issues)</li>
                  <li>• Requires API configuration</li>
                </ul>
              </div>
              <div
                className={`rounded-lg p-4 border-2 ${
                  plan.generationSource === "FallbackRules"
                    ? "border-blue-500 bg-blue-50"
                    : "border-gray-200 bg-gray-50 opacity-60"
                }`}
              >
                <h4 className="font-semibold text-blue-900 mb-2">
                  📋 Deterministic Fallback
                </h4>
                <ul className="text-sm text-gray-700 space-y-1">
                  <li>• Rule-based risk scoring</li>
                  <li>• Always available (no network needed)</li>
                  <li>• Predictable, consistent results</li>
                  <li>• Fast execution (no API calls)</li>
                  <li>• Activated when AI fails validation</li>
                </ul>
              </div>
            </div>
            <div className="mt-4 p-4 bg-green-50 border border-green-200 rounded-lg">
              <p className="text-sm text-green-800">
                <strong>Safety Guarantee:</strong> The AI agent can only{" "}
                <em>recommend</em> shipping plans. It cannot approve plans, book
                couriers, purchase insurance, or mark shipments as delivered. All
                AI recommendations require Admin review and approval before any
                irreversible actions.
              </p>
            </div>
          </div>
        )}
      </div>
    </DashboardLayout>
  );
}

export default AdminAIDashboard;
