import {
  Button,
  Card,
  Col,
  DatePicker,
  Form,
  Input,
  Popconfirm,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  message
} from "antd";
import type { ColumnsType } from "antd/es/table";
import dayjs from "dayjs";
import utc from "dayjs/plugin/utc";
import { useCallback, useEffect, useMemo, useState } from "react";

dayjs.extend(utc);
import { useNavigate } from "react-router-dom";
import { http } from "../api/http";
import { useAuth } from "../auth/AuthContext";
import { downloadBlob } from "../utils/download";

type TestResultRow = {
  id: number;
  date: string;
  updatedAtUtc: string;
  batchNumber: string;
  status: 1 | 2;
  wireCodeId: number;
  wireCode: string;
  assistant: string;
  rowVersion: string;
};

type FilterForm = {
  fromUtc?: dayjs.Dayjs;
  toUtc?: dayjs.Dayjs;
  wireCodeId?: number;
  batchNumber?: string;
  status?: 1 | 2;
};

function useDebouncedValue<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = useState(value);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedValue(value), delay);
    return () => clearTimeout(timer);
  }, [value, delay]);

  return debouncedValue;
}

export default function TestResultsPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [form] = Form.useForm();
  const [data, setData] = useState<TestResultRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [wireCodes, setWireCodes] = useState<{ id: number; code: string }[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [total, setTotal] = useState(0);
  const [sortConfig, setSortConfig] = useState<{ sortBy?: string; sortDesc?: boolean }>({});
  const [filters, setFilters] = useState<FilterForm>({});
  const debouncedFilters = useDebouncedValue(filters, 500);
  const canOpenInWorkbench = auth.role === "Assistant" || auth.role === "Engineer";
  const isAdmin = auth.role === "Admin";

  const handleDelete = useCallback(async (id: number) => {
    try {
      await http.delete(`/testresults/${id}`);
      message.success("Испытание удалено");
      await loadData();
    } catch {
      message.error("Не удалось удалить испытание");
    }
  }, []);

  const columns = useMemo<ColumnsType<TestResultRow>>(
    () => [
      {
        title: "Дата",
        dataIndex: "date",
        render: (v: string) => dayjs.utc(v).local().format("YYYY-MM-DD HH:mm"),
        width: 170,
        sorter: true,
        sortOrder: sortConfig.sortBy === "date" ? (sortConfig.sortDesc ? "descend" : "ascend") : undefined
      },
      {
        title: "Партия",
        dataIndex: "batchNumber",
        sorter: true,
        sortOrder: sortConfig.sortBy === "batchNumber" ? (sortConfig.sortDesc ? "descend" : "ascend") : undefined
      },
      {
        title: "Код проволоки",
        dataIndex: "wireCode",
        sorter: true,
        sortOrder: sortConfig.sortBy === "wireCode" ? (sortConfig.sortDesc ? "descend" : "ascend") : undefined
      },
      {
        title: "Лаборант",
        dataIndex: "assistant",
        sorter: true,
        sortOrder: sortConfig.sortBy === "assistant" ? (sortConfig.sortDesc ? "descend" : "ascend") : undefined
      },
      {
        title: "Статус",
        dataIndex: "status",
        render: (value: TestResultRow["status"]) =>
          value === 2 ? <Tag color="green">Завершено</Tag> : <Tag color="blue">В работе</Tag>,
        sorter: true,
        sortOrder: sortConfig.sortBy === "status" ? (sortConfig.sortDesc ? "descend" : "ascend") : undefined
      },
      {
        title: "Сертификат",
        key: "pdf",
        render: (_, row) => (
          <Button
            size="small"
            onClick={async () => {
              try {
                await downloadBlob(`/reports/test-results/${row.id}/certificate`, `certificate-test-${row.id}.pdf`);
              } catch {
                message.error("Не удалось скачать ПДФ сертификат");
              }
            }}
          >
            Сертификат
          </Button>
        )
      },
      {
        title: "Действия",
        key: "actions",
        render: (_, row) => (
          <Space>
            {(canOpenInWorkbench || isAdmin) && (
              <Button size="small" onClick={() => navigate(`/lab?testResultId=${row.id}`)}>
                Открыть в рабочем месте
              </Button>
            )}
            {isAdmin && (
              <Popconfirm title="Удалить испытание?" onConfirm={() => void handleDelete(row.id)}>
                <Button size="small" danger>
                  Удалить
                </Button>
              </Popconfirm>
            )}
          </Space>
        )
      }
    ],
    [canOpenInWorkbench, isAdmin, navigate, handleDelete, sortConfig]
  );

  const loadWireCodes = useCallback(async () => {
    if (wireCodes.length === 0) {
      const response = await http.get("/wirecodes");
      setWireCodes(response.data);
    }
  }, [wireCodes.length]);

  const loadData = useCallback(async (p = 1, ps = 20) => {
    setLoading(true);
    try {
      const params: any = {
        wireCodeId: debouncedFilters.wireCodeId,
        batchNumber: debouncedFilters.batchNumber || undefined,
        status: debouncedFilters.status,
        page: p,
        pageSize: ps,
        sortBy: sortConfig.sortBy,
        sortDesc: sortConfig.sortDesc
      };

      if (debouncedFilters.fromUtc) {
        params.fromUtc = debouncedFilters.fromUtc.startOf('day').toISOString();
      }
      if (debouncedFilters.toUtc) {
        params.toUtc = debouncedFilters.toUtc.endOf('day').toISOString();
      }

      const response = await http.get("/testresults", { params });
      setData(response.data.items);
      setTotal(response.data.totalCount);
      setPage(response.data.page);
      setPageSize(response.data.pageSize);
    } catch {
      message.error("Не удалось загрузить журнал испытаний");
    } finally {
      setLoading(false);
    }
  }, [debouncedFilters, sortConfig]);

  const handleTableChange = useCallback((newPagination: any, _filters: any, sorter: any) => {
    const actualSorter = Array.isArray(sorter) ? sorter[0] : sorter;
    const newSortBy = actualSorter.order ? actualSorter.field : undefined;
    const newSortDesc = actualSorter.order === "descend";

    const sortChanged = newSortBy !== sortConfig.sortBy || newSortDesc !== sortConfig.sortDesc;
    const pageChanged = newPagination.current !== page || newPagination.pageSize !== pageSize;

    if (sortChanged) {
      setSortConfig({ sortBy: newSortBy, sortDesc: newSortDesc });
      setPage(1);
      setPageSize(newPagination.pageSize);
    } else if (pageChanged) {
      setPage(newPagination.current);
      setPageSize(newPagination.pageSize);
    }
    
    if (sortChanged || pageChanged) {
      window.scrollTo({ top: 0, behavior: "smooth" });
    }
  }, [sortConfig, page, pageSize]);

  useEffect(() => {
    void loadData(page, pageSize);
  }, [loadData, page, pageSize]);

  const handleSearch = useCallback((values: FilterForm) => {
    setFilters(values);
    setPage(1);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }, []);

  const handleReset = useCallback(() => {
    form.resetFields();
    setFilters({});
    setSortConfig({});
    setPage(1);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }, [form]);

  useEffect(() => {
    void loadWireCodes();
  }, []);

  return (
    <Space direction="vertical" size={16} style={{ width: "100%" }}>
      {(auth.role === "Engineer" || auth.role === "Assistant") && auth.laboratoryName && (
        <Card size="small">
          <Typography.Text type="secondary">
            Журнал испытаний лаборатории: <strong>{auth.laboratoryName}</strong>
          </Typography.Text>
        </Card>
      )}
      <Card>
        <Form<FilterForm>
          form={form}
          layout="vertical"
          onFinish={handleSearch}
          initialValues={{ status: undefined }}
        >
          <Row gutter={[16, 0]} align="bottom">
            <Col xs={24} sm={12} md={6}>
              <Form.Item name="fromUtc" label="С">
                <DatePicker showTime style={{ width: "100%" }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} md={6}>
              <Form.Item name="toUtc" label="По">
                <DatePicker showTime style={{ width: "100%" }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} md={4}>
              <Form.Item name="wireCodeId" label="Код">
                <Select
                  allowClear
                  style={{ width: "100%" }}
                  placeholder="Выберите код"
                  options={wireCodes.map((x) => ({ value: x.id, label: x.code }))}
                  onOpenChange={(open) => {
                    if (open) void loadWireCodes();
                  }}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} md={4}>
              <Form.Item name="batchNumber" label="Партия">
                <Input
                  allowClear
                  style={{ width: "100%" }}
                  placeholder="Поиск по номеру партии"
                  onChange={(e) => {
                    setFilters((prev) => ({ ...prev, batchNumber: e.target.value }));
                  }}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} md={4}>
              <Form.Item name="status" label="Статус">
                <Select
                  allowClear
                  style={{ width: "100%" }}
                  options={[
                    { value: 1, label: "В работе" },
                    { value: 2, label: "Завершено" }
                  ]}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={24} md={4}>
              <Form.Item>
                <Space style={{ width: "100%" }}>
                  <Button type="primary" htmlType="submit" block>
                    Поиск
                  </Button>
                  <Button onClick={handleReset} block>
                    Сброс
                  </Button>
                </Space>
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Card>

      <Card>
        <Table<TestResultRow>
          rowKey="id"
          columns={columns}
          dataSource={data}
          loading={loading}
          scroll={{ x: "max-content" }}
          pagination={{
            current: page,
            pageSize: pageSize,
            total: total,
            showSizeChanger: true,
            showTotal: (total) => `Всего: ${total}`,
            pageSizeOptions: ["10", "20", "50", "100"]
          }}
          onChange={handleTableChange}
        />
      </Card>
    </Space>
  );
}