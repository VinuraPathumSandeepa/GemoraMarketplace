import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { notificationDestination, useNotifications } from "../context/NotificationContext";
import "./notifications.css";

export function NotificationItem({ item, onOpen }) {
  const icon = {
    GemSubmitted: "↗", GemApproved: "✓", GemChangesRequested: "✎", GemRejected: "×",
    GemVerificationRequested: "◇",
  }[item.type] || "•";
  const date = new Date(item.createdAt);
  const time = Number.isNaN(date.getTime()) ? "" : date.toLocaleString(undefined, {
    dateStyle: "medium", timeStyle: "short",
  });
  return <button type="button" className={`gemora-notification-item ${item.isRead ? "" : "unread"}`}
    onClick={() => onOpen(item)}>
    <span className="gemora-notification-icon" aria-hidden="true">{icon}</span>
    <span className="gemora-notification-copy">
      <strong>{item.title}</strong><span>{item.message}</span><small>{time}</small>
    </span>
    {!item.isRead && <span className="gemora-notification-dot" aria-label="Unread" />}
  </button>;
}

export default function NotificationBell() {
  const [open, setOpen] = useState(false);
  const root = useRef(null);
  const navigate = useNavigate();
  const { user } = useAuth();
  const { items, unreadCount, loading, working, error, actionError, refresh, markRead, markAllRead } = useNotifications();

  useEffect(() => {
    if (!open) return;
    const onPointer = (event) => {
      if (!root.current?.contains(event.target)) setOpen(false);
    };
    const onKey = (event) => { if (event.key === "Escape") setOpen(false); };
    document.addEventListener("pointerdown", onPointer);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("pointerdown", onPointer);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  const openItem = async (item) => {
    if (!await markRead(item)) return;
    const destination = notificationDestination(item, user?.role);
    if (destination) navigate(destination);
    setOpen(false);
  };

  return <div className="gemora-notification-root" ref={root}>
    <button type="button" className="gemora-notification-bell"
      title="Notifications" aria-label={`Notifications${unreadCount ? `, ${unreadCount} unread` : ""}`}
      aria-expanded={open} aria-haspopup="dialog"
      onClick={() => { setOpen((value) => !value); if (!open) refresh(); }}>
      <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor"
        strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
        <path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4" />
      </svg>
      {unreadCount > 0 && <span className="gemora-notification-badge">{unreadCount > 99 ? "99+" : unreadCount}</span>}
    </button>
    {open && <section className="gemora-notification-popover" role="dialog" aria-label="Notifications">
      <div className="gemora-notification-head"><strong>Notifications</strong>
        <button type="button" disabled={!unreadCount || working} onClick={markAllRead}>Mark all read</button>
      </div>
      <div className="gemora-notification-list">
        {actionError && <p className="gemora-notification-action-error" role="alert">{actionError}</p>}
        {loading ? <p className="gemora-notification-state">Loading notifications…</p> :
          error ? <div className="gemora-notification-state"><p>{error}</p><button onClick={refresh}>Retry</button></div> :
          items.length ? items.slice(0, 5).map((item) => <NotificationItem key={item.id} item={item} onOpen={openItem} />) :
          <p className="gemora-notification-state">You’re all caught up.</p>}
      </div>
      <button type="button" className="gemora-notification-view-all"
        onClick={() => { setOpen(false); navigate(`/${user.role.toLowerCase()}/notifications`); }}>
        View all notifications <span aria-hidden="true">→</span>
      </button>
    </section>}
  </div>;
}
