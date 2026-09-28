import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';

export type AppLanguage = 'en' | 'si' | 'ta';
const storageKey = 'surpluslink.language';
const labels: Record<AppLanguage, string> = { en: 'English', si: 'සිංහල', ta: 'தமிழ்' };
const dictionary: Record<AppLanguage, Record<string, string>> = {
  en: { language: 'Language', listing: 'What are you listing?', searchItems: 'Search construction items...', category: 'Category', preferences: 'Preferences (optional)' },
  si: { language: 'භාෂාව', listing: 'ඔබ ලැයිස්තුගත කරන්නේ කුමක්ද?', searchItems: 'ඉදිකිරීම් අයිතම සොයන්න...', category: 'කාණ්ඩය', preferences: 'කැමැත්තන් (විකල්ප)' },
  ta: { language: 'மொழி', listing: 'நீங்கள் பட்டியலிடுவது என்ன?', searchItems: 'கட்டுமானப் பொருட்களைத் தேடுங்கள்...', category: 'வகை', preferences: 'விருப்பங்கள் (விருப்பத்தேர்வு)' },
};

const LanguageContext = createContext<{ language: AppLanguage; setLanguage: (language: AppLanguage) => void; t: (key: string) => string }>({ language: 'en', setLanguage: () => {}, t: key => dictionary.en[key] ?? key });

export function LanguageProvider({ children }: { children: ReactNode }) {
  const [language, setLanguageState] = useState<AppLanguage>(() => (localStorage.getItem(storageKey) as AppLanguage) || 'en');
  const setLanguage = (next: AppLanguage) => setLanguageState(next);
  useEffect(() => { localStorage.setItem(storageKey, language); document.documentElement.lang = language; }, [language]);
  return <LanguageContext.Provider value={{ language, setLanguage, t: key => dictionary[language][key] ?? dictionary.en[key] ?? key }}>{children}</LanguageContext.Provider>;
}

export function useLanguage() { return useContext(LanguageContext); }
export function localized(value: unknown, language: AppLanguage, fallback = ''): string {
  if (value && typeof value === 'object') {
    const record = value as Record<string, unknown>;
    return String(record[language] ?? record.en ?? fallback);
  }
  return fallback;
}

export function LanguageSelector() {
  const { language, setLanguage, t } = useLanguage();
  return <label className="language-selector">🌐 <span className="sr-only">{t('language')}</span><select aria-label={t('language')} value={language} onChange={event => setLanguage(event.target.value as AppLanguage)}>{(Object.keys(labels) as AppLanguage[]).map(code => <option key={code} value={code}>{labels[code]}</option>)}</select></label>;
}
