import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import { ThemeProvider, useTheme } from "../theme/ThemeContext";

function Consumer() {
  const { mode, toggleTheme } = useTheme();
  return (
    <div>
      <div data-testid="mode">{mode}</div>
      <button onClick={() => toggleTheme()}>toggle</button>
    </div>
  );
}

describe("ThemeContext", () => {
  beforeEach(() => {
    localStorage.clear();
    document.body.className = "";
    document.documentElement.style.colorScheme = "";
  });

  it("uses stored theme mode when present", () => {
    localStorage.setItem("labtests-theme-mode", "light");

    render(
      <ThemeProvider>
        <Consumer />
      </ThemeProvider>
    );

    expect(screen.getByTestId("mode")).toHaveTextContent("light");
    expect(document.body.className).toBe("theme-light");
    expect(document.documentElement.style.colorScheme).toBe("light");
  });

  it("toggles theme and persists to localStorage", async () => {
    localStorage.setItem("labtests-theme-mode", "dark");

    render(
      <ThemeProvider>
        <Consumer />
      </ThemeProvider>
    );

    const user = userEvent.setup();
    expect(screen.getByTestId("mode")).toHaveTextContent("dark");

    await user.click(screen.getByRole("button", { name: "toggle" }));

    expect(screen.getByTestId("mode")).toHaveTextContent("light");
    expect(localStorage.getItem("labtests-theme-mode")).toBe("light");
    expect(document.body.className).toBe("theme-light");
  });

  it("falls back to system preference when nothing is stored", () => {
    (window as any).matchMedia = (query: string) => ({
      matches: query.includes("light"),
      media: query,
      onchange: null,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      addListener: vi.fn(),
      removeListener: vi.fn(),
      dispatchEvent: vi.fn()
    });

    render(
      <ThemeProvider>
        <Consumer />
      </ThemeProvider>
    );

    expect(screen.getByTestId("mode")).toHaveTextContent("light");
  });
});

