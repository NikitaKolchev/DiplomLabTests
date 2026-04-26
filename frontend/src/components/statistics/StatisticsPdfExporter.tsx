import ReactApexChart from "react-apexcharts";
import type { ApexOptions } from "apexcharts";
import { Modal, Typography, Row, Col, Card, Spin, Space } from "antd";
import dayjs from "dayjs";
import html2canvas from "html2canvas";
import jsPDF from "jspdf";
import { useRef, useState, useMemo } from "react";

const { Text, Title } = Typography;

/* ── Цветовая палитра для отчета (более темная и контрастная) ── */
const C = {
  danger: "#c53030", // Темно-красный
  success: "#2f855a", // Темно-зеленый
  info: "#2b6cb0", // Темно-синий
  accent: "#c05621", // Темно-оранжевый
  purple: "#6b46c1", // Темно-фиолетовый
  teal: "#2c7a7b", // Темно-бирюзовый
  textDark: "#1a202c", // Почти черный для текста
  labelDark: "#2d3748", // Темно-серый для подписей
};

const PALETTE = [C.danger, C.accent, C.purple, C.info, C.success, C.teal, "#9c4221", "#2c5282"];

type Props = {
  open: boolean;
  onClose: () => void;
  overview?: any;
  laboratories: any[];
  wireCodes: any[];
  trends: any[];
  rejectReasons: any[];
  parameterViolations: any[];
  assistantCycles: any[];
  fromUtc: Date;
  toUtc: Date;
};

