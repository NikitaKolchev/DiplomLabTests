/**
 * Настройка Axios для выполнения HTTP-запросов к API.
 * Включает базовый URL, интерцепторы для передачи JWT токена и обработку ошибок авторизации.
 */
import axios from "axios";
import { getAccessToken } from "../auth/tokenStorage";

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5287/api";
let unauthorizedHandler: (() => void) | null = null;

/**
 * Устанавливает колбэк, который будет вызван при получении ошибки 401 Unauthorized.
 * Используется в AuthContext для автоматического выхода пользователя.
 */
export function setUnauthorizedHandler(handler: (() => void) | null): void {
  unauthorizedHandler = handler;
}

export const http = axios.create({
  baseURL: API_BASE_URL
});

/**
 * Интерцептор запроса: автоматически добавляет заголовок Authorization с Bearer токеном,
 * если он присутствует в локальном хранилище.
 */
http.interceptors.request.use((config) => {
  const token = getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

/**
 * Интерцептор ответа: обрабатывает ошибки. 
 * Если API возвращает 401, вызывается обработчик разлогина.
 */
http.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error?.response?.status === 401) {
      unauthorizedHandler?.();
    }
    return Promise.reject(error);
  }
);
