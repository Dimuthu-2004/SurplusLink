import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useReducer,
  type ReactNode,
} from 'react';
import type { AxiosInstance } from 'axios';
import {
  apiClient,
  normalizeApiError,
  registerUnauthorizedHandler,
} from '../api/apiClient';
import {
  parseAuthResponse,
  parseUser,
  type AuthUser,
  type PublicRegistration,
} from './authTypes';
import {
  sessionTokenStorage,
  type TokenStorage,
} from './tokenStorage';

type AuthStatus = 'bootstrapping' | 'anonymous' | 'authenticated';

interface AuthState {
  status: AuthStatus;
  user: AuthUser | null;
  pending: boolean;
  error: string | null;
}

interface AuthContextValue extends AuthState {
  login(email: string, password: string): Promise<boolean>;
  register(request: PublicRegistration): Promise<boolean>;
  logout(): void;
  clearError(): void;
}

type AuthAction =
  | { type: 'RESTORED'; user: AuthUser }
  | { type: 'ANONYMOUS'; error?: string }
  | { type: 'REQUEST_STARTED' }
  | { type: 'AUTHENTICATED'; user: AuthUser }
  | { type: 'REQUEST_FAILED'; error: string }
  | { type: 'CLEAR_ERROR' };

const initialState: AuthState = {
  status: 'bootstrapping',
  user: null,
  pending: false,
  error: null,
};

export const AuthContext = createContext<AuthContextValue | null>(null);

interface AuthProviderProps {
  children: ReactNode;
  client?: Pick<AxiosInstance, 'get' | 'post'>;
  storage?: TokenStorage;
}

export function AuthProvider({
  children,
  client = apiClient,
  storage = sessionTokenStorage,
}: AuthProviderProps) {
  const [state, dispatch] = useReducer(authReducer, initialState);

  const logout = useCallback(() => {
    storage.clear();
    dispatch({ type: 'ANONYMOUS' });
  }, [storage]);

  useEffect(() => registerUnauthorizedHandler(logout), [logout]);

  useEffect(() => {
    let active = true;
    async function restoreSession() {
      if (!storage.read()) {
        dispatch({ type: 'ANONYMOUS' });
        return;
      }
      try {
        const response = await client.get('/api/auth/me');
        if (active) {
          dispatch({ type: 'RESTORED', user: parseUser(response.data) });
        }
      } catch (error) {
        storage.clear();
        if (active) {
          const apiError = normalizeApiError(error);
          dispatch({
            type: 'ANONYMOUS',
            error: apiError.status === 401 ? undefined : apiError.message,
          });
        }
      }
    }
    void restoreSession();
    return () => {
      active = false;
    };
  }, [client, storage]);

  const login = useCallback(
    async (email: string, password: string) => {
      dispatch({ type: 'REQUEST_STARTED' });
      try {
        const response = await client.post('/api/auth/login', {
          email: email.trim(),
          password,
        });
        const session = parseAuthResponse(response.data);
        storage.write(session.token);
        dispatch({ type: 'AUTHENTICATED', user: session.user });
        return true;
      } catch (error) {
        dispatch({
          type: 'REQUEST_FAILED',
          error: normalizeApiError(error).message,
        });
        return false;
      }
    },
    [client, storage],
  );

  const register = useCallback(
    async (request: PublicRegistration) => {
      dispatch({ type: 'REQUEST_STARTED' });
      try {
        const response = await client.post('/api/auth/register', {
          ...request,
          fullName: request.fullName.trim(),
          email: request.email.trim(),
          phoneNumber: request.phoneNumber.trim(),
          businessName: request.businessName.trim() || undefined,
          address: request.address.trim(),
        });
        const session = parseAuthResponse(response.data);
        storage.write(session.token);
        dispatch({ type: 'AUTHENTICATED', user: session.user });
        return true;
      } catch (error) {
        dispatch({
          type: 'REQUEST_FAILED',
          error: normalizeApiError(error).message,
        });
        return false;
      }
    },
    [client, storage],
  );

  const clearError = useCallback(() => dispatch({ type: 'CLEAR_ERROR' }), []);
  const value = useMemo(
    () => ({ ...state, login, register, logout, clearError }),
    [state, login, register, logout, clearError],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider.');
  }
  return context;
}

function authReducer(state: AuthState, action: AuthAction): AuthState {
  switch (action.type) {
    case 'RESTORED':
    case 'AUTHENTICATED':
      return {
        status: 'authenticated',
        user: action.user,
        pending: false,
        error: null,
      };
    case 'ANONYMOUS':
      return {
        status: 'anonymous',
        user: null,
        pending: false,
        error: action.error ?? null,
      };
    case 'REQUEST_STARTED':
      return { ...state, pending: true, error: null };
    case 'REQUEST_FAILED':
      return { ...state, pending: false, error: action.error };
    case 'CLEAR_ERROR':
      return { ...state, error: null };
    default:
      return state;
  }
}
