import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { App as AntdApp } from "antd";
import { useGetIdentity } from "@refinedev/core";
import {
  signalRService,
  NotificationPayload,
} from "@providers/signalr-provider";
import type { IUserByMe } from "@routes/identity/users";

export interface StoredNotification extends NotificationPayload {
  id: string;
  read: boolean;
}

interface NotificationsContextValue {
  notifications: StoredNotification[];
  unreadCount: number;
  markRead: (id: string) => void;
  markAllRead: () => void;
  clear: () => void;
}

const NotificationsContext = createContext<NotificationsContextValue>({
  notifications: [],
  unreadCount: 0,
  markRead: () => {},
  markAllRead: () => {},
  clear: () => {},
});

export const useNotifications = () => useContext(NotificationsContext);

let seq = 0;
const nextId = () => `${Date.now()}-${++seq}`;

export const NotificationProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const { data: user } = useGetIdentity<IUserByMe>();
  const userId = user?.Uid;

  const [notifications, setNotifications] = useState<StoredNotification[]>([]);

  // Dùng auth-notification từ context AntdApp thay vì hàm static `notification`,
  // để tránh cảnh báo "Static function can not consume context like dynamic theme"
  // và để toast tuân theo theme động.
  const { notification } = AntdApp.useApp();

  // Giữ ref để effect không bị chạy lại mỗi khi `notification` đổi tham chiếu
  const notificationRef = useRef(notification);
  notificationRef.current = notification;

  useEffect(() => {
    if (!userId) return;

    signalRService.start(userId);

    const unsubscribe = signalRService.onNotification((noti) => {
      setNotifications((prev) => [
        { ...noti, id: nextId(), read: false },
        ...prev,
      ]);
      notificationRef.current.info({
        message: noti.Title,
        description: noti.Message,
        placement: "topRight",
      });
    });

    return () => {
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