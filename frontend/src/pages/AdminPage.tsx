import {
  Button,
  Card,
  Form,
  Input,
  InputNumber,
  Modal,
  Popconfirm,
  Row,
  Col,
  Select,
  Space,
  Switch,
  Table,
  Tabs,
  Tag,
  message
} from "antd";
import type { ColumnsType } from "antd/es/table";
import { useEffect, useMemo, useState } from "react";
import { http } from "../api/http";

type Country = { id: number; name: string };
type Customer = { id: number; name: string; countryId?: number; countryName?: string };
type WireCode = { id: number; code: string; marking: string; diameter: number };
type Parameter = { id: number; name: string; dataType: 1 | 2; unit?: string };
type Limit = { parameterId: number; isRequired: boolean; minValue?: number; maxValue?: number };

export default function AdminPage() {
  const [countries, setCountries] = useState<Country[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [wireCodes, setWireCodes] = useState<WireCode[]>([]);
  const [parameters, setParameters] = useState<Parameter[]>([]);
  const [limits, setLimits] = useState<Limit[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedWireCodeId, setSelectedWireCodeId] = useState<number>();
  const [generationCount, setGenerationCount] = useState(100);
  const [generating, setGenerating] = useState(false);

  const [countrySearch, setCountrySearch] = useState("");
  const [customerSearch, setCustomerSearch] = useState("");
  const [wireCodeSearch, setWireCodeSearch] = useState("");
  const [parameterSearch, setParameterSearch] = useState("");

  const [countryModal, setCountryModal] = useState<{ open: boolean; item?: Country }>({ open: false });
  const [customerModal, setCustomerModal] = useState<{ open: boolean; item?: Customer }>({ open: false });
  const [wireCodeModal, setWireCodeModal] = useState<{ open: boolean; item?: WireCode }>({ open: false });
  const [parameterModal, setParameterModal] = useState<{ open: boolean; item?: Parameter }>({ open: false });

  const [countryForm] = Form.useForm<{ name: string }>();
  const [customerForm] = Form.useForm<{ name: string; countryId?: number }>();
  const [wireCodeForm] = Form.useForm<{ code: string; marking: string; diameter: number }>();
  const [parameterForm] = Form.useForm<{ name: string; dataType: 1 | 2; unit?: string }>();

   const loadCountries = async (search = countrySearch) => {
    try {
      const response = await http.get("/countries", { params: { searchTerm: search } });
      setCountries(response.data);
    } catch {
      message.error("Не удалось загрузить страны");
    }
  };

  const loadCustomers = async (search = customerSearch) => {
    try {
      const response = await http.get("/customers", { params: { searchTerm: search } });
      setCustomers(response.data);
    } catch {
      message.error("Не удалось загрузить потребителей");
    }
  };

  const loadWireCodes = async (search = wireCodeSearch) => {
    try {
      const response = await http.get("/wirecodes", { params: { searchTerm: search } });
      setWireCodes(response.data);
    } catch {
      message.error("Не удалось загрузить коды проволоки");
    }
  };

  const loadParameters = async (search = parameterSearch) => {
    try {
      const response = await http.get("/parameters", { params: { searchTerm: search } });
      setParameters(response.data);
    } catch {
      message.error("Не удалось загрузить параметры");
    }
  };

  const loadAll = async () => {
    setLoading(true);
    try {
      await Promise.all([
        loadCountries(""),
        loadCustomers(""),
        loadWireCodes(""),
        loadParameters("")
      ]);
      setCountrySearch("");
      setCustomerSearch("");
      setWireCodeSearch("");
      setParameterSearch("");
    } finally {
      setLoading(false);
    }
  };

  const loadLimits = async (wireCodeId: number) => {
    try {
      const response = await http.get(`/wire-codes/${wireCodeId}/limits`);
      setLimits(response.data);
    } catch {
      message.error("Не удалось загрузить лимиты");
    }
  };

  useEffect(() => {
    void loadAll();
  }, []);

  const limitRows = useMemo(() => {
    const map = new Map<number, Limit>(limits.map((l) => [l.parameterId, l]));
    return parameters.map((p) => {
      const l = map.get(p.id);
      return {
        parameterId: p.id,
        parameterName: p.name,
        dataType: p.dataType,
        unit: p.unit,
        isRequired: l?.isRequired ?? false,
        minValue: l?.minValue,
        maxValue: l?.maxValue
      };
    });
  }, [parameters, limits]);

  const setLimitValue = (parameterId: number, patch: Partial<Limit>) => {
    setLimits((prev) => {
      const existing = prev.find((x) => x.parameterId === parameterId);
      if (!existing) {
        return [...prev, { parameterId, isRequired: false, ...patch }];
      }
      return prev.map((x) => (x.parameterId === parameterId ? { ...x, ...patch } : x));
    });
  };

  const saveLimits = async () => {
    if (!selectedWireCodeId) {
      message.warning("Выберите код проволоки");
      return;
    }
    const items = limits.filter((x) => x.isRequired || x.minValue !== undefined || x.maxValue !== undefined);
    
    try {
      await http.post(`/wire-codes/${selectedWireCodeId}/limits/validate`, { items });
    } catch (err: any) {
      const errorMsg = err?.response?.data?.error;
      if (errorMsg) {
        message.warning(errorMsg);
        return;
      }
    }
    
    try {
      await http.put(`/wire-codes/${selectedWireCodeId}/limits`, { items });
      message.success("Лимиты сохранены");
      await loadLimits(selectedWireCodeId);
    } catch {
      message.error("Не удалось сохранить лимиты");
    }
  };

  const countryColumns: ColumnsType<Country> = [
    { title: "Название", dataIndex: "name", sorter: (a, b) => (a.name || "").localeCompare(b.name || "") },
    {
      title: "Действия",
      key: "actions",
      width: 180,
      render: (_, row) => (
        <Space>
          <Button size="small" onClick={() => openCountryModal(row)}>
            Изменить
          </Button>
          <Popconfirm title="Удалить страну?" onConfirm={() => deleteCountry(row.id)}>
            <Button size="small" danger>
              Удалить
            </Button>
          </Popconfirm>
        </Space>
      )
    }
  ];

  const customerColumns: ColumnsType<Customer> = [
    { title: "Название", dataIndex: "name", sorter: (a, b) => (a.name || "").localeCompare(b.name || "") },
    { title: "Страна", dataIndex: "countryName", render: (v?: string) => v ?? "-", sorter: (a, b) => (a.countryName ?? "").localeCompare(b.countryName ?? "") },
    {
      title: "Действия",
      key: "actions",
      width: 180,
      render: (_, row) => (
        <Space>
          <Button size="small" onClick={() => openCustomerModal(row)}>
            Изменить
          </Button>
          <Popconfirm title="Удалить потребителя?" onConfirm={() => deleteCustomer(row.id)}>
            <Button size="small" danger>
              Удалить
            </Button>
          </Popconfirm>
        </Space>
      )
    }
  ];

  const wireCodeColumns: ColumnsType<WireCode> = [
    { title: "Код", dataIndex: "code", sorter: (a, b) => (a.code || "").localeCompare(b.code || "") },
    { title: "Маркировка", dataIndex: "marking", sorter: (a, b) => (a.marking || "").localeCompare(b.marking || "") },
    { title: "Диаметр", dataIndex: "diameter", sorter: (a, b) => a.diameter - b.diameter },
    {
      title: "Действия",
      key: "actions",
      width: 180,
      render: (_, row) => (
        <Space>
          <Button size="small" onClick={() => openWireCodeModal(row)}>
            Изменить
          </Button>
          <Popconfirm title="Удалить код проволоки?" onConfirm={() => deleteWireCode(row.id)}>
            <Button size="small" danger>
              Удалить
            </Button>
          </Popconfirm>
        </Space>
      )
    }
  ];

  const parameterColumns: ColumnsType<Parameter> = [
    { title: "Параметр", dataIndex: "name", sorter: (a, b) => (a.name || "").localeCompare(b.name || "") },
    { title: "Тип", dataIndex: "dataType", render: (v: 1 | 2) => (v === 1 ? "Число" : "Текст"), sorter: (a, b) => a.dataType - b.dataType },
    { title: "Ед.", dataIndex: "unit", render: (v?: string) => v ?? "-", sorter: (a, b) => (a.unit ?? "").localeCompare(b.unit ?? "") },
    {
      title: "Действия",
      key: "actions",
      width: 180,
      render: (_, row) => (
        <Space>
          <Button size="small" onClick={() => openParameterModal(row)}>
            Изменить
          </Button>
          <Popconfirm title="Удалить параметр?" onConfirm={() => deleteParameter(row.id)}>
            <Button size="small" danger>
              Удалить
            </Button>
          </Popconfirm>
        </Space>
      )
    }
  ];

  const limitColumns: ColumnsType<(typeof limitRows)[number]> = [
    { title: "Параметр", dataIndex: "parameterName" },
    { title: "Тип", dataIndex: "dataType", render: (v: 1 | 2) => (v === 1 ? "Число" : "Текст"), width: 100 },
    { title: "Ед.", dataIndex: "unit", render: (v?: string) => v ?? "-", width: 90 },
    {
      title: "Обяз.",
      dataIndex: "isRequired",
      width: 100,
      render: (_, row) => (
        <Switch
          checked={row.isRequired}
          onChange={(checked) => setLimitValue(row.parameterId, { isRequired: checked })}
        />
      )
    },
    {
      title: "Мин",
      width: 140,
      render: (_, row) => (
        <InputNumber
          disabled={row.dataType !== 1}
          value={row.minValue}
          onChange={(v) => setLimitValue(row.parameterId, { minValue: v === null ? undefined : v })}
        />
      )
    },
    {
      title: "Макс",
      width: 140,
      render: (_, row) => (
        <InputNumber
          disabled={row.dataType !== 1}
          value={row.maxValue}
          onChange={(v) => setLimitValue(row.parameterId, { maxValue: v === null ? undefined : v })}
        />
      )
    }
  ];

  const openCountryModal = (item?: Country) => {
    setCountryModal({ open: true, item });
    countryForm.setFieldsValue({ name: item?.name ?? "" });
  };
  const openCustomerModal = (item?: Customer) => {
    setCustomerModal({ open: true, item });
    customerForm.setFieldsValue({ name: item?.name ?? "", countryId: item?.countryId });
  };
  const openWireCodeModal = (item?: WireCode) => {
    setWireCodeModal({ open: true, item });
    wireCodeForm.setFieldsValue({ code: item?.code ?? "", marking: item?.marking ?? "", diameter: item?.diameter ?? 0.1 });
  };
  const openParameterModal = (item?: Parameter) => {
    setParameterModal({ open: true, item });
    parameterForm.setFieldsValue({ name: item?.name ?? "", dataType: item?.dataType ?? 1, unit: item?.unit });
  };

  const saveCountry = async () => {
    const values = await countryForm.validateFields();
    try {
      if (countryModal.item) {
        await http.put(`/countries/${countryModal.item.id}`, values);
      } else {
        await http.post("/countries", values);
      }
      setCountryModal({ open: false });
      await loadAll();
    } catch {
      message.error("Не удалось сохранить страну");
    }
  };

  const saveCustomer = async () => {
    const values = await customerForm.validateFields();
    try {
      if (customerModal.item) {
        await http.put(`/customers/${customerModal.item.id}`, values);
      } else {
        await http.post("/customers", values);
      }
      setCustomerModal({ open: false });
      await loadAll();
    } catch {
      message.error("Не удалось сохранить потребителя");
    }
  };

  const saveWireCode = async () => {
    const values = await wireCodeForm.validateFields();
    try {
      if (wireCodeModal.item) {
        await http.put(`/wirecodes/${wireCodeModal.item.id}`, values);
      } else {
        await http.post("/wirecodes", values);
      }
      setWireCodeModal({ open: false });
      await loadAll();
    } catch {
      message.error("Не удалось сохранить код проволоки");
    }
  };

  const saveParameter = async () => {
    const values = await parameterForm.validateFields();
    try {
      if (parameterModal.item) {
        await http.put(`/parameters/${parameterModal.item.id}`, values);
      } else {
        await http.post("/parameters", values);
      }
      setParameterModal({ open: false });
      await loadAll();
    } catch {
      message.error("Не удалось сохранить параметр");
    }
  };

  const deleteCountry = async (id: number) => {
    try {
      await http.delete(`/countries/${id}`);
      await loadAll();
    } catch {
      message.error("Не удалось удалить страну");
    }
  };
  const deleteCustomer = async (id: number) => {
    try {
      await http.delete(`/customers/${id}`);
      await loadAll();
    } catch {
      message.error("Не удалось удалить потребителя");
    }
  };
  const deleteWireCode = async (id: number) => {
    try {
      await http.delete(`/wirecodes/${id}`);
      await loadAll();
    } catch {
      message.error("Не удалось удалить код проволоки");
    }
  };
  const deleteParameter = async (id: number) => {
    try {
      await http.delete(`/parameters/${id}`);
      await loadAll();
    } catch {
      message.error("Не удалось удалить параметр");
    }
  };

  const handleGenerateData = async () => {
    setGenerating(true);
    try {
      await http.post("/maintenance/generate-data", null, { params: { count: generationCount } });
      message.success(`Успешно создано ${generationCount} записей`);
    } catch (err: any) {
      const errorMsg = err?.response?.data || "Не удалось сгенерировать данные";
      message.error(typeof errorMsg === "string" ? errorMsg : "Ошибка при генерации");
    } finally {
      setGenerating(false);
    }
  };

  return (
    <>
      <Tabs
        items={[
          {
            key: "countries",
            label: "Страны",
            children: (
              <Card
                title={
                  <Input.Search
                    placeholder="Поиск по названию"
                    value={countrySearch}
                    onChange={(e) => setCountrySearch(e.target.value)}
                    onSearch={(v) => void loadCountries(v)}
                    style={{ maxWidth: 300 }}
                    allowClear
                  />
                }
                extra={<Button onClick={() => openCountryModal()}>Добавить</Button>}
              >
                <Table
                  rowKey="id"
                  dataSource={countries}
                  columns={countryColumns}
                  loading={loading}
                  scroll={{ x: "max-content" }}
                />
              </Card>
            )
          },
          {
            key: "customers",
            label: "Потребители",
            children: (
              <Card
                title={
                  <Input.Search
                    placeholder="Поиск по названию"
                    value={customerSearch}
                    onChange={(e) => setCustomerSearch(e.target.value)}
                    onSearch={(v) => void loadCustomers(v)}
                    style={{ maxWidth: 300 }}
                    allowClear
                  />
                }
                extra={<Button onClick={() => openCustomerModal()}>Добавить</Button>}
              >
                <Table
                  rowKey="id"
                  dataSource={customers}
                  columns={customerColumns}
                  loading={loading}
                  scroll={{ x: "max-content" }}
                />
              </Card>
            )
          },
          {
            key: "wirecodes",
            label: "Коды проволоки",
            children: (
              <Card
                title={
                  <Input.Search
                    placeholder="Поиск по коду или маркировке"
                    value={wireCodeSearch}
                    onChange={(e) => setWireCodeSearch(e.target.value)}
                    onSearch={(v) => void loadWireCodes(v)}
                    style={{ maxWidth: 400 }}
                    allowClear
                  />
                }
                extra={<Button onClick={() => openWireCodeModal()}>Добавить</Button>}
              >
                <Table
                  rowKey="id"
                  dataSource={wireCodes}
                  columns={wireCodeColumns}
                  loading={loading}
                  scroll={{ x: "max-content" }}
                />
              </Card>
            )
          },
          {
            key: "parameters",
            label: "Параметры",
            children: (
              <Card
                title={
                  <Input.Search
                    placeholder="Поиск по названию"
                    value={parameterSearch}
                    onChange={(e) => setParameterSearch(e.target.value)}
                    onSearch={(v) => void loadParameters(v)}
                    style={{ maxWidth: 300 }}
                    allowClear
                  />
                }
                extra={<Button onClick={() => openParameterModal()}>Добавить</Button>}
              >
                <Table
                  rowKey="id"
                  dataSource={parameters}
                  columns={parameterColumns}
                  loading={loading}
                  scroll={{ x: "max-content" }}
                />
              </Card>
            )
          },
          {
            key: "limits",
            label: "Лимиты / сборка протокола",
            children: (
              <Space direction="vertical" style={{ width: "100%" }}>
                <Card>
                  <Row gutter={[16, 16]} align="bottom">
                    <Col xs={24} sm={12} md={8}>
                      <div style={{ marginBottom: 8 }}>Код проволоки:</div>
                      <Select
                        placeholder="Выберите код проволоки"
                        style={{ width: "100%" }}
                        value={selectedWireCodeId}
                        options={wireCodes.map((w) => ({ value: w.id, label: `${w.code} (${w.marking})` }))}
                        onChange={(v) => setSelectedWireCodeId(v)}
                      />
                    </Col>
                    <Col xs={24} sm={6} md={4}>
                      <Button
                        block
                        onClick={() => {
                          if (selectedWireCodeId) {
                            void loadLimits(selectedWireCodeId);
                          }
                        }}
                      >
                        Загрузить
                      </Button>
                    </Col>
                    <Col xs={24} sm={6} md={4}>
                      <Button type="primary" onClick={saveLimits} block>
                        Сохранить лимиты
                      </Button>
                    </Col>
                  </Row>
                </Card>
                <Card>
                  <Table
                    rowKey="parameterId"
                    dataSource={limitRows}
                    columns={limitColumns}
                    pagination={false}
                    scroll={{ x: "max-content" }}
                  />
                </Card>
              </Space>
            )
          },
          {
            key: "maintenance",
            label: "Обслуживание",
            children: (
              <Card title="Генерация тестовых данных">
                <Space direction="vertical" style={{ width: "100%" }}>
                  <p>
                    Этот инструмент позволяет массово генерировать реалистичные данные испытаний.
                    Данные создаются на основе существующих кодов проволоки, лимитов, лаборантов и потребителей.
                  </p>
                  <Space align="center" size="large">
                    <span>Количество записей:</span>
                    <InputNumber
                      min={1}
                      max={100000}
                      value={generationCount}
                      onChange={(v) => setGenerationCount(v ?? 100)}
                      style={{ width: 120 }}
                    />
                    <Button
                      type="primary"
                      onClick={handleGenerateData}
                      loading={generating}
                      disabled={generating}
                    >
                      Сгенерировать
                    </Button>
                  </Space>
                  <div style={{ marginTop: 16, color: "rgba(0,0,0,0.45)", fontSize: "0.85em" }}>
                    * Генерация большого объема данных может занять некоторое время.
                    Для 20 000+ записей рекомендуется генерировать порциями по 5 000 - 10 000.
                  </div>
                </Space>
              </Card>
            )
          }
        ]}
      />

      <Modal
        title={countryModal.item ? "Изменить страну" : "Добавить страну"}
        open={countryModal.open}
        onCancel={() => setCountryModal({ open: false })}
        onOk={() => void saveCountry()}
      >
        <Form form={countryForm} layout="vertical">
          <Form.Item name="name" label="Название" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title={customerModal.item ? "Изменить потребителя" : "Добавить потребителя"}
        open={customerModal.open}
        onCancel={() => setCustomerModal({ open: false })}
        onOk={() => void saveCustomer()}
      >
        <Form form={customerForm} layout="vertical">
          <Form.Item name="name" label="Название" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="countryId" label="Страна">
            <Select allowClear options={countries.map((c) => ({ value: c.id, label: c.name }))} />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title={wireCodeModal.item ? "Изменить код проволоки" : "Добавить код проволоки"}
        open={wireCodeModal.open}
        onCancel={() => setWireCodeModal({ open: false })}
        onOk={() => void saveWireCode()}
      >
        <Form form={wireCodeForm} layout="vertical">
          <Form.Item name="code" label="Код" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="marking" label="Маркировка" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="diameter" label="Диаметр" rules={[{ required: true }]}>
            <InputNumber min={0.001} style={{ width: "100%" }} />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title={parameterModal.item ? "Изменить параметр" : "Добавить параметр"}
        open={parameterModal.open}
        onCancel={() => setParameterModal({ open: false })}
        onOk={() => void saveParameter()}
      >
        <Form form={parameterForm} layout="vertical">
          <Form.Item name="name" label="Название" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="dataType" label="Тип" rules={[{ required: true }]}>
            <Select
              options={[
                { value: 1, label: "Число" },
                { value: 2, label: "Текст" }
              ]}
            />
          </Form.Item>
          <Form.Item noStyle shouldUpdate={(prev, curr) => prev.dataType !== curr.dataType}>
            {({ getFieldValue }) => (
              <Form.Item
                name="unit"
                label="Единица измерения"
                rules={getFieldValue("dataType") === 1 ? [{ required: true }] : []}
              >
                <Input />
              </Form.Item>
            )}
          </Form.Item>
        </Form>
      </Modal>
    </>
  );
}
