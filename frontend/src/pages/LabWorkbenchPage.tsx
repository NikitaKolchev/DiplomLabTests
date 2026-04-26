import {
  Alert,
  Button,
  Card,
  Col,
  Form,
  Input,
  InputNumber,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  message
} from "antd";
import type { ColumnsType } from "antd/es/table";
import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { http } from "../api/http";
import { useAuth } from "../auth/AuthContext";

type WireCode = { id: number; code: string };
type Customer = { id: number; name: string };

type InputField = {
  parameterId: number;
  parameterName: string;
  dataType: 1 | 2;
  unit?: string;
  isRequired: boolean;
  minValue?: number;
  maxValue?: number;
};

type ProtocolState = {
  testResultId: number;
  wireCodeId: number;
  rowVersion: string;
  status: 1 | 2;
};

type CreateForm = { wireCodeId: number; batchNumber: string; customerId?: number };

export default function LabWorkbenchPage() {
  const auth = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();
  const [wireCodes, setWireCodes] = useState<WireCode[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [fields, setFields] = useState<InputField[]>([]);
  const [values, setValues] = useState<Record<number, string>>({});
  const [protocol, setProtocol] = useState<ProtocolState | null>(null);
  const [completionInfo, setCompletionInfo] = useState<{ accepted: boolean; reason?: string } | null>(null);
  const [loading, setLoading] = useState(false);
  const canCreateProtocol = auth.role === "Assistant";

  const normalizeStatus = (value: unknown): 1 | 2 => (value === 2 || value === "Completed" ? 2 : 1);
  const statusLabel = (status: 1 | 2) => (status === 2 ? "Завершено" : "В работе");

  const loadWireCodes = async () => {
    const response = await http.get("/wirecodes");
    setWireCodes(response.data);
  };

  const loadCustomers = async () => {
    const response = await http.get("/customers");
    setCustomers(response.data);
  };

  const loadFields = async (wireCodeId: number) => {
    const response = await http.get(`/wire-codes/${wireCodeId}/input-fields`);
    setFields(response.data.fields);
  };

  const createProtocol = async (form: CreateForm) => {
    setLoading(true);
    try {
      const payload = { wireCodeId: form.wireCodeId, batchNumber: form.batchNumber, customerId: form.customerId || null };
      const created = await http.post("/testresults", payload);
      const nextProtocol: ProtocolState = {
        testResultId: created.data.id,
        wireCodeId: created.data.wireCodeId,
        rowVersion: created.data.rowVersion,
        status: normalizeStatus(created.data.status)
      };
      setProtocol(nextProtocol);
      setSearchParams({ testResultId: String(nextProtocol.testResultId) });
      setValues({});
      setCompletionInfo(null);
      await loadFields(form.wireCodeId);
      message.success(`Протокол #${nextProtocol.testResultId} создан`);
    } catch {
      message.error("Не удалось создать протокол");
    } finally {
      setLoading(false);
    }
  };

  const saveValues = async () => {
    if (!protocol) return;
    setLoading(true);
    try {
      const payload = {
        rowVersion: protocol.rowVersion,
        values: fields.map((f) => ({
          parameterId: f.parameterId,
          value: values[f.parameterId] ?? ""
        }))
      };
      const response = await http.put(`/testresults/${protocol.testResultId}/values`, payload);
      setProtocol({ ...protocol, rowVersion: response.data.rowVersion });
      message.success("Значения сохранены");
    } catch {
      message.error("Ошибка сохранения (возможно, конфликт версии 409)");
    } finally {
      setLoading(false);
    }
  };

  const completeProtocol = async () => {
    if (!protocol) return;
    setLoading(true);
    try {
      const response = await http.post(`/testresults/${protocol.testResultId}/complete`, {
        rowVersion: protocol.rowVersion
      });
      setCompletionInfo({
        accepted: response.data.isAccepted,
        reason: response.data.rejectReason ?? undefined
      });
      setProtocol({ ...protocol, status: 2 });
      message.success("Протокол завершен");
    } catch {
      message.error("Не удалось завершить протокол (возможно, конфликт версии 409)");
    } finally {
      setLoading(false);
    }
  };

  const columns = useMemo<ColumnsType<InputField>>(
    () => [
      { title: "Параметр", dataIndex: "parameterName" },
      {
        title: "Тип",
        dataIndex: "dataType",
        width: 100
      },
      {
        title: "Обяз.",
        dataIndex: "isRequired",
        width: 90,
        render: (v: boolean) => (v ? <Tag color="red">Да</Tag> : <Tag>Нет</Tag>)
      },
      {
        title: "Мин",
        dataIndex: "minValue",
        width: 110,
        render: (v?: number) => (v ?? "-")
      },
      {
        title: "Макс",
        dataIndex: "maxValue",
        width: 110,
        render: (v?: number) => (v ?? "-")
      },
      {
        title: "Ед.",
        dataIndex: "unit",
        width: 90,
        render: (v?: string) => v ?? "-"
      },
      {
        title: "Значение",
        key: "value",
        render: (_, row) =>
          row.dataType === 1 ? (
            <InputNumber
              style={{ width: "100%" }}
              value={values[row.parameterId] === "" ? null : Number(values[row.parameterId] ?? "")}
              disabled={protocol?.status === 2}
              onChange={(v) =>
                setValues((prev) => ({
                  ...prev,
                  [row.parameterId]: v === null ? "" : String(v)
                }))
              }
            />
          ) : (
            <Input
              value={values[row.parameterId] ?? ""}
              disabled={protocol?.status === 2}
              onChange={(e) =>
                setValues((prev) => ({
                  ...prev,
                  [row.parameterId]: e.target.value
                }))
              }
            />
          )
      }
    ],
    [values, protocol?.status]
  );

  const loadProtocolById = async (testResultId: number) => {
    setLoading(true);
    try {
      const response = await http.get(`/testresults/${testResultId}`);
      const details = response.data as {
        id: number;
        wireCodeId: number;
        rowVersion: string;
        status: unknown;
        values: Array<{ parameterId: number; value: string }>;
      };

      const loadedValues = details.values.reduce<Record<number, string>>((acc, item) => {
        acc[item.parameterId] = item.value;
        return acc;
      }, {});

      setProtocol({
        testResultId: details.id,
        wireCodeId: details.wireCodeId,
        rowVersion: details.rowVersion,
        status: normalizeStatus(details.status)
      });
      setValues(loadedValues);
      setCompletionInfo(null);
      await loadFields(details.wireCodeId);
    } catch {
      message.error("Не удалось открыть протокол из журнала");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const rawId = searchParams.get("testResultId");
    if (!rawId) {
      return;
    }

    const testResultId = Number(rawId);
    if (!Number.isInteger(testResultId) || testResultId <= 0) {
      return;
    }

    if (protocol?.testResultId === testResultId) {
      return;
    }

    void loadProtocolById(testResultId);
  }, [searchParams]);

  return (
    <Space direction="vertical" size={16} style={{ width: "100%" }}>
      {canCreateProtocol && (
        <Card>
          <Typography.Title level={4}>Создать протокол</Typography.Title>
          <Form<CreateForm> layout="vertical" onFinish={createProtocol}>
            <Row gutter={[16, 0]} align="bottom">
              <Col xs={24} sm={12} md={8}>
                <Form.Item name="wireCodeId" label="Код проволоки" rules={[{ required: true }]}>
                  <Select
                    style={{ width: "100%" }}
                    showSearch
                    optionFilterProp="label"
                    options={wireCodes.map((w) => ({ value: w.id, label: w.code }))}
                    onOpenChange={(open) => {
                      if (open && wireCodes.length === 0) {
                        void loadWireCodes();
                      }
                    }}
                  />
                </Form.Item>
              </Col>
              <Col xs={24} sm={12} md={6}>
                <Form.Item name="batchNumber" label="Партия" rules={[{ required: true }]}>
                  <Input style={{ width: "100%" }} />
                </Form.Item>
              </Col>
              <Col xs={24} sm={12} md={6}>
                <Form.Item name="customerId" label="Компания">
                  <Select
                    style={{ width: "100%" }}
                    placeholder="Не выбрано"
                    allowClear
                    showSearch
                    optionFilterProp="label"
                    options={customers.map((c) => ({ value: c.id, label: c.name }))}
                    onOpenChange={(open) => {
                      if (open && customers.length === 0) {
                        void loadCustomers();
                      }
                    }}
                  />
                </Form.Item>
              </Col>
              <Col xs={24} sm={24} md={4}>
                <Form.Item>
                  <Button type="primary" htmlType="submit" loading={loading} block>
                    Создать
                  </Button>
                </Form.Item>
              </Col>
            </Row>
          </Form>
        </Card>
      )}

      {protocol && (
        <Card
          title={`Протокол #${protocol.testResultId}`}
          extra={<Tag color={protocol.status === 2 ? "green" : "blue"}>{statusLabel(protocol.status)}</Tag>}
        >
          <Space direction="vertical" size={12} style={{ width: "100%" }}>
            {completionInfo && (
              <Alert
                type={completionInfo.accepted ? "success" : "error"}
                message={completionInfo.accepted ? "Партия принята" : "Партия забракована"}
                description={completionInfo.reason}
                showIcon
              />
            )}
            <Table<InputField>
              rowKey="parameterId"
              columns={columns}
              dataSource={fields}
              pagination={false}
              scroll={{ x: "max-content" }}
            />
            <Space wrap>
              <Button onClick={saveValues} disabled={protocol.status === 2} loading={loading}>
                Сохранить значения
              </Button>
              <Button
                type="primary"
                danger
                onClick={completeProtocol}
                disabled={protocol.status === 2}
                loading={loading}
              >
                Завершить протокол
              </Button>
            </Space>
          </Space>
        </Card>
      )}
    </Space>
  );
}
