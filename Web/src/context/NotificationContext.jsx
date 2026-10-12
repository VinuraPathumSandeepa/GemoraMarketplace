import { createContext, useCallback, useContext, useEffect, useState } from "react";
import { notificationError, notificationService } from "../services/notificationService";

const NotificationContext = createContext(null);

export function NotificationProvider({ children }) {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [working, setWorking] = useState(false);
  const [error, setError] = useState("");
  const [actionError, setActionError] = useState("");

  const refresh = useCallback(async () => {
    setLoading(true);
    setError("");
    setActionError("");
    try {
      setItems(await notificationService.list());
    } catch (failure) {
      setError(notificationError(failure));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    refresh();
    const onVisible = () => {
      if (document.visibilityState === "visible") refresh();
    };
    document.addEventListener("visibilitychange", onVisible);
    return () => document.removeEventListener("visibilitychange", onVisible);
  }, [refresh]);

  const markRead = async (item) => {
    if (item.isRead) return true;
    setActionError("");
    try {
      await notificationService.markRead(item.id);
      setItems((current) => current.map((entry) =>
        entry.id === item.id ? { ...entry, isRead: true } : entry));
      return true;
    } catch (failure) {
      setActionError(notificationError(failure));
      return false;
    }
  };

  const markAllRead = async () => {
    if (working) return false;
    setActionError("");
    setWorking(true);
    try {
      await notificationService.markAllRead();
      setItems((current) => current.map((entry) => ({ ...entry, isRead: true })));
      return true;
    } catch (failure) {
      setActionError(notificationError(failure));
      return false;
    } finally {
      setWorking(false);
    }
  };

  return <NotificationContext.Provider value={{
    items, loading, working, error, actionError, refresh, markRead, markAllRead,
    unreadCount: items.filter((item) => !item.isRead).length,
  }}>{children}</NotificationContext.Provider>;
}

export function useNotifications() {
  return useContext(NotificationContext);
}

export function notificationDestination(item, role) {
  const id = Number(item.relatedEntityId);
  if (role === "Seller" && item.relatedEntityType === "GemListing" &&
      Number.isSafeInteger(id) && id > 0) return `/seller/listings/${id}`;
  if (role === "Gemologist" && item.relatedEntityType === "GemVerification" &&
      Number.isSafeInteger(id) && id > 0) return `/gemologist/verifications/${id}`;
  if (role === "Gemologist" && item.type === "GemVerificationRequested")
    return "/gemologist/verifications";
  return null;
}
