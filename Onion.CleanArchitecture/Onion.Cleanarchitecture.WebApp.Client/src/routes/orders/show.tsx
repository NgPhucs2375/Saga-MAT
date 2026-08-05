import { useShow, useNavigation, useCan } from "@refinedev/core";
import { Show, EditButton, DeleteButton } from "@refinedev/antd";
import { Typography, Space, Tooltip } from "antd";
import {
  IOrderDetail,
  OrderStatus,
} from "./types";
import { EditOutlined, DeleteOutlined } from "@ant-design/icons";
import { OrderShowContent } from "./ordercomponent";
const { Text } = Typography;

export const ShowOrder = () => {
  const {
    queryResult: { isLoading, data },
  } = useShow<IOrderDetail>();

  const order = data?.data;

  const {  goBack } = useNavigation();

  // Check permissions for edit/delete
  const { data: canEdit } = useCan({
    resource: "orders",
    action: "edit",
    params: { record: order },
  });
  const { data: canDelete } = useCan({
    resource: "orders",
    action: "delete",
    params: { record: order },
  });

  return (
    <Show
      isLoading={isLoading}
      title={<Text>Chi tiết Đơn hàng #{order?.OrderCode}</Text>}
      headerButtons={
        <Space>
          {order?.Status === OrderStatus.Submitted && canEdit?.can && (
            <Tooltip title="Chỉnh sửa đơn hàng">
              <EditButton
                resource="orders"
                recordItemId={order?.OrderId}
                hideText
                icon={<EditOutlined />}
              />
            </Tooltip>
          )}
          {canDelete?.can && (
            <Tooltip title="Xóa đơn hàng">
              <DeleteButton
                resource="orders"
                recordItemId={order?.OrderId}
                hideText
                icon={<DeleteOutlined />}
                onSuccess={() => {
                  goBack();
                }}
              />
            </Tooltip>
          )}
        </Space>
      }
    >
      <OrderShowContent order={order} isLoading={isLoading} />
    </Show>
  );
};