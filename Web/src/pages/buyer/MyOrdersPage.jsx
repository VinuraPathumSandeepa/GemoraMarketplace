import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { AnimatePresence, motion } from "framer-motion";

import {
  ArrowRight,
  CalendarDays,
  Check,
  ChevronDown,
  Clock3,
  CreditCard,
  Gem,
  MapPin,
  PackageCheck,
  RefreshCw,
  Search,
  ShieldCheck,
  Store,
  Truck,
  XCircle,
} from "lucide-react";

import {
  getMyOrders,
  resolveMediaUrl,
} from "../../services/buyerApi";


export default function MyOrdersPage() {
  const navigate = useNavigate();

  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [search, setSearch] = useState("");
  const [activeStatus, setActiveStatus] = useState("All");
  const [expandedOrder, setExpandedOrder] = useState(null);


  useEffect(() => {
    loadOrders();
  }, []);


  async function loadOrders() {
    try {
      setLoading(true);
      setError("");

      const response = await getMyOrders();

      const realOrders = Array.isArray(response)
        ? response
        : response?.items || [];

      setOrders(realOrders);
    } catch (err) {
      console.error("Failed to load buyer orders:", err);

      setError(
        err.message ||
          "Unable to load your orders."
      );
    } finally {
      setLoading(false);
    }
  }


  const statuses = useMemo(() => {
    return [
      "All",
      ...new Set(
        orders
          .map((order) => order.status)
          .filter(Boolean)
      ),
    ];
  }, [orders]);


  const filteredOrders = useMemo(() => {
    let result = [...orders];

    if (activeStatus !== "All") {
      result = result.filter(
        (order) =>
          order.status === activeStatus
      );
    }

    const query =
      search.trim().toLowerCase();

    if (query) {
      result = result.filter(
        (order) =>
          order.orderNumber
            ?.toLowerCase()
            .includes(query) ||

          order.gemTitle
            ?.toLowerCase()
            .includes(query) ||

          order.sellerName
            ?.toLowerCase()
            .includes(query)
      );
    }

    return result;
  }, [
    orders,
    search,
    activeStatus,
  ]);


  function formatPrice(
    amount,
    currency = "LKR"
  ) {
    return `${currency} ${Number(
      amount || 0
    ).toLocaleString("en-LK", {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })}`;
  }


  function formatDate(value) {
    if (!value) {
      return "Not available";
    }

    return new Date(value)
      .toLocaleDateString(
        "en-LK",
        {
          day: "numeric",
          month: "short",
          year: "numeric",
        }
      );
  }


  return (
    <div className="buyer-orders-experience">

      {/* ================= HERO ================= */}

      <motion.section
        className="orders-luxury-hero"
        initial={{
          opacity: 0,
          y: 25,
        }}
        animate={{
          opacity: 1,
          y: 0,
        }}
      >
        <div className="orders-hero-copy">

          <span className="orders-kicker">
            <PackageCheck size={15} />
            Buyer Transactions
          </span>

          <h1>
            Your gemstone
            <span> journey.</span>
          </h1>

          <p>
            Follow every purchase from order
            placement and seller confirmation
            through secure payment and delivery.
          </p>

          {!loading && (
            <div className="orders-hero-meta">
              <span>
                <Gem size={15} />
                {orders.length} orders
              </span>

              <span>
                <ShieldCheck size={15} />
                Secure marketplace transactions
              </span>
            </div>
          )}

        </div>

        <div className="orders-total-panel">
          <span>Total Orders</span>

          <strong>
            {orders.length}
          </strong>

          <small>
            Gemora purchases
          </small>
        </div>

      </motion.section>


      {/* ================= CONTROLS ================= */}

      <section className="orders-control-bar">

        <div className="orders-search-field">
          <Search size={18} />

          <input
            value={search}
            onChange={(e) =>
              setSearch(e.target.value)
            }
            placeholder="Search order, gemstone or seller..."
          />
        </div>

        <div className="orders-filter-tabs">

          {statuses.map((status) => (
            <button
              key={status}
              type="button"
              className={
                activeStatus === status
                  ? "active"
                  : ""
              }
              onClick={() =>
                setActiveStatus(status)
              }
            >
              {status}
            </button>
          ))}

        </div>

      </section>


      {/* ================= ERROR ================= */}

      {error && (
        <section className="buyer-orders-error">

          <div>
            <strong>
              Unable to load orders
            </strong>

            <p>{error}</p>
          </div>

          <button
            type="button"
            onClick={loadOrders}
          >
            <RefreshCw size={16} />
            Retry
          </button>

        </section>
      )}


      {/* ================= LOADING ================= */}

      {loading && (
        <div className="orders-premium-list">

          {[1, 2].map((item) => (
            <div
              key={item}
              className="premium-order-skeleton"
            />
          ))}

        </div>
      )}


      {/* ================= ORDERS ================= */}

      {!loading &&
        !error &&
        filteredOrders.length > 0 && (

          <div className="orders-premium-list">

            {filteredOrders.map(
              (order, index) => (

                <PremiumOrderCard
                  key={order.id}
                  order={order}
                  index={index}

                  expanded={
                    expandedOrder ===
                    order.id
                  }

                  toggleExpanded={() =>
                    setExpandedOrder(
                      expandedOrder ===
                        order.id
                        ? null
                        : order.id
                    )
                  }

                  onPay={() =>
                    navigate(
                      `/buyer/orders/${order.id}/payment`
                    )
                  }

                  formatPrice={
                    formatPrice
                  }

                  formatDate={
                    formatDate
                  }
                />

              )
            )}

          </div>
        )}


      {/* ================= EMPTY ================= */}

      {!loading &&
        !error &&
        filteredOrders.length === 0 && (

          <section className="orders-premium-empty">

            <Gem size={40} />

            <h2>
              {orders.length === 0
                ? "No gemstone purchases yet."
                : "No matching orders found."}
            </h2>

            <p>
              {orders.length === 0
                ? "Explore verified gemstone listings and begin your Gemora collection."
                : "Try another search or filter."}
            </p>

            {orders.length === 0 && (
              <Link
                to="/buyer/marketplace"
                className="orders-marketplace-link"
              >
                Explore Marketplace
                <ArrowRight size={18} />
              </Link>
            )}

          </section>
        )}

    </div>
  );
}



