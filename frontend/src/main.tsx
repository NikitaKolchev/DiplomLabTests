import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { ConfigProvider, theme } from "antd";
import ruRU from "antd/locale/ru_RU";
import App from "./App";
import { ThemeProvider, useTheme } from "./theme/ThemeContext";
import { getAntdTheme } from "./theme/antdTheme";
import "antd/dist/reset.css";
import "./theme.css";

function ThemedApp() {
  const { mode } = useTheme();

  return (
    <ConfigProvider
      locale={ruRU}
      theme={getAntdTheme(mode)}
    >
      <BrowserRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
        <App />
      </BrowserRouter>
    </ConfigProvider>
  );
}

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <ThemeProvider>
      <ThemedApp />
    </ThemeProvider>
  </React.StrictMode>
);
