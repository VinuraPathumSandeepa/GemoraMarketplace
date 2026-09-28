import { useState } from "react";
import {
  Link,
  useLocation,
  useNavigate,
} from "react-router-dom";

import { useAuth } from "../context/AuthContext";

function Login() {
  const { login } = useAuth();

  const navigate = useNavigate();
  const location = useLocation();

  const verifiedEmail =
    location.state?.email || "";

  const [email, setEmail] =
    useState(verifiedEmail);

  const [password, setPassword] =
    useState("");

  const [error, setError] =
    useState("");

  const [success, setSuccess] =
    useState(
      location.state?.verified
        ? "Email verified successfully. You can now sign in."
        : ""
    );

  const [submitting, setSubmitting] =
    useState(false);

  // ============================================================
  // HANDLE LOGIN
  // ==========================================

  const handleSubmit = async (event) => {
    event.preventDefault();

    setError("");
    setSuccess("");

    if (!email.trim()) {
      setError(
        "Please enter your email address."
      );

      return;
    }

    if (!password) {
      setError(
        "Please enter your password."
      );

      return;
    }

    setSubmitting(true);

    try {
      const user =
        await login(
          email
            .trim()
            .toLowerCase(),
          password
        );

      console.log(
        "Logged in user:",
        user
      );

      navigate(
        "/dashboard",
        {
          replace: true,
        }
      );
    } catch (error) {
      console.error(
        "Login failed:",
        error
      );

      const errorCode =
        error.response?.data?.errorCode;

      if (
        errorCode ===
        "EMAIL_NOT_VERIFIED"
      ) {
        const normalizedEmail =
          email
            .trim()
            .toLowerCase();

        sessionStorage.setItem(
          "gemora_pending_verification_email",
          normalizedEmail
        );

        setError(
          "Your email has not been verified yet. Verify your email before signing in."
        );

        return;
      }

      setError(
        error.response?.data?.message ||
          "Invalid email or password."
      );
    } finally {
      setSubmitting(false);
    }
  };

  // ============================================================
  // PAGE
  // ============================================================

  return (
    <div className="login-page">

      <div className="login-card">

        {/* BRAND */}

        <div className="auth-brand">
          <h1>
            Gemora
          </h1>

          <p>
            Secure Gemstone Marketplace
          </p>
        </div>


        {/* HEADING */}

        <div className="auth-heading">

          <span>
            WELCOME BACK
          </span>

          <h2>
            Sign in to Gemora
          </h2>

          <p>
            Access your marketplace workspace,
            gemstone listings, and verification
            activities securely.
          </p>

        </div>


        {/* SUCCESS */}

        {success && (
          <div className="success-message">
            {success}
          </div>
        )}


        {/* ERROR */}

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
              placeholder="you@example.com"
              value={email}
              onChange={(event) => {
                setEmail(
                  event.target.value
                );

                setError("");
              }}
              required
              autoComplete="email"
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
              onChange={(event) => {
                setPassword(
                  event.target.value
                );

                setError("");
              }}
              required
              autoComplete="current-password"
            />

          </div>


          {/* SIGN IN BUTTON */}

          <button
            type="submit"
            disabled={submitting}
            className="auth-primary-button"
          >
            {submitting
              ? "Signing In..."
              : "Sign In →"}
          </button>

        </form>


        {/* UNVERIFIED EMAIL HELP */}

        {error &&
          error
            .toLowerCase()
            .includes("verify") && (
            <div className="login-verification-help">

              <p>
                Still waiting to verify
                your account?
              </p>

              <button
                type="button"
                onClick={() => {
                  const normalizedEmail =
                    email
                      .trim()
                      .toLowerCase();

                  sessionStorage.setItem(
                    "gemora_pending_verification_email",
                    normalizedEmail
                  );

                  navigate(
                    "/verify-email",
                    {
                      state: {
                        email:
                          normalizedEmail,
                      },
                    }
                  );
                }}
              >
                Go to Email Verification →
              </button>

            </div>
          )}


        {/* FOOTER */}

        <div className="auth-footer">

          <p>
            Don't have an account?{" "}

            <Link to="/register">
              Create Account
            </Link>
          </p>

        </div>

      </div>

    </div>
  );
}

export default Login;