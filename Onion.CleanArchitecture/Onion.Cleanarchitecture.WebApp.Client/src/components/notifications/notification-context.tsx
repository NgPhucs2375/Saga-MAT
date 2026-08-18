// components/notifications/notification-context.tsx
import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { App as AntdApp } from "antd";
import { useGetIdentity, useInvalidate } from "@refinedev/core";
import { signalRService } from "@providers/signalr-provider";
import type { IUserByMe } from "@routes/identity/users";
import { NotificationsContext } from "./notification-context-model";
import type { StoredNotification } from "./notification-context-model";

let seq = 0;
const nextId = () => `${Date.now()}-${++seq}`;

const INVALIDATE_DEBOUNCE_MS = 500;
const COALESCE_MS = 100;
const MAX_NOTIFICATIONS = 50;
const TOAST_THROTTLE_MS = 2000;

export const NotificationProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const { data: user } = useGetIdentity<IUserByMe>();
  const userId = user?.Uid;
  const invalidate = useInvalidate();
  const { notification } = AntdApp.useApp();

  const [notifications, setNotifications] = useState<StoredNotification[]>([]);

  const notificationRef = useRef(notification);
  notificationRef.current = notification;

  const invalidateRef = useRef(invalidate);
  invalidateRef.current = invalidate;

const invalidateTimersRef = useRef<Record<string, number>>({}); 
 const pendingNotificationsRef = useRef<StoredNotification[]>([]);
const flushTimerRef = useRef<number | null>(null);
  const lastToastAtRef = useRef<Record<string, number>>({});

  useEffect(() => {
    if (!userId) return;

    signalRService.start(userId);

    const flushPending = () => {
      flushTimerRef.current = null;
      if (pendingNotificationsRef.current.length === 0) return;

      const batch = pendingNotificationsRef.current;
      pendingNotificationsRef.current = [];

      setNotifications((prev) => {
        const merged = [...batch, ...prev];
        const seen = new Set<string>();
        const deduped = merged.filter((n) => {
          const key = `${n.OrderId ?? ""}-${n.Timestamp}`;
          if (seen.has(key)) return false;
          seen.add(key);
          return true;
        });
        return deduped.slice(0, MAX_NOTIFICATIONS);
      });
    };

    const scheduleFlush = () => {
      if (flushTimerRef.current != null) return;
      flushTimerRef.current = setTimeout(flushPending, COALESCE_MS);
    };

    const unsubscribe = signalRService.onNotification((noti) => {
      const toastKey = noti.OrderId ?? noti.Timestamp;
      const now = Date.now();
      if (
        !lastToastAtRef.current[toastKey] ||
        now - lastToastAtRef.current[toastKey] > TOAST_THROTTLE_MS
      ) {
        lastToastAtRef.current[toastKey] = now;
        notificationRef.current.info({
          message: noti.Title,
          description: noti.Message,
          placement: "topRight",
        });
      }

      pendingNotificationsRef.current.push({
        ...noti,
        id: nextId(),
        read: false,
      });
      scheduleFlush();

      if (noti.OrderId) {
        const orderId = noti.OrderId;
        if (invalidateTimersRef.current[orderId]) {
          clearTimeout(invalidateTimersRef.current[orderId]);
        }

        // Tự động làm mới dữ liệu Table khi nhận event từ SignalR
        invalidateTimersRef.current[orderId] = window.setTimeout(() => {
          invalidateRef.current({
            resource: "orders",
            invalidates: ["list", "many", "detail"],
          });
          delete invalidateTimersRef.current[orderId];
        }, INVALIDATE_DEBOUNCE_MS);
      }
    });

    return () => {
      Object.values(invalidateTimersRef.current).forEach(clearTimeout);
      invalidateTimersRef.current = {};

      if (flushTimerRef.current != null) {
        clearTimeout(flushTimerRef.current);
        flushTimerRef.current = null;
      }
      pendingNotificationsRef.current = [];
      lastToastAtRef.current = {};

      unsubscribe();
      signalRService.stop();
    };
  }, [userId]);

  const markRead = useCallback((id: string) => {
    setNotifications((prev) =>
      prev.map((n) => (n.id === id ? { ...n, read: true } : n))
    );
  }, []);

  const markAllRead = useCallback(() => {
    setNotifications((prev) => prev.map((n) => ({ ...n, read: true })));
  }, []);

  const clear = useCallback(() => setNotifications([]), []);

  const unreadCount = useMemo(
    () => notifications.filter((n) => !n.read).length,
    [notifications]
  );

  const value = useMemo(
    () => ({ notifications, unreadCount, markRead, markAllRead, clear }),
    [notifications, unreadCount, markRead, markAllRead, clear]
  );

  return (
    <NotificationsContext.Provider value={value}>
      {children}
    </NotificationsContext.Provider>
  );
};