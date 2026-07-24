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

  // Per-theme palette shared with the CSS variables in index.css.
  const palette = {
    light: { primary: '#0969da', bgBase: '#ffffff', bgContainer: '#ffffff', border: '#dde3ea', bgElevated: '#ffffff' },
    dark: { primary: '#2f81f7', bgBase: '#0d1117', bgContainer: '#161b22', border: '#30363d', bgElevated: '#1c2129' },
    'slate-blue': { primary: '#38bdf8', bgBase: '#1e2530', bgContainer: '#252d3a', border: '#3a4454', bgElevated: '#2b3442' },
  }[currentTheme];

  // Dynamically build Ant Design configuration. Compact everywhere: dense,
  // professional data surfaces are the product's core.
  const antConfig: ThemeConfig = {
    algorithm:
      currentTheme === 'light'
        ? [antTheme.defaultAlgorithm, antTheme.compactAlgorithm]
        : [antTheme.darkAlgorithm, antTheme.compactAlgorithm],
    token: {
      colorPrimary: palette.primary,
      colorInfo: palette.primary,
      colorBgBase: palette.bgBase,
      colorBgContainer: palette.bgContainer,
      colorBgElevated: palette.bgElevated,
      colorBorder: palette.border,
      colorLink: palette.primary,
      borderRadius: 8,
      borderRadiusSM: 6,
      borderRadiusLG: 12,
      fontFamily: `'Inter Variable', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif`,
      fontSize: 13,
      controlOutlineWidth: 2,
    },
    components: {
      Button: { fontWeight: 550, primaryShadow: 'none' },
      Modal: { borderRadiusLG: 14, titleFontSize: 15 },
      Menu: {
        itemBorderRadius: 6,
        activeBarBorderWidth: 0,
        itemSelectedBg: 'transparent', // handled by .sidebar CSS accent pill
      },
      Table: { headerSplitColor: 'transparent' },
      Card: { borderRadiusLG: 12 },
      Select: { optionSelectedFontWeight: 600 },
      Tag: { borderRadiusSM: 5 },
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
