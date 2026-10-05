import {
  useEffect,
  useState,
} from "react";

import {
  ArrowLeft,
  CheckCircle2,
  CreditCard,
  Gem,
  LockKeyhole,
  ReceiptText,
  ShieldCheck,
  Store,
} from "lucide-react";

import {
  useNavigate,
  useParams,
} from "react-router-dom";

import {
  getOrderById,
  payOrder,
  resolveMediaUrl,
} from "../../services/buyerApi";


export default function PaymentPage() {
  const { orderId } =
    useParams();

  const navigate =
    useNavigate();


  // =========================================================
  // STATE
  // =========================================================

  const [order, setOrder] =
    useState(null);

  const [loading, setLoading] =
    useState(true);

  const [error, setError] =
    useState("");

  const [
    processing,
    setProcessing,
  ] = useState(false);

  const [
    paymentSuccess,
    setPaymentSuccess,
  ] = useState(null);

  const [
    paymentError,
    setPaymentError,
  ] = useState("");

  const [
    imageFailed,
    setImageFailed,
  ] = useState(false);


  // =========================================================
  // LOAD ACTUAL ORDER FROM BACKEND
  // =========================================================

  useEffect(() => {
    let mounted = true;

    async function loadOrder() {
      try {
        setLoading(true);
        setError("");

        const data =
          await getOrderById(
            orderId
          );

        if (mounted) {
          setOrder(data);
        }
      } catch (err) {
        console.error(
          "Failed to load payment order:",
          err
        );

        if (mounted) {
          setError(
            err.message ||
              "Unable to load payment details."
          );
        }
      } finally {
        if (mounted) {
          setLoading(false);
        }
      }
    }

    if (orderId) {
      loadOrder();
    }

    return () => {
      mounted = false;
    };
  }, [orderId]);


  // =========================================================
  // PAYMENT
  // =========================================================

  async function handlePayment() {
    if (!order) {
      return;
    }

    try {
      setProcessing(true);
      setPaymentError("");

      const result =
        await payOrder(
          order.id,
          "Card"
        );

      /*
       * Backend response:
       *
       * PaymentResponseDto
       * {
       *   id,
       *   orderId,
       *   provider,
       *   externalReference,
       *   amount,
       *   currency,
       *   status,
       *   createdAt,
       *   order
       * }
       */

      setPaymentSuccess(
        result
      );

      if (result?.order) {
        setOrder(
          result.order
        );
      }
    } catch (err) {
      console.error(
        "Payment failed:",
        err
      );

      setPaymentError(
        err.message ||
          "Payment could not be completed."
      );
    } finally {
      setProcessing(false);
    }
  }


  // =========================================================
  // HELPERS
  // =========================================================

  function formatPrice(
    amount,
    currency = "LKR"
  ) {
    return `${currency} ${Number(
      amount || 0
    ).toLocaleString(
      "en-LK",
      {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }
    )}`;
  }


  function formatDate(value) {
    if (!value) {
      return "Not available";
    }

    return new Date(
      value
    ).toLocaleString(
      "en-LK",
      {
        day: "numeric",
        month: "short",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      }
    );
  }


  // =========================================================
  // LOADING
  // =========================================================

  if (loading) {
    return (
      <div className="buyer-payment-page">

        <div className="glass-card payment-state-card">

          <div className="payment-loading-content">

            <div className="payment-loading-icon">
              <CreditCard
                size={28}
              />
            </div>

            <div>
              <h3>
                Preparing secure checkout
              </h3>

              <p>
                Loading your order details...
              </p>
            </div>

          </div>

        </div>

      </div>
    );
  }


  // =========================================================
  // ERROR / ORDER NOT FOUND
  // =========================================================

  if (
    error ||
    !order
  ) {
    return (
      <div className="buyer-payment-page">

        <div className="glass-card payment-state-card">

          <h2>
            Payment unavailable
          </h2>

          <p>
            {error ||
              "Unable to locate this order."}
          </p>

          <button
            type="button"
            className="hero-btn primary"
            onClick={() =>
              navigate(
                "/buyer/orders"
              )
            }
          >
            Back to My Orders
          </button>

        </div>

      </div>
    );
  }


  // =========================================================
  // PAYMENT RULES
  // =========================================================

  /*
   * Buyer may pay only when:
   *
   * Confirmed
   * OR
   * AwaitingPayment
   *
   * Paid / Completed orders cannot
   * be paid again.
   */

  const canPay =
    order.status ===
      "Confirmed" ||
    order.status ===
      "AwaitingPayment";


  const alreadyPaid =
    order.status ===
      "Paid" ||
    order.status ===
      "Completed" ||
    Boolean(
      order.paidAt
    );


  const imageUrl =
    resolveMediaUrl(
      order.gemImageUrl
    );


  // =========================================================
  // UI
  // =========================================================

  return (
    <div className="buyer-payment-page">

      {/* =====================================
          BACK
      ====================================== */}

      <button
        type="button"
        className="back-inline-btn"
        onClick={() =>
          navigate(
            "/buyer/orders"
          )
        }
      >
        <ArrowLeft
          size={18}
        />

        My Orders
      </button>


      {/* =====================================
          PAYMENT SUCCESS
      ====================================== */}

      {paymentSuccess && (
        <section className="glass-card payment-success-card">

          <div className="payment-success-icon">
            <CheckCircle2
              size={38}
            />
          </div>

          <span className="section-mini-title">
            PAYMENT COMPLETED
          </span>

          <h1>
            Payment successful.
          </h1>

          <p>
            Your gemstone payment
            has been successfully
            recorded by Gemora.
          </p>

          <div className="payment-success-details">

            <div>
              <span>
                Payment Reference
              </span>

              <strong>
                {
                  paymentSuccess
                    .externalReference
                }
              </strong>
            </div>

            <div>
              <span>
                Amount Paid
              </span>

              <strong>
                {formatPrice(
                  paymentSuccess
                    .amount,
                  paymentSuccess
                    .currency
                )}
              </strong>
            </div>

            <div>
              <span>
                Payment Status
              </span>

              <strong>
                {
                  paymentSuccess
                    .status
                }
              </strong>
            </div>

            <div>
              <span>
                Paid At
              </span>

              <strong>
                {formatDate(
                  paymentSuccess
                    .createdAt
                )}
              </strong>
            </div>

          </div>


          <div className="payment-success-actions">

            <button
              type="button"
              className="hero-btn primary"
              onClick={() =>
                navigate(
                  "/buyer/orders"
                )
              }
            >
              View My Orders
            </button>

            <button
              type="button"
              className="hero-btn secondary"
              onClick={() =>
                navigate(
                  "/buyer/marketplace"
                )
              }
            >
              Continue Shopping
            </button>

          </div>

        </section>
      )}


      {/* =====================================
          MAIN PAYMENT AREA
      ====================================== */}

      {!paymentSuccess && (
        <div className="payment-page-grid">

          {/* =================================
              LEFT SIDE
          ================================== */}

          <section className="glass-card payment-main-card">

            <span className="section-mini-title">
              SECURE CHECKOUT
            </span>

            <h1>
              Complete your purchase
            </h1>

            <p className="payment-page-description">
              Review your verified gemstone
              order before completing your
              payment.
            </p>


            {/* SECURITY */}

            <div className="payment-security-banner">

              <ShieldCheck
                size={25}
              />

              <div>
                <strong>
                  Secure Gemora Transaction
                </strong>

                <p>
                  Payment is available only
                  after seller confirmation.
                  Your order information and
                  transaction history are
                  securely recorded.
                </p>
              </div>

            </div>


            {/* ORDER STATUS */}

            <div className="payment-order-state">

              <div>
                <span>
                  Current Order Status
                </span>

                <strong>
                  {order.status}
                </strong>
              </div>

              {canPay && (
                <span className="payment-ready-chip">
                  Ready for Payment
                </span>
              )}

              {alreadyPaid && (
                <span className="payment-paid-chip">
                  Payment Completed
                </span>
              )}

            </div>


            {/* PAYMENT METHOD */}

            <div className="payment-method-panel">

              <div className="payment-method-heading">

                <div className="payment-method-icon">
                  <CreditCard
                    size={23}
                  />
                </div>

                <div>
                  <h3>
                    Card Payment
                  </h3>

                  <p>
                    Secure Gemora payment
                    processing
                  </p>
                </div>

              </div>


              {/* DEMO PAYMENT NOTICE */}

              <div className="payment-demo-note">

                <LockKeyhole
                  size={18}
                />

                <div>
                  <strong>
                    Secure Payment
                  </strong>

                  <p>
                    This payment creates
                    a transaction record
                    and updates your
                    order to Paid.
                  </p>
                </div>

              </div>


              {/* PAYMENT ERROR */}

              {paymentError && (
                <div className="payment-warning">
                  {paymentError}
                </div>
              )}


              {/* NOT READY */}

              {!canPay &&
                !alreadyPaid && (
                  <div className="payment-warning">

                    This order must be
                    confirmed by the
                    seller before payment
                    can be made.

                  </div>
                )}


              {/* ALREADY PAID */}

              {alreadyPaid && (
                <div className="payment-already-completed">

                  <CheckCircle2
                    size={20}
                  />

                  <div>
                    <strong>
                      Payment already completed
                    </strong>

                    <p>
                      This order has already
                      been paid and cannot
                      be charged again.
                    </p>
                  </div>

                </div>
              )}


              {/* PAYMENT BUTTON */}

              {!alreadyPaid && (
                <button
                  type="button"
                  className="checkout-submit-btn"

                  disabled={
                    !canPay ||
                    processing
                  }

                  onClick={
                    handlePayment
                  }
                >
                  <LockKeyhole
                    size={19}
                  />

                  {processing
                    ? "Processing Payment..."
                    : canPay
                      ? `Pay ${formatPrice(
                          order.agreedPrice,
                          order.currency
                        )}`
                      : `Order ${order.status}`}
                </button>
              )}


              {alreadyPaid && (
                <button
                  type="button"
                  className="checkout-submit-btn"
                  onClick={() =>
                    navigate(
                      "/buyer/orders"
                    )
                  }
                >
                  <ReceiptText
                    size={19}
                  />

                  View Order
                </button>
              )}

            </div>

          </section>


          {/* =================================
              RIGHT ORDER SUMMARY
          ================================== */}

          <aside className="glass-card payment-order-summary">

            <span className="section-mini-title">
              ORDER SUMMARY
            </span>


            {/* REAL DATABASE GEM IMAGE */}

            <div className="payment-gem-preview">

              {imageUrl &&
              !imageFailed ? (

                <img
                  src={imageUrl}
                  alt={
                    order.gemTitle ||
                    "Gemstone"
                  }
                  onError={() =>
                    setImageFailed(
                      true
                    )
                  }
                />

              ) : (

                <div className="payment-image-fallback">

                  <Gem
                    size={44}
                  />

                  <span>
                    Image unavailable
                  </span>

                </div>

              )}

            </div>


            <span className="payment-product-label">
              VERIFIED GEMSTONE
            </span>

            <h2>
              {order.gemTitle ||
                "Gemstone Purchase"}
            </h2>


            {/* ORDER */}

            <div className="payment-summary-row">

              <span>
                Order Number
              </span>

              <strong>
                {order.orderNumber ||
                  order.id}
              </strong>

            </div>


            {/* SELLER */}

            <div className="payment-summary-row">

              <span>
                Seller
              </span>

              <strong className="payment-seller-value">
                <Store
                  size={15}
                />

                {order.sellerName ||
                  "Verified Seller"}
              </strong>

            </div>


            {/* STATUS */}

            <div className="payment-summary-row">

              <span>
                Order Status
              </span>

              <strong>
                {order.status}
              </strong>

            </div>


            {/* SHIPPING */}

            <div className="payment-summary-row">

              <span>
                Delivery Region
              </span>

              <strong>
                {order.shippingRegion ||
                  order.shippingCountryCode ||
                  "Not provided"}
              </strong>

            </div>


            {/* ORDERED DATE */}

            <div className="payment-summary-row">

              <span>
                Ordered
              </span>

              <strong>
                {formatDate(
                  order.createdAt
                )}
              </strong>

            </div>


            {/* TOTAL */}

            <div className="payment-total-row">

              <span>
                Total
              </span>

              <strong>
                {formatPrice(
                  order.agreedPrice,
                  order.currency
                )}
              </strong>

            </div>


            {/* TRUST */}

            <div className="payment-summary-trust">

              <ShieldCheck
                size={18}
              />

              <span>
                Protected Gemora
                marketplace transaction
              </span>

            </div>

          </aside>

        </div>
      )}

    </div>
  );
}