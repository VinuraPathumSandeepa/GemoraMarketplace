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
      console.error("Failed to load current user:", error);
      
      if (error.response?.status === 401) {
        localStorage.removeItem("gemora_token");
        setUser(null);
      }
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
    const initAuth = async () => {
      // Development mode: Auto-login as admin if no token exists
      const existingToken = localStorage.getItem("gemora_token");
      
      if (!existingToken) {
        try {
          console.log("Development mode: Auto-logging in as admin...");
          const response = await api.post("/Auth/login", {
            email: "admin@gemora.com",
            password: "123456789"
          });
          
          const token = response.data.token;
          localStorage.setItem("gemora_token", token);
          console.log("Auto-login successful");
        } catch (error) {
          console.warn("Auto-login failed, will try loading from token:", error.message);
        }
      }
      
      await loadCurrentUser();
    };
    
    initAuth();
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