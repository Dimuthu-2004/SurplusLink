import { AxiosError } from 'axios';
import { afterEach, describe, expect, it } from 'vitest';
import { apiClient, normalizeApiError } from '../api/apiClient';

const originalAdapter = apiClient.defaults.adapter;

afterEach(() => {
  apiClient.defaults.adapter = originalAdapter;
});

describe('API client', () => {
  it('adds the stored JWT as a bearer authorization header', async () => {
    window.sessionStorage.setItem('surpluslink.jwt', 'stored-jwt');
    apiClient.defaults.adapter = async (config) => {
      expect(config.headers.get('Authorization')).toBe('Bearer stored-jwt');
      expect(config.baseURL).toBe('http://localhost:5170');
      return { data: {}, status: 200, statusText: 'OK', headers: {}, config };
    };

    await apiClient.get('/api/probe');
  });

  it('normalizes ASP.NET validation problem details', () => {
    const error = new AxiosError('Bad request', 'ERR_BAD_REQUEST', undefined, undefined, {
      data: {
        title: 'Validation failed.',
        errors: { Email: ['The Email field is required.'] },
      },
      status: 400,
      statusText: 'Bad Request',
      headers: {},
      config: { headers: undefined as never },
    });

    const normalized = normalizeApiError(error);

    expect(normalized.status).toBe(400);
    expect(normalized.message).toBe('Validation failed.');
    expect(normalized.validationErrors).toEqual({
      Email: ['The Email field is required.'],
    });
  });
});
