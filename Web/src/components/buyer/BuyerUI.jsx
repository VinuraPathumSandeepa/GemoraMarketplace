import { useEffect, useId, useRef, useState } from "react";
import { ArrowRight, Gem, Search, ShieldCheck, Truck, X } from "lucide-react";
import { Link } from "react-router-dom";
import { resolveMediaUrl } from "../../services/buyerApi";

export function GemImage({ src, alt = "Gemstone", className = "" }) {
  const url = resolveMediaUrl(src);
  const [failedUrl, setFailedUrl] = useState(null);
  return <div className={`gm-image ${className}`}>
    {url && failedUrl !== url ? <img src={url} alt={alt} loading="lazy" decoding="async" width="600" height="600" onError={() => setFailedUrl(url)} /> :
      <div className="gm-image-fallback"><Gem size={56} strokeWidth={1} aria-hidden="true" /><span>{alt}</span><small>Image unavailable</small></div>}
  </div>;
}

export function Dialog({ open, onClose, title, children, className = "" }) {
  const ref = useRef(null);
  const id = useId();
  useEffect(() => {
    const dialog = ref.current;
    if (!open) return;
    const previous = document.activeElement;
    const overflow = document.body.style.overflow;
    dialog.showModal();
    document.body.style.overflow = "hidden";
    return () => {
      dialog.close();
      document.body.style.overflow = overflow;
      if (previous?.isConnected) previous.focus();
    };
  }, [open]);
  return <dialog ref={ref} aria-labelledby={id} className={`gm-dialog ${className}`} onCancel={(e) => { e.preventDefault(); onClose(); }} onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}>
    <div className="gm-dialog-inner"><header className="gm-section-heading"><h2 id={id}>{title}</h2><button type="button" className="gm-icon-button" aria-label="Close dialog" onClick={onClose}><X size={22} /></button></header>{children}</div>
  </dialog>;
}

export function SectionHeading({ eyebrow, title, children }) {
  return <div className="gm-section-heading"><div>{eyebrow && <span className="gm-eyebrow">{eyebrow}</span>}<h2>{title}</h2></div>{children}</div>;
}
export function PageHeading({ eyebrow, title, description, children }) {
  return <header className="gm-page-heading"><div><span className="gm-eyebrow">{eyebrow}</span><h1>{title}</h1>{description && <p>{description}</p>}</div>{children}</header>;
}
export function EmptyState({ title, description, children }) {
  return <div className="gm-empty gm-glass"><Gem size={40} strokeWidth={1.2} /><h2>{title}</h2><p>{description}</p>{children}</div>;
}
export function SkeletonGrid({ count = 4 }) {
  return <div className="gm-gem-grid" aria-label="Loading gemstones" aria-busy="true">{Array.from({ length: count }, (_, i) => <div key={i} className="gm-skeleton-card" aria-hidden="true"><div /><span /><span /></div>)}</div>;
}
export function SearchField({ value, onChange, placeholder, label }) {
  return <div className="gm-search"><Search size={19} aria-hidden="true" /><input type="search" aria-label={label} value={value} onChange={onChange} placeholder={placeholder} />{value && <button className="gm-icon-button" type="button" aria-label="Clear search" onClick={() => onChange({ target: { value: "" } })}><X size={18} /></button>}</div>;
}
export function BuyerGuide() {
  return <section className="gm-guide" aria-label="How buying works">
    <SectionHeading eyebrow="A little knowledge. A confident choice." title="Your journey to a Ceylon gem" />
    <div className="gm-guide-grid">{[
      [Search, "01", "Discover & compare", "Explore carat weight, colour and cut. Find a gemstone that feels right for you."],
      [ShieldCheck, "02", "Look at the details", "Read the listing and certificate reference. Review the seller and the available evidence."],
      [Truck, "03", "Follow your purchase", "Request an order, wait for seller confirmation, then pay and follow delivery in My Orders."],
    ].map(([Icon, step, title, copy]) => <article className="gm-glass" key={step}><div className="gm-guide-top"><Icon size={24} strokeWidth={1.4} /><span>{step}</span></div><h3>{title}</h3><p>{copy}</p></article>)}</div>
  </section>;
}
export function BuyingNotes() {
  return <section className="gm-notes gm-glass"><SectionHeading eyebrow="Before you choose" title="Know your gemstone" />
    {[['What should I compare?', 'Start with gem type, carat weight, colour, clarity and cut. Compare the listed price alongside these details, rather than size alone.'], ['What does a certificate tell me?', 'Review the certificate authority and reference provided in the listing. A certificate reference is not a substitute for reading the actual report and its findings.'], ['When do I pay?', 'Place an order request with your delivery details first. Payment becomes available in My Orders after the seller confirms your request.']].map(([q, a]) => <details key={q}><summary>{q}</summary><p>{a}</p></details>)}
    <Link to="/buyer/orders" className="gm-text-link">Visit My Orders <ArrowRight size={17} /></Link>
  </section>;
}
