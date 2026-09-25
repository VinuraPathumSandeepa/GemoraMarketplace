import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

function Dashboard() {
  const { user, loading } = useAuth();

  if (loading) {
    return <p>Loading...</p>;
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  switch (user.role) {
    case "Buyer":
      return <Navigate to="/buyer" replace />;

    case "Seller":
      return <Navigate to="/seller" replace />;

    case "Admin":
      return <Navigate to="/admin" replace />;

    case "Gemologist":
      return <Navigate to="/gemologist" replace />;

    case "ExportOfficer":
      return (
        <Navigate
          to="/export-officer"
          replace
        />
      );

    default:
      return <Navigate to="/login" replace />;
  }
}

export default Dashboard;