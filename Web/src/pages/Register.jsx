import { useState } from "react";
import {
  Link,
  useNavigate,
} from "react-router-dom";

import api from "../services/api";
import "../styles/Register.css";


// ============================================================
// COUNTRY OPTIONS
// ============================================================

const countries = [
  {
    code: "LK",
    name: "Sri Lanka",
    dialCode: "+94",
  },
  {
    code: "IN",
    name: "India",
    dialCode: "+91",
  },
  {
    code: "SG",
    name: "Singapore",
    dialCode: "+65",
  },
  {
    code: "TH",
    name: "Thailand",
    dialCode: "+66",
  },
  {
    code: "AU",
    name: "Australia",
    dialCode: "+61",
  },
  {
    code: "GB",
    name: "United Kingdom",
    dialCode: "+44",
  },
  {
    code: "US",
    name: "United States",
    dialCode: "+1",
  },
  {
    code: "CA",
    name: "Canada",
    dialCode: "+1",
  },
  {
    code: "AE",
    name: "United Arab Emirates",
    dialCode: "+971",
  },
  {
    code: "JP",
    name: "Japan",
    dialCode: "+81",
  },
  {
    code: "MY",
    name: "Malaysia",
    dialCode: "+60",
  },
  {
    code: "ID",
    name: "Indonesia",
    dialCode: "+62",
  },
  {
    code: "BD",
    name: "Bangladesh",
    dialCode: "+880",
  },
  {
    code: "PK",
    name: "Pakistan",
    dialCode: "+92",
  },
  {
    code: "NZ",
    name: "New Zealand",
    dialCode: "+64",
  },
];


