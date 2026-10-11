import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import GemoraSection from "../components/ui/GemoraSection";

describe("GemoraSection Component", () => {
  it("renders eyebrow, title, description, and children content", () => {
    render(
      <GemoraSection
        eyebrow="Marketplace Stats"
        title="Active Gemstone Listings"
        description="Browse certified gemstones available for export."
      >
        <div data-testid="section-content">Content Block</div>
      </GemoraSection>
    );

    expect(screen.getByText("Marketplace Stats")).toBeInTheDocument();
    expect(screen.getByText("Active Gemstone Listings")).toBeInTheDocument();
    expect(
      screen.getByText("Browse certified gemstones available for export.")
    ).toBeInTheDocument();
    expect(screen.getByTestId("section-content")).toBeInTheDocument();
  });
});