import api from "./api";

export const getReviewQueue = async () => {
  const response = await api.get("/export-officer/requests");
  return response.data;
};

export const getRequestDetail = async (requestId) => {
  const response = await api.get(`/export-officer/requests/${requestId}`);
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
