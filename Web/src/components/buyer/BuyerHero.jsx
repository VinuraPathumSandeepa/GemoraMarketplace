import { useEffect, useRef, useState } from "react";
import { ArrowRight, Gem, ShieldCheck } from "lucide-react";
import { Link } from "react-router-dom";

export default function BuyerHero() {
  const video = useRef(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    const media = video.current;
    if (!media) return;
    let visible = true;
    let disposed = false;
    const shouldPlay = () => !disposed && visible && !document.hidden;
    const syncPlayback = () => {
      if (!shouldPlay()) media.pause();
      else {
        const attempt = media.play();
        attempt?.then(() => {
          if (!shouldPlay()) media.pause();
        }).catch(() => {});
      }
    };
    const observer = typeof IntersectionObserver === "undefined" ? null : new IntersectionObserver(([entry]) => {
      visible = entry.isIntersecting;
      syncPlayback();
    }, { threshold: 0.15 });
    if (observer) {
      visible = false;
      observer.observe(media);
    } else syncPlayback();
    document.addEventListener("visibilitychange", syncPlayback);
    return () => {
      disposed = true;
      observer?.disconnect();
      document.removeEventListener("visibilitychange", syncPlayback);
      media.pause();
    };
  }, [failed]);

  return (
    <section className="gm-dashboard-hero gm-video-hero">
      {!failed && (
        <video
          ref={video}
          className="gm-hero-video"
          src="/videos/buyerside.mp4"
          poster="/images/gems/hero-poster.webp"
          autoPlay
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
        <span className="gm-eyebrow">THE GEMORA COLLECTION · SRI LANKA</span>
        <h1>Discover a gem<br /><em>worth keeping.</em></h1>
        <p>Explore thoughtfully selected Ceylon gemstones, understand the details, and choose with confidence.</p>
        <div className="gm-hero-actions">
          <Link className="gm-button" to="/buyer/marketplace">Explore the collection <ArrowRight size={18} /></Link>
          <Link className="gm-hero-secondary" to="/buyer/orders">View my orders</Link>
        </div>
        <div className="gm-hero-trust">
          <span><Gem size={16} />Ceylon gemstones</span>
          <span><ShieldCheck size={16} />Details before decisions</span>
        </div>
      </div>
      <div className="gm-hero-caption"><span>THE GEMORA EDIT</span><p>Distinctive by nature.<br />Chosen by you.</p></div>
    </section>
  );
}
