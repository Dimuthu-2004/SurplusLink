import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';

export type AppLanguage = 'en' | 'si' | 'ta';
const storageKey = 'surpluslink.language';
const labels: Record<AppLanguage, string> = { en: 'English', si: 'සිංහල', ta: 'தமிழ்' };
const dictionary: Record<AppLanguage, Record<string, string>> = {
  en: { language: 'Language', listing: 'What are you listing?', searchItems: 'Search construction items...', category: 'Category', preferences: 'Preferences (optional)', marketplace: 'Marketplace', myActivity: 'My Activity', findMaterials: 'Find surplus construction materials', browseCategories: 'Browse by category', backToCategories: 'Back to categories', activeListings: 'active listings', searchMaterials: 'Search materials...', allConditions: 'All conditions', sortNewest: 'Newest listings', clearFilters: 'Clear filters', filters: 'Filters', loading: 'Loading…', noListings: 'No active listings found.', findBestMatch: 'Find my best match with AI', handoffTitle: 'Want the best match for your requirement?', handoffDescription: 'SurplusLink AI compares available materials using your required quantity, budget, location and delivery needs.', continueQr: 'Show QR code', notNow: 'Not now', selectedCategory: 'Selected category', scanQr: 'Open SurplusLink Mobile and scan this QR code to continue.', packageCountRequired: 'Enter the number of packages available.', quantityRequired: 'Enter the quantity available.', priceRequired: 'Enter the price for this item.', required: 'This field is required.', correctFields: 'Please correct the highlighted fields.' },
  si: { language: 'භාෂාව', listing: 'ඔබ ලැයිස්තුගත කරන්නේ කුමක්ද?', searchItems: 'ඉදිකිරීම් අයිතම සොයන්න...', category: 'කාණ්ඩය', preferences: 'කැමැත්තන් (විකල්ප)' },
  ta: { language: 'மொழி', listing: 'நீங்கள் பட்டியலிடுவது என்ன?', searchItems: 'கட்டுமானப் பொருட்களைத் தேடுங்கள்...', category: 'வகை', preferences: 'விருப்பங்கள் (விருப்பத்தேர்வு)' },
};

Object.assign(dictionary.en, {
  listingSubmitted: 'Listing submitted',
  listingSubmittedMessage: 'Your material was sent for manager review.',
  findingSuitableMatches: 'Finding suitable matches...',
  matchingWorkflowMessage: 'Our matching workflow is checking available sellers.',
  stillWorkingOnMatches: 'Still working on your matches...',
  matchingCouldNotComplete: 'Matching could not be completed.',
  retryMatching: 'Retry Matching',
});
Object.assign(dictionary.si, {
  listingSubmitted: 'ලැයිස්තුගත කිරීම ඉදිරිපත් කරන ලදී',
  listingSubmittedMessage: 'ඔබගේ ද්‍රව්‍යය කළමනාකරු සමාලෝචනයට යොමු කරන ලදී.',
  findingSuitableMatches: 'සුදුසු ගැළපීම් සොයමින්...',
  matchingWorkflowMessage: 'අපගේ ගැළපීම් කාර්ය ප්‍රවාහය පවතින විකුණුම්කරුවන් පරීක්ෂා කරයි.',
  stillWorkingOnMatches: 'ඔබගේ ගැළපීම් තවමත් සකසමින්...',
  matchingCouldNotComplete: 'ගැළපීම් සම්පූර්ණ කළ නොහැකි විය.',
  retryMatching: 'ගැළපීම නැවත උත්සාහ කරන්න',
});
Object.assign(dictionary.ta, {
  listingSubmitted: 'பட்டியல் சமர்ப்பிக்கப்பட்டது',
  listingSubmittedMessage: 'உங்கள் பொருள் மேலாளர் மதிப்பாய்வுக்கு அனுப்பப்பட்டது.',
  findingSuitableMatches: 'பொருத்தமான பொருத்தங்களைக் கண்டறிகிறது...',
  matchingWorkflowMessage: 'எங்கள் பொருத்தப் பணிப்பாய்வு கிடைக்கும் விற்பனையாளர்களைச் சரிபார்க்கிறது.',
  stillWorkingOnMatches: 'உங்கள் பொருத்தங்களில் இன்னும் பணிபுரிகிறது...',
  matchingCouldNotComplete: 'பொருத்தத்தை முடிக்க முடியவில்லை.',
  retryMatching: 'பொருத்தத்தை மீண்டும் முயற்சிக்கவும்',
});

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

// Canonical catalog values remain unchanged in the API.  This display map is a
// client concern so saved records and matching inputs are never language-specific.
const catalogDisplay: Record<string, Partial<Record<AppLanguage, string>>> = {
  'Rapid Hardening': { si: 'ඉක්මන් දැඩිවන', ta: 'விரைவாக உறையும்' },
  Tiles: { si: 'ටයිල්', ta: 'டைல்ஸ்' },
  Paint: { si: 'තීන්ත', ta: 'பெயிண்ட்' },
  Cement: { si: 'සිමෙන්ති', ta: 'சிமெண்டு' },
  'Structural & Masonry': { si: 'ව්‍යුහාත්මක සහ පෙදරේරු', ta: 'கட்டமைப்பு மற்றும் கல் வேலை' },
  'Concrete & Aggregates': { si: 'කොන්ක්‍රීට් සහ මිශ්‍ර ද්‍රව්‍ය', ta: 'கான்கிரீட் மற்றும் கலவைகள்' },
  Finishes: { si: 'නිමාවන්', ta: 'முடிப்புகள்' },
  'Construction Tools': { si: 'ඉදිකිරීම් මෙවලම්', ta: 'கட்டுமானக் கருவிகள்' },
  'Machinery / Equipment': { si: 'යන්ත්‍රෝපකරණ', ta: 'இயந்திரங்கள் / உபகரணங்கள்' },
};

export function catalogLabel(value: string, language: AppLanguage): string {
  return catalogDisplay[value]?.[language] ?? value;
}

export function LanguageSelector() {
  const { language, setLanguage, t } = useLanguage();
  return <label className="language-selector">🌐 <span className="sr-only">{t('language')}</span><select aria-label={t('language')} value={language} onChange={event => setLanguage(event.target.value as AppLanguage)}>{(Object.keys(labels) as AppLanguage[]).map(code => <option key={code} value={code}>{labels[code]}</option>)}</select></label>;
}
