import { useForm, Create } from "@refinedev/antd";
import { IProduct } from "./types";
import { Form } from "antd";
import { ProductFormFields } from "./product-form";

export const CloneProduct = () => {
  const { formProps, saveButtonProps, queryResult } = useForm<IProduct>({
    redirect: "list",
  });

  const imageUrl = queryResult?.data?.data?.ImageUrl;

  return (
    <Create
      resource="products"
      saveButtonProps={saveButtonProps}
      title="Nhân bản sản phẩm"
    >
      <Form {...formProps} layout="vertical">
        <ProductFormFields
          form={formProps.form}
          initialImageUrl={imageUrl}
        />
      </Form>
    </Create>
  );
};