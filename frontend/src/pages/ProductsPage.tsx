import { Button, Card, Col, DatePicker, Form, Row, Select, Space, Table, Tag, Typography } from "antd";
import type { ColumnsType } from "antd/es/table";
import dayjs from "dayjs";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { http } from "../api/http";
import { useAuth } from "../auth/AuthContext";
import { downloadBlob } from "../utils/download";

type ProductRow = {
  id: number;
  date: string;
  batchNumber: string;
  wireCode: string;
  laboratory: string;
  customerName: string | null;
  assistant: string;
  isAccepted: boolean;
  rejectReason: string | null;
};

type Filters = {
  laboratories: { id: number; name: string }[];
  wireCodes: { id: number; code: string }[];
  customers: { id: number; name: string }[];
};

type FilterForm = {
  fromUtc?: dayjs.Dayjs;
  toUtc?: dayjs.Dayjs;
  laboratoryId?: number;
  wireCodeId?: number;
  customerId?: number;
  status?: "all" | "accepted" | "rejected";
  sortBy?: string;
  sortDesc?: boolean;
};

export default function ProductsPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [form] = Form.useForm<FilterForm>();
  const [data, setData] = useState<ProductRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [filters, setFilters] = useState<Filters>({ laboratories: [], wireCodes: [], customers: [] });
  const [pagination, setPagination] = useState({ page: 1, pageSize: 20, total: 0, totalPages: 0 });
  const [sortConfig, setSortConfig] = useState<{ sortBy?: string; sortDesc?: boolean }>({ sortBy: "date", sortDesc: true });
  const isAdmin = auth.role === "Admin";

  const loadFilters = useCallback(async () => {
    const response = await http.get("/products/filters");
    setFilters(response.data);
  }, []);

  const loadData = useCallback(
    async (page = 1, pageSize = 20, sortBy?: string, sortDesc?: boolean) => {
      setLoading(true);
      try {
        const formValues = form.getFieldsValue();
        const params: Record<string, unknown> = {
          page,
          pageSize: pageSize || 20,
          sortBy: sortBy ?? sortConfig.sortBy,
          sortDesc: sortDesc ?? sortConfig.sortDesc
        };
        if (formValues.fromUtc) params.fromUtc = formValues.fromUtc.toISOString();
        if (formValues.toUtc) params.toUtc = formValues.toUtc.toISOString();
        if (formValues.laboratoryId) params.laboratoryId = formValues.laboratoryId;
        if (formValues.wireCodeId) params.wireCodeId = formValues.wireCodeId;
        if (formValues.customerId) params.customerId = formValues.customerId;
        if (formValues.status && formValues.status !== "all") params.status = formValues.status === "accepted" ? "Accepted" : "Rejected";

        const response = await http.get("/products", { params });
        setData(response.data.items);
        setPagination({
          page: response.data.page,
          pageSize: response.data.pageSize,
          total: response.data.totalCount,
          totalPages: response.data.totalPages
        });
      } catch {
        setData([]);
      } finally {
        setLoading(false);
      }
    },
    [form, sortConfig]
  );

  const handleTableChange = useCallback((pagination: any, _filters: any, sorter: any) => {
    let sortBy = "date";
    let sortDesc = true;

    if (sorter.field) {
      sortBy = sorter.field;
      sortDesc = sorter.order === "descend";
    }

    setSortConfig({ sortBy, sortDesc });
    void loadData(pagination.current, pagination.pageSize, sortBy, sortDesc);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }, [loadData]);

  const handleReset = useCallback(() => {
    form.resetFields();
    setSortConfig({ sortBy: "date", sortDesc: true });
    void loadData(1, 20, "date", true);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }, [form, loadData]);

  const columns = useMemo<ColumnsType<ProductRow>>(
    () => [
      {
        title: "Дата",
        dataIndex: "date",
        render: (v: string) => dayjs(v).format("YYYY-MM-DD HH:mm"),
        width: 170,
        sorter: true,
        sortOrder: sortConfig.sortBy === "date" ? (sortConfig.sortDesc ? "descend" : "ascend") : undefined
      },
      {
        title: "Партия",
        dataIndex: "batchNumber",
        width: 140,
        sorter: true,
        sortOrder: sortConfig.sortBy === "batchNumber" ? (sortConfig.sortDesc ? "descend" : "ascend") : undefined
      },
      {
        title: "Код проволоки",
        dataIndex: "wireCode",
        width: 120,
        sorter: true,
        sortOrder: sortConfig.sortBy === "wireCode" ? (sortConfig.sortDesc ? "descend" : "ascend") : undefined
      },
      {
        title: "Лаборатория",
        dataIndex: "laboratory",
        width: 140,
        sorter: true,
        sortOrder: sortConfig.sortBy === "laboratory" ? (sortConfig.sortDesc ? "descend" : "ascend") : undefined
      },
      {
        title: "Компания",
        dataIndex: "customerName",
        width: 160,
        render: (v: string | null) => v ?? "—",
        sorter: true,
        sortOrder: sortConfig.sortBy === "customerName" ? (sortConfig.sortDesc ? "descend" : "ascend") : undefined
      },
      {
        title: "Лаборант",
        dataIndex: "assistant",
        width: 140,
        sorter: true,
        sortOrder: sortConfig.sortBy === "assistant" ? (sortConfig.sortDesc ? "descend" : "ascend") : undefined
      },
      {
        title: "Результат",
        dataIndex: "isAccepted",
        width: 120,
        sorter: true,
        sortOrder: sortConfig.sortBy === "isAccepted" ? (sortConfig.sortDesc ? "descend" : "ascend") : undefined,
        render: (accepted: boolean, row: ProductRow) =>
          accepted ? (
            <Tag color="green">Принято</Tag>
          ) : (
            <Tag color="red" title={row.rejectReason ?? undefined}>
              Брак
            </Tag>
          )
      },
      {
        title: "Сертификат",
        key: "pdf",
        width: 120,
        render: (_, row) => (
          <a
            href="#"
            onClick={async (e) => {
              e.preventDefault();
              try {
                await downloadBlob(`/reports/test-results/${row.id}/certificate`, `certificate-${row.id}.pdf`);
              } catch {
                // ignore
              }
            }}
          >
            Сертификат
          </a>
        )
      },
      {
        title: "",
        key: "open",
        width: 180,
        render: (_, row) => (
          <a href="#" onClick={(e) => { e.preventDefault(); navigate(`/lab?testResultId=${row.id}`); }}>
            Открыть протокол
          </a>
        )
      }
    ],
    [navigate, sortConfig]
  );

  useEffect(() => {
    void loadFilters();
    void loadData(1, 20);
  }, [loadFilters, loadData]);

  return (
    <Space direction="vertical" size={16} style={{ width: "100%" }}>
      <Typography.Title level={4}>Брак и продукция</Typography.Title>
      <Card>
        <Form
          form={form}
          layout="vertical"
          onFinish={() => void loadData(1, pagination.pageSize)}
        >
          <Row gutter={[16, 0]} align="bottom">
            <Col xs={24} sm={12} md={6} lg={4}>
              <Form.Item name="fromUtc" label="С">
                <DatePicker showTime style={{ width: "100%" }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} md={6} lg={4}>
              <Form.Item name="toUtc" label="По">
                <DatePicker showTime style={{ width: "100%" }} />
              </Form.Item>
            </Col>
            {isAdmin && (
              <Col xs={24} sm={12} md={6} lg={4}>
                <Form.Item name="laboratoryId" label="Лаборатория">
                  <Select
                    style={{ width: "100%" }}
                    placeholder="Все"
                    allowClear
                    showSearch
                    optionFilterProp="label"
                    options={filters.laboratories.map((l) => ({ value: l.id, label: l.name }))}
                  />
                </Form.Item>
              </Col>
            )}
            <Col xs={24} sm={12} md={6} lg={4}>
              <Form.Item name="wireCodeId" label="Код проволоки">
                <Select
                  style={{ width: "100%" }}
                  placeholder="Все"
                  allowClear
                  showSearch
                  optionFilterProp="label"
                  options={filters.wireCodes.map((w) => ({ value: w.id, label: w.code }))}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} md={6} lg={4}>
              <Form.Item name="customerId" label="Компания">
                <Select
                  style={{ width: "100%" }}
                  placeholder="Все"
                  allowClear
                  showSearch
                  optionFilterProp="label"
                  options={filters.customers.map((c) => ({ value: c.id, label: c.name }))}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} md={6} lg={4}>
              <Form.Item name="status" label="Результат" initialValue="all">
                <Select
                  style={{ width: "100%" }}
                  options={[
                    { value: "all", label: "Все" },
                    { value: "accepted", label: "Принято" },
                    { value: "rejected", label: "Брак" }
                  ]}
                />
              </Form.Item>
            </Col>
            <Col xs={24} md={12} lg={4}>
              <Form.Item>
                <Space style={{ width: "100%" }}>
                  <Button type="primary" htmlType="submit" loading={loading} block>
                    Применить
                  </Button>
                  <Button onClick={handleReset} block>
                    Сбросить
                  </Button>
                </Space>
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Card>
      <Table
        rowKey="id"
        columns={columns}
        dataSource={data}
        loading={loading}
        onChange={handleTableChange}
        scroll={{ x: "max-content" }}
        pagination={{
          current: pagination.page,
          pageSize: pagination.pageSize,
          total: pagination.total,
          showSizeChanger: true,
          pageSizeOptions: ["10", "20", "50", "100"]
        }}
      />
    </Space>
  );
}
