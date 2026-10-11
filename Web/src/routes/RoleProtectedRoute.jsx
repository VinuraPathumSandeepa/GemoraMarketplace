import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

function RoleProtectedRoute({
  roles,
  allowedRoles,
  children,
}) {
  const {
    user,
    isAuthenticated,
    loading,
  } = useAuth();

  const permittedRoles =
    roles ?? allowedRoles ?? [];

  if (loading) {
    return <p>Loading...</p>;
  }

  if (!isAuthenticated || !user) {
    return (
      <Navigate
        to="/login"
        replace
      />
    );
  }

  if (
    permittedRoles.length > 0 &&
    !permittedRoles.includes(user.role)
  ) {
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