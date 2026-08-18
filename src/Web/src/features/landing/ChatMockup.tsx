import { useTranslation } from 'react-i18next';
import styles from './ChatMockup.module.css';

export function ChatMockup() {
  const { t } = useTranslation();

  return (
    <div className={styles.window}>
      <div className={styles.titleBar}>
        <span className={styles.dot} />
        <span className={styles.dot} />
        <span className={styles.dot} />
        <span className={styles.titleBarLabel}>{t('landing.chatMockup.aiLabel')}</span>
      </div>

      <div className={styles.messages}>
        <div className={styles.userMessage}>
          <p>{t('landing.chatMockup.userMessage')}</p>
        </div>

        <div className={styles.aiMessage}>
          <div className={styles.aiAvatar}>✦</div>
          <p>{t('landing.chatMockup.aiMessage')}</p>
        </div>
      </div>

      <div className={styles.inputBar}>
        <span className={styles.inputPlaceholder}>Ask anything…</span>
        <button className={styles.sendButton} aria-label="Send">↑</button>
      </div>
    </div>
  );
}
