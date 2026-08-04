import { useState, useEffect } from "react";
import { Card, Table, Tag, Button, Modal } from "antd";
import { useTable } from "@refinedev/antd";
import { IEventStore, EventStoreStatus } from "./types";
import { EyeOutlined } from "@ant-design/icons";

export const LiveEventStream = ({ correlationId }: { correlationId: string }) => {
  const { tableProps, tableQueryResult } = useTable<IEventStore>({
    resource: "eventstores",
    filters: {
      permanent: [{ field: "CorrelationId", operator: "eq", value: correlationId }],
    },
    sorters: {
      initial: [{ field: "SequenceNumber", order: "asc" }],
    },
    pagination: { pageSize: 50 },
    queryOptions: {
      enabled: !!correlationId,
    },
    syncWithLocation: false,
  });

  const [isModalVisible, setIsModalVisible] = useState(false);
  const [payload, setPayload] = useState({});

  // Auto-refresh logic
  useEffect(() => {
    const interval = setInterval(() => {
      tableQueryResult.refetch();
    }, 5000); // Refetch every 5 seconds

    return () => clearInterval(interval);
  }, [tableQueryResult]);

  const showPayload = (record: IEventStore) => {
    try {
      setPayload(JSON.parse(record.PayLoad));
    } catch (e) {
      setPayload({ error: "Invalid JSON format", raw: record.PayLoad });
    }
    setIsModalVisible(true);
  };

  const columns = [
    { title: "Seq", dataIndex: "SequenceNumber", key: "SequenceNumber", width: 60, sorter: true },
    { title: "Event Type", dataIndex: "EventType", key: "EventType", sorter: true },
    {
      title: "Status",
      dataIndex: "Status",
      key: "Status",
      render: (status: EventStoreStatus) => {
        let color = "default";
        let text = "Pending";
        if (status === EventStoreStatus.Processed) { color = "success"; text = "Processed"; }
        if (status === EventStoreStatus.Failed) { color = "error"; text = "Failed"; }
        return <Tag color={color}>{text}</Tag>;
      },
    },
    { title: "Retries", dataIndex: "RetryCount", key: "RetryCount", width: 80 },
    { title: "Error", dataIndex: "ErrorMessage", key: "ErrorMessage", ellipsis: true },
    {
      title: "Action",
      key: "action",
      render: (_: unknown, record: IEventStore) => (
        <Button icon={<EyeOutlined />} onClick={() => showPayload(record)}>View</Button>
      ),
    },
  ];

  return (
    <>
      <Card title="Giám sát Luồng Sự kiện (EventStore)">
        <Table
          {...tableProps}
          columns={columns}
          rowKey="StoreId"
          loading={tableQueryResult.isFetching}
          rowClassName={(record) => record.Status === EventStoreStatus.Failed ? "ant-table-row-error" : ""}
        />
      </Card>
      <Modal
        title="Event Payload"
        open={isModalVisible}
        onOk={() => setIsModalVisible(false)}
        onCancel={() => setIsModalVisible(false)}
        width={800}
        footer={[<Button key="back" onClick={() => setIsModalVisible(false)}>Close</Button>]}
      >
        <pre style={{ maxHeight: 500, overflow: "auto", background: "#1e1e1e", color: "#d4d4d4", padding: 12, borderRadius: 8, fontSize: 12 }}>
          {JSON.stringify(payload, null, 2)}
        </pre>
      </Modal>
    </>
  );
};
