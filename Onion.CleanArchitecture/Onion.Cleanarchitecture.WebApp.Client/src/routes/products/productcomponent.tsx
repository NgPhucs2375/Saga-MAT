import {
  Dropdown,
  MenuProps,
  Button,
  Modal,
  Badge,
  Image,
  Typography,
  Space,
} from "antd";
import {
  MoreOutlined,
  EyeOutlined,
  EditOutlined,
  DeleteOutlined,
  ExclamationCircleOutlined,
} from "@ant-design/icons";
import { useNavigation, useCan, useDelete } from "@refinedev/core";
import { IProduct } from "./types";

export const PRODUCT_PLACEHOLDER =
  "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='160' height='160'%3E%3Crect width='100%25' height='100%25' fill='%23f1f5f9'/%3E%3Ctext x='50%25' y='50%25' fill='%2394a3b8' font-size='13' text-anchor='middle' dy='.35em'%3ENo image%3C/text%3E%3C/svg%3E";

export const ProductStatusTag = ({ isActive }: { isActive: boolean }) => {
  return (
    <Badge
      status={isActive ? "success" : "error"}
      text={isActive ? "Đang bán" : "Ngừng bán"}
    />
  );
};

export const StockLevelTag = ({ qty }: { qty?: number }) => {
  const value = qty ?? 0;
  const color = value <= 0 ? "error" : value <= 5 ? "warning" : "success";
  const label =
    value <= 0 ? "Hết hàng" : value <= 5 ? `Sắp hết (${value})` : `Còn hàng (${value})`;
  return <Badge status={color} text={label} />;
};

export const ProductThumb = ({ record }: { record: IProduct }) => (
  <Space size={12}>
    <Image
      src={record.ImageUrl || PRODUCT_PLACEHOLDER}
      fallback={PRODUCT_PLACEHOLDER}
      width={44}
      height={44}
      preview={false}
      style={{
        objectFit: "cover",
        borderRadius: 8,
        border: "1px solid #eef2f7",
      }}
    />
    <div style={{ display: "flex", flexDirection: "column", minWidth: 0 }}>
      <Typography.Text strong ellipsis style={{ maxWidth: 260 }}>
        {record.Name}
      </Typography.Text>
      {record.Description && (
        <Typography.Text
          type="secondary"
          ellipsis
          style={{ maxWidth: 260, fontSize: 12 }}
        >
          {record.Description}
        </Typography.Text>
      )}
    </div>
  </Space>
);

export const ProductActions = ({ record }: { record: IProduct }) => {
  const { show, edit } = useNavigation();
  const { mutate: deleteMutate } = useDelete();

  const { data: canEdit } = useCan({
    resource: "products",
    action: "edit",
    params: { id: record.Id },
  });
  const { data: canDelete } = useCan({
    resource: "products",
    action: "delete",
    params: { id: record.Id },
  });

  const showDeleteConfirm = (id: number) => {
    Modal.confirm({
      title: "Bạn có chắc muốn xóa sản phẩm này?",
      icon: <ExclamationCircleOutlined />,
      content: "Hành động này không thể hoàn tác.",
      okText: "Xóa",
      okButtonProps: { danger: true },
      cancelText: "Hủy",
      onOk() {
        deleteMutate({ resource: "products", id });
      },
    });
  };

  const menuItems: MenuProps["items"] = [
    {
      key: "show",
      label: "Xem chi tiết",
      icon: <EyeOutlined />,
      onClick: () => show("products", record.Id),
    },
  ];

  if (canEdit?.can) {
    menuItems.push({
      key: "edit",
      label: "Chỉnh sửa",
      icon: <EditOutlined />,
      onClick: () => edit("products", record.Id),
    });
  }
  if (canDelete?.can) {
    menuItems.push({ key: "divider", type: "divider" });
    menuItems.push({
      key: "delete",
      label: "Xóa",
      icon: <DeleteOutlined />,
      danger: true,
      onClick: () => showDeleteConfirm(record.Id),
    });
  }

  return (
    <Dropdown menu={{ items: menuItems }} trigger={["click"]}>
      <Button type="text" icon={<MoreOutlined />} />
    </Dropdown>
  );
};