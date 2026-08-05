import React, { useMemo } from "react";
import { Card, Typography, Tag, Empty, Steps, Tooltip } from "antd";
import { useList } from "@refinedev/core";
import {
  IOrderHistory,
  OrderStatus,
  OrderStatusColor,
  OrderStatusLabel,
} from "@routes/orders/types";
import {
  ApiOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  CloudSyncOutlined,
  DatabaseOutlined,
  HistoryOutlined,
  RocketOutlined,
  SendOutlined,
  ThunderboltOutlined,
} from "@ant-design/icons";

// Extend IOrderHistory to include properties expected from the API response.
// This is done because IOrderHistory from @routes/orders/types might be a more basic version,
// and these properties are used in this component and saga-timeline.tsx.
interface FullOrderHistory extends IOrderHistory {
  HistoryId: string;
  EventType: string;
  ConsumerName: string;
  Message: string;
  CreatedAt: string;
}
const { Text } = Typography;

enum HistoryStatus {
  Success = 1,
  Failed = 2,
}

interface ServiceNode {
  key: string;
  label: string;
  color: string;
  icon: React.ReactNode;
}

interface PipelineHop {
  id: string;
  from: string;
  to: string;
  message: string;
  eventLabel: string;
  status?: HistoryStatus;
  isCommand: boolean; // true: Saga -> service (command) / false: service -> Saga (event)
}

const SERVICES: Record<string, ServiceNode> = {
  webapp: { key: "webapp", label: "UI / WebApp", color: "#1677ff", icon: <RocketOutlined /> },
  saga: { key: "saga", label: "Saga (Orchestrator)", color: "#722ed1", icon: <CloudSyncOutlined /> },
  submit: { key: "submit", label: "OrderSubmitService", color: "#13c2c2", icon: <SendOutlined /> },
  accept: { key: "accept", label: "OrderAcceptService", color: "#fa8c16", icon: <DatabaseOutlined /> },
  complete: { key: "complete", label: "OrderCompleteService", color: "#52c41a", icon: <ThunderboltOutlined /> },
  notify: { key: "notify", label: "NotificationService", color: "#eb2f96", icon: <HistoryOutlined /> },
};

// Fixed happy-path pipeline definition. Each hop is a real message in the saga.
const HAPPY_PATH: PipelineHop[] = [
  { id: "create", from: "webapp", to: "saga", message: "OrderCreatedEvent", eventLabel: "Tạo đơn", isCommand: false },
  { id: "validate-cmd", from: "saga", to: "submit", message: "ValidateOrderCommand", eventLabel: "Validate & giữ chỗ tồn kho", isCommand: true },
  { id: "validated", from: "submit", to: "saga", message: "OrderValidatedEvent", eventLabel: "Đã validate", isCommand: false },
  { id: "accept-cmd", from: "saga", to: "accept", message: "AcceptOrderCommand", eventLabel: "Duyệt đơn + timer", isCommand: true },
  { id: "accepted", from: "accept", to: "saga", message: "OrderAcceptedEvent", eventLabel: "Đã duyệt", isCommand: false },
  { id: "complete-cmd", from: "saga", to: "complete", message: "CompleteOrderCommand", eventLabel: "Hoàn tất đơn", isCommand: true },
  { id: "completed", from: "complete", to: "saga", message: "OrderCompletedEvent", eventLabel: "Đã hoàn tất", isCommand: false },
  { id: "notify", from: "saga", to: "notify", message: "*Response", eventLabel: "Thông báo UI", isCommand: false },
];

// Compensation hops (LIFO) triggered only when a failure occurs after inventory is reserved.
const COMPENSATION: PipelineHop[] = [
  { id: "release-cmd", from: "saga", to: "complete", message: "ReleaseInventoryCommand", eventLabel: "Bồi hoàn tồn kho (LIFO 1)", isCommand: true },
  { id: "cancel-cmd", from: "saga", to: "accept", message: "CancelOrderCommand", eventLabel: "Hủy đơn + timer (LIFO 2)", isCommand: true },
];

