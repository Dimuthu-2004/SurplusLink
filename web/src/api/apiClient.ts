import axios, { AxiosError, type AxiosInstance } from 'axios';
import { sessionTokenStorage } from '../auth/tokenStorage';
import { environment } from '../config/environment';

interface ErrorPayload {
  code?: unknown;
  message?: unknown;
  detail?: unknown;
  title?: unknown;
  errors?: unknown;
}

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status?: number,
    public readonly validationErrors?: Record<string, string[]>,
    public readonly code?: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export const apiClient: AxiosInstance = axios.create({
  baseURL: environment.apiBaseUrl,
  timeout: 15_000,
  headers: { Accept: 'application/json' },
});

apiClient.interceptors.request.use((config) => {
  const token = sessionTokenStorage.read();
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`);
  }
  return config;
});

let unauthorizedHandler: (() => void) | undefined;

export function registerUnauthorizedHandler(handler: () => void): () => void {
  unauthorizedHandler = handler;
  return () => {
    if (unauthorizedHandler === handler) {
      unauthorizedHandler = undefined;
    }
  };
}

apiClient.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    const normalized = normalizeApiError(error);
    const url = axios.isAxiosError(error) ? error.config?.url : undefined;
    const isLogin = typeof url === 'string' && url.includes('/api/auth/login');
    if (normalized.status === 401 && !isLogin) {
      unauthorizedHandler?.();
    }
    return Promise.reject(normalized);
  },
);

export function normalizeApiError(error: unknown): ApiError {
  if (error instanceof ApiError) {
    return error;
  }
  if (!axios.isAxiosError(error)) {
    return new ApiError(
      error instanceof Error ? error.message : 'An unexpected error occurred.',
    );
  }

  const axiosError = error as AxiosError<ErrorPayload>;
  if (axiosError.code === AxiosError.ECONNABORTED) {
    return new ApiError('The server took too long to respond.');
  }
  if (!axiosError.response) {
    return new ApiError('Unable to connect to the server. Please check your network connection.');
  }

  const payload = axiosError.response.data;
  const validationErrors = parseValidationErrors(payload?.errors);
  const validationMessage = Object.values(validationErrors).flat().join(' ');
  const code = stringValue(payload?.code);

  if (axiosError.response.status === 401 && (code === 'INVALID_CREDENTIALS' || !payload?.message)) {
    return new ApiError('Invalid email or password.', 401, undefined, 'INVALID_CREDENTIALS');
  }

  const message =
    stringValue(payload?.message) ??
    stringValue(payload?.detail) ??
    stringValue(payload?.title) ??
    (validationMessage || (axiosError.response.status >= 500 ? 'A server error occurred. Please try again later.' : `Request failed with status ${axiosError.response.status}.`));

  return new ApiError(
    message,
    axiosError.response.status,
    Object.keys(validationErrors).length ? validationErrors : undefined,
    code,
  );
}

function parseValidationErrors(value: unknown): Record<string, string[]> {
  if (Array.isArray(value)) {
    return Object.fromEntries(value.flatMap((item) => {
      if (!item || typeof item !== 'object') return [];
      const record = item as Record<string, unknown>;
      const field = stringValue(record.field) ?? stringValue(record.property);
      if (!field) return [];
      const code = stringValue(record.code);
      const message = stringValue(record.message) ?? friendlyValidationMessage(code, field);
      return [[field, [message]]];
    }));
  }
  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    return {};
  }
  return Object.fromEntries(
    Object.entries(value).flatMap(([key, messages]) =>
      Array.isArray(messages)
        ? [[key, messages.map((message) => String(message))]]
        : [],
    ),
  );
}

function friendlyValidationMessage(code: string | undefined, field: string): string {
  const messages: Record<string, string> = {
    PRICE_REQUIRED: 'Enter the price for this item.',
    LOCATION_REQUIRED: 'Enter the pickup or delivery location.',
    QUANTITY_REQUIRED: 'Enter the quantity currently available or needed.',
  };
  return code ? (messages[code] ?? `Check ${field}.`) : `Check ${field}.`;
}

function stringValue(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim() ? value : undefined;
}
