import { useCallback, useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { Bell, Check, Clock3, RefreshCw, X } from "lucide-react";
import { orderInboxRequest } from "../services/buyerApi";
import { utcTime } from "../utils/orderTime";
import { latestOrderMessage } from "../utils/orderMessages";
import "../styles/order-inbox.css";

export function PaymentDeadline({ dueAt, onExpire }) {
  const [now, setNow] = useState(() => Date.now());
  const fired = useRef(false);
  useEffect(() => {
    fired.current = false;
    const timer = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, [dueAt]);
  const remaining = Math.max(0, Math.ceil((utcTime(dueAt) - now) / 1000));
  useEffect(() => {
    if (remaining === 0 && !fired.current) { fired.current = true; onExpire?.(); }
  }, [remaining, onExpire]);
  if (!dueAt) return null;
  const hours = Math.floor(remaining / 3600);
  const minutes = Math.floor(remaining % 3600 / 60);
  return <p className="oi-deadline"><Clock3 size={16} aria-hidden="true" />{remaining > 0
    ? `Pay within ${hours}h ${minutes}m ${remaining % 60}s · Deadline ${new Date(utcTime(dueAt)).toLocaleString()}`
    : "Payment window expired. This order can no longer be paid."}</p>;
}

const errorMessage = error => error.message || "Unable to update the order. Please try again.";
const dateLabel = value => new Date(utcTime(value)).toLocaleString();

export default function OrderInbox({ seller = false }) {
  const [now, setNow] = useState(() => Date.now());
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(null);
  const [decision, setDecision] = useState(null);
  const [reason, setReason] = useState("");
  const [all, setAll] = useState(false);
  const dialog = useRef(null);
  const endpoint = seller ? "/api/seller/orders" : "/api/orders";
  const load = useCallback(async () => {
    try {
      const result = await orderInboxRequest(seller ? endpoint : `${endpoint}/my`);
      setOrders(Array.isArray(result) ? result : []);
      setNow(Date.now());
      setError("");
    } catch (err) { setError(errorMessage(err)); }
    finally { setLoading(false); }
  }, [endpoint, seller]);
  useEffect(() => {
    const initial = setTimeout(load, 0);
    const timer = setInterval(load, 15000);
    window.addEventListener("gemora-orders-changed", load);
    return () => { clearTimeout(initial); clearInterval(timer); window.removeEventListener("gemora-orders-changed", load); };
  }, [load]);
  const unread = order => seller
    ? order.status === "Pending" && !order.sellerReadAt
    : order.buyerMessageAt && (!order.buyerReadAt || utcTime(order.buyerReadAt) < utcTime(order.buyerMessageAt));
  const count = orders.filter(unread).length;
  const visible = orders.filter(order => seller ? all || order.status === "Pending" : order.buyerMessageAt)
    .sort((a, b) => utcTime(seller ? b.createdAt : b.buyerMessageAt) - utcTime(seller ? a.createdAt : a.buyerMessageAt));
  const close = () => { dialog.current?.close(); setDecision(null); setReason(""); };
  async function act(order, action) {
    setBusy(order.id); setError("");
    try {
      await orderInboxRequest(`${endpoint}/${order.id}/${action}`, {
        method: "POST",
        body: JSON.stringify(action === "message/read"
          ? { messageAt: seller ? order.createdAt : order.buyerMessageAt }
          : { reason: reason.trim(), alreadySold: decision?.type === "sold" }),
      });
      setDecision(null); setReason("");
      await load();
      window.dispatchEvent(new Event("gemora-orders-changed"));
    } catch (err) { setError(errorMessage(err)); }
    finally { setBusy(null); }
  }
  return <>
    <button type="button" className="oi-trigger" onClick={() => dialog.current?.showModal()} aria-label={`${seller ? "Orders" : "Messages"}, ${count} unread`}>
      <Bell size={18} aria-hidden="true" /><span>{seller ? "Orders" : "Messages"}</span>
      {count > 0 && <strong className="oi-badge">{count > 99 ? "99+" : count}</strong>}
    </button>
    <dialog className="oi-dialog" ref={dialog} onCancel={() => { setDecision(null); setReason(""); }} aria-labelledby={`oi-title-${seller ? "seller" : "buyer"}`}>
      <header className="oi-header"><div><small>GEMORA INBOX</small><h2 id={`oi-title-${seller ? "seller" : "buyer"}`}>{seller ? "Order requests" : "Your messages"}</h2><p>{count} new {seller ? "orders" : "messages"}</p></div><button type="button" onClick={close} aria-label="Close inbox"><X size={22} /></button></header>
      <div className="oi-toolbar">{seller && <button type="button" onClick={() => setAll(!all)}>{all ? "Show pending requests" : "Show all orders"}</button>}<button type="button" onClick={load}><RefreshCw size={15} />Refresh</button></div>
      {error && <p role="alert" className="oi-error">{error}</p>}
      <div className="oi-list">
        {loading && <p role="status">Loading your inbox…</p>}
        {!loading && !error && visible.length === 0 && <p className="oi-empty">{seller ? "No pending requests. New buyer orders will appear here." : "No updates yet. Seller decisions and payment updates will appear here."}</p>}
        {visible.map(order => {
          const message = latestOrderMessage(order);
          const payable = ["Confirmed", "AwaitingPayment"].includes(order.status);
          return <article className={`oi-card ${unread(order) ? "oi-unread" : ""}`} key={order.id}>
            <div className="oi-card-heading"><h3>{order.gemTitle}</h3><span>{seller ? order.status : message.label}</span></div>
            <small>{order.orderNumber} · {seller ? `${order.buyerName} placed an order` : `Seller: ${order.sellerName}`}</small>
            <p className="oi-date">{dateLabel(seller ? order.createdAt : order.buyerMessageAt)}</p>
            <strong>{order.currency} {Number(order.agreedPrice).toLocaleString(undefined, { minimumFractionDigits: 2 })}</strong>
            {(!seller || order.status !== "Pending") && <p>{message.text}</p>}
            {payable && <PaymentDeadline dueAt={order.paymentDueAt} onExpire={load} />}
            <div className="oi-actions">
              {seller && order.status === "Pending" && <>
                <button disabled={busy !== null} onClick={() => { setDecision({ id: order.id, type: "approve" }); setReason(""); }}>Approve</button>
                <button disabled={busy !== null} onClick={() => { setDecision({ id: order.id, type: "reject" }); setReason(""); }}>Reject</button>
                <button disabled={busy !== null} onClick={() => { setDecision({ id: order.id, type: "sold" }); setReason(""); }}>Already sold</button>
              </>}
              {!seller && payable && utcTime(order.paymentDueAt) > now && <Link onClick={close} to={`/buyer/orders/${order.id}/payment`}>Pay now</Link>}
              {!seller && <Link onClick={close} to="/buyer/orders">View orders</Link>}
              {unread(order) && <button disabled={busy !== null} onClick={() => act(order, "message/read")}><Check size={14} />Mark read</button>}
            </div>
            {decision?.id === order.id && <form className="oi-decision" onSubmit={event => { event.preventDefault(); act(order, decision.type === "approve" ? "approve" : "reject"); }}>
              <p>{decision.type === "approve" ? "Approve this buyer? The gemstone will be reserved for 3 hours and all other pending requests will be rejected." : decision.type === "sold" ? "Mark this gemstone as sold? All pending requests will be rejected and the listing will leave the marketplace." : "Tell the buyer why you are rejecting this request. The gemstone will remain available."}</p>
              {decision.type === "reject" && <label>Rejection reason<textarea required maxLength={1000} value={reason} onChange={event => setReason(event.target.value)} placeholder="Explain your decision to the buyer" /></label>}
              <div className="oi-actions"><button disabled={busy !== null || (decision.type === "reject" && !reason.trim())} type="submit">{busy === order.id ? "Saving…" : "Confirm decision"}</button><button type="button" disabled={busy !== null} onClick={() => setDecision(null)}>Back</button></div>
            </form>}
          </article>;
        })}
      </div>
    </dialog>
  </>;
}
