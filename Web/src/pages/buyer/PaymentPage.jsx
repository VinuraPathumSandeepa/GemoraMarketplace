import { PaymentDeadline } from "../../components/OrderInbox";
import { utcTime } from "../../utils/orderTime";
import { GemImage } from "../../components/buyer/BuyerUI";
import {
  useEffect,
  useState,
} from "react";

import {
  ArrowLeft,
  CheckCircle2,
  CreditCard,
  FileText,
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
  confirmPayment,
  createPaymentIntent,
  getOrderById,
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

  const [clock, setClock] = useState(() => Date.now());
  useEffect(() => { const timer = setInterval(() => setClock(Date.now()), 1000); return () => clearInterval(timer); }, []);

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
    paymentStep,
    setPaymentStep,
  ] = useState("entry");

  const [
    paymentIntent,
    setPaymentIntent,
  ] = useState(null);

  const [
    cardForm,
    setCardForm,
  ] = useState({
    cardholderName: "",
    cardNumber: "",
    expiry: "",
    cvc: "",
  });



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

  function updateCardField(event) {
    const { name, value } = event.target;

    setCardForm((current) => ({
      ...current,
      [name]:
        name === "cardNumber"
          ? formatCardNumber(value)
          : name === "expiry"
            ? formatExpiry(value)
            : value,
    }));
  }

  async function handleCardDetailsSubmit(event) {
    event.preventDefault();

    const validationMessage = validateCardForm(cardForm);

    if (validationMessage) {
      setPaymentError(validationMessage);
      return;
    }

    try {
      setProcessing(true);
      setPaymentError("");

      const intent = await createPaymentIntent(order.id, "Card");
      setPaymentIntent(intent);
      setPaymentStep("review");
    } catch (err) {
      console.error("Unable to create payment intent:", err);
      setPaymentError(
        err.message ||
        "Unable to prepare the card payment."
      );
    } finally {
      setProcessing(false);
    }
  }

  function editCardDetails() {
    setPaymentError("");
    setPaymentStep("entry");
  }

  async function handleConfirmPayment() {
    if (!paymentIntent) {
      setPaymentError("Start the card payment again before confirming.");
      setPaymentStep("entry");
      return;
    }

    try {
      setProcessing(true);
      setPaymentError("");

      const paymentMethodToken =
        await createDemoPaymentMethodToken(cardForm);

      const result = await confirmPayment(
        order.id,
        {
          paymentIntentId:
            paymentIntent.paymentIntentId,
          paymentMethodToken,
          paymentMethod: "Card",
        }
      );

      setPaymentSuccess(result);

      if (result?.order) {
        setOrder(result.order);
        setDeliveryForm(buildDeliveryForm(result.order));
      }
    } catch (err) {
      console.error("Payment failed:", err);
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
    deliveryComplete && utcTime(order.paymentDueAt) > clock;


  const alreadyPaid =
    order.status ===
    "Paid" ||

    order.status ===
    "Completed" ||

    Boolean(
      order.paidAt
    );

  const advanceAmount = calculateAdvanceAmount(
    order.agreedPrice
  );

  const payableAmount = Number(
    paymentIntent?.amount ?? advanceAmount
  );

  const remainingAmount = Math.max(
    0,
    Number(order.agreedPrice || 0) - payableAmount
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
                Advance Paid (60%)
              </span>

              <strong>
                {formatPaymentAmount(
                  paymentSuccess
                    .amount,

                  paymentSuccess
                    .currency
                )}
              </strong>
            </div>

            <div>
              <span>
                Remaining Balance
              </span>

              <strong>
                {formatPaymentAmount(
                  paymentSuccess.remainingAmount ??
                    Math.max(
                      0,
                      Number(
                        paymentSuccess.order?.agreedPrice ||
                        paymentSuccess.orderTotalAmount ||
                        0
                      ) - Number(paymentSuccess.amount || 0)
                    ),
                  paymentSuccess.currency
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

            {["Confirmed", "AwaitingPayment"].includes(order.status) && <PaymentDeadline dueAt={order.paymentDueAt} />}
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
                <CreditCard size={23} />

                <div>
                  <h3>Card Payment</h3>

                  <p>
                    Secure Gemora payment processing
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
                    Complete all required delivery information
                    before making payment.
                  </div>
                )}

              {!canPay &&
                !alreadyPaid &&
                deliveryComplete && (
                  <div className="payment-warning">
                    This order must be confirmed by the seller
                    before payment can be made.
                  </div>
                )}

              {alreadyPaid && (
                <div className="payment-already-completed">
                  <CheckCircle2 size={20} />

                  <div>
                    <strong>Payment completed</strong>

                    <p>
                      This order has already been paid. Delivery
                      details can still be changed until courier
                      handover.
                    </p>
                  </div>
                </div>
              )}

              {!alreadyPaid && (
                <div className="payment-advance-summary">
                  <div>
                    <span>Order Total (100%)</span>
                    <strong>
                      {formatPrice(order.agreedPrice, order.currency)}
                    </strong>
                  </div>
                  <div>
                    <span>Advance Due Now (60%)</span>
                    <strong>
                      {formatPaymentAmount(
                        advanceAmount,
                        order.currency
                      )}
                    </strong>
                  </div>
                  <p>
                    Only a 60% advance is collected now. The remaining{" "}
                    {formatPaymentAmount(
                      remainingAmount,
                      order.currency
                    )}{" "}
                    will be settled in the next payment stage.
                  </p>
                </div>
              )}

              {!alreadyPaid &&
                canPay &&
                !editingDelivery && (
                  paymentStep === "entry" ? (
                    <form
                      className="payment-card-form"
                      onSubmit={handleCardDetailsSubmit}
                    >
                      <div className="payment-card-form-note">
                        <ShieldCheck size={18} />

                        <span>
                          Enter your card details. Gemora creates a
                          secure payment token in this browser; raw
                          card data is never stored in the order.
                        </span>
                      </div>

                      <label>
                        <span>Cardholder Name *</span>

                        <input
                          name="cardholderName"
                          value={cardForm.cardholderName}
                          onChange={updateCardField}
                          autoComplete="cc-name"
                          placeholder="Name on card"
                          required
                        />
                      </label>

                      <label>
                        <span>Card Number *</span>

                        <input
                          name="cardNumber"
                          value={cardForm.cardNumber}
                          onChange={updateCardField}
                          inputMode="numeric"
                          autoComplete="cc-number"
                          maxLength={19}
                          placeholder="4242 4242 4242 4242"
                          required
                        />
                      </label>

                      <div className="payment-card-fields-row">
                        <label>
                          <span>Expiry *</span>

                          <input
                            name="expiry"
                            value={cardForm.expiry}
                            onChange={updateCardField}
                            inputMode="numeric"
                            autoComplete="cc-exp"
                            maxLength={5}
                            placeholder="MM/YY"
                            required
                          />
                        </label>

                        <label>
                          <span>Security Code *</span>

                          <input
                            name="cvc"
                            value={cardForm.cvc}
                            onChange={updateCardField}
                            inputMode="numeric"
                            autoComplete="cc-csc"
                            maxLength={4}
                            placeholder="123"
                            required
                          />
                        </label>
                      </div>

                      <div className="payment-card-form-actions">
                        <span>
                          You will review these details before
                          payment is submitted.
                        </span>

                        <button
                          type="submit"
                          className="checkout-submit-btn"
                          disabled={processing}
                        >
                          <CreditCard size={18} />

                          {processing
                            ? "Preparing Secure Payment..."
                            : "Review Card Details"}
                        </button>
                      </div>
                    </form>
                  ) : (
                    <div className="payment-card-review">
                      <div className="payment-card-review-header">
                        <div className="payment-card-preview">
                          <CreditCard size={22} />

                          <div>
                            <strong>
                              {getCardBrand(cardForm.cardNumber)} ending in{" "}
                              {lastFourDigits(cardForm.cardNumber)}
                            </strong>

                            <span>
                              {cardForm.cardholderName} · Expires{" "}
                              {cardForm.expiry}
                            </span>
                          </div>
                        </div>

                        <button
                          type="button"
                          className="delivery-edit-btn"
                          onClick={editCardDetails}
                          disabled={processing}
                        >
                          <Pencil size={15} />
                          Edit Details
                        </button>
                      </div>

                      <div className="payment-review-confirmation">
                        <CheckCircle2 size={18} />

                        <span>
                          Review complete. Confirming will authorize{" "}
                          {formatPaymentAmount(payableAmount, order.currency)}{" "}
                          as the 60% advance and save the order as Paid after the gateway
                          approves it.
                        </span>
                      </div>

                      <div className="payment-card-form-actions">
                        <span>
                          Payment intent expires{" "}
                          {formatDate(paymentIntent?.expiresAt)}.
                        </span>

                        <button
                          type="button"
                          className="checkout-submit-btn"
                          onClick={handleConfirmPayment}
                          disabled={processing}
                        >
                          <LockKeyhole size={18} />

                          {processing
                            ? "Confirming Payment..."
                            : "Confirm & Pay " +
                              formatPaymentAmount(
                                payableAmount,
                                order.currency
                              )}
                        </button>
                      </div>
                    </div>
                  )
                )}

              {alreadyPaid && (
                <button
                  type="button"
                  className="checkout-submit-btn"
                  onClick={() => navigate("/buyer/orders")}
                >
                  <ReceiptText size={19} />
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

              <GemImage src={imageUrl} alt={order.gemTitle || "Gemstone purchase"} />
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


          <div className="payment-summary-row">
            <span>Order Total (100%)</span>
            <strong>{formatPrice(order.agreedPrice, order.currency)}</strong>
          </div>

          <div className="payment-total-row">
            <span>Advance Due (60%)</span>
            <strong>
              {formatPaymentAmount(advanceAmount, order.currency)}
            </strong>
          </div>

          <div className="payment-summary-row">
            <span>Remaining Balance</span>
            <strong>
              {formatPaymentAmount(remainingAmount, order.currency)}
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
function calculateAdvanceAmount(orderTotal) {
  const total = Number(orderTotal || 0);
  const advance = total * 0.6;

  return Math.round(advance / 1000) * 1000;
}

function formatPaymentAmount(amount, currency = "LKR") {
  return `${currency} ${Math.round(Number(amount || 0)).toLocaleString("en-LK")}`;
}

// CARD PAYMENT HELPERS
// =========================================================

function formatCardNumber(value) {
  return value
    .replace(/\D/g, "")
    .slice(0, 19)
    .replace(/(.{4})/g, "$1 ")
    .trim();
}

function formatExpiry(value) {
  const digits = value
    .replace(/\D/g, "")
    .slice(0, 4);

  if (digits.length <= 2) {
    return digits;
  }

  return `${digits.slice(0, 2)}/${digits.slice(2)}`;
}

function lastFourDigits(cardNumber) {
  return cardNumber
    .replace(/\D/g, "")
    .slice(-4);
}

function getCardBrand(cardNumber) {
  const digits = cardNumber.replace(/\D/g, "");

  if (digits.startsWith("4")) {
    return "Visa";
  }

  if (/^(5[1-5]|2[2-7])/.test(digits)) {
    return "Mastercard";
  }

  if (/^3[47]/.test(digits)) {
    return "American Express";
  }

  return "Card";
}

function validateCardForm(card) {
  const number = card.cardNumber.replace(/\D/g, "");

  if (!card.cardholderName.trim()) {
    return "Enter the name shown on the card.";
  }

  if (
    number.length < 13 ||
    number.length > 19 ||
    !passesLuhnCheck(number)
  ) {
    return "Enter a valid card number.";
  }

  const expiryMatch = /^(0[1-9]|1[0-2])\/(\d{2})$/.exec(
    card.expiry.trim()
  );

  if (!expiryMatch) {
    return "Enter the expiry date as MM/YY.";
  }

  const now = new Date();
  const expiryMonth = Number(expiryMatch[1]);
  const expiryYear = 2000 + Number(expiryMatch[2]);

  if (
    expiryYear < now.getFullYear() ||
    (
      expiryYear === now.getFullYear() &&
      expiryMonth <= now.getMonth()
    )
  ) {
    return "The card expiry date has passed.";
  }

  if (!/^\d{3,4}$/.test(card.cvc.trim())) {
    return "Enter a valid 3 or 4 digit security code.";
  }

  return "";
}

function passesLuhnCheck(number) {
  let sum = 0;
  let shouldDouble = false;

  for (let index = number.length - 1; index >= 0; index--) {
    let digit = Number(number[index]);

    if (shouldDouble) {
      digit *= 2;
      if (digit > 9) {
        digit -= 9;
      }
    }

    sum += digit;
    shouldDouble = !shouldDouble;
  }

  return sum % 10 === 0;
}

async function createDemoPaymentMethodToken(card) {
  if (!globalThis.crypto?.subtle) {
    throw new Error(
      "Secure card tokenization is not available in this browser."
    );
  }

  const payload = [
    "gemora-demo-card",
    card.cardNumber.replace(/\D/g, ""),
    card.expiry,
    card.cvc,
  ].join(":");

  const bytes = new TextEncoder().encode(payload);
  const digest = await globalThis.crypto.subtle.digest(
    "SHA-256",
    bytes
  );

  const token = Array.from(
    new Uint8Array(digest)
  )
    .map((byte) => byte.toString(16).padStart(2, "0"))
    .join("");

  return `demo_card_${token}`;
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
