import { Button, Drawer, Grid, Image, Layout, Menu, Typography, theme } from "antd";
import { MenuOutlined, MoonOutlined, SunOutlined } from "@ant-design/icons";
import { Link, Outlet, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { useTheme } from "../theme/ThemeContext";
import ThemeToggleButton from "../components/ThemeToggleButton";
import { useState } from "react";

const { Header, Content } = Layout;
const { useBreakpoint } = Grid;
const { useToken } = theme;

export default function AppShell() {
  const { token } = useToken();
  const auth = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const { mode, toggleTheme } = useTheme();
  const screens = useBreakpoint();
  const [drawerVisible, setDrawerVisible] = useState(false);

  const isMobile = !screens.md;

  const hasRole = (roles: string[]) => !!auth.role && roles.includes(auth.role);

  const items = [
    { key: "/", label: <Link to="/">Журнал испытаний</Link> },
    ...(hasRole(["Assistant"])
      ? [{ key: "/lab", label: <Link to="/lab">Рабочее место лаборатории</Link> }]
      : []),
    ...(hasRole(["Engineer"]) ? [{ key: "/engineer/team", label: <Link to="/engineer/team">Мои лаборанты</Link> }] : []),
    ...(hasRole(["Admin", "Engineer"]) ? [{ key: "/products", label: <Link to="/products">Брак и продукция</Link> }] : []),
    ...(hasRole(["Admin", "Engineer"]) ? [{ key: "/reports", label: <Link to="/reports">Отчеты</Link> }] : []),
    ...(hasRole(["Admin", "Engineer"]) ? [{ key: "/admin", label: <Link to="/admin">Справочники</Link> }] : []),
    ...(hasRole(["Admin"]) ? [{ key: "/admin/organization", label: <Link to="/admin/organization">Организация</Link> }] : [])
  ];

  const selectedKey = location.pathname.startsWith("/lab")
    ? "/lab"
    : location.pathname.startsWith("/engineer/team")
      ? "/engineer/team"
      : location.pathname.startsWith("/products")
        ? "/products"
        : location.pathname.startsWith("/reports")
          ? "/reports"
          : location.pathname.startsWith("/admin/organization")
            ? "/admin/organization"
          : location.pathname.startsWith("/admin")
            ? "/admin"
            : "/";

  const isDark = mode === "dark";

  return (
    <Layout style={{ minHeight: "100vh" }}>
      <Header
        style={{
          display: "flex",
          alignItems: "center",
          gap: 16,
          borderBottom: `1px solid ${token.colorBorderSecondary}`,
          boxShadow: isDark ? "0 2px 8px rgba(0,0,0,0.45)" : "0 2px 8px rgba(0,0,0,0.06)",
          background: token.colorBgContainer,
          padding: isMobile ? "0 12px" : "0 24px",
          position: "sticky",
          top: 0,
          zIndex: 1000,
          width: "100%"
        }}
      >
        {isMobile && (
          <Button
            type="text"
            icon={<MenuOutlined />}
            onClick={() => setDrawerVisible(true)}
            style={{ fontSize: "18px" }}
          />
        )}
        <Image
          src="/Bmz-removebg-preview.png"
          alt="БМЗ"
          preview={false}
          style={{
            height: isMobile ? 32 : 40,
            width: "auto",
            objectFit: "contain",
            background: isDark ? "rgba(255,255,255,0.85)" : "#ffffff",
            borderRadius: 4,
            padding: 4
          }}
        />
        {!isMobile && (
          <Typography.Text
            strong
            style={{
              fontSize: 18,
              whiteSpace: "nowrap"
            }}
          >
            Контроль качества
          </Typography.Text>
        )}
        {!isMobile ? (
          <Menu
            theme={isDark ? "dark" : "light"}
            mode="horizontal"
            selectedKeys={[selectedKey]}
            items={items}
            style={{ flex: 1, minWidth: 0, background: "transparent", borderBottom: "none" }}
          />
        ) : (
          <div style={{ flex: 1 }} />
        )}
        {!isMobile && (
          <Typography.Text>
            {auth.fullName ?? "Пользователь"}
            {(auth.role === "Engineer" || auth.role === "Assistant") && auth.laboratoryName && (
              <span style={{ marginLeft: 8, opacity: 0.8 }}>— {auth.laboratoryName}</span>
            )}
          </Typography.Text>
        )}
        <ThemeToggleButton />
        <Button
          type="primary"
          ghost={!isDark}
          danger
          onClick={() => {
            auth.logout();
            navigate("/login");
          }}
          size={isMobile ? "small" : "middle"}
        >
          {isMobile ? "Выход" : "Выйти"}
        </Button>
      </Header>

      <Drawer
        title="Меню"
        placement="left"
        onClose={() => setDrawerVisible(false)}
        open={drawerVisible}
        styles={{ body: { padding: 0 } }}
        width={280}
      >
        <div style={{ padding: "16px", borderBottom: "1px solid #f0f0f0" }}>
          <Typography.Text strong>{auth.fullName ?? "Пользователь"}</Typography.Text>
          <br />
          <Typography.Text type="secondary" style={{ fontSize: "12px" }}>
            {auth.role} {auth.laboratoryName ? `— ${auth.laboratoryName}` : ""}
          </Typography.Text>
        </div>
        <Menu
          mode="vertical"
          selectedKeys={[selectedKey]}
          items={items}
          onClick={() => setDrawerVisible(false)}
          style={{ borderRight: "none" }}
        />
      </Drawer>

      <Content style={{ padding: isMobile ? 12 : 24 }}>
        <Outlet />
      </Content>
    </Layout>
  );
}
