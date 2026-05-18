import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import type { ReactElement } from "react";

const httpGetMock = vi.fn();
const httpPostMock = vi.fn();
const httpPutMock = vi.fn();
const httpDeleteMock = vi.fn();

vi.mock("../api/http", () => ({
  http: {
    get: (...args: any[]) => httpGetMock(...args),
    post: (...args: any[]) => httpPostMock(...args),
    put: (...args: any[]) => httpPutMock(...args),
    delete: (...args: any[]) => httpDeleteMock(...args)
  }
}));

vi.mock("../utils/download", () => ({
  downloadBlob: vi.fn().mockResolvedValue(undefined)
}));

vi.mock("../auth/AuthContext", () => ({
  useAuth: () => ({
    token: "t",
    role: "Admin",
    fullName: "Тест",
    laboratoryId: 1,
    laboratoryName: "Лаб",
    isAuthenticated: true,
    login: vi.fn(),
    logout: vi.fn(),
    refreshMe: vi.fn()
  })
}));

vi.mock("../theme/ThemeContext", () => ({
  useTheme: () => ({
    mode: "light",
    toggleTheme: vi.fn()
  })
}));

vi.mock("../components/statistics/StatisticsPdfExporter", () => ({
  default: () => null
}));

import AdminPage from "../pages/AdminPage";
import EngineerTeamPage from "../pages/EngineerTeamPage";
import GuestJournalPage from "../pages/GuestJournalPage";
import LabWorkbenchPage from "../pages/LabWorkbenchPage";
import OrganizationAdminPage from "../pages/OrganizationAdminPage";
import ProductsPage from "../pages/ProductsPage";
import ReportsPage from "../pages/ReportsPage";
import TestResultsPage from "../pages/TestResultsPage";

const routerFuture = { v7_startTransition: true, v7_relativeSplatPath: true } as any;

function renderWithRouter(path: string, element: ReactElement) {
  return render(
    <MemoryRouter future={routerFuture} initialEntries={[path]}>
      <Routes>
        <Route path={path} element={element} />
      </Routes>
    </MemoryRouter>
  );
}

describe("Pages smoke", () => {
  beforeEach(() => {
    httpGetMock.mockReset();
    httpPostMock.mockReset();
    httpPutMock.mockReset();
    httpDeleteMock.mockReset();

    httpGetMock.mockImplementation(async (url: string) => {
      switch (url) {
        case "/testresults":
          return { data: { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 } };
        case "/products":
          return { data: { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 } };
        case "/products/filters":
          return { data: { laboratories: [], wireCodes: [], customers: [] } };
        case "/reports/filters":
          return { data: { laboratories: [], wireCodes: [] } };
        case "/admin/utils/transliterate":
          return { data: "test.user" };
        case "/admin/utils/password":
          return { data: "P@ssw0rd!" };
        default:
          return { data: [] };
      }
    });

    httpPostMock.mockResolvedValue({ data: {} });
    httpPutMock.mockResolvedValue({ data: {} });
    httpDeleteMock.mockResolvedValue({ data: {} });
  });

  it("renders AdminPage", async () => {
    const { container } = renderWithRouter("/admin", <AdminPage />);
    await waitFor(() => expect(container).not.toBeEmptyDOMElement());
  });

  it("renders OrganizationAdminPage and loads organization data", async () => {
    renderWithRouter("/admin/organization", <OrganizationAdminPage />);
    await waitFor(() => expect(httpGetMock).toHaveBeenCalled());
  });

  it("renders EngineerTeamPage and loads assistants", async () => {
    renderWithRouter("/engineer/team", <EngineerTeamPage />);
    await waitFor(() => expect(httpGetMock).toHaveBeenCalledWith("/engineer/users/assistants", expect.anything()));
  });

  it("renders GuestJournalPage and loads initial data", async () => {
    renderWithRouter("/guest", <GuestJournalPage />);
    await waitFor(() => expect(httpGetMock).toHaveBeenCalledWith("/testresults", expect.anything()));
  });

  it("renders TestResultsPage and loads initial data", async () => {
    renderWithRouter("/", <TestResultsPage />);
    await waitFor(() => expect(httpGetMock).toHaveBeenCalledWith("/testresults", expect.anything()));
  });

  it("renders ProductsPage and loads filters + data", async () => {
    renderWithRouter("/products", <ProductsPage />);
    await waitFor(() => expect(httpGetMock).toHaveBeenCalledWith("/products/filters"));
    await waitFor(() => expect(httpGetMock).toHaveBeenCalledWith("/products", expect.anything()));
  });

  it("renders ReportsPage and loads filters", async () => {
    renderWithRouter("/reports", <ReportsPage />);
    await waitFor(() => expect(httpGetMock).toHaveBeenCalledWith("/reports/filters"));
  });

  it("renders LabWorkbenchPage", async () => {
    const { container } = renderWithRouter("/lab", <LabWorkbenchPage />);
    await waitFor(() => expect(container).not.toBeEmptyDOMElement());
  });
});
