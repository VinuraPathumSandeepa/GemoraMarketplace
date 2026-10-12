import { useState } from "react";
import { useNavigate } from "react-router-dom";
import DashboardLayout from "../layouts/DashboardLayout";
import { useAuth } from "../context/AuthContext";
import { notificationDestination, useNotifications } from "../context/NotificationContext";
import { NotificationItem } from "../components/NotificationBell";
import "../components/notifications.css";

function NotificationsContent() {
  const [unreadOnly, setUnreadOnly] = useState(false);
  const navigate = useNavigate();
  const { user } = useAuth();
  const { items, unreadCount, loading, working, error, actionError, refresh, markRead, markAllRead } = useNotifications();
  const visible = unreadOnly ? items.filter((item) => !item.isRead) : items;

  const openItem = async (item) => {
    if (!await markRead(item)) return;
    const destination = notificationDestination(item, user?.role);
    if (destination) navigate(destination);
  };

  return <div className="gemora-notifications-page">
    <div className="gemora-notifications-title">
      <div><span>YOUR WORKSPACE</span><h1>Notifications</h1>
        <p>{unreadCount ? `${unreadCount} unread update${unreadCount === 1 ? "" : "s"}` : "You’re all caught up."}</p></div>
      <div className="gemora-notifications-actions">
        <button type="button" onClick={refresh}>Refresh</button>
        <button type="button" disabled={!unreadCount || working} onClick={markAllRead}>Mark all as read</button>
      </div>
    </div>
    <div className="gemora-notification-filters" aria-label="Filter notifications">
      <button type="button" className={!unreadOnly ? "active" : ""} onClick={() => setUnreadOnly(false)}>All</button>
      <button type="button" className={unreadOnly ? "active" : ""} onClick={() => setUnreadOnly(true)}>Unread ({unreadCount})</button>
    </div>
    <div className="gemora-notifications-panel">
      {actionError && <p className="gemora-notification-action-error" role="alert">{actionError}</p>}
      {loading ? <p className="gemora-notification-state">Loading notifications…</p> :
        error ? <div className="gemora-notification-state"><p>{error}</p><button onClick={refresh}>Try again</button></div> :
        visible.length ? visible.map((item) => <NotificationItem key={item.id} item={item} onOpen={openItem} />) :
        <p className="gemora-notification-state">{unreadOnly ? "No unread notifications." : "Important activity will appear here."}</p>}
    </div>
  </div>;
}

export default function NotificationsPage() {
  return <DashboardLayout><NotificationsContent /></DashboardLayout>;
}
