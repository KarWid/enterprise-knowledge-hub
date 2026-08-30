import { useTranslation } from 'react-i18next';

export function DocumentsPage() {
  const { t } = useTranslation();

  return <h1>{t('nav.documents')}</h1>;
}
