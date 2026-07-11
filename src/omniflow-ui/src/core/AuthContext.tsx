import React, { createContext, useContext, useState, useEffect } from 'react';
import type { ReactNode } from 'react';
import { jwtDecode } from 'jwt-decode';
import axios from 'axios';

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
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // Initial load
  useEffect(() => {
    const token = localStorage.getItem('access_token');
    if (token) {
      try {
        const decoded = jwtDecode<DecodedToken>(token);
        // Check if expired
        if (decoded.exp * 1000 < Date.now()) {
          localStorage.removeItem('access_token');
          localStorage.removeItem('refresh_token');
        } else {
          const roles = Array.isArray(decoded.role) ? decoded.role : [decoded.role].filter(Boolean);
          setUser({
            id: decoded.sub,
            email: decoded.email,
            name: decoded.name,
            roles,
            tenantId: decoded.tenant_id
          });
        }
      } catch (err) {
        console.error("Invalid token", err);
        localStorage.removeItem('access_token');
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

  const login = async (username: string, password: string) => {
    try {
      const params = new URLSearchParams();
      params.append('grant_type', 'password');
      params.append('username', username);
      params.append('password', password);
      params.append('scope', 'openid email profile roles offline_access');

      // Call token endpoint directly using axios (not the interceptor one to avoid looping on 401s)
      const response = await axios.post('http://localhost:5226/connect/token', params, {
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded'
        }
      });

      const { access_token, refresh_token } = response.data;
      
      localStorage.setItem('access_token', access_token);
      if (refresh_token) {
        localStorage.setItem('refresh_token', refresh_token);
      }

      const decoded = jwtDecode<DecodedToken>(access_token);
      const roles = Array.isArray(decoded.role) ? decoded.role : [decoded.role].filter(Boolean);
      
      setUser({
        id: decoded.sub,
        email: decoded.email,
        name: decoded.name,
        roles,
        tenantId: decoded.tenant_id
      });

    } catch (error) {
      console.error("Login failed", error);
      throw error;
    }
  };

  const logout = () => {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, isLoading, login, logout }}>
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
