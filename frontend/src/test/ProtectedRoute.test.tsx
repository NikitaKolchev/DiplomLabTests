import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import ProtectedRoute from "../components/ProtectedRoute";

const routerFuture = { v7_startTransition: true, v7_relativeSplatPath: true } as any;

const useAuthMock = vi.fn();

vi.mock("../auth/AuthContext", () => ({
  useAuth: () => useAuthMock()
}));

describe("ProtectedRoute", () => {
  it("renders children when user is authenticated", () => {
    useAuthMock.mockReturnValue({
      isAuthenticated: true,
      role: "Admin"
    });

    render(
      <MemoryRouter future={routerFuture}>
        <ProtectedRoute>
          <div data-testid="protected-content">Защищенный контент</div>
        </ProtectedRoute>
      </MemoryRouter>
    );

    expect(screen.getByTestId("protected-content")).toBeInTheDocument();
  });

  it("renders children when user has required role", () => {
    useAuthMock.mockReturnValue({
      isAuthenticated: true,
      role: "Admin"
    });

    render(
      <MemoryRouter future={routerFuture}>
        <ProtectedRoute roles={["Admin"]}>
          <div data-testid="protected-content">Контент для администратора</div>
        </ProtectedRoute>
      </MemoryRouter>
    );

    expect(screen.getByTestId("protected-content")).toBeInTheDocument();
  });

  it("redirects to /login when user is not authenticated", () => {
    useAuthMock.mockReturnValue({
      isAuthenticated: false,
      role: null
    });

    render(
      <MemoryRouter future={routerFuture} initialEntries={["/"]}>
        <Routes>
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <div>Secret</div>
              </ProtectedRoute>
            }
          />
          <Route path="/login" element={<div data-testid="login-page">Login</div>} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByTestId("login-page")).toBeInTheDocument();
  });

  it("redirects to / when user lacks required role", () => {
    useAuthMock.mockReturnValue({
      isAuthenticated: true,
      role: "Admin"
    });

    render(
      <MemoryRouter future={routerFuture} initialEntries={["/admin"]}>
        <Routes>
          <Route path="/" element={<div data-testid="home-page">Home</div>} />
          <Route
            path="/admin"
            element={
              <ProtectedRoute roles={["Engineer"]}>
                <div>Admin</div>
              </ProtectedRoute>
            }
          />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByTestId("home-page")).toBeInTheDocument();
  });
});
