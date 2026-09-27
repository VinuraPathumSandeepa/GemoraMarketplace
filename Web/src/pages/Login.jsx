import { useState } from "react";
import {
  Link,
  useNavigate,
} from "react-router-dom";

import { useAuth } from "../context/AuthContext";
import "../styles/Login.css";

function Login() {
  // ==========================================
  // STATE
  // ==========================================

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const { login } = useAuth();
  const navigate = useNavigate();


  // ==========================================
  // HANDLE LOGIN
  // ==========================================

  const handleSubmit = async (event) => {
    event.preventDefault();

    setError("");
    setSubmitting(true);

    try {
      // Try to login with backend first
      const user = await login(email, password);
      console.log("Logged in user:", user);
      navigate("/dashboard");

    } catch (error) {
      console.error("Login failed:", error);
      
      // If backend is not available, use local demo login
      if (!error.response) {
        console.log("Backend not available, using local login...");
        
        // Simple local authentication for development
        const mockUser = {
          id: "00000000-0000-0000-0000-000000000001",
          email: email,
          fullName: email.split('@')[0] || "User",
          role: email.toLowerCase().includes('admin') ? "Admin" : 
                email.toLowerCase().includes('seller') ? "Seller" : "Buyer"
        };
        
        localStorage.setItem("gemora_token", "local-dev-token");
        localStorage.setItem("gemora_user", JSON.stringify(mockUser));
        
        navigate("/dashboard");
      } else {
        setError(
          error.response?.data?.message ||
            "Invalid email or password."
        );
      }
    } finally {
      setSubmitting(false);
    }
  };


  // ==========================================
  // PAGE
  // ==========================================

  return (
    <div className="login-page">

      <div className="login-card">

        {/* GEMORA TITLE */}

        <h1>
          Gemora
        </h1>

        <p className="login-subtitle">
          Gem Marketplace Management System
        </p>


        {/* LOGIN TITLE */}

        <h2>
          Sign In
        </h2>


        {/* ERROR MESSAGE */}

        {error && (
          <div className="error-message">
            {error}
          </div>
        )}


        {/* LOGIN FORM */}

        <form onSubmit={handleSubmit}>

          {/* EMAIL */}

          <div className="form-group">

            <label htmlFor="email">
              Email
            </label>

            <input
              id="email"
              type="email"
              placeholder="Enter your email"
              value={email}
              onChange={(event) =>
                setEmail(
                  event.target.value
                )
              }
              required
            />

          </div>


          {/* PASSWORD */}

          <div className="form-group">

            <label htmlFor="password">
              Password
            </label>

            <input
              id="password"
              type="password"
              placeholder="Enter your password"
              value={password}
              onChange={(event) =>
                setPassword(
                  event.target.value
                )
              }
              required
            />

          </div>


          {/* LOGIN BUTTON */}

          <button
            type="submit"
            disabled={submitting}
          >

            {submitting
              ? "Signing in..."
              : "Sign In"}

          </button>

        </form>


        {/* REGISTER LINK */}

        <p>
          Don't have an account?{" "}

          <Link to="/register">
            Create Account
          </Link>
        </p>

      </div>

    </div>
  );
}

export default Login;