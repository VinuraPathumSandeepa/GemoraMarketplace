import { Link }
  from "react-router-dom";

export default function BuyerOrdersPreview({
  orders,
  loading,
}) {
  const latest =
    [...(orders || [])]
      .slice(0, 3);

  return (
    <section className="section-block liquid-card">
      <div className="section-head">
        <div>
          <span className="section-mini-title">
            Recent Activity
          </span>

          <h3>
            Latest Orders
          </h3>
        </div>

        <Link
          to="/buyer/orders"
          className="ghost-btn"
        >
          View Orders
        </Link>
      </div>

      {loading ? (
        <div className="buyer-loading-block">
          Loading orders...
        </div>
      ) : latest.length === 0 ? (
        <div className="buyer-empty-state">
          <h4>No orders yet.</h4>

          <p>
            Your purchases will appear here.
          </p>
        </div>
      ) : (
        <div className="orders-preview-list">
          {latest.map((order) => (
            <div
              className="order-preview-item"
              key={order.id}
            >
              <div>
                <h4>
                  {order.gemTitle}
                </h4>

                <p>
                  #{order.orderNumber}
                </p>
              </div>

              <span
                className={`status-pill ${order.status?.toLowerCase()}`}
              >
                {order.status}
              </span>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}