import axios, { AxiosError, type AxiosInstance } from 'axios';
import { sessionTokenStorage } from '../auth/tokenStorage';
import { environment } from '../config/environment';

interface ErrorPayload {
  message?: unknown;
  title?: unknown;
  errors?: unknown;
}

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status?: number,
    public readonly validationErrors?: Record<string, string[]>,
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
    if (normalized.status === 401) {
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
    return new ApiError('Unable to connect to the SurplusLink API.');
  }

  const payload = axiosError.response.data;
  const validationErrors = parseValidationErrors(payload?.errors);
  const validationMessage = Object.values(validationErrors).flat().join(' ');
  const message =
    stringValue(payload?.message) ??
    stringValue(payload?.title) ??
    (validationMessage || `Request failed with status ${axiosError.response.status}.`);

  return new ApiError(
    message,
    axiosError.response.status,
    Object.keys(validationErrors).length ? validationErrors : undefined,
  );
}

function parseValidationErrors(value: unknown): Record<string, string[]> {
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

function stringValue(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim() ? value : undefined;
}