function PremiumOrderCard({
  order,
  index,
  expanded,
  toggleExpanded,
  onPay,
  formatPrice,
  formatDate,
}) {

  const imageUrl =
    resolveMediaUrl(
      order.gemImageUrl
    );

  const [imageFailed, setImageFailed] =
    useState(false);

  const status =
    order.status || "Pending";


  return (
    <motion.article
      className="premium-order-card"

      initial={{
        opacity: 0,
        y: 28,
      }}

      whileInView={{
        opacity: 1,
        y: 0,
      }}

      viewport={{
        once: true,
        amount: 0.1,
      }}

      transition={{
        duration: 0.42,
        delay:
          Math.min(
            index * 0.04,
            0.16
          ),
      }}
    >

      {/* IMAGE */}

      <div className="premium-order-image">

        {imageUrl &&
        !imageFailed ? (

          <img
            src={imageUrl}
            alt={
              order.gemTitle ||
              "Gemstone"
            }
            loading="lazy"
            onError={() =>
              setImageFailed(true)
            }
          />

        ) : (

          <div className="premium-order-image-fallback">
            <Gem size={44} />

            <span>
              Image unavailable
            </span>
          </div>

        )}

        <div className="order-image-shade" />

        <div className="order-image-id">
          #
          {order.orderNumber ||
            "Gemora Order"}
        </div>

      </div>


      {/* BODY */}

      <div className="premium-order-body">

        <div className="premium-order-heading">

          <div>
            <span className="order-category-label">
              Verified Gemstone
            </span>

            <h2>
              {order.gemTitle ||
                "Gemstone Purchase"}
            </h2>
          </div>

          <OrderStatus
            status={status}
          />

        </div>


        {/* BASIC INFO */}

        <div className="premium-order-info">

          <InfoItem
            icon={<Store size={18} />}
            label="Seller"
            value={
              order.sellerName ||
              "Not available"
            }
          />

          <InfoItem
            icon={
              <CalendarDays
                size={18}
              />
            }
            label="Ordered"
            value={formatDate(
              order.createdAt
            )}
          />

          <InfoItem
            icon={<MapPin size={18} />}
            label="Destination"
            value={
              order.shippingRegion ||
              order.shippingCountryCode ||
              "Not provided"
            }
          />

        </div>


        {/* =====================================
            PROFESSIONAL ECOMMERCE TIMELINE
        ====================================== */}

        <OrderProgress
          status={status}
        />


        {/* CURRENT STATE MESSAGE */}

        <CurrentOrderMessage
          status={status}
        />


        {/* FOOTER */}

        <div className="premium-order-footer">

          <div className="premium-order-price">

            <span>
              Purchase Value
            </span>

            <strong>
              {formatPrice(
                order.agreedPrice,
                order.currency
              )}
            </strong>

          </div>


          <div className="premium-order-actions">

            {order.gemListingId && (
              <Link
                to={`/buyer/marketplace/${order.gemListingId}`}
                className="secondary-order-action"
              >
                View Gem
              </Link>
            )}


            {/* PAYMENT BUTTON ONLY WHEN CONFIRMED */}

            {status === "Confirmed" && (
              <button
                type="button"
                className="premium-pay-btn"
                onClick={onPay}
              >
                <CreditCard size={18} />

                Pay Now
              </button>
            )}


            <button
              type="button"
              className="primary-order-action"
              onClick={toggleExpanded}
            >
              Order Details

              <ChevronDown
                size={17}
                className={
                  expanded
                    ? "rotate-chevron"
                    : ""
                }
              />
            </button>

          </div>

        </div>


        {/* EXPANDED DETAILS */}

        <AnimatePresence>

          {expanded && (

            <motion.div
              className="premium-order-expanded"

              initial={{
                opacity: 0,
                height: 0,
              }}

              animate={{
                opacity: 1,
                height: "auto",
              }}

              exit={{
                opacity: 0,
                height: 0,
              }}
            >

              <div className="expanded-order-grid">

                <ExpandedInfo
                  label="Shipping Address"
                  value={
                    order.shippingAddress ||
                    "Not provided"
                  }
                />

                <ExpandedInfo
                  label="Region"
                  value={
                    order.shippingRegion ||
                    "Not provided"
                  }
                />

                <ExpandedInfo
                  label="Country"
                  value={
                    order.shippingCountryCode ||
                    "Not provided"
                  }
                />

                <ExpandedInfo
                  label="Payment"
                  value={
                    order.paidAt
                      ? "Paid"
                      : "Not Paid"
                  }
                />

                <ExpandedInfo
                  label="Order Status"
                  value={status}
                />

                <ExpandedInfo
                  label="Last Updated"
                  value={formatDate(
                    order.updatedAt
                  )}
                />

              </div>


              {/* REAL DATABASE HISTORY */}

              {Array.isArray(
                order.statusHistory
              ) &&
                order.statusHistory
                  .length > 0 && (

                  <div className="premium-history">

                    <h4>
                      Transaction History
                    </h4>

                    {order.statusHistory.map(
                      (
                        history,
                        historyIndex
                      ) => (

                        <div
                          className="premium-history-row"
                          key={`${order.id}-${historyIndex}`}
                        >

                          <span className="premium-history-dot" />

                          <div>

                            <strong>
                              {history.newStatus ||
                                "Updated"}
                            </strong>

                            {history.reason && (
                              <p>
                                {
                                  history.reason
                                }
                              </p>
                            )}

                            <small>
                              {formatDate(
                                history.createdAt
                              )}
                            </small>

                          </div>

                        </div>

                      )
                    )}

                  </div>
                )}

            </motion.div>

          )}

        </AnimatePresence>

      </div>

    </motion.article>
  );
}



