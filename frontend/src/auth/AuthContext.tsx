import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { http, setUnauthorizedHandler } from "../api/http";
import { clearAccessToken, getAccessToken, saveAccessToken } from "./tokenStorage";
import { extractName, extractRole } from "./jwt";

type AuthState = {
  token: string | null;
  role: string | null;
  fullName: string | null;
  laboratoryId: number | null;
  laboratoryName: string | null;
};

type AuthContextValue = AuthState & {
  isAuthenticated: boolean;
  login: (token: string) => void;
  logout: () => void;
  refreshMe: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function getInitialState(): AuthState {
  const token = getAccessToken();
  return {
    token,
    role: token ? extractRole(token) : null,
    fullName: token ? extractName(token) : null,
    laboratoryId: null,
    laboratoryName: null
  };
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<AuthState>(getInitialState);
  const resetAuth = useCallback(() => {
    clearAccessToken();
    setState({
      token: null,
      role: null,
      fullName: null,
      laboratoryId: null,
      laboratoryName: null
    });
  }, []);

  const refreshMe = useCallback(async () => {
    const token = getAccessToken();
    if (!token) return;
    try {
      const res = await http.get<{ fullName: string; role: string; laboratoryId?: number; laboratoryName?: string }>("/auth/me");
      setState((prev) => ({
        ...prev,
        fullName: res.data.fullName,
        role: res.data.role,
        laboratoryId: res.data.laboratoryId ?? null,
        laboratoryName: res.data.laboratoryName ?? null
      }));
    } catch {
      // ignore
    }
  }, []);

  useEffect(() => {
    if (state.token) void refreshMe();
  }, [state.token, refreshMe]);

  useEffect(() => {
    setUnauthorizedHandler(() => {
      resetAuth();
    });
    return () => setUnauthorizedHandler(null);
  }, [resetAuth]);

  const value = useMemo<AuthContextValue>(() => {
    return {
      ...state,
      isAuthenticated: !!state.token,
      login: (token: string) => {
        saveAccessToken(token);
        setState({
          token,
          role: extractRole(token),
          fullName: extractName(token),
          laboratoryId: null,
          laboratoryName: null
        });
      },
      logout: () => {
        resetAuth();
      },
      refreshMe
    };
  }, [state, refreshMe, resetAuth]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used inside AuthProvider");
  }
  return context;
}
