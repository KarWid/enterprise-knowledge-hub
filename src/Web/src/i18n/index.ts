import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import en from './locales/en';
import pl from './locales/pl';

export const supportedLanguages = ['en', 'pl'] as const;

export type ApplicationLanguage = (typeof supportedLanguages)[number];

const languageStorageKey = 'enterprise-knowledge-hub.language';

function isApplicationLanguage(value: string | null): value is ApplicationLanguage {
  return supportedLanguages.some((language) => language === value);
}

function readStoredLanguage(): ApplicationLanguage | null {
  try {
    const language = window.localStorage.getItem(languageStorageKey);
    return isApplicationLanguage(language) ? language : null;
  } catch {
    return null;
  }
}

function resolveInitialLanguage(): ApplicationLanguage {
  const storedLanguage = readStoredLanguage();
  if (storedLanguage !== null) {
    return storedLanguage;
  }

  return window.navigator.language.toLowerCase().startsWith('pl') ? 'pl' : 'en';
}

function updateDocumentLanguage(language: ApplicationLanguage) {
  document.documentElement.lang = language;
}

const initialLanguage = resolveInitialLanguage();
updateDocumentLanguage(initialLanguage);

i18n
  .use(initReactI18next)
  .init({
    resources: {
      en: { translation: en },
      pl: { translation: pl },
    },
    lng: initialLanguage,
    fallbackLng: 'en',
    supportedLngs: supportedLanguages,
    interpolation: {
      escapeValue: false,
    },
  });

export async function changeApplicationLanguage(language: ApplicationLanguage) {
  await i18n.changeLanguage(language);
  updateDocumentLanguage(language);

  try {
    window.localStorage.setItem(languageStorageKey, language);
  } catch {
    // The selected language still applies when browser storage is unavailable.
  }
}

export default i18n;
