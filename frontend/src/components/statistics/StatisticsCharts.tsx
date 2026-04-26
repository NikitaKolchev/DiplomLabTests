import ReactApexChart from "react-apexcharts";
import type { ApexOptions } from "apexcharts";
import { Badge, Card, Col, Progress, Row, Skeleton, Tag, Typography } from "antd";
import dayjs from "dayjs";
import { useMemo } from "react";
import { useTheme } from "../../theme/ThemeContext";

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
  laboratoryName: string;
  completedTests: number;
  rejectedTests: number;
  rejectRatePercent: number;
  avgCycleHours: number;
};
type StatisticsWireCode = {
  wireCode: string;
  completedTests: number;
  rejectedTests: number;
  rejectRatePercent: number;
};
type StatisticsParameterViolation = {
  parameterName: string;
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
  assistantName: string;
  laboratoryName: string;
  completedTests: number;
  avgCycleHours: number;
};

type Props = {
  overview?: StatisticsOverview;
  laboratories: StatisticsLaboratory[];
  wireCodes: StatisticsWireCode[];
  parameterViolations: StatisticsParameterViolation[];
  trends: StatisticsTrendPoint[];
  rejectReasons: StatisticsRejectReason[];
  assistantCycles: StatisticsAssistantCycle[];
  loading?: boolean;
};

/* ── Цветовая палитра ── */
const C = {
  danger:  "#e74c3c",
  success: "#2ecc71",
  info:    "#3498db",
  accent:  "#f39c12",
  purple:  "#9b59b6",
  teal:    "#1abc9c",
};

const PALETTE = [C.danger, C.accent, C.purple, C.info, C.success, C.teal, "#e67e22", "#2980b9"];

/* ── Базовые настройки ApexCharts ── */
function baseOptions(isDark: boolean, extra: ApexOptions = {}): ApexOptions {
  return {
    chart: {
      background: "transparent",
      toolbar: { show: false },
      fontFamily: "inherit",
      animations: { enabled: true, speed: 500 },
      ...extra.chart,
    },
    theme: { mode: isDark ? "dark" : "light" },
    grid: {
      borderColor: isDark ? "#2a3444" : "#e2e8f0",
      strokeDashArray: 4,
      xaxis: { lines: { show: false } },
    },
    tooltip: {
      theme: isDark ? "dark" : "light",
      style: { fontSize: "13px" },
      ...extra.tooltip,
    },
    dataLabels: { enabled: false, ...extra.dataLabels },
    legend: {
      labels: { colors: isDark ? "#8899aa" : "#4a5568" },
      ...extra.legend,
    },
    ...extra,
  };
}

function SectionLabel({ children }: { children: React.ReactNode }) {
  const { mode } = useTheme();
  const isDark = mode === "dark";
  
  return (
    <Typography.Text
      strong
      style={{
        fontSize: 11,
        textTransform: "uppercase",
        letterSpacing: "0.08em",
        color: isDark ? "#8899aa" : "#4a5568",
        display: "block",
        marginBottom: 10,
      }}
    >
      {children}
    </Typography.Text>
  );
}

function KpiCard({
  label,
  value,
  color,
  sub,
}: {
  label: string;
  value: string | number;
  color: string;
  sub?: string;
}) {
  const { mode } = useTheme();
  const isDark = mode === "dark";
  
  return (
    <Card
      size="small"
      style={{
        background: isDark ? "#202939" : "#ffffff",
        borderTop: `3px solid ${color}`,
        borderRadius: 10,
        height: "100%",
      }}
    >
      <div style={{ fontSize: 30, fontWeight: 800, color, lineHeight: 1.15 }}>{value}</div>
      <div style={{ fontSize: 11, color: isDark ? "#8899aa" : "#4a5568", marginTop: 4 }}>{label}</div>
      {sub && <div style={{ fontSize: 11, color: isDark ? "#8899aa" : "#4a5568", marginTop: 2 }}>{sub}</div>}
    </Card>
  );
}

function EmptyState({ text = "Нет данных за период" }: { text?: string }) {
  const { mode } = useTheme();
  const isDark = mode === "dark";
  
  return (
    <div
      style={{
        height: 260,
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        gap: 8,
        color: isDark ? "#8899aa" : "#4a5568",
      }}
    >
      <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
        <circle cx="12" cy="12" r="10" />
        <path d="M8 15h8M9 9h.01M15 9h.01" />
      </svg>
      <Typography.Text type="secondary">{text}</Typography.Text>
    </div>
  );
}

