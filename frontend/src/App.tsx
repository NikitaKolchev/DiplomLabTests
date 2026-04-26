import { ConfigProvider, message } from "antd";
import ruRU from "antd/locale/ru_RU";
import { Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider } from "./auth/AuthContext";
import ProtectedRoute from "./components/ProtectedRoute";
import AppShell from "./layout/AppShell";
import AdminPage from "./pages/AdminPage";
import EngineerTeamPage from "./pages/EngineerTeamPage";
import GuestJournalPage from "./pages/GuestJournalPage";
import LabWorkbenchPage from "./pages/LabWorkbenchPage";
import LoginPage from "./pages/LoginPage";
import OrganizationAdminPage from "./pages/OrganizationAdminPage";
import ProductsPage from "./pages/ProductsPage";
import ReportsPage from "./pages/ReportsPage";
import TestResultsPage from "./pages/TestResultsPage";

message.config({
  maxCount: 3,
  duration: 4,
  prefixCls: "",
});

export default function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/guest" element={<GuestJournalPage />} />
        <Route
          path="/"
          element={
            <ProtectedRoute roles={["Admin", "Engineer", "Assistant"]}>
              <AppShell />
            </ProtectedRoute>
          }
        >
          <Route index element={<TestResultsPage />} />
          <Route
            path="lab"
            element={
              <ProtectedRoute roles={["Admin", "Engineer", "Assistant"]}>
                <LabWorkbenchPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="products"
            element={
              <ProtectedRoute roles={["Admin", "Engineer"]}>
                <ProductsPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="reports"
            element={
              <ProtectedRoute roles={["Admin", "Engineer"]}>
                <ReportsPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="admin"
            element={
              <ProtectedRoute roles={["Admin", "Engineer"]}>
                <AdminPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="admin/organization"
            element={
              <ProtectedRoute roles={["Admin"]}>
                <OrganizationAdminPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="engineer/team"
            element={
              <ProtectedRoute roles={["Engineer"]}>
                <EngineerTeamPage />
              </ProtectedRoute>
            }
          />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AuthProvider>
  );
}
