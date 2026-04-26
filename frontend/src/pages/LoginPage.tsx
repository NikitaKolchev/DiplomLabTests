import { Button, Card, Form, Input, message, Typography, theme } from "antd";
import { Link, useNavigate } from "react-router-dom";
import { http } from "../api/http";
import { useAuth } from "../auth/AuthContext";
import { useTheme } from "../theme/ThemeContext";

const { useToken } = theme;

type LoginForm = {
  username: string;
  password: string;
};

export default function LoginPage() {
  const { token } = useToken();
  const auth = useAuth();
  const navigate = useNavigate();
  const { mode } = useTheme();
  const isDark = mode === "dark";

  const onFinish = async (values: LoginForm) => {
    try {
      const response = await http.post<{ token?: string }>("/auth/login", {
        username: values.username,
        password: values.password
      });
      const tokenRes = response?.data?.token;
      if (!tokenRes) {
        message.error("Неверный формат ответа сервера");
        return;
      }
      auth.login(tokenRes);
      navigate("/");
    } catch {
      message.error("Неверный логин или пароль");
    }
  };

  return (
    <div
      style={{
        minHeight: "100vh",
        display: "grid",
        placeItems: "center",
        padding: 16,
        background: isDark
          ? "radial-gradient(circle at 20% 20%, #273246 0%, #131820 60%)"
          : "radial-gradient(circle at 20% 20%, #e8eef5 0%, #f5f7fa 60%)"
      }}
    >
      <Card style={{ width: '100%', maxWidth: 440, border: `1px solid ${token.colorBorderSecondary}` }}>
        <Typography.Title level={3} style={{ marginBottom: 4 }}>
          Лабораторные испытания
        </Typography.Title>
        <Typography.Text type="secondary">
          Система регистрации лабораторных испытаний
        </Typography.Text>
        <Form<LoginForm> layout="vertical" onFinish={onFinish}>
          <Form.Item label="Логин" name="username" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item label="Пароль" name="password" rules={[{ required: true }]}>
            <Input.Password />
          </Form.Item>
          <Button type="primary" htmlType="submit" block>
            Войти
          </Button>
          <div style={{ marginTop: 12, textAlign: "center" }}>
            <Link to="/guest" style={{ color: token.colorPrimary }}>
              Просмотр журнала без входа
            </Link>
          </div>
        </Form>
      </Card>
    </div>
  );
}