function InfoItem({
  icon,
  label,
  value,
}) {
  return (
    <div>
      {icon}

      <section>
        <span>{label}</span>
        <strong>{value}</strong>
      </section>
    </div>
  );
}



function ExpandedInfo({
  label,
  value,
}) {
  return (
    <div>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}



function OrderStatus({
  status,
}) {
  const className =
    status
      .toLowerCase()
      .replace(/\s+/g, "-");

  return (
    <span
      className={`premium-status ${className}`}
    >
      {status}
    </span>
  );
}



function CurrentOrderMessage({
  status,
}) {

  const config = {

    Pending: {
      icon:
        <Clock3 size={18} />,

      title:
        "Waiting for seller confirmation",

      text:
        "Your purchase request was submitted successfully.",
    },

    Confirmed: {
      icon:
        <CreditCard size={18} />,

      title:
        "Ready for payment",

      text:
        "The seller confirmed your gemstone. Complete payment to continue.",
    },

    Paid: {
      icon:
        <Truck size={18} />,

      title:
        "Payment successful",

      text:
        "Your payment has been recorded. Delivery preparation is pending.",
    },

    Completed: {
      icon:
        <PackageCheck size={18} />,

      title:
        "Order completed",

      text:
        "Your gemstone transaction has been successfully completed.",
    },

    Cancelled: {
      icon:
        <XCircle size={18} />,

      title:
        "Order cancelled",

      text:
        "This transaction has been cancelled.",
    },

    Refunded: {
      icon:
        <RefreshCw size={18} />,

      title:
        "Payment refunded",

      text:
        "The transaction amount has been marked as refunded.",
    },

    Failed: {
      icon:
        <XCircle size={18} />,

      title:
        "Transaction failed",

      text:
        "This transaction could not be completed.",
    },
  };


  const item =
    config[status];

  if (!item) {
    return null;
  }


  return (
    <div
      className={`order-current-message ${status.toLowerCase()}`}
    >

      <div className="order-current-icon">
        {item.icon}
      </div>

      <div>
        <strong>
          {item.title}
        </strong>

        <p>
          {item.text}
        </p>
      </div>

    </div>
  );
}



function OrderProgress({
  status,
}) {

  const failureStatuses = [
    "Cancelled",
    "Refunded",
    "Failed",
  ];


  if (
    failureStatuses.includes(status)
  ) {
    return (
      <div className="order-terminal-state">
        <XCircle size={20} />

        Transaction {status}
      </div>
    );
  }


  /*
    Backend → Buyer timeline mapping:

    Pending:
      Order Placed complete
      Seller Confirmation current

    Confirmed:
      Order Placed complete
      Seller Confirmed complete
      Payment current

    Paid:
      Payment complete
      Delivery current

    Completed:
      Everything complete
  */

  const stageMap = {
    Pending: 1,
    Confirmed: 2,
    Paid: 3,
    Completed: 5,
  };


  const current =
    stageMap[status] ?? 1;


  const stages = [
    {
      label:
        "Order Placed",
      icon:
        <Check size={15} />,
    },

    {
      label:
        "Seller Confirmed",
      icon:
        <Store size={15} />,
    },

    {
      label:
        "Payment",
      icon:
        <CreditCard size={15} />,
    },

    {
      label:
        "Delivery",
      icon:
        <Truck size={15} />,
    },

    {
      label:
        "Completed",
      icon:
        <PackageCheck size={15} />,
    },
  ];


  return (
    <div className="ecommerce-order-progress">

      <div className="order-progress-line" />

      <div
        className="order-progress-active-line"
        style={{
          width:
            `${
              ((current - 1) /
                (stages.length - 1)) *
              100
            }%`,
        }}
      />

      {stages.map(
        (stage, index) => {

          const stageNumber =
            index + 1;

          const completed =
            stageNumber <=
            current;

          const active =
            stageNumber ===
              current &&
            status !==
              "Completed";


          let subLabel = "";

          if (
            stage.label ===
              "Payment" &&
            status ===
              "Confirmed"
          ) {
            subLabel =
              "Pending";
          }

          if (
            stage.label ===
              "Payment" &&
            ["Paid", "Completed"]
              .includes(status)
          ) {
            subLabel =
              "Successful";
          }

          if (
            stage.label ===
              "Delivery" &&
            status ===
              "Paid"
          ) {
            subLabel =
              "Pending";
          }


          return (
            <div
              key={stage.label}
              className={
                `order-progress-stage
                ${
                  completed
                    ? "completed"
                    : ""
                }
                ${
                  active
                    ? "active"
                    : ""
                }`
              }
            >

              <div className="order-progress-node">

                {completed &&
                !active ? (
                  <Check
                    size={15}
                  />
                ) : (
                  stage.icon
                )}

              </div>

              <strong>
                {stage.label}
              </strong>

              {subLabel && (
                <small>
                  {subLabel}
                </small>
              )}

            </div>
          );
        }
      )}

    </div>
  );
}