// Map a ConsumerName + EventType combo from OrderHistory to a pipeline hop and its result.
function matchHop(history: FullOrderHistory): { hopId: string; from: string; to: string; status: HistoryStatus } | null {
  const consumer = history.ConsumerName;
  const event = history.EventType;
  const status = history.Status;

  const byConsumer: Record<string, Record<string, string>> = {
    OrderSubmitConsumer: { ValidateOrderCommand: "validate-cmd" },
    OrderAcceptConsumer: { AcceptOrderCommand: "accept-cmd", CancelOrderCommand: "cancel-cmd", Compensate: "cancel-cmd" },
    OrderCompleteConsumer: { CompleteOrderCommand: "complete-cmd", ReleaseInventoryCommand: "release-cmd" },
    ReleaseInventoryConsumer: { Compensate: "release-cmd", ReleaseInventoryCommand: "release-cmd" },
    CancelOrderConsumer: { Compensate: "cancel-cmd" },
  };

  const key = byConsumer[consumer]?.[event];
  if (!key) return null;

  // Resolve direction: command hops go Saga -> service; event hops go service -> Saga.
  const known = [...HAPPY_PATH, ...COMPENSATION].find((h) => h.id === key);
  if (!known) return null;
  return { hopId: key, from: known.from, to: known.to, status };
}

interface Props {
  orderId: string;
  orderStatus?: OrderStatus;
}

