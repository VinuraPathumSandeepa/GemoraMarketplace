import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import api from "../services/api";

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

const CheckIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="20 6 9 17 4 12"/>
  </svg>
);

function Register() {
  const navigate = useNavigate();

  const [formData, setFormData] = useState({
    fullName: "",
    email: "",
    password: "",
    role: "Buyer",
  });

  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const handleChange = (event) => {
    const { name, value } = event.target;

    setFormData((previous) => ({
      ...previous,
      [name]: value,
    }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    setError("");
    setSuccess("");
    setSubmitting(true);

    try {
      await api.post("/Auth/register", formData);

      setSuccess("Registration successful. Redirecting to login...");

      setTimeout(() => {
        navigate("/login");
      }, 1500);

    } catch (error) {
      setError(
        error.response?.data?.message ||
          "Registration failed. Please try again."
      );
    } finally {
      setSubmitting(false);
    }
  };

  // Password strength helper
  const pwLen = formData.password.length;
  const pwStrength =
    pwLen === 0 ? null :
    pwLen < 6   ? "weak" :
    pwLen < 10  ? "fair" :
                  "strong";

  const strengthLabel = {
    weak: "Weak",
    fair: "Fair",
    strong: "Strong",
  };

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

        <ul className="auth-left-benefits">
          <li><span className="auth-benefit-icon"><CheckIcon /></span>Verified gem listings</li>
          <li><span className="auth-benefit-icon"><CheckIcon /></span>Secure buyer-seller transactions</li>
          <li><span className="auth-benefit-icon"><CheckIcon /></span>Gemologist-certified quality</li>
        </ul>

        <div className="auth-left-footer">
          <p>"Where authentic gems meet trusted traders."</p>
        </div>
      </div>

      {/* ── RIGHT PANEL ── */}
      <div className="auth-right-panel">
        <div className="auth-form-wrapper">

          {/* HEADER */}
          <div className="auth-form-header">
            <p className="auth-form-eyebrow">Get started free</p>
            <h2 className="auth-form-title">Create Account</h2>
            <p className="auth-form-subtitle">
              Join Gemora to buy, sell, or verify gemstones.
            </p>
          </div>

          {/* ERROR */}
          {error && (
            <div className="auth-error">
              <span className="auth-error-icon">!</span>
              {error}
            </div>
          )}

          {/* SUCCESS */}
          {success && (
            <div className="auth-success">
              <span className="auth-success-icon">✓</span>
              {success}
            </div>
          )}

          {/* FORM */}
          <form className="auth-form" onSubmit={handleSubmit}>

            {/* FULL NAME */}
            <div className="auth-field">
              <label className="auth-label" htmlFor="fullName">
                Full Name
              </label>
              <input
                id="fullName"
                className="auth-input"
                type="text"
                name="fullName"
                placeholder="John Silva"
                value={formData.fullName}
                onChange={handleChange}
                required
                minLength={2}
                maxLength={100}
                autoComplete="name"
              />
            </div>

            {/* EMAIL */}
            <div className="auth-field">
              <label className="auth-label" htmlFor="email">
                Email address
              </label>
              <input
                id="email"
                className="auth-input"
                type="email"
                name="email"
                placeholder="you@example.com"
                value={formData.email}
                onChange={handleChange}
                required
                autoComplete="email"
              />
            </div>

            {/* PASSWORD */}
            <div className="auth-field">
              <label className="auth-label" htmlFor="password">
                Password
              </label>
              <div className="auth-input-wrapper">
                <input
                  id="password"
                  className="auth-input"
                  type={showPassword ? "text" : "password"}
                  name="password"
                  placeholder="Minimum 6 characters"
                  value={formData.password}
                  onChange={handleChange}
                  required
                  minLength={6}
                  autoComplete="new-password"
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

              {/* STRENGTH METER */}
              {pwStrength && (
                <div className="auth-pw-strength">
                  <div className={`auth-pw-bar auth-pw-${pwStrength}`} />
                  <span className={`auth-pw-label auth-pw-${pwStrength}`}>
                    {strengthLabel[pwStrength]}
                  </span>
                </div>
              )}
            </div>

            {/* ACCOUNT TYPE */}
            <div className="auth-field">
              <label className="auth-label" htmlFor="role">
                Account Type
              </label>
              <div className="auth-role-grid">
                {["Buyer", "Seller"].map((r) => (
                  <label
                    key={r}
                    className={`auth-role-card ${formData.role === r ? "auth-role-selected" : ""}`}
                  >
                    <input
                      type="radio"
                      name="role"
                      value={r}
                      checked={formData.role === r}
                      onChange={handleChange}
                      className="auth-role-radio"
                    />
                    <span className="auth-role-icon">
                      {r === "Buyer" ? "🛍️" : "💎"}
                    </span>
                    <span className="auth-role-name">{r}</span>
                    <span className="auth-role-desc">
                      {r === "Buyer"
                        ? "Browse & purchase gems"
                        : "List & sell gemstones"}
                    </span>
                  </label>
                ))}
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
                "Create Account"
              )}
            </button>

          </form>

          {/* FOOTER */}
          <p className="auth-switch-text">
            Already have an account?{" "}
            <Link className="auth-switch-link" to="/login">
              Sign In
            </Link>
          </p>

        </div>
      </div>

    </div>
  );
}

export default Register;