import { describe, expect, it } from "vitest";
import { getAntdTheme } from "../theme/antdTheme";

describe("getAntdTheme", () => {
  it("returns dark theme tokens when mode is dark", () => {
    const cfg = getAntdTheme("dark");
    expect(cfg.token?.colorBgBase).toBe("#131820");
    expect(cfg.token?.colorTextBase).toBe("#e7edf5");
  });

  it("returns light theme tokens when mode is light", () => {
    const cfg = getAntdTheme("light");
    expect(cfg.token?.colorBgBase).toBe("#f5f7fa");
    expect(cfg.token?.colorTextBase).toBe("#1a202c");
  });
});

