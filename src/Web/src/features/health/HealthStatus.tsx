import { useGetHealthQuery } from '../../services/api';

export function HealthStatus() {
  const { data, isLoading, isError } = useGetHealthQuery();

  if (isError) return <p>API unreachable</p>;
  if (isLoading) return <p>Checking API…</p>;
  return <p>API: {data?.status} | DB: {data?.database}</p>;
}
