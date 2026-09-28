import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import api from "../services/api";
import "../styles/Register.css";

function Register() {
  const navigate = useNavigate();

  const [formData, setFormData] = useState({
    fullName: "",
    email: "",
    password: "",
    role: "Buyer",
    region: "",
    countryCode: "",
    phoneNumber: "",
  });

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

      setSuccess(
        "Registration successful. Redirecting to login..."
      );

      setTimeout(() => {
        navigate("/login");
      }, 1500);
    } catch (error) {
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

  return (
    <div className="register-page">
      <div className="register-card">
        <h1>Gemora</h1>

        <p>Gem Marketplace Management System</p>

        <h2>Create Account</h2>

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

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Full Name</label>

            <input
              type="text"
              name="fullName"
              value={formData.fullName}
              onChange={handleChange}
              placeholder="Enter your full name"
              required
              minLength={2}
              maxLength={100}
            />
          </div>

          <div className="form-group">
            <label>Email</label>

            <input
              type="email"
              name="email"
              value={formData.email}
              onChange={handleChange}
              placeholder="Enter your email"
              required
            />
          </div>

          <div className="form-group">
            <label>Password</label>

            <input
              type="password"
              name="password"
              value={formData.password}
              onChange={handleChange}
              placeholder="Minimum 6 characters"
              required
              minLength={6}
            />
          </div>

          <div className="form-group">
            <label>Account Type</label>

            <select
              name="role"
              value={formData.role}
              onChange={handleChange}
            >
              <option value="Buyer">
                Buyer
              </option>

              <option value="Seller">
                Seller
              </option>
            </select>
          </div>

          <div className="form-group">
            <label>Region</label>

            <input
              type="text"
              name="region"
              value={formData.region}
              onChange={handleChange}
              placeholder="Enter your region/state"
              required
              minLength={2}
              maxLength={100}
            />
          </div>

          <div className="form-group">
            <label>Country Code</label>

            <input
              type="text"
              name="countryCode"
              value={formData.countryCode}
              onChange={handleChange}
              placeholder="2-letter country code (e.g., US, LK)"
              required
              minLength={2}
              maxLength={2}
              pattern="[A-Za-z]{2}"
            />
          </div>

          <div className="form-group">
            <label>Phone Number</label>

            <input
              type="tel"
              name="phoneNumber"
              value={formData.phoneNumber}
              onChange={handleChange}
              placeholder="+94771234567 (international format)"
              required
              minLength={7}
              maxLength={20}
              pattern="\+[1-9]\d{6,14}"
            />
          </div>

          <button
            type="submit"
            disabled={submitting}
          >
            {submitting
              ? "Creating Account..."
              : "Create Account"}
          </button>
        </form>

        <p>
          Already have an account?{" "}
          <Link to="/login">
            Sign In
          </Link>
        </p>
      </div>
    </div>
  );
}

export default Register;