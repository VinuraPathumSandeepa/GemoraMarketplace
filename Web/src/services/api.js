import axios from "axios";

const api = axios.create({
  baseURL:
    import.meta.env.VITE_API_URL ||
    "http://localhost:5198/api",

  headers: {
    "Content-Type": "application/json",
  },
});

api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem("gemora_token");

    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error) => Promise.reject(error)
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
  generateShippingPlan: (id) => api.post(`/Shipments/${id}/plan`),

  // Approve shipping plan (Admin only)
  approveShippingPlan: (id) => api.post(`/Shipments/${id}/plan/approve`),

  // Get tracking events
  getTrackingEvents: (id) => api.get(`/Shipments/${id}/tracking`),

  // Add tracking event (Admin only)
  addTrackingEvent: (id, eventData) =>
    api.post(`/Shipments/${id}/tracking-events`, eventData),

  // Get insurance information
  getInsurance: (id) => api.get(`/Shipments/${id}/insurance`),

  // Create insurance record
  createInsurance: (id, insuranceData) =>
    api.post(`/Shipments/${id}/insurance`, insuranceData),
};
