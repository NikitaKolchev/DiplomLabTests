import { Button, Card, Col, Form, Input, Modal, Row, Space, Table, message } from "antd";
import { useEffect, useState } from "react";
import { http } from "../api/http";

type Assistant = {
  id: number;
  fullName: string;
  login: string;
  laboratoryId?: number;
};

export default function EngineerTeamPage() {
  const [items, setItems] = useState<Assistant[]>([]);
  const [filtersForm] = Form.useForm<{ search?: string; login?: string }>();
  const [createForm] = Form.useForm<{ fullName: string; login: string; password: string }>();
  const [editForm] = Form.useForm<{ fullName: string; login: string; password?: string }>();
  const [editTarget, setEditTarget] = useState<Assistant | null>(null);

  const loadAssistants = async (search?: string, login?: string) => {
    try {
      const response = await http.get("/engineer/users/assistants", {
        params: { search: search || undefined, login: login || undefined }
      });
      setItems(response.data);
    } catch {
      message.error("Не удалось загрузить список лаборантов");
    }
  };

  useEffect(() => {
    void loadAssistants();
  }, []);

  const createAssistant = async () => {
    const values = await createForm.validateFields();
    try {
      await http.post("/engineer/users/assistants", values);
      createForm.resetFields();
      const filters = filtersForm.getFieldsValue();
      await loadAssistants(filters.search, filters.login);
      message.success("Лаборант создан");
    } catch {
      message.error("Не удалось создать лаборанта");
    }
  };

  const openEdit = (item: Assistant) => {
    setEditTarget(item);
    editForm.setFieldsValue({
      fullName: item.fullName,
      login: item.login,
      password: undefined
    });
  };

  const saveEdit = async () => {
    if (!editTarget) return;
    const values = await editForm.validateFields();
    try {
      await http.put(`/engineer/users/assistants/${editTarget.id}`, values);
      setEditTarget(null);
      const filters = filtersForm.getFieldsValue();
      await loadAssistants(filters.search, filters.login);
      message.success("Лаборант обновлен");
    } catch {
      message.error("Не удалось обновить лаборанта");
    }
  };

  return (
    <Space direction="vertical" size={16} style={{ width: "100%" }}>
      <Card title="Фильтры списка лаборантов">
        <Form
          form={filtersForm}
          layout="vertical"
          onFinish={(v) => void loadAssistants(v.search, v.login)}
        >
          <Row gutter={[16, 0]} align="bottom">
            <Col xs={24} sm={10} md={8}>
              <Form.Item name="search" label="ФИО">
                <Input allowClear style={{ width: "100%" }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={8} md={6}>
              <Form.Item name="login" label="Логин">
                <Input allowClear style={{ width: "100%" }} />
              </Form.Item>
            </Col>
            <Col xs={12} sm={3} md={2}>
              <Form.Item>
                <Button type="primary" htmlType="submit" block>
                  Применить
                </Button>
              </Form.Item>
            </Col>
            <Col xs={12} sm={3} md={2}>
              <Form.Item>
                <Button
                  block
                  onClick={() => {
                    filtersForm.resetFields();
                    void loadAssistants();
                  }}
                >
                  Сброс
                </Button>
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Card>

      <Card title="Создать лаборанта">
        <Form form={createForm} layout="vertical">
          <Row gutter={[16, 0]} align="bottom">
            <Col xs={24} sm={12} md={8}>
              <Form.Item name="fullName" label="ФИО" rules={[{ required: true }]}>
                <Input style={{ width: "100%" }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} md={6}>
              <Form.Item name="login" label="Логин" rules={[{ required: true }]}>
                <Input style={{ width: "100%" }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} md={6}>
              <Form.Item name="password" label="Пароль" rules={[{ required: true, min: 8 }]}>
                <Input.Password style={{ width: "100%" }} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} md={4}>
              <Form.Item>
                <Button type="primary" onClick={() => void createAssistant()} block>
                  Создать
                </Button>
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Card>

      <Card title="Лаборанты">
        <Table
          rowKey="id"
          dataSource={items}
          scroll={{ x: "max-content" }}
          columns={[
            { title: "ФИО", dataIndex: "fullName" },
            { title: "Логин", dataIndex: "login" },
            {
              title: "Действия",
              key: "actions",
              render: (_, row) => (
                <Button size="small" onClick={() => openEdit(row)}>
                  Редактировать
                </Button>
              )
            }
          ]}
          pagination={false}
        />
      </Card>

      <Modal
        title={editTarget ? `Редактировать #${editTarget.id}` : "Редактировать"}
        open={!!editTarget}
        onCancel={() => setEditTarget(null)}
        onOk={() => void saveEdit()}
      >
        <Form form={editForm} layout="vertical">
          <Form.Item name="fullName" label="ФИО" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="login" label="Логин" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="password" label="Новый пароль (опционально)" rules={[{ min: 8 }]}>
            <Input.Password />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
