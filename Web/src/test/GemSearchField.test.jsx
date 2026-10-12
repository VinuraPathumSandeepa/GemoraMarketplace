import { fireEvent, render, screen } from "@testing-library/react";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import GemSearchField from "../components/buyer/GemSearchField";
import { buildGemSuggestions } from "../components/buyer/gemSearchSuggestions";

const gems = [
  { id: 1, title: "Royal Blue Sapphire", gemType: "Sapphire", color: "Royal Blue", cut: "Oval", sellerName: "Gem House", caratWeight: 2.4 },
  { id: 2, title: "Ceylon Blue Sapphire", gemType: "Sapphire", color: "Blue", cut: "Oval", sellerName: "Gem House", caratWeight: 1.8 },
  { id: 3, title: "Ruby Flame", gemType: "Ruby", color: "Red", cut: "Cushion", sellerName: "Ceylon Gems", caratWeight: 1.2 },
];

function SearchHarness() {
  const [value, setValue] = useState("");
  return <GemSearchField gems={gems} value={value} onChange={setValue} />;
}

describe("GemSearchField", () => {
  it("suggests matching listings and unique facets from marketplace data", () => {
    const suggestions = buildGemSuggestions(gems, "sap");
    expect(suggestions.map((item) => item.label)).toEqual([
      "Ceylon Blue Sapphire",
      "Royal Blue Sapphire",
      "Sapphire",
    ]);
    expect(buildGemSuggestions(gems, "BLUE").some((item) => item.label === "Blue")).toBe(true);
  });

  it("supports keyboard selection and closes the list after choosing a suggestion", () => {
    render(<SearchHarness />);
    const input = screen.getByRole("combobox", { name: "Search gemstones" });
    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: "sap" } });
    expect(screen.getByRole("listbox", { name: "Gemstone suggestions" })).toBeInTheDocument();
    fireEvent.keyDown(input, { key: "ArrowDown" });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(input).toHaveValue("Ceylon Blue Sapphire");
    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
  });

  it("offers a search action even when no listing matches", () => {
    render(<SearchHarness />);
    const input = screen.getByRole("combobox", { name: "Search gemstones" });
    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: "alexandrite" } });
    fireEvent.click(screen.getByRole("option", { name: /Search all for “alexandrite”/ }));
    expect(input).toHaveValue("alexandrite");
    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
  });
});
