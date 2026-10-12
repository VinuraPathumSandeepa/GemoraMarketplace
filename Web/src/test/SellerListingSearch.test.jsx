import { fireEvent, render, screen } from "@testing-library/react";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import SellerListingSearch from "../components/seller/SellerListingSearch";
import { buildListingSuggestions, matchesListingSearch } from "../components/seller/listingSearchSuggestions";

const listings = [
  { id: 1, title: "Royal Blue Sapphire", gemType: "Sapphire", color: "Royal Blue", cut: "Oval", status: "Approved", certificateNumber: "GIA-123" },
  { id: 2, title: "Ceylon Ruby", gemType: "Ruby", color: "Red", cut: "Cushion", status: "Draft", certificateNumber: "GIA-456" },
];

function SearchHarness() {
  const [value, setValue] = useState("");
  return <SellerListingSearch listings={listings} value={value} onChange={setValue} />;
}

describe("SellerListingSearch", () => {
  it("suggests seller listing titles, gemstone details and certificates", () => {
    expect(buildListingSuggestions(listings, "roy").map((item) => item.label)).toEqual(["Royal Blue Sapphire", "Royal Blue"]);
    expect(buildListingSuggestions(listings, "GIA").map((item) => item.label)).toEqual(["GIA-123", "GIA-456"]);
    expect(matchesListingSearch(listings[1], "gia-456")).toBe(true);
    expect(matchesListingSearch(listings[0], "ruby")).toBe(false);
  });

  it("selects a suggestion with the keyboard and clears the search", () => {
    render(<SearchHarness />);
    const input = screen.getByRole("combobox", { name: "Search gemstone listings" });
    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: "roy" } });
    expect(screen.getByRole("listbox", { name: "Listing suggestions" })).toBeInTheDocument();
    fireEvent.keyDown(input, { key: "ArrowDown" });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(input).toHaveValue("Royal Blue Sapphire");
    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Clear search" }));
    expect(input).toHaveValue("");
  });
});
