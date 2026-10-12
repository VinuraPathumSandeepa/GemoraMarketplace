import { render, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import ErrorBoundary from "../components/ErrorBoundary";

afterEach(() => vi.restoreAllMocks());

it("shows a recovery screen when a component throws during render", () => {
  vi.spyOn(console, "error").mockImplementation(() => {});
  function BrokenPage() {
    throw new ReferenceError("resolveApiAssetUrl is not defined");
  }

  render(<ErrorBoundary><BrokenPage /></ErrorBoundary>);

  expect(screen.getByRole("heading", { name: "We couldn't display this page" }))
    .toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Reload page" })).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Go to home" })).toHaveAttribute("href", "/");
});
