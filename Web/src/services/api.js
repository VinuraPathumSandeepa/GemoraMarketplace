import axios from "axios";

const API_BASE_URL =
  // "https://gemora-api.onrender.com/api";
  // import.meta.env.VITE_API_URL ||
  "http://localhost:5198/api";

export const API_ORIGIN =
  API_BASE_URL.replace(
    /\/api\/?$/i,
    ""
  );

export function resolveApiAssetUrl(
  value
) {
  if (!value) {
    return null;
  }

  if (
    value.startsWith("http://") ||
    value.startsWith("https://")
  ) {
    return value;
  }

  return `${API_ORIGIN}${value.startsWith("/")
      ? value
      : `/${value}`
    }`;
}

const api = axios.create({
  baseURL: API_BASE_URL,
});

/*
 * Do not permanently force:
 *
 * Content-Type: application/json
 *
 * Axios will automatically use:
 *
 * application/json
 * for normal objects
 *
 * multipart/form-data
 * for FormData uploads.
 */

api.interceptors.request.use(
  (config) => {
    const token =
      localStorage.getItem(
        "gemora_token"
      );

    if (token) {
      config.headers.Authorization =
        `Bearer ${token}`;
    }

    return config;
  },

  (error) =>
    Promise.reject(error)
);

export default api;



// ==========================================
// SHIPMENT API SERVICE
// ==========================================

export const shipmentApi = {
    // Create a new shipment (Seller only)
    createShipment: (shipmentData) => api.post("/Shipments", shipmentData),

    // Get shipment by ID
    getShipmentById: (id) => api.get(`/Shipments/${id}`),

    // Get shipment by order ID
    getShipmentByOrderId: (orderId) => api.get(`/Shipments/order/${orderId}`),

    // Get authenticated user's shipments
    getMyShipments: () => api.get("/Shipments/my"),

    // Update shipment status
    updateShipmentStatus: (id, statusData) =>
        api.put(`/Shipments/${id}/status`, statusData),

    // Generate shipping plan using AI
    getShippingPlan: (id) => api.get(`/Shipments/${id}/plan`),
    generateShippingPlan: (id) => api.post(`/Shipments/${id}/plan`),

    // Approve shipping plan (Admin only)
    approveShippingPlan: (id, notes) => 
        api.post(`/Shipments/${id}/plan/approve`, notes ? { notes } : {}),

    // Reject shipping plan (Admin only)
    rejectShippingPlan: (id, reason) => 
        api.post(`/Shipments/${id}/plan/reject`, reason ? { reason } : {}),

    // Request revision of shipping plan (Admin only)
    requestRevisionShippingPlan: (id, notes) => 
        api.post(`/Shipments/${id}/plan/request-revision`, notes ? { notes } : {}),

    // Get tracking events
    getTrackingEvents: (id) => api.get(`/Shipments/${id}/tracking`),

    // Get shipment audit history (Admin only)
    getShipmentAuditHistory: (id) => api.get(`/Shipments/${id}/audit`),

    // Add tracking event (Admin only)
    addTrackingEvent: (id, eventData) =>
        api.post(`/Shipments/${id}/tracking-events`, eventData),

    // Get insurance information
    getInsurance: (id) => api.get(`/Shipments/${id}/insurance`),

    // Create insurance record (Admin only)
    createInsurance: (id, insuranceData) =>
        api.post(`/Shipments/${id}/insurance`, insuranceData),

    // Book shipment with courier (Admin only)
    bookShipment: (id) => api.post(`/Shipments/${id}/book`),
};
