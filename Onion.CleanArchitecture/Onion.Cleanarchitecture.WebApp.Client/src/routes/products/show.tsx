import { useShow } from "@refinedev/core";
import { IProduct } from "./types";
import { Show, NumberField } from "@refinedev/antd";
import { Typography, Row, Col, Card, Descriptions, Image } from "antd";
import { ProductStatusTag } from "./productcomponent";

const { Title, Text: TypographyText } = Typography;

export const ShowProduct = () => {
  const {
    queryResult: { isLoading, data },
  } = useShow<IProduct>();

  const product = data?.data;

  return (
    <Show isLoading={isLoading} title={<Title level={4}>{product?.Name ?? "Loading..."}</Title>}>
      <Row gutter={[16, 16]}>
        <Col xs={24} md={8}>
          <Card
            hoverable
            cover={
              <Image
                alt={product?.Name}
                src={product?.ImageUrl ?? "/images/product-placeholder.png"}
                fallback="/images/product-placeholder.png"
                style={{ objectFit: "cover", height: 300 }}
              />
            }
          >
            <Card.Meta
              title={product?.Name}
              description={
                <TypographyText copyable>
                  {product?.Barcode ? `Barcode: ${product.Barcode}` : "Chưa có mã vạch"}
                </TypographyText>
              }
            />
          </Card>
        </Col>
        <Col xs={24} md={16}>
          <Card>
            <Descriptions bordered column={1} size="small">
              <Descriptions.Item label="Mô tả">
                {product?.Description || "Chưa có mô tả."}
              </Descriptions.Item>
              <Descriptions.Item label="Giá bán">
                <NumberField
                  value={product?.Price ?? 0}
                  options={{ style: "currency", currency: "VND" }}
                  strong
                />
              </Descriptions.Item>
              <Descriptions.Item label="Số lượng tồn kho">
                <TypographyText strong style={{ color: (product?.PhysicalQty ?? 0) > 0 ? 'inherit' : 'red' }}>
                  {product?.PhysicalQty ?? 0}
                </TypographyText>
              </Descriptions.Item>
              <Descriptions.Item label="Trạng thái">
                <ProductStatusTag isActive={product?.IsActive ?? false} />
              </Descriptions.Item>
            </Descriptions>
          </Card>
        </Col>
      </Row>
    </Show>
  );
};
