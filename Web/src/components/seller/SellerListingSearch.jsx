import { useId, useMemo, useState } from "react";
import { ArrowUpRight, FileText, Gem, Search, X } from "lucide-react";
import { buildListingSuggestions } from "./listingSearchSuggestions";

export default function SellerListingSearch({ listings, value, onChange }) {
  const listId = useId();
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(-1);
  const suggestions = useMemo(() => buildListingSuggestions(listings, value), [listings, value]);
  const visible = open && value.trim().length > 0;

  const select = (label) => {
    onChange(label);
    setActive(-1);
    setOpen(false);
  };

  const onKeyDown = (event) => {
    if (event.key === "Escape") {
      setOpen(false);
      setActive(-1);
    } else if (event.key === "ArrowDown" && visible) {
      event.preventDefault();
      setActive((index) => (index + 1) % (suggestions.length + 1));
    } else if (event.key === "ArrowUp" && visible) {
      event.preventDefault();
      setActive((index) => index <= 0 ? suggestions.length : index - 1);
    } else if (event.key === "Enter" && visible) {
      event.preventDefault();
      select(active >= 0 && active < suggestions.length ? suggestions[active].label : value.trim());
    }
  };

  return <div className="seller-listings-search-wrap" onBlur={(event) => {
    if (!event.currentTarget.contains(event.relatedTarget)) setOpen(false);
  }}>
    <div className="seller-listings-search">
      <Search className="seller-search-icon" size={19} aria-hidden="true" />
      <input
        type="search"
        role="combobox"
        aria-label="Search gemstone listings"
        aria-autocomplete="list"
        aria-expanded={visible}
        aria-controls={visible ? listId : undefined}
        aria-activedescendant={visible && active >= 0 ? `${listId}-${active}` : undefined}
        autoComplete="off"
        value={value}
        onFocus={() => setOpen(true)}
        onChange={(event) => { onChange(event.target.value); setOpen(true); setActive(-1); }}
        onKeyDown={onKeyDown}
        placeholder="Search title, type, colour or certificate..."
      />
      {value && <button type="button" className="seller-search-clear" onClick={() => select("")} aria-label="Clear search"><X size={16} /></button>}
    </div>
    {visible && <div className="seller-search-suggestions" id={listId} role="listbox" aria-label="Listing suggestions">
      <div className="seller-search-suggestions-heading">SUGGESTED SEARCHES <span>{suggestions.length ? `${suggestions.length} matches` : "Search your listings"}</span></div>
      {suggestions.map((item, index) => <button key={item.key} type="button" id={`${listId}-${index}`} role="option" aria-selected={active === index} className="seller-search-suggestion" onPointerDown={(event) => event.preventDefault()} onClick={() => select(item.label)}>
        <span className="seller-search-suggestion-icon">{item.kind === "Certificate" || item.kind === "Authority" ? <FileText size={18} /> : <Gem size={18} />}</span>
        <span className="seller-search-suggestion-copy"><strong>{item.label}</strong><small>{item.detail}</small></span>
        <span className="seller-search-suggestion-kind">{item.kind}</span>
      </button>)}
      <button type="button" id={`${listId}-${suggestions.length}`} role="option" aria-selected={active === suggestions.length} className="seller-search-suggestion seller-search-suggestion-all" onPointerDown={(event) => event.preventDefault()} onClick={() => select(value.trim())}>
        <span className="seller-search-suggestion-icon"><Search size={17} /></span>
        <span className="seller-search-suggestion-copy"><strong>Search all for “{value.trim()}”</strong><small>Find matches in your listings</small></span>
        <ArrowUpRight size={18} aria-hidden="true" />
      </button>
    </div>}
  </div>;
}
