import { GemImage } from "../../components/buyer/BuyerUI";
import { useEffect, useState } from "react";
import {
  ArrowLeft,
  CheckCircle2,
  MapPin,
  ShieldCheck,
  Truck,
} from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import { motion } from "framer-motion";

import {
  createOrder,
  getMarketplaceGemById,
  resolveMediaUrl,
} from "../../services/buyerApi";

export default function CheckoutPage() {
  const { gemId } = useParams();
  const navigate = useNavigate();

  const [gem, setGem] = useState(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const [form, setForm] = useState({
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
  });

  useEffect(() => {
    async function loadGem() {
      try {
        setLoading(true);

        const data =
          await getMarketplaceGemById(gemId);

        if (!data?.isAvailable) {
          setError(
            "This gemstone is no longer available for a new purchase."
          );
          return;
        }

        setGem(data);
      } catch (err) {
        setError(
          err.message ||
          "Unable to load gemstone."
        );
      } finally {
        setLoading(false);
      }
    }

    loadGem();
  }, [gemId]);




  function handleChange(e) {
    const { name, value } = e.target;

    setForm((prev) => ({
      ...prev,
      [name]: value,
    }));
  }

  async function handleSubmit(e) {
    e.preventDefault();

    const requiredFields = [
      "recipientName", "recipientPhone", "addressLine1",
      "city", "district", "region", "postalCode", "countryCode",
    ];

    if (requiredFields.some((field) => !form[field]?.trim())) {
      setError("Please complete all required delivery information.");
      return;
    }

    try {
      setSubmitting(true);
      setError("");

      const order =
        await createOrder({
          gemListingId:
            Number(gemId),

          deliveryDetails: {
            recipientName:
              form.recipientName.trim(),

            recipientPhone:
              form.recipientPhone.trim(),

            alternatePhone:
              form.alternatePhone.trim(),

            addressLine1:
              form.addressLine1.trim(),

            addressLine2:
              form.addressLine2.trim(),

            city:
              form.city.trim(),

            district:
              form.district.trim(),

            region:
              form.region.trim(),

            postalCode:
              form.postalCode.trim(),

            countryCode:
              form.countryCode
                .trim()
                .toUpperCase(),

            nearestLandmark:
              form.nearestLandmark.trim(),

            deliveryInstructions:
              form.deliveryInstructions.trim(),
          },
        });

      navigate("/buyer/orders", {
        replace: true,
        state: {
          createdOrderId: order?.id,
          orderCreated: true,
        },
      });
    } catch (err) {
      setError(
        err.message ||
        "Unable to create your order."
      );
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) {
    return (
      <div className="buyer-checkout-page">
        <div className="glass-card checkout-state-card">
          Loading checkout...
        </div>
      </div>
    );
  }

  if (error && !gem) {
    return (
      <div className="buyer-checkout-page">
        <div className="glass-card checkout-state-card">
          <h2>Checkout unavailable</h2>
          <p>{error}</p>

          <button
            className="hero-btn primary"
            onClick={() =>
              navigate("/buyer/marketplace")
            }
          >
            Back to Marketplace
          </button>
        </div>
      </div>
    );
  }

  const imageUrl =
    resolveMediaUrl(
      gem?.primaryImageUrl
    );

  return (
    <div className="buyer-checkout-page">
      <button
        className="back-inline-btn"
        onClick={() => navigate(-1)}
      >
        <ArrowLeft size={18} />
        Back to gemstone
      </button>

      <motion.div
        className="checkout-layout"
        initial={{
          opacity: 0,
          y: 20,
        }}
        animate={{
          opacity: 1,
          y: 0,
        }}
      >
        <section className="glass-card checkout-form-card">
          <span className="section-mini-title">
            SECURE ORDER REQUEST
          </span>

          <h1>Complete your order</h1>

          <p className="checkout-intro">
            Provide your delivery information.
            Your order will remain pending until
            the gemstone seller confirms it.
          </p>

          {error && (
            <div className="checkout-error">
              {error}
            </div>
          )}

          <form
            className="checkout-form"
            onSubmit={handleSubmit}
          >
            <div className="checkout-two-column">
              <label>
                <span>Recipient Name *</span>
                <input name="recipientName" value={form.recipientName} onChange={handleChange} required placeholder="Full name" />
              </label>
              <label>
                <span>Phone Number *</span>
                <input name="recipientPhone" value={form.recipientPhone} onChange={handleChange} required placeholder="0712345678" />
              </label>
            </div>

            <label>
              <span>Alternate Phone (optional)</span>
              <input name="alternatePhone" value={form.alternatePhone} onChange={handleChange} placeholder="Alternative contact number" />
            </label>

            <label>
              <span><MapPin size={16} /> Address Line 1 *</span>
              <input name="addressLine1" value={form.addressLine1} onChange={handleChange} required placeholder="House number and street" />
            </label>

            <label>
              <span>Address Line 2 (optional)</span>
              <input name="addressLine2" value={form.addressLine2} onChange={handleChange} placeholder="Apartment, building or additional details" />
            </label>

            <div className="checkout-two-column">
              <label>
                <span>City *</span>
                <input name="city" value={form.city} onChange={handleChange} required placeholder="Colombo" />
              </label>
              <label>
                <span>District *</span>
                <input name="district" value={form.district} onChange={handleChange} required placeholder="Colombo" />
              </label>
              <label>
                <span>Province / Region *</span>
                <input name="region" value={form.region} onChange={handleChange} required placeholder="Western" />
              </label>
              <label>
                <span>Postal Code *</span>
                <input name="postalCode" value={form.postalCode} onChange={handleChange} required placeholder="10350" />
              </label>
              <label>
                <span>Country Code *</span>
                <input name="countryCode" value={form.countryCode} onChange={handleChange} required maxLength={2} placeholder="LK" />
              </label>
              <label>
                <span>Nearest Landmark (optional)</span>
                <input name="nearestLandmark" value={form.nearestLandmark} onChange={handleChange} placeholder="Near main junction" />
              </label>
            </div>

            <label>
              <span>Delivery Instructions (optional)</span>
              <textarea name="deliveryInstructions" value={form.deliveryInstructions} onChange={handleChange} placeholder="Additional instructions for delivery" rows={3} />
            </label>

            <div className="checkout-security-row">
              <ShieldCheck size={20} />

              <div>
                <strong>
                  Protected marketplace order
                </strong>

                <p>
                  Payment is not collected until
                  the seller confirms the order.
                </p>
              </div>
            </div>

            <button
              type="submit"
              disabled={submitting}
              className="checkout-submit-btn"
            >
              {submitting
                ? "Creating Order..."
                : "Place Order Request"}

              {!submitting && (
                <CheckCircle2 size={19} />
              )}
            </button>
          </form>
        </section>

        <aside className="glass-card checkout-summary-card">
          <span className="section-mini-title">
            ORDER SUMMARY
          </span>

          <div className="checkout-gem-image">
            <GemImage src={imageUrl} alt={gem.title} />
          </div>

          <div>
            <span className="checkout-gem-type">
              {gem.gemType}
            </span>

            <h2>{gem.title}</h2>
          </div>

          <div className="checkout-summary-row">
            <span>Seller</span>
            <strong>
              {gem.sellerName}
            </strong>
          </div>

          <div className="checkout-summary-row">
            <span>Certification</span>
            <strong>
              {gem.certificateAuthority ||
                "Verified"}
            </strong>
          </div>

          <div className="checkout-summary-price">
            <span>Order Value</span>

            <strong>
              {gem.currency}{" "}
              {Number(
                gem.price
              ).toLocaleString(
                "en-LK",
                {
                  minimumFractionDigits: 2,
                }
              )}
            </strong>
          </div>

          <div className="checkout-delivery-note">
            <Truck size={19} />

            <span>
              Delivery information will be shared
              securely after order processing.
            </span>
          </div>
        </aside>
      </motion.div>
    </div>
  );
}
