import {
  createContext,
  useContext,
  useEffect,
  useState,
} from "react";

import api from "../services/api";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  // ==========================================
  // CHECK CURRENT USER
  // ==========================================

  const loadCurrentUser = async () => {
    const token = localStorage.getItem("gemora_token");

    if (!token) {
      setUser(null);
      setLoading(false);
      return;
    }

    try {
      const response = await api.get("/Auth/me");

      setUser(response.data);
    } catch (error) {
      console.error(
        "Failed to load current user:",
        error
      );

      localStorage.removeItem("gemora_token");

      setUser(null);
    } finally {
      setLoading(false);
    }
  };

  // ==========================================
  // LOGIN
  // ==========================================

  const login = async (email, password) => {
    const response = await api.post(
      "/Auth/login",
      {
        email,
        password,
      }
    );

    const token = response.data.token;

    localStorage.setItem(
      "gemora_token",
      token
    );

    // Get logged-in user's information
    const userResponse =
      await api.get("/Auth/me");

    setUser(userResponse.data);

    return userResponse.data;
  };

  // ==========================================
  // LOGOUT
  // ==========================================

  const logout = () => {
    localStorage.removeItem("gemora_token");

    setUser(null);
  };

  // ==========================================
  // LOAD USER WHEN APP STARTS
  // ==========================================

  useEffect(() => {
    loadCurrentUser();
  }, []);

  // ==========================================
  // PROVIDE AUTHENTICATION STATE
  // ==========================================

  return (
    <AuthContext.Provider
      value={{
        user,
        loading,
        login,
        logout,
        isAuthenticated: !!user,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

// ==========================================
// CUSTOM AUTH HOOK
// ==========================================

export function useAuth() {
  return useContext(AuthContext);
}