import { useTranslation } from "react-i18next";
import type { DocumentResponse } from "../../../../services/api/generated/api";
import { DocumentListItem } from "../DocumentListItem/DocumentListItem";
import styles from "./DocumentList.module.less";

interface DocumentListProps {
  documents: DocumentResponse[];
  isLoading: boolean;
}

export function DocumentList({ documents, isLoading }: DocumentListProps) {
  const { t } = useTranslation();

  if (isLoading) return <p className={styles.muted}>{t("app.pleaseWait")}</p>;
  if (documents.length === 0) return <p className={styles.muted}>{t("documents.empty")}</p>;

  return (
    <ul className={styles.list}>
      {documents.map(document => (
        <DocumentListItem key={document.id} document={document} />
      ))}
    </ul>
  );
}
