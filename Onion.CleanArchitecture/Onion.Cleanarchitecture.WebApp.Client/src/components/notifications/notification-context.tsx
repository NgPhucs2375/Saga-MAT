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

  // Ref lưu danh sách timeout ID theo OrderId để xử lý debounce
  const invalidateTimersRef = useRef<Record<string, number>>({});

  useEffect(() => {
    if (!userId) return;

    signalRService.start(userId);

    const unsubscribe = signalRService.onNotification((noti) => {
      // 1. Giữ nguyên logic append danh sách thông báo
      setNotifications((prev) => [
        { ...noti, id: nextId(), read: false },
        ...prev,
      ]);

      // 2. Giữ nguyên logic toast
      notificationRef.current.info({
        message: noti.Title,
        description: noti.Message,
        placement: "topRight",
      });

      // 3. Debounce invalidate khi có orderId
      if (noti.OrderId) {
        const orderId = noti.OrderId;

        if (invalidateTimersRef.current[orderId]) {
          clearTimeout(invalidateTimersRef.current[orderId]);
        }

        invalidateTimersRef.current[orderId] = setTimeout(() => {
          invalidate({
            resource: "orders",
            invalidates: ["list", "many"],
          });
          invalidate({
            resource: "orders",
            id: orderId,
            invalidates: ["detail"],
          });
          delete invalidateTimersRef.current[orderId];
        }, INVALIDATE_DEBOUNCE_MS);
      }
    });

    return () => {
      // Clean up các timer debounce còn tồn tại khi unmount/đổi user
      Object.values(invalidateTimersRef.current).forEach(clearTimeout);
      invalidateTimersRef.current = {};

      unsubscribe();
      signalRService.stop();
    };
  }, [userId, invalidate]);

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