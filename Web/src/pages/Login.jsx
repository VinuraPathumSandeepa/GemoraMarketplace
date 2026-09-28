import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

// ── tiny inline SVG icons (no extra deps) ──────────────────
const EyeIcon = () => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
    <circle cx="12" cy="12" r="3"/>
  </svg>
);

const EyeOffIcon = () => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94"/>
    <path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19"/>
    <line x1="1" y1="1" x2="23" y2="23"/>
  </svg>
);

function Login() {
  // ==========================================
  // STATE
  // ==========================================

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
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
      const user = await login(email, password);

      console.log("Logged in user:", user);

      navigate("/dashboard");

    } catch (error) {
      console.error("Login failed:", error);

      setError(
        error.response?.data?.message ||
          "Invalid email or password."
      );

    } finally {
      setSubmitting(false);
    }
  };


  // ==========================================
  // PAGE
  // ==========================================

  return (
    <div className="auth-page">

      {/* ── LEFT PANEL ── */}
      <div className="auth-left-panel">
        <div className="auth-brand">
          <div className="auth-gem-mark">
            <span>◆</span>
          </div>
          <h1 className="auth-brand-name">Gemora</h1>
          <p className="auth-brand-tagline">Gem Marketplace Management System</p>
        </div>

        <div className="auth-left-footer">
          <p>"Where authentic gems meet trusted traders."</p>
        </div>
      </div>

      {/* ── RIGHT PANEL ── */}
      <div className="auth-right-panel">
        <div className="auth-form-wrapper">

          {/* HEADER */}
          <div className="auth-form-header">
            <p className="auth-form-eyebrow">Welcome back</p>
            <h2 className="auth-form-title">Sign In</h2>
            <p className="auth-form-subtitle">
              Enter your credentials to access your account.
            </p>
          </div>

          {/* ERROR */}
          {error && (
            <div className="auth-error">
              <span className="auth-error-icon">!</span>
              {error}
            </div>
          )}

          {/* FORM */}
          <form className="auth-form" onSubmit={handleSubmit}>

            {/* EMAIL */}
            <div className="auth-field">
              <label className="auth-label" htmlFor="email">
                Email address
              </label>
              <input
                id="email"
                className="auth-input"
                type="email"
                placeholder="you@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoComplete="email"
              />
            </div>

            {/* PASSWORD */}
            <div className="auth-field">
              <div className="auth-label-row">
                <label className="auth-label" htmlFor="password">
                  Password
                </label>
              </div>
              <div className="auth-input-wrapper">
                <input
                  id="password"
                  className="auth-input"
                  type={showPassword ? "text" : "password"}
                  placeholder="Enter your password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  autoComplete="current-password"
                />
                <button
                  type="button"
                  className="auth-eye-btn"
                  onClick={() => setShowPassword((v) => !v)}
                  aria-label={showPassword ? "Hide password" : "Show password"}
                >
                  {showPassword ? <EyeOffIcon /> : <EyeIcon />}
                </button>
              </div>
            </div>

            {/* SUBMIT */}
            <button
              type="submit"
              className="auth-submit-btn"
              disabled={submitting}
            >
              {submitting ? (
                <span className="auth-spinner" />
              ) : (
                "Sign In"
              )}
            </button>

          </form>

          {/* FOOTER */}
          <p className="auth-switch-text">
            Don't have an account?{" "}
            <Link className="auth-switch-link" to="/register">
              Create Account
            </Link>
          </p>

        </div>
      </div>

    </div>
  );
}

export default Login;