function Register() {
  const navigate = useNavigate();

  const [formData, setFormData] = useState({
    fullName: "",
    email: "",
    phoneNumber: "",
    phoneDialCode: "+94",
    countryCode: "LK",
    region: "",
    password: "",
    confirmPassword: "",
    role: "Buyer",
  });

  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  const handleChange = (event) => {
    const { name, value } = event.target;

    setFormData((previous) => ({
      ...previous,
      [name]: value,
    }));
  };


  // ============================================================
  // COUNTRY CHANGE
  //
  // Automatically updates both:
  // - ISO country
  // - phone dialing code
  // ============================================================

  const handleCountryChange = (event) => {
    const countryCode =
      event.target.value;

    const selectedCountry =
      countries.find(
        (country) =>
          country.code ===
          countryCode
      );

    setFormData((previous) => ({
      ...previous,

      countryCode,

      phoneDialCode:
        selectedCountry?.dialCode ||
        previous.phoneDialCode,
    }));

    setError("");
  };


  // ============================================================
  // PHONE NUMBER CHANGE
  //
  // Only allow digits.
  // ============================================================

  const handlePhoneChange = (event) => {
    const value =
      event.target.value
        .replace(/\D/g, "")
        .slice(0, 15);

    setFormData((previous) => ({
      ...previous,
      phoneNumber: value,
    }));

    setError("");
  };


  // ============================================================
  // BUILD INTERNATIONAL PHONE NUMBER
  // ============================================================

  const buildPhoneNumber = () => {
    let localNumber =
      formData.phoneNumber.trim();

    /*
      Example:

      User enters:
      0771234567

      Selected:
      +94

      Result:
      +94771234567
    */

    if (localNumber.startsWith("0")) {
      localNumber =
        localNumber.substring(1);
    }

    return (
      formData.phoneDialCode +
      localNumber
    );
  };


  // ============================================================
  // VALIDATION
  // ============================================================

  const validateForm = () => {
    if (
      formData.fullName
        .trim()
        .length < 2
    ) {
      return (
        "Please enter your full name."
      );
    }


    if (!formData.email.trim()) {
      return (
        "Please enter your email address."
      );
    }


    if (
      formData.phoneNumber
        .trim()
        .length < 7
    ) {
      return (
        "Please enter a valid mobile number."
      );
    }


    const completePhoneNumber =
      buildPhoneNumber();


    if (
      !/^\+[1-9]\d{6,14}$/.test(
        completePhoneNumber
      )
    ) {
      return (
        "Please enter a valid mobile number."
      );
    }


    if (
      !/^[A-Za-z]{2}$/.test(
        formData.countryCode
      )
    ) {
      return (
        "Please select a valid country."
      );
    }


    if (
      formData.region
        .trim()
        .length < 2
    ) {
      return (
        "Please enter your province, state, or region."
      );
    }


    if (
      formData.password.length < 6
    ) {
      return (
        "Password must contain at least 6 characters."
      );
    }


    if (
      formData.password !==
      formData.confirmPassword
    ) {
      return (
        "Passwords do not match. Please re-enter your password."
      );
    }


    return "";
  };


  // ============================================================
  // REGISTER
  // ============================================================

  const handleSubmit = async (event) => {
    event.preventDefault();

    setError("");
    setSuccess("");

    const validationError = validateForm();
    if (validationError) {
      setError(validationError);
      return;
    }

    const payload = {
      fullName: formData.fullName.trim(),
      email: formData.email.trim(),
      phoneNumber: buildPhoneNumber(),
      countryCode: formData.countryCode,
      region: formData.region.trim(),
      password: formData.password,
      role: formData.role,
    };

    setSubmitting(true);

    try {
      await api.post("/Auth/register", payload);

      setSuccess(
        "Registration successful. Redirecting to email verification..."
      );


      sessionStorage.setItem(
        "gemora_pending_verification_email",
        payload.email
      );


      setTimeout(() => {
        navigate(
          "/verify-email",
          {
            state: {
              email: payload.email,
            },
          }
        );
      }, 800);

    } catch (error) {
      setError(
        error.response?.data?.message ||
          "Registration failed. Please check your information and try again."
      );

      console.error("Registration failed:", error);

      // Show detailed validation errors from backend
      if (error.response?.status === 409) {
        setError("An account with this email already exists. Please use a different email or try logging in.");
      } else if (error.response?.data?.errors) {
        const errorMessages = Object.entries(error.response.data.errors)
          .map(([field, messages]) => `${field}: ${messages.join(", ")}`)
          .join("\n");
        setError(errorMessages);
      } else {
        setError(
          error.response?.data?.message ||
            "Registration failed. Please check your input and try again."
        );
      }
    } finally {
      setSubmitting(false);
    }
  };


  // ============================================================
  // PAGE
  // ============================================================

  return (
    <div className="register-page">

      <div className="register-card">

        {/* ====================================================
            BRAND
            ==================================================== */}

        <div className="auth-brand">

          <h1>
            Gemora
          </h1>

          <p>
            Secure Gemstone Marketplace
          </p>

        </div>


        {/* ====================================================
            HEADING
            ==================================================== */}

        <div className="auth-heading">

          <span>
            CREATE YOUR ACCOUNT
          </span>

          <h2>
            Join Gemora
          </h2>

        <p>Gem Marketplace Management System</p>

        </div>


        {/* ====================================================
            ERROR
            ==================================================== */}

        {error && (
          <div className="error-message">
            {error}
          </div>
        )}


        {/* ====================================================
            SUCCESS
            ==================================================== */}

        {success && (
          <div className="success-message">
            {success}
          </div>
        )}


        {/* ====================================================
            FORM
            ==================================================== */}

        <form
          onSubmit={handleSubmit}
          className="register-form"
        >

          {/* FULL NAME */}

          <div className="form-group">

            <label htmlFor="fullName">
              Full Name
            </label>

            <input
              id="fullName"
              type="text"
              name="fullName"
              value={formData.fullName}
              onChange={handleChange}
              placeholder="Enter your full name"
              required
              minLength={2}
              maxLength={100}
              autoComplete="name"
            />

          </div>


          {/* EMAIL */}

          <div className="form-group">

            <label htmlFor="email">
              Email
            </label>

            <input
              id="email"
              type="email"
              name="email"
              value={formData.email}
              onChange={handleChange}
              placeholder="you@example.com"
              required
              autoComplete="email"
            />

          </div>


          {/* ==================================================
              MOBILE NUMBER
              ================================================== */}

          <div className="form-group">

            <label htmlFor="phoneNumber">
              Mobile Number
            </label>


            <div className="phone-input-wrapper">

              {/* COUNTRY DIAL CODE */}

              <select
                className="phone-code-select"
                name="phoneDialCode"
                value={
                  formData.phoneDialCode
                }
                onChange={handleChange}
                aria-label="Phone country code"
              >

                {countries.map(
                  (country) => (
                    <option
                      key={
                        `${country.code}-${country.dialCode}`
                      }
                      value={
                        country.dialCode
                      }
                    >
                      {country.dialCode}{" "}
                      {country.name}
                    </option>
                  )
                )}

              </select>


              {/* PHONE */}

              <input
                id="phoneNumber"
                className="phone-number-input"
                type="tel"
                name="phoneNumber"
                value={
                  formData.phoneNumber
                }
                onChange={
                  handlePhoneChange
                }
                placeholder="771234567"
                required
                autoComplete="tel-national"
                inputMode="numeric"
              />

            </div>


            <small className="form-helper">

              Select your country code and
              enter your mobile number.

            </small>

          </div>


          {/* ==================================================
              COUNTRY
              ================================================== */}

          <div className="form-group">

            <label htmlFor="countryCode">
              Country
            </label>

            <select
              id="countryCode"
              name="countryCode"
              value={
                formData.countryCode
              }
              onChange={
                handleCountryChange
              }
              required
            >

              {countries.map(
                (country) => (
                  <option
                    key={
                      country.code
                    }
                    value={
                      country.code
                    }
                  >
                    {country.name}
                  </option>
                )
              )}

            </select>

          </div>


          {/* ==================================================
              REGION
              ================================================== */}

          <div className="form-group">

            <label htmlFor="region">
              Province / State / Region
            </label>

            <input
              id="region"
              type="text"
              name="region"
              value={formData.region}
              onChange={handleChange}
              placeholder="Example: Western Province"
              required
              minLength={2}
              maxLength={100}
              autoComplete="address-level1"
            />

          </div>


          {/* ==================================================
              PASSWORD
              ================================================== */}

          <div className="form-group">

            <label htmlFor="password">
              Password
            </label>


            <div className="password-input-wrapper">

              <input
                id="password"
                type={
                  showPassword
                    ? "text"
                    : "password"
                }
                name="password"
                value={
                  formData.password
                }
                onChange={
                  handleChange
                }
                placeholder="Minimum 6 characters"
                required
                minLength={6}
                maxLength={100}
                autoComplete="new-password"
              />


              <button
                type="button"
                className="password-eye-button"
                onClick={() =>
                  setShowPassword(
                    (current) =>
                      !current
                  )
                }
                aria-label={
                  showPassword
                    ? "Hide password"
                    : "Show password"
                }
                title={
                  showPassword
                    ? "Hide password"
                    : "Show password"
                }
              >

                {showPassword ? (
                  /* EYE OFF */

                  <svg
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path
                      d="M3 3l18 18"
                    />

                    <path
                      d="M10.6 10.6a2 2 0 0 0 2.8 2.8"
                    />

                    <path
                      d="M9.9 4.2A10.8 10.8 0 0 1 12 4c5.5 0 9 5 9 5a15.7 15.7 0 0 1-2.1 2.7"
                    />

                    <path
                      d="M6.6 6.6C4.4 8 3 10 3 10s3.5 5 9 5c1.1 0 2.1-.2 3-.5"
                    />
                  </svg>

                ) : (
                  /* EYE */

                  <svg
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path
                      d="M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6z"
                    />

                    <circle
                      cx="12"
                      cy="12"
                      r="2.5"
                    />
                  </svg>
                )}

              </button>

            </div>

          </div>


          {/* ==================================================
              CONFIRM PASSWORD
              ================================================== */}

          <div className="form-group">

            <label htmlFor="confirmPassword">
              Confirm Password
            </label>


            <div className="password-input-wrapper">

              <input
                id="confirmPassword"
                type={
                  showConfirmPassword
                    ? "text"
                    : "password"
                }
                name="confirmPassword"
                value={
                  formData.confirmPassword
                }
                onChange={
                  handleChange
                }
                placeholder="Re-enter your password"
                required
                minLength={6}
                maxLength={100}
                autoComplete="new-password"
              />


              <button
                type="button"
                className="password-eye-button"
                onClick={() =>
                  setShowConfirmPassword(
                    (current) =>
                      !current
                  )
                }
                aria-label={
                  showConfirmPassword
                    ? "Hide confirm password"
                    : "Show confirm password"
                }
                title={
                  showConfirmPassword
                    ? "Hide password"
                    : "Show password"
                }
              >

                {showConfirmPassword ? (
                  <svg
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path
                      d="M3 3l18 18"
                    />

                    <path
                      d="M10.6 10.6a2 2 0 0 0 2.8 2.8"
                    />

                    <path
                      d="M9.9 4.2A10.8 10.8 0 0 1 12 4c5.5 0 9 5 9 5a15.7 15.7 0 0 1-2.1 2.7"
                    />

                    <path
                      d="M6.6 6.6C4.4 8 3 10 3 10s3.5 5 9 5c1.1 0 2.1-.2 3-.5"
                    />
                  </svg>

                ) : (
                  <svg
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path
                      d="M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6z"
                    />

                    <circle
                      cx="12"
                      cy="12"
                      r="2.5"
                    />
                  </svg>
                )}

              </button>

            </div>


            {formData.confirmPassword &&
              formData.password !==
                formData.confirmPassword && (
                <small className="password-mismatch">
                  Passwords do not match.
                </small>
              )}


            {formData.confirmPassword &&
              formData.password ===
                formData.confirmPassword && (
                <small className="password-match">
                  ✓ Passwords match.
                </small>
              )}

          </div>


          {/* ==================================================
              ACCOUNT TYPE
              ================================================== */}

          <div className="form-group">

            <label htmlFor="role">
              Account Type
            </label>

            <select
              id="role"
              name="role"
              value={
                formData.role
              }
              onChange={
                handleChange
              }
            >

              <option value="Buyer">
                Buyer
              </option>

              <option value="Seller">
                Seller
              </option>

            </select>

          </div>


          {/* ==================================================
              SUBMIT
              ================================================== */}

          <button
            type="submit"
            disabled={
              submitting ||
              (
                formData.confirmPassword &&
                formData.password !==
                  formData.confirmPassword
              )
            }
            className="auth-primary-button"
          >

            {submitting
              ? "Creating Account..."
              : "Create Account →"}

          </button>

        </form>


        {/* ====================================================
            FOOTER
            ==================================================== */}

        <div className="auth-footer">

          <p>
            Already have an account?{" "}

            <Link to="/login">
              Sign In
            </Link>
          </p>

        </div>

      </div>

    </div>
  );
}

export default Register;
