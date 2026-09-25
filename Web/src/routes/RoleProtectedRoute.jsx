import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

function RoleProtectedRoute({
  allowedRoles,
  children,
}) {
  const {
    user,
    isAuthenticated,
    loading,
  } = useAuth();

  // Wait until AuthContext checks the JWT
  if (loading) {
    return <p>Loading...</p>;
  }

  // User is not logged in
  if (!isAuthenticated) {
    return (
      <Navigate
        to="/login"
        replace
      />
    );
  }

  // User is logged in but does not have
  // permission to access this page
  if (!allowedRoles.includes(user?.role)) {
    return (
      <Navigate
        to="/dashboard"
        replace
      />
    );
  }

  return children;
}

export default RoleProtectedRoute;