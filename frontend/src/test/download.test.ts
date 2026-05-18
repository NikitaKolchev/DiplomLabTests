import { afterEach, describe, expect, it, vi } from "vitest";

const httpGetMock = vi.fn();

vi.mock("../api/http", () => ({
  http: {
    get: (...args: any[]) => httpGetMock(...args)
  }
}));

import { downloadBlob } from "../utils/download";

describe("downloadBlob", () => {
  const originalCreateElement = document.createElement.bind(document);
  const originalCreateObjectURL = URL.createObjectURL;
  const originalRevokeObjectURL = URL.revokeObjectURL;
  const createObjectURLMock = vi.fn(() => "blob:mock");
  const revokeObjectURLMock = vi.fn();

  afterEach(() => {
    httpGetMock.mockReset();
    vi.restoreAllMocks();
    URL.createObjectURL = originalCreateObjectURL;
    URL.revokeObjectURL = originalRevokeObjectURL;
  });

  it("downloads blob via anchor element and revokes object URL", async () => {
    (URL as any).createObjectURL = createObjectURLMock;
    (URL as any).revokeObjectURL = revokeObjectURLMock;

    const clickSpy = vi.fn();

    vi.spyOn(document, "createElement").mockImplementation((tagName: any) => {
      const el = originalCreateElement(tagName);
      if (String(tagName).toLowerCase() === "a") {
        (el as any).click = clickSpy;
      }
      return el;
    });

    httpGetMock.mockResolvedValue({ data: new Blob(["data"], { type: "text/plain" }) });

    await downloadBlob("/reports/file", "file.txt");

    expect(httpGetMock).toHaveBeenCalledWith("/reports/file", { responseType: "blob" });
    expect(createObjectURLMock).toHaveBeenCalledTimes(1);
    expect(clickSpy).toHaveBeenCalledTimes(1);
    expect(revokeObjectURLMock).toHaveBeenCalledWith("blob:mock");
  });
});
