export interface TokenStorage {
  read(): string | null;
  write(token: string): void;
  clear(): void;
}

const tokenKey = 'surpluslink.jwt';

export const sessionTokenStorage: TokenStorage = {
  read: () => window.sessionStorage.getItem(tokenKey),
  write: (token) => window.sessionStorage.setItem(tokenKey, token),
  clear: () => window.sessionStorage.removeItem(tokenKey),
};
