import { useTranslation } from 'react-i18next';
import { useGetHealthQuery } from '../../services/api/baseApi';

export function HealthStatus() {
  const { t } = useTranslation();
  const { data, isLoading, isError } = useGetHealthQuery();

  if (isError) return <p>{t('health.unreachable')}</p>;
  if (isLoading) return <p>{t('health.checking')}</p>;
  return <p>{t('health.status', { status: data?.status, database: data?.database })}</p>;
}
