import type { IResourceItem } from "@refinedev/core";

import {
  DashboardOutlined,
  TeamOutlined,
  ShoppingCartOutlined,
  UserOutlined,
  UserSwitchOutlined,
} from "@ant-design/icons";

export const resources: IResourceItem[] = [
  {
    name: "dashboard",
    list: "/dashboard",
    meta: {
      label: "Bảng điều khiển",
      icon: <DashboardOutlined />,
    },
  },
  {
    name: "products",
    list: "/products",
    create: "/products/create",
    clone: "/products/:id/clone",
    edit: "/products/:id/edit",
    show: "/products/:id",
    meta: {
      canDelete: true,
      label: "Sản phẩm",
      icon: <UserOutlined />,
    },
  },
  {
    name: "orders",
    list: "/orders",
    create: "/orders/create",
    edit: "/orders/:id/edit",
    show: "/orders/:id",
    meta: {
      canDelete: true,
      label: "Đơn hàng",
      icon: <ShoppingCartOutlined />,
    },
  },
  {
    name: "users",
    list: "/users",
    create: "/users/create",
    clone: "/users/:id/clone",
    edit: "/users/:id/edit",
    show: "/users/:id",
    meta: {
      canDelete: true,
      label: "Người dùng",
      icon: <UserOutlined />,
    },
  },
  {
    name: "roles",
    list: "/roles",
    create: "/roles/create",
    clone: "/roles/:id/clone",
    edit: "/roles/:id/edit",
    show: "/roles/:id",
    meta: {
      label: "Vai trò",
      canDelete: true,
      icon: <UserSwitchOutlined />,
    },
  },
  {
    name: "roleclaims",
    identifier: "data-roleclaims",
    list: "/roleclaims",
    create: "/roleclaims/create",
    clone: "/roleclaims/:id/clone",
    edit: "/roleclaims/:id/edit",
    show: "/roleclaims/:id",
    meta: {
      label: "Quyền hạn",
      canDelete: true,
      icon: <TeamOutlined />,
    },
  },
  // Thêm các resource sau để Refine nhận diện
  {
    name: "orderhistories",
    meta: {
      // Ẩn khỏi menu nếu bạn không muốn nó xuất hiện
      // parent: "hidden", 
    },
  },
  {
    name: "ordertimers",
    meta: {
    },
  },
  {
    name: "eventstores",
    meta: {
    },
  },
];
