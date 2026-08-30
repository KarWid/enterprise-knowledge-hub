import { useTranslation } from 'react-i18next';

export function ChatsPage() {
  const { t } = useTranslation();

  return <h1>{t('nav.chats')}</h1>;
}
