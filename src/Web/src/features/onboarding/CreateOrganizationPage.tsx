import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { useCreateOrganizationMutation, useGetMeQuery } from '../../services/api/generated/api';
import { LogoutButton } from '../../components/LogoutButton';
import styles from './CreateOrganizationPage.module.less';

export function CreateOrganizationPage() {
  const { t } = useTranslation();
  const [name, setName] = useState('');
  const { refetch: refetchMe } = useGetMeQuery();
  const [createOrganization, { isLoading, isError }] = useCreateOrganizationMutation();

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!name.trim()) return;
    const result = await createOrganization({ createOrganizationRequest: { name: name.trim() } });
    if (!('error' in result)) {
      refetchMe();
    }
  }

  return (
    <div className={styles.page}>
      <LogoutButton className={styles.logout} />
      <div className={styles.card}>
        <span className={styles.logo}>{t('app.title')}</span>
        <h1 className={styles.heading}>{t('onboarding.createOrganization')}</h1>
        <p className={styles.subtitle}>{t('onboarding.createOrganizationSubtitle')}</p>

        <form onSubmit={handleSubmit} className={styles.form}>
          <label htmlFor="company-name" className={styles.label}>
            {t('onboarding.companyNameLabel')}
          </label>
          <input
            id="company-name"
            type="text"
            className={styles.input}
            placeholder={t('onboarding.companyNamePlaceholder')}
            value={name}
            onChange={e => setName(e.target.value)}
            disabled={isLoading}
            autoFocus
            maxLength={100}
          />
          {isError && (
            <p className={styles.error} role="alert">
              {t('onboarding.createOrganizationError')}
            </p>
          )}
          <button
            type="submit"
            className={styles.button}
            disabled={isLoading || !name.trim()}
          >
            {isLoading ? t('app.pleaseWait') : t('onboarding.createOrganizationButton')}
          </button>
        </form>
      </div>
    </div>
  );
}
