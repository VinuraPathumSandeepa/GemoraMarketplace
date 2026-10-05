import {
  useEffect,
  useState,
} from "react";

import {
  ArrowLeft,
  CheckCircle2,
  CreditCard,
  FileText,
  Gem,
  Landmark,
  LockKeyhole,
  MapPin,
  Pencil,
  Phone,
  ReceiptText,
  ShieldCheck,
  Store,
  UserRound,
  X,
} from "lucide-react";

import {
  useNavigate,
  useParams,
} from "react-router-dom";

import {
  getOrderById,
  payOrder,
  resolveMediaUrl,
  updateOrderDeliveryDetails,
} from "../../services/buyerApi";


const EMPTY_DELIVERY = {
  recipientName: "",
  recipientPhone: "",
  alternatePhone: "",

  addressLine1: "",
  addressLine2: "",

  city: "",
  district: "",
  region: "",

  postalCode: "",
  countryCode: "LK",

  nearestLandmark: "",
  deliveryInstructions: "",
};


export default function PaymentPage() {
  const { orderId } =
    useParams();

  const navigate =
    useNavigate();


  // =========================================================
  // ORDER / PAYMENT STATE
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
  // DELIVERY STATE
  // =========================================================

  const [
    editingDelivery,
    setEditingDelivery,
  ] = useState(false);

  const [
    savingDelivery,
    setSavingDelivery,
  ] = useState(false);

  const [
    deliveryError,
    setDeliveryError,
  ] = useState("");

  const [
    deliveryForm,
    setDeliveryForm,
  ] = useState(
    EMPTY_DELIVERY
  );


  // =========================================================
  // LOAD ORDER
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

        if (!mounted) {
          return;
        }

        setOrder(data);

        setDeliveryForm(
          buildDeliveryForm(
            data
          )
        );
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
  // DELIVERY HANDLERS
  // =========================================================

  function updateDeliveryField(
    event
  ) {
    const {
      name,
      value,
    } = event.target;

    setDeliveryForm(
      (current) => ({
        ...current,

        [name]:
          value,
      })
    );
  }


  function beginDeliveryEdit() {
    setDeliveryError("");

    setDeliveryForm(
      buildDeliveryForm(
        order
      )
    );

    setEditingDelivery(true);
  }


  function cancelDeliveryEdit() {
    setDeliveryError("");

    setDeliveryForm(
      buildDeliveryForm(
        order
      )
    );

    setEditingDelivery(false);
  }


  async function
    handleSaveDelivery() {
    const validationMessage =
      validateDeliveryForm(
        deliveryForm
      );

    if (validationMessage) {
      setDeliveryError(
        validationMessage
      );

      return;
    }

    try {
      setSavingDelivery(true);
      setDeliveryError("");

      const updatedOrder =
        await updateOrderDeliveryDetails(
          order.id,
          {
            recipientName:
              deliveryForm
                .recipientName
                .trim(),

            recipientPhone:
              deliveryForm
                .recipientPhone
                .trim(),

            alternatePhone:
              deliveryForm
                .alternatePhone
                .trim(),

            addressLine1:
              deliveryForm
                .addressLine1
                .trim(),

            addressLine2:
              deliveryForm
                .addressLine2
                .trim(),

            city:
              deliveryForm
                .city
                .trim(),

            district:
              deliveryForm
                .district
                .trim(),

            region:
              deliveryForm
                .region
                .trim(),

            postalCode:
              deliveryForm
                .postalCode
                .trim(),

            countryCode:
              deliveryForm
                .countryCode
                .trim()
                .toUpperCase(),

            nearestLandmark:
              deliveryForm
                .nearestLandmark
                .trim(),

            deliveryInstructions:
              deliveryForm
                .deliveryInstructions
                .trim(),
          }
        );

      setOrder(
        updatedOrder
      );

      setDeliveryForm(
        buildDeliveryForm(
          updatedOrder
        )
      );

      setEditingDelivery(false);
    } catch (err) {
      console.error(
        "Unable to update delivery details:",
        err
      );

      setDeliveryError(
        err.message ||
        "Unable to update delivery details."
      );
    } finally {
      setSavingDelivery(false);
    }
  }


  // =========================================================
  // PAYMENT
  // =========================================================

  async function handlePayment() {
    if (!order) {
      return;
    }

    if (
      !hasCompleteDeliveryDetails(
        order.deliveryDetails
      )
    ) {
      setPaymentError(
        "Complete your delivery details before making payment."
      );

      setEditingDelivery(true);

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

      setPaymentSuccess(
        result
      );

      if (result?.order) {
        setOrder(
          result.order
        );

        setDeliveryForm(
          buildDeliveryForm(
            result.order
          )
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

            <CreditCard
              size={28}
            />

            <div>
              <h3>
                Preparing secure checkout
              </h3>

              <p>
                Loading order and delivery information...
              </p>
            </div>

          </div>

        </div>

      </div>
    );
  }


  // =========================================================
  // ERROR
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
  // DERIVED STATE
  // =========================================================

  const delivery =
    order.deliveryDetails;

  const deliveryLocked =
    Boolean(
      delivery?.isLocked
    ) ||
    [
      "HandedOverToCourier",
      "InTransit",
      "OutForDelivery",
      "Delivered",
      "DeliveryFailed",
      "Returned",
    ].includes(
      order.fulfillmentStatus
    );


  const canEditDelivery =
    !deliveryLocked &&
    ![
      "Cancelled",
      "Refunded",
      "Failed",
      "Completed",
    ].includes(
      order.status
    );


  const deliveryComplete =
    hasCompleteDeliveryDetails(
      delivery
    );


  const canPay =
    (
      order.status ===
      "Confirmed" ||

      order.status ===
      "AwaitingPayment"
    ) &&
    deliveryComplete;


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


      {/* =====================================================
          PAYMENT SUCCESS
      ====================================================== */}

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
            Your payment was successfully
            recorded. Your gemstone will
            now move into fulfilment and
            delivery preparation.
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
                setPaymentSuccess(
                  null
                )
              }
            >
              Review Delivery Details
            </button>

          </div>


          <p className="payment-page-description">
            You can still change the
            delivery information until
            the gemstone is handed over
            to the courier.
          </p>

        </section>
      )}


      {/* =====================================================
          MAIN PAGE
      ====================================================== */}

      {!paymentSuccess && (

        <div className="payment-page-grid">


          {/* =================================================
              LEFT
          ================================================== */}

          <section className="glass-card payment-main-card">

            <span className="section-mini-title">
              SECURE CHECKOUT
            </span>

            <h1>
              Complete your purchase
            </h1>

            <p className="payment-page-description">
              Review your verified gemstone,
              delivery recipient and delivery
              address before proceeding.
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
                  after seller confirmation
                  and completion of required
                  delivery information.
                </p>
              </div>

            </div>


            {/* ORDER STATUS */}

            <div className="payment-order-state">

              <div>
                <span>
                  Order Status
                </span>

                <strong>
                  {order.status}
                </strong>
              </div>

              <div>
                <span>
                  Fulfilment
                </span>

                <strong>
                  {order.fulfillmentStatus ||
                    "Pending"}
                </strong>
              </div>

            </div>


            {/* =================================================
                DELIVERY INFORMATION
            ================================================== */}

            <div className="payment-delivery-card">

              <div className="payment-delivery-header">

                <div>
                  <span className="section-mini-title">
                    DELIVERY INFORMATION
                  </span>

                  <h3>
                    <MapPin size={19} />
                    Recipient & Delivery Address
                  </h3>
                </div>


                {canEditDelivery &&
                  !editingDelivery && (

                    <button
                      type="button"
                      className="delivery-edit-btn"
                      onClick={
                        beginDeliveryEdit
                      }
                    >
                      <Pencil
                        size={15}
                      />

                      {delivery
                        ? "Edit Delivery Details"
                        : "Add Delivery Details"}
                    </button>
                  )}

              </div>


              {/* ===============================================
                  DELIVERY DISPLAY
              ================================================ */}

              {!editingDelivery &&
                delivery && (

                  <div className="delivery-address-display">

                    <div className="delivery-detail-row">
                      <UserRound
                        size={17}
                      />

                      <div>
                        <span>
                          Recipient
                        </span>

                        <strong>
                          {delivery.recipientName}
                        </strong>
                      </div>
                    </div>


                    <div className="delivery-detail-row">
                      <Phone
                        size={17}
                      />

                      <div>
                        <span>
                          Mobile
                        </span>

                        <strong>
                          {delivery.recipientPhone}
                        </strong>

                        {delivery.alternatePhone && (
                          <small>
                            Alternate:{" "}
                            {delivery.alternatePhone}
                          </small>
                        )}
                      </div>
                    </div>


                    <div className="delivery-detail-row">
                      <MapPin
                        size={17}
                      />

                      <div>
                        <span>
                          Delivery Address
                        </span>

                        <strong>
                          {delivery.addressLine1}
                        </strong>

                        {delivery.addressLine2 && (
                          <p>
                            {delivery.addressLine2}
                          </p>
                        )}

                        <p>
                          {delivery.city},{" "}
                          {delivery.district}
                        </p>

                        <p>
                          {delivery.region},{" "}
                          {delivery.postalCode}
                        </p>

                        <p>
                          {delivery.countryCode}
                        </p>
                      </div>
                    </div>


                    {delivery.nearestLandmark && (
                      <div className="delivery-detail-row">

                        <Landmark
                          size={17}
                        />

                        <div>
                          <span>
                            Nearest Landmark
                          </span>

                          <strong>
                            {delivery.nearestLandmark}
                          </strong>
                        </div>

                      </div>
                    )}


                    {delivery.deliveryInstructions && (
                      <div className="delivery-detail-row">

                        <FileText
                          size={17}
                        />

                        <div>
                          <span>
                            Delivery Instructions
                          </span>

                          <strong>
                            {delivery.deliveryInstructions}
                          </strong>
                        </div>

                      </div>
                    )}


                    <div className="delivery-signature-note">
                      <ShieldCheck
                        size={16}
                      />

                      Signature required
                      when the gemstone
                      is delivered.
                    </div>


                    {deliveryLocked && (
                      <div className="delivery-lock-notice">

                        <LockKeyhole
                          size={18}
                        />

                        <div>
                          <strong>
                            Delivery details locked
                          </strong>

                          <p>
                            This gemstone has
                            already been handed
                            over for delivery.
                            The recipient and
                            address can no longer
                            be changed.
                          </p>
                        </div>

                      </div>
                    )}

                  </div>
                )}


              {/* NO DELIVERY */}

              {!editingDelivery &&
                !delivery && (

                  <div className="payment-warning">

                    Complete the recipient
                    and delivery information
                    before payment.

                  </div>
                )}


              {/* ===============================================
                  DELIVERY EDIT FORM
              ================================================ */}

              {editingDelivery && (

                <div className="delivery-address-form">

                  <div className="delivery-form-section">

                    <h4>
                      Recipient Details
                    </h4>


                    <div className="delivery-form-row">

                      <label>
                        Recipient Full Name *

                        <input
                          name="recipientName"
                          value={
                            deliveryForm
                              .recipientName
                          }
                          onChange={
                            updateDeliveryField
                          }
                          placeholder="Full name of recipient"
                        />
                      </label>


                      <label>
                        Mobile Number *

                        <input
                          name="recipientPhone"
                          value={
                            deliveryForm
                              .recipientPhone
                          }
                          onChange={
                            updateDeliveryField
                          }
                          placeholder="+94 77 123 4567"
                        />
                      </label>

                    </div>


                    <label>
                      Alternate Phone

                      <input
                        name="alternatePhone"
                        value={
                          deliveryForm
                            .alternatePhone
                        }
                        onChange={
                          updateDeliveryField
                        }
                        placeholder="Optional"
                      />
                    </label>

                  </div>


                  <div className="delivery-form-section">

                    <h4>
                      Delivery Address
                    </h4>


                    <label>
                      Address Line 1 *

                      <input
                        name="addressLine1"
                        value={
                          deliveryForm
                            .addressLine1
                        }
                        onChange={
                          updateDeliveryField
                        }
                        placeholder="House / building number and street"
                      />
                    </label>


                    <label>
                      Address Line 2

                      <input
                        name="addressLine2"
                        value={
                          deliveryForm
                            .addressLine2
                        }
                        onChange={
                          updateDeliveryField
                        }
                        placeholder="Apartment, floor, unit, etc."
                      />
                    </label>


                    <div className="delivery-form-row">

                      <label>
                        City / Town *

                        <input
                          name="city"
                          value={
                            deliveryForm.city
                          }
                          onChange={
                            updateDeliveryField
                          }
                          placeholder="Colombo"
                        />
                      </label>


                      <label>
                        District *

                        <input
                          name="district"
                          value={
                            deliveryForm
                              .district
                          }
                          onChange={
                            updateDeliveryField
                          }
                          placeholder="Colombo"
                        />
                      </label>

                    </div>


                    <div className="delivery-form-row">

                      <label>
                        Province / Region *

                        <input
                          name="region"
                          value={
                            deliveryForm
                              .region
                          }
                          onChange={
                            updateDeliveryField
                          }
                          placeholder="Western"
                        />
                      </label>


                      <label>
                        Postal Code *

                        <input
                          name="postalCode"
                          value={
                            deliveryForm
                              .postalCode
                          }
                          onChange={
                            updateDeliveryField
                          }
                          placeholder="00300"
                        />
                      </label>

                    </div>


                    <label>
                      Country Code *

                      <input
                        name="countryCode"
                        maxLength={2}
                        value={
                          deliveryForm
                            .countryCode
                        }
                        onChange={
                          updateDeliveryField
                        }
                        placeholder="LK"
                      />
                    </label>


                    <label>
                      Nearest Landmark

                      <input
                        name="nearestLandmark"
                        value={
                          deliveryForm
                            .nearestLandmark
                        }
                        onChange={
                          updateDeliveryField
                        }
                        placeholder="Optional landmark"
                      />
                    </label>


                    <label>
                      Delivery Instructions

                      <textarea
                        name="deliveryInstructions"
                        rows={3}
                        value={
                          deliveryForm
                            .deliveryInstructions
                        }
                        onChange={
                          updateDeliveryField
                        }
                        placeholder="Gate access, call before arrival, etc."
                      />
                    </label>

                  </div>


                  {deliveryError && (
                    <div className="payment-warning">
                      {deliveryError}
                    </div>
                  )}


                  <div className="delivery-form-actions">

                    <button
                      type="button"
                      className="hero-btn secondary"
                      onClick={
                        cancelDeliveryEdit
                      }
                    >
                      <X size={16} />
                      Cancel
                    </button>


                    <button
                      type="button"
                      className="premium-pay-btn"
                      disabled={
                        savingDelivery
                      }
                      onClick={
                        handleSaveDelivery
                      }
                    >
                      <CheckCircle2
                        size={17}
                      />

                      {savingDelivery
                        ? "Saving..."
                        : "Save Delivery Details"}
                    </button>

                  </div>

                </div>
              )}

            </div>


            {/* =================================================
                PAYMENT
            ================================================== */}

            <div className="payment-method-panel">

              <div className="payment-method-heading">

                <CreditCard
                  size={23}
                />

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


              {paymentError && (
                <div className="payment-warning">
                  {paymentError}
                </div>
              )}


              {!deliveryComplete &&
                !alreadyPaid && (

                  <div className="payment-warning">
                    Complete all required
                    delivery information
                    before making payment.
                  </div>
                )}


              {!canPay &&
                !alreadyPaid &&
                deliveryComplete && (

                  <div className="payment-warning">

                    This order must be
                    confirmed by the seller
                    before payment can be made.

                  </div>
                )}


              {alreadyPaid && (
                <div className="payment-already-completed">

                  <CheckCircle2
                    size={20}
                  />

                  <div>
                    <strong>
                      Payment completed
                    </strong>

                    <p>
                      This order has already
                      been paid. Delivery
                      details can still be
                      changed until courier
                      handover.
                    </p>
                  </div>

                </div>
              )}


              {!alreadyPaid && (
                <button
                  type="button"
                  className="checkout-submit-btn"

                  disabled={
                    !canPay ||
                    processing ||
                    editingDelivery ||
                    savingDelivery
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


          {/* =================================================
              ORDER SUMMARY
          ================================================== */}

          <aside className="glass-card payment-order-summary">

            <span className="section-mini-title">
              ORDER SUMMARY
            </span>


            <div className="payment-gem-preview">

              {imageUrl &&
                !imageFailed ? (

                <img
                  src={
                    imageUrl
                  }
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


            <div className="payment-summary-row">

              <span>
                Order Number
              </span>

              <strong>
                {order.orderNumber ||
                  order.id}
              </strong>

            </div>


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


            <div className="payment-summary-row">

              <span>
                Order Status
              </span>

              <strong>
                {order.status}
              </strong>

            </div>


            <div className="payment-summary-row">

              <span>
                Fulfilment
              </span>

              <strong>
                {order.fulfillmentStatus ||
                  "Pending"}
              </strong>

            </div>


            {delivery && (
              <div className="payment-summary-row">

                <span>
                  Deliver To
                </span>

                <strong>
                  {delivery.city},{" "}
                  {delivery.region}
                </strong>

              </div>
            )}


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


// =========================================================
// DELIVERY FORM HELPERS
// =========================================================

function buildDeliveryForm(
  order
) {
  const delivery =
    order?.deliveryDetails;

  if (delivery) {
    return {
      recipientName:
        delivery.recipientName ||
        "",

      recipientPhone:
        delivery.recipientPhone ||
        "",

      alternatePhone:
        delivery.alternatePhone ||
        "",

      addressLine1:
        delivery.addressLine1 ||
        "",

      addressLine2:
        delivery.addressLine2 ||
        "",

      city:
        delivery.city ||
        "",

      district:
        delivery.district ||
        "",

      region:
        delivery.region ||
        "",

      postalCode:
        delivery.postalCode ||
        "",

      countryCode:
        delivery.countryCode ||
        "LK",

      nearestLandmark:
        delivery.nearestLandmark ||
        "",

      deliveryInstructions:
        delivery.deliveryInstructions ||
        "",
    };
  }


  // ===========================================
  // LEGACY ORDER FALLBACK
  //
  // Useful for orders created before
  // structured delivery was introduced.
  // ===========================================

  return {
    ...EMPTY_DELIVERY,

    recipientName:
      order?.buyerName ||
      "",

    addressLine1:
      order?.shippingAddress ||
      "",

    region:
      order?.shippingRegion ||
      "",

    countryCode:
      order?.shippingCountryCode ||
      "LK",
  };
}


function validateDeliveryForm(
  form
) {
  if (
    !form.recipientName.trim()
  ) {
    return "Recipient full name is required.";
  }

  if (
    !form.recipientPhone.trim()
  ) {
    return "Recipient mobile number is required.";
  }

  if (
    !form.addressLine1.trim()
  ) {
    return "Address line 1 is required.";
  }

  if (
    !form.city.trim()
  ) {
    return "City or town is required.";
  }

  if (
    !form.district.trim()
  ) {
    return "District is required.";
  }

  if (
    !form.region.trim()
  ) {
    return "Province or region is required.";
  }

  if (
    !form.postalCode.trim()
  ) {
    return "Postal code is required.";
  }

  if (
    form.countryCode
      .trim()
      .length !== 2
  ) {
    return "Enter a valid two-letter country code.";
  }

  return "";
}


function hasCompleteDeliveryDetails(
  delivery
) {
  if (!delivery) {
    return false;
  }

  return Boolean(
    delivery.recipientName?.trim() &&
    delivery.recipientPhone?.trim() &&
    delivery.addressLine1?.trim() &&
    delivery.city?.trim() &&
    delivery.district?.trim() &&
    delivery.region?.trim() &&
    delivery.postalCode?.trim() &&
    delivery.countryCode?.trim()
  );
}