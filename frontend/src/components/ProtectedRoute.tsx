import { Navigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

type Props = {
  roles?: string[];
  children: React.ReactNode;
};

export default function ProtectedRoute({ roles, children }: Props) {
  const auth = useAuth();

  if (!auth.isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (roles && roles.length > 0) {
    const hasRole = auth.role ? roles.includes(auth.role) : false;
    if (!hasRole) {
      return <Navigate to="/" replace />;
    }
  }

  return <>{children}</>;
}
