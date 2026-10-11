import { useRef, useState } from "react";
import { CheckCircle2, Gem, Mail, MapPin, Pencil, Save, ShieldCheck, UserRound, X, Camera, Trash2 } from "lucide-react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import api from "../../services/api";
import UserAvatar from "../../components/UserAvatar";
import { PageHeading, SectionHeading } from "../../components/buyer/BuyerUI";

export default function BuyerProfilePage() {
  const { user } = useAuth();
  return <ProfileContent key={user?.id || user?.email} user={user} />;
}
function ProfileContent({ user }) {
  const { refreshUser } = useAuth();
  const fileInputRef = useRef(null);
  const initial = () => ({ fullName: user?.fullName || user?.name || "", email: user?.email || "", phoneNumber: user?.phoneNumber || "", region: user?.region || "", countryCode: user?.countryCode || "" });
  const [form, setForm] = useState(initial);
  const [isEditing, setIsEditing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [photoBusy, setPhotoBusy] = useState(false);
  const [notice, setNotice] = useState("");
  const [error, setError] = useState("");
  const cancel = () => { setForm(initial()); setIsEditing(false); setNotice(""); setError(""); };
  const save = async (event) => {
    event.preventDefault();
    setNotice(""); setError(""); setSaving(true);
    try {
      await api.put("/Auth/me/profile", {
        fullName: form.fullName.trim(),
        phoneNumber: form.phoneNumber.trim(),
        region: form.region.trim(),
        countryCode: form.countryCode.trim().toUpperCase(),
      });
      await refreshUser();
      setIsEditing(false);
      setNotice("Your profile has been saved.");
    } catch (requestError) {
      setError(requestError?.response?.data?.message || "We couldn't save your profile. Please try again.");
    } finally { setSaving(false); }
  };
  const uploadPhoto = async (event) => {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;
    setNotice(""); setError("");
    if (!["image/jpeg", "image/png", "image/webp"].includes(file.type)) {
      setError("Choose a JPG, PNG, or WebP image."); return;
    }
    if (file.size > 5 * 1024 * 1024) { setError("Profile photos must be 5 MB or smaller."); return; }
    const data = new FormData();
    data.append("file", file);
    setPhotoBusy(true);
    try {
      await api.post("/Auth/me/profile-image", data);
      await refreshUser();
      setNotice("Your profile photo has been saved.");
    } catch (requestError) {
      setError(requestError?.response?.data?.message || "We couldn't upload your profile photo. Please try again.");
    } finally { setPhotoBusy(false); }
  };
  const removePhoto = async () => {
    setNotice(""); setError(""); setPhotoBusy(true);
    try {
      await api.delete("/Auth/me/profile-image");
      await refreshUser();
      setNotice("Your profile photo has been removed.");
    } catch (requestError) {
      setError(requestError?.response?.data?.message || "We couldn't remove your profile photo. Please try again.");
    } finally { setPhotoBusy(false); }
  };
  const feedback = error || notice;
  return <div className="gm-page gm-profile-page"><PageHeading eyebrow="YOUR SPACE AT GEMORA" title="A profile as individual as you." description="Your account details, a little peace of mind, and everything you need for your next discovery." />
    <section className="gm-profile-banner gm-glass"><div className="gm-profile-identity"><UserAvatar user={user} size={80} /><div><span className="gm-badge"><UserRound size={14} />{user?.role || "Buyer"} account</span><h2>{user?.fullName || user?.name || "Gemora buyer"}</h2><p>{user?.email}</p></div></div><div className="gm-profile-photo-actions"><input ref={fileInputRef} type="file" accept="image/jpeg,image/png,image/webp" hidden onChange={uploadPhoto} /><button className="gm-button" type="button" onClick={() => fileInputRef.current?.click()} disabled={photoBusy} aria-label={user?.profileImageUrl ? "Change profile photo" : "Upload profile photo"}><Camera size={17} />{photoBusy ? "Saving photo…" : user?.profileImageUrl ? "Change photo" : "Add profile photo"}</button>{user?.profileImageUrl && <button className="gm-button gm-button-secondary" type="button" onClick={removePhoto} disabled={photoBusy}><Trash2 size={17} />Remove photo</button>}</div></section>
    <div className="gm-profile-overview">{[[UserRound, "Your role", user?.role || "Buyer"], [MapPin, "Your location", form.region || form.countryCode || "Not provided"], [Gem, "Your collection", "A discovery away"]].map(([Icon, label, value]) => <div className="gm-glass" key={label}><Icon size={22} strokeWidth={1.5} /><div><small>{label}</small><strong>{value}</strong></div></div>)}</div>
    <div className="gm-profile-grid"><section className="gm-profile-form gm-glass"><SectionHeading eyebrow="THE ESSENTIALS" title="Personal information" /><p className="gm-form-intro">Keep your information easy to review before your next purchase.</p>{feedback && <p className={error ? "gm-feedback gm-feedback-error" : "gm-feedback gm-feedback-success"} role={error ? "alert" : "status"}>{feedback}</p>}
      <form onSubmit={save}><div className="gm-form-grid">{[["fullName", "Full name", "text", "Your full name"], ["email", "Email address", "email", ""], ["phoneNumber", "Phone number", "tel", "Add your phone number"], ["region", "Region", "text", "Add your region"], ["countryCode", "Country code", "text", "LK"]].map(([name, label, type, placeholder]) => <label key={name} htmlFor={`profile-${name}`}>{label}<input id={`profile-${name}`} type={type} name={name} value={form[name]} readOnly={!isEditing || name === "email"} required={name === "fullName"} maxLength={name === "fullName" || name === "region" ? 100 : name === "phoneNumber" ? 20 : name === "countryCode" ? 2 : undefined} placeholder={placeholder} onChange={(e) => setForm(current => ({ ...current, [name]: e.target.value }))} />{name === "email" && <small>Your account email is read-only.</small>}</label>)}</div>{isEditing ? <div className="gm-save-bar"><button className="gm-button gm-button-secondary" type="button" onClick={cancel} disabled={saving}><X size={17} />Cancel</button><button className="gm-button" type="submit" disabled={saving}><Save size={17} />{saving ? "Saving…" : "Save changes"}</button></div> : <div className="gm-save-bar"><button className="gm-button" type="button" onClick={() => { setIsEditing(true); setNotice(""); setError(""); }}><Pencil size={17} />Edit profile</button></div>}</form>
    </section><aside className="gm-profile-aside"><section className="gm-glass"><SectionHeading eyebrow="ACCOUNT & ACCESS" title="At a glance" /><div className="gm-security-item"><CheckCircle2 size={23} /><div><strong>Signed in to Gemora</strong><p>Your buyer account gives you access to the collection and your orders.</p></div></div><div className="gm-key-value"><span>Marketplace role</span><strong>{user?.role || "Buyer"}</strong></div><div className="gm-key-value"><span>Account email</span><Mail size={17} /></div><p>Review your delivery information during checkout. Each order keeps its own delivery details.</p></section>
      <section className="gm-glass"><SectionHeading eyebrow="A MORE PERSONAL COLLECTION" title="Your preferences" /><div className="gm-chip-list"><span>Sapphires</span><span>Ceylon gems</span><span>Certificate details</span></div><p>Personalised gemstone preferences and your AI gem advisor are coming soon. These are suggestions to explore, not saved preferences.</p><Link to="/buyer/marketplace" className="gm-text-link">Discover what speaks to you →</Link></section></aside></div>
  </div>;
}
