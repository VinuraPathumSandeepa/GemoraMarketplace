import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export default function Dashboard() {
  const {
    user,
    isAuthenticated,
    loading,
  } = useAuth();

  if (loading) {
    return (
      <div style={{ padding: "40px" }}>
        Loading...
      </div>
    );
  }

  if (!isAuthenticated || !user) {
    return <Navigate to="/login" replace />;
  }

  switch (user.role) {
    case "Buyer":
      return (
        <Navigate
          to="/buyer/dashboard"
          replace
        />
      );

    case "Seller":
      return (
        <Navigate
          to="/seller"
          replace
        />
      );

    case "Gemologist":
      return (
        <Navigate
          to="/gemologist"
          replace
        />
      );

    case "Admin":
      return (
        <Navigate
          to="/admin"
          replace
        />
      );

    case "ExportOfficer":
      return (
        <Navigate
          to="/export-officer"
          replace
        />
      );

    default:
      return <Navigate to="/" replace />;
  }
}