export const SagaFlowDiagram: React.FC<Props> = ({ orderId, orderStatus }) => {
  const { data, isLoading, isError } = useList<FullOrderHistory>({
    resource: "orderhistories", // The API returns FullOrderHistory, but the type is IOrderHistory
    filters: [{ field: "OrderId", operator: "eq", value: orderId }],
    sorters: [{ field: "CreatedAt", order: "asc" }],
    pagination: { pageSize: 100 },
    queryOptions: { enabled: !!orderId },
  });

  const histories = (data?.data ?? []).slice().sort(
    (a, b) => new Date(a.CreatedAt).getTime() - new Date(b.CreatedAt).getTime()
  );

  const { hopStates, executedCount, failed } = useMemo(() => {
    const state: Record<string, HistoryStatus> = {};
    let failedHop: string | null = null;
    for (const h of histories) {
      const m = matchHop(h);
      if (!m) continue;
      state[m.hopId] = m.status;
      if (m.status === HistoryStatus.Failed && !failedHop) failedHop = m.hopId;
    }
    return {
      hopStates: state,
      executedCount: Object.keys(state).length,
      failed: failedHop,
    };
  }, [histories]);

  // Infer the current saga state from the last executed hop.
  const currentState = useMemo(() => getSagaState(histories, orderStatus), [histories, orderStatus]);

  const renderNode = (key: string, state?: HistoryStatus) => {
    const svc = SERVICES[key];
    if (!svc) return null;
    const isActive = getActiveHop(forHop(key), hopStates) && !getHopResult(key, hopStates, "isFailedOnly");
    return (
      <Tooltip title={svc.label}>
        <div
          style={{
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            minWidth: 96,
            padding: "10px 12px",
            borderRadius: 8,
            border: `1px solid ${svc.color}`,
            borderTop: `4px solid ${svc.color}`,
            background: "#fff",
            ...(isActive ? { boxShadow: `0 0 0 3px ${svc.color}33` } : {}),
          }}
        >
          <span style={{ fontSize: 18, color: svc.color }}>{svc.icon}</span>
          <Text strong style={{ fontSize: 11, textAlign: "center", marginTop: 4 }}>
            {svc.label}
          </Text>
          {state !== undefined && <Tag color={state === HistoryStatus.Success ? "success" : "error"} style={{ marginTop: 4 }}>{state === HistoryStatus.Success ? "OK" : "FAIL"}</Tag>}
        </div>
      </Tooltip>
    );
  };

  const renderHop = (hop: PipelineHop) => {
    const st = hopStates[hop.id];
    const isInvolved = st !== undefined;
    const isFailed = st === HistoryStatus.Failed;
    const color = isFailed ? "#f5222d" : isInvolved ? "#1677ff" : "#d9d9d9";
    return (
      <div key={hop.id} style={{ display: "flex", alignItems: "center", flex: "0 0 auto" }}>
        <div style={{ width: isCommandSpan(hop) ? 44 : 34, textAlign: "center", cursor: "default" }}>
          {isCommandSpan(hop) ? (
            <Tag
              style={{ width: 40, textAlign: "center", margin: 0 }}
              color={isFailed ? "error" : isInvolved ? "processing" : "default"}
            >
              CMD
            </Tag>
          ) : (
            <Tag style={{ width: 30, textAlign: "center", margin: 0 }} color={isFailed ? "error" : isInvolved ? "success" : "default"}>
              EVT
            </Tag>
          )}
        </div>
        <div style={{ display: "flex", flexDirection: "column", alignItems: "center", minWidth: 150 }}>
          <div
            style={{
              height: 3,
              width: "100%",
              background: color,
              position: "relative",
            }}
          >
            <span
              style={{
                position: "absolute",
                right: -6,
                top: -3,
                width: 0,
                height: 0,
                borderTop: "4px solid transparent",
                borderBottom: "4px solid transparent",
                borderLeft: `6px solid ${color}`,
              }}
            />
          </div>
          <Text style={{ fontSize: 11, color, textAlign: "center", marginTop: 2, whiteSpace: "nowrap" }}>
            {hop.message}
          </Text>
        </div>
      </div>
    );
  };

  return (
    <Card
      title={
        <>
          <ApiOutlined style={{ marginRight: 8 }} />
          Flow luồng Saga & State Machine
        </>
      }
      extra={<Text type="secondary">NHẬN / GỬI message giữa các service</Text>}
      loading={isLoading}
    >
      {isError ? (
        <Text type="danger">Không thể tải dữ liệu EventStore.</Text>
      ) : histories.length === 0 ? (
        <Empty description="Chưa có dữ liệu OrderHistory để vẽ luồng." />
      ) : (
        <>
          {/* Row 1: Saga State Machine (horizontal steps) */}
          <div style={{ marginBottom: 24 }}>
            <Text strong>Trạng thái Saga (State Machine):</Text>
            <Steps
              size="small"
              style={{ marginTop: 8 }}
              current={currentState.stepIndex}
              status={currentState.terminal === "completed" ? "finish" : currentState.terminal === "cancelled" ? "error" : "process"}
              items={currentState.items}
            />
            <div style={{ marginTop: 8 }}>
              <Tag icon={<Text strong>Trạng thái đơn:</Text>} color={orderStatus ? OrderStatusColor[orderStatus] : undefined}>
                {orderStatus ? OrderStatusLabel[orderStatus] : "Không xác định"}
              </Tag>
            </div>
          </div>

          {/* Row 2: Happy path pipeline */}
          <div style={{ overflowX: "auto", paddingBottom: 8 }}>
            <Text strong>Luồng happy-path (command/event qua queue):</Text>
            <div style={{ display: "flex", alignItems: "center", marginTop: 12, gap: 0 }}>
              {renderHop(HAPPY_PATH[0])}
              {HAPPY_PATH.slice(1).map((hop) => (
                <React.Fragment key={hop.id}>
                  {renderNode(hop.from, getHopState(hop.from, hopStates))}
                  {renderHop(hop)}
                </React.Fragment>
              ))}
              {renderNode("notify")}
            </div>
          </div>

          {/* Row 3: state summary */}
          <div style={{ marginTop: 16, display: "flex", gap: 24, flexWrap: "wrap" }}>
            <span>
              <CheckCircleOutlined style={{ color: "#52c41a" }} /> <Text>Hops đã thực hiện: {executedCount}</Text>
            </span>
            {failed ? (
              <span>
                <CloseCircleOutlined style={{ color: "#f5222d" }} />{" "}
                <Text type="danger">Thất bại tại hop: {failed} — sẽ kích hoạt Compensation nếu đã giữ chỗ tồn kho</Text>
              </span>
            ) : (
              <span>
                <CheckCircleOutlined style={{ color: "#52c41a" }} /> <Text type="success">Happy path đang tiến triển bình thường</Text>
              </span>
            )}
          </div>
        </>
      )}
    </Card>
  );
};

