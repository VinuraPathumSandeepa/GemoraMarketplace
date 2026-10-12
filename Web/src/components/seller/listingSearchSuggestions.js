export function matchesListingSearch(listing, query) {
  const needle = query.trim().toLocaleLowerCase();
  if (!needle) return true;
  return [
    listing.title,
    listing.gemType,
    listing.color,
    listing.clarity,
    listing.cut,
    listing.certificateNumber,
    listing.certificateAuthority,
  ].some((value) => value?.toString().toLocaleLowerCase().includes(needle));
}

export function buildListingSuggestions(listings, query) {
  const needle = query.trim().toLocaleLowerCase();
  if (!needle) return [];

  const byRelevance = (a, b) =>
    Number(b.label.toLocaleLowerCase().startsWith(needle)) - Number(a.label.toLocaleLowerCase().startsWith(needle)) || a.label.localeCompare(b.label);

  const titles = listings
    .filter((listing) => listing.title?.toLocaleLowerCase().includes(needle))
    .map((listing) => ({
      key: `listing-${listing.id}`,
      label: listing.title,
      detail: [listing.gemType, listing.color, listing.status].filter(Boolean).join(" · "),
      kind: "Listing",
    }))
    .sort(byRelevance)
    .slice(0, 3);

  const facets = new Map();
  for (const listing of listings) {
    for (const [field, kind] of [["gemType", "Type"], ["color", "Colour"], ["cut", "Cut"], ["clarity", "Clarity"], ["certificateNumber", "Certificate"], ["certificateAuthority", "Authority"]]) {
      const label = listing[field]?.toString().trim();
      if (!label || !label.toLocaleLowerCase().includes(needle)) continue;
      const key = `${kind}-${label.toLocaleLowerCase()}`;
      if (!facets.has(key)) facets.set(key, { key, label, detail: `Search ${kind.toLocaleLowerCase()} in your listings`, kind });
    }
  }

  return [...titles, ...[...facets.values()].sort(byRelevance).slice(0, 3)];
}
