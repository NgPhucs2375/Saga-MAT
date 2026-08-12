import * as signalR from "@microsoft/signalr";

const HUB_URL = "/hubs/notification";

export interface NotificationPayload {
  TargetUserId: string;
  Title: string;
  Message: string;
  NotificationType: string;
  OrderId?: string;
  Timestamp: string;
}

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private listeners: Set<(noti: NotificationPayload) => void> = new Set();
  private userId: string | null = null;

  async start(userId: string): Promise<void> {
    if (
      this.connection &&
      (this.connection.state === signalR.HubConnectionState.Connected ||
        this.connection.state === signalR.HubConnectionState.Connecting)
    ) {
      return;
    }

    this.userId = userId;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Lắng nghe event từ server
    this.connection.on("ReceiveNotification", (noti: NotificationPayload) => {
      this.listeners.forEach((cb) => cb(noti));
    });

    // Sau mỗi lần tự reconnect (kết nối rơi do idle/timeout), đăng ký lại vào group user
    this.connection.onreconnected(async () => {
      if (this.userId) {
        await this.connection?.invoke("Subscribe", this.userId);
      }
    });

    await this.connection.start();
    // Subscribe vào group của user
    await this.connection.invoke("Subscribe", userId);
  }

  onNotification(callback: (noti: NotificationPayload) => void): () => void {
    this.listeners.add(callback);
    return () => this.listeners.delete(callback);
  }

  async stop(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
    }
  }
}

export const signalRService = new SignalRService();