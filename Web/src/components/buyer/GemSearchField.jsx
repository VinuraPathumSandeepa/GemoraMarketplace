import { useId, useMemo, useState } from "react";
import { ArrowUpRight, Gem, Search, X } from "lucide-react";
import { buildGemSuggestions } from "./gemSearchSuggestions";

export default function GemSearchField({ gems, value, onChange }) {
  const listId = useId();
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(-1);
  const suggestions = useMemo(() => buildGemSuggestions(gems, value), [gems, value]);
  const visible = open && value.trim().length > 0;

  const select = (label) => {
    onChange(label);
    setOpen(false);
    setActive(-1);
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

  return <div className="gm-search-combobox" onBlur={(event) => {
    if (!event.currentTarget.contains(event.relatedTarget)) setOpen(false);
  }}>
    <div className="gm-search"><Search size={19} aria-hidden="true" /><input
      type="search"
      aria-label="Search gemstones"
      role="combobox"
      aria-autocomplete="list"
      aria-expanded={visible}
      aria-controls={visible ? listId : undefined}
      aria-activedescendant={visible && active >= 0 ? `${listId}-${active}` : undefined}
      autoComplete="off"
      value={value}
      placeholder="Search gemstone, colour, cut or seller…"
      onFocus={() => setOpen(true)}
      onChange={(event) => { onChange(event.target.value); setOpen(true); setActive(-1); }}
      onKeyDown={onKeyDown}
    />{value && <button className="gm-icon-button" type="button" aria-label="Clear search" onClick={() => select("")}><X size={18} /></button>}</div>
    {visible && <div className="gm-search-suggestions" id={listId} role="listbox" aria-label="Gemstone suggestions">
      <div className="gm-search-suggestions-heading">SUGGESTED SEARCHES <span>{suggestions.length ? `${suggestions.length} matches` : "Explore all gems"}</span></div>
      {suggestions.map((item, index) => <button key={item.key} id={`${listId}-${index}`} type="button" role="option" aria-selected={active === index} className="gm-search-suggestion" onPointerDown={(event) => event.preventDefault()} onClick={() => select(item.label)}>
        <span className="gm-search-suggestion-icon"><Gem size={18} strokeWidth={1.7} /></span>
        <span className="gm-search-suggestion-copy"><strong>{item.label}</strong><small>{item.detail}</small></span>
        <span className="gm-search-suggestion-kind">{item.kind}</span>
      </button>)}
      <button id={`${listId}-${suggestions.length}`} type="button" role="option" aria-selected={active === suggestions.length} className="gm-search-suggestion gm-search-suggestion-all" onPointerDown={(event) => event.preventDefault()} onClick={() => select(value.trim())}>
        <span className="gm-search-suggestion-icon"><Search size={17} /></span>
        <span className="gm-search-suggestion-copy"><strong>Search all for “{value.trim()}”</strong><small>View matching gemstones in the collection</small></span>
        <ArrowUpRight size={17} aria-hidden="true" />
      </button>
    </div>}
  </div>;
}
