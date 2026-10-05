import {
    useEffect,
    useMemo,
    useState,
} from "react";

import {
    Search,
    SlidersHorizontal,
    Gem,
    RefreshCw,
    X,
    ChevronDown,
} from "lucide-react";

import { motion } from "framer-motion";

import GemCard from "../../components/buyer/GemCard";

import {
    getAllMarketplaceGems,
} from "../../services/buyerApi";

export default function MarketplacePage() {
    const [gems, setGems] = useState([]);
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

    return (
        <div className="marketplace-page">
            {/* =====================================
          HERO
      ===================================== */}

            <motion.section
                className="marketplace-hero"
                initial={{
                    opacity: 0,
                    y: 40,
                }}
                animate={{
                    opacity: 1,
                    y: 0,
                }}
                transition={{
                    duration: 0.7,
                }}
            >
                <div className="marketplace-hero-orb marketplace-orb-one" />
                <div className="marketplace-hero-orb marketplace-orb-two" />

                <div className="marketplace-hero-content">
                    <span className="marketplace-eyebrow">
                        <Gem size={16} />
                        Verified Ceylon Gemstones
                    </span>

                    <h1>
                        Discover your next
                        <span> exceptional gem.</span>
                    </h1>

                    <p>
                        Explore verified gemstone
                        listings from trusted sellers.
                        Compare specifications,
                        certificates and pricing before
                        making your purchase.
                    </p>
                </div>

                <div className="marketplace-counter-card">
                    <span>
                        Available Marketplace
                    </span>

                    <strong>
                        {loading
                            ? "—"
                            : gems.length}
                    </strong>

                    <p>
                        Verified listings currently
                        available
                    </p>
                </div>
            </motion.section>

            {/* =====================================
          SEARCH
      ===================================== */}

            <motion.section
                className="marketplace-toolbar liquid-card"
                initial={{
                    opacity: 0,
                    y: 35,
                }}
                animate={{
                    opacity: 1,
                    y: 0,
                }}
                transition={{
                    delay: 0.1,
                    duration: 0.65,
                }}
            >
                <div className="marketplace-search">
                    <Search size={20} />

                    <input
                        type="text"
                        value={search}
                        placeholder="Search sapphire, ruby, color, cut, seller..."
                        onChange={(event) =>
                            setSearch(
                                event.target.value
                            )
                        }
                    />

                    {search && (
                        <button
                            type="button"
                            onClick={() =>
                                setSearch("")
                            }
                        >
                            <X size={17} />
                        </button>
                    )}
                </div>

                <div className="marketplace-toolbar-actions">
                    <div className="marketplace-sort">
                        <select
                            value={sort}
                            onChange={(event) =>
                                setSort(
                                    event.target.value
                                )
                            }
                        >
                            <option value="newest">
                                Newest Listings
                            </option>

                            <option value="price-low">
                                Price: Low to High
                            </option>

                            <option value="price-high">
                                Price: High to Low
                            </option>

                            <option value="carat-high">
                                Carat: High to Low
                            </option>

                            <option value="name">
                                Name: A–Z
                            </option>
                        </select>

                        <ChevronDown size={16} />
                    </div>

                    <button
                        type="button"
                        className={`marketplace-filter-toggle ${filtersOpen
                            ? "active"
                            : ""
                            }`}
                        onClick={() =>
                            setFiltersOpen(
                                (current) =>
                                    !current
                            )
                        }
                    >
                        <SlidersHorizontal
                            size={18}
                        />

                        Filters
                    </button>
                </div>
            </motion.section>

            {/* =====================================
          FILTER PANEL
      ===================================== */}

            {filtersOpen && (
                <motion.section
                    className="marketplace-filter-panel liquid-card"
                    initial={{
                        opacity: 0,
                        height: 0,
                        y: -15,
                    }}
                    animate={{
                        opacity: 1,
                        height: "auto",
                        y: 0,
                    }}
                    exit={{
                        opacity: 0,
                        height: 0,
                    }}
                >
                    <div className="filter-field">
                        <label>
                            Gem Type
                        </label>

                        <select
                            value={gemType}
                            onChange={(event) =>
                                setGemType(
                                    event.target.value
                                )
                            }
                        >
                            {gemTypes.map(
                                (type) => (
                                    <option
                                        value={type}
                                        key={type}
                                    >
                                        {type}
                                    </option>
                                )
                            )}
                        </select>
                    </div>

                    <div className="filter-field">
                        <label>
                            Minimum Price
                        </label>

                        <input
                            type="number"
                            min="0"
                            value={minPrice}
                            placeholder="LKR 0"
                            onChange={(event) =>
                                setMinPrice(
                                    event.target.value
                                )
                            }
                        />
                    </div>

                    <div className="filter-field">
                        <label>
                            Maximum Price
                        </label>

                        <input
                            type="number"
                            min="0"
                            value={maxPrice}
                            placeholder="Any price"
                            onChange={(event) =>
                                setMaxPrice(
                                    event.target.value
                                )
                            }
                        />
                    </div>

                    <div className="filter-panel-actions">
                        {hasFilters && (
                            <button
                                type="button"
                                onClick={
                                    clearFilters
                                }
                            >
                                <X size={16} />

                                Clear filters
                            </button>
                        )}
                    </div>
                </motion.section>
            )}

            {/* =====================================
          RESULTS INFO
      ===================================== */}

            <div className="marketplace-results-header">
                <div>
                    <span>
                        Marketplace Collection
                    </span>

                    <h2>
                        Available Gemstones
                    </h2>
                </div>

                <p>
                    {loading
                        ? "Loading..."
                        : `${filteredGems.length} ${filteredGems.length ===
                            1
                            ? "listing"
                            : "listings"
                        } found`}
                </p>
            </div>

            {/* =====================================
          ERROR
      ===================================== */}

            {error && (
                <div className="marketplace-error liquid-card">
                    <div>
                        <strong>
                            Marketplace unavailable
                        </strong>

                        <p>
                            {error}
                        </p>
                    </div>

                    <button
                        type="button"
                        onClick={
                            loadMarketplace
                        }
                    >
                        <RefreshCw size={17} />
                        Try Again
                    </button>
                </div>
            )}

            {/* =====================================
          LOADING
      ===================================== */}

            {loading && (
                <div className="marketplace-skeleton-grid">
                    {Array.from({
                        length: 6,
                    }).map((_, index) => (
                        <div
                            className="marketplace-skeleton"
                            key={index}
                        >
                            <div />
                            <span />
                            <span />
                            <span />
                        </div>
                    ))}
                </div>
            )}

            {/* =====================================
          GEM GRID
      ===================================== */}

            {!loading &&
                !error &&
                filteredGems.length > 0 && (
                    <motion.div
                        className="marketplace-gem-grid"
                        initial="hidden"
                        animate="visible"
                        variants={{
                            hidden: {},

                            visible: {
                                transition: {
                                    staggerChildren:
                                        0.07,
                                },
                            },
                        }}
                    >
                        {filteredGems.map(
                            (gem) => (
                                <motion.div
                                    key={gem.id}
                                    variants={{
                                        hidden: {
                                            opacity: 0,
                                            y: 40,
                                        },

                                        visible: {
                                            opacity: 1,
                                            y: 0,

                                            transition: {
                                                duration: 0.5,
                                            },
                                        },
                                    }}
                                >
                                    <GemCard
                                        gem={gem}
                                    />
                                </motion.div>
                            )
                        )}
                    </motion.div>
                )}

            {/* =====================================
          EMPTY
      ===================================== */}

            {!loading &&
                !error &&
                filteredGems.length === 0 && (
                    <motion.div
                        className="marketplace-empty liquid-card"
                        initial={{
                            opacity: 0,
                            scale: 0.97,
                        }}
                        animate={{
                            opacity: 1,
                            scale: 1,
                        }}
                    >
                        <div className="marketplace-empty-icon">
                            <Gem size={34} />
                        </div>

                        <h3>
                            No gemstones match your
                            search.
                        </h3>

                        <p>
                            Try adjusting your search,
                            gemstone type or price
                            range.
                        </p>

                        <button
                            onClick={
                                clearFilters
                            }
                        >
                            Clear Filters
                        </button>
                    </motion.div>
                )}
        </div>
    );
}