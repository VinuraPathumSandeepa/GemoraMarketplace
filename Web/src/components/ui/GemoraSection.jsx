function GemoraSection({
  eyebrow,
  title,
  description,
  action,
  children,
  variant = "default",
  className = "",
}) {
  return (
    <section
      className={`gemora-section gemora-section-${variant} ${className}`}
    >
      <div className="gemora-section-header">

        <div className="gemora-section-heading">

          {eyebrow && (
            <span className="gemora-section-eyebrow">
              {eyebrow}
            </span>
          )}

          {title && (
            <h2>
              {title}
            </h2>
          )}

          {description && (
            <p>
              {description}
            </p>
          )}

        </div>

        {action && (
          <div className="gemora-section-action">
            {action}
          </div>
        )}

      </div>

      <div className="gemora-section-content">
        {children}
      </div>

    </section>
  );
}

export default GemoraSection;