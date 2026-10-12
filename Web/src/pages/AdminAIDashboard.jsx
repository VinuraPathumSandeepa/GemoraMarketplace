import { Navigate } from "react-router-dom";

// Preserve saved links while keeping shipping work in one queue.
export default function AdminAIDashboard() {
  return <Navigate to="/admin/shipments" replace />;
}
