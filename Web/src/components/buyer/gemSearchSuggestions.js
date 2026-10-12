export function buildGemSuggestions(gems, query) {
  const needle = query.trim().toLocaleLowerCase();
  if (!needle) return [];

  const listings = gems
    .filter((gem) => gem.title?.toLocaleLowerCase().includes(needle))
    .map((gem) => ({
      key: `listing-${gem.id}`,
      label: gem.title,
      detail: [gem.gemType, gem.color, gem.caratWeight != null ? `${gem.caratWeight} ct` : null].filter(Boolean).join(" · "),
      kind: "Gemstone",
    }))
    .sort((a, b) => Number(b.label.toLocaleLowerCase().startsWith(needle)) - Number(a.label.toLocaleLowerCase().startsWith(needle)) || a.label.localeCompare(b.label))
    .slice(0, 3);

  const facets = new Map();
  for (const gem of gems) {
    for (const [field, kind] of [["gemType", "Type"], ["color", "Colour"], ["cut", "Cut"], ["sellerName", "Seller"]]) {
      const label = gem[field]?.trim();
      if (!label || !label.toLocaleLowerCase().includes(needle)) continue;
      const key = `${kind}-${label.toLocaleLowerCase()}`;
      if (!facets.has(key)) facets.set(key, { key, label, detail: `Explore ${kind.toLocaleLowerCase()} matches`, kind });
    }
  }

  return [...listings, ...[...facets.values()]
    .sort((a, b) => Number(b.label.toLocaleLowerCase().startsWith(needle)) - Number(a.label.toLocaleLowerCase().startsWith(needle)) || a.label.localeCompare(b.label))
    .slice(0, 3)];
}
