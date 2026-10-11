import api from "./api";

export const notificationService = {
  async list() {
    const { data } = await api.get("/Notifications");
    return Array.isArray(data) ? data : [];
  },
  async markRead(id) {
    await api.put(`/Notifications/${encodeURIComponent(id)}/read`);
  },
  async markAllRead() {
    await api.put("/Notifications/read-all");
  },
};

export function notificationError(error) {
  if (error?.response?.status === 401) return "Your session has expired. Please sign in again.";
  return "Notifications are unavailable right now. Please try again.";
}
