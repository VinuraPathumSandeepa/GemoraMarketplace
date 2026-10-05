import { motion }
  from "framer-motion";

export default function BuyerStatCard({
  title,
  value,
  icon,
  subtitle,
  loading,
  index,
}) {
  return (
    <motion.article
      className="buyer-stat-card liquid-card"
      initial={{
        opacity: 0,
        y: 45,
      }}
      whileInView={{
        opacity: 1,
        y: 0,
      }}
      viewport={{
        once: true,
      }}
      transition={{
        duration: 0.55,
        delay: index * 0.08,
      }}
      whileHover={{
        y: -10,
        scale: 1.015,
      }}
    >
      <div className="buyer-stat-icon">
        {icon}
      </div>

      <h3>
        {loading ? "—" : value}
      </h3>

      <h4>{title}</h4>

      <p>{subtitle}</p>
    </motion.article>
  );
}