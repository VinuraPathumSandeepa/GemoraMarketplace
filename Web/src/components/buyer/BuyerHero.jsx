import { motion } from "framer-motion";
import {
  ArrowRight,
  Gem,
  ShieldCheck,
  Sparkles,
} from "lucide-react";
import { Link } from "react-router-dom";

export default function BuyerHero() {
  return (
    <section className="buyer-hero">
      <video
        className="buyer-hero-video"
        autoPlay
        muted
        loop
        playsInline
        preload="metadata"
      >
        <source
          src="/videos/gemora-hero.mp4"
          type="video/mp4"
        />
      </video>

      
      <div className="buyer-hero-video-tint" />

      <div className="hero-light hero-light-one" />
      <div className="hero-light hero-light-two" />

      <motion.div
        className="buyer-hero-glass-panel"
        initial={{
          opacity: 0,
          y: 60,
          scale: 0.94,
        }}
        animate={{
          opacity: 1,
          y: 0,
          scale: 1,
        }}
        transition={{
          duration: 0.9,
          ease: [0.16, 1, 0.3, 1],
        }}
      >
        <motion.div
          className="hero-badge"
          initial={{
            opacity: 0,
            x: -30,
          }}
          animate={{
            opacity: 1,
            x: 0,
          }}
          transition={{
            delay: 0.25,
            duration: 0.65,
          }}
        >
          <Sparkles size={16} />
          Ceylon's Premium Digital Gem Marketplace
        </motion.div>

        <motion.h1
          initial={{
            opacity: 0,
            y: 30,
          }}
          animate={{
            opacity: 1,
            y: 0,
          }}
          transition={{
            delay: 0.32,
            duration: 0.75,
          }}
        >
          Discover rare,
          <span> verified gemstones.</span>
        </motion.h1>

        <motion.p
          initial={{
            opacity: 0,
            y: 25,
          }}
          animate={{
            opacity: 1,
            y: 0,
          }}
          transition={{
            delay: 0.42,
            duration: 0.75,
          }}
        >
          Explore trusted Sri Lankan gemstone listings,
          review verified specifications and purchase
          securely through Gemora.
        </motion.p>

        <motion.div
          className="hero-trust-row"
          initial={{
            opacity: 0,
          }}
          animate={{
            opacity: 1,
          }}
          transition={{
            delay: 0.58,
            duration: 0.7,
          }}
        >
          <span>
            <ShieldCheck size={17} />
            Verified sellers
          </span>

          <span>
            <Gem size={17} />
            Certified gemstones
          </span>
        </motion.div>

        <motion.div
          className="hero-actions"
          initial={{
            opacity: 0,
            y: 25,
          }}
          animate={{
            opacity: 1,
            y: 0,
          }}
          transition={{
            delay: 0.68,
            duration: 0.65,
          }}
        >
          <Link
            to="/buyer/marketplace"
            className="hero-btn hero-btn-primary"
          >
            Explore Marketplace
            <ArrowRight size={18} />
          </Link>

          <Link
            to="/buyer/orders"
            className="hero-btn hero-btn-glass"
          >
            View My Orders
          </Link>
        </motion.div>
      </motion.div>
    </section>
  );
}