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
  private startPromise: Promise<void> | null = null;

  async start(userId: string): Promise<void> {
    // Đã kết nối đúng user -> không làm gì thêm
    if (
      this.userId === userId &&
      this.connection &&
      (this.connection.state === signalR.HubConnectionState.Connected ||
        this.connection.state === signalR.HubConnectionState.Connecting)
    ) {
      return this.startPromise ?? Promise.resolve();
    }

    // Đang có một start chạy dở -> chờ xong rồi đánh giá lại
    if (this.startPromise) {
      await this.startPromise;
      if (this.userId === userId) {
        return;
      }
    }

    this.userId = userId;
    this.startPromise = this.createConnection(userId);
    try {
      await this.startPromise;
    } finally {
      this.startPromise = null;
    }
  }

  private async createConnection(userId: string): Promise<void> {
    try {
      // Đóng connection cũ (nếu có) trước khi tạo connection mới
      if (this.connection) {
        const oldConnection = this.connection;
        this.connection = null;
        oldConnection.off("ReceiveNotification");
        oldConnection.onreconnected = null;
        try {
          await oldConnection.stop();
        } catch {
          // Bỏ qua nếu connection cũ chưa sẵn sàng stop (vd đang Connecting)
        }
      }

      const connection = new signalR.HubConnectionBuilder()
        .withUrl(HUB_URL)
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

      connection.on("ReceiveNotification", (noti: NotificationPayload) => {
        this.listeners.forEach((cb) => cb(noti));
      });

      // Sau mỗi lần tự reconnect, đăng ký lại vào group user
      connection.onreconnected = async () => {
        if (this.userId) {
          try {
            await connection.invoke("Subscribe", this.userId);
          } catch {
            // Reconnect xảy ra trong lúc stop/unmount -> bỏ qua
          }
        }
      };

      this.connection = connection;
      await connection.start();
      await connection.invoke("Subscribe", userId);
    } catch (error) {
      // Start thất bại -> dọn dẹp để lần sau thử lại được
      if (
        this.connection?.state === signalR.HubConnectionState.Disconnected ||
        this.connection?.state === signalR.HubConnectionState.Disconnecting
      ) {
        this.connection = null;
      }
      throw error;
    }
  }

  onNotification(callback: (noti: NotificationPayload) => void): () => void {
    this.listeners.add(callback);
    return () => this.listeners.delete(callback);
  }

  async stop(): Promise<void> {
    this.userId = null;
    this.startPromise = null;

    const connection = this.connection;
    this.connection = null;
    if (!connection) {
      return;
    }

    connection.off("ReceiveNotification");
    connection.onreconnected = null;

    try {
      if (
        connection.state === signalR.HubConnectionState.Connected ||
        connection.state === signalR.HubConnectionState.Connecting
      ) {
        await connection.stop();
      }
    } catch {
      // Bỏ qua nếu connection đã đóng hoặc đang ở trạng thái không stop được
    }
  }
}

export const signalRService = new SignalRService();
