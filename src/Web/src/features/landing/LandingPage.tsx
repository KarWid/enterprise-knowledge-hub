import { useTranslation } from 'react-i18next';
import { login } from '../../auth/authService';
import { ChatMockup } from './ChatMockup';
import styles from './LandingPage.module.css';

export function LandingPage() {
  const { t } = useTranslation();

  return (
    <div className={styles.page}>
      {/* Nav */}
      <nav className={styles.nav}>
        <span className={styles.navLogo}>{t('app.title')}</span>
        <button className={styles.navLoginButton} onClick={login}>
          {t('landing.nav.logIn')}
        </button>
      </nav>

      {/* Hero */}
      <section className={styles.hero}>
        <div className={styles.heroContent}>
          <h1 className={styles.heroHeadline}>{t('landing.hero.headline')}</h1>
          <p className={styles.heroSubheadline}>{t('landing.hero.subheadline')}</p>
          <button className={styles.ctaButton} onClick={login}>
            {t('landing.hero.cta')}
          </button>
        </div>
        <div className={styles.heroMockup}>
          <ChatMockup />
        </div>
      </section>

      {/* How it works */}
      <section className={styles.howItWorks}>
        <h2 className={styles.sectionTitle}>{t('landing.howItWorks.title')}</h2>
        <div className={styles.steps}>
          <div className={styles.step}>
            <span className={styles.stepNumber}>1</span>
            <span className={styles.stepLabel}>{t('landing.howItWorks.step1')}</span>
          </div>
          <span className={styles.stepArrow}>→</span>
          <div className={styles.step}>
            <span className={styles.stepNumber}>2</span>
            <span className={styles.stepLabel}>{t('landing.howItWorks.step2')}</span>
          </div>
          <span className={styles.stepArrow}>→</span>
          <div className={styles.step}>
            <span className={styles.stepNumber}>3</span>
            <span className={styles.stepLabel}>{t('landing.howItWorks.step3')}</span>
          </div>
        </div>
      </section>

      {/* Use cases */}
      <section className={styles.useCases}>
        <h2 className={styles.sectionTitle}>{t('landing.useCases.title')}</h2>
        <div className={styles.cards}>
          <div className={styles.card}>
            <span className={styles.cardIcon}>📄</span>
            <h3 className={styles.cardTitle}>{t('landing.useCases.documents.title')}</h3>
            <p className={styles.cardDescription}>{t('landing.useCases.documents.description')}</p>
          </div>
          <div className={styles.card}>
            <span className={styles.cardIcon}>🎙️</span>
            <h3 className={styles.cardTitle}>{t('landing.useCases.meetings.title')}</h3>
            <p className={styles.cardDescription}>{t('landing.useCases.meetings.description')}</p>
          </div>
          <div className={styles.card}>
            <span className={styles.cardIcon}>⚙️</span>
            <h3 className={styles.cardTitle}>{t('landing.useCases.processes.title')}</h3>
            <p className={styles.cardDescription}>{t('landing.useCases.processes.description')}</p>
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className={styles.ctaSection}>
        <h2 className={styles.ctaHeadline}>{t('landing.cta.headline')}</h2>
        <button className={styles.ctaButton} onClick={login}>
          {t('landing.cta.button')}
        </button>
      </section>

      {/* Footer */}
      <footer className={styles.footer}>
        <p>{t('landing.footer.copyright')}</p>
      </footer>
    </div>
  );
}