export default function StatisticsPdfExporter({
  open,
  onClose,
  overview,
  laboratories,
  wireCodes,
  trends,
  rejectReasons,
  parameterViolations,
  assistantCycles,
  fromUtc,
  toUtc,
}: Props) {
  const [exporting, setExporting] = useState(false);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(5);

  // Ссылки на секции для захвата
  const sectionHeader = useRef<HTMLDivElement>(null);
  const sectionOverview = useRef<HTMLDivElement>(null);
  const sectionTrends = useRef<HTMLDivElement>(null);
  const sectionLabs = useRef<HTMLDivElement>(null);
  const sectionWires = useRef<HTMLDivElement>(null);
  const sectionRejectsParams = useRef<HTMLDivElement>(null);
  const sectionEfficiency = useRef<HTMLDivElement>(null);
  const sectionFooter = useRef<HTMLDivElement>(null);

  /* ── Очистка данных для диаграмм ── */
  const cleanedReasons = useMemo(() => {
    return rejectReasons.map(r => {
      let label = r.reason || "Не указана";
      // Очистка мусора
      label = label.replace(/значение обязательно/gi, "").replace(/x+/g, " ");
      
      // Сокращение и перевод типичных фраз
      if (label.includes("is greater than")) {
        label = label.split("is greater than")[0].trim() + " (превышение)";
      } else if (label.includes("is less than")) {
        label = label.split("is less than")[0].trim() + " (занижение)";
      }
      
      label = label.replace(/:/g, "").split(".")[0].trim();

      const mapping: Record<string, string> = {
        "Tensile strength": "Прочность на разрыв",
        "Diameter deviation": "Отклонение диаметра",
        "Yield strength": "Предел текучести",
        "Elongation": "Удлинение",
        "Distance between rib centers": "Расстояние между ребрами"
      };

      Object.entries(mapping).forEach(([k, v]) => {
        if (label.toLowerCase().includes(k.toLowerCase())) label = v;
      });

      return { ...r, cleanLabel: label };
    });
  }, [rejectReasons]);

  const cleanedViolations = useMemo(() => {
    return parameterViolations.map(v => {
      let label = v.parameterName || "Параметр";
      label = label.replace(/значение обязательно/gi, "").replace(/x+/g, " ");
      return { ...v, cleanLabel: label };
    });
  }, [parameterViolations]);

  /* ── Базовые опции для PDF (белый фон, темный текст) ── */
  const baseOptions = (extra: ApexOptions = {}): ApexOptions => ({
    chart: {
      background: "#ffffff",
      toolbar: { show: false },
      animations: { enabled: false },
      ...extra.chart,
    },
    states: { active: { filter: { type: "none" } }, hover: { filter: { type: "none" } } },
    theme: { mode: "light" },
    grid: { borderColor: "#e2e8f0", strokeDashArray: 4 },
    legend: { labels: { colors: C.textDark }, fontSize: "12px", ...extra.legend },
    tooltip: { enabled: false },
    dataLabels: { enabled: false, ...extra.dataLabels },
    ...extra,
  });

  const handleExport = async () => {
    setExporting(true);
    try {
      // Даем время на отрисовку графиков
      await new Promise(resolve => setTimeout(resolve, 1000));

      const pdf = new jsPDF({ orientation: "portrait", unit: "mm", format: "a4" });
      const pageWidth = pdf.internal.pageSize.getWidth();
      const pageHeight = pdf.internal.pageSize.getHeight();
      const margin = 10;
      const contentWidth = pageWidth - margin * 2;

      const captureRef = async (ref: React.RefObject<HTMLDivElement>) => {
        if (!ref.current) return null;
        return await html2canvas(ref.current, {
          scale: 3,
          useCORS: true,
          logging: false,
          backgroundColor: "#ffffff",
        });
      };

      const addFooter = async (pNum: number, tNum: number) => {
        setPageNumber(pNum);
        setTotalPages(tNum);
        // Небольшая задержка для обновления стейта в DOM
        await new Promise(resolve => setTimeout(resolve, 50));
        const footerCanvas = await captureRef(sectionFooter);
        if (footerCanvas) {
          const fWidth = contentWidth;
          const fHeight = (footerCanvas.height * contentWidth) / footerCanvas.width;
          pdf.addImage(footerCanvas.toDataURL("image/png"), "PNG", margin, pageHeight - fHeight - 5, fWidth, fHeight);
        }
      };

      // --- СТРАНИЦА 1: Заголовок + Сводка + Тренды ---
      const headerCanvas = await captureRef(sectionHeader);
      const overviewCanvas = await captureRef(sectionOverview);
      const trendsCanvas = await captureRef(sectionTrends);

      let currentY = margin;
      if (headerCanvas) {
        const hW = contentWidth;
        const hH = (headerCanvas.height * contentWidth) / headerCanvas.width;
        pdf.addImage(headerCanvas.toDataURL("image/png"), "PNG", margin, currentY, hW, hH);
        currentY += hH + 5;
      }
      if (overviewCanvas) {
        const oW = contentWidth;
        const oH = (overviewCanvas.height * contentWidth) / overviewCanvas.width;
        pdf.addImage(overviewCanvas.toDataURL("image/png"), "PNG", margin, currentY, oW, oH);
        currentY += oH + 10;
      }
      if (trendsCanvas) {
        const tW = contentWidth;
        const tH = (trendsCanvas.height * contentWidth) / trendsCanvas.width;
        pdf.addImage(trendsCanvas.toDataURL("image/png"), "PNG", margin, currentY, tW, tH);
      }
      await addFooter(1, 5);

      // --- СТРАНИЦА 2: Лаборатории ---
      pdf.addPage();
      const labsCanvas = await captureRef(sectionLabs);
      if (labsCanvas) {
        const lW = contentWidth;
        const lH = (labsCanvas.height * contentWidth) / labsCanvas.width;
        pdf.addImage(labsCanvas.toDataURL("image/png"), "PNG", margin, margin, lW, lH);
      }
      await addFooter(2, 5);

      // --- СТРАНИЦА 3: Коды проволоки ---
      pdf.addPage();
      const wiresCanvas = await captureRef(sectionWires);
      if (wiresCanvas) {
        const wW = contentWidth;
        const wH = (wiresCanvas.height * contentWidth) / wiresCanvas.width;
        pdf.addImage(wiresCanvas.toDataURL("image/png"), "PNG", margin, margin, wW, wH);
      }
      await addFooter(3, 5);

      // --- СТРАНИЦА 4: Причины брака и Параметры ---
      pdf.addPage();
      const rejectsParamsCanvas = await captureRef(sectionRejectsParams);
      if (rejectsParamsCanvas) {
        const rpW = contentWidth;
        const rpH = (rejectsParamsCanvas.height * contentWidth) / rejectsParamsCanvas.width;
        pdf.addImage(rejectsParamsCanvas.toDataURL("image/png"), "PNG", margin, margin, rpW, rpH);
      }
      await addFooter(4, 5);

      // --- СТРАНИЦА 5: Эффективность персонала ---
      pdf.addPage();
      const efficiencyCanvas = await captureRef(sectionEfficiency);
      if (efficiencyCanvas) {
        const eW = contentWidth;
        const eH = (efficiencyCanvas.height * contentWidth) / efficiencyCanvas.width;
        pdf.addImage(efficiencyCanvas.toDataURL("image/png"), "PNG", margin, margin, eW, eH);
      }
      await addFooter(5, 5);

      pdf.save(`Отчет_статистика_${dayjs().format("YYYY-MM-DD_HH-mm")}.pdf`);
      onClose();
    } catch (err) {
      console.error("PDF Export failed", err);
    } finally {
      setExporting(false);
    }
  };

  const chartHeaderStyle: React.CSSProperties = {
    fontSize: "16px",
    fontWeight: "bold",
    color: C.textDark,
    marginBottom: "12px",
    borderBottom: `2px solid ${C.textDark}`,
    paddingBottom: "6px",
    textTransform: "uppercase",
    letterSpacing: "0.5px"
  };

  const tableStyle: React.CSSProperties = {
    width: "100%",
    borderCollapse: "collapse",
    marginTop: "15px",
    fontSize: "12px",
    color: C.textDark,
  };

  const thStyle: React.CSSProperties = {
    backgroundColor: "#f8fafc",
    border: "1px solid #e2e8f0",
    padding: "8px",
    textAlign: "left",
    fontWeight: "bold",
  };

  const tdStyle: React.CSSProperties = {
    border: "1px solid #e2e8f0",
    padding: "8px",
  };

  return (
    <Modal
      open={open}
      onCancel={onClose}
      title="Подготовка отчета"
      footer={null}
      width={1000}
      destroyOnHidden
    >
      <div style={{ textAlign: "center", padding: "20px 0" }}>
        <Spin spinning={exporting} tip="Генерация PDF отчета...">
          <Space direction="vertical" size="large">
            <Text>Пожалуйста, подождите, мы формируем профессиональный отчет с графиками высокого разрешения.</Text>
            <button 
              onClick={handleExport} 
              disabled={exporting}
              style={{
                padding: "10px 24px",
                background: C.info,
                color: "white",
                border: "none",
                borderRadius: "6px",
                cursor: "pointer",
                fontWeight: "bold"
              }}
            >
              Сгенерировать PDF
            </button>
          </Space>
        </Spin>
      </div>

      {/* Скрытый контейнер для рендеринга графиков перед захватом */}
      <div style={{ position: "absolute", left: "-9999px", top: 0, width: "1000px", background: "white" }}>
        
        {/* SECTION: HEADER */}
        <div ref={sectionHeader} style={{ padding: "20px", borderBottom: `2px solid ${C.textDark}` }}>
          <Title level={2} style={{ margin: 0, color: C.textDark }}>ОТЧЕТ ПО СТАТИСТИКЕ ИСПЫТАНИЙ</Title>
          <Text strong style={{ color: C.labelDark }}>
            Период: {dayjs(fromUtc).format("DD.MM.YYYY")} — {dayjs(toUtc).format("DD.MM.YYYY")}
          </Text>
        </div>

        {/* SECTION: OVERVIEW */}
        <div ref={sectionOverview} style={{ padding: "20px" }}>
          <div style={chartHeaderStyle}>Ключевые показатели</div>
          <Row gutter={[10, 10]}>
            {[
              { label: "Всего испытаний", value: overview?.totalTests ?? 0, color: C.info },
              { label: "Завершено", value: overview?.completedTests ?? 0, color: C.success },
              { label: "Брак", value: overview?.rejectedTests ?? 0, color: C.danger },
              { label: "% брака", value: `${(overview?.rejectRatePercent ?? 0).toFixed(2)}%`, color: C.danger },
              { label: "Средний цикл", value: `${(overview?.avgCycleHours ?? 0).toFixed(1)} ч`, color: C.purple },
            ].map(kpi => (
              <Col span={4} key={kpi.label}>
                <div style={{ border: `1px solid ${kpi.color}`, padding: "10px", borderRadius: "8px", textAlign: "center" }}>
                  <div style={{ fontSize: "20px", fontWeight: "bold", color: kpi.color }}>{kpi.value}</div>
                  <div style={{ fontSize: "10px", color: C.labelDark, fontWeight: "bold" }}>{kpi.label}</div>
                </div>
              </Col>
            ))}
          </Row>
        </div>

        {/* SECTION: TRENDS */}
        <div ref={sectionTrends} style={{ padding: "20px" }}>
          <div style={chartHeaderStyle}>Динамика испытаний за период</div>
          <ReactApexChart
            type="area"
            height={400}
            series={[
              { name: "Всего", data: trends.map(x => x.totalTests), color: C.info },
              { name: "Завершено", data: trends.map(x => x.completedTests), color: C.success },
              { name: "Брак", data: trends.map(x => x.rejectedTests), color: C.danger },
            ]}
            options={baseOptions({
              xaxis: {
                categories: trends.map(x => dayjs(x.periodStartUtc).format("DD.MM")),
                labels: { style: { colors: C.textDark, fontWeight: "bold", fontSize: "12px" } }
              },
              yaxis: { labels: { style: { colors: C.textDark, fontWeight: "bold", fontSize: "12px" } } },
              stroke: { curve: "smooth", width: 4 },
              fill: { type: "gradient", gradient: { shadeIntensity: 1, opacityFrom: 0.45, opacityTo: 0.05 } },
              markers: { size: 5, strokeWidth: 2 }
            })}
          />
        </div>

        {/* SECTION: LABORATORIES */}
        <div ref={sectionLabs} style={{ padding: "20px" }}>
          <div style={chartHeaderStyle}>Аналитика по лабораториям</div>
          <ReactApexChart
            type="bar"
            height={400}
            series={[{ name: "% брака", data: laboratories.slice(0, 10).map(x => +x.rejectRatePercent.toFixed(2)) }]}
            options={baseOptions({
              plotOptions: { bar: { horizontal: false, distributed: true, columnWidth: "60%" } },
              colors: laboratories.slice(0, 10).map(x => x.rejectRatePercent > 15 ? C.danger : C.success),
              xaxis: {
                categories: laboratories.slice(0, 10).map(x => x.laboratoryName),
                labels: { style: { colors: C.textDark, fontWeight: "bold", fontSize: "12px" } }
              },
              yaxis: { labels: { style: { colors: C.textDark, fontWeight: "bold" } }, title: { text: "Процент брака (%)" } },
              dataLabels: { enabled: true, formatter: v => `${v}%`, style: { fontSize: "12px" }, offsetY: -20 }
            })}
          />
          <table style={tableStyle}>
            <thead>
              <tr>
                <th style={thStyle}>Лаборатория</th>
                <th style={thStyle}>Всего</th>
                <th style={thStyle}>Завершено</th>
                <th style={thStyle}>Брак</th>
                <th style={thStyle}>% Брака</th>
                <th style={thStyle}>Ср. цикл</th>
              </tr>
            </thead>
            <tbody>
              {laboratories.slice(0, 10).map((l, i) => (
                <tr key={i}>
                  <td style={tdStyle}>{l.laboratoryName}</td>
                  <td style={tdStyle}>{l.totalTests}</td>
                  <td style={tdStyle}>{l.completedTests}</td>
                  <td style={{ ...tdStyle, color: l.rejectedTests > 0 ? C.danger : C.textDark, fontWeight: l.rejectedTests > 0 ? "bold" : "normal" }}>
                    {l.rejectedTests}
                  </td>
                  <td style={{ ...tdStyle, fontWeight: "bold" }}>{l.rejectRatePercent.toFixed(2)}%</td>
                  <td style={tdStyle}>{l.avgCycleHours.toFixed(1)} ч</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* SECTION: WIRE CODES */}
        <div ref={sectionWires} style={{ padding: "20px" }}>
          <div style={chartHeaderStyle}>Аналитика по кодам проволоки</div>
          <ReactApexChart
            type="bar"
            height={400}
            series={[{ name: "% брака", data: wireCodes.slice(0, 12).map(x => +x.rejectRatePercent.toFixed(2)) }]}
            options={baseOptions({
              plotOptions: { bar: { horizontal: false, distributed: true, columnWidth: "70%" } },
              colors: wireCodes.slice(0, 12).map(x => x.rejectRatePercent > 15 ? C.danger : C.success),
              xaxis: {
                categories: wireCodes.slice(0, 12).map(x => x.wireCode),
                labels: { style: { colors: C.textDark, fontWeight: "bold", fontSize: "11px" }, rotate: -45 }
              },
              yaxis: { labels: { style: { colors: C.textDark, fontWeight: "bold" } } },
              dataLabels: { enabled: true, formatter: v => `${v}%`, style: { fontSize: "10px" }, offsetY: -20 }
            })}
          />
          <table style={tableStyle}>
            <thead>
              <tr>
                <th style={thStyle}>Код проволоки</th>
                <th style={thStyle}>Всего испытаний</th>
                <th style={thStyle}>Брак (кол-во)</th>
                <th style={thStyle}>Процент брака</th>
              </tr>
            </thead>
            <tbody>
              {wireCodes.slice(0, 12).map((w, i) => (
                <tr key={i}>
                  <td style={tdStyle}>{w.wireCode}</td>
                  <td style={tdStyle}>{w.totalTests}</td>
                  <td style={{ ...tdStyle, color: w.rejectedTests > 0 ? C.danger : C.textDark }}>{w.rejectedTests}</td>
                  <td style={{ ...tdStyle, fontWeight: "bold" }}>{w.rejectRatePercent.toFixed(2)}%</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* SECTION: REJECTS & PARAMS */}
        <div ref={sectionRejectsParams} style={{ padding: "20px" }}>
          <div style={chartHeaderStyle}>Детальный анализ отклонений</div>
          <Row gutter={[20, 20]}>
            <Col span={10}>
              <div style={{ ...chartHeaderStyle, borderBottom: "none", fontSize: "14px" }}>Причины брака</div>
              <ReactApexChart
                type="donut"
                height={350}
                series={cleanedReasons.map(x => x.count)}
                options={baseOptions({
                  labels: cleanedReasons.map(x => x.cleanLabel),
                  colors: PALETTE,
                  legend: { position: "bottom" },
                  dataLabels: { enabled: true }
                })}
              />
            </Col>
            <Col span={14}>
              <div style={{ ...chartHeaderStyle, borderBottom: "none", fontSize: "14px" }}>Нарушения лимитов</div>
              <ReactApexChart
                type="bar"
                height={350}
                series={[{ name: "Выходов", data: cleanedViolations.slice(0, 10).map(x => x.outOfSpecCount) }]}
                options={baseOptions({
                  colors: [C.accent],
                  xaxis: {
                    categories: cleanedViolations.slice(0, 10).map(x => x.cleanLabel),
                    labels: { style: { colors: C.textDark, fontWeight: "bold" }, rotate: -45 }
                  },
                  dataLabels: { enabled: true }
                })}
              />
            </Col>
          </Row>
          
          <div style={{ marginTop: "20px" }}>
            <div style={{ ...chartHeaderStyle, borderBottom: "none", fontSize: "14px" }}>Таблица нарушений параметров</div>
            <table style={tableStyle}>
              <thead>
                <tr>
                  <th style={thStyle}>Параметр</th>
                  <th style={thStyle}>Кол-во выходов за лимит</th>
                  <th style={thStyle}>Доля в общем браке</th>
                </tr>
              </thead>
              <tbody>
                {cleanedViolations.slice(0, 10).map((v, i) => (
                  <tr key={i}>
                    <td style={tdStyle}>{v.cleanLabel}</td>
                    <td style={{ ...tdStyle, fontWeight: "bold", color: C.danger }}>{v.outOfSpecCount}</td>
                    <td style={tdStyle}>{((v.outOfSpecCount / (overview?.rejectedTests || 1)) * 100).toFixed(1)}%</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {/* SECTION: PERSONNEL EFFICIENCY (PAGE 5) */}
        <div ref={sectionEfficiency} style={{ padding: "20px" }}>
          <div style={chartHeaderStyle}>Эффективность персонала (Ср. цикл)</div>
          <ReactApexChart
            type="bar"
            height={500}
            series={[{ name: "Ср. цикл (ч)", data: assistantCycles.slice(0, 15).map(x => +x.avgCycleHours.toFixed(1)) }]}
            options={baseOptions({
              plotOptions: { bar: { horizontal: true, borderRadius: 6, barHeight: "60%" } },
              colors: [C.teal],
              xaxis: {
                categories: assistantCycles.slice(0, 15).map(x => x.assistantName),
                labels: { style: { colors: C.textDark, fontWeight: "bold", fontSize: "12px" } }
              },
              yaxis: { labels: { style: { colors: C.textDark, fontWeight: "bold" } } },
              dataLabels: { enabled: true, formatter: v => `${v} ч`, style: { colors: ["#fff"], fontSize: "12px" }, offsetX: 5 }
            })}
          />
          <table style={tableStyle}>
            <thead>
              <tr>
                <th style={thStyle}>Лаборант</th>
                <th style={thStyle}>Лаборатория</th>
                <th style={thStyle}>Испытаний</th>
                <th style={thStyle}>Ср. цикл (ч)</th>
              </tr>
            </thead>
            <tbody>
              {assistantCycles.slice(0, 15).map((a, i) => (
                <tr key={i}>
                  <td style={tdStyle}>{a.assistantName}</td>
                  <td style={tdStyle}>{a.laboratoryName}</td>
                  <td style={tdStyle}>{a.completedTests}</td>
                  <td style={{ ...tdStyle, fontWeight: "bold", color: a.avgCycleHours > 12 ? C.accent : C.success }}>
                    {a.avgCycleHours.toFixed(1)} ч
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <div style={{ marginTop: "15px", fontSize: "11px", color: C.labelDark, fontStyle: "italic" }}>
            * Показатель среднего цикла отражает среднее время обработки одного протокола от создания до завершения.
          </div>
        </div>

        {/* SECTION: FOOTER (для корректной кириллицы через canvas) */}
        <div ref={sectionFooter} style={{ padding: "10px 20px", borderTop: `1px solid ${C.textDark}`, display: "flex", justifyContent: "space-between" }}>
          <Text style={{ fontSize: "10px", color: C.textDark, fontWeight: "bold" }}>
            ОТЧЕТ СФОРМИРОВАН: {dayjs().format("DD.MM.YYYY HH:mm")}
          </Text>
          <Text style={{ fontSize: "10px", color: C.textDark, fontWeight: "bold" }}>
            СТРАНИЦА {pageNumber} ИЗ {totalPages}
          </Text>
        </div>

      </div>
    </Modal>
  );
}
