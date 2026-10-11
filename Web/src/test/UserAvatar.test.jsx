import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import UserAvatar from "../components/UserAvatar";

describe("UserAvatar Component", () => {
  it("renders user initials when profile image is not provided", () => {
    const user = { fullName: "Sarah Jenkins" };
    render(<UserAvatar user={user} />);

    const fallbackElement = screen.getByText("SJ");
    expect(fallbackElement).toBeInTheDocument();
    expect(fallbackElement).toHaveAttribute("aria-label", "Sarah Jenkins");
  });

  it("renders default fallback initial 'U' when name is missing", () => {
    render(<UserAvatar user={null} />);

    const fallbackElement = screen.getByText("U");
    expect(fallbackElement).toBeInTheDocument();
  });
});
