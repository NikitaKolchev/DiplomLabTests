import { Button, Tooltip } from "antd";
import { MoonOutlined, SunOutlined } from "@ant-design/icons";
import { useTheme } from "../theme/ThemeContext";

interface ThemeToggleButtonProps {
  showTooltip?: boolean;
  className?: string;
  style?: React.CSSProperties;
}

export default function ThemeToggleButton({ 
  showTooltip = true, 
  className, 
  style 
}: ThemeToggleButtonProps) {
  const { mode, toggleTheme } = useTheme();
  const isDark = mode === "dark";

  const button = (
    <Button
      type="text"
      icon={isDark ? <SunOutlined /> : <MoonOutlined />}
      onClick={toggleTheme}
      className={className}
      style={{ 
        fontSize: "16px", 
        display: "flex", 
        alignItems: "center", 
        justifyContent: "center",
        ...style 
      }}
    />
  );

  if (!showTooltip) return button;

  return (
    <Tooltip title={isDark ? "Светлая тема" : "Тёмная тема"} placement="bottom">
      {button}
    </Tooltip>
  );
}
