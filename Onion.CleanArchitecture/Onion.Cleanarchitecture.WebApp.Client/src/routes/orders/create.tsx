import { useForm, Create } from "@refinedev/antd";
import { CreateOrderWizard } from "@components/orders/create-order-wizard";
import { ICreateOrder } from "./types";
import { BaseRecord, HttpError } from "@refinedev/core";

export const CreateOrder = () => {
  const { formProps, saveButtonProps } = useForm<BaseRecord, HttpError, ICreateOrder>({
    resource: "orders",
    redirect: "list",
  });

  return (
    <Create title="Tạo đơn hàng" headerButtons={false}>
      <CreateOrderWizard formProps={formProps} saveButtonProps={saveButtonProps} />
    </Create>
  );
};
