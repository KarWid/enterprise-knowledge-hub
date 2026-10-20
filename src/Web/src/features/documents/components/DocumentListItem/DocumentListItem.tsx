import { useTranslation } from "react-i18next";
import type { DocumentResponse } from "../../../../services/api/generated/api";
import styles from "./DocumentListItem.module.less";

interface DocumentListItemProps {
  document: DocumentResponse;
}

export function DocumentListItem({ document }: DocumentListItemProps) {
  const { t } = useTranslation();
  const createdAt = new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(document.createdAt));

  return (
    <li className={styles.document}>
      <span className={styles.documentIcon} aria-hidden="true">
        PDF
      </span>
      <div>
        <strong>{document.name}</strong>
        <span className={styles.documentMeta}>
          {t("documents.uploadedAt", { date: createdAt })}
        </span>
      </div>
      <span className={styles.status}>{t(`documents.status.${document.status}`)}</span>
    </li>
  );
}
