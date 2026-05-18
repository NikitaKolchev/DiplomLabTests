import { beforeEach, describe, expect, it, vi } from "vitest";
import { act, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

let unauthorizedHandler: (() => void) | null = null;

const getAccessTokenMock = vi.fn<() => string | null>();
const saveAccessTokenMock = vi.fn<(token: string) => void>();
const clearAccessTokenMock = vi.fn<() => void>();

const extractRoleMock = vi.fn<(token: string) => string | null>();
const extractNameMock = vi.fn<(token: string) => string | null>();

const httpGetMock = vi.fn();
const setUnauthorizedHandlerMock = vi.fn((handler: (() => void) | null) => {
  unauthorizedHandler = handler;
});

vi.mock("../auth/tokenStorage", () => ({
  getAccessToken: () => getAccessTokenMock(),
  saveAccessToken: (t: string) => saveAccessTokenMock(t),
  clearAccessToken: () => clearAccessTokenMock()
}));

vi.mock("../auth/jwt", () => ({
  extractRole: (t: string) => extractRoleMock(t),
  extractName: (t: string) => extractNameMock(t)
}));

vi.mock("../api/http", () => ({
  http: {
    get: (...args: any[]) => httpGetMock(...args)
  },
  setUnauthorizedHandler: (handler: (() => void) | null) => setUnauthorizedHandlerMock(handler)
}));

import { AuthProvider, useAuth } from "../auth/AuthContext";

function Consumer() {
  const auth = useAuth();

  return (
    <div>
      <div data-testid="is-auth">{String(auth.isAuthenticated)}</div>
      <div data-testid="role">{auth.role ?? ""}</div>
      <div data-testid="name">{auth.fullName ?? ""}</div>
      <div data-testid="lab-id">{auth.laboratoryId ?? ""}</div>
      <div data-testid="lab-name">{auth.laboratoryName ?? ""}</div>
      <button onClick={() => auth.login("token-1")}>login</button>
      <button onClick={() => auth.logout()}>logout</button>
      <button onClick={() => void auth.refreshMe()}>refresh</button>
    </div>
  );
}

describe("AuthContext", () => {
  beforeEach(() => {
    unauthorizedHandler = null;
    getAccessTokenMock.mockReset();
    saveAccessTokenMock.mockReset();
    clearAccessTokenMock.mockReset();
    extractRoleMock.mockReset();
    extractNameMock.mockReset();
    httpGetMock.mockReset();
    setUnauthorizedHandlerMock.mockReset();
  });

  it("initializes from stored token and registers unauthorized handler", () => {
    getAccessTokenMock.mockReturnValue("stored-token");
    extractRoleMock.mockReturnValue("Admin");
    extractNameMock.mockReturnValue("Иван Иванов");
    httpGetMock.mockRejectedValueOnce(new Error("ignore"));

    render(
      <AuthProvider>
        <Consumer />
      </AuthProvider>
    );

    expect(screen.getByTestId("is-auth")).toHaveTextContent("true");
    expect(screen.getByTestId("role")).toHaveTextContent("Admin");
    expect(screen.getByTestId("name")).toHaveTextContent("Иван Иванов");
    expect(setUnauthorizedHandlerMock).toHaveBeenCalledTimes(1);
    expect(unauthorizedHandler).toBeTypeOf("function");
  });

  it("login persists token and updates state", async () => {
    getAccessTokenMock.mockReturnValue(null);
    extractRoleMock.mockReturnValue("Engineer");
    extractNameMock.mockReturnValue("Петр Петров");

    render(
      <AuthProvider>
        <Consumer />
      </AuthProvider>
    );

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "login" }));

    expect(saveAccessTokenMock).toHaveBeenCalledWith("token-1");
    expect(screen.getByTestId("is-auth")).toHaveTextContent("true");
    expect(screen.getByTestId("role")).toHaveTextContent("Engineer");
    expect(screen.getByTestId("name")).toHaveTextContent("Петр Петров");
  });

  it("logout clears token and resets state", async () => {
    getAccessTokenMock.mockReturnValue(null);
    extractRoleMock.mockReturnValue("Admin");
    extractNameMock.mockReturnValue("Имя");

    render(
      <AuthProvider>
        <Consumer />
      </AuthProvider>
    );

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "login" }));
    await user.click(screen.getByRole("button", { name: "logout" }));

    expect(clearAccessTokenMock).toHaveBeenCalled();
    expect(screen.getByTestId("is-auth")).toHaveTextContent("false");
    expect(screen.getByTestId("role")).toHaveTextContent("");
    expect(screen.getByTestId("name")).toHaveTextContent("");
  });

  it("refreshMe updates profile fields from API", async () => {
    getAccessTokenMock.mockReturnValue("stored-token");
    extractRoleMock.mockReturnValue("Assistant");
    extractNameMock.mockReturnValue("Старое имя");
    httpGetMock.mockResolvedValue({
      data: { fullName: "Новое имя", role: "Admin", laboratoryId: 10, laboratoryName: "Лаб 1" }
    });

    render(
      <AuthProvider>
        <Consumer />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(httpGetMock).toHaveBeenCalledWith("/auth/me");
    });

    expect(screen.getByTestId("name")).toHaveTextContent("Новое имя");
    expect(screen.getByTestId("role")).toHaveTextContent("Admin");
    expect(screen.getByTestId("lab-id")).toHaveTextContent("10");
    expect(screen.getByTestId("lab-name")).toHaveTextContent("Лаб 1");
  });

  it("clears auth state when unauthorized handler is triggered", async () => {
    getAccessTokenMock.mockReturnValue(null);
    extractRoleMock.mockReturnValue("Admin");
    extractNameMock.mockReturnValue("Имя");

    render(
      <AuthProvider>
        <Consumer />
      </AuthProvider>
    );

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "login" }));

    expect(screen.getByTestId("is-auth")).toHaveTextContent("true");
    expect(unauthorizedHandler).toBeTypeOf("function");

    act(() => {
      unauthorizedHandler?.();
    });

    expect(clearAccessTokenMock).toHaveBeenCalled();
    await waitFor(() => {
      expect(screen.getByTestId("is-auth")).toHaveTextContent("false");
    });
  });
});
