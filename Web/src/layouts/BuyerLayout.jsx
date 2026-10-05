import {
    useEffect,
    useRef,
    useState,
} from "react";

import {
    NavLink,
    Outlet,
    useNavigate,
} from "react-router-dom";

import {
    ChevronDown,
    LogOut,
    Package,
    ShieldCheck,
    UserRound,
} from "lucide-react";

import { useAuth }
    from "../context/AuthContext";

import {
    API_ORIGIN,
} from "../services/buyerApi";

import SiteFooter from "../components/buyer/SiteFooter";

import "../styles/buyer.css";

export default function BuyerLayout() {
    const {
        user,
        logout,
    } = useAuth();

    const navigate =
        useNavigate();

    const dropdownRef =
        useRef(null);


    const [profileImageFailed, setProfileImageFailed] =
        useState(false);

    const [
        menuOpen,
        setMenuOpen,
    ] = useState(false);

    const name =
        user?.fullName ||
        user?.name ||
        "Buyer";

    const initials =
        name
            .split(" ")
            .filter(Boolean)
            .map((part) => part[0])
            .slice(0, 2)
            .join("")
            .toUpperCase();

    const profileImage =
        user?.profileImageUrl
            ? `${API_ORIGIN}${user.profileImageUrl.startsWith("/")
                ? user.profileImageUrl
                : `/${user.profileImageUrl}`
            }`
            : null;

    useEffect(() => {
        setProfileImageFailed(false);
    }, [profileImage]);


    useEffect(() => {
        function closeDropdown(event) {
            if (
                dropdownRef.current &&
                !dropdownRef.current.contains(
                    event.target
                )
            ) {
                setMenuOpen(false);
            }
        }

        document.addEventListener(
            "mousedown",
            closeDropdown
        );

        return () => {
            document.removeEventListener(
                "mousedown",
                closeDropdown
            );
        };
    }, []);

    const goTo = (path) => {
        setMenuOpen(false);
        navigate(path);
    };

    const handleLogout = () => {
        setMenuOpen(false);

        if (
            typeof logout ===
            "function"
        ) {
            logout();
        } else {
            localStorage.removeItem(
                "token"
            );

            localStorage.removeItem(
                "authToken"
            );

            localStorage.removeItem(
                "user"
            );

            sessionStorage.removeItem(
                "token"
            );
        }

        navigate(
            "/login",
            { replace: true }
        );
    };

    return (
        <div className="buyer-shell">
            <header className="buyer-topbar">
                <NavLink
                    to="/buyer/dashboard"
                    className="buyer-brand"
                >
                    <div className="brand-badge">
                        G
                    </div>

                    <div>
                        <h1>GEMORA</h1>

                        <p>
                            Ceylon Gem Marketplace
                        </p>
                    </div>
                </NavLink>

                <nav className="buyer-nav">
                    <NavLink
                        to="/buyer/dashboard"
                    >
                        Dashboard
                    </NavLink>

                    <NavLink
                        to="/buyer/marketplace"
                    >
                        Marketplace
                    </NavLink>

                    <NavLink
                        to="/buyer/orders"
                    >
                        My Orders
                    </NavLink>

                    <NavLink
                        to="/buyer/profile"
                    >
                        Profile
                    </NavLink>
                </nav>

                <div
                    className="buyer-account"
                    ref={dropdownRef}
                >
                    <button
                        type="button"
                        className={`buyer-user-pill ${menuOpen
                            ? "menu-active"
                            : ""
                            }`}
                        onClick={() =>
                            setMenuOpen(
                                (previous) =>
                                    !previous
                            )
                        }
                    >
                        <span className="role-pill">
                            {user?.role ||
                                "Buyer"}
                        </span>

                        <div className="avatar-circle">
                            {profileImage &&
                                !profileImageFailed ? (
                                <img
                                    src={profileImage}
                                    alt={name}
                                    onError={() =>
                                        setProfileImageFailed(
                                            true
                                        )
                                    }
                                />
                            ) : (
                                initials
                            )}
                        </div>

                        <div className="buyer-user-text">
                            <strong>
                                {name}
                            </strong>

                            <p>
                                {user?.email}
                            </p>
                        </div>

                        <ChevronDown
                            className={`account-chevron ${menuOpen
                                ? "rotate"
                                : ""
                                }`}
                            size={18}
                        />
                    </button>

                    {menuOpen && (
                        <div className="account-dropdown">
                            <div className="account-dropdown-head">
                                <div className="dropdown-avatar">
                                    {profileImage &&
                                        !profileImageFailed ? (
                                        <img
                                            src={profileImage}
                                            alt={name}
                                            onError={() =>
                                                setProfileImageFailed(
                                                    true
                                                )
                                            }
                                        />
                                    ) : (
                                        initials
                                    )}
                                </div>

                                <div>
                                    <strong>
                                        {name}
                                    </strong>

                                    <span>
                                        {user?.email}
                                    </span>
                                </div>
                            </div>

                            <div className="dropdown-divider" />

                            <button
                                onClick={() =>
                                    goTo(
                                        "/buyer/profile"
                                    )
                                }
                            >
                                <UserRound
                                    size={18}
                                />

                                <div>
                                    <strong>
                                        Manage Profile
                                    </strong>

                                    <span>
                                        Personal details
                                        and account
                                    </span>
                                </div>
                            </button>

                            <button
                                onClick={() =>
                                    goTo(
                                        "/buyer/orders"
                                    )
                                }
                            >
                                <Package
                                    size={18}
                                />

                                <div>
                                    <strong>
                                        My Orders
                                    </strong>

                                    <span>
                                        Track gemstone
                                        purchases
                                    </span>
                                </div>
                            </button>

                            <button
                                onClick={() =>
                                    goTo(
                                        "/buyer/profile"
                                    )
                                }
                            >
                                <ShieldCheck
                                    size={18}
                                />

                                <div>
                                    <strong>
                                        Account Security
                                    </strong>

                                    <span>
                                        Verification and
                                        security
                                    </span>
                                </div>
                            </button>

                            <div className="dropdown-divider" />

                            <button
                                className="signout-item"
                                onClick={
                                    handleLogout
                                }
                            >
                                <LogOut size={18} />

                                <div>
                                    <strong>
                                        Sign Out
                                    </strong>

                                    <span>
                                        End this session
                                    </span>
                                </div>
                            </button>
                        </div>
                    )}
                </div>
            </header>

            <main className="buyer-main">
                <Outlet />
            </main>

            <SiteFooter />
        </div>
    );
}