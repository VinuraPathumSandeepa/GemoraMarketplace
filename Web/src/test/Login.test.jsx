import { render, screen, fireEvent } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import Login from "../pages/Login";
import * as AuthContextModule from "../context/AuthContext";

vi.mock("../context/AuthContext", () => ({
  useAuth: vi.fn(),
}));

describe("Login Page", () => {
  it("renders branding, heading, form fields, and submit button", () => {
    vi.spyOn(AuthContextModule, "useAuth").mockReturnValue({
      login: vi.fn(),
    });

    render(
      <MemoryRouter>
        <Login />
      </MemoryRouter>
    );

    expect(screen.getByRole("heading", { name: "Gemora" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Sign in to Gemora" })).toBeInTheDocument();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /sign in/i })
    ).toBeInTheDocument();
  });

  it("updates email and password fields on user input", () => {
    vi.spyOn(AuthContextModule, "useAuth").mockReturnValue({
      login: vi.fn(),
    });

    render(
      <MemoryRouter>
        <Login />
      </MemoryRouter>
    );

    const emailInput = screen.getByLabelText(/email/i);
    const passwordInput = screen.getByLabelText(/password/i);

    fireEvent.change(emailInput, { target: { value: "test@gemora.com" } });
    fireEvent.change(passwordInput, { target: { value: "Secret123!" } });

    expect(emailInput.value).toBe("test@gemora.com");
    expect(passwordInput.value).toBe("Secret123!");
  });
});
