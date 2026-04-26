import {
  Button,
  Card,
  Col,
  DatePicker,
  Form,
  Row,
  Select,
  Space,
  Spin,
  Tabs,
  Typography,
  message
} from "antd";
import { useState } from "react";
import dayjs from "dayjs";
import { Suspense, lazy, useEffect } from "react";
import { http } from "../api/http";
import { downloadBlob } from "../utils/download";
import StatisticsPdfExporter from "../components/statistics/StatisticsPdfExporter";

type FormMonthly = { period: dayjs.Dayjs };
type FormDetailed = {
  period: [dayjs.Dayjs, dayjs.Dayjs];
  laboratoryId?: number;
  wireCodeId?: number;
};
type StatisticsForm = {
  period: [dayjs.Dayjs, dayjs.Dayjs];
  groupBy: "Day" | "Week" | "Month";
};

type Filters = { laboratories: { id: number; name: string }[]; wireCodes: { id: number; code: string }[] };
type StatisticsOverview = {
  totalTests: number;
  completedTests: number;
  inProgressTests: number;
  rejectedTests: number;
  rejectRatePercent: number;
  acceptanceRatePercent: number;
  avgCycleHours: number;
};
type StatisticsLaboratory = {
  laboratoryId: number;
  laboratoryName: string;
  completedTests: number;
  rejectedTests: number;
  rejectRatePercent: number;
  avgCycleHours: number;
};
type StatisticsWireCode = {
  wireCodeId: number;
  wireCode: string;
  completedTests: number;
  rejectedTests: number;
  rejectRatePercent: number;
};
type StatisticsParameterViolation = {
  parameterId: number;
  parameterName: string;
  unit?: string;
  outOfSpecCount: number;
  sharePercent: number;
};
type StatisticsTrendPoint = {
  periodStartUtc: string;
  totalTests: number;
  completedTests: number;
  rejectedTests: number;
};
type StatisticsRejectReason = {
  reason: string;
  count: number;
  sharePercent: number;
};
type StatisticsAssistantCycle = {
  assistantId: number;
  assistantName: string;
  laboratoryName: string;
  completedTests: number;
  avgCycleHours: number;
};

type StatisticsResponse = {
  overview: StatisticsOverview;
  laboratories: StatisticsLaboratory[];
  wireCodes: StatisticsWireCode[];
  parameterViolations: StatisticsParameterViolation[];
  trends: StatisticsTrendPoint[];
  rejectReasons: StatisticsRejectReason[];
  assistantCycles: StatisticsAssistantCycle[];
};

const StatisticsCharts = lazy(() => import("../components/statistics/StatisticsCharts"));

