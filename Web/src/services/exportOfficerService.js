import api from "./api";

export const getReviewQueue = async () => {
  const response = await api.get("/export-officer/requests");
  return response.data;
};

export const getRequestDetail = async (requestId) => {
  const response = await api.get(`/export-officer/requests/${requestId}`);
  return response.data;
};

export const startReview = async (requestId) => {
  const response = await api.post(`/export-officer/requests/${requestId}/start-review`);
  return response.data;
};

export const makeDecision = async (requestId, decision, reviewNotes) => {
  const response = await api.post(`/export-officer/requests/${requestId}/decision`, {
    decision,
    reviewNotes: reviewNotes || null,
  });
  return response.data;
};

export const downloadComplianceDocument = async (requestId, documentId) => {
  const response = await api.get(
    `/export-officer/requests/${requestId}/documents/${documentId}/file`,
    {
      responseType: "blob",
    }
  );
  return response.data;
};
