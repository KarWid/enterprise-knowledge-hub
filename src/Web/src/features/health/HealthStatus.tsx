import { useTranslation } from 'react-i18next';
import { useGetHealthQuery } from '../../services/api/generated/api';

export function HealthStatus() {
  const { t } = useTranslation();
  const { data, isLoading, isError } = useGetHealthQuery();
  const health = data as { status?: string; database?: string } | undefined;

  if (isError) return <p>{t('health.unreachable')}</p>;
  if (isLoading) return <p>{t('health.checking')}</p>;
  return <p>{t('health.status', { status: health?.status, database: health?.database })}</p>;
}
