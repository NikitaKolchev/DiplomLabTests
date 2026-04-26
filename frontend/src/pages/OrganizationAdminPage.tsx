import { Button, Card, Form, Input, Modal, Popconfirm, Select, Space, Table, Typography, message, Tooltip, Row, Col, theme } from "antd";
import { ReloadOutlined } from "@ant-design/icons";
import type { ColumnsType } from "antd/es/table";
import { useEffect, useState } from "react";
import { http } from "../api/http";

const { useToken } = theme;

type Engineer = {
  id: number;
  fullName: string;
  login: string;
  laboratoryId?: number;
  roleName?: string;
};

type Laboratory = {
  id: number;
  name: string;
  engineerId?: number;
  engineerName?: string;
};

type Assistant = {
  id: number;
  fullName: string;
  login: string;
  laboratoryId?: number;
  roleName?: string;
};

export default function OrganizationAdminPage() {
  const { token } = useToken();
  const [engineers, setEngineers] = useState<Engineer[]>([]);
  const [laboratories, setLaboratories] = useState<Laboratory[]>([]);
  const [assistants, setAssistants] = useState<Assistant[]>([]);
  const [loading, setLoading] = useState(false);

  const [labForm] = Form.useForm<{ name: string; engineerId?: number }>();
  const [engineerCreateForm] = Form.useForm<{ fullName: string; login: string; password?: string; laboratoryId?: number }>();
  const [engineerEditForm] = Form.useForm<{ fullName: string; login: string; password?: string; laboratoryId?: number; roleName?: string }>();
  const [assistantForm] = Form.useForm<{ fullName: string; login: string; password?: string; laboratoryId: number }>();
  const [assistantEditForm] = Form.useForm<{ fullName: string; login: string; password?: string; laboratoryId: number; roleName?: string }>();

  const [labModal, setLabModal] = useState<{ open: boolean; item?: Laboratory }>({ open: false });
  const [engineerModal, setEngineerModal] = useState<{ open: boolean; item?: Engineer }>({ open: false });
  const [assistantModal, setAssistantModal] = useState<{ open: boolean; item?: Assistant }>({ open: false });

  const transliterateAsync = async (fullName: string): Promise<string> => {
    const res = await http.get("/admin/utils/transliterate", { params: { fullName } });
    return res.data;
  };

  const generatePassword = (length = 10): string => {
    const charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*";
    let retVal = "";
    for (let i = 0, n = charset.length; i < length; ++i) {
      retVal += charset.charAt(Math.floor(Math.random() * n));
    }
    return retVal;
  };

  const loadPassword = async () => {
    const res = await http.get("/admin/utils/password", { params: { length: 10 } });
    return res.data as string;
  };

  const loadOrganization = async () => {
    setLoading(true);
    try {
      const [engRes, labRes, astRes] = await Promise.all([
        http.get("/admin/users/engineers"),
        http.get("/admin/laboratories"),
        http.get("/admin/users/assistants")
      ]);
      setEngineers(engRes.data);
      setLaboratories(labRes.data);
      setAssistants(astRes.data);
    } catch {
      message.error("Не удалось загрузить данные организации");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadOrganization();
  }, []);

  const createLaboratory = async () => {
    const values = await labForm.validateFields();
    try {
      await http.post("/admin/laboratories", values);
      labForm.resetFields();
      await loadOrganization();
      message.success("Лаборатория создана");
    } catch {
      message.error("Не удалось создать лабораторию");
    }
  };

  const createEngineer = async () => {
    const values = await engineerCreateForm.validateFields();
    try {
      await http.post("/admin/users/engineers", {
        fullName: values.fullName,
        login: values.login,
        password: values.password,
        laboratoryId: values.laboratoryId
      });
      engineerCreateForm.resetFields();
      await loadOrganization();
      message.success("Инженер создан");
    } catch (error: any) {
      const errorMsg = error.response?.data?.detail || error.response?.data || "Не удалось создать инженера";
      message.error(typeof errorMsg === 'string' ? errorMsg : "Не удалось создать инженера");
    }
  };

  const createAssistant = async () => {
    const values = await assistantForm.validateFields();
    if (!values.password) {
      message.error("Пароль обязателен при создании");
      return;
    }
    try {
      await http.post("/admin/users/assistants", {
        fullName: values.fullName,
        login: values.login,
        password: values.password,
        laboratoryId: values.laboratoryId
      });
      assistantForm.resetFields();
      await loadOrganization();
      message.success("Лаборант создан");
    } catch (error: any) {
      const errorMsg = error.response?.data?.detail || error.response?.data || "Не удалось создать лаборанта";
      message.error(typeof errorMsg === 'string' ? errorMsg : "Не удалось создать лаборанта");
    }
  };

  const openLabModal = (item?: Laboratory) => {
    setLabModal({ open: true, item });
    labForm.setFieldsValue({ name: item?.name ?? "", engineerId: item?.engineerId });
  };
  const openEngineerModal = (item?: Engineer) => {
    setEngineerModal({ open: true, item });
    engineerEditForm.setFieldsValue({
      fullName: item?.fullName ?? "",
      login: item?.login ?? "",
      laboratoryId: item?.laboratoryId,
      roleName: "Engineer"
    });
  };
  const openAssistantModal = (item?: Assistant) => {
    setAssistantModal({ open: true, item });
    assistantEditForm.setFieldsValue({
      fullName: item?.fullName ?? "",
      login: item?.login ?? "",
      laboratoryId: item?.laboratoryId ?? laboratories[0]?.id,
      roleName: "Assistant"
    });
  };

  const saveLab = async () => {
    const values = await labForm.validateFields();
    if (!labModal.item) return;
    try {
      await http.put(`/admin/laboratories/${labModal.item.id}`, values);
      setLabModal({ open: false });
      await loadOrganization();
      message.success("Лаборатория обновлена");
    } catch {
      message.error("Не удалось обновить лабораторию");
    }
  };
  const saveEngineer = async () => {
    const values = await engineerEditForm.validateFields();
    if (!engineerModal.item) return;
    try {
      let currentLabId = values.laboratoryId;
      if (values.roleName && values.roleName !== "Engineer") {
        await http.put(`/admin/users/${engineerModal.item.id}/role`, { roleName: values.roleName });
        currentLabId = undefined; // Clear lab on role change
      }

      // If switching from Engineer to Assistant, use the assistant update endpoint
      if (values.roleName === "Assistant") {
        await http.put(`/admin/users/assistants/${engineerModal.item.id}`, {
          fullName: values.fullName,
          login: values.login,
          password: values.password || undefined,
          laboratoryId: currentLabId
        });
      } else {
        await http.put(`/admin/users/engineers/${engineerModal.item.id}`, {
          fullName: values.fullName,
          login: values.login,
          password: values.password || undefined,
          laboratoryId: currentLabId
        });
      }

      await loadOrganization();
      setEngineerModal({ open: false });
      message.success("Данные инженера обновлены");
    } catch (error: any) {
      const errorMsg = error.response?.data?.detail || error.response?.data || "Не удалось сохранить данные инженера";
      message.error(typeof errorMsg === 'string' ? errorMsg : "Не удалось сохранить данные инженера");
    }
  };
  const saveAssistant = async () => {
    const values = await assistantEditForm.validateFields();
    if (!assistantModal.item) return;
    try {
      let currentLabId: number | undefined = values.laboratoryId;
      if (values.roleName && values.roleName !== "Assistant") {
        await http.put(`/admin/users/${assistantModal.item.id}/role`, { roleName: values.roleName });
        currentLabId = undefined; // Clear lab on role change
      }

      // If switching from Assistant to Engineer, use the engineer update endpoint
      if (values.roleName === "Engineer") {
        await http.put(`/admin/users/engineers/${assistantModal.item.id}`, {
          fullName: values.fullName,
          login: values.login,
          password: values.password || undefined,
          laboratoryId: currentLabId
        });
      } else {
        await http.put(`/admin/users/assistants/${assistantModal.item.id}`, {
          fullName: values.fullName,
          login: values.login,
          password: values.password || undefined,
          laboratoryId: currentLabId
        });
      }

      setAssistantModal({ open: false });
      await loadOrganization();
      message.success("Лаборант обновлён");
    } catch {
      message.error("Не удалось обновить лаборанта");
    }
  };

  const deleteLab = async (id: number) => {
    try {
      await http.delete(`/admin/laboratories/${id}`);
      await loadOrganization();
      message.success("Лаборатория удалена");
    } catch {
      message.error("Не удалось удалить лабораторию");
    }
  };
  const deleteEngineer = async (id: number) => {
    try {
      await http.delete(`/admin/users/engineers/${id}`);
      await loadOrganization();
      message.success("Инженер удалён");
    } catch {
      message.error("Не удалось удалить инженера");
    }
  };
  const deleteAssistant = async (id: number) => {
    try {
      await http.delete(`/admin/users/assistants/${id}`);
      await loadOrganization();
      message.success("Лаборант удалён");
    } catch {
      message.error("Не удалось удалить лаборанта");
    }
  };

  const assignEngineer = async (laboratoryId: number, engineerId: number) => {
    try {
      await http.put(`/admin/laboratories/${laboratoryId}/engineer`, { engineerId });
      await loadOrganization();
      message.success("Инженер назначен");
    } catch {
      message.error("Не удалось назначить инженера");
    }
  };

  const labColumns: ColumnsType<Laboratory> = [
    { title: "Лаборатория", dataIndex: "name", sorter: (a, b) => (a.name || "").localeCompare(b.name || "") },
    {
      title: "Инженер",
      dataIndex: "engineerId",
      render: (v: number | undefined, row: Laboratory) => (
        <Select
          allowClear
          style={{ width: 260 }}
          placeholder="Выберите инженера"
          value={v}
          options={engineers.map((e) => ({ value: e.id, label: `${e.fullName} (${e.login})` }))}
          onChange={(value) => void assignEngineer(row.id, value)}
        />
      )
    },
    {
      title: "Действия",
      key: "actions",
      width: 180,
      render: (_, row) => (
        <Space>
          <Button size="small" onClick={() => openLabModal(row)}>
            Изменить
          </Button>
          <Popconfirm title="Удалить лабораторию?" onConfirm={() => void deleteLab(row.id)}>
            <Button size="small" danger>Удалить</Button>
          </Popconfirm>
        </Space>
      )
    }
  ];

  const engineerColumns: ColumnsType<Engineer> = [
    { title: "ФИО", dataIndex: "fullName", sorter: (a, b) => (a.fullName || "").localeCompare(b.fullName || "") },
    { title: "Логин", dataIndex: "login", sorter: (a, b) => (a.login || "").localeCompare(b.login || "") },
    {
      title: "Лаборатория",
      dataIndex: "laboratoryId",
      render: (v?: number) => laboratories.find((l) => l.id === v)?.name ?? "-"
    },
    {
      title: "Действия",
      key: "actions",
      width: 180,
      render: (_, row) => (
        <Space>
          <Button size="small" onClick={() => openEngineerModal(row)}>
            Изменить
          </Button>
          <Popconfirm title="Удалить инженера?" onConfirm={() => void deleteEngineer(row.id)}>
            <Button size="small" danger>Удалить</Button>
          </Popconfirm>
        </Space>
      )
    }
  ];

  const assistantColumns: ColumnsType<Assistant> = [
    { title: "ФИО", dataIndex: "fullName", sorter: (a, b) => (a.fullName || "").localeCompare(b.fullName || "") },
    { title: "Логин", dataIndex: "login", sorter: (a, b) => (a.login || "").localeCompare(b.login || "") },
    {
      title: "Лаборатория",
      dataIndex: "laboratoryId",
      render: (v?: number) => laboratories.find((l) => l.id === v)?.name ?? "-"
    },
    {
      title: "Инженер лаборатории",
      render: (_, row) => {
        const lab = laboratories.find((l) => l.id === row.laboratoryId);
        return lab?.engineerName ?? "-";
      }
    },
    {
      title: "Действия",
      key: "actions",
      width: 180,
      render: (_, row) => (
        <Space>
          <Button size="small" onClick={() => openAssistantModal(row)}>
            Изменить
          </Button>
          <Popconfirm title="Удалить лаборанта?" onConfirm={() => void deleteAssistant(row.id)}>
            <Button size="small" danger>Удалить</Button>
          </Popconfirm>
        </Space>
      )
    }
  ];

  return (
    <Space direction="vertical" size={16} style={{ width: "100%" }}>
      <Typography.Title level={4}>Организация лабораторий</Typography.Title>

      <Card title="Создать лабораторию">
        <Form form={labForm} layout="vertical">
          <Row gutter={[16, 0]}>
            <Col xs={24} sm={12} md={8}>
              <Form.Item name="name" label="Название" rules={[{ required: true }]}>
                <Input style={{ width: "100%" }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} md={8}>
              <Form.Item name="engineerId" label="Инженер">
                <Select
                  allowClear
                  style={{ width: "100%" }}
                  options={engineers.map((e) => ({ value: e.id, label: `${e.fullName} (${e.login})` }))}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={24} md={8} style={{ display: 'flex', alignItems: 'flex-end', marginBottom: 24 }}>
              <Button type="primary" onClick={() => void createLaboratory()} block>
                Создать
              </Button>
            </Col>
          </Row>
        </Form>
      </Card>

      <Card title="Создать инженера">
        <Form form={engineerCreateForm} layout="vertical">
          <Row gutter={[16, 0]}>
            <Col xs={24} sm={12} md={6}>
              <Form.Item name="fullName" label="ФИО" rules={[{ required: true }]}>
                <Input 
                  style={{ width: "100%" }}
                  onBlur={async (e) => {
                    const login = engineerCreateForm.getFieldValue("login");
                    if (!login && e.target.value) {
                      const transliterated = await transliterateAsync(e.target.value);
                      engineerCreateForm.setFieldsValue({ login: transliterated });
                    }
                  }}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} md={5}>
              <Form.Item name="login" label="Логин" rules={[{ required: true }]}>
                <Input 
                  style={{ width: "100%" }}
                  suffix={
                    <Tooltip title="Сгенерировать логин из ФИО">
                      <ReloadOutlined 
                        onClick={async () => {
                          const name = engineerCreateForm.getFieldValue("fullName");
                          if (name) {
                            const transliterated = await transliterateAsync(name);
                            engineerCreateForm.setFieldsValue({ login: transliterated });
                          }
                        }}
                        style={{ cursor: 'pointer', color: token.colorPrimary }}
                      />
                    </Tooltip>
                  }
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} md={5}>
              <Form.Item name="password" label="Пароль" rules={[{ required: true, min: 8 }]}>
                <Input.Password 
                  style={{ width: "100%" }}
                  suffix={
                    <Tooltip title="Сгенерировать пароль">
                      <ReloadOutlined 
                        onClick={async () => {
                          const password = await loadPassword();
                          engineerCreateForm.setFieldsValue({ password });
                        }}
                        style={{ cursor: 'pointer', color: token.colorPrimary }}
                      />
                    </Tooltip>
                  }
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} md={5}>
              <Form.Item name="laboratoryId" label="Лаборатория">
                <Select
                  allowClear
                  style={{ width: "100%" }}
                  options={laboratories.map((l) => ({ value: l.id, label: l.name }))}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={24} md={3} style={{ display: 'flex', alignItems: 'flex-end', marginBottom: 24 }}>
              <Button type="primary" onClick={() => void createEngineer()} block>
                Создать
              </Button>
            </Col>
          </Row>
        </Form>
      </Card>

      <Card title="Создать лаборанта">
        <Form form={assistantForm} layout="vertical">
          <Row gutter={[16, 0]}>
            <Col xs={24} sm={12} md={6}>
              <Form.Item name="fullName" label="ФИО" rules={[{ required: true }]}>
                <Input 
                  style={{ width: "100%" }}
                  onBlur={async (e) => {
                    const login = assistantForm.getFieldValue("login");
                    if (!login && e.target.value) {
                      const transliterated = await transliterateAsync(e.target.value);
                      assistantForm.setFieldsValue({ login: transliterated });
                    }
                  }}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} md={5}>
              <Form.Item name="login" label="Логин" rules={[{ required: true }]}>
                <Input 
                  style={{ width: "100%" }}
                  suffix={
                    <Tooltip title="Сгенерировать логин из ФИО">
                      <ReloadOutlined 
                        onClick={async () => {
                          const name = assistantForm.getFieldValue("fullName");
                          if (name) {
                            const transliterated = await transliterateAsync(name);
                            assistantForm.setFieldsValue({ login: transliterated });
                          }
                        }}
                        style={{ cursor: 'pointer', color: token.colorPrimary }}
                      />
                    </Tooltip>
                  }
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} md={5}>
              <Form.Item name="password" label="Пароль" rules={[{ required: true, min: 8 }]}>
                <Input.Password 
                  style={{ width: "100%" }}
                  suffix={
                    <Tooltip title="Сгенерировать пароль">
                      <ReloadOutlined 
                        onClick={async () => {
                          const password = await loadPassword();
                          assistantForm.setFieldsValue({ password });
                        }}
                        style={{ cursor: 'pointer', color: token.colorPrimary }}
                      />
                    </Tooltip>
                  }
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} md={5}>
              <Form.Item name="laboratoryId" label="Лаборатория" rules={[{ required: true }]}>
                <Select style={{ width: "100%" }} options={laboratories.map((l) => ({ value: l.id, label: l.name }))} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={24} md={3} style={{ display: 'flex', alignItems: 'flex-end', marginBottom: 24 }}>
              <Button type="primary" onClick={() => void createAssistant()} block>
                Создать
              </Button>
            </Col>
          </Row>
        </Form>
      </Card>

      <Card title="Лаборатории">
        <Table rowKey="id" columns={labColumns} dataSource={laboratories} loading={loading} scroll={{ x: 'max-content' }} />
      </Card>

      <Card title="Инженеры">
        <Table rowKey="id" columns={engineerColumns} dataSource={engineers} loading={loading} scroll={{ x: 'max-content' }} />
      </Card>

      <Card title="Лаборанты">
        <Table rowKey="id" columns={assistantColumns} dataSource={assistants} loading={loading} scroll={{ x: 'max-content' }} />
      </Card>

      <Modal title="Изменить лабораторию" open={labModal.open} onCancel={() => setLabModal({ open: false })} onOk={() => void saveLab()}>
        <Form form={labForm} layout="vertical">
          <Form.Item name="name" label="Название" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="engineerId" label="Инженер">
            <Select allowClear options={engineers.map((e) => ({ value: e.id, label: `${e.fullName} (${e.login})` }))} />
          </Form.Item>
        </Form>
      </Modal>

      <Modal title="Изменить инженера" open={engineerModal.open} onCancel={() => setEngineerModal({ open: false })} onOk={() => void saveEngineer()}>
        <Form form={engineerEditForm} layout="vertical">
          <Form.Item name="fullName" label="ФИО" rules={[{ required: true }]}>
            <Input 
              onBlur={async (e) => {
                const login = engineerEditForm.getFieldValue("login");
                if (!login && e.target.value) {
                  const transliterated = await transliterateAsync(e.target.value);
                  engineerEditForm.setFieldsValue({ login: transliterated });
                }
              }}
            />
          </Form.Item>
          <Form.Item name="login" label="Логин" rules={[{ required: true }]}>
            <Input 
              suffix={
                <Tooltip title="Сгенерировать логин из ФИО">
                  <ReloadOutlined 
                    onClick={async () => {
                      const name = engineerEditForm.getFieldValue("fullName");
                      if (name) {
                        const transliterated = await transliterateAsync(name);
                        engineerEditForm.setFieldsValue({ login: transliterated });
                      }
                    }}
                    style={{ cursor: 'pointer', color: token.colorPrimary }}
                  />
                </Tooltip>
              }
            />
          </Form.Item>
          <Form.Item name="password" label={engineerModal.item ? "Новый пароль (оставьте пустым, чтобы не менять)" : "Пароль"} rules={[{ required: !engineerModal.item }]}>
            <Input.Password 
              suffix={
                <Tooltip title="Сгенерировать пароль">
                  <ReloadOutlined 
                    onClick={async () => {
                      const password = await loadPassword();
                      engineerEditForm.setFieldsValue({ password });
                    }}
                    style={{ cursor: 'pointer', color: token.colorPrimary }}
                  />
                </Tooltip>
              }
            />
          </Form.Item>
          <Form.Item name="roleName" label="Роль" rules={[{ required: true }]}>
            <Select options={[
              { value: "Engineer", label: "Инженер" },
              { value: "Assistant", label: "Лаборант" }
            ]} />
          </Form.Item>
          <Form.Item name="laboratoryId" label="Лаборатория">
            <Select allowClear options={laboratories.map((l) => ({ value: l.id, label: l.name }))} />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="Изменить лаборанта"
        open={assistantModal.open}
        onCancel={() => {
          setAssistantModal({ open: false });
          assistantEditForm.resetFields();
        }}
        onOk={() => void saveAssistant()}
      >
        <Form form={assistantEditForm} layout="vertical">
          <Form.Item name="fullName" label="ФИО" rules={[{ required: true }]}>
            <Input 
              onBlur={async (e) => {
                const login = assistantEditForm.getFieldValue("login");
                if (!login && e.target.value) {
                  const transliterated = await transliterateAsync(e.target.value);
                  assistantEditForm.setFieldsValue({ login: transliterated });
                }
              }}
            />
          </Form.Item>
          <Form.Item name="login" label="Логин" rules={[{ required: true }]}>
            <Input 
              suffix={
                <Tooltip title="Сгенерировать логин из ФИО">
                  <ReloadOutlined 
                    onClick={async () => {
                      const name = assistantEditForm.getFieldValue("fullName");
                      if (name) {
                        const transliterated = await transliterateAsync(name);
                        assistantEditForm.setFieldsValue({ login: transliterated });
                      }
                    }}
                    style={{ cursor: 'pointer', color: token.colorPrimary }}
                  />
                </Tooltip>
              }
            />
          </Form.Item>
          <Form.Item name="password" label="Новый пароль (оставьте пустым, чтобы не менять)">
            <Input.Password 
              suffix={
                <Tooltip title="Сгенерировать пароль">
                  <ReloadOutlined 
                    onClick={async () => {
                      const password = await loadPassword();
                      assistantEditForm.setFieldsValue({ password });
                    }}
                    style={{ cursor: 'pointer', color: token.colorPrimary }}
                  />
                </Tooltip>
              }
            />
          </Form.Item>
          <Form.Item name="roleName" label="Роль" rules={[{ required: true }]}>
            <Select options={[
              { value: "Engineer", label: "Инженер" },
              { value: "Assistant", label: "Лаборант" }
            ]} />
          </Form.Item>
          <Form.Item name="laboratoryId" label="Лаборатория" rules={[{ required: true }]}>
            <Select options={laboratories.map((l) => ({ value: l.id, label: l.name }))} />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
