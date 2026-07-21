import React, { createContext, useContext, useState, useEffect } from 'react';
import { api } from './api';

interface LanguageContextType {
  language: string;
  setLanguage: (lang: string) => void;
  t: (key: string, ...args: any[]) => string;
  loading: boolean;
  /** Refetches the dictionary — used after a tenant customizes a string. */
  reloadTranslations: () => void;
}

const LanguageContext = createContext<LanguageContextType | undefined>(undefined);

export function LanguageProvider({ children }: { children: React.ReactNode }) {
  const [language, setLangState] = useState(() => {
    return localStorage.getItem('app-lang') || 'en-US';
  });
  const [dictionary, setDictionary] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);

  const fetchDictionary = async (lang: string) => {
    setLoading(true);
    try {
      const response = await api.get(`/localization/${lang}`);
      setDictionary(response.data);
    } catch (error) {
      console.error('Failed to load localization dictionary from API', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDictionary(language);
  }, [language]);

  const reloadTranslations = () => { fetchDictionary(language); };

  const setLanguage = (lang: string) => {
    localStorage.setItem('app-lang', lang);
    // The language-change effect refetches the dictionary and every t() call
    // re-renders — no full page reload needed. Data grids pick up the new
    // Accept-Language header on their next fetch (navigation / refresh).
    setLangState(lang);
  };

  const t = (key: string, ...args: any[]) => {
    let value = dictionary[key];
    if (value === undefined) {
      // Missing key: a string first argument acts as the fallback text;
      // otherwise the raw key is shown so gaps stay visible during dev.
      value = typeof args[0] === 'string' ? args[0] : key;
    }
    args.forEach((arg, index) => {
      value = value!.replace(`{${index}}`, arg);
    });
    return value;
  };

  return (
    <LanguageContext.Provider value={{ language, setLanguage, t, loading, reloadTranslations }}>
      {children}
    </LanguageContext.Provider>
  );
}

export function useTranslation() {
  const context = useContext(LanguageContext);
  if (!context) throw new Error('useTranslation must be used within LanguageProvider');
  return context;
}
