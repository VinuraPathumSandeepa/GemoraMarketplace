import { PaymentDeadline } from "../../components/OrderInbox";
import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { AnimatePresence, motion } from "framer-motion";
import { BuyerGuide, GemImage, PageHeading } from "../../components/buyer/BuyerUI";

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
  Store,
  Truck,
  XCircle,
} from "lucide-react";

import {
  completeOrder,
  getMyOrders,
} from "../../services/buyerApi";


export default function MyOrdersPage() {
  const navigate = useNavigate();

  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [actionError, setActionError] = useState("");
  const [updatingOrderId, setUpdatingOrderId] = useState(null);

  const [search, setSearch] = useState("");
  const [activeStatus, setActiveStatus] = useState("All");
  const [expandedOrder, setExpandedOrder] = useState(null);


  useEffect(() => {
    loadOrders();
    const refresh = () => loadOrders(true);
    const timer = setInterval(refresh, 15000);
    window.addEventListener("gemora-orders-changed", refresh);
    return () => { clearInterval(timer); window.removeEventListener("gemora-orders-changed", refresh); };
  }, []);


  async function loadOrders(quiet = false) {
    try {
      if (!quiet) setLoading(true);
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


  async function handleCompleteOrder(order) {
    if (!order?.id) return;

    try {
      setActionError("");
      setUpdatingOrderId(order.id);

      await completeOrder(
        order.id,
        "Buyer confirmed successful delivery."
      );

      await loadOrders();
    } catch (err) {
      console.error("Failed to complete order:", err);

      setActionError(
        err.message ||
          "Unable to confirm delivery for this order."
      );
    } finally {
      setUpdatingOrderId(null);
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

      <PageHeading eyebrow="YOUR GEMSTONE JOURNEY" title="Every purchase. Every milestone." description="Follow your orders from that first discovery to the moment your gemstone arrives." />
      <div className="gm-order-summary">{[["Total orders", orders.length, "Your Gemora purchases"], ["Awaiting payment", orders.filter(o => ["Confirmed", "AwaitingPayment"].includes(o.status)).length, "Confirmed by the seller"], ["Completed", orders.filter(o => o.status === "Completed").length, "A journey fulfilled"]].map(([label, count, caption]) => <article className="gm-glass" key={label}><span>{label}</span><strong>{loading ? "—" : count}</strong><small>{caption}</small></article>)}</div>
      {/* ================= CONTROLS ================= */}

      <section className="orders-control-bar">

        <div className="orders-search-field">
          <Search size={18} />

          <input
            value={search}
            onChange={(e) =>
              setSearch(e.target.value)
            }
            aria-label="Search your orders"
            placeholder="Search order, gemstone or seller..."
          />
        </div>

        <div className="orders-filter-tabs">

          {statuses.map((status) => (
            <button
              key={status}
              aria-pressed={activeStatus === status}
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

      {actionError && (
        <section className="buyer-orders-error">
          <div>
            <strong>Unable to update order</strong>
            <p>{actionError}</p>
          </div>
        </section>
      )}


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
              (order) => (

                <PremiumOrderCard
                  key={order.id}
                  order={order}

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

                  onComplete={() =>
                    handleCompleteOrder(order)
                  }

                  completing={
                    updatingOrderId === order.id
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

      <BuyerGuide />
    </div>
  );
}



function PremiumOrderCard({
  order,
  expanded,
  toggleExpanded,
  onPay,
  onComplete,
  completing,
  formatPrice,
  formatDate,
}) {

  const status =
    order.status || "Pending";

  const fulfillmentStatus =
    order.fulfillmentStatus || "Pending";


  return (
    <article className="premium-order-card">

      {/* IMAGE */}

      <div className="premium-order-image">

        <GemImage src={order.gemImageUrl} alt={order.gemTitle || "Gemstone purchase"} />

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
          fulfillmentStatus={fulfillmentStatus}
        />


        {/* CURRENT STATE MESSAGE */}

        {["Confirmed", "AwaitingPayment"].includes(status) && <PaymentDeadline dueAt={order.paymentDueAt} />}
        {["Rejected", "Cancelled"].includes(status) && order.statusHistory?.at(-1)?.reason && <p className="oi-error">{order.statusHistory.at(-1).reason}</p>}
        <CurrentOrderMessage
          status={status}
          fulfillmentStatus={fulfillmentStatus}
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

            {["Confirmed", "AwaitingPayment"].includes(status) && (
              <button
                type="button"
                className="premium-pay-btn"
                onClick={onPay}
              >
                <CreditCard size={18} />

                Pay Now
              </button>
            )}

            {status === "Paid" &&
              fulfillmentStatus === "Delivered" && (
                <button
                  type="button"
                  className="premium-pay-btn"
                  onClick={onComplete}
                  disabled={completing}
                >
                  <PackageCheck size={18} />
                  {completing
                    ? "Confirming..."
                    : "Confirm Delivery"}
                </button>
              )}


            <button
              type="button"
              className="primary-order-action"
              onClick={toggleExpanded}
              aria-expanded={expanded}
              aria-controls={`order-details-${order.id}`}
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
              id={`order-details-${order.id}`}

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
                  label="Fulfillment"
                  value={humanizeStatus(fulfillmentStatus)}
                />

                <ExpandedInfo
                  label="Courier"
                  value={
                    order.shipment?.courierName ||
                    "Not assigned"
                  }
                />

                <ExpandedInfo
                  label="Tracking Number"
                  value={
                    order.shipment?.trackingNumber ||
                    "Not available"
                  }
                />

                <ExpandedInfo
                  label="Expected Delivery"
                  value={
                    order.shipment?.expectedDeliveryDate
                      ? formatDate(order.shipment.expectedDeliveryDate)
                      : "Not available"
                  }
                />

                <ExpandedInfo
                  label="Last Updated"
                  value={formatDate(
                    order.updatedAt
                  )}
                />

              </div>


              {order.shipment?.trackingUrl && (
                <div className="premium-history">
                  <h4>Shipment Tracking</h4>
                  <a
                    href={order.shipment.trackingUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="secondary-order-action"
                  >
                    <Truck size={16} />
                    Open courier tracking
                    <ArrowRight size={16} />
                  </a>
                </div>
              )}

              {Array.isArray(order.fulfillmentHistory) &&
                order.fulfillmentHistory.length > 0 && (
                  <div className="premium-history">
                    <h4>Delivery History</h4>

                    {order.fulfillmentHistory.map(
                      (history, historyIndex) => (
                        <div
                          className="premium-history-row"
                          key={`${order.id}-fulfillment-${historyIndex}`}
                        >
                          <span className="premium-history-dot" />

                          <div>
                            <strong>
                              {humanizeStatus(
                                history.newStatus || "Updated"
                              )}
                            </strong>

                            {history.note && (
                              <p>{history.note}</p>
                            )}

                            <small>
                              {formatDate(history.createdAt)}
                              {history.changedByName
                                ? ` · ${history.changedByName}`
                                : ""}
                            </small>
                          </div>
                        </div>
                      )
                    )}
                  </div>
                )}


              {/* REAL DATABASE ORDER STATUS HISTORY */}

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

    </article>
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
  fulfillmentStatus,
}) {

  if (status === "Paid") {
    const fulfillmentConfig = {
      Pending: {
        icon: <Clock3 size={18} />,
        title: "Payment successful",
        text: "Your payment is secure. The seller will prepare the gemstone next.",
      },
      Preparing: {
        icon: <Gem size={18} />,
        title: "Gemstone is being prepared",
        text: "The seller is securely preparing your gemstone for dispatch.",
      },
      ReadyForDispatch: {
        icon: <PackageCheck size={18} />,
        title: "Ready for dispatch",
        text: "Your gemstone package is ready for courier handover.",
      },
      HandedOverToCourier: {
        icon: <Truck size={18} />,
        title: "Handed over to courier",
        text: "The delivery address is now locked and the courier has your package.",
      },
      InTransit: {
        icon: <Truck size={18} />,
        title: "Shipment in transit",
        text: "Your gemstone is moving through the courier network.",
      },
      OutForDelivery: {
        icon: <Truck size={18} />,
        title: "Out for delivery",
        text: "Your gemstone is on its final delivery route.",
      },
      Delivered: {
        icon: <PackageCheck size={18} />,
        title: "Delivered — confirmation required",
        text: "Confirm delivery after you have safely received and checked the package.",
      },
    };

    const fulfillmentItem =
      fulfillmentConfig[fulfillmentStatus || "Pending"];

    if (fulfillmentItem) {
      return (
        <div className="order-current-message paid">
          <div className="order-current-icon">
            {fulfillmentItem.icon}
          </div>
          <div>
            <strong>{fulfillmentItem.title}</strong>
            <p>{fulfillmentItem.text}</p>
          </div>
        </div>
      );
    }
  }

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

    AwaitingPayment: {
      icon:
        <CreditCard size={18} />,

      title:
        "Payment required",

      text:
        "Complete payment to continue with secure gemstone delivery.",
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

    Rejected: { icon: <XCircle size={18} />, title: "Order rejected", text: "See the seller decision above. You can continue exploring the marketplace." },
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




function humanizeStatus(value) {
  if (!value) return "Not available";

  return value
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/_/g, " ")
    .trim();
}


function OrderProgress({
  status,
  fulfillmentStatus,
}) {

  const failureStatuses = [
    "Rejected",
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
    AwaitingPayment: 2,
    Paid: 4,
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
    <div className="ecommerce-order-progress" role="list" aria-label="Order progress">

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
            ["Confirmed", "AwaitingPayment"]
              .includes(status)
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
            stage.label === "Delivery" &&
            status === "Paid"
          ) {
            subLabel =
              humanizeStatus(
                fulfillmentStatus || "Pending"
              );
          }


          return (
            <div
              key={stage.label}
              role="listitem"
              aria-current={active ? "step" : undefined}
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
