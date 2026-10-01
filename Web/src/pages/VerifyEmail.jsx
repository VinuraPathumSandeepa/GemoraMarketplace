import {
  useEffect,
  useState,
} from "react";

import {
  Link,
  useLocation,
  useNavigate,
} from "react-router-dom";

import api from "../services/api";

function VerifyEmail() {
  const navigate = useNavigate();
  const location = useLocation();

  const passedEmail =
    location.state?.email || "";

  const storedEmail =
    sessionStorage.getItem(
      "gemora_pending_verification_email"
    ) || "";

  const [email, setEmail] =
    useState(
      passedEmail || storedEmail
    );

  const [code, setCode] =
    useState("");

  const [error, setError] =
    useState("");

  const [success, setSuccess] =
    useState("");

  const [verifying, setVerifying] =
    useState(false);

  const [resending, setResending] =
    useState(false);

  const [cooldown, setCooldown] =
    useState(60);

  // ============================================================
  // RESEND COUNTDOWN
  // ============================================================

  useEffect(() => {
    if (cooldown <= 0) {
      return;
    }

    const timer = setInterval(() => {
      setCooldown((current) =>
        current > 0
          ? current - 1
          : 0
      );
    }, 1000);

    return () =>
      clearInterval(timer);
  }, [cooldown]);

  // ============================================================
  // CODE INPUT
  // ============================================================

  const handleCodeChange = (event) => {
    const value =
      event.target.value
        .replace(/\D/g, "")
        .slice(0, 6);

    setCode(value);

    setError("");
  };

  // ============================================================
  // VERIFY
  // ============================================================

  const handleVerify = async (event) => {
    event.preventDefault();

    setError("");
    setSuccess("");

    if (!email.trim()) {
      setError(
        "Please enter the email address you registered with."
      );

      return;
    }

    if (!/^\d{6}$/.test(code)) {
      setError(
        "Please enter the complete 6-digit verification code."
      );

      return;
    }

    setVerifying(true);

    try {
      const response =
        await api.post(
          "/Auth/verify-email",
          {
            email:
              email
                .trim()
                .toLowerCase(),

            code,
          }
        );

      setSuccess(
        response.data?.message ||
          "Email verified successfully."
      );

      sessionStorage.removeItem(
        "gemora_pending_verification_email"
      );

      setTimeout(() => {
        navigate(
          "/login",
          {
            replace: true,
            state: {
              verified: true,
              email:
                email
                  .trim()
                  .toLowerCase(),
            },
          }
        );
      }, 1200);
    } catch (error) {
      setError(
        error.response?.data?.message ||
          "Verification failed. Please check the code and try again."
      );
    } finally {
      setVerifying(false);
    }
  };

  // ============================================================
  // RESEND
  // ============================================================

  const handleResend = async () => {
    if (cooldown > 0 || resending) {
      return;
    }

    setError("");
    setSuccess("");
    setResending(true);

    try {
      const response =
        await api.post(
          "/Auth/resend-verification-code",
          {
            email:
              email
                .trim()
                .toLowerCase(),
          }
        );

      setSuccess(
        response.data?.message ||
          "A new verification code has been sent."
      );

      setCode("");

      setCooldown(60);
    } catch (error) {
      setError(
        error.response?.data?.message ||
          "We could not resend the verification code."
      );
    } finally {
      setResending(false);
    }
  };

  return (
    <div className="register-page">
      <div className="register-card">

        <div className="auth-brand">
          <h1>Gemora</h1>

          <p>
            Secure Gemstone Marketplace
          </p>
        </div>

        <div className="auth-heading">
          <span>
            EMAIL VERIFICATION
          </span>

          <h2>
            Check your inbox
          </h2>

          <p>
            We sent a 6-digit verification
            code to your email address.
          </p>
        </div>

        {error && (
          <div className="error-message">
            {error}
          </div>
        )}

        {success && (
          <div className="success-message">
            {success}
          </div>
        )}

        <form
          onSubmit={handleVerify}
          className="register-form"
        >

          {/* EMAIL */}

          <div className="form-group">
            <label htmlFor="verificationEmail">
              Email
            </label>

            <input
              id="verificationEmail"
              type="email"
              value={email}
              onChange={(event) =>
                setEmail(event.target.value)
              }
              placeholder="you@example.com"
              required
              autoComplete="email"
            />
          </div>

          {/* OTP */}

          <div className="form-group">
            <label htmlFor="verificationCode">
              Verification Code
            </label>

            <input
              id="verificationCode"
              className="otp-code-input"
              type="text"
              inputMode="numeric"
              autoComplete="one-time-code"
              value={code}
              onChange={handleCodeChange}
              placeholder="000000"
              maxLength={6}
              required
            />

            <small className="form-helper">
              The code contains exactly
              6 digits and expires in
              10 minutes.
            </small>
          </div>

          <button
            type="submit"
            disabled={
              verifying ||
              code.length !== 6
            }
            className="auth-primary-button"
          >
            {verifying
              ? "Verifying..."
              : "Verify Email →"}
          </button>
        </form>

        {/* RESEND */}

        <div className="otp-resend-area">
          <p>
            Didn't receive the email?
          </p>

          <button
            type="button"
            onClick={handleResend}
            disabled={
              cooldown > 0 ||
              resending
            }
            className="otp-resend-button"
          >
            {resending
              ? "Sending..."
              : cooldown > 0
              ? `Resend code in ${cooldown}s`
              : "Resend verification code"}
          </button>
        </div>

        <div className="auth-footer">
          <Link to="/login">
            ← Back to Sign In
          </Link>
        </div>

      </div>
    </div>
  );
}

export default VerifyEmail;