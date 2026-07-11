import React, { createContext, useContext, useState, useEffect } from 'react';
import { ConfigProvider, theme as antTheme } from 'antd';
import type { ThemeConfig } from 'antd';

export type AppTheme = 'light' | 'dark' | 'slate-blue';

interface ThemeContextType {
  theme: AppTheme;
  setTheme: (theme: AppTheme) => void;
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [currentTheme, setCurrentTheme] = useState<AppTheme>(() => {
    return (localStorage.getItem('app-theme') as AppTheme) || 'slate-blue';
  });

  const setTheme = (theme: AppTheme) => {
    setCurrentTheme(theme);
    localStorage.setItem('app-theme', theme);
  };

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', currentTheme);
  }, [currentTheme]);

  // Dynamically build Ant Design configurations
  const antConfig: ThemeConfig = {
    algorithm:
      currentTheme === 'light'
        ? antTheme.defaultAlgorithm
        : [antTheme.darkAlgorithm, antTheme.compactAlgorithm],
    token: {
      colorPrimary: '#2f81f7',
      // Dynamic adjustments for custom slate-blue theme
      colorBgBase:
        currentTheme === 'light'
          ? '#ffffff'
          : currentTheme === 'slate-blue'
          ? '#1e2530'
          : '#0d1117',
      colorBgContainer:
        currentTheme === 'light'
          ? '#f6f8fa'
          : currentTheme === 'slate-blue'
          ? '#252d3a'
          : '#161b22',
      colorBorder:
        currentTheme === 'light'
          ? '#d0d7de'
          : currentTheme === 'slate-blue'
          ? '#3a4454'
          : '#30363d',
      fontFamily: `-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif`,
      fontSize: 13,
    },
  };

  return (
    <ThemeContext.Provider value={{ theme: currentTheme, setTheme }}>
      <ConfigProvider theme={antConfig}>
        {children}
      </ConfigProvider>
    </ThemeContext.Provider>
  );
}

export function useAppTheme() {
  const context = useContext(ThemeContext);
  if (!context) throw new Error('useAppTheme must be used within ThemeProvider');
  return context;
}