export default function ReportsPage() {
  const [activeTab, setActiveTab] = useState("monthly");
  const [filters, setFilters] = useState<Filters>({ laboratories: [], wireCodes: [] });
  const [loading, setLoading] = useState(false);
  const [statisticsLoading, setStatisticsLoading] = useState(false);
  const [statistics, setStatistics] = useState<StatisticsResponse | null>(null);
  const [statisticsForm] = Form.useForm<StatisticsForm>();
  const [pdfModalOpen, setPdfModalOpen] = useState(false);
  const [statsPeriod, setStatsPeriod] = useState<[dayjs.Dayjs, dayjs.Dayjs]>([
    dayjs().startOf("month"),
    dayjs().endOf("month")
  ]);

  const loadFilters = async () => {
    try {
      const res = await http.get<{ laboratories: { id: number; name: string }[]; wireCodes: { id: number; code: string }[] }>("/reports/filters");
      setFilters({
        laboratories: res.data.laboratories ?? [],
        wireCodes: res.data.wireCodes ?? []
      });
    } catch {
      message.error("Не удалось загрузить фильтры");
    }
  };

  useEffect(() => {
    void loadFilters();
  }, []);

  const loadStatistics = async (values: StatisticsForm) => {
    setStatisticsLoading(true);
    try {
      setStatsPeriod(values.period);
      const [from, to] = values.period;
      const response = await http.get<StatisticsResponse>("/statistics", {
        params: {
          fromUtc: from.toISOString(),
          toUtc: to.toISOString(),
          groupBy: values.groupBy
        }
      });
      setStatistics(response.data);
    } catch {
      message.error("Не удалось загрузить статистику");
      setStatistics(null);
    } finally {
      setStatisticsLoading(false);
    }
  };

  const onDownloadPdf = () => {
    if (!statistics) {
      message.warning("Сначала загрузите статистику");
      return;
    }
    setPdfModalOpen(true);
  };

  const onMonthlySubmit = async (values: FormMonthly) => {
    setLoading(true);
    try {
      const year = values.period.year();
      const month = values.period.month() + 1;
      await downloadBlob(
        `/reports/monthly-journal?year=${year}&month=${month}`,
        `journal-${year}-${String(month).padStart(2, "0")}.xlsx`
      );
      message.success("Скачан журнал за месяц");
    } catch {
      message.error("Не удалось сформировать отчет");
    } finally {
      setLoading(false);
    }
  };

  const onDetailedSubmit = async (values: FormDetailed) => {
    setLoading(true);
    try {
      const [from, to] = values.period;
      const params = new URLSearchParams({
        fromUtc: from.toISOString(),
        toUtc: to.toISOString()
      });
      if (values.laboratoryId) params.set("laboratoryId", String(values.laboratoryId));
      if (values.wireCodeId) params.set("wireCodeId", String(values.wireCodeId));
      await downloadBlob(
        `/reports/detailed-journal?${params}`,
        `journal-${from.format("YYYY-MM-DD")}-${to.format("YYYY-MM-DD")}.xlsx`
      );
      message.success("Скачан детальный журнал");
    } catch {
      message.error("Не удалось сформировать отчет");
    } finally {
      setLoading(false);
    }
  };

  const tabItems = [
    {
      key: "monthly",
      label: "Журнал за месяц",
      children: (
        <Card>
          <Typography.Text type="secondary" style={{ display: "block", marginBottom: 16 }}>
            Скачать журнал испытаний в Excel за выбранный месяц. Включает сводку, данные по лабораториям и статусы.
          </Typography.Text>
          <Form<FormMonthly>
            layout="vertical"
            onFinish={onMonthlySubmit}
            initialValues={{ period: dayjs() }}
          >
            <Row gutter={[16, 0]} align="bottom">
              <Col xs={24} sm={12} md={8}>
                <Form.Item name="period" label="Месяц" rules={[{ required: true }]}>
                  <DatePicker picker="month" style={{ width: "100%" }} />
                </Form.Item>
              </Col>
              <Col xs={24} sm={12} md={6}>
                <Form.Item>
                  <Button type="primary" htmlType="submit" loading={loading} block>
                    Скачать Excel
                  </Button>
                </Form.Item>
              </Col>
            </Row>
          </Form>
        </Card>
      )
    },
    {
      key: "detailed",
      label: "Детальный журнал",
      children: (
        <Card>
          <Typography.Text type="secondary" style={{ display: "block", marginBottom: 16 }}>
            Скачать журнал за произвольный период с фильтрами по лаборатории и коду проволоки. Сводная статистика.
          </Typography.Text>
          <Form<FormDetailed>
            layout="vertical"
            onFinish={onDetailedSubmit}
            initialValues={{
              period: [dayjs().startOf("month"), dayjs().endOf("month")]
            }}
          >
            <Row gutter={[16, 0]} align="bottom">
              <Col xs={24} sm={24} md={10}>
                <Form.Item name="period" label="Период" rules={[{ required: true }]}>
                  <DatePicker.RangePicker showTime style={{ width: "100%" }} />
                </Form.Item>
              </Col>
              <Col xs={24} sm={12} md={7}>
                <Form.Item name="laboratoryId" label="Лаборатория">
                  <Select
                    allowClear
                    showSearch
                    optionFilterProp="label"
                    placeholder="Все лаборатории"
                    style={{ width: "100%" }}
                    options={filters.laboratories.map((l) => ({ value: l.id, label: l.name }))}
                  />
                </Form.Item>
              </Col>
              <Col xs={24} sm={12} md={7}>
                <Form.Item name="wireCodeId" label="Код проволоки">
                  <Select
                    allowClear
                    showSearch
                    optionFilterProp="label"
                    placeholder="Все коды"
                    style={{ width: "100%" }}
                    options={filters.wireCodes.map((w) => ({ value: w.id, label: w.code }))}
                  />
                </Form.Item>
              </Col>
              <Col xs={24} sm={24} md={6}>
                <Form.Item>
                  <Button type="primary" htmlType="submit" loading={loading} block>
                    Скачать Excel
                  </Button>
                </Form.Item>
              </Col>
            </Row>
          </Form>
        </Card>
      )
    },
    {
      key: "statistics",
      label: "Статистика",
      children: (
        <Space direction="vertical" size={16} style={{ width: "100%" }}>
          <Card>
            <Form<StatisticsForm>
              form={statisticsForm}
              layout="vertical"
              onFinish={(values) => void loadStatistics(values)}
              initialValues={{
                period: [dayjs().startOf("month"), dayjs().endOf("month")],
                groupBy: "Day"
              }}
            >
              <Row gutter={[16, 0]} align="bottom">
                <Col xs={24} sm={14} md={10}>
                  <Form.Item name="period" label="Период" rules={[{ required: true }]}>
                    <DatePicker.RangePicker showTime style={{ width: "100%" }} />
                  </Form.Item>
                </Col>
                <Col xs={24} sm={10} md={6}>
                  <Form.Item name="groupBy" label="Тренд">
                    <Select
                      style={{ width: "100%" }}
                      options={[
                        { value: "Day", label: "По дням" },
                        { value: "Week", label: "По неделям" },
                        { value: "Month", label: "По месяцам" }
                      ]}
                    />
                  </Form.Item>
                </Col>
                <Col xs={24} sm={24} md={8}>
                  <Form.Item>
                    <Space style={{ width: "100%" }}>
                      <Button type="primary" htmlType="submit" loading={statisticsLoading} block>
                        Обновить
                      </Button>
                      <Button onClick={onDownloadPdf} disabled={!statistics} block>
                        Скачать PDF
                      </Button>
                    </Space>
                  </Form.Item>
                </Col>
              </Row>
            </Form>
          </Card>

          <Suspense fallback={<Card><Spin /></Card>}>
            <StatisticsCharts
              loading={statisticsLoading}
              overview={statistics?.overview}
              laboratories={statistics?.laboratories ?? []}
              wireCodes={statistics?.wireCodes ?? []}
              trends={statistics?.trends ?? []}
              rejectReasons={statistics?.rejectReasons ?? []}
              parameterViolations={statistics?.parameterViolations ?? []}
              assistantCycles={statistics?.assistantCycles ?? []}
            />
          </Suspense>
        </Space>
      )
    }
  ];

  return (
    <Space direction="vertical" size={16} style={{ width: "100%" }}>
      <Typography.Title level={4}>Отчеты</Typography.Title>
      <Tabs
        activeKey={activeTab}
        onChange={(key) => {
          setActiveTab(key);
          if (key === "statistics" && !statistics && !statisticsLoading) {
            void loadStatistics({
              period: [dayjs().startOf("month"), dayjs().endOf("month")],
              groupBy: "Day"
            });
          }
        }}
        items={tabItems}
      />
      <StatisticsPdfExporter
        open={pdfModalOpen}
        onClose={() => setPdfModalOpen(false)}
        overview={statistics?.overview}
        laboratories={statistics?.laboratories ?? []}
        wireCodes={statistics?.wireCodes ?? []}
        trends={statistics?.trends ?? []}
        rejectReasons={statistics?.rejectReasons ?? []}
        parameterViolations={statistics?.parameterViolations ?? []}
        assistantCycles={statistics?.assistantCycles ?? []}
        fromUtc={statsPeriod[0].toDate()}
        toUtc={statsPeriod[1].toDate()}
      />
    </Space>
  );
}
