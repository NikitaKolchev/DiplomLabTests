import { MenuOutlined, MoonOutlined, SunOutlined } from "@ant-design/icons";
import {
  Button,
  Card,
  Col,
  DatePicker,
  Form,
  Input,
  Layout,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  message,
  theme
} from "antd";
import type { ColumnsType } from "antd/es/table";
import dayjs from "dayjs";
import utc from "dayjs/plugin/utc";
import { useEffect, useMemo, useState } from "react";

dayjs.extend(utc);
import { useNavigate } from "react-router-dom";
import { http } from "../api/http";
import { useTheme } from "../theme/ThemeContext";
import ThemeToggleButton from "../components/ThemeToggleButton";

const { useToken } = theme;

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
  page?: number;
};

type ApiResponse = {
  items: TestResultRow[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
};

export default function GuestJournalPage() {
  const { token } = useToken();
  const navigate = useNavigate();
  const [data, setData] = useState<TestResultRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [wireCodes, setWireCodes] = useState<{ id: number; code: string }[]>([]);
  const [pagination, setPagination] = useState({ page: 1, pageSize: 20, total: 0, totalPages: 0 });
  const { mode, toggleTheme } = useTheme();
  const isDark = mode === "dark";

  const columns = useMemo<ColumnsType<TestResultRow>>(
    () => [
      {
        title: "Дата",
        dataIndex: "date",
        render: (v: string) => dayjs.utc(v).local().format("YYYY-MM-DD HH:mm"),
        width: 170
      },
      { title: "Партия", dataIndex: "batchNumber" },
      { title: "Код проволоки", dataIndex: "wireCode" },
      { title: "Лаборант", dataIndex: "assistant" },
      {
        title: "Статус",
        dataIndex: "status",
        render: (value: TestResultRow["status"]) =>
          value === 2 ? <Tag color="success">Завершено</Tag> : <Tag color="processing">В работе</Tag>
      },
    ],
    []
  );

  const loadWireCodes = async () => {
    try {
      const response = await http.get("/wirecodes");
      setWireCodes(response.data);
    } catch {
      // ignore
    }
  };

  const loadData = async (page = 1, pageSize = 20, filters?: FilterForm) => {
    setLoading(true);
    try {
      const response = await http.get<ApiResponse>("/testresults", {
        params: {
          fromUtc: filters?.fromUtc?.toISOString(),
          toUtc: filters?.toUtc?.toISOString(),
          wireCodeId: filters?.wireCodeId,
          batchNumber: filters?.batchNumber || undefined,
          status: filters?.status,
          page,
          pageSize
        }
      });
      
      const result = response.data;
      if (result && typeof result === 'object' && 'items' in result) {
        setData(result.items);
        setPagination({
          page: result.page,
          pageSize: result.pageSize,
          total: result.totalCount,
          totalPages: result.totalPages
        });
      } else if (Array.isArray(result)) {
        setData(result);
      } else {
        setData([]);
      }
    } catch {
      message.error("Не удалось загрузить журнал испытаний");
      setData([]);
    } finally {
      setLoading(false);
    }
  };

  const [filters, setFilters] = useState<FilterForm>({});

  useEffect(() => {
    void loadData(1, 20, filters);
    void loadWireCodes();
  }, []);

  const handleSearch = (values: FilterForm) => {
    setFilters(values);
    void loadData(1, 20, values);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  return (
    <Layout style={{ minHeight: "100vh" }}>
      <Layout.Header
        style={{
          display: "flex",
          alignItems: "center",
          gap: 16,
          borderBottom: `1px solid ${token.colorBorderSecondary}`,
          background: token.colorBgContainer,
          padding: "0 24px"
        }}
      >
        <Typography.Text strong style={{ flex: 1 }}>
          БМЗ Лабораторные испытания — Просмотр журнала
        </Typography.Text>
        
        <ThemeToggleButton />
        
        <Button type="primary" onClick={() => navigate("/login")}>
          Войти в систему
        </Button>
      </Layout.Header>
      <Layout.Content style={{ padding: 24 }}>
        <Space direction="vertical" size={16} style={{ width: "100%" }}>
          <Card>
            <Typography.Text type="secondary">
              Просмотр журнала испытаний без входа. Для редактирования и создания протоколов войдите в систему.
            </Typography.Text>
            <Form<FilterForm>
              layout="vertical"
              onFinish={handleSearch}
              initialValues={{ status: undefined }}
              style={{ marginTop: 16 }}
              onValuesChange={(_, all) => {
                if (wireCodes.length === 0) void loadWireCodes();
                void handleSearch(all);
              }}
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
                      options={wireCodes.map((x) => ({ value: x.id, label: x.code }))}
                      onOpenChange={(open) =>
                        open && wireCodes.length === 0 && void loadWireCodes()
                      }
                    />
                  </Form.Item>
                </Col>
                <Col xs={24} sm={12} md={4}>
                  <Form.Item name="batchNumber" label="Партия">
                    <Input allowClear style={{ width: "100%" }} />
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
                <Col xs={24} sm={24} md={2}>
                  <Form.Item>
                    <Button type="primary" htmlType="submit" block>
                      Поиск
                    </Button>
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
                current: pagination.page,
                pageSize: pagination.pageSize,
                total: pagination.total,
                showSizeChanger: true,
                showTotal: (total) => `Всего: ${total}`,
                onChange: (page, pageSize) => {
                  void loadData(page, pageSize);
                  window.scrollTo({ top: 0, behavior: "smooth" });
                }
              }}
            />
          </Card>
        </Space>
      </Layout.Content>
    </Layout>
  );
}
