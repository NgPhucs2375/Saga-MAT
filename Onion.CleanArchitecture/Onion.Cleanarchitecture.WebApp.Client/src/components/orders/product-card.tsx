import React from "react";
import { Card, Row, Col, Typography, Button, Tooltip, Empty, Tag } from "antd";
import { PlusOutlined, CheckOutlined } from "@ant-design/icons";
import { IProduct } from "@routes/orders/types";

const { Text: TypographyText } = Typography;

interface ProductCardProps {
  product: IProduct;
  selectedQuantity: number;
  onAdd: (product: IProduct) => void;
}

export const ProductCard: React.FC<ProductCardProps> = ({
  product,
  selectedQuantity,
  onAdd,
}) => {
  const outOfStock = product.AvailableQty <= 0;
  const lowStock = product.AvailableQty > 0 && product.AvailableQty <= 5;
  const availableQty = product.AvailableQty - selectedQuantity;
  const canAddMore = availableQty > 0;

  return (
    <Card
      size="small"
      hoverable
      style={{ height: "100%" }}
      cover={
        product.ImageUrl ? (
          <img
            alt={product.Name}
            src={product.ImageUrl}
            style={{ height: 120, objectFit: "cover" }}
          />
        ) : (
          <div
            style={{
              height: 120,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              background: "#fafafa",
              color: "#bbb",
            }}
          >
            <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="No image" />
          </div>
        )
      }
    >
      <Row gutter={[4, 4]}>
        <Col span={24}>
          <TypographyText strong ellipsis>{product.Name}</TypographyText>
        </Col>
        <Col span={24}>
          <TypographyText type="secondary" ellipsis>Mã: {product.Code}</TypographyText>
        </Col>
        <Col span={24}>
          <TypographyText strong style={{ color: "#1890ff" }}>
            {product.Price.toLocaleString()} VND
          </TypographyText>
        </Col>
        <Col span={24}>
          <span style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
            <Tag
              color={outOfStock ? "red" : lowStock ? "orange" : "green"}
              style={{ fontSize: 11, padding: "0 6px" }}
            >
              {outOfStock ? "Hết hàng" : lowStock ? `Sắp hết (${product.AvailableQty})` : `Khả dụng: ${product.AvailableQty}`}
            </Tag>
            {selectedQuantity > 0 && (
              <Tag color="blue" style={{ fontSize: 11, padding: "0 6px" }}>
                Đã chọn: {selectedQuantity}
              </Tag>
            )}
            {availableQty >= 0 && (
              <Tag 
                color={availableQty <= 0 ? "red" : availableQty <= 5 ? "orange" : "green"} 
                style={{ fontSize: 11, padding: "0 6px", fontWeight: 500 }}
              >
                Còn khả dụng: {Math.max(0, availableQty)}
              </Tag>
            )}
          </span>
        </Col>
        <Col span={24}>
          <Tooltip title={outOfStock ? "Hết hàng" : !canAddMore ? "Đã hết khả dụng" : selectedQuantity > 0 ? "Đã thêm vào giỏ - click để thêm tiếp" : "Thêm vào giỏ"}>
            <Button
              type={selectedQuantity > 0 ? "primary" : "default"}
              icon={selectedQuantity > 0 ? <CheckOutlined /> : <PlusOutlined />}
              disabled={outOfStock || !canAddMore}
              block
              onClick={() => onAdd(product)}
            >
              {outOfStock ? "Hết hàng" : !canAddMore ? "Đã hết khả dụng" : selectedQuantity > 0 ? `Đã chọn (${selectedQuantity}) - Thêm tiếp` : "Chọn"}
            </Button>
          </Tooltip>
        </Col>
      </Row>
    </Card>
  );
};