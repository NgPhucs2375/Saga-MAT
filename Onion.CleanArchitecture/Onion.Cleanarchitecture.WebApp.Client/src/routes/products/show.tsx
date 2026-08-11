import { useShow } from "@refinedev/core";
import { IProduct } from "./types";
import { Show, DateField } from "@refinedev/antd";
import {
  Typography,
  Row,
  Col,
  Card,
  Descriptions,
  Image,
  Statistic,
} from "antd";
import {
  DollarOutlined,
  ShoppingCartOutlined,
  WarningOutlined,
} from "@ant-design/icons";
import {
  ProductStatusTag,
  StockLevelTag,
  PRODUCT_PLACEHOLDER,
} from "./productcomponent";

const { Title, Text: TypographyText } = Typography;

const formatCurrency = (value: number | string) =>
  `${Number(value || 0).toLocaleString("vi-VN")} ₫`;

export const ShowProduct = () => {
  const {
    queryResult: { isLoading, data },
  } = useShow<IProduct>();

  const product = data?.data;
  const inventoryValue = (product?.Price ?? 0) * (product?.PhysicalQty ?? 0);

  return (
    <Show
      isLoading={isLoading}
      title={
        product?.Name ? (
          <Title level={4}>{product.Name}</Title>
        ) : (
          "Sản phẩm"
        )
      }
    >
      {/* Quick stats */}
      <Row gutter={[16, 16]}>
        <Col xs={24} sm={8}>
          <Card bordered={false}>
            <Statistic
              title="Giá bán"
              value={product?.Price ?? 0}
              prefix={<DollarOutlined />}
              formatter={formatCurrency}
              valueStyle={{ color: "#0052CC" }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={8}>
          <Card bordered={false}>
            <Statistic
              title="Tồn kho"
              value={product?.PhysicalQty ?? 0}
              prefix={<ShoppingCartOutlined />}
              valueStyle={{
                color: (product?.PhysicalQty ?? 0) <= 0 ? "#cf1322" : "#389e0d",
              }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={8}>
          <Card bordered={false}>
            <Statistic
              title="Giá trị tồn kho"
              value={inventoryValue}
              prefix={<WarningOutlined />}
              formatter={formatCurrency}
            />
          </Card>
        </Col>
      </Row>

      <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
        <Col xs={24} md={8}>
          <Card
            hoverable
            cover={
              <Image
                alt={product?.Name}
                src={product?.ImageUrl ?? PRODUCT_PLACEHOLDER}
                fallback={PRODUCT_PLACEHOLDER}
                style={{ objectFit: "cover", height: 300, borderRadius: "8px 8px 0 0" }}
              />
            }
          >
            <Card.Meta
              title={product?.Name}
              description={
                <TypographyText copyable>
                  {product?.Barcode
                    ? `Barcode: ${product.Barcode}`
                    : "Chưa có mã vạch"}
                </TypographyText>
              }
            />
          </Card>
          <Card style={{ marginTop: 16 }}>
            <TypographyText type="secondary">Trạng thái tồn kho</TypographyText>
            <div style={{ marginTop: 8 }}>
              <StockLevelTag qty={product?.PhysicalQty ?? 0} />
            </div>
          </Card>
        </Col>
        <Col xs={24} md={16}>
          <Card>
            <Descriptions
              bordered
              column={1}
              size="small"
              title="Thông tin chi tiết"
            >
              <Descriptions.Item label="Mã vạch">
                <TypographyText code copyable>
                  {product?.Barcode || "—"}
                </TypographyText>
              </Descriptions.Item>
              <Descriptions.Item label="Mô tả">
                {product?.Description || "Chưa có mô tả."}
              </Descriptions.Item>
              <Descriptions.Item label="Trạng thái">
                <ProductStatusTag isActive={product?.IsActive ?? false} />
              </Descriptions.Item>
              <Descriptions.Item label="Tạo lúc">
                {product?.Created ? (
                  <DateField format="LLL" value={product.Created} />
                ) : (
                  "—"
                )}
              </Descriptions.Item>
              <Descriptions.Item label="Tạo bởi">
                {product?.CreatedBy || "—"}
              </Descriptions.Item>
              <Descriptions.Item label="Cập nhật lúc">
                {product?.LastModified ? (
                  <DateField format="LLL" value={product.LastModified} />
                ) : (
                  "—"
                )}
              </Descriptions.Item>
              <Descriptions.Item label="Cập nhật bởi">
                {product?.LastModifiedBy || "—"}
              </Descriptions.Item>
            </Descriptions>
          </Card>
        </Col>
      </Row>
    </Show>
  );
};