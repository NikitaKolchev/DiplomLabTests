import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";

const postMock = vi.fn();
const loginMock = vi.fn();

vi.mock("../api/http", () => ({
  http: {
    post: (...args: any[]) => postMock(...args)
  }
}));

vi.mock("../auth/AuthContext", () => ({
  useAuth: () => ({
    login: loginMock
  })
}));

vi.mock("../theme/ThemeContext", () => ({
  useTheme: () => ({
    mode: "light"
  })
}));

const messageErrorMock = vi.fn();

vi.mock("antd", async () => {
  const actual = await vi.importActual<any>("antd");
  return {
    ...actual,
    message: {
      ...actual.message,
      error: (...args: any[]) => messageErrorMock(...args)
    }
  };
});

import LoginPage from "../pages/LoginPage";

const routerFuture = { v7_startTransition: true, v7_relativeSplatPath: true } as any;

function getInputByLabelText(label: string): HTMLInputElement {
  const labelEl = screen.getByText(label, { selector: "label" });
  const formItem = labelEl.closest(".ant-form-item");
  const input = formItem?.querySelector("input");
  if (!input) {
    throw new Error(`Не найден input для поля: ${label}`);
  }
  return input as HTMLInputElement;
}

describe("LoginPage", () => {
  beforeEach(() => {
    postMock.mockReset();
    loginMock.mockReset();
    messageErrorMock.mockReset();
  });

  it("submits form, stores token and navigates to / on success", async () => {
    postMock.mockResolvedValue({ data: { token: "jwt-token" } });

    render(
      <MemoryRouter future={routerFuture} initialEntries={["/login"]}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/" element={<div data-testid="home-page">Home</div>} />
        </Routes>
      </MemoryRouter>
    );

    const user = userEvent.setup();

    await user.type(getInputByLabelText("Логин"), "user");
    await user.type(getInputByLabelText("Пароль"), "password");
    await user.click(screen.getByRole("button", { name: "Войти" }));

    await waitFor(() => {
      expect(postMock).toHaveBeenCalledWith("/auth/login", {
        username: "user",
        password: "password"
      });
    });

    expect(loginMock).toHaveBeenCalledWith("jwt-token");
    expect(screen.getByTestId("home-page")).toBeInTheDocument();
    expect(messageErrorMock).not.toHaveBeenCalled();
  });

  it("shows error when server response does not contain token", async () => {
    postMock.mockResolvedValue({ data: {} });

    render(
      <MemoryRouter future={routerFuture}>
        <LoginPage />
      </MemoryRouter>
    );

    const user = userEvent.setup();

    await user.type(getInputByLabelText("Логин"), "user");
    await user.type(getInputByLabelText("Пароль"), "password");
    await user.click(screen.getByRole("button", { name: "Войти" }));

    await waitFor(() => {
      expect(messageErrorMock).toHaveBeenCalledWith("Неверный формат ответа сервера");
    });

    expect(loginMock).not.toHaveBeenCalled();
  });

  it("shows error when login request fails", async () => {
    postMock.mockRejectedValue(new Error("401"));

    render(
      <MemoryRouter future={routerFuture}>
        <LoginPage />
      </MemoryRouter>
    );

    const user = userEvent.setup();

    await user.type(getInputByLabelText("Логин"), "user");
    await user.type(getInputByLabelText("Пароль"), "wrong");
    await user.click(screen.getByRole("button", { name: "Войти" }));

    await waitFor(() => {
      expect(messageErrorMock).toHaveBeenCalledWith("Неверный логин или пароль");
    });

    expect(loginMock).not.toHaveBeenCalled();
  });
});
