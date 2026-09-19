import { useTranslation } from 'react-i18next';
import { useGetHealthQuery } from '../../services/api/generated/api';
import { ErrorModal } from "../../components/ErrorModal/ErrorModal";

export function HealthStatus() {
  const { t } = useTranslation();
  const { data, error, isLoading, isError, refetch } = useGetHealthQuery();
  const health = data as { status?: string; database?: string } | undefined;

  const status = isError
    ? t('health.unreachable')
    : isLoading
      ? t('health.checking')
      : t('health.status', { status: health?.status, database: health?.database });

  function retryHealthCheck() {
    void refetch();
  }

  return (
    <>
      <p>{status}</p>
      <ErrorModal
        error={error}
        fallbackMessage={t("errorModal.genericMessage")}
        onContinue={retryHealthCheck}
      />
    </>
  );
}
