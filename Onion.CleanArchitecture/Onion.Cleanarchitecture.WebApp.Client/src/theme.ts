import { ThemeConfig } from "antd";

/**
 * Cấu hình theme cho Ant Design.
 * Bạn có thể tùy chỉnh màu sắc, font chữ, bo góc, và nhiều hơn nữa.
 * Tham khảo: https://ant.design/docs/react/customize-theme
 */
export const appTheme: ThemeConfig = {
  token: {
    // Màu sắc chủ đạo
    colorPrimary: "#52c41a", // Một màu xanh lá cây dễ chịu
    // Bo góc
    borderRadius: 6,
  },
  components: {
    Button: {
      // Tùy chỉnh riêng cho component Button
      controlHeight: 36,
    },
    Card: {
      // Tùy chỉnh cho Card để có đổ bóng nhẹ
      boxShadow: "0 2px 8px rgba(0, 0, 0, 0.09)",
    },
  },
};