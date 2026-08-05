import {
  Dropdown,
  MenuProps,
  Button,
  Modal,
  Tag,
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

/**
 * Component hiển thị trạng thái sản phẩm (Đang bán / Ngừng bán)
 */
export const ProductStatusTag = ({ isActive }: { isActive: boolean }) => {
  const color = isActive ? "green" : "red";
  const text = isActive ? "Đang bán" : "Ngừng bán";
  return <Tag color={color}>{text}</Tag>;
};

/**
 * Component hiển thị menu các hành động cho mỗi dòng sản phẩm
 */
export const ProductActions = ({ record }: { record: IProduct }) => {
  const { show, edit } = useNavigation();
  const { mutate: deleteMutate } = useDelete();

  const { data: canEdit } = useCan({ resource: "products", action: "edit", params: { id: record.Id } });
  const { data: canDelete } = useCan({ resource: "products", action: "delete", params: { id: record.Id } });

  const showDeleteConfirm = (id: number) => {
    Modal.confirm({
      title: 'Bạn có chắc muốn xóa sản phẩm này?',
      icon: <ExclamationCircleOutlined />,
      content: 'Hành động này không thể hoàn tác.',
      okText: 'Xóa',
      cancelText: 'Hủy',
      onOk() {
        deleteMutate({ resource: "products", id });
      },
    });
  };

  const menuItems: MenuProps["items"] = [
    { key: "show", label: "Xem chi tiết", icon: <EyeOutlined />, onClick: () => show("products", record.Id) },
  ];

  if (canEdit?.can) {
    menuItems.push({ key: "edit", label: "Chỉnh sửa", icon: <EditOutlined />, onClick: () => edit("products", record.Id) });
  }
  if (canDelete?.can) {
    menuItems.push({ key: "divider", type: "divider" });
    menuItems.push({ key: "delete", label: "Xóa", icon: <DeleteOutlined />, danger: true, onClick: () => showDeleteConfirm(record.Id) });
  }

  return (
    <Dropdown menu={{ items: menuItems }} trigger={["click"]}>
      <Button type="text" icon={<MoreOutlined />} />
    </Dropdown>
  );
};