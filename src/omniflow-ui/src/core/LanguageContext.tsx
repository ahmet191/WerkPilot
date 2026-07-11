import React, { createContext, useContext, useState, useEffect } from 'react';
import { api } from './api';

interface LanguageContextType {
  language: string;
  setLanguage: (lang: string) => void;
  t: (key: string, ...args: any[]) => string;
  loading: boolean;
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

  const setLanguage = (lang: string) => {
    localStorage.setItem('app-lang', lang);
    setLangState(lang);
    
    // Clean reload to ensure all Axios instances and active views fetch data fresh 
    // using the new Accept-Language headers.
    setTimeout(() => {
      window.location.reload();
    }, 100);
  };

  const t = (key: string, ...args: any[]) => {
    let value = dictionary[key] || key;
    args.forEach((arg, index) => {
      value = value.replace(`{${index}}`, arg);
    });
    return value;
  };

  return (
    <LanguageContext.Provider value={{ language, setLanguage, t, loading }}>
      {children}
    </LanguageContext.Provider>
  );
}

export function useTranslation() {
  const context = useContext(LanguageContext);
  if (!context) throw new Error('useTranslation must be used within LanguageProvider');
  return context;
}