export default function StatisticsCharts({
  overview,
  laboratories,
  wireCodes,
  parameterViolations,
  trends,
  rejectReasons,
  assistantCycles,
  loading,
}: Props) {
  const { mode } = useTheme();
  const isDark = mode === "dark";
  
  /* ── Тренд ── */
  const trendCategories = useMemo(
    () => trends.map((x) => dayjs(x.periodStartUtc).format("DD.MM")),
    [trends]
  );
  const trendSeries = useMemo(
    () => [
      { name: "Всего",     data: trends.map((x) => x.totalTests),     color: C.info    },
      { name: "Завершено", data: trends.map((x) => x.completedTests), color: C.success },
      { name: "Брак",      data: trends.map((x) => x.rejectedTests),  color: C.danger  },
    ],
    [trends]
  );
  const trendOptions: ApexOptions = useMemo(
    () =>
      baseOptions(isDark, {
        chart: { type: "area", zoom: { enabled: false } },
        xaxis: {
          categories: trendCategories,
          labels: { style: { colors: isDark ? "#8899aa" : "#4a5568", fontSize: "11px" } },
          axisBorder: { color: isDark ? "#2a3444" : "#e2e8f0" },
          axisTicks: { color: isDark ? "#2a3444" : "#e2e8f0" },
        },
        yaxis: {
          labels: { style: { colors: isDark ? "#8899aa" : "#4a5568" } },
        },
        fill: { type: "gradient", gradient: { opacityFrom: 0.35, opacityTo: 0.05 } },
        stroke: { curve: "smooth", width: 2 },
        legend: { position: "top", horizontalAlign: "right", labels: { colors: isDark ? "#8899aa" : "#4a5568" } },
        tooltip: { shared: true, intersect: false },
      }),
    [trendCategories, isDark]
  );

  /* ── Лаборатории ── */
  const labData = useMemo(
    () =>
      laboratories
        .slice()
        .sort((a, b) => b.rejectRatePercent - a.rejectRatePercent)
        .slice(0, 8),
    [laboratories]
  );
  const labOptions: ApexOptions = useMemo(
    () =>
      baseOptions(isDark, {
        chart: { type: "bar" },
        plotOptions: {
          bar: {
            horizontal: true,
            barHeight: "60%",
            borderRadius: 4,
            distributed: true,
          },
        },
        colors: labData.map((x) =>
          x.rejectRatePercent > 20 ? C.danger : x.rejectRatePercent > 10 ? C.accent : C.success
        ),
        xaxis: {
          categories: labData.map((x) => x.laboratoryName),
          labels: { formatter: (v: string) => `${v}%`, style: { colors: isDark ? "#8899aa" : "#4a5568", fontSize: "11px" } },
          axisBorder: { color: isDark ? "#2a3444" : "#e2e8f0" },
        },
        yaxis: { labels: { style: { colors: isDark ? "#8899aa" : "#4a5568", fontSize: "11px" } } },
        dataLabels: {
          enabled: true,
          formatter: (v: number) => `${v.toFixed(1)}%`,
          style: { fontSize: "11px", colors: ["#fff"] },
          offsetX: 4,
        },
        legend: { show: false },
        tooltip: {
          y: { formatter: (v: number) => `${v.toFixed(2)}%` },
        },
      }),
    [labData, isDark]
  );

  /* ── Коды проволоки ── */
  const wireData = useMemo(
    () =>
      wireCodes
        .slice()
        .sort((a, b) => b.rejectRatePercent - a.rejectRatePercent)
        .slice(0, 8),
    [wireCodes]
  );
  const wireOptions: ApexOptions = useMemo(
    () =>
      baseOptions(isDark, {
        chart: { type: "bar" },
        plotOptions: {
          bar: {
            horizontal: false,
            columnWidth: "55%",
            borderRadius: 4,
            distributed: true,
          },
        },
        colors: wireData.map((x) =>
          x.rejectRatePercent > 20 ? C.danger : x.rejectRatePercent > 10 ? C.accent : C.success
        ),
        xaxis: {
          categories: wireData.map((x) => x.wireCode),
          labels: { style: { colors: isDark ? "#8899aa" : "#4a5568", fontSize: "11px" }, rotate: -35 },
          axisBorder: { color: isDark ? "#2a3444" : "#e2e8f0" },
        },
        yaxis: {
          labels: {
            formatter: (v: number) => `${v}%`,
            style: { colors: isDark ? "#8899aa" : "#4a5568" },
          },
        },
        dataLabels: {
          enabled: true,
          formatter: (v: number) => `${v.toFixed(1)}%`,
          style: { fontSize: "11px", colors: ["#fff"] },
          offsetY: -4,
        },
        legend: { show: false },
        tooltip: { y: { formatter: (v: number) => `${v.toFixed(2)}%` } },
      }),
    [wireData, isDark]
  );

  /* ── Причины брака (donut) ── */
  const cleanedReasons = useMemo(() => {
    return rejectReasons.map(r => {
      let label = r.reason || "Не указана";
      label = label.replace(/значение обязательно/gi, "").replace(/x+/g, " ");
      if (label.includes("is greater than")) {
        label = label.split("is greater than")[0].trim() + " (превышение)";
      } else if (label.includes("is less than")) {
        label = label.split("is less than")[0].trim() + " (занижение)";
      }
      label = label.replace(/:/g, "").split(".")[0].trim();
      const m: Record<string, string> = {
        "Tensile strength": "Прочность на разрыв",
        "Diameter deviation": "Отклонение диаметра",
        "Yield strength": "Предел текучести",
        "Elongation": "Удлинение",
        "Distance between rib centers": "Расстояние между ребрами"
      };
      Object.entries(m).forEach(([k, v]) => {
        if (label.toLowerCase().includes(k.toLowerCase())) label = v;
      });
      return { ...r, cleanLabel: label };
    });
  }, [rejectReasons]);

  const pieOptions: ApexOptions = useMemo(
    () =>
      baseOptions(isDark, {
        chart: { type: "donut" },
        colors: PALETTE,
        labels: cleanedReasons.map((x) => x.cleanLabel),
        plotOptions: {
          pie: {
            donut: {
              size: "62%",
              labels: {
                show: true,
                total: {
                  show: true,
                  label: "Всего",
                  color: isDark ? "#8899aa" : "#4a5568",
                  fontSize: "13px",
                  formatter: () =>
                    String(cleanedReasons.reduce((s, x) => s + x.count, 0)),
                },
                value: { color: isDark ? "#e7edf5" : "#1a202c", fontSize: "22px", fontWeight: "700" },
              },
            },
          },
        },
        legend: {
          position: "bottom",
          labels: { colors: isDark ? "#8899aa" : "#4a5568" },
          fontSize: "11px",
        },
        tooltip: {
          y: {
            formatter: (v: number, opts: { dataPointIndex: number }) => {
              const r = cleanedReasons[opts.dataPointIndex];
              return r ? `${v} случ. (${r.sharePercent.toFixed(1)}%)` : String(v);
            },
          },
        },
        dataLabels: {
          enabled: true,
          formatter: (_v: number, opts: { dataPointIndex: number }) => {
            const r = cleanedReasons[opts.dataPointIndex];
            return r ? `${r.sharePercent.toFixed(1)}%` : "";
          },
          style: { fontSize: "11px" },
          dropShadow: { enabled: false },
        },
      }),
    [cleanedReasons, isDark]
  );

  /* ── Параметры вне лимитов ── */
  const violData = useMemo(
    () =>
      parameterViolations
        .slice()
        .sort((a, b) => b.outOfSpecCount - a.outOfSpecCount)
        .slice(0, 10),
    [parameterViolations]
  );
  const violOptions: ApexOptions = useMemo(
    () =>
      baseOptions(isDark, {
        chart: { type: "bar" },
        plotOptions: {
          bar: {
            horizontal: false,
            columnWidth: "50%",
            borderRadius: 4,
            distributed: true,
          },
        },
        colors: [C.purple],
        xaxis: {
          categories: violData.map((x) => x.parameterName),
          labels: { style: { colors: isDark ? "#8899aa" : "#4a5568", fontSize: "10px" }, rotate: -40 },
          axisBorder: { color: isDark ? "#2a3444" : "#e2e8f0" },
        },
        yaxis: {
          title: { text: "выходов за лимит", style: { color: isDark ? "#8899aa" : "#4a5568", fontSize: "11px" } },
          labels: { style: { colors: isDark ? "#8899aa" : "#4a5568" } },
        },
        dataLabels: {
          enabled: true,
          style: { fontSize: "11px", colors: ["#ddd"] },
          offsetY: -4,
        },
        legend: { show: false },
        tooltip: {
          y: {
            formatter: (v: number, opts: { dataPointIndex: number }) => {
              const d = violData[opts.dataPointIndex];
              return d ? `${v} выходов (${d.sharePercent.toFixed(1)}%)` : String(v);
            },
          },
        },
      }),
    [violData, isDark]
  );

  const maxRejLab  = labData[0];
  const maxRejWire = wireData[0];
  const topReason  = rejectReasons[0];
  const rejectRate = overview?.rejectRatePercent ?? 0;
  const rejectColor = rejectRate < 5 ? C.success : rejectRate < 15 ? C.accent : C.danger;

  if (loading) {
    return (
      <Row gutter={[16, 16]}>
        {[1, 2, 3, 4].map((k) => (
          <Col key={k} xs={24} sm={12}>
            <Card style={{ background: isDark ? "#1a212c" : "#ffffff", borderRadius: 12 }}>
              <Skeleton active paragraph={{ rows: 5 }} />
            </Card>
          </Col>
        ))}
      </Row>
    );
  }

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 24 }}>

      {/* ══ KPI ══ */}
      <div>
        <SectionLabel>Ключевые показатели за период</SectionLabel>
        <Row gutter={[10, 10]}>
          {[
            { label: "Всего испытаний",  value: overview?.totalTests ?? 0,                            color: C.info    },
            { label: "Завершено",        value: overview?.completedTests ?? 0,                        color: C.success },
            { label: "В работе",         value: overview?.inProgressTests ?? 0,                       color: C.accent  },
            { label: "Брак",             value: overview?.rejectedTests ?? 0,                         color: C.danger  },
            { label: "% брака",          value: `${rejectRate.toFixed(2)}%`,                          color: rejectColor },
            { label: "Средний цикл",     value: `${(overview?.avgCycleHours ?? 0).toFixed(1)} ч`,    color: C.purple  },
          ].map(({ label, value, color }) => (
            <Col key={label} xs={12} sm={8} md={4}>
              <KpiCard label={label} value={value} color={color} />
            </Col>
          ))}
        </Row>
      </div>

      {/* ══ Инсайты ══ */}
      {(maxRejLab || maxRejWire || topReason) && (
        <Row gutter={[12, 12]}>
          {maxRejLab && (
            <Col xs={24} sm={8}>
              <Card
                size="small"
                style={{
                  background: isDark ? "#1e1214" : "#fff5f5",
                  border: `1px solid ${C.danger}33`,
                  borderRadius: 10
                }}
              >
                <Badge color={C.danger} text={<span style={{ fontSize: 11, color: isDark ? "#8899aa" : "#4a5568" }}>Лидер по браку — лаборатория</span>} />
                <div style={{ fontWeight: 700, fontSize: 16, marginTop: 6, color: isDark ? "#e7edf5" : "#1a202c" }}>{maxRejLab.laboratoryName}</div>
                <div style={{ color: C.danger, fontWeight: 600 }}>
                  {maxRejLab.rejectRatePercent.toFixed(2)}% брака · {maxRejLab.completedTests} завершено
                </div>
              </Card>
            </Col>
          )}
          {maxRejWire && (
            <Col xs={24} sm={8}>
              <Card
                size="small"
                style={{
                  background: isDark ? "#1e1a10" : "#fffff0",
                  border: `1px solid ${C.accent}33`,
                  borderRadius: 10
                }}
              >
                <Badge color={C.accent} text={<span style={{ fontSize: 11, color: isDark ? "#8899aa" : "#4a5568" }}>Лидер по браку — код проволоки</span>} />
                <div style={{ fontWeight: 700, fontSize: 16, marginTop: 6, color: isDark ? "#e7edf5" : "#1a202c" }}>{maxRejWire.wireCode}</div>
                <div style={{ color: C.accent, fontWeight: 600 }}>
                  {maxRejWire.rejectRatePercent.toFixed(2)}% брака · {maxRejWire.completedTests} завершено
                </div>
              </Card>
            </Col>
          )}
          {topReason && (
            <Col xs={24} sm={8}>
              <Card
                size="small"
                style={{
                  background: isDark ? "#15101e" : "#faf5ff",
                  border: `1px solid ${C.purple}33`,
                  borderRadius: 10
                }}
              >
                <Badge color={C.purple} text={<span style={{ fontSize: 11, color: isDark ? "#8899aa" : "#4a5568" }}>Основная причина брака</span>} />
                <div
                  style={{ fontWeight: 700, fontSize: 15, marginTop: 6, color: isDark ? "#e7edf5" : "#1a202c", wordBreak: "break-word" }}
                  title={topReason.reason}
                >
                  {topReason.reason.length > 55 ? `${topReason.reason.slice(0, 55)}…` : topReason.reason}
                </div>
                <div style={{ color: C.purple, fontWeight: 600 }}>
                  {topReason.count} случаев · {topReason.sharePercent.toFixed(1)}%
                </div>
              </Card>
            </Col>
          )}
        </Row>
      )}

      {/* ══ Тренд ══ */}
      <div>
        <SectionLabel>Динамика испытаний по периоду</SectionLabel>
        <Card style={{ background: isDark ? "#1a212c" : "#ffffff", borderRadius: 12, padding: "4px 0" }}>
          {trends.length > 0 ? (
            <ReactApexChart
              type="area"
              series={trendSeries}
              options={trendOptions}
              height={300}
            />
          ) : (
            <EmptyState />
          )}
        </Card>
      </div>

      {/* ══ Лаборатории + Коды проволоки ══ */}
      <Row gutter={[16, 16]}>
        <Col xs={24} lg={12}>
          <SectionLabel>Лаборатории — % брака (топ 8)</SectionLabel>
          <Card style={{ background: isDark ? "#1a212c" : "#ffffff", borderRadius: 12, padding: "4px 0" }}>
            {labData.length > 0 ? (
              <ReactApexChart
                type="bar"
                series={[{ name: "% брака", data: labData.map((x) => +x.rejectRatePercent.toFixed(2)) }]}
                options={labOptions}
                height={320}
              />
            ) : (
              <EmptyState />
            )}
          </Card>
        </Col>

        <Col xs={24} lg={12}>
          <SectionLabel>Коды проволоки — % брака (топ 8)</SectionLabel>
          <Card style={{ background: isDark ? "#1a212c" : "#ffffff", borderRadius: 12, padding: "4px 0" }}>
            {wireData.length > 0 ? (
              <ReactApexChart
                type="bar"
                series={[{ name: "% брака", data: wireData.map((x) => +x.rejectRatePercent.toFixed(2)) }]}
                options={wireOptions}
                height={320}
              />
            ) : (
              <EmptyState />
            )}
          </Card>
        </Col>
      </Row>

      {/* ══ Причины брака + Параметры ══ */}
      <Row gutter={[16, 16]}>
        <Col xs={24} lg={10}>
          <SectionLabel>Причины брака — доля</SectionLabel>
          <Card style={{ background: isDark ? "#1a212c" : "#ffffff", borderRadius: 12, padding: "4px 0" }}>
            {cleanedReasons.length > 0 ? (
              <ReactApexChart
                type="donut"
                series={cleanedReasons.map((x) => x.count)}
                options={pieOptions}
                height={340}
              />
            ) : (
              <EmptyState text="Браков за период нет" />
            )}
          </Card>
        </Col>

        <Col xs={24} lg={14}>
          <SectionLabel>Параметры с нарушением лимитов (топ 10)</SectionLabel>
          <Card style={{ background: isDark ? "#1a212c" : "#ffffff", borderRadius: 12, padding: "4px 0" }}>
            {violData.length > 0 ? (
              <ReactApexChart
                type="bar"
                series={[{ name: "Выходов", data: violData.map((x) => x.outOfSpecCount) }]}
                options={violOptions}
                height={340}
              />
            ) : (
              <EmptyState text="Нарушений лимитов нет" />
            )}
          </Card>
        </Col>
      </Row>

      {/* ══ Время цикла ══ */}
      {assistantCycles.length > 0 && (
        <div>
          <SectionLabel>Средний цикл протокола по лаборантам</SectionLabel>
          <Card style={{ background: isDark ? "#1a212c" : "#ffffff", borderRadius: 12 }}>
            <div style={{ display: "flex", flexDirection: "column", gap: 14 }}>
              {assistantCycles.slice(0, 10).map((a) => {
                const maxH = Math.max(...assistantCycles.slice(0, 10).map((x) => x.avgCycleHours), 1);
                const pct  = Math.round((a.avgCycleHours / maxH) * 100);
                const col  = pct < 40 ? C.success : pct < 70 ? C.accent : C.danger;
                return (
                  <div
                    key={`${a.assistantName}-${a.laboratoryName}`}
                    style={{ display: "flex", alignItems: "center", gap: 14 }}
                  >
                    <div style={{ minWidth: 200, flex: "0 0 200px" }}>
                      <div style={{ fontWeight: 600, fontSize: 13, color: isDark ? "#e7edf5" : "#1a202c" }}>{a.assistantName}</div>
                      <div style={{ fontSize: 11, color: isDark ? "#8899aa" : "#4a5568" }}>
                        {a.laboratoryName} · {a.completedTests} пр.
                      </div>
                    </div>
                    <Progress
                      percent={pct}
                      showInfo={false}
                      strokeColor={col}
                      trailColor={isDark ? "#2a3444" : "#e2e8f0"}
                      style={{ flex: 1, margin: 0 }}
                    />
                    <Tag
                      color={col}
                      style={{ minWidth: 60, textAlign: "center", fontWeight: 700, fontSize: 13 }}
                    >
                      {a.avgCycleHours.toFixed(1)} ч
                    </Tag>
                  </div>
                );
              })}
            </div>
          </Card>
        </div>
      )}
    </div>
  );
}
