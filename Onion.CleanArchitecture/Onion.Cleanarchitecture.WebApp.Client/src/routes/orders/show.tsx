import { useShow, useNavigation, useCan } from "@refinedev/core";
import { Show, EditButton, DeleteButton } from "@refinedev/antd";
import { Typography, Space, Tooltip } from "antd";
import { IOrderDetail, OrderStatus, OrderStatusLabel } from "./types";
import { EditOutlined, DeleteOutlined } from "@ant-design/icons";
import { OrderShowContent, OrderStatusTag } from "./ordercomponent";
const { Text: TypographyText } = Typography;

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
      title={
        <Space wrap>
          <TypographyText>Chi tiết Đơn hàng</TypographyText>
          {order && (
            <>
              <TypographyText strong>#{order.OrderCode}</TypographyText>
              <OrderStatusTag
                size="large"
                status={
                  order.Status != null
                    ? OrderStatusLabel[order.Status] ?? String(order.Status)
                    : "Unknown"
                }
              />
            </>
          )}
        </Space>
      }
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