import { ThemeConfig, theme } from "antd";

export const getAntdTheme = (mode: "light" | "dark"): ThemeConfig => {
  const isDark = mode === "dark";
  
  return {
    algorithm: isDark ? theme.darkAlgorithm : theme.defaultAlgorithm,
    token: {
      colorPrimary: "#f39c12",
      colorBgBase: isDark ? "#131820" : "#f5f7fa",
      colorTextBase: isDark ? "#e7edf5" : "#1a202c",
      borderRadius: 8,
      // Add more shared tokens here
    },
    components: {
      Layout: {
        headerBg: isDark ? "#10151c" : "#ffffff",
        bodyBg: isDark ? "#131820" : "#f5f7fa",
        triggerBg: isDark ? "#10151c" : "#ffffff"
      },
      Card: {
        colorBgContainer: isDark ? "#1a212c" : "#ffffff"
      },
      Table: {
        headerBg: isDark ? "#1a212c" : "#f7fafc",
        rowHoverBg: isDark ? "#263142" : "#ebf4ff"
      },
      Menu: {
        darkItemBg: isDark ? "#10151c" : "#ffffff",
        darkSubMenuItemBg: isDark ? "#10151c" : "#ffffff",
        darkItemSelectedBg: isDark ? "#2c3a4f" : "#ebf4ff",
        darkItemColor: isDark ? "#e7edf5" : "#1a202c"
      }
    }
  };
};
