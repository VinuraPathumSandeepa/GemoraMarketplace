import {
    useEffect,
    useMemo,
    useState,
} from "react";

import { ArrowUpRight, SlidersHorizontal, X, Gem } from "lucide-react";

import { BuyerGuide, BuyingNotes, Dialog, EmptyState, SearchField, SkeletonGrid } from "../../components/buyer/BuyerUI";

import GemCard from "../../components/buyer/GemCard";

import {
    getAllMarketplaceGems,
} from "../../services/buyerApi";

export default function MarketplacePage() {
    const [gems, setGems] = useState([]);
    const [page, setPage] = useState(1);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    const [search, setSearch] = useState("");
    const [gemType, setGemType] = useState("All");
    const [sort, setSort] = useState("newest");

    const [minPrice, setMinPrice] = useState("");
    const [maxPrice, setMaxPrice] = useState("");

    const [filtersOpen, setFiltersOpen] =
        useState(false);

    useEffect(() => {
        loadMarketplace();
    }, []);

    async function loadMarketplace() {
        try {
            setLoading(true);
            setError("");

            const response =
                await getAllMarketplaceGems();

            setGems(
                response?.items || []
            );
        } catch (err) {
            setError(
                err.message ||
                "Unable to load marketplace."
            );
        } finally {
            setLoading(false);
        }
    }

    const gemTypes = useMemo(() => {
        const types = gems
            .map((gem) => gem.gemType)
            .filter(Boolean);

        return [
            "All",
            ...new Set(types),
        ];
    }, [gems]);

    const filteredGems = useMemo(() => {
        let result = [...gems];

        const query =
            search.trim().toLowerCase();

        if (query) {
            result = result.filter((gem) => {
                return (
                    gem.title
                        ?.toLowerCase()
                        .includes(query) ||
                    gem.gemType
                        ?.toLowerCase()
                        .includes(query) ||
                    gem.color
                        ?.toLowerCase()
                        .includes(query) ||
                    gem.cut
                        ?.toLowerCase()
                        .includes(query) ||
                    gem.clarity
                        ?.toLowerCase()
                        .includes(query) ||
                    gem.sellerName
                        ?.toLowerCase()
                        .includes(query)
                );
            });
        }

        if (gemType !== "All") {
            result = result.filter(
                (gem) =>
                    gem.gemType === gemType
            );
        }

        if (minPrice !== "") {
            result = result.filter(
                (gem) =>
                    Number(gem.price) >=
                    Number(minPrice)
            );
        }

        if (maxPrice !== "") {
            result = result.filter(
                (gem) =>
                    Number(gem.price) <=
                    Number(maxPrice)
            );
        }

        switch (sort) {
            case "price-low":
                result.sort(
                    (a, b) =>
                        Number(a.price) -
                        Number(b.price)
                );
                break;

            case "price-high":
                result.sort(
                    (a, b) =>
                        Number(b.price) -
                        Number(a.price)
                );
                break;

            case "carat-high":
                result.sort(
                    (a, b) =>
                        Number(b.caratWeight || 0) -
                        Number(a.caratWeight || 0)
                );
                break;

            case "name":
                result.sort((a, b) =>
                    (a.title || "").localeCompare(
                        b.title || ""
                    )
                );
                break;

            default:
                result.sort(
                    (a, b) =>
                        Number(b.id || 0) -
                        Number(a.id || 0)
                );
                break;
        }

        return result;
    }, [
        gems,
        search,
        gemType,
        minPrice,
        maxPrice,
        sort,
    ]);

    function clearFilters() {
        setSearch("");
        setGemType("All");
        setMinPrice("");
        setMaxPrice("");
        setSort("newest");
    }

    const hasFilters =
        search ||
        gemType !== "All" ||
        minPrice ||
        maxPrice;

    const pageSize = 12;
    const totalPages = Math.max(1, Math.ceil(filteredGems.length / pageSize));
    const currentPage = Math.min(page, totalPages);
    const activeCount = Number(gemType !== "All") + Number(minPrice !== "") + Number(maxPrice !== "");
    const changeFilter = (setter) => (event) => { setter(event.target.value); setPage(1); };
    const filterFields = (suffix) => <div className="gm-filter-fields">
      <label htmlFor={`gem-type-${suffix}`}>Gemstone type<select id={`gem-type-${suffix}`} value={gemType} onChange={changeFilter(setGemType)}>{gemTypes.map(type => <option key={type}>{type}</option>)}</select></label>
      <fieldset><legend>Price range · LKR</legend><label htmlFor={`min-${suffix}`}>Minimum<input id={`min-${suffix}`} type="number" min="0" placeholder="No minimum" value={minPrice} onChange={changeFilter(setMinPrice)} /></label><label htmlFor={`max-${suffix}`}>Maximum<input id={`max-${suffix}`} type="number" min="0" placeholder="No maximum" value={maxPrice} onChange={changeFilter(setMaxPrice)} /></label></fieldset>
      <button className="gm-button gm-button-secondary" onClick={() => { clearFilters(); setPage(1); }}>Clear all filters</button>
    </div>;
    return <div className="gm-page gm-marketplace">
      <section className="gm-market-hero"><div><span className="gm-eyebrow"><span className="gm-dot" />THE CEYLON COLLECTION</span><h1>Find a gem.<br /><em>Make it yours.</em></h1><p>Discover Sri Lankan gemstones, explore their individual character, and choose with a little more confidence.</p><a className="gm-text-link" href="#gem-collection">Explore the collection <ArrowUpRight size={18} /></a></div><div className="gm-market-visual"><img src="/images/gems/sapphire.webp" alt="Faceted sapphire, an illustration of Ceylon's gemstone heritage" width="600" height="600" loading="lazy" /><span className="gm-glass gm-visual-caption"><Gem size={20} /><span>Extraordinary by nature.<small>Discover the details in every stone.</small></span></span></div></section>
      <div className="gm-market-toolbar gm-glass"><SearchField label="Search gemstones" value={search} onChange={changeFilter(setSearch)} placeholder="Search gemstone, colour, cut or seller…" /><div className="gm-toolbar-actions"><label className="gm-sort"><span className="gm-sr-only">Sort gemstones</span><select value={sort} onChange={changeFilter(setSort)}><option value="newest">Newest listings</option><option value="price-low">Price: low to high</option><option value="price-high">Price: high to low</option><option value="carat-high">Carat: high to low</option><option value="name">Name: A–Z</option></select></label><button className="gm-button gm-button-secondary gm-filter-toggle" onClick={() => setFiltersOpen(true)}><SlidersHorizontal size={18} />Filters{activeCount > 0 && <span>({activeCount})</span>}</button></div></div>
      {hasFilters && <div className="gm-active-filters" aria-label="Active filters">{[[search, () => setSearch(""), `Search: ${search}`], [gemType !== "All", () => setGemType("All"), gemType], [minPrice, () => setMinPrice(""), `From LKR ${minPrice}`], [maxPrice, () => setMaxPrice(""), `To LKR ${maxPrice}`]].filter(([show]) => show).map(([, clear, label]) => <button key={label} onClick={() => { clear(); setPage(1); }} aria-label={`Remove filter ${label}`}>{label}<X size={15} /></button>)}</div>}
      <div className="gm-market-layout" id="gem-collection"><aside className="gm-filter-sidebar gm-glass"><div className="gm-section-heading"><h2>Refine your search</h2><SlidersHorizontal size={19} /></div>{filterFields("desktop")}<div className="gm-filter-tip"><Gem size={24} strokeWidth={1.2} /><h3>A considered choice</h3><p>Compare colour, clarity and cut alongside carat weight. Each gemstone has its own character.</p></div></aside>
        <section className="gm-results" aria-label="Gemstone collection"><div className="gm-section-heading"><div><span className="gm-eyebrow">CURATED BY NATURE</span><h2>The collection</h2></div><span className="gm-result-count" role="status">{loading ? "Finding your gems…" : `${filteredGems.length} gemstone${filteredGems.length === 1 ? "" : "s"}`}</span></div>
          {error && <EmptyState title="The collection is taking a moment" description={error}><button className="gm-button" onClick={loadMarketplace}>Try again</button></EmptyState>}
          {loading && <SkeletonGrid />}
          {!loading && !error && (filteredGems.length ? <><div className="gm-gem-grid">{filteredGems.slice((currentPage - 1) * pageSize, currentPage * pageSize).map(gem => <GemCard gem={gem} key={gem.id} />)}</div><nav className="gm-pagination" aria-label="Collection pages"><button className="gm-button gm-button-secondary" disabled={currentPage === 1} onClick={() => setPage(currentPage - 1)}>Previous</button><span aria-live="polite">Page {currentPage} of {totalPages}</span><button className="gm-button gm-button-secondary" disabled={currentPage === totalPages} onClick={() => setPage(currentPage + 1)}>Next</button></nav></> : <EmptyState title="A different search may uncover your gem" description="Try another gemstone type or a wider price range."><button className="gm-button" onClick={clearFilters}>Clear filters</button></EmptyState>)}
        </section></div>
      <BuyerGuide /><BuyingNotes />
      <Dialog open={filtersOpen} onClose={() => setFiltersOpen(false)} title="Find your gemstone" className="gm-filter-dialog">{filterFields("mobile")}<button className="gm-button gm-full" onClick={() => setFiltersOpen(false)}>Show {filteredGems.length} gemstones</button></Dialog>
    </div>;
}
