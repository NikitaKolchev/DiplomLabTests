import { beforeEach, describe, expect, it, vi } from "vitest";

const getAccessTokenMock = vi.fn<() => string | null>();

vi.mock("../auth/tokenStorage", () => ({
  getAccessToken: () => getAccessTokenMock()
}));

import { http, setUnauthorizedHandler } from "../api/http";

describe("http", () => {
  beforeEach(() => {
    getAccessTokenMock.mockReset();
    setUnauthorizedHandler(null);
  });

  it("adds Authorization header when token exists", () => {
    getAccessTokenMock.mockReturnValue("token-123");

    const requestInterceptor = (http.interceptors.request as any).handlers[0].fulfilled as (
      config: any
    ) => any;

    const config = requestInterceptor({ headers: {} });
    expect(config.headers.Authorization).toBe("Bearer token-123");
  });

  it("does not add Authorization header when token is absent", () => {
    getAccessTokenMock.mockReturnValue(null);

    const requestInterceptor = (http.interceptors.request as any).handlers[0].fulfilled as (
      config: any
    ) => any;

    const config = requestInterceptor({ headers: {} });
    expect(config.headers.Authorization).toBeUndefined();
  });

  it("calls unauthorized handler on 401 responses", async () => {
    const onUnauthorized = vi.fn();
    setUnauthorizedHandler(onUnauthorized);

    const responseRejected = (http.interceptors.response as any).handlers[0].rejected as (
      error: any
    ) => Promise<never>;

    await expect(responseRejected({ response: { status: 401 } })).rejects.toBeTruthy();
    expect(onUnauthorized).toHaveBeenCalledTimes(1);
  });
});
