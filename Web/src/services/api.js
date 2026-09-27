import axios from "axios";

// ==========================================
// BASE AXIOS INSTANCE
// ==========================================

const api = axios.create({
  baseURL:
    import.meta.env.VITE_API_URL ||
    "http://localhost:5198/api",

  headers: {
    "Content-Type": "application/json",
  },
});

// Add JWT token to all requests
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
  createShipment: (shipmentData) => api.post("/Shipment", shipmentData),

  // Get shipment by ID
  getShipmentById: (id) => api.get(`/Shipment/${id}`),

  // Get shipment by order ID
  getShipmentByOrderId: (orderId) => api.get(`/Shipment/order/${orderId}`),

  // Get authenticated user's shipments
  getMyShipments: () => api.get("/Shipment/my"),

  // Update shipment status
  updateShipmentStatus: (id, statusData) =>
    api.put(`/Shipment/${id}/status`, statusData),

  // Generate shipping plan using AI
  generateShippingPlan: (id) => api.post(`/Shipment/${id}/plan`),

  // Get shipping plan
  getShippingPlan: (id) => api.get(`/Shipment/${id}/plan`),

  // Approve shipping plan (Admin only)
  approveShippingPlan: (id) => api.post(`/Shipment/${id}/plan/approve`),

  // Get tracking events
  getTrackingEvents: (id) => api.get(`/Shipment/${id}/tracking`),

  // Get insurance information
  getInsurance: (id) => api.get(`/Shipment/${id}/insurance`),
};
