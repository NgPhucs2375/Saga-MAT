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

  const [notifications, setNotifications] = useState<StoredNotification[]>([]);

  const { notification } = AntdApp.useApp();

  const notificationRef = useRef(notification);
  notificationRef.current = notification;

  const invalidateRef = useRef(invalidate);
  invalidateRef.current = invalidate;

  // Ref lưu danh sách timeout ID theo OrderId để xử lý debounce
  const invalidateTimersRef = useRef<Record<string, number>>({});

  // Bộ đệm gộp nhiều notification trong cửa sổ ngắn thành 1 state update
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
        // Ghép batch mới lên đầu, loại trùng (cùng OrderId + Timestamp),
        // giới hạn danh sách để không phình vô hạn
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
      // Toast có throttle theo OrderId/Timestamp để không spam re-render
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

      // Debounce invalidate khi có orderId
      if (noti.OrderId) {
        const orderId = noti.OrderId;

        if (invalidateTimersRef.current[orderId]) {
          clearTimeout(invalidateTimersRef.current[orderId]);
        }

        invalidateTimersRef.current[orderId] = setTimeout(() => {
          invalidateRef.current({
            resource: "orders",
            invalidates: ["list", "many"],
          });
          invalidateRef.current({
            resource: "orders",
            id: orderId,
            invalidates: ["detail"],
          });
          delete invalidateTimersRef.current[orderId];
        }, INVALIDATE_DEBOUNCE_MS);
      }
    });

    return () => {
      // Clean up các timer còn tồn tại khi unmount/đổi user
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