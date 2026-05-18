import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

const toggleThemeMock = vi.fn();

vi.mock("../theme/ThemeContext", () => ({
  useTheme: () => ({
    mode: "dark",
    toggleTheme: toggleThemeMock
  })
}));

import ThemeToggleButton from "../components/ThemeToggleButton";

describe("ThemeToggleButton", () => {
  it("calls toggleTheme on click", async () => {
    toggleThemeMock.mockReset();

    render(<ThemeToggleButton showTooltip={false} />);

    const user = userEvent.setup();
    await user.click(screen.getByRole("button"));

    expect(toggleThemeMock).toHaveBeenCalledTimes(1);
  });
});

