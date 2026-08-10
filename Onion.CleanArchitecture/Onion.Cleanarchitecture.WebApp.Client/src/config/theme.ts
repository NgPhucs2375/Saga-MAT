import { ThemeConfig } from "antd";

export const appTheme: ThemeConfig = {
  token: {
    // 1. Màu sắc thương hiệu chuẩn Enterprise
    colorPrimary: "#0052CC", // Blue Atlassian/Enterprise
    colorInfo: "#0052CC",
    colorSuccess: "#52c41a",
    colorWarning: "#faad14",
    colorError: "#ff4d4f",
    colorBgBase: "#f8fafc", // Nền xám nhạt hiện đại cho toàn bộ App

    // 2. Phông chữ & Bo góc
    fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif",
    borderRadius: 8,
    controlHeight: 38, // Chiều cao chuẩn cho các ô Input/Button

    // 3. Đổ bóng (Elevation/Shadow)
    boxShadowSecondary: "0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)",
  },
  components: {
    Button: {
      fontWeight: 500,
      borderRadius: 6,
    },
    Card: {
      boxShadowTertiary: "0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px 0 rgba(0, 0, 0, 0.06)",
      headerBg: "#ffffff",
    },
    Table: {
      headerBg: "#f1f5f9", // Header bảng màu xám mờ phân biệt rõ nét
      headerColor: "#334155",
      rowHoverBg: "#f8fafc",
    },
    Tag: {
      borderRadius: 4, // Tag bo góc gọn gàng
    },
  },
};