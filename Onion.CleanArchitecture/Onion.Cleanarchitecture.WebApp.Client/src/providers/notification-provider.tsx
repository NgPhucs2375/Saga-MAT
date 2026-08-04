import { notification } from "antd";
import {
  
  CloseCircleOutlined,
  InfoCircleOutlined,
  LoadingOutlined,
  CheckCircleOutlined,
} from "@ant-design/icons";
import { NotificationProvider, OpenNotificationParams } from "@refinedev/core";
import { ArgsProps } from "antd/es/notification";

// Định nghĩa một kiểu tùy chỉnh mở rộng OpenNotificationParams để bao gồm 'meta'
interface CustomOpenNotificationParams extends OpenNotificationParams {
  meta?: {
    resource?: string;
    id?: string;
    view?: 'show' | 'edit';
  };
}

// Hàm helper để tạo config chung cho các notification
const createNotificationConfig = (
  { key, message, description, meta }: CustomOpenNotificationParams,
  icon: React.ReactNode,
  color: string
): ArgsProps => {
  const { resource, id, view } = meta ?? {};
  const isClickable = resource && id;

  return {
    key,
    message,
    description,
    icon: <span style={{ color }}>{icon}</span>,
    placement: "topRight",
    style: {
      cursor: isClickable ? "pointer" : "default",
    },
    onClick: () => {
      if (isClickable) {
        // Điều hướng đến trang chi tiết của resource.
        // Lưu ý: window.location.href sẽ reload toàn bộ trang.
        // Trong một ứng dụng SPA thực tế, bạn nên dùng history.push() hoặc navigate() từ react-router-dom.
        // Để làm được điều đó, bạn cần truyền instance của history/navigate vào provider này khi khởi tạo.
        const path = view === 'edit' ? `/${resource}/edit/${id}` : `/${resource}/show/${id}`;
        window.location.href = path;
      }
    },
  };
};

export const notificationProvider: NotificationProvider = {
  open: (args: OpenNotificationParams) => { // args vẫn là OpenNotificationParams theo interface của Refine
    const { type } = args;

    switch (type) {
      case "success":
        notification.success(
          createNotificationConfig(args as CustomOpenNotificationParams, <CheckCircleOutlined />, "#52c41a")
        );
        break;
      case "error":
        notification.error(
          createNotificationConfig(args as CustomOpenNotificationParams, <CloseCircleOutlined />, "#ff4d4f")
        );
        break;
      case "progress":
        notification.open({
          ...createNotificationConfig(args as CustomOpenNotificationParams, <LoadingOutlined />, "#1677ff"),
          duration: 0, // Giữ notification mở cho đến khi được đóng thủ công
        });
        break;
      default:
        notification.info(
          createNotificationConfig(args as CustomOpenNotificationParams, <InfoCircleOutlined />, "#1677ff")
        );
        break;
    }
  },
  close: (key) => {
    notification.destroy(key);
  },
};