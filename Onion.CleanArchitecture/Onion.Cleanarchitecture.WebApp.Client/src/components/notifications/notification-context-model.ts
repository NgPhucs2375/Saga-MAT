import { createContext, useContext } from "react";
import type { NotificationPayload } from "@providers/signalr-provider";

export interface StoredNotification extends NotificationPayload {
  id: string;
  read: boolean;
}

export interface NotificationsContextValue {
  notifications: StoredNotification[];
  unreadCount: number;
  markRead: (id: string) => void;
  markAllRead: () => void;
  clear: () => void;
}

export const NotificationsContext =
  createContext<NotificationsContextValue>({
    notifications: [],
    unreadCount: 0,
    markRead: () => {},
    markAllRead: () => {},
    clear: () => {},
  });

export const useNotifications = () => useContext(NotificationsContext);
