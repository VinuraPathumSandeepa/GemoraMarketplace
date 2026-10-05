import { useEffect, useState } from "react";
import { motion } from "framer-motion";
import {
  User,
  Mail,
  Phone,
  MapPin,
  ShieldCheck,
  Pencil,
  Save,
  X,
  Gem,
  ShoppingBag,
  CheckCircle2,
} from "lucide-react";

import { useAuth } from "../../context/AuthContext";

export default function BuyerProfilePage() {
  const { user } = useAuth();

  const [isEditing, setIsEditing] = useState(false);

  const [form, setForm] = useState({
    fullName: "",
    email: "",
    phoneNumber: "",
    countryCode: "",
    region: "",
  });

  useEffect(() => {
    if (!user) return;

    setForm({
      fullName: user.fullName || user.name || "",
      email: user.email || "",
      phoneNumber: user.phoneNumber || "",
      countryCode: user.countryCode || "",
      region: user.region || "",
    });
  }, [user]);

  const handleChange = (event) => {
    const { name, value } = event.target;

    setForm((previous) => ({
      ...previous,
      [name]: value,
    }));
  };

  const handleCancel = () => {
    setForm({
      fullName: user?.fullName || user?.name || "",
      email: user?.email || "",
      phoneNumber: user?.phoneNumber || "",
      countryCode: user?.countryCode || "",
      region: user?.region || "",
    });

    setIsEditing(false);
  };

  const handleSave = () => {
    // Backend profile update API will be connected later.
    setIsEditing(false);
  };

  const initials =
    form.fullName
      ?.split(" ")
      .filter(Boolean)
      .map((name) => name[0])
      .slice(0, 2)
      .join("")
      .toUpperCase() || "BU";

  return (
    <motion.div
      className="buyer-profile-page"
      initial={{ opacity: 0, y: 25 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5 }}
    >
      {/* ============================
          PROFILE HERO
      ============================ */}

      <section className="buyer-profile-hero glass-card">
        <div className="buyer-profile-hero-glow" />

        <div className="buyer-profile-main">
          <div className="buyer-profile-avatar">
            {user?.profileImageUrl ? (
              <img
                src={user.profileImageUrl}
                alt={form.fullName}
              />
            ) : (
              <span>{initials}</span>
            )}

            <div className="buyer-profile-verified-dot">
              <CheckCircle2 size={16} />
            </div>
          </div>

          <div className="buyer-profile-heading">
            <span className="buyer-profile-role">
              <ShieldCheck size={15} />
              Verified Buyer
            </span>

            <h1>{form.fullName || "Gemora Buyer"}</h1>

            <p>
              Manage your personal details, marketplace identity,
              shipping information and account preferences.
            </p>
          </div>
        </div>

        {!isEditing ? (
          <button
            className="buyer-profile-edit-btn"
            onClick={() => setIsEditing(true)}
          >
            <Pencil size={17} />
            Edit Profile
          </button>
        ) : (
          <div className="buyer-profile-edit-actions">
            <button
              className="buyer-profile-cancel-btn"
              onClick={handleCancel}
            >
              <X size={17} />
              Cancel
            </button>

            <button
              className="buyer-profile-save-btn"
              onClick={handleSave}
            >
              <Save size={17} />
              Save Changes
            </button>
          </div>
        )}
      </section>

      {/* ============================
          PROFILE STATS
      ============================ */}

      <section className="buyer-profile-stats">
        <ProfileStat
          icon={<Gem size={21} />}
          value="Verified"
          label="Account Status"
        />

        <ProfileStat
          icon={<ShoppingBag size={21} />}
          value="Buyer"
          label="Marketplace Role"
        />

        <ProfileStat
          icon={<ShieldCheck size={21} />}
          value="Secure"
          label="Account Protection"
        />
      </section>

      {/* ============================
          MAIN GRID
      ============================ */}

      <section className="buyer-profile-grid">
        <div className="buyer-profile-form-card glass-card">
          <div className="buyer-profile-section-title">
            <div>
              <span>Personal Information</span>
              <h2>Account Details</h2>
            </div>

            <User size={22} />
          </div>

          <div className="buyer-profile-form-grid">
            <ProfileField
              icon={<User size={17} />}
              label="Full Name"
              name="fullName"
              value={form.fullName}
              editing={isEditing}
              onChange={handleChange}
            />

            <ProfileField
              icon={<Mail size={17} />}
              label="Email Address"
              name="email"
              value={form.email}
              editing={false}
              onChange={handleChange}
            />

            <ProfileField
              icon={<Phone size={17} />}
              label="Phone Number"
              name="phoneNumber"
              value={form.phoneNumber}
              editing={isEditing}
              onChange={handleChange}
              placeholder="Add phone number"
            />

            <ProfileField
              icon={<MapPin size={17} />}
              label="Region"
              name="region"
              value={form.region}
              editing={isEditing}
              onChange={handleChange}
              placeholder="Add region"
            />

            <ProfileField
              icon={<MapPin size={17} />}
              label="Country Code"
              name="countryCode"
              value={form.countryCode}
              editing={isEditing}
              onChange={handleChange}
              placeholder="LK"
            />
          </div>
        </div>

        {/* RIGHT SIDE */}

        <div className="buyer-profile-side-column">
          <section className="buyer-profile-security glass-card">
            <div className="buyer-profile-section-title">
              <div>
                <span>Account Security</span>
                <h2>Verification</h2>
              </div>

              <ShieldCheck size={22} />
            </div>

            <div className="buyer-security-status">
              <div className="buyer-security-icon">
                <CheckCircle2 size={24} />
              </div>

              <div>
                <strong>Email Verified</strong>
                <p>
                  Your email address is verified and your account is
                  active.
                </p>
              </div>
            </div>

            <div className="buyer-security-row">
              <span>Account Role</span>
              <strong>{user?.role || "Buyer"}</strong>
            </div>

            <div className="buyer-security-row">
              <span>Marketplace Access</span>
              <strong className="buyer-success-text">
                Enabled
              </strong>
            </div>
          </section>

          <section className="buyer-profile-preferences glass-card">
            <div className="buyer-profile-section-title">
              <div>
                <span>Marketplace</span>
                <h2>Buyer Preferences</h2>
              </div>

              <Gem size={22} />
            </div>

            <div className="buyer-preference-chip-list">
              <span>Sapphires</span>
              <span>Certified Gems</span>
              <span>Sri Lankan Gems</span>
              <span>Premium Listings</span>
            </div>

            <p className="buyer-preference-note">
              AI-assisted gemstone preferences will be connected to
              your Marketplace Agent later.
            </p>
          </section>
        </div>
      </section>
    </motion.div>
  );
}

function ProfileField({
  icon,
  label,
  name,
  value,
  editing,
  onChange,
  placeholder = "",
}) {
  return (
    <div className="buyer-profile-field">
      <label>
        {icon}
        {label}
      </label>

      {editing ? (
        <input
          type="text"
          name={name}
          value={value}
          placeholder={placeholder}
          onChange={onChange}
        />
      ) : (
        <div className="buyer-profile-field-value">
          {value || "Not provided"}
        </div>
      )}
    </div>
  );
}

function ProfileStat({ icon, value, label }) {
  return (
    <motion.div
      className="buyer-profile-stat-card glass-card"
      whileHover={{
        y: -6,
        scale: 1.02,
      }}
      transition={{
        duration: 0.25,
      }}
    >
      <div className="buyer-profile-stat-icon">
        {icon}
      </div>

      <div>
        <strong>{value}</strong>
        <span>{label}</span>
      </div>
    </motion.div>
  );
}