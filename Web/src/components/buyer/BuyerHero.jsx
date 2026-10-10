import { useEffect, useRef, useState } from "react";
import { ArrowRight, Gem, ShieldCheck } from "lucide-react";
import { Link } from "react-router-dom";

export default function BuyerHero() {
  const video = useRef(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    const media = video.current;
    if (!media) return;
    const preference = window.matchMedia("(prefers-reduced-motion: reduce)");
    let visible = false;
    let disposed = false;
    const shouldPlay = () => !disposed && visible && !document.hidden && !preference.matches;
    const syncPlayback = () => {
      if (!shouldPlay()) media.pause();
      else media.play().then(() => {
        if (!shouldPlay()) media.pause();
      }).catch(() => { /* Keep the poster when autoplay is blocked. */ });
    };
    const observer = new IntersectionObserver(([entry]) => {
      visible = entry.isIntersecting;
      syncPlayback();
    });
    observer.observe(media);
    preference.addEventListener("change", syncPlayback);
    document.addEventListener("visibilitychange", syncPlayback);
    return () => {
      disposed = true;
      observer.disconnect();
      preference.removeEventListener("change", syncPlayback);
      document.removeEventListener("visibilitychange", syncPlayback);
      media.pause();
    };
  }, []);

  return (
    <section className="gm-dashboard-hero gm-video-hero">
      {!failed && (
        <video
          ref={video}
          className="gm-hero-video"
          src="/videos/buyerside.mp4"
          poster="/images/gems/hero-poster.webp"
          muted
          loop
          playsInline
          preload="metadata"
          aria-hidden="true"
          tabIndex={-1}
          onError={() => setFailed(true)}
        />
      )}
      <div className="gm-hero-video-tint" aria-hidden="true" />
      <div className="gm-hero-copy">
        <span className="gm-eyebrow">FROM THE HEART OF CEYLON</span>
        <h1>Extraordinary gems.<br /><em>Meaningful discoveries.</em></h1>
        <p>A place to discover Sri Lankan gemstones, understand their story, and find a stone to call your own.</p>
        <div className="gm-hero-actions">
          <Link className="gm-button" to="/buyer/marketplace">Explore the collection <ArrowRight size={18} /></Link>
          <Link className="gm-hero-secondary" to="/buyer/orders">View my orders</Link>
        </div>
        <div className="gm-hero-trust">
          <span><Gem size={16} />Ceylon gemstones</span>
          <span><ShieldCheck size={16} />Details before decisions</span>
        </div>
      </div>
      <div className="gm-hero-art">
        <div className="gm-hero-caption"><span>THE GEMORA EDIT</span><p>Nature's finest.<br />Yours to discover.</p></div>
      </div>
    </section>
  );
}
