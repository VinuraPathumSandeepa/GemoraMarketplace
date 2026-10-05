import {
  Gem,
  Mail,
  MapPin,
  Phone,
  ShieldCheck,
} from "lucide-react";

import {
  Link,
} from "react-router-dom";

export default function SiteFooter() {
  const year =
    new Date().getFullYear();

  return (
    <footer className="gemora-footer">
      <div className="footer-glow" />

      <div className="footer-grid">
        {/* BRAND */}
        <div className="footer-brand-column">
          <div className="footer-logo">
            <div>
              G
            </div>

            <section>
              <strong>
                GEMORA
              </strong>

              <span>
                Ceylon Gem Marketplace
              </span>
            </section>
          </div>

          <p>
            A trusted digital marketplace
            connecting verified gemstone sellers
            and buyers through secure,
            transparent and traceable
            transactions.
          </p>

          <div className="footer-trust">
            <span>
              <ShieldCheck size={16} />
              Verified Marketplace
            </span>

            <span>
              <Gem size={16} />
              Certified Listings
            </span>
          </div>
        </div>

        {/* MARKETPLACE */}
        <div className="footer-column">
          <h4>
            Marketplace
          </h4>

          <Link to="/buyer/marketplace">
            Browse Gemstones
          </Link>

          <Link to="/buyer/orders">
            My Orders
          </Link>

          <Link to="/buyer/profile">
            My Profile
          </Link>

          <Link to="/buyer/dashboard">
            Buyer Dashboard
          </Link>
        </div>

        {/* LEGAL */}
        <div className="footer-column">
          <h4>
            Legal & Support
          </h4>

          <a href="#privacy">
            Privacy Policy
          </a>

          <a href="#terms">
            Terms & Conditions
          </a>

          <a href="#security">
            Marketplace Security
          </a>

          <a href="mailto:support@gemora.lk">
            Help & Support
          </a>
        </div>

        {/* CONTACT */}
        <div className="footer-column footer-contact">
          <h4>
            Contact Gemora
          </h4>

          <a href="mailto:support@gemora.lk">
            <Mail size={17} />
            support@gemora.lk
          </a>

          <a href="tel:+94112345678">
            <Phone size={17} />
            +94 11 234 5678
          </a>

          <div>
            <MapPin size={17} />

            <span>
              Colombo, Sri Lanka
            </span>
          </div>

          <small>
            Customer Support:
            Mon – Fri,
            9:00 AM – 5:00 PM
          </small>
        </div>
      </div>

      <div className="footer-bottom">
        <span>
          © {year} Gemora.
          All rights reserved.
        </span>

        <span>
          Built for trusted Ceylon gemstone commerce.
        </span>
      </div>
    </footer>
  );
}