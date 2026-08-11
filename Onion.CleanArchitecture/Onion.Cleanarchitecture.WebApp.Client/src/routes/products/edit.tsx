import { useForm, Edit } from "@refinedev/antd";
import { IProduct } from "./types";
import { Form } from "antd";
import { ProductFormFields } from "./product-form";

export const EditProduct = () => {
  const { formProps, saveButtonProps, queryResult } = useForm<IProduct>({
    redirect: "show",
  });

  const imageUrl = queryResult?.data?.data?.ImageUrl;

  return (
    <Edit
      saveButtonProps={saveButtonProps}
      title={`Chỉnh sửa sản phẩm: ${
        queryResult?.data?.data.Name ?? ""
      }`}
    >
      <Form {...formProps} layout="vertical">
        <ProductFormFields
          form={formProps.form}
          initialImageUrl={imageUrl}
          showId
        />
      </Form>
    </Edit>
  );
};