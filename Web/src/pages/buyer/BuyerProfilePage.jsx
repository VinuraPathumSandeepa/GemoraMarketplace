import { useState } from "react";
import { CheckCircle2, Gem, Mail, MapPin, Pencil, Save, ShieldCheck, UserRound, X } from "lucide-react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import UserAvatar from "../../components/UserAvatar";
import { PageHeading, SectionHeading } from "../../components/buyer/BuyerUI";

export default function BuyerProfilePage() {
  const { user } = useAuth();
  return <ProfileContent key={user?.id || user?.email} user={user} />;
}
function ProfileContent({ user }) {
  const initial = () => ({ fullName: user?.fullName || user?.name || "", email: user?.email || "", phoneNumber: user?.phoneNumber || "", region: user?.region || "", countryCode: user?.countryCode || "" });
  const [form, setForm] = useState(initial);
  const [isEditing, setIsEditing] = useState(false);
  const [notice, setNotice] = useState("");
  const cancel = () => { setForm(initial()); setIsEditing(false); setNotice(""); };
  const save = (e) => { e.preventDefault(); setIsEditing(false); setNotice("Your preview has been updated on this page. These changes have not been saved to your account."); };
  return <div className="gm-page gm-profile-page"><PageHeading eyebrow="YOUR SPACE AT GEMORA" title="A profile as individual as you." description="Your account details, a little peace of mind, and everything you need for your next discovery." />
    <section className="gm-profile-banner gm-glass"><div className="gm-profile-identity"><UserAvatar user={user} size={80} /><div><span className="gm-badge"><UserRound size={14} />{user?.role || "Buyer"} account</span><h2>{form.fullName || "Gemora buyer"}</h2><p>{form.email}</p></div></div>{!isEditing && <button className="gm-button" onClick={() => { setIsEditing(true); setNotice(""); }}><Pencil size={17} />Edit profile</button>}</section>
    <div className="gm-profile-overview">{[[UserRound, "Your role", user?.role || "Buyer"], [MapPin, "Your location", form.region || form.countryCode || "Not provided"], [Gem, "Your collection", "A discovery away"]].map(([Icon, label, value]) => <div className="gm-glass" key={label}><Icon size={22} strokeWidth={1.5} /><div><small>{label}</small><strong>{value}</strong></div></div>)}</div>
    <div className="gm-profile-grid"><section className="gm-profile-form gm-glass"><SectionHeading eyebrow="THE ESSENTIALS" title="Personal information" /><p className="gm-form-intro">Keep your information easy to review before your next purchase.</p><div className="gm-inline-notice"><ShieldCheck size={19} /><p>Profile editing is currently a preview. Changes on this page are not saved to your account.</p></div>{notice && <p className="gm-feedback" role="status">{notice}</p>}
      <form onSubmit={save}><div className="gm-form-grid">{[["fullName", "Full name", "text", "Your full name"], ["email", "Email address", "email", ""], ["phoneNumber", "Phone number", "tel", "Add your phone number"], ["region", "Region", "text", "Add your region"], ["countryCode", "Country code", "text", "LK"]].map(([name, label, type, placeholder]) => <label key={name} htmlFor={`profile-${name}`}>{label}<input id={`profile-${name}`} type={type} name={name} value={form[name]} readOnly={!isEditing || name === "email"} placeholder={placeholder} onChange={(e) => setForm({ ...form, [name]: e.target.value })} />{name === "email" && <small>Your account email is read-only.</small>}</label>)}</div>{isEditing && <div className="gm-save-bar"><button className="gm-button gm-button-secondary" type="button" onClick={cancel}><X size={17} />Cancel</button><button className="gm-button" type="submit"><Save size={17} />Save preview</button></div>}</form></section>
      <aside className="gm-profile-aside"><section className="gm-glass"><SectionHeading eyebrow="ACCOUNT & ACCESS" title="At a glance" /><div className="gm-security-item"><CheckCircle2 size={23} /><div><strong>Signed in to Gemora</strong><p>Your buyer account gives you access to the collection and your orders.</p></div></div><div className="gm-key-value"><span>Marketplace role</span><strong>{user?.role || "Buyer"}</strong></div><div className="gm-key-value"><span>Account email</span><Mail size={17} /></div><p>Review your delivery information during checkout. Each order keeps its own delivery details.</p></section>
      <section className="gm-glass"><SectionHeading eyebrow="A MORE PERSONAL COLLECTION" title="Your preferences" /><div className="gm-chip-list"><span>Sapphires</span><span>Ceylon gems</span><span>Certificate details</span></div><p>Personalised gemstone preferences and your AI gem advisor are coming soon. These are suggestions to explore, not saved preferences.</p><Link to="/buyer/marketplace" className="gm-text-link">Discover what speaks to you →</Link></section></aside></div>
  </div>;
}
