import React, { useState } from "react";
import {
  ImportButton,
  useImport,
  Create,
  Breadcrumb,
} from "@refinedev/antd";
import {
  Space,
  Table,
  Tag,
  Card,
  Row,
  Col,
  Statistic,
  Progress,
  Typography,
} from "antd";
import type { TableProps } from "antd";
import { HttpError } from "@refinedev/core";
import { IProduct } from "./types";

interface IProductError extends IProduct {
  Message: string;
  Success: boolean;
}

export const CreateRangeProduct: React.FC = () => {
  const [importProgress, setImportProgress] = useState({
    processed: 0,
    total: 0,
  });
  const [responses, setResponses] = useState<IProductError[]>([]);

  const handleSuccess = (successes: any[]) => {
    successes.forEach((success) => {
      const successData = success.response as IProductError[];
      setResponses((prev: IProductError[]) => [...prev, ...successData]);
    });
  };

  const handleError = (errors: any[]) => {
    errors.forEach((error) => {
      const errorRequest = error.request as IProduct[];
      const errorResponse = error.response as HttpError[];
      setResponses((prev) => [
        ...prev,
        ...errorRequest.map((request, index) => ({
          ...request,
          Message: errorResponse[index].message,
          Success: false,
        })),
      ]);
    });
  };

  const importProps = useImport<IProductError>({
    resource: "products",
    onFinish: (result) => {
      const { succeeded, errored } = result;

      if (succeeded.length > 0) {
        handleSuccess(succeeded);
      }

      if (errored.length > 0) {
        handleError(errored);
      }
    },
    onProgress: (progress) => {
      setImportProgress({
        processed: progress.processedAmount,
        total: progress.totalAmount,
      });
    },
    paparseOptions: {
      header: false,
    },
    batchSize: 5,
  });

  const successCount = responses.filter((r) => r.Success).length;
  const errorCount = responses.length - successCount;
  const percent =
    importProgress.total > 0
      ? Math.round((importProgress.processed / importProgress.total) * 100)
      : 0;

  const columns: TableProps<IProductError>["columns"] = [
    {
      title: "Kết quả",
      dataIndex: "Success",
      key: "Success",
      width: 120,
      render: (value) =>
        value ? (
          <Tag color="green">Thành công</Tag>
        ) : (
          <Tag color="red">Thất bại</Tag>
        ),
    },
    {
      title: "Tên sản phẩm",
      dataIndex: "Name",
      key: "Name",
    },
    {
      title: "Mã vạch",
      dataIndex: "Barcode",
      key: "Barcode",
    },
    {
      title: "Thông báo",
      dataIndex: "Message",
      key: "Message",
      render: (value) => value || "—",
    },
  ];

  return (
    <Create
      title="Nhập hàng loạt sản phẩm"
      breadcrumb={
        <Breadcrumb
          breadcrumbProps={{
            items: [
              {
                title: "Sản phẩm",
                href: "/products",
              },
              {
                title: "Nhập hàng loạt",
              },
            ],
          }}
        />
      }
    >
      <Row gutter={[16, 16]} style={{ marginBottom: 16 }}>
        <Col xs={12} md={6}>
          <Card bordered={false}>
            <Statistic
              title="Đã xử lý"
              value={importProgress.processed}
              suffix={`/ ${importProgress.total}`}
            />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card bordered={false}>
            <Statistic
              title="Thành công"
              value={successCount}
              valueStyle={{ color: "#389e0d" }}
            />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card bordered={false}>
            <Statistic
              title="Lỗi"
              value={errorCount}
              valueStyle={{ color: "#cf1322" }}
            />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card bordered={false}>
            <Typography.Text type="secondary">Tiến trình</Typography.Text>
            <Progress
              percent={percent}
              status={percent === 100 ? "success" : "active"}
              size="small"
              format={() => `${importProgress.processed}/${importProgress.total}`}
            />
          </Card>
        </Col>
      </Row>

      <Space style={{ marginBottom: 16 }}>
        <ImportButton {...importProps} accept=".csv">
          Chọn file CSV
        </ImportButton>
        <Typography.Text type="secondary">
          Nhập tệp CSV chứa danh sách sản phẩm để tạo hàng loạt.
        </Typography.Text>
      </Space>
      <Table
        columns={columns}
        dataSource={responses}
        rowKey={(record) =>
          `${record.Success}-${record.Name}-${record.Barcode}-${record.Message}`
        }
        size="middle"
        pagination={{ pageSize: 10, showSizeChanger: false }}
      />
    </Create>
  );
};