import React, { createContext, useContext, useState, useEffect } from 'react';
import type { ReactNode } from 'react';
import { jwtDecode } from 'jwt-decode';
import axios from 'axios';
import { CONNECT_URL, TOKEN_KEY, REFRESH_TOKEN_KEY } from './config';

/**
 * Identity may emit the role claim under the bare "role" name or the SOAP
 * namespaced URI, and as a string or an array. Normalize all shapes to string[].
 */
const ROLE_CLAIM_URI = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
function extractRoles(decoded: Record<string, unknown>): string[] {
  const raw = decoded.role ?? decoded[ROLE_CLAIM_URI];
  if (Array.isArray(raw)) return raw.filter(Boolean) as string[];
  if (typeof raw === 'string') return [raw];
  return [];
}

interface DecodedToken {
  sub: string;
  email: string;
  name: string;
  role: string | string[];
  tenant_id: string;
  exp: number;
}

export interface User {
  id: string;
  email: string;
  name: string;
  roles: string[];
  tenantId: string;
}

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (username: string, password: string, mfaCode?: string) => Promise<void>;
  loginWithSsoTicket: (ticket: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // Initial load
  useEffect(() => {
    const token = localStorage.getItem(TOKEN_KEY);
    if (token) {
      try {
        const decoded = jwtDecode<DecodedToken>(token);
        // An expired access token is fine if a refresh token exists — the api
        // interceptor will silently refresh on the first 401. Only clear the
        // session when there's no way to recover.
        if (decoded.exp * 1000 < Date.now() && !localStorage.getItem(REFRESH_TOKEN_KEY)) {
          localStorage.removeItem(TOKEN_KEY);
          localStorage.removeItem(REFRESH_TOKEN_KEY);
        } else {
          setUser({
            id: decoded.sub,
            email: decoded.email,
            name: decoded.name,
            roles: extractRoles(decoded as unknown as Record<string, unknown>),
            tenantId: decoded.tenant_id
          });
        }
      } catch (err) {
        console.error("Invalid token", err);
        localStorage.removeItem(TOKEN_KEY);
      }
    }
    setIsLoading(false);

    // Listen for global auth errors from api.ts
    const handleAuthError = () => {
      logout();
    };
    window.addEventListener('auth-error', handleAuthError);
    return () => window.removeEventListener('auth-error', handleAuthError);
  }, []);

  // Shared by every grant: store tokens, decode, hydrate the user.
  const requestTokens = async (params: URLSearchParams) => {
    // Call token endpoint directly using axios (not the interceptor one to avoid looping on 401s)
    const response = await axios.post(`${CONNECT_URL}/token`, params, {
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded'
      }
    });

    const { access_token, refresh_token } = response.data;

    localStorage.setItem(TOKEN_KEY, access_token);
    if (refresh_token) {
      localStorage.setItem(REFRESH_TOKEN_KEY, refresh_token);
    }

    const decoded = jwtDecode<DecodedToken>(access_token);

    setUser({
      id: decoded.sub,
      email: decoded.email,
      name: decoded.name,
      roles: extractRoles(decoded as unknown as Record<string, unknown>),
      tenantId: decoded.tenant_id
    });
  };

  const login = async (username: string, password: string, mfaCode?: string) => {
    try {
      const params = new URLSearchParams();
      params.append('grant_type', 'password');
      params.append('username', username);
      params.append('password', password);
      if (mfaCode) {
        params.append('mfa_code', mfaCode);
      }
      params.append('scope', 'openid email profile roles offline_access');
      await requestTokens(params);
    } catch (error) {
      console.error("Login failed", error);
      throw error;
    }
  };

  // Redeems the one-time ticket minted by the SSO callback for real tokens.
  const loginWithSsoTicket = async (ticket: string) => {
    const params = new URLSearchParams();
    params.append('grant_type', 'urn:werkpilot:sso');
    params.append('ticket', ticket);
    params.append('scope', 'openid email profile roles offline_access');
    await requestTokens(params);
  };

  const logout = () => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, isLoading, login, loginWithSsoTicket, logout }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