// ---- helpers ----

function isCommandSpan(hop: PipelineHop): boolean {
  return hop.isCommand;
}

function forHop(id: string): string {
  return id;
}

function getActiveHop(id: string, state: Record<string, HistoryStatus>): boolean {
  // TODO: Implement logic to determine if a hop is active based on id and state
  void id; // Mark as intentionally unused
  void state; // Mark as intentionally unused
  return false;
}

function getHopResult(_key: string, _state: Record<string, HistoryStatus>, _opts: string): boolean {
  // TODO: Implement logic to determine hop result based on key, state, and options
  void _key; // Mark as intentionally unused
  void _state; // Mark as intentionally unused
  void _opts; // Mark as intentionally unused
  return false;
}

function getHopState(nodeKey: string, state: Record<string, HistoryStatus>): HistoryStatus | undefined {
  // Find any hop whose `from` or consumer executed belongs to this node. Simple heuristic:
  const map: Record<string, string[]> = {
    submit: ["validate-cmd"],
    accept: ["accept-cmd", "cancel-cmd"],
    complete: ["complete-cmd", "release-cmd"],
  };
  const hops = map[nodeKey] ?? [];
  const anyFailed = hops.some((h) => state[h] === HistoryStatus.Failed);
  const anySuccess = hops.some((h) => state[h] === HistoryStatus.Success);
  if (anyFailed) return HistoryStatus.Failed;
  if (anySuccess) return HistoryStatus.Success;
  return undefined;
}

// ---- current saga state inference from history + terminal order status ----
function getSagaState(histories: FullOrderHistory[], orderStatus?: OrderStatus) {
  const items = [
    { title: "Submitted" },
    { title: "Validating" },
    { title: "Accepting" },
    { title: "Completing" },
    { title: "Completed / Cancelled" },
  ] as { title: string }[];

  let stepIndex = -1;
  let terminal: "completed" | "cancelled" | null = null;

  if (orderStatus === OrderStatus.Completed) {
    stepIndex = 4;
    terminal = "completed";
    items[4] = { title: "Completed" };
  } else if (orderStatus === OrderStatus.Rejected) {
    stepIndex = 4;
    terminal = "cancelled";
    items[4] = { title: "Cancelled" };
  } else {
    // non-final: derive from last recorded hop
    for (let i = histories.length - 1; i >= 0; i--) {
      const h = histories[i];
      const m = matchHop(h);
      if (!m) continue;
      if (m.hopId === "validated") { stepIndex = 1; break; }
      if (m.hopId === "accepted") { stepIndex = 2; break; }
      if (m.hopId === "completed") { stepIndex = 3; break; }
      if (m.hopId === "release-cmd" || m.hopId === "cancel-cmd") { stepIndex = 3; terminal = "cancelled"; items[4] = { title: "Compensating…" }; break; }
      if (m.hopId === "validate-cmd") { stepIndex = 0; break; }
      if (m.hopId === "accept-cmd") { stepIndex = 1; break; }
      if (m.hopId === "complete-cmd") { stepIndex = 2; break; }
    }
    if (stepIndex === -1) stepIndex = 0;
  }

  return { stepIndex, terminal, items